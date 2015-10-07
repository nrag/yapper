
CREATE TABLE NotificationSubscriptionTable
(
ID int PRIMARY KEY IDENTITY,
PhoneNumber nvarchar(25) NOT NULL,
Name nvarchar(255) NOT NULL,
Secret varchar(50),
LastSyncTime datetime
)