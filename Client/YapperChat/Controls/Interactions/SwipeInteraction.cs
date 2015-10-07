using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Windows.Input;
using System.ComponentModel;

using YapperChat.Common;
using System.Windows.Controls;

namespace YapperChat.Controls.Interactions
{
    /// <summary>
    /// Adds an interaction that allows the user to swipe an item to mark it as complete
    /// or delete it
    /// </summary>
    public class SwipeInteraction<T> : InteractionBase<T>, IInteraction<T> 
        where T : class, INotifyPropertyChanged, IComparable, IItem
    {
        private static readonly double FlickVelocity = 2000.0;

        // the drag distance required to consider this a swipe interaction
        private static readonly double DragStartedDistance = 5.0;

        private FrameworkElement _tickAndCrossContainer;
        private SoundEffect _completeSound;
        private SoundEffect _deleteSound;

        public SwipeInteraction()
        {
            //_completeSound = SoundEffect.FromStream(TitleContainer.OpenStream("Sounds/Windows XP Exclamation.wav"));
            //_deleteSound = SoundEffect.FromStream(TitleContainer.OpenStream("Sounds/Windows XP Notify.wav"));
        }

        public override void AddElement(FrameworkElement element)
        {
            T manipulatedItem = element.DataContext as T;
            if (!manipulatedItem.IsPullDown)
            {
                element.ManipulationDelta += Element_ManipulationDelta;
                element.ManipulationCompleted += Element_ManipulationCompleted;
            }
        }

        private void Element_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (!IsActive)
                return;

            FrameworkElement fe = sender as FrameworkElement;
            if (Math.Abs(e.TotalManipulation.Translation.X) > fe.ActualWidth / 2 ||
              Math.Abs(e.FinalVelocities.LinearVelocity.X) > FlickVelocity)
            {
                if (e.TotalManipulation.Translation.X < 0.0)
                {
                    ToDoItemDeletedAction(fe);
                }
                else
                {
                    ToDoItemCompletedAction(fe);
                }
            }
            else
            {
                ToDoItemBounceBack(fe, null);
            }

            IsActive = false;
        }

        private void Element_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (!IsEnabled)
                return;

            if (!IsActive)
            {
                // has the user dragged far enough?
                if (Math.Abs(e.CumulativeManipulation.Translation.X) < DragStartedDistance)
                    return;

                IsActive = true;

                // initialize the drag
                FrameworkElement fe = sender as FrameworkElement;
                fe.SetHorizontalOffset(0);

                // find the container for the tick and cross graphics
                _tickAndCrossContainer = fe.Descendants()
                                           .OfType<FrameworkElement>()
                                           .Single(i => i.Name == "tickAndCross");
            }
            else
            {
                // handle the drag to offset the element
                FrameworkElement fe = sender as FrameworkElement;
                double offset = fe.GetHorizontalOffset().Value + e.DeltaManipulation.Translation.X;
                fe.SetHorizontalOffset(offset);

                _tickAndCrossContainer.Opacity = TickAndCrossOpacity(offset);
            }
        }


        private double TickAndCrossOpacity(double offset)
        {
            offset = Math.Abs(offset);
            if (offset < 50)
                return 0;

            offset -= 50;
            double opacity = offset / 100;

            opacity = Math.Max(Math.Min(opacity, 1), 0);
            return opacity;
        }

        private void ToDoItemBounceBack(FrameworkElement fe, Action action)
        {
            var trans = fe.GetHorizontalOffset().Transform;

            trans.Animate(trans.X, 0, TranslateTransform.XProperty, 300, 0, new BounceEase()
            {
                Bounciness = 5,
                Bounces = 2
            },
            action);
        }

        private void ToDoItemDeletedAction(FrameworkElement deletedElement)
        {
            //_deleteSound.Play();

            var trans = deletedElement.GetHorizontalOffset().Transform;
            trans.Animate(trans.X, -(deletedElement.ActualWidth + 50),
                          TranslateTransform.XProperty, 300, 0, new SineEase()
                          {
                              EasingMode = EasingMode.EaseOut
                          },
            () =>
            {
                // find the model object that was deleted
                T deletedItem = deletedElement.DataContext as T;

                // determine how much we have to 'shuffle' up by
                double elementOffset = -deletedElement.ActualHeight;

                // find the items in view, and the location of the deleted item in this list
                var itemsInView = this.interactionListControl.GetItemsInView().ToList();
                var lastItem = itemsInView.Last();
                int startTime = 0;
                int deletedItemIndex = itemsInView.Select(i => i.DataContext)
                                                  .ToList().IndexOf(deletedItem);

                // iterate over each item
                foreach (FrameworkElement element in itemsInView.Skip(deletedItemIndex))
                {
                    // for the last item, create an action that deletes the model object
                    // and re-renders the list
                    Action action = null;
                    if (element == lastItem)
                    {
                        action = () =>
                        {
                            // remove the item
                            this.interactionList.Remove(deletedItem);

                            this.itemManager.DeleteItem(deletedItem);

                            // re-populate our ObservableCollection
                            this.interactionList.Reset();
                        };
                    }

                    // shuffle this item up
                    TranslateTransform elementTrans = new TranslateTransform();
                    element.RenderTransform = elementTrans;
                    elementTrans.Animate(0, elementOffset, TranslateTransform.YProperty, 200, startTime, null, action);
                    startTime += 10;
                }
            });
        }

        private void ToDoItemCompletedAction(FrameworkElement fe)
        {
            var trans = fe.GetVerticalOffset().Transform;
            double current = fe.GetVerticalOffset().Value;

            // set the mode object to complete
            T completedItem = fe.DataContext as T;

            int initialIndex = this.interactionList.IndexOf(completedItem);
            T clone = completedItem.Clone() as T;
            clone.IsCompleted = true;
            int laterIndex = this.interactionList.FindIndex(clone);

            ToDoItemBounceBack(fe, null);

            double offset = 0;
            for (int i = initialIndex + 1; i < laterIndex; i++)
            {
                var container = this.interactionListControl.ItemContainerGenerator.ContainerFromItem(this.interactionList[i]) as FrameworkElement;
                var transform = container.GetVerticalOffset().Transform;
                transform.Animate(null, -fe.ActualHeight, TranslateTransform.YProperty, 500, 0);
                offset += container.ActualHeight;
            }

            trans.Animate(null, offset, TranslateTransform.YProperty, 1000, 0, null, () => { this.itemManager.CompleteItem(completedItem); });

            
            //_completeSound.Play();
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