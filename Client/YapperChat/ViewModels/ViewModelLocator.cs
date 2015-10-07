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
using System.Collections.Generic;

namespace YapperChat.ViewModels
{
    /// <summary>
    /// This is used to locate and load viewmodels. 
    /// This uses WeakReference so as to not hold 
    /// the viewmodel even after garbage collection
    /// </summary>
    public class ViewModelLocator
    {
        private static object DummyKey = new object();

        private Dictionary<Type, Dictionary<object, WeakReference>> viewModelDictionary = new Dictionary<Type, Dictionary<object, WeakReference>>();

        private static ViewModelLocator instance = new ViewModelLocator();

        public static ViewModelLocator Instance
        {
            get
            {
                return ViewModelLocator.instance;
            }
        }

        public T CreateOrGetViewModel<T>(object key = null) where T: new()
        {
            lock (this.viewModelDictionary)
            {
                if (key == null)
                {
                    key = ViewModelLocator.DummyKey;
                }

                T viewModel = this.GetNoLock<T>(key);

                if (viewModel != null)
                {
                    return viewModel;
                }

                viewModel = new T();

                this.AddNoLock<T>(key, viewModel);
                return viewModel;
            }
        }

        public void Add<T>(object key, T value)
        {
            lock (this.viewModelDictionary)
            {
                this.AddNoLock<T>(key, value);
            }
        }

        public T Get<T>(object key)
        {
            lock (this.viewModelDictionary)
            {
                return this.GetNoLock<T>(key);
            }
        }

        private void AddNoLock<T>(object key, T value)
        {
            if (key == null)
            {
                key = ViewModelLocator.DummyKey;
            }

            if (!this.viewModelDictionary.ContainsKey(typeof(T)))
            {
                Dictionary<object, WeakReference> cacheForType = new Dictionary<object, WeakReference>();
                this.viewModelDictionary.Add(typeof(T), cacheForType);
            }

            WeakReference val = null;
            this.viewModelDictionary[typeof(T)].TryGetValue(key, out val);

            if (val == null)
            {
                this.viewModelDictionary[typeof(T)].Add(key, new WeakReference(value));
            }
        }

        private T GetNoLock<T>(object key)
        {
            if (this.viewModelDictionary.ContainsKey(typeof(T)))
            {
                Dictionary<object, WeakReference> typeObjects = this.viewModelDictionary[typeof(T)];

                if (typeObjects.ContainsKey(key))
                {
                    WeakReference reference = typeObjects[key];
                    if (reference.IsAlive)
                    {
                        return (T)reference.Target;
                    }
                    else
                    {
                        typeObjects.Remove(key);
                    }
                }
            }

            return default(T);
        }
    }
}
