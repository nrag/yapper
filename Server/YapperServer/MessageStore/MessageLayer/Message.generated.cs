
using System; 
using System.Collections.Generic;
using System.Runtime.Serialization;

using MessageStore.Database;
using ProtoBuf;

namespace MessageStore.MessageLayer
{
      [YapperTable(Name="MessageTable")]
      [DataContract]
      [ProtoContract]
      public partial class Message
      {
           [YapperColumn(Name="MessageId", Type=typeof(Guid), ColumnLocation=ColumnLocation.Database)]
           [DataMember]
           [ProtoMember(1, IsRequired = true)]
           public Guid MessageId
           {
               get;
               set;
           }
    
           [YapperColumn(Name="ConversationId", Type=typeof(Guid), ColumnLocation=ColumnLocation.Database)]
           [DataMember]
           [ProtoMember(2, IsRequired = true)]
           public Guid ConversationId
           {
               get;
               set;
           }
    
           [YapperColumn(Name="MessageFlags", Type=typeof(MessageFlags), ColumnLocation=ColumnLocation.Database)]
           [DataMember]
           [ProtoMember(3, IsRequired = true)]
           public MessageFlags MessageFlags
           {
               get;
               set;
           }
    
           [YapperColumn(Name="SenderId", Type=typeof(int), ColumnLocation=ColumnLocation.Database)]
           [ProtoMember(4, IsRequired = true)]
           public int SenderId
           {
               get;
               set;
           }
    
           [YapperColumn(Name="RecipientId", Type=typeof(int), ColumnLocation=ColumnLocation.Database)]
           [ProtoMember(5, IsRequired = true)]
           public int RecipientId
           {
               get;
               set;
           }
    
           [YapperColumn(Name="LastUpdateTimeUtcTicks", Type=typeof(long), ColumnLocation=ColumnLocation.Database)]
           [DataMember]
           [ProtoMember(6, IsRequired = true)]
           public long LastUpdateTimeUtcTicks
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(7, IsRequired = true)]
           public long PostDateTimeUtcTicks
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(8, IsRequired = false)]
           public string TextMessage
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(9, IsRequired = false)]
           public byte[] Image
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(10, IsRequired = false)]
           public List<string> PollOptions
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(11, IsRequired = false)]
           public Guid? PollMessageId
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(12, IsRequired = false)]
           public string PollResponse
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(13, IsRequired = false)]
           public Guid ClientMessageId
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(14, IsRequired = false)]
           public double? LocationLatitude
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(15, IsRequired = false)]
           public double? LocationLongitude
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(16, IsRequired = false)]
           public long? AppointmentDateTimeTicks
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(17, IsRequired = false)]
           public string TaskName
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(18, IsRequired = false)]
           public Guid? TaskId
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(19, IsRequired = false)]
           public List<Message> TaskItemList
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(20, IsRequired = false)]
           public bool? TaskIsCompleted
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(21, IsRequired = false)]
           public byte[] EncryptedMessage
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(22, IsRequired = false)]
           public long? AppointmentDuration
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(23, IsRequired = false)]
           public bool? IsTaskDeleted
           {
               get;
               set;
           }
    
           [DataMember]
           [ProtoMember(24, IsRequired = false)]
           public string ItemOrder
           {
               get;
               set;
           }
    
            [YapperColumn(Name="MessageBlobName", Type=typeof(Guid), ColumnLocation=ColumnLocation.Database, IsBlobName = true)]
            public Guid  MessageBlobName
            {
                get;
                set;
            }

            [YapperColumn(Name="MessageBlobValue", Type=typeof(byte[]), ColumnLocation=ColumnLocation.BlobStore)]
            public byte[]  MessageBlobValue
            {
                get
                {
                    return this.GetMessageBlobValue();
                }

                set
                {
                    this.ParseMessageBlobValue(value);
                }
            }
  
      }
}
