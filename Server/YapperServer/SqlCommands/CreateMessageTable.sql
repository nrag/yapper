
CREATE TABLE MessageTable
(
MessageId bigint PRIMARY KEY IDENTITY,
SenderId int NOT NULL,
RecipientId int NOT NULL,
SenderPhoneNumber nvarchar(25) NOT NULL,
SenderName nvarchar(255) NOT NULL,
ConversationId nvarchar(100) NOT NULL,
MessageType int NOT NULL,
MessageUtcTime datetime NOT NULL,
MessageText nvarchar(255),
MessageBlobLocation nvarchar(1000))