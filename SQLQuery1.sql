CREATE TABLE Locations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100),
    Latitude FLOAT,
    Longitude FLOAT,
    Timestamp DATETIME DEFAULT GETDATE()
);
