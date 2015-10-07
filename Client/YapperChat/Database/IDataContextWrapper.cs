using System;
using System.Data.Linq;
using System.Collections.Generic;

namespace YapperChat.Database
{
    public interface IDataContextWrapper : IDisposable
    {
        ITable<T> Table<T>() where T : class;

        void DeleteAllOnSubmit<T>(IEnumerable<T> entities) where T : class;
        
        void DeleteOnSubmit<T>(T entity) where T : class;
        
        void InsertOnSubmit<T>(T entity) where T : class;

        void Attach<T>(T entity) where T : class;

        void SubmitChanges();
    }
}
