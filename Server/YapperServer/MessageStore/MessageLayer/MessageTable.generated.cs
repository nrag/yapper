
using System; 
using System.Collections.Generic;
using System.Runtime.Serialization;

using MessageStore.Database;

namespace MessageStore.MessageLayer
{
      partial class MessageTable : DatabaseTable
      {
          private static List<IColumn> MessageTableColumns;

          private static MessageTable _instance = new MessageTable();

          private IColumn blobNameColumn;

          private IColumn blobValueColumn;

          private IColumn identityColumn;

    
          public static IColumn MessageIdColumn = new DatabaseColumn(){
              Name="MessageId",
              Type=typeof(Guid).IsEnum ? typeof(int) : typeof(Guid),
              ColumnLocation = ColumnLocation.Database,
              IsRequired = true};
      
          public override IColumn BlobNameColumn
          {
              get
              {
                  return MessageTable.MessageIdColumn;
              }
          }
          
          public static IColumn ConversationIdColumn = new DatabaseColumn(){
              Name="ConversationId",
              Type=typeof(Guid).IsEnum ? typeof(int) : typeof(Guid),
              ColumnLocation = ColumnLocation.Database,
              IsRequired = true};
      
          public override IColumn BlobContainerColumn
          {
              get
              {
                  return MessageTable.ConversationIdColumn;
              }
          }
          
          public static IColumn MessageFlagsColumn = new DatabaseColumn(){
              Name="MessageFlags",
              Type=typeof(MessageFlags).IsEnum ? typeof(int) : typeof(MessageFlags),
              ColumnLocation = ColumnLocation.Database,
              IsRequired = true};
      
          public static IColumn SenderIdColumn = new DatabaseColumn(){
              Name="SenderId",
              Type=typeof(int).IsEnum ? typeof(int) : typeof(int),
              ColumnLocation = ColumnLocation.Database,
              IsRequired = true};
      
          public static IColumn RecipientIdColumn = new DatabaseColumn(){
              Name="RecipientId",
              Type=typeof(int).IsEnum ? typeof(int) : typeof(int),
              ColumnLocation = ColumnLocation.Database,
              IsRequired = true};
      
          public static IColumn LastUpdateTimeUtcTicksColumn = new DatabaseColumn(){
              Name="LastUpdateTimeUtcTicks",
              Type=typeof(long).IsEnum ? typeof(int) : typeof(long),
              ColumnLocation = ColumnLocation.Database,
              IsRequired = true};
      

          static MessageTable()
          {
              if (MessageTable.MessageTableColumns == null)
              {
                  MessageTable.MessageTableColumns = new List<IColumn>();
          
                  MessageTable.MessageTableColumns.Add(MessageTable.MessageIdColumn);
                  MessageTable.MessageTableColumns.Add(MessageTable.ConversationIdColumn);
                  MessageTable.MessageTableColumns.Add(MessageTable.MessageFlagsColumn);
                  MessageTable.MessageTableColumns.Add(MessageTable.SenderIdColumn);
                  MessageTable.MessageTableColumns.Add(MessageTable.RecipientIdColumn);
                  MessageTable.MessageTableColumns.Add(MessageTable.LastUpdateTimeUtcTicksColumn);
                  MessageTable.MessageTableColumns.Add(new DatabaseColumn(){
                      Name="MessageBlobName",
                      Identity=false,
                      Type=typeof(Guid),
                      ColumnLocation = ColumnLocation.Database,
                      IsRequired = true,
                      IsBlobName = true});
                  
                  MessageTable.MessageTableColumns.Add(new DatabaseColumn(){
                      Name="MessageBlobValue",
                      Identity=false,
                      Type=typeof(byte[]),
                      ColumnLocation = ColumnLocation.BlobStore,
                      IsRequired = true});
  
              }
          }

          public static ITable Instance
          {
              get
              {
                  return MessageTable._instance;
              }
          }

          public static IColumn GetColumnFromName(string name)
          {
              return MessageTable.MessageTableColumns.Find( x => string.Compare(x.Name, name, StringComparison.OrdinalIgnoreCase) == 0);
          }

          public override string Name
          {
              get
              {
                  return "MessageTable";
              }
          }

          public override List<IColumn> Columns
          {
              get
              {
                  return MessageTable.MessageTableColumns;
              } 
          }

          public override IColumn BlobValueColumn
          {
              get
              {
                  if (this.blobValueColumn == null)
                  {
                      foreach (IColumn column in MessageTable.MessageTableColumns)
                      {
                          if (column.ColumnLocation == ColumnLocation.BlobStore)
                          {
                              this.blobValueColumn = column;
                          }
                      }
                  }

                  return this.blobValueColumn;
              }
          }
          
          public override IColumn Identity
          {
              get
              {
                  if (this.identityColumn == null)
                  {
                      foreach (IColumn column in MessageTable.MessageTableColumns)
                      {
                          if (column.Identity == true)
                          {
                              this.identityColumn = column;
                          }
                      }
                  }

                  return this.identityColumn;
              }
          }
          
      }
}
