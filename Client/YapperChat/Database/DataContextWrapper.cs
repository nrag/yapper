using System;
using System.Data.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace YapperChat.Database
{
    /// <summary>
    /// Wrapper around DataContext to make it unittestable
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataContextWrapper<T> : IDataContextWrapper where T : DataContext, new()
    {
        /// <summary>
        /// Instance of the real datacontext
        /// </summary>
        private readonly T db;

        /// <summary>
        /// If true, it's been disposed already
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Creates an instance of DataContextWrapper
        /// </summary>
        public DataContextWrapper()
        {
            var t = typeof(T);
            db = (T)Activator.CreateInstance(t);
        }

        /// <summary>
        /// Creates an instance of DataContextWrapper to the datatabase
        /// </summary>
        /// <param name="connectionString"></param>
        public DataContextWrapper(string connectionString)
        {
            var t = typeof(T);
            db = (T)Activator.CreateInstance(t, connectionString);
        }

        #region IDataContextWrapper Members

        /// <summary>
        /// Tables this instance.
        /// </summary>
        /// <typeparam name="TableName"></typeparam>
        /// <returns></returns>
        public ITable<TableName> Table<TableName>() where TableName : class
        {
            lock (this.db)
            {
                return (Table<TableName>)db.GetTable(typeof(TableName));
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="entities"></param>
        public void DeleteAllOnSubmit<Entity>(IEnumerable<Entity> entities) where Entity : class
        {
            lock (this.db)
            {
                db.GetTable(typeof(Entity)).DeleteAllOnSubmit(entities);
            }
        }

        public void DeleteOnSubmit<Entity>(Entity entity) where Entity : class
        {
            lock (this.db)
            {
                db.GetTable(typeof(Entity)).DeleteOnSubmit(entity);
            }
        }

        public void InsertOnSubmit<Entity>(Entity entity) where Entity : class
        {
            lock (this.db)
            {
                db.GetTable(typeof(Entity)).InsertOnSubmit(entity);
            }
        }

        public void Attach<Entity>(Entity entity) where Entity : class
        {
            lock (this.db)
            {
                db.GetTable(typeof(Entity)).Attach(entity);
            }
        }

        public void SubmitChanges()
        {
            lock (this.db)
            {
                StringBuilder sb = new StringBuilder();

                try
                {
                    db.SubmitChanges();
                }
                catch (ChangeConflictException cce)
                {
                    sb.Append(cce);
                    foreach (ObjectChangeConflict occ in db.ChangeConflicts)
                    {
                        MetaTable metatable = db.Mapping.GetTable(occ.Object.GetType());
                        sb.AppendFormat("\nTable name: {0}\n", metatable.TableName);
                        foreach (MemberChangeConflict mcc in occ.MemberConflicts)
                        {
                            sb.AppendFormat("Member: {0}", mcc.Member);
                            sb.AppendFormat("\tCurrent  value: {0}", mcc.CurrentValue);
                            sb.AppendFormat("\tOriginal value: {0}", mcc.OriginalValue);
                            sb.AppendFormat("\tDatabase value: {0}", mcc.DatabaseValue);
                        }
                    }
                }
            }
        }

        #endregion

        // Dispose Members...

        public void Dispose()
        {
            if (!this._disposed)
            {
                this.db.Dispose();
                this._disposed = true;
            }
        }
    }
}
