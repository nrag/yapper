using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using YapperChat.Controls.Interactions;
using YapperChat.EventMessages;
using YapperChat.Models;
using YapperChat.Resources;
using YapperChat.Sync;
using YapperChat.Common;
using YapperChat.ViewModels;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using Windows.Phone.Speech.Recognition;

namespace YapperChat.Views
{
    public partial class Tasklist : PhoneApplicationPage
    {
        private static Guid pullDownMessageGuid = new Guid("bbaf9b96-b875-4a23-97ac-77912ca74832");
        private InteractionManager<MessageModel> _interactionManager = new InteractionManager<MessageModel>();

        public static MessageModel pullMeDownMessage = new MessageModel() { TaskName = Strings.PullMeDownTaskItemMessage, MessageId = pullDownMessageGuid, PostDateTime = DateTime.MaxValue, IsPullDown = true, IsCompleted = false, ItemOrder = "0" };

        private DragReOrderInteraction<MessageModel> dragReOrderInteraction;
        private SwipeInteraction<MessageModel> swipeInteraction;
        private TapEditInteraction<MessageModel> tapEditInteraction;
        //private PullDownToAddNewInteraction<MessageModel> addItemInteraction;
        private PinchAddNewInteraction<MessageModel> pinchAddNewItemInteraction;

        private MessageModel message;

