using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using YapperChat.Common;
using System.Linq;
using System.ComponentModel;

namespace YapperChat.Controls.Interactions
{
    /// <summary>
    /// A base class for interactions.
    /// </summary>
    public abstract class InteractionBase<T> : IInteraction<T> 
        where T : class, INotifyPropertyChanged, IComparable, IItem
    {
        private bool _isActive = false;

        public ItemsControl interactionListControl;
        public ObservableSortedList<T> interactionList;
        protected ScrollViewer _scrollViewer;
        protected IItemManager<T> itemManager;

        public virtual void Initialize(ItemsControl todoList, IItemManager<T> itemManager, ObservableSortedList<T> list)
        {
            this.interactionListControl = todoList;
            this.interactionList = list;
            this.itemManager = itemManager;

            // when the ItemsControl has been rendered, we can locate the ScrollViewer
            // that is within its template.
            this.interactionListControl.InvokeOnNextLayoutUpdated(() => LocateScrollViewer());

            this.IsEnabled = false;
        }

        private void LocateScrollViewer()
        {
            _scrollViewer = interactionListControl.Descendants<ScrollViewer>()
                                    .Cast<ScrollViewer>()
                                    .Single();

            // allow interactions to perform some action when the ScrollViewer has been located
            // such as add event handlers
            ScrollViewerLocated(_scrollViewer);
        }

        protected virtual void ScrollViewerLocated(ScrollViewer scrollViewer)
        {
        }

        public virtual void AddElement(FrameworkElement rootElement)
        {
        }

        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                _isActive = value;

                if (_isActive == true)
                {
                    if (Activated != null)
                    {
                        Activated(this, EventArgs.Empty);
                    }
                }
                else
                {
                    if (DeActivated != null)
                    {
                        DeActivated(this, EventArgs.Empty);
                    }
                }
            }
        }

        public bool IsEnabled { get; set; }

        public event EventHandler Activated;

        public event EventHandler DeActivated;

        /// <summary>
        /// Some interactions involve adding transformations or performing other visual modifications
        /// to items within the list. When the interaction is complete, we need to remove these and return
        /// the list to its original state. This method simply forces the ItemsControl to re-render all items.
        /// </summary>
        protected void RefreshView()
        {
            interactionList.Reset();
        }
    }
}