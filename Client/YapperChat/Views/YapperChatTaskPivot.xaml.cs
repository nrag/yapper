using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

using Microsoft.Xna.Framework.Audio;

using GalaSoft.MvvmLight.Messaging;
using YapperChat.Common;
using YapperChat.EventMessages;
using YapperChat.Models;
using YapperChat.PushNotification;
using YapperChat.ViewModels;
using YapperChat.Sync;
using Microsoft.Xna.Framework;
using YapperChat.Controls.Interactions;
using YapperChat.Resources;
using Windows.Phone.Speech.Recognition;
using YapperChat.Database;

namespace YapperChat.Views
{
    public partial class YapperChatTaskPivot : PhoneApplicationPage
    {
        private static Guid pullDownMessageGuid = new Guid("bbaf9b96-b875-4a23-97ac-77912ca74832");
        private InteractionManager<MessageModel> _interactionManager = new InteractionManager<MessageModel>();

        public static MessageModel pullMeDownMessage = new MessageModel() { TaskName = Strings.PullMeDownMessage, MessageId = pullDownMessageGuid, PostDateTime = DateTime.MaxValue, IsPullDown = true, IsCompleted = true, ItemOrder = "0" };

        private TaskMessageCreator<MessageModel> itemManager = new TaskMessageCreator<MessageModel>();

        private SwipeInteraction<MessageModel> swipeInteraction;

        private TapEditInteraction<MessageModel> tapEditInteraction;

        private SpeechRecognizerUI speechRecognizer;

        private bool isTextBeingProcessed;

        public YapperChatTaskPivot()
        {
            InitializeComponent();

            UserSettingsModel.Instance.LastTaskPageViewTime = DateTime.Now;
            Messenger.Default.Send<SuspendTaskCountEvent>(new SuspendTaskCountEvent() { Suspend = true});
            YapperChatViewModel viewModel = new YapperChatViewModel(false);
            this.DataContext = viewModel;
            PushNotification.PushNotification.Instance.Setup();
            this.TasksListSelector.ItemsSource = viewModel.Tasks.Tasks;

            this.swipeInteraction = new SwipeInteraction<MessageModel>();
            swipeInteraction.Initialize(this.TasksListSelector, itemManager, viewModel.Tasks.Tasks);

            this.tapEditInteraction = new TapEditInteraction<MessageModel>();
            tapEditInteraction.Initialize(this.TasksListSelector, itemManager, viewModel.Tasks.Tasks);
            tapEditInteraction.SaveEditText = this.SaveTaskName;

            _interactionManager.AddInteraction(swipeInteraction);
            FrameworkDispatcher.Update();

            BuildLocalizedApplicationBar();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            UserSettingsModel.Instance.LastTaskPageViewTime = DateTime.Now;
            _interactionManager.DisableInteractions();
            Messenger.Default.Send<SuspendTaskCountEvent>(new SuspendTaskCountEvent() { Suspend = false });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.TasksListSelector.Visibility = System.Windows.Visibility.Visible;
        }

        private void UpdateTaskButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            MessageModel task = (MessageModel)button.DataContext;

            DataSync.Instance.SendMessage(task);
            this.TasksListSelector.Visibility = System.Windows.Visibility.Visible;
        }

