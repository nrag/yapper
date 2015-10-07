using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace YapperUnitTest
{
    public class NotifyCollectionChangedTester<T>
    {
        public NotifyCollectionChangedTester(ObservableCollection<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("viewModel", "Argument cannot be null.");
            }

            collection.CollectionChanged += new NotifyCollectionChangedEventHandler(collection_PropertyChanged);
        }

        public int Count
        {
            get;
            set;
        }

        void collection_PropertyChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // We are not including olditems here because
            // some of our implementation removes and adds the
            // object from observablecollection. In which case, we'll be double counting.
            if (e.NewItems != null)
            {
                this.Count += e.NewItems.Count;
            }
        }
    }

    /// <summary>
    /// Used for testing changes in collection of collections
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NotifyCollectionOfCollectionChangedTester<T>
    {
        public NotifyCollectionOfCollectionChangedTester(ObservableCollection<ObservableCollection<T>> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("viewModel", "Argument cannot be null.");
            }

            collection.CollectionChanged += new NotifyCollectionChangedEventHandler(collection_PropertyChanged);
        }

        public int Count
        {
            get;
            set;
        }

        void collection_PropertyChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            lock (this)
            {
                if (e.NewItems != null)
                {
                    foreach (ObservableCollection<T> collection in e.NewItems)
                    {
                        collection.CollectionChanged += new NotifyCollectionChangedEventHandler(childCollection_PropertyChanged);
                        this.Count += collection.Count;
                    }
                }
            }
        }

        void childCollection_PropertyChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            lock (this)
            {
                if (e.NewItems != null)
                {
                    this.Count += e.NewItems.Count;
                }

                if (e.OldItems != null)
                {
                    this.Count += e.OldItems.Count;
                }
            }
        }
    }
}
