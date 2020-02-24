using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Azure.Storage.Blob;

namespace CodeFlip.CodeJar.Api.Controllers
{
    public class SQL
    {
        public SQL(string connectionString)
        {
            Connection = new SqlConnection(connectionString);
        }
        private SqlConnection Connection { get; set; }

        /// <summary>
        /// Inserts batch information in the database
        /// </summary>
        public void CreateCampaign(Campaign campaign, List<Code> codes)
        {
            Connection.Open();

            // Begin transaction
            var command = Connection.CreateCommand();
            var transaction = Connection.BeginTransaction();
            command.Transaction = transaction;

            try
            {
                // Insert Campaign
                command.CommandText = @"
                    DECLARE @codeIDStart int
                    SET @codeIDStart = (SELECT ISNULL(MAX(CodeIDEnd), 0) FROM Campaigns) + 1
                    INSERT INTO Campaigns (Name, Size, CodeIDStart)
                    VALUES (@name, @size, @codeIDStart)
                    SELECT SCOPE_IDENTITY()
                ";
                command.Parameters.AddWithValue("@name", campaign.Name);
                command.Parameters.AddWithValue("@size", campaign.Size);
                campaign.ID = Convert.ToInt32(command.ExecuteScalar());

                // Create codes
                CreateCodes(codes, command);
                transaction.Commit();
            }
            catch(Exception e)
            {
                transaction.Rollback();
            }

            Connection.Close();
        }

        /// <summary>
        /// Inserts generated codes into the database
        /// </summary>
        public void CreateCodes(List<Code> codes, SqlCommand command)
        {
            foreach(var code in codes)
            {
                command.CommandText = @"
                    INSERT INTO Codes (State, SeedValue)
                    VALUES (@state, @seedValue)";

                command.Parameters.Clear();
                command.Parameters.AddWithValue("@state", States.Active);
                command.Parameters.AddWithValue("@seedValue", code.SeedValue);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Returns a list of all the campaigns in the database
        /// </summary>
        public List<Campaign> GetAllCampaigns()
        {
            var campaigns = new List<Campaign>();

            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT * FROM Campaigns
                ";

                using(var reader = command.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        var campaign = new Campaign()
                        {
                            ID = (int)reader["ID"],
                            Name = (string)reader["Name"],
                            Size = (int)reader["Size"]
                        };
                        campaigns.Add(campaign);
                    }
                }
            }

            Connection.Close();
            return campaigns;
        }

        /// <summary>
        /// Get a single campaign from the database by ID
        /// </summary>
        public Campaign GetCampaignByID(int id)
        {
            var campaign = new Campaign();
            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT [ID], [Name], [Size] FROM Campaigns
                    WHERE ID = @id
                ";
                command.Parameters.AddWithValue("@id", id);

                using(var reader = command.ExecuteReader())
                {
                    if(reader.Read())
                    {
                        campaign.ID = (int)reader["ID"];
                        campaign.Name = (string)reader["Name"];
                        campaign.Size = (int)reader["Size"];
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            Connection.Close();
            return campaign;
        }

        public long[] UpdateOffset(int campaignSize)
        {
            var prevAndNextOffset = new long[2];
            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE Offset SET OffsetValue = OffsetValue + @nextOffset
                    OUTPUT INSERTED.OffsetValue
                    WHERE ID = 1
                ";
                command.Parameters.AddWithValue("@nextOffset", campaignSize * 4);
                var insertedOffset = (long)command.ExecuteScalar();
                prevAndNextOffset[0] = insertedOffset - campaignSize * 4;
                prevAndNextOffset[1] = insertedOffset;
            }

            Connection.Close();

            return prevAndNextOffset;
        }

        public bool DeactivateCampaign(int campaignID)
        {
            int codesAffected;
            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText = @"
                    DECLARE @codeIDStart int
                    DECLARE @codeIDEnd int
                    SET @codeIDStart = (SELECT CodeIDStart FROM Campaigns WHERE ID = @campaignID)
                    SET @codeIDEnd = (SELECT CodeIDEnd FROM Campaigns WHERE ID = @campaignID)

                    UPDATE Codes SET [State] = @inactive
                    WHERE ID BETWEEN @codeIDStart AND @codeIDEnd
                    AND [State] = @active
                ";
                command.Parameters.AddWithValue("@active", States.Active);
                command.Parameters.AddWithValue("@inactive", States.Inactive);
                command.Parameters.AddWithValue("@campaignID", campaignID);
                codesAffected = command.ExecuteNonQuery();
            }

            Connection.Close();
            if(codesAffected >= 1)
            {
                return true;
            }
            return false;
        }

