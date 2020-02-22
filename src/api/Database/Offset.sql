USE [CautiousPancake]
GO

CREATE TABLE [Offset] (
    ID INT PRIMARY KEY NOT NULL,
    OffsetValue BIGINT NOT NULL
)
GO

INSERT INTO [Offset] (ID, OffsetValue)
VALUES (1,0)