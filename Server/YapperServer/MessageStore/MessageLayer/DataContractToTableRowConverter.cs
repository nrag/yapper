using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using MessageStore.Database;

namespace MessageStore.MessageLayer
{
    static class DataContractToTableRowConverter
    {
        public static TableRow ConvertToTableRow(this Message dataContract)
        {
            if (!DataContractToTableRowConverter.CheckIfDataContract(dataContract.GetType()))
            {
                return null;
            }

            return new MessageTableRow(DataContractToTableRowConverter.GetYapperColumnValues(dataContract));
        }

        public static Message ConvertToMessage(ITable table, ITableRow tableRow)
        {
            Message message = null;
            if (table.BlobValueColumn != null)
            {
                byte[] value = (byte[])tableRow.ColumnValues[table.BlobValueColumn];
                message = MessageSerializer.DeSerializeFromProtocolBuffer(value);
            }
            else
            {
                message = new Message();
            }

            DataContractToTableRowConverter.SetYapperColumnValues(message, tableRow);

            return message;
        }

        private static Dictionary<IColumn, object> GetYapperColumnValues(object dataContract)
        {
            PropertyInfo[] propertyInfoArray = dataContract.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Dictionary<IColumn, object> columnValues = new Dictionary<IColumn,object>();

            foreach (PropertyInfo propInfo in propertyInfoArray)
            {
                IEnumerable<Attribute> attrs = propInfo.GetCustomAttributes();
                if (attrs == null)
                {
                    continue;
                }

                foreach (Attribute a in attrs)
                {
                    if (a is YapperColumnAttribute)
                    {
                        YapperColumnAttribute yapperColumn = a as YapperColumnAttribute;

                        IColumn column = MessageTable.GetColumnFromName(yapperColumn.Name);
                        if (column != null)
                        {
                            columnValues.Add(column, DataContractToTableRowConverter.Convert(propInfo.GetValue(dataContract), column.Type));
                        }
                    }
                }
            }

            return columnValues;
        }

        private static void SetYapperColumnValues(Message message, ITableRow row)
        {
            PropertyInfo[] propertyInfoArray = message.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo propInfo in propertyInfoArray)
            {
                IEnumerable<Attribute> attrs = propInfo.GetCustomAttributes();
                if (attrs == null)
                {
                    continue;
                }

                IColumn column = null;
                bool isDbColumn = false;
                bool isInBlob = false; ;
                foreach (Attribute a in attrs)
                {
                    if (a is YapperColumnAttribute && ((YapperColumnAttribute)a).ColumnLocation == ColumnLocation.Database)
                    {
                        isDbColumn = true;
                        YapperColumnAttribute yapperColumn = a as YapperColumnAttribute;
                        column = MessageTable.GetColumnFromName(yapperColumn.Name);
                    }

                    if (a is ProtoBuf.ProtoMemberAttribute)
                    {
                        isInBlob = true;
                    }
                }

                if (column != null && isDbColumn && !isInBlob)
                {
                    propInfo.SetValue(message, row.ColumnValues[column]);
                }
            }
        }

        private static object Convert(object value, Type type)
        {
            try
            {
                return System.Convert.ChangeType(value, type);
            }
            catch(InvalidCastException ex)
            {
                return null;
            }
            catch (FormatException ex)
            {
                return null;
            }
            catch (OverflowException ex)
            {
                return null;
            }
        }

        private static bool CheckIfDataContract(Type type)
        {
            Attribute[] attrs = Attribute.GetCustomAttributes(type);  // Reflection. 

            // Displaying output. 
            foreach (Attribute attr in attrs)
            {
                if (attr is YapperTableAttribute)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