        public bool RedeemCode(int seedValue, string email)
        {
            var rowsAffected = 0;
            Connection.Open();

            var transaction = Connection.BeginTransaction();
            var command = Connection.CreateCommand();
            command.Transaction = transaction;

            try
            {
                command.CommandText = @"
                    UPDATE Codes SET [State] = @redeemed
                    WHERE SeedValue = @seedValue
                    AND [State] = @active
                ";
                command.Parameters.AddWithValue("@redeemed", States.Redeemed);
                command.Parameters.AddWithValue("@active", States.Active);
                command.Parameters.AddWithValue("@seedValue", seedValue);
                command.Parameters.AddWithValue("@email", email);
                rowsAffected = command.ExecuteNonQuery();

                command.CommandText = @"
                    INSERT INTO RedeemAttempts (CodeSeedValue, Email)
                    VALUES (@seedValue, @email)
                ";
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            catch(Exception e)
            {
                transaction.Rollback();
                return false;
            }

            Connection.Close();

            if(rowsAffected > 0)
            {
                return true;
            }
            return false;
        }

        public void DeactivateCode(int campaignID, int seedValue)
        {
            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE Codes SET [State] = @inactive
                    WHERE SeedValue = @seedValue
                    AND [State] = @active
                ";
                command.Parameters.AddWithValue("@active", States.Active);
                command.Parameters.AddWithValue("@inactive", States.Inactive);
                command.Parameters.AddWithValue("@seedValue", seedValue);
                command.ExecuteNonQuery();
            }

            Connection.Close();
        }

        public TableData GetCodes(int campaignID, int pageNumber, int pageSize, CodeConverter codeConverter)
        {
            int campaignSize = 0;
            int codeIDStart = 0;
            int codeIDEnd = 0;
            var td = new TableData(pageSize, pageNumber);
            List<Code> codes = new List<Code>();
            Connection.Open();

            var transaction = Connection.BeginTransaction();
            var command = Connection.CreateCommand();
            command.Transaction = transaction;

            try
            {
                // Get information about the campaign
                command.CommandText = @"
                    SELECT [Size], CodeIDStart, CodeIDEnd FROM Campaigns
                    WHERE ID = @campaignID
                ";
                command.Parameters.AddWithValue("@campaignID", campaignID);
                using(var reader = command.ExecuteReader())
                {
                    if(reader.Read())
                    {
                        campaignSize = (int)reader["Size"];
                        codeIDStart = (int)reader["CodeIDStart"];
                        codeIDEnd = (int)reader["CodeIDEnd"];
                    }
                }

                // Set page count
                td.SetPageCount(campaignSize);

                // Get a page of codes from the campaign
                command.CommandText = @"
                    SELECT * FROM Codes
                    WHERE ID BETWEEN @codeIDStart AND @codeIDEnd
                    ORDER BY ID OFFSET @rowOffset ROWS
                    FETCH NEXT @pageSize ROWS ONLY
                ";
                command.Parameters.AddWithValue("@codeIDStart", codeIDStart);
                command.Parameters.AddWithValue("@codeIDEnd", codeIDEnd);
                command.Parameters.AddWithValue("@rowOffset", td.RowOffset);
                command.Parameters.AddWithValue("@pageSize", pageSize);

                // Read codes
                using(var reader = command.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        var code = new Code
                        {
                            StringValue = codeConverter.ConvertToCode((int)reader["SeedValue"]),
                            State = States.ConvertToString((byte)reader["State"])
                        };
                        codes.Add(code);
                    }
                }

                transaction.Commit();

                // Add codes to the table data
                td.Codes = codes;
            }
            catch(Exception e)
            {
                transaction.Rollback();
            }

            Connection.Close();
            return td;
        }

        public Code SearchCode(string stringValue, CodeConverter codeConverter)
        {
            var code = new Code();
            var seedValue = codeConverter.ConvertFromCode(stringValue);
            code.SeedValue = seedValue;
            code.StringValue = stringValue;

            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT [State] FROM Codes
                    WHERE SeedValue = @seedValue
                ";
                command.Parameters.AddWithValue("@seedValue", code.SeedValue);
                using(var reader = command.ExecuteReader())
                {
                    if(reader.Read())
                    {
                        code.State = States.ConvertToString((byte)reader["State"]);
                    }
                }
            }

            Connection.Close();
            return code.State == null ? null : code;
        }
    }
}