        private TaskListMessageCreator<MessageModel> itemManager = new TaskListMessageCreator<MessageModel>();
        public Tasklist()
        {
            InitializeComponent();
            BuildLocalizedApplicationBar();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (this.DataContext == null)
            {
                Guid messageId;

                messageId = Guid.Parse(NavigationContext.QueryString["clientmessageid"]);

                message = DataSync.Instance.GetMessageFromClientId(messageId);

                if (message != null)
                {
                    Messenger.Default.Register<NewMessageEvent>(this, this.MessageSavedEvent);
                    this.DataContext = message;

                    message.LoadTaskList();
                    if (message.TaskItemList != null)
                    {
                        message.TaskItemList = new ObservableSortedList<MessageModel>(message.TaskItemList, new TaskListComparer<MessageModel>());
                    }
                    else
                    {
                        message.TaskItemList = new ObservableSortedList<MessageModel>(4, new TaskListComparer<MessageModel>());
                    }

                    this.TasksListSelector.ItemsSource = message.TaskItemList;

                    dragReOrderInteraction = new DragReOrderInteraction<MessageModel>(dragImageControl);
                    dragReOrderInteraction.Initialize(this.TasksListSelector, itemManager, message.TaskItemList);

                    swipeInteraction = new SwipeInteraction<MessageModel>();
                    swipeInteraction.Initialize(this.TasksListSelector, itemManager, message.TaskItemList);

                    tapEditInteraction = new TapEditInteraction<MessageModel>();
                    tapEditInteraction.Initialize(this.TasksListSelector, itemManager, message.TaskItemList);
                    tapEditInteraction.SaveEditText = this.SaveTaskItemName;

                    //addItemInteraction = new PullDownToAddNewInteraction<MessageModel>(tapEditInteraction, pullDownItemInFront);
                    //addItemInteraction.Initialize(this.TasksListSelector, itemManager, message.TaskItemList);

                    pinchAddNewItemInteraction = new PinchAddNewInteraction<MessageModel>(tapEditInteraction, pullDownItemBehind);
                    pinchAddNewItemInteraction.Initialize(this.TasksListSelector, itemManager, message.TaskItemList);

                    _interactionManager.AddInteraction(swipeInteraction);
                    _interactionManager.AddInteraction(dragReOrderInteraction);
                    //_interactionManager.AddInteraction(addItemInteraction);
                    _interactionManager.AddInteraction(tapEditInteraction);
                    _interactionManager.AddInteraction(pinchAddNewItemInteraction);


                    if (this.message.TaskItemCount == 0)
                    {
                        MessageModel newItem = itemManager.CreateTaskMessage();
                        newItem.ItemOrder = itemManager.GetItemOrder(null, null);
                        this.message.TaskItemList.Add(newItem);
                        this.TasksListSelector.InvokeOnNextLayoutUpdated(() => this.tapEditInteraction.EditItem(newItem));
                    }

                    FrameworkDispatcher.Update();
                }
            }
            
            this._interactionManager.EnableInteractions();

            if (DataContext != null)
            {
                if (message.TaskItemList != null &&
                    (message.TaskItemList.Count > 0 && message.TaskItemList[0].CompareTo(pullMeDownMessage) != 0) ||
                    message.TaskItemList.Count == 0)
                {
                    //message.TaskItemList.Add(pullMeDownMessage);
                }

                this.ApplicationBar = (ApplicationBar)this.Resources["ShareTaskApplicationBar"];
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (this._interactionManager != null)
            {
                this._interactionManager.DisableInteractions();
            }

            if (this.message != null && 
                this.message.IsTaskShared && 
                (this.itemManager.HasChanged || this.itemManager.IsDeleted))
            {
                this.SendMessage();
            }

            MessageModel m = this.DataContext as MessageModel;
            if (m != null)
            {
                DataSync.Instance.SetLastReadTime(message);
                
                Messenger.Default.Send<RefreshTaskMessage>(new RefreshTaskMessage() { TaskId = m.ClientMessageId });
                m = null;
                this.TasksListSelector.ItemsSource = null;
            }
        }

        private void Border_Loaded(object sender, RoutedEventArgs e)
        {
            if (this._interactionManager != null)
            {
                this._interactionManager.AddElement(sender as FrameworkElement);
            }
        }


        private void Border_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void SaveTaskItemName(MessageModel task, string name, bool createNew)
        {
            if (this.IsMultiLineTask(name))
            {
                string[] multilinetask = name.Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < multilinetask.Length; i++)
                {
                    multilinetask[i] = multilinetask[i].Trim();
                    if (string.IsNullOrWhiteSpace(multilinetask[i]))
                    {
                        continue;
                    }

                    MessageModel newItem = task;

                    if (multilinetask[i].Length > 50)
                    {
                        multilinetask[i] = multilinetask[i].Substring(0, 50);
                    }

                    if (i != 0)
                    {
                        newItem = itemManager.CreateTaskMessage();
                        newItem.ItemOrder = itemManager.GetItemOrder(this.message.TaskItemList[i - 1], null);
                        this.message.TaskItemList.Add(newItem);
                    }

                    newItem.SetTaskName(multilinetask[i]);
                    DataSync.Instance.SetTaskName(newItem, multilinetask[i], this.message.ClientMessageId);
                }

                return;
            }

            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();
                if (name.Length > 50)
                {
                    name = name.Substring(0, 50);
                }

                if (0 != StringComparer.OrdinalIgnoreCase.Compare(task.TaskName, name))
                {
                    this.itemManager.HasChanged = true;
                    task.SetTaskName(name);
                    if (string.IsNullOrEmpty(task.TaskName))
                    {
                        task.LastTaskUpdaterId = UserSettingsModel.Instance.Me.Id;
                    }
                    
                    MessageModel m = this.DataContext as MessageModel;
                    DataSync.Instance.SetTaskName(task, name, m.ClientMessageId);

                    if (string.IsNullOrEmpty(this.message.TaskName))
                    {
                        this.message.TaskName = name;
                        DataSync.Instance.SetTaskName(this.message, name, Guid.Empty);
                    }
                }

                int index = this.message.TaskItemList.IndexOf(task);
                if (createNew && (index == this.message.TaskItemList.Count - 1 || this.message.TaskItemList[index + 1].IsCompleted))
                {
                    MessageModel newItem = itemManager.CreateTaskMessage();
                    newItem.ItemOrder = itemManager.GetItemOrder(this.message.TaskItemList[index], null);
                    this.message.TaskItemList.Add(newItem);
                    this.TasksListSelector.InvokeOnNextLayoutUpdated(() => this.tapEditInteraction.EditItem(newItem));
                }
            }
            else if (string.IsNullOrEmpty(name))
            {
                int index = this.message.TaskItemList.IndexOf(task);
                if (index != this.message.TaskItemList.Count - 1 && !this.message.TaskItemList[index + 1].IsCompleted)
                {
                    this.itemManager.DeleteItem(task);
                }

                this.message.TaskItemList.Remove(task);
            }
        }

        private bool IsMultiLineTask(string name)
        {
            string[] multilinetask = name.Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);

            if (multilinetask.Length > 1)
            {
                return true;
            }

