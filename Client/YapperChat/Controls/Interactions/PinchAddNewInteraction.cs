using System;
using System.Net;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Audio;
using YapperChat.Common;
using System.ComponentModel;
using YapperChat.Models;

namespace YapperChat.Controls.Interactions
{
    public class PinchAddNewInteraction<T> : InteractionBase<T> 
        where T : class, INotifyPropertyChanged, IComparable, IItem
    {
        private static readonly double ToDoItemHeight = 100;

        private int _itemOneIndex;
        private int _itemTwoIndex;

        private Grid layoutRoot;

        private double _initialDelta;

        private double _newItemLocation;

        private bool _addNewThresholdReached;

        private TapEditInteraction<T> _editInteraction;
        private PullDownItem _pullDownItem;

        private SoundEffect _popSound;

        private bool _effectPlayed = false;

        public PinchAddNewInteraction(TapEditInteraction<T> editInteraction, PullDownItem pullDownItem)
        {
            _editInteraction = editInteraction;
            _pullDownItem = pullDownItem;

            //_popSound = SoundEffect.FromStream(Microsoft.Xna.Framework.TitleContainer.OpenStream("Sounds/pop.wav"));
        }

        public override void Initialize(ItemsControl todoList, IItemManager<T> itemManager, ObservableSortedList<T> todoItems)
        {
            base.Initialize(todoList, itemManager, todoItems);

            Touch.FrameReported += new TouchFrameEventHandler(Touch_FrameReported);
        }

        private void Touch_FrameReported(object sender, TouchFrameEventArgs e)
        {
            try
            {
                if (!IsEnabled)
                    return;

                if (IsActive)
                {
                    var touchPoints = e.GetTouchPoints(null);//this.interactionListControl);

                    Debug.WriteLine("Touch is active. Number of touchpoints {0}", touchPoints.Count);
                    // if we still have two touch points continue the pinch gesture
                    if (touchPoints.Count == 2)
                    {
                        double currentDelta = GetDelta(touchPoints[0], touchPoints[1]);
                        Debug.WriteLine("Current delta {0}. Initial Delta {1}", currentDelta, _initialDelta);

                        double itemsOffset = 0;

                        // is the delta bigger than the initial?
                        if (currentDelta > _initialDelta)
                        {
                            double delta = currentDelta - _initialDelta;
                            itemsOffset = delta / 2;

                            // play a sound effect if the users has pinched far enough to add a new item
                            if (delta > ToDoItemHeight && !_effectPlayed)
                            {
                                _effectPlayed = true;
                                //_popSound.Play();
                            }

                            Debug.WriteLine("Delta {0}", delta);
                            _addNewThresholdReached = delta > ToDoItemHeight;

                            // stretch and fade in the new item
                            var cappedDelta = Math.Min(ToDoItemHeight, delta);
                            ((ScaleTransform)_pullDownItem.RenderTransform).ScaleY = cappedDelta / ToDoItemHeight;
                            _pullDownItem.Opacity = cappedDelta / ToDoItemHeight;

                            // set the text
                            _pullDownItem.Text = cappedDelta < ToDoItemHeight ? "Pull to create new item" : "Release to add new item";
                        }

                        // offset all the items in the list so that they 'part'
                        Debug.WriteLine("Offsetting other items");
                        for (int i = 0; i < this.interactionList.Count; i++)
                        {
                            var container = this.interactionListControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                            var translateTransform = (TranslateTransform)container.RenderTransform;
                            translateTransform.Y = i <= _itemOneIndex ? -itemsOffset : itemsOffset;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("No longer two touch points");

                        // if we no longer have two touch points, end the interactions
                        IsActive = false;

                        RefreshView();

                        // hide the pull-down item
                        _pullDownItem.Visibility = Visibility.Collapsed;
                        _pullDownItem.VerticalOffset = -ToDoItemHeight - ToDoItemHeight;

                        if (_addNewThresholdReached)
                        {
                            var newItem = this.itemManager.CreateItem();
                            this.itemManager.SetItemOrder(newItem, this.interactionList[_itemOneIndex], this.interactionList[_itemTwoIndex]);
                            this.interactionList.Add(newItem);

                            // when the new item has been rendered, use the edit interaction to place the UI
                            // into edit mode
                            this.interactionListControl.InvokeOnNextLayoutUpdated(() => _editInteraction.EditItem(newItem));
                        }
                    }
                }
                else
                {
                    var touchPoints = e.GetTouchPoints(null);//this.interactionListControl);
                    Debug.WriteLine("Pinch gesture start. Number of touch points {0}", touchPoints.Count);
                    if (touchPoints.Count == 2)
                    {
                        _addNewThresholdReached = false;
                        _effectPlayed = false;

                        // find the items that were touched ...
                        var itemOne = GetToDoItemAtLocation(touchPoints[0].Position);
                        var itemTwo = GetToDoItemAtLocation(touchPoints[1].Position);

                        Debug.WriteLine("point 1 X: {0}, Y: {1}", touchPoints[0].Position.X, touchPoints[0].Position.Y);
                        Debug.WriteLine("point 2 X: {0}, Y: {1}", touchPoints[1].Position.X, touchPoints[1].Position.Y);

                        if (itemOne != null && itemTwo != null)
                        {
                            // find their indices
                            _itemOneIndex = this.interactionList.IndexOf(itemOne);
                            _itemTwoIndex = this.interactionList.IndexOf(itemTwo);

                            Debug.WriteLine("Pinch gesture item one position {0}, task name {1}", _itemOneIndex, (itemOne as MessageModel).TaskName);
                            Debug.WriteLine("Pinch gesture item two position {0} task name {1}", _itemTwoIndex, (itemTwo as MessageModel).TaskName);

                            // are the two items next to each other?
                            if (Math.Abs(_itemOneIndex - _itemTwoIndex) == 1)
                            {
                                if (_itemOneIndex > _itemTwoIndex)
                                {
                                    // We need to swap the two
                                    int tempIndex = _itemOneIndex;
                                    _itemOneIndex = _itemTwoIndex;
                                    _itemTwoIndex = tempIndex;

                                    var tempItem = itemOne;
                                    itemOne = itemTwo;
                                    itemTwo = tempItem;
                                }
                                IsActive = true;

                                // determine where to locate the new item placeholder
                                var itemOneContainer = this.interactionListControl.ItemContainerGenerator.ContainerFromItem(itemOne) as FrameworkElement;
                                var itemOneContainerPos = itemOneContainer.GetRelativePosition(this.interactionListControl);
                                _newItemLocation = itemOneContainerPos.Y + ToDoItemHeight - (ToDoItemHeight / 2);

                                // position the placeholder and add a scale transform
                                _pullDownItem.Visibility = Visibility.Visible;
                                _pullDownItem.VerticalOffset = _newItemLocation;
                                _pullDownItem.Opacity = 0;
                                _pullDownItem.RenderTransform = new ScaleTransform()
                                {
                                    ScaleY = 1,
                                    CenterY = ToDoItemHeight / 2
                                };

                                // record the initial distance between touch point
                                _initialDelta = GetDelta(touchPoints[0], touchPoints[1]);

                                AddTranslateTransfromToElements();

                                _pullDownItem.Opacity = 1;
                            }
                        }
                    }
                }
            }
            catch (ArgumentException)
            {
            }
        }

        private double GetDelta(TouchPoint tpOne, TouchPoint tpTwo)
        {
            double tpOneYPos = tpOne.Position.Y;
            double tpTwoYPos = tpTwo.Position.Y;

            return tpOneYPos > tpTwoYPos ? tpOneYPos - tpTwoYPos : tpTwoYPos - tpOneYPos;
        }

        private void AddTranslateTransfromToElements()
        {
            foreach (var item in this.interactionList)
            {
                var container = this.interactionListControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                container.RenderTransform = new TranslateTransform();
            }
        }

        private T GetToDoItemAtLocation(Point location)
        {
            var elements = VisualTreeHelper.FindElementsInHostCoordinates(location, this.interactionListControl);
            Border border = elements.OfType<Border>()
                                    .Where(i => i.Name == "task")
                                    .SingleOrDefault();

            return border != null ? border.DataContext as T : null;
        }
    }
}