        private void TaskPivotView_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/YapperChatTaskPivot.xaml"), UriKind.Relative));
        }

        /// <summary>
        /// Handles the Application bar settings selection. The settings page displays
        /// the current user settings for the Yapper application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplicationBarSettings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/UserSettingsView.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Handles the Application bar settings selection. The settings page displays
        /// the current user settings for the Yapper application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>ApplicationBarSignout_Click
        private void ApplicationBarSignout_Click(object sender, EventArgs e)
        {
            UserSettingsModel.Instance.Clear();

            // Clear the backstack because back should not go back to the page that 
            // requires signing in
            while (NavigationService.BackStack.Any())
            {
                NavigationService.RemoveBackEntry();
            }

            // Go to the new user's registration page.
            NavigationService.Navigate(new Uri("/Views/NewUserRegistrationView.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Handles the Application bar settings selection. The settings page displays
        /// the current user settings for the Yapper application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>ApplicationBarSignout_Click
        private void ApplicationBarTutorial_Click(object sender, EventArgs e)
        {
            // Go to the new user's registration page.
            NavigationService.Navigate(new Uri("/Views/TutorialPageWelcome.xaml?page=task", UriKind.Relative));
        }

        private void ContactsPivotView_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/YapperChatContactsPivot.xaml"), UriKind.Relative));
        }

        private void CalendarPivotView_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/YapperChatCalendarPivot.xaml"), UriKind.Relative));
        }

        private void MessagePivotView_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/YapperChatMessagesPivot.xaml"), UriKind.Relative));
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.tapEditInteraction.IsActive)
            {
                this.tapEditInteraction.ClickButton = false;
                this.tapEditInteraction.LoseFocus();
                e.Cancel = true;
            }
            else
            {
                while (NavigationService.CanGoBack)
                {
                    NavigationService.RemoveBackEntry();
                }
            }
        }

        private void SaveTaskName(MessageModel task, string name, bool createNew)
        {
            if (!string.IsNullOrEmpty(name))
            {
                task.SetTaskName(name);
                DataSync.Instance.SetTaskName(task, name, Guid.Empty);
            }
            else
            {
                task.TaskName = null;
                this.itemManager.DeleteItem(task);

                task.ClientMessageId = Guid.Empty;
                task.MessageId = Guid.Empty;
                ((YapperChatViewModel)this.DataContext).Tasks.Tasks.Remove(task);
            }
        }

        private void Border_Loaded(object sender, RoutedEventArgs e)
        {
            swipeInteraction.IsEnabled = true;
            tapEditInteraction.IsEnabled = true;
            //addItemInteraction.IsEnabled = true; 
            _interactionManager.AddElement(sender as FrameworkElement);
        }

        private void TasksListSelector_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Border b = (Border)sender;
            MessageModel task = (MessageModel)b.DataContext;

            if (this.isTextBeingProcessed)
            {
                this.isTextBeingProcessed = false;
                return;
            }

            if (task != null && task.ClientMessageId != Guid.Empty)
            {
                NavigationService.Navigate(new Uri(string.Format("/Views/Tasklist.xaml?clientmessageid={0}", task.ClientMessageId), UriKind.Relative));
            }
        }

        private void taskButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            MessageModel task = (MessageModel)b.DataContext;

            if (task != null && task.ClientMessageId != Guid.Empty)
            {
                NavigationService.Navigate(new Uri(string.Format("/Views/Tasklist.xaml?clientmessageid={0}", task.ClientMessageId), UriKind.Relative));
            }
        }

        private void BuildLocalizedApplicationBar()
        {
            ApplicationBar appbar = (ApplicationBar)this.Resources["TaskPageApplicationBar"];

            ((ApplicationBarMenuItem)appbar.MenuItems[0]).Text = YapperChat.Resources.Strings.SignoutText;
            ((ApplicationBarMenuItem)appbar.MenuItems[1]).Text = YapperChat.Resources.Strings.SettingsText;
            ((ApplicationBarMenuItem)appbar.MenuItems[2]).Text = YapperChat.Resources.Strings.TutorialText;
            ((ApplicationBarIconButton)appbar.Buttons[0]).Text = YapperChat.Resources.Strings.NewTaskText;

            ApplicationBar = (ApplicationBar)this.Resources["TaskPageApplicationBar"];
        }

        private void AddNewTaskApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            var newItem = this.itemManager.CreateItem();
            this.itemManager.SetItemOrder(newItem, null, ((YapperChatViewModel)this.DataContext).Tasks.Tasks.Count > 2 ? ((YapperChatViewModel)this.DataContext).Tasks.Tasks[1] : null);
            ((YapperChatViewModel)this.DataContext).Tasks.Tasks.Add(newItem);
            this.TasksListSelector.InvokeOnNextLayoutUpdated(() => this.tapEditInteraction.EditItem(newItem));
        }
    }
}