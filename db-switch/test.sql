IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DbSwitchStatus')
BEGIN
    CREATE TABLE DbSwitchStatus (LastUpdate DATETIME, Info VARCHAR(100));
END

INSERT INTO DbSwitchStatus (LastUpdate, Info) 
VALUES (GETDATE(), 'Restore');