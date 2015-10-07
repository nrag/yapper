
SELECT TOP 20 MessageId, SenderId, MessageBlob, PostDateTime from dbo.MessageTable WHERE ConversationId = @conversationId ORDER BY PostDateTime DESC