            return false;
        }

        private void SendMessage()
        {
            // Send messagesage
            MessageModel clone = ((MessageModel)this.DataContext).Clone() as MessageModel;
            if (clone.TaskItemList != null)
            {
                clone.TaskItemList = new ObservableSortedList<MessageModel>(((MessageModel)this.DataContext).TaskItemList);
                for (int i = clone.TaskItemList.Count - 1; i > 0; i--)
                {
                    if (string.IsNullOrEmpty(clone.TaskItemList[i].TaskName))
                    {
                        clone.TaskItemList.RemoveAt(i);
                    }
                }

                clone.TaskItemList.Remove(pullMeDownMessage);
            }

            if (this.itemManager.IsDeleted)
            {
                foreach (MessageModel m in this.itemManager.DeletedItems)
                {
                    m.IsTaskDeleted = true;
                    clone.TaskItemList.Add(m);
                }
            }

            if (clone.RecipientId == UserSettingsModel.Instance.Me.Id)
            {
                clone.Recipient = ((MessageModel)this.DataContext).Sender;
                clone.RecipientId = clone.Recipient.Id;
                clone.Sender = UserSettingsModel.Instance.Me;
                clone.SenderId = clone.Sender.Id;
            }

            DataSync.Instance.SendMessage(clone);
        }

        private void ShareTask_Click(object sender, EventArgs e)
        {
            if (((MessageModel)this.DataContext).Recipient != null)
            {
                MessageBox.Show(string.Format(Strings.ListAlreadyShared, ((MessageModel)this.DataContext).Recipient.Name));
                return;
            }

            this.ShareTaskPopup.IsOpen = true;
            this.ShareTaskPopup.Visibility = Visibility.Visible;
            this.ContactsListSelector.ItemsSource = (ViewModelLocator.Instance.CreateOrGetViewModel<RegisteredUsersViewModel>()).RegisteredUsers;
        }

        private void ContactsListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ContactsListSelector.SelectedItem != null)
            {
                foreach (var button in ApplicationBar.Buttons)
                {
                    ((ApplicationBarIconButton)button).IsEnabled = false; // disables the button
                }

                ((MessageModel)this.DataContext).Recipient = (UserModel)this.ContactsListSelector.SelectedItem;
                ((MessageModel)this.DataContext).RecipientId = ((MessageModel)this.DataContext).Recipient.Id;

                this.SendMessage();
                this.ProgressIndicator.IsVisible = true;
                Messenger.Default.Register<NewMessageEvent>(this, this.MessageSavedEvent);
            }
        }

        private void MessageSavedEvent(NewMessageEvent obj)
        {
            DispatcherHelper.InvokeOnUiThread(() =>
                {
                    if (obj.Message != null && obj.Message.ClientMessageId == ((MessageModel)this.DataContext).ClientMessageId)
                    {
                        this.ShareTaskPopup.Visibility = Visibility.Collapsed;
                        this.ShareTaskPopup.IsOpen = false;
                        this.ProgressIndicator.IsVisible = false;

                        ((MessageModel)this.DataContext).NotifyPropertyChanged("IsTaskShared");
                        ((MessageModel)this.DataContext).NotifyPropertyChanged("TaskSharedWithString");

                        foreach (var button in ApplicationBar.Buttons)
                        {
                            ((ApplicationBarIconButton)button).IsEnabled = true; // disables the button
                        }

                        ((MessageModel)this.DataContext).TaskItemList = null;
                        ((MessageModel)this.DataContext).LoadTaskList();

                        this.TasksListSelector.ItemsSource = this.message.TaskItemList;

                        if (message.TaskItemList != null &&
                            (message.TaskItemList.Count > 0 && message.TaskItemList[0].CompareTo(pullMeDownMessage) != 0) ||
                            message.TaskItemList.Count == 0)
                        {
                            //message.TaskItemList.Add(pullMeDownMessage);
                        }
                    }

                    if (obj.Message != null)
                    {
                        Messenger.Default.Send<RefreshTaskMessage>(new RefreshTaskMessage() { TaskId = obj.Message.ClientMessageId });
                    }
                });
        }

        private void BuildLocalizedApplicationBar()
        {
            ApplicationBar appbar = (ApplicationBar)this.Resources["ShareTaskApplicationBar"];
            ((ApplicationBarIconButton)appbar.Buttons[0]).Text = YapperChat.Resources.Strings.NewTaskItemText;
            ((ApplicationBarIconButton)appbar.Buttons[1]).Text = YapperChat.Resources.Strings.ShareTaskText;
        }

        private int FindLastBeforeCompleted()
        {
            for (int i = 0; i < message.TaskItemList.Count; i++)
            {
                if (i < message.TaskItemList.Count - 1 && message.TaskItemList[i + 1].IsCompleted)
                {
                    return i;
                }
            }

            return message.TaskItemList.Count - 1;
        }

        private void AddNewTaskItemApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            var newItem = this.itemManager.CreateItem();
            this.itemManager.SetItemOrder(newItem, message.TaskItemList.Count > 1 ? message.TaskItemList[FindLastBeforeCompleted()] : null, null);
            message.TaskItemList.Add(newItem);
            this.TasksListSelector.InvokeOnNextLayoutUpdated(() => this.tapEditInteraction.EditItem(newItem));
        }

        private int FindIndex(MessageModel m)
        {
            for (int i = 0; i < message.TaskItemList.Count; i++)
            {
                if (m.ClientMessageId == message.TaskItemList[i].ClientMessageId)
                {
                    return i + 1;
                }
            }

            return message.TaskItemList.Count + 1;
        }

        private void IndexForGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (((MessageModel)((TextBlock)sender).DataContext).IsCompleted == false)
            {
                ((TextBlock)sender).Text = Convert.ToString(this.FindIndex((MessageModel)((TextBlock)sender).DataContext), CultureInfo.CurrentUICulture) + ".";
            }
        }

        private void TasksListSelector_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
        }
    }
}