
CREATE TABLE GroupTable
(
ID int PRIMARY KEY IDENTITY,
UserId int NOT NULL,
UserPhoneNumber nvarchar(25) NOT NULL,
UserName nvarchar(255) NOT NULL
)