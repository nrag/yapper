using System;
using System.Collections.Generic;
using System.Data.Linq;
using YapperChat.Database;

namespace YapperUnitTest.Mock
{
    public class MockDataContextWrapper : IDataContextWrapper
    {
        private readonly MockDatabase _mockDatabase;

        public MockDataContextWrapper(MockDatabase mockDatabase)
        {
            _mockDatabase = mockDatabase;
        }

        public ITable<T> Table<T>() where T : class
        {
            return new MockTable<T>((List<T>)this._mockDatabase.Tables[typeof(T)]);
        }

        public void DeleteAllOnSubmit<T>(IEnumerable<T> entities) where T : class
        {
            
        }

        public void DeleteOnSubmit<T>(T entity) where T : class
        {
            
        }

        public void InsertOnSubmit<T>(T entity) where T : class
        {
            
        }

        public void SubmitChanges()
        {
            
        }

        public void Dispose()
        {
            
        }
    }
}
