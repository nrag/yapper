using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using YapperChat.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.ComponentModel;

namespace YapperChat.Controls.Interactions
{
    /// <summary>
    /// Adds the ability to be able to drag items within the list
    /// </summary>
    public class DragReOrderInteraction<T> : InteractionBase<T> 
        where T : class, INotifyPropertyChanged, IComparable, IItem
    {
        private static readonly int AutoScrollHitRegionSize = 80;

        private DispatcherTimer _autoScrollTimer;
        private DragImage _dragImage;
        private int _initialDragIndex;
        private SoundEffect _moveSound;

        public DragReOrderInteraction(DragImage dragImage)
        {
            _dragImage = dragImage;

            // a timer which is used to periodically detect the position of the
            // item being dragged in order to allow auto-scroll behaviour
            _autoScrollTimer = new DispatcherTimer();
            _autoScrollTimer.Interval = TimeSpan.FromMilliseconds(50);
            _autoScrollTimer.Tick += (s, e) =>
            {
                AutoScrollList();
                ShuffleItemsOnDrag();
            };

            //_moveSound = SoundEffect.FromStream(TitleContainer.OpenStream("Sounds/Windows XP Menu Command.wav"));
        }

        public override void AddElement(FrameworkElement rootElement)
        {
            rootElement.Hold += Element_Hold;
            rootElement.ManipulationDelta += Element_ManipulationDelta;
            rootElement.ManipulationCompleted += Element_ManipulationCompleted;
        }


        private void Element_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (!IsActive)
                return;

            IsActive = false;
            _autoScrollTimer.Stop();

            int dragIndex = GetDragIndex();

            // fade in the list
            this.interactionListControl.Animate(null, 1.0, FrameworkElement.OpacityProperty, 200, 0);

            // animated the dragged item into location
            double targetLocation = dragIndex * _dragImage.ActualHeight - _scrollViewer.VerticalOffset;
            var trans = _dragImage.GetVerticalOffset().Transform;
            trans.Animate(null, targetLocation, TranslateTransform.YProperty, 200, 0, null,
              () =>
              {
                  // move the dragged item
                  var draggedItem = this.interactionList[_initialDragIndex];
                  this.itemManager.SetItemOrder(draggedItem, dragIndex == 0 ? null : this.interactionList[dragIndex - 1], dragIndex < this.interactionList.Count - 1 ? this.interactionList[dragIndex] : null, true);
                  //this.interactionList.RemoveAt(_initialDragIndex);
                  //this.interactionList.Add(draggedItem);

                  // re-populate our ObservableCollection
                  RefreshView();

                  // fade out the dragged image and collapse on completion
                  _dragImage.Animate(null, 0.0, FrameworkElement.OpacityProperty, 1000, 0, null, ()
                    => _dragImage.Visibility = Visibility.Collapsed);
              });
        }

        private void Element_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            Debug.WriteLine("ManipulationDelta");

            if (!IsActive)
                return;

            // set the event to handled in order to avoid scrolling the ScrollViewer
            e.Handled = true;

