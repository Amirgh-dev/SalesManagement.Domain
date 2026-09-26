IF EXISTS (
    SELECT 1
    FROM sys.columns AS c
        INNER JOIN sys.types AS t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.PostProfile') AND c.name = N'CreationDate' AND t.name = N'date')
    ALTER TABLE dbo.PostProfile ALTER COLUMN CreationDate datetime NULL;
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns AS c
        INNER JOIN sys.types AS t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.PartyComment') AND c.name = N'CreationDate' AND t.name = N'date')
    ALTER TABLE dbo.PartyComment ALTER COLUMN CreationDate datetime NULL;
GO
