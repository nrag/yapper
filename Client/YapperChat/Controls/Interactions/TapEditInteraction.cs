using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using YapperChat.Common;
using System.ComponentModel;
using YapperChat.Sync;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

using WrapPanel = Microsoft.Phone.Controls.WrapPanel;

namespace YapperChat.Controls.Interactions
{
    /// <summary>
    /// An interaction that allows a user to edit an item by tapping on it.
    /// </summary>
    public class TapEditInteraction<T> : InteractionBase<T> 
        where T: class, INotifyPropertyChanged, IComparable, IItem
    {
        private Grid _taskGrid;
        private TextBlock _taskText;
        private TextBox _taskTextEdit;
        private StackPanel _taskPanel;
        private Button _taskButton;
        private WrapPanel infoPanel;

        private string _originalText;
        private T _editItem;

        public Action<T, string, bool> SaveEditText
        {
            get;
            set;
        }

        public bool ClickButton
        {
            get;
            set;
        }

        public override void Initialize(ItemsControl interactionListControl, IItemManager<T> itemManager, ObservableSortedList<T> interactionListItems)
        {
            base.Initialize(interactionListControl, itemManager, interactionListItems);
            this.interactionListControl.KeyDown += ItemsControl_KeyDown;
        }

        public override void AddElement(FrameworkElement element)
        {
            element.Tap += Element_Tap;
        }

        public void LoseFocus()
        {
            lock (this)
            {
                if (this.IsActive && this._taskTextEdit != null && FocusManager.GetFocusedElement() == this._taskTextEdit)
                {
                    this._taskTextEdit.Visibility = Visibility.Collapsed;
                    this._taskText.Visibility = Visibility.Visible;
                }
            }
        }

        private void Element_Tap(object sender, GestureEventArgs e)
        {
            if (!IsEnabled)
                return;

            // find the edit and static text controls
            var border = sender as Border;
            T dataContext = border.DataContext as T;
            if (dataContext != null & dataContext.IsCompleted)
            {
                return;
            }

            EditItem(dataContext);
        }

        public void EditItem(T editItem)
        {
            lock (this)
            {
                if (IsActive)
                {
                    return;
                }

                IsActive = true;
                this.ClickButton = true;

                _editItem = editItem;

                // find the edit and static text controls
                var container = this.interactionListControl.ItemContainerGenerator.ContainerFromItem(editItem);
                _taskTextEdit = FindNamedDescendant<TextBox>(container, "taskTextEdit");
                _taskText = FindNamedDescendant<TextBlock>(container, "taskText");
                _taskGrid = FindNamedDescendant<Grid>(container, "countgrid");
                _taskPanel = FindNamedDescendant<StackPanel>(container, "TaskStackPanel");
                _taskButton = FindNamedDescendant<Button>(container, "taskButton");
                infoPanel = FindNamedDescendant<WrapPanel>(container, "infoPanel");

                // store the original text to allow undo
                if (_taskTextEdit != null)
                {
                    _originalText = _taskTextEdit.Text;
                }

                EditFieldVisible(true);

                // set the caret position to the end of the text field
                if (_taskTextEdit != null)
                {
                    _taskTextEdit.Focus();
                    _taskTextEdit.Select(_originalText.Length, 0);
                    _taskTextEdit.LostFocus += TaskTextEdit_LostFocus;

                    // Scroll up to bring the edit box in view
                    double offset = _taskTextEdit.GetRelativePosition(this.interactionListControl).Y + this.interactionListControl.GetVerticalOffset().Value;
                    this._scrollViewer.ScrollToVerticalOffset(this._scrollViewer.VerticalOffset + offset);
                }

                // fade out all other items
                ((FrameworkElement)this.interactionListControl.ItemContainerGenerator.ContainerFromItem(_editItem)).Opacity = 1;
                var elements = this.interactionList.Where(i => i != _editItem)
                                            .Select(i => this.interactionListControl.ItemContainerGenerator.ContainerFromItem(i))
                                            .Cast<FrameworkElement>();
                foreach (var el in elements)
                {
                    el.Animate(1.0, 0.4, FrameworkElement.OpacityProperty, 800, 0);
                }
            }
        }

        private void EditFieldVisible(bool visible)
        {
            if (_taskTextEdit != null)
            {
                _taskTextEdit.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_taskText != null)
            {
                _taskText.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
            }
            
            if (_taskGrid != null)
            {
                _taskGrid.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
            }

            if (infoPanel != null)
            {
                infoPanel.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
            }

            if (visible == false)
            {
                var elements = this.interactionList.Select(i => this.interactionListControl.ItemContainerGenerator.ContainerFromItem(i))
                                       .Cast<FrameworkElement>();
                foreach (var el in elements)
                {
                    el.Animate(null, 1.0, FrameworkElement.OpacityProperty, 800, 0);
                }
            }
        }

        private void ItemsControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this._taskTextEdit.Text.TrimEnd();
                EndEdit(true);
            }
        }

        private void ItemsControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this._taskTextEdit.Text.TrimEnd();
            }
        }

        public void EndEdit(bool enterPressed = false)
        {
            lock (this)
            {
                _taskTextEdit.LostFocus -= TaskTextEdit_LostFocus;

                T task = this._taskPanel.DataContext as T;
                this.SaveEditText(task, this._taskTextEdit.Text, enterPressed);

                if (this._taskButton != null && enterPressed && this.ClickButton)
                {
                    ButtonAutomationPeer peer = new ButtonAutomationPeer(this._taskButton);

                    IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                    invokeProv.Invoke();
                }

                EditFieldVisible(false);
                IsActive = false;
            }
        }

        private void TaskTextEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            EndEdit();
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