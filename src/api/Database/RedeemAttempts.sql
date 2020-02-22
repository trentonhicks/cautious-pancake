USE [CautiousPancake]
GO

CREATE TABLE [RedeemAttempts] (
    ID INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
    CodeSeedValue INT NOT NULL,
    Email VARCHAR(70) NOT NULL
)
GO