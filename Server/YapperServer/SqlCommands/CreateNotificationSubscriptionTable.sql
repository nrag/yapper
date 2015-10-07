
CREATE TABLE SubscriptionTable
(
DeviceId nvarchar(60) NOT NULL,
SubscriptionType int NOT NULL,
UserId int NOT NULL,
PushUrl nvarchar(255) NOT NULL
PRIMARY KEY (DeviceId, SubscriptionType)
)