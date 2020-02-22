USE [CautiousPancake]
GO

CREATE TABLE [Campaigns] (
    [ID] INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
    [Name] VARCHAR(50) NOT NULL,
    [Size] INT NOT NULL,
    [CodeIDStart] INT NOT NULL,
    [CodeIDEnd] AS [CodeIDStart] + [Size] - 1
)
GO