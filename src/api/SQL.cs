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
    }
}