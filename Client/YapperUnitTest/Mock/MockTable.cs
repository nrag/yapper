using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Data.Linq;
using System.Collections.Generic;
using System.Linq;

namespace YapperUnitTest.Mock
{
    public class MockTable<T> : ITable<T> where T : class
    {
        private List<T> list;

        public MockTable(List<T> list)
        {
            this.list = list;
        }

        public int Count
        {
            get
            {
                return this.list.Count;
            }
        }

        public void Attach(T entity)
        {
            
        }

        public void DeleteOnSubmit(T entity)
        {
            this.list.Remove(entity);
        }

        public void InsertOnSubmit(T entity)
        {
            if (!this.list.Contains(entity))
            {
                this.list.Add(entity);
            }
            else
            {
                throw new Exception("Entity already exists");
            }
        }

        public System.Collections.Generic.IEnumerator<T> GetEnumerator()
        {
            return this.list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.list.GetEnumerator();
        }

        public Type ElementType
        {
            get 
            {
                return this.list[0].GetType();
            }
        }

        public System.Linq.Expressions.Expression Expression
        {
            get { return this.list.AsQueryable<T>().Expression; }
        }

        public System.Linq.IQueryProvider Provider
        {
            get { return this.list.AsQueryable<T>().Provider; }
        }
    }
}