            // move our 'drag image'.
            _dragImage.SetVerticalOffset(_dragImage.GetVerticalOffset().Value + e.DeltaManipulation.Translation.Y);
        }

        private void Element_Hold(object sender, GestureEventArgs e)
        {
            if (IsEnabled == false)
                return;

            FrameworkElement draggedItem = sender as FrameworkElement;
            T draggedDataContext = (T)draggedItem.DataContext;

            if (draggedDataContext.IsPullDown || draggedDataContext.IsCompleted)
            {
                return;
            }

            IsActive = true;

            // copy the dragged item to our 'dragImage' 
            var bitmap = new WriteableBitmap(draggedItem, null);
            _dragImage.Image.Source = bitmap;
            _dragImage.Visibility = Visibility.Visible;
            _dragImage.Opacity = 1.0;
            _dragImage.SetVerticalOffset(draggedItem.GetRelativePosition(this.interactionListControl).Y);

            // hide the real item
            draggedItem.Opacity = 0.0;

            // fade out the list
            this.interactionListControl.Animate(1.0, 0.7, FrameworkElement.OpacityProperty, 300, 0);

            _initialDragIndex = this.interactionList.IndexOf(((T)draggedItem.DataContext));

            _autoScrollTimer.Start();
        }

        // Determines the index that the dragged item would occupy when dropped
        private int GetDragIndex()
        {
            System.Windows.Point relativePosition = _dragImage.GetRelativePosition(this.interactionListControl);

            Debug.WriteLine("DragReOrderInteraction. RelativePosition {0}, scrollViewer Vertical offset {1}, dragImage height {2}",
                             relativePosition.Y,
                             _scrollViewer.VerticalOffset,
                             _dragImage.ActualHeight);
            double dragLocation = relativePosition.Y +
                                 _scrollViewer.VerticalOffset +
                                 _dragImage.ActualHeight / 2;
            Debug.WriteLine("Drag location {0}", dragLocation);

            int dragIndex = 0;
            double offset = 0;
            if (this.interactionList != null)
            {
                for (int i = 0; i < this.interactionList.Count; i++)
                {
                    var container = this.interactionListControl.ItemContainerGenerator.ContainerFromItem(this.interactionList[i]);
                    var taskPanel = FindNamedDescendant<StackPanel>(container, "TaskStackPanel");
                    offset += taskPanel.ActualHeight;
                    Debug.WriteLine("item {0}, height {1}, offset so far {2}", i, taskPanel.ActualHeight, offset);
                    dragIndex++;
                    if (offset > dragLocation)
                    {
                        break;
                    }
                }
            }
            
            //int dragIndex = (int)(dragLocation / _dragImage.ActualHeight);

            Debug.WriteLine("Drag index {0}", dragIndex);
            dragIndex = Math.Min(this.interactionList.Count - 1, dragIndex);

            if (dragIndex <= 0)
            {
                dragIndex = 1;
            }

            if (this.interactionList[dragIndex].IsPullDown)
            {
                dragIndex++;
            }

            return dragIndex;
        }

        private void ShuffleItemsOnDrag()
        {
            // find its current index
            int dragIndex = GetDragIndex();

            // iterate over the items in the list and offset as required
            double offset = _dragImage.ActualHeight;
            for (int i = 0; i < this.interactionList.Count; i++)
            {
                FrameworkElement item = this.interactionListControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;

                // determine which direction to offset this item by
                if (i <= dragIndex && i > _initialDragIndex)
                {
                    OffsetItem(-offset, item);
                }
                else if (i >= dragIndex && i < _initialDragIndex)
                {
                    OffsetItem(offset, item);
                }
                else
                {
                    OffsetItem(0, item);
                }
            }
        }

        private void OffsetItem(double offset, FrameworkElement item)
        {
            double targetLocation = item.Tag != null ? (double)item.Tag : 0;
            if (targetLocation != offset)
            {
                var trans = item.GetVerticalOffset().Transform;
                trans.Animate(null, offset, TranslateTransform.YProperty, 500, 0);
                item.Tag = offset;
                //_moveSound.Play();
            }
        }

        // checks the current location of the item being dragged, and scrolls if it is
        // close to the top or the bottom
        private void AutoScrollList()
        {
            // where is the dragged item relative to the list bounds?
            double draglocation = _dragImage.GetRelativePosition(this.interactionListControl).Y + _dragImage.ActualHeight / 2;

            if (draglocation < AutoScrollHitRegionSize)
            {
                // if close to the top, scroll up
                double velocity = (AutoScrollHitRegionSize - draglocation);
                _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - velocity);
            }
            else if (draglocation > this.interactionListControl.ActualHeight - AutoScrollHitRegionSize)
            {
                // if close to the bottom, scroll down
                double velocity = (AutoScrollHitRegionSize - (this.interactionListControl.ActualHeight - draglocation));
                _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + velocity);
            }
        }

        private T FindNamedDescendant<T>(DependencyObject element, string name)
        where T : FrameworkElement
        {
            return element.Descendants()
                          .OfType<T>()
                          .Where(i => i.Name == name)
                          .SingleOrDefault();
        }

    }
}