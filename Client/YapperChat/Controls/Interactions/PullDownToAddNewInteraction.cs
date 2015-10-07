using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YapperChat.Common;
using Microsoft.Xna.Framework.Audio;
using System.ComponentModel;
using YapperChat.Views;
using YapperChat.Resources;
using YapperChat.Sync;

namespace YapperChat.Controls.Interactions
{
    /// <summary>
    /// An interaction that allows the user to add new items by dragging them down from
    /// the top of the screen;
    /// </summary>
    public class PullDownToAddNewInteraction<T> : InteractionBase<T> 
        where T : class, INotifyPropertyChanged, IComparable, IItem
    {
        private static readonly double ToDoItemHeight = 100;
        private static readonly double ToDoItemCollapsedHeight = 110;

        private TapEditInteraction<T> _editInteraction;
        private PullDownItem _pullDownItem;
        private double _distance = 0;
        private long startTime = 0;

        private bool _effectPlayed = false;
        private SoundEffect _popSound;

        public PullDownToAddNewInteraction(TapEditInteraction<T> editInteraction, PullDownItem pullDownItem)
        {
            _editInteraction = editInteraction;
            _pullDownItem = pullDownItem;

            //_popSound = SoundEffect.FromStream(Microsoft.Xna.Framework.TitleContainer.OpenStream("Sounds/pop.wav"));
        }

        protected override void ScrollViewerLocated(ScrollViewer scrollViewer)
        {
            scrollViewer.MouseMove += ScrollViewer_MouseMove;
            scrollViewer.MouseLeftButtonUp += ScrollViewer_MouseLeftButtonUp;
        }

        private void ScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("scrollViewer_MouseMove");
            if (!IsEnabled)
                return;

            // determine whether the user is pulling the list down by inspecting the ScrollViewer.Content abd
            // looking for the required transform.
            UIElement scrollContent = (UIElement)_scrollViewer.Content;
            CompositeTransform ct = scrollContent.RenderTransform as CompositeTransform;
            if (ct != null && ct.TranslateY > 0)
            {
                //_pullDownItem.Visibility = Visibility.Visible;
                IsActive = true;

                // offset the pull-down element, set its text and opacity
                _distance = ct.TranslateY;
                YapperChatTaskPivot.pullMeDownMessage.TaskName = Strings.ReleaseToCreateNewItem;

                if (_distance > ToDoItemHeight && !_effectPlayed)
                {
                    _effectPlayed = true;
                    //_popSound.Play();
                }

                //_pullDownItem.Text = _distance > ToDoItemHeight ? "Release to create new item" : "Pull to create new item";

                //_pullDownItem.Opacity = Math.Min(1.0, _distance / ToDoItemHeight);
            }
        }

        private void ScrollViewer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("scrollViewer_MouseLeftButtonUp");

            if (!IsActive)
                return;

            YapperChatTaskPivot.pullMeDownMessage.TaskName = Strings.PullMeDownMessage;

            // if the list was pulled down far enough, add a new item
            if (_distance > ToDoItemCollapsedHeight)
            {
                var newItem = this.itemManager.CreateItem();
                this.itemManager.SetItemOrder(newItem, null, this.interactionList.Count > 2 ? this.interactionList[1] : null);
                this.interactionList.Add(newItem);

                // when the new item has been rendered, use the edit interaction to place the UI
                // into edit mode
                this.interactionListControl.InvokeOnNextLayoutUpdated(() => _editInteraction.EditItem(newItem));
            }

            IsActive = false;
            _effectPlayed = false;
        }
    }
}