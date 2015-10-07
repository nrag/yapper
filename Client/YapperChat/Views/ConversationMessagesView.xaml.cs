using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using YapperChat.ViewModels;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.EventMessages;
using System.Collections.Specialized;
using System.Windows.Data;
using Microsoft.Xna.Framework;
using Microsoft.Phone.Tasks;
using System.Text;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Phone;
using System.Windows.Resources;
using System.IO.IsolatedStorage;
using YapperChat.Models;
using YapperChat.Sync;
using Microsoft.Phone.Shell;
using Windows.Devices.Geolocation;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Windows.Navigation;

using Strings = YapperChat.Resources.Strings;
using YapperChat.Common;
using System.Diagnostics;
using YapperChat.Controls.Converters;
using Coding4Fun.Toolkit.Controls;
using Windows.Phone.Speech.Recognition;

namespace YapperChat.Views
{
    public partial class ConversationMessagesView : PhoneApplicationPage
    {
        private byte[] imageBlob = null;
        string selectedOption = null;
        ApplicationBarIconButton attachButton = new ApplicationBarIconButton();
        ApplicationBarIconButton keyboardButton = new ApplicationBarIconButton();

        public ConversationMessagesView()
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "CoversationMessagesView::ConversationMessagesView", "Start"));
            InitializeComponent();
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "CoversationMessagesView::ConversationMessagesView", "End"));

            PushNotification.PushNotification.Instance.Setup();
            Messenger.Default.Register<QuitApplicationEvent>(this, this.HandleQuitApplicationEvent);
            attachButton.IconUri = new Uri("/Images/appbar.paperclip.png", UriKind.Relative);
            attachButton.Text = "Attachments";
            attachButton.Click += new EventHandler(ToggleAttachment_Click);

            keyboardButton.IconUri = new Uri("/Images/appbar.input.keyboard.png", UriKind.Relative);
            keyboardButton.Text = "Keyboard";
            keyboardButton.Click += new EventHandler(ToggleKeyboard_Click);

            //this.Cal.VerticalContentAlignment = VerticalAlignment.Stretch;
            //this.Cal.HorizontalContentAlignment = HorizontalAlignment.Stretch;

            Messenger.Default.Register<ScrollToEvent>(this, this.HandleScrollToBottomEvent);
            ((ApplicationBar)this.Resources["MessagesAppBar"]).Buttons.Add(attachButton);

            this.ApplicationBar = (ApplicationBar)this.Resources["MessagesAppBar"];
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "CoversationMessagesView::ConversationView", "Initialized"));

            this.ApptDurationPicker.Value = new TimeSpan(1, 0, 0);
            DateTime time = DateTime.Now;
            if (time.Minute >= 30)
            {
                this.ApptTimePicker.Value = new DateTime(time.Year, time.Month, time.Day, time.Hour,0, 0).AddHours(1);
            }
            else
            {
                this.ApptTimePicker.Value = new DateTime(time.Year, time.Month, time.Day, time.Hour,0, 0).AddMinutes(30);
            }

            BuildLocalizedApplicationBar();
        }

        async protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (this.DataContext == null)
            {
                Guid conversationId;
                string senderName = null;

                base.OnNavigatedTo(e);
                conversationId = Guid.Parse(NavigationContext.QueryString["conversationId"]);

                if (NavigationContext.QueryString.ContainsKey("recipientName"))
                {
                    senderName = NavigationContext.QueryString["recipientName"];
                }

                Guid messageId = Guid.Empty;
                if (NavigationContext.QueryString.ContainsKey("messageId"))
                {
                    messageId = Guid.Parse(NavigationContext.QueryString["messageId"]);
                }

                int recipient = -1;
                if (NavigationContext.QueryString.ContainsKey("recipientId"))
                {
                    recipient = Int32.Parse(NavigationContext.QueryString["recipientId"]);
                }

                bool? isGroup = null;
                if (NavigationContext.QueryString.ContainsKey("isGroup"))
                {
                    isGroup = Boolean.Parse(NavigationContext.QueryString["isGroup"]);
                }

                int pivot = 0;
                if (NavigationContext.QueryString.ContainsKey("pivot"))
                {
                    pivot = Int32.Parse(NavigationContext.QueryString["pivot"]);
                }

                if (messageId != Guid.Empty)
                {
                    //DataSync.Instance.SyncMessageId(conversationId, messageId);
                }

                // Before the messages are loaded, ensure that the scroll handler is set correctly.
                var scrollToEndHandler = new System.Collections.Specialized.NotifyCollectionChangedEventHandler(
                (s1, e1) =>
                {
                    if (this.messagesListBox.ItemsSource.Count > 0)
                    {
                        this.messagesListBox.UpdateLayout();
                        object lastItem = this.messagesListBox.ItemsSource[this.messagesListBox.ItemsSource.Count - 1];
                        this.messagesListBox.ScrollTo(lastItem);
                    }
                });


                //INotifyCollectionChanged listItems = this.messagesListBox.ItemsSource as INotifyCollectionChanged;
                //listItems.CollectionChanged += scrollToEndHandler;

                UserModel recipientUser = DataSync.Instance.GetUser(recipient);
                if (recipientUser == null)
                {
                    UserType userType = isGroup.HasValue && isGroup.Value ? UserType.Group : UserType.User;
                    recipientUser = new Models.UserModel() { Id = recipient, Name = senderName,  UserType = userType};
                }

                // Create the viewmodel and load the messages
                ConversationMessagesViewModel cvm = new ConversationMessagesViewModel(conversationId, recipientUser, isGroup);
                this.DataContext = cvm;
                cvm.LoadMessagesForConversations();

                if (cvm.IsGroup && cvm.Recipient != null && !cvm.Recipient.IsGroupMember)
                {
                    MessageBox.Show(Strings.NotAGroupMember);
                }

                // Bind the list items to a collection view source
                // This helps in sorting the messages based on post time
                // without having to maintain the list in the view model in a sorted order
                CollectionViewSource messageCollection = new CollectionViewSource();
                messageCollection.Source = ((ConversationMessagesViewModel)this.DataContext).Messages;
                System.ComponentModel.SortDescription sort = new System.ComponentModel.SortDescription("PostDateTime", System.ComponentModel.ListSortDirection.Ascending);
                messageCollection.SortDescriptions.Add(sort);
                messagesListBox.ItemsSource = ((ConversationMessagesViewModel)this.DataContext).Messages;
                this.ConversationPagePivot.SelectedIndex = pivot;

                (Application.Current as App).watcher.PositionChanged += watcher_PositionChanged;
                (Application.Current as App).watcher.StatusChanged += watcher_StatusChanged;
                (Application.Current as App).Currentlocation = await (Application.Current as App).watcher.GetGeopositionAsync();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ((ConversationMessagesViewModel)this.DataContext).IsCurrentyViewing = false;
        }

        async void watcher_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            (Application.Current as App).Currentlocation = await (Application.Current as App).watcher.GetGeopositionAsync();
        }

        void watcher_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            (Application.Current as App).Currentlocation = args.Position;
        }

        private void ShowPollResponsesButton_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            MessageModel message = ((MessageModel)button.DataContext);

            if (message != null)
            {
                this.PopupChatBubble.DataContext = message;
                this.PopupChatBubble.ChatBubbleDirection = (ChatBubbleDirection)((BooleanToChatPropertyConverter)App.Current.Resources["booleanToChatPropertyConverter"]).Convert(message.IsMine, typeof(ChatBubbleDirection), "direction", "en-US");
                this.PopupChatBubble.Background = (SolidColorBrush)((BooleanToChatPropertyConverter)App.Current.Resources["booleanToChatPropertyConverter"]).Convert(message.IsMine, typeof(SolidColorBrush), "background", "en-US");
                this.PopupChatBubble.BorderBrush = this.PopupChatBubble.Background;
                this.PollResponsesStack.Visibility = System.Windows.Visibility.Visible;
                this.PollResponsesPopup.Visibility = Visibility.Visible;
                this.PollResponsesPopup.IsOpen = true;
                this.PollResponsesSelector.ItemsSource = message.GetGroupedPollResponses();

                this.ApplicationBar = (ApplicationBar)this.Resources["PollResponsesAppBar"];
            }
        }

        private void PollResponsesApplicationBarDone_Click(object sender, EventArgs e)
        {
            this.PollResponsesStack.Visibility = System.Windows.Visibility.Collapsed;
            this.PollResponsesPopup.Visibility = Visibility.Collapsed;
            this.PollResponsesPopup.IsOpen = false;
            this.SelectApplicationBar();
        }

        private void SeeTaskButton_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            Grid listbox = ConversationMessagesView.FindParent<Grid>(button);
            MessageModel message = ((MessageModel)listbox.DataContext);

            if (message.TaskId.HasValue && message.TaskId.Value != Guid.Empty)
            {
                NavigationService.Navigate(new Uri(string.Format("/Views/Tasklist.xaml?clientmessageid={0}", message.TaskId.Value), UriKind.Relative));
            }
        }

        private void PollResponseButtons_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            ItemsControl listbox = ConversationMessagesView.FindParent<ItemsControl>(button);
            MessageModel message = ((MessageModel)listbox.DataContext);

            if (button != null)
            {
                string calendarOption = button.Content.ToString();
                try
                {
                    if (message.IsCalendarMessage && string.Compare(button.Content.ToString(), Strings.CalendarAddOption, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        calendarOption = MessageModel.CalendarAcceptStringNonLocalized;
                        this.AddAppointmentToCalendar(message.AppointmentDateTime.Value, message.AppointmentDuration.Value, ((MessageModel)listbox.DataContext).TextMessage);
                    }
                    else if (message.IsCalendarMessage && string.Compare(button.Content.ToString(), Strings.CalendarDeclineOption, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                         calendarOption = MessageModel.CalendarDeclineStringNonLocalized;
                    }
                }
                catch (Exception)
                {
                }

                // For calendar messages, the localized strings are only for display.
                // When responses are sent over the wire, hard coded strings are sent
                // This is because the sender's language and the recipient's language might be different
                this.PostPollResponse(((MessageModel)listbox.DataContext).MessageId, ((MessageModel)listbox.DataContext).ClientMessageId, calendarOption);
            }
        }

        public void AddAppointmentToCalendar(DateTime dateTimeOfAppt, long duration, string ApptTitle)
        {
            SaveAppointmentTask saveAppointmentTask = new SaveAppointmentTask();

            saveAppointmentTask.StartTime = dateTimeOfAppt;
            saveAppointmentTask.EndTime = dateTimeOfAppt.AddMinutes(30);
            saveAppointmentTask.Subject = "Yapper Appointment: " + ApptTitle;
            saveAppointmentTask.Location = "Yapper Appointment location: ";
            saveAppointmentTask.Details = "Yapper Appointment details: ";
            saveAppointmentTask.IsAllDayEvent = false;
            saveAppointmentTask.Reminder = Reminder.FifteenMinutes;
            saveAppointmentTask.EndTime = dateTimeOfAppt.AddTicks(duration);
            saveAppointmentTask.AppointmentStatus = Microsoft.Phone.UserData.AppointmentStatus.Busy;
            
            saveAppointmentTask.Show();
        }


        private void PostPollResponse(Guid messageId, Guid clientMessageId, string pollResponse)
        {
            ((ConversationMessagesViewModel)this.DataContext).PostPollResponse(messageId, clientMessageId, pollResponse);
        }

        private void SendMessage_Click(object sender, EventArgs e)
        {
            this.messagesListBox.Focus();
            (Application.Current as App).PerfTrackerStopWatch.Start();
            (Application.Current as App).PerfTrackerString += "Send clicked:" + (Application.Current as App).PerfTrackerStopWatch.ElapsedMilliseconds.ToString();
            this.SendMessage();
            if (this.AttachmentGrid.Visibility == System.Windows.Visibility.Collapsed)
            {
                this.NewMessageTextBox.Focus();
            }
        }

        private void ToggleAttachment_Click(object sender, EventArgs e)
        {
            this.messagesListBox.Focus();
            this.AttachmentGrid.Visibility = Visibility.Visible;
            this.ApplicationBar.Buttons.Remove(this.attachButton);
            this.ApplicationBar.Buttons.Add(this.keyboardButton);
        }

        private void ToggleKeyboard_Click(object sender, EventArgs e)
        {
            this.ApplicationBar.Buttons.Remove(this.keyboardButton);
            this.ApplicationBar.Buttons.Add(this.attachButton);
            this.AttachmentGrid.Visibility = Visibility.Collapsed;
            this.NewMessageTextBox.Visibility = Visibility.Visible;
            this.microphone.Visibility = Visibility.Visible;
            this.SetAttachmentPanelVisibility(null);
            this.NewMessageTextBox.Focus();
        }

        private void SendImage_Click(object sender, EventArgs e)
        {
            PhotoChooserTask photoChooserTask = new PhotoChooserTask();
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);
            photoChooserTask.Show();
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] imagebytes = new byte[e.ChosenPhoto.Length];
                    e.ChosenPhoto.Read(imagebytes, 0, (int)e.ChosenPhoto.Length);
                    string textMessage = string.Empty;

                    BitmapImage bmp = new BitmapImage();
                    bmp.SetSource(e.ChosenPhoto);

                    Image tempImage = new Image()
                    {
                        Visibility = System.Windows.Visibility.Collapsed,
                        Source = bmp
                    };

                    WriteableBitmap wb = new WriteableBitmap(tempImage, null);
                    //wb.SaveJpeg(ms, bmp.PixelWidth, bmp.PixelHeight, 0, 100);
                    wb.SaveJpeg(ms, bmp.PixelWidth, bmp.PixelHeight, 0, 50);
                    this.PollQuestionImage.Source = wb;

                    if (!string.IsNullOrEmpty(this.NewMessageTextBox.Text))
                    {
                        textMessage = this.NewMessageTextBox.Text;
                    }
                    else
                    {
                        textMessage = "Sent an image";
                    }

                    this.imageBlob = ms.ToArray();

                    if (this.PollCompositionPanel.Visibility == System.Windows.Visibility.Collapsed)
                    {
                        this.imageBlob = ms.ToArray();
                        this.NewMessageTextBox.Text = textMessage;
                        this.SendMessage();
                    }
                    else
                    {
                        this.PollQuestionImage.Source = wb;
                    }
                }
            }

            this.SetAttachmentPanelVisibility(null);
        }

        public static Stream ToStream(string str)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public void PostSpeedWarning(Geoposition pos)
        {
            string location = string.Empty;
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "CoversationMessagesView::PostSpeedWarning", "start"));

            pos = (Application.Current as App).Currentlocation;
            if (pos != null)
            {
                double speed = pos.Coordinate.Speed.HasValue ? pos.Coordinate.Speed.Value : 0;
                speed = speed * 2.24;

                if (speed > 15.0)
                {
                    MessageBox.Show(string.Format(Strings.DontTextAndDrive, speed));
                }
            }

            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "CoversationMessagesView::PostSpeedWarning", "end"));
        }

        private void SendMessage()
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "CoversationMessagesView::Send", "start"));

            if ( (this.imageBlob == null) &&
                ((string.IsNullOrEmpty(this.PollQuestion.Text) && (this.PollCompositionPanel.Visibility == System.Windows.Visibility.Visible)) ||
                 (string.IsNullOrEmpty(this.ApptQuestion.Text) && (this.ApptCompositionPanel.Visibility == System.Windows.Visibility.Visible)) ))
            {
                MessageBox.Show(Strings.EnterAMessage);
                return;
            }

            if (string.IsNullOrEmpty(this.NewMessageTextBox.Text.Trim()) &&
                (this.PollCompositionPanel.Visibility == System.Windows.Visibility.Collapsed) &&
                (this.ApptCompositionPanel.Visibility == System.Windows.Visibility.Collapsed))
            {
                MessageBox.Show(Strings.EnterAMessage);
                return;
            }

            Geoposition pos = (Application.Current as App).Currentlocation;
            MessageModel newMessageToSend = new MessageModel();
            newMessageToSend.MessageType = MessageType.Message;
            newMessageToSend.ClientMessageId = Guid.NewGuid();
            newMessageToSend.LastUpdateTime = DateTime.UtcNow;
            newMessageToSend.PostDateTime = newMessageToSend.LastUpdateTime;
            newMessageToSend.LastReadTime = new DateTime(1970, 1, 1);

            if (this.imageBlob != null)
            {
                newMessageToSend.MessageFlags |= MessageFlags.Image;
                newMessageToSend.Image = this.imageBlob;
                this.imageBlob = null;
            }

            newMessageToSend.TextMessage = this.NewMessageTextBox.Text;

            if (UserSettingsModel.Instance.SendingLocationEnabled == true && pos != null)
            {
                newMessageToSend.LocationLatitude = pos.Coordinate.Latitude;
                newMessageToSend.LocationLongitude = pos.Coordinate.Longitude;
            }

            this.PostSpeedWarning(pos);

            if (this.PollCompositionPanel.Visibility == System.Windows.Visibility.Visible)
            {
                List<string> pollOptions = new List<string>();

                foreach (StringBuilder item in this.QuestionsListBox.Items)
                {
                    if (!string.IsNullOrEmpty(item.ToString()))
                    {
                        pollOptions.Add(item.ToString());
                    }
                }

                newMessageToSend.TextMessage = this.PollQuestion.Text;
                newMessageToSend.PollOptions = pollOptions;
                newMessageToSend.MessageFlags |= MessageFlags.PollMessage;

                this.imageBlob = null;
            }
            else if (this.ApptCompositionPanel.Visibility == System.Windows.Visibility.Visible)
            {
                newMessageToSend.TextMessage = this.ApptQuestion.Text;
                newMessageToSend.MessageFlags |= MessageFlags.PollMessage | MessageFlags.Calendar;
                DateTime apptTime = new DateTime(this.Cal.SelectedDate.Year, this.Cal.SelectedDate.Month, this.Cal.SelectedDate.Day, this.ApptTimePicker.Value.Value.Hour, this.ApptTimePicker.Value.Value.Minute, 0, DateTimeKind.Local);
                newMessageToSend.AppointmentDateTimeTicks = apptTime.ToUniversalTime().Ticks;
                newMessageToSend.AppointmentDuration = this.ApptDurationPicker.Value.HasValue ? this.ApptDurationPicker.Value.Value.Ticks : (new TimeSpan(1, 0, 0)).Ticks;
            }

            //Messenger.Default.Register<MessageSentEvent>(this, this.ClearTextBox);
            ((ConversationMessagesViewModel)this.DataContext).SendNewMessage(newMessageToSend);

            this.ClearTextBox(new MessageSentEvent() { Success = true });

            this.SetAttachmentPanelVisibility(null);
            this.messagesListBox.Visibility = System.Windows.Visibility.Visible;
        }

        private void SetAttachmentPanelVisibility(StackPanel panel)
        {
            this.PollCompositionPanel.Visibility = System.Windows.Visibility.Collapsed;
            this.ApptCompositionPanel.Visibility = System.Windows.Visibility.Collapsed;

            if (panel != null)
            {
                panel.Visibility = System.Windows.Visibility.Visible;
                this.NewMessageTextBox.Visibility = Visibility.Collapsed;
                this.microphone.Visibility = Visibility.Collapsed;
                this.messagesListBox.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                this.messagesListBox.Visibility = System.Windows.Visibility.Visible;
                this.NewMessageTextBox.Visibility = Visibility.Visible;
                this.microphone.Visibility = Visibility.Visible;
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton button = sender as RadioButton;
            if(button != null)
            {
                this.selectedOption = (string)button.Content;
            }
        }

        private void ClearTextBox(MessageSentEvent messageSent)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "CoversationMessagesView::Send", "End"));
            DispatcherHelper.InvokeOnUiThread(() =>
            {
                if (messageSent.Success)
                {
                    this.NewMessageTextBox.Text = string.Empty;
                    this.messagesListBox.ScrollTo(this.messagesListBox.ItemsSource[this.messagesListBox.ItemsSource.Count - 1]);
                }
            });

            Messenger.Default.Unregister<MessageSentEvent>(this);
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.PollCompositionPanel.Visibility == System.Windows.Visibility.Visible ||
                this.ApptCompositionPanel.Visibility == System.Windows.Visibility.Visible ||
                this.SelectUserPopup.Visibility == System.Windows.Visibility.Visible ||
                this.PollResponsesStack.Visibility == System.Windows.Visibility.Visible)
            {
                if (this.PollResponsesStack.Visibility != System.Windows.Visibility.Visible &&
                    this.SelectUserPopup.Visibility != System.Windows.Visibility.Visible)
                {
                    this.ApplicationBar.Buttons.Remove(this.keyboardButton);
                    this.ApplicationBar.Buttons.Add(this.attachButton);
                }

                this.PollCompositionPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.ApptCompositionPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.AttachmentGrid.Visibility = Visibility.Collapsed;
                this.NewMessageTextBox.Visibility = System.Windows.Visibility.Visible;
                this.microphone.Visibility = Visibility.Visible;

                this.SelectUserPopup.Visibility = System.Windows.Visibility.Collapsed;
                this.SelectUserPopup.IsOpen = false;

                this.PollResponsesStack.Visibility = Visibility.Collapsed;
                this.PollResponsesPopup.Visibility = Visibility.Collapsed;
                this.PollResponsesPopup.IsOpen = false;

                this.messagesListBox.Visibility = System.Windows.Visibility.Visible;
                e.Cancel = true;
                this.SelectApplicationBar();

            }
            else
            {
                if (this.ConversationPagePivot.SelectedIndex == 0)
                {
                    NavigationService.Navigate(new Uri("/Views/YapperChatMessagesPivot.xaml", UriKind.Relative));
                }
                else
                {
                    NavigationService.Navigate(new Uri("/Views/YapperChatContactsPivot.xaml", UriKind.Relative));
                }

                this.SelectApplicationBar();
            }
        }

        private void NewMessageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //this.GridScrollViewer.ScrollToVerticalOffset(this.GridScrollViewer.ExtentHeight);
            //object lastItem = this.messagesListBox.ItemsSource[this.messagesListBox.ItemsSource.Count - 1];
            //this.messagesListBox.ScrollTo(lastItem);
        }

        private void NewMessageTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.messagesListBox.ItemsSource != null && this.messagesListBox.ItemsSource.Count > 0)
            {
                object lastItem = this.messagesListBox.ItemsSource[this.messagesListBox.ItemsSource.Count - 1];
                this.messagesListBox.ScrollTo(lastItem);
            }
        }

        private void NewMessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.SendMessage();
            }
        }

        private void NewMessageTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            this.ApplicationBar.Buttons.Remove(this.keyboardButton);
            this.AttachmentGrid.Visibility = Visibility.Collapsed;
            
            if (!this.ApplicationBar.Buttons.Contains(this.attachButton))
            {
                this.ApplicationBar.Buttons.Add(this.attachButton);
            }

            if (this.messagesListBox.ItemsSource.Count > 0)
            {
                this.messagesListBox.ScrollTo(this.messagesListBox.ItemsSource[this.messagesListBox.ItemsSource.Count - 1]);
            }
        }

        /// <summary>
        /// Handle quit application event
        /// </summary>
        /// <param name="quitEvent"></param>
        private void HandleQuitApplicationEvent(QuitApplicationEvent quitEvent)
        {
        }

        private void GridScrollViewer_LayoutUpdated(object sender, EventArgs e)
        {
        }

        private void ImageTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Image img = sender as Image;
            Guid id = (img.DataContext as MessageModel).MessageId;
            if(img != null)
            {
                NavigationService.Navigate(new Uri(string.Format("/Views/DisplayFullImage.xaml?ImageSource={0}", id.ToString()), UriKind.Relative));
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox senderTextbox = sender as TextBox;

            if (senderTextbox != null)
            {
                StringBuilder source = senderTextbox.DataContext as StringBuilder;

                if (source != null && !string.IsNullOrEmpty(senderTextbox.Text))
                {
                    if (source.Length == 0)
                    {
                        source.Append(senderTextbox.Text);
                    }
                    else
                    {
                    source.Replace(source.ToString(), senderTextbox.Text);
                }
            }
        }
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);    //we’ve reached the end of the tree
            if (parentObject == null)
            {
                return null;
            }

            //check if the parent matches the type we’re looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                return FindParent<T>(parentObject);
            }
        }

        private void QuestionButton_Click(object sender, RoutedEventArgs e)
        {
            this.SetAttachmentPanelVisibility(this.PollCompositionPanel);
            this.PollQuestion.Text = this.NewMessageTextBox.Text;
        }

        private void PollCancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.SetAttachmentPanelVisibility(null);
            this.PollQuestion.Text = string.Empty;
        }

        private void ApptButton_Click(object sender, RoutedEventArgs e)
        {
            this.SetAttachmentPanelVisibility(this.ApptCompositionPanel);
            this.ApptQuestion.Text = this.NewMessageTextBox.Text;
        }

        private void ApptCancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.SetAttachmentPanelVisibility(null);
            this.ApptQuestion.Text = string.Empty;
        }


        private void CallMobileButton_Click(object sender, RoutedEventArgs e)
        {
            PhoneCallTask phoneCallTask = new PhoneCallTask();

            phoneCallTask.PhoneNumber = ((ConversationMessagesViewModel)this.DataContext).ContactDetail.MobilePhone;
            phoneCallTask.DisplayName = ((ConversationMessagesViewModel)this.DataContext).ContactDetail.YapperName;

            phoneCallTask.Show();
        }

        private void CallHomeButton_Click(object sender, RoutedEventArgs e)
        {
            PhoneCallTask phoneCallTask = new PhoneCallTask();

            phoneCallTask.PhoneNumber = ((ConversationMessagesViewModel)this.DataContext).ContactDetail.HomePhone;
            phoneCallTask.DisplayName = ((ConversationMessagesViewModel)this.DataContext).ContactDetail.YapperName;

            phoneCallTask.Show();
        }

        private void AddMemberApplicationBarDone_Click(object sender, EventArgs e)
        {
            foreach (var button in ApplicationBar.Buttons)
            {
                ((ApplicationBarIconButton)button).IsEnabled = false; // disables the button
            }

            foreach (object user in this.ContactsListSelector.SelectedItems)
            {
                ((ConversationMessagesViewModel)this.DataContext).GroupDetail.AddMember((UserModel)user);
            }

            Messenger.Default.Register<GroupMemberAddedEvent>(this, this.GroupMemberAdded);
        }

        private void GroupMember_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get item of LongListSelector.
            List<UserControl> listItems = new List<UserControl>();
            GetItemsRecursive<UserControl>(GroupMemberListSelector, ref listItems);

            // Selected.
            if (e.AddedItems.Count > 0 && e.AddedItems[0] != null)
            {
                foreach (UserControl userControl in listItems)
                {
                    if (e.AddedItems[0].Equals(userControl.DataContext))
                    {
                        VisualStateManager.GoToState(userControl, "Selected", true);
                    }
                }
            }

            // Unselected.
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] != null)
            {
                foreach (UserControl userControl in listItems)
                {
                    if (e.RemovedItems[0].Equals(userControl.DataContext))
                    {
                        VisualStateManager.GoToState(userControl, "Normal", true);
                    }
                }
            }

            ConversationMessagesViewModel vm = (ConversationMessagesViewModel)this.DataContext;

            if (this.GroupMemberListSelector.SelectedItem != null)
            {
                this.ApplicationBar = (ApplicationBar)this.Resources["RemoveMemberAppBar"];
            }

            if (this.GroupMemberListSelector.SelectedItem == null)
            {
                this.SelectApplicationBar();
            }
        }

        /// <summary>
        /// Recursively get the item.
        /// </summary>
        /// <typeparam name="T">The item to get.</typeparam>
        /// <param name="parents">Parent container.</param>
        /// <param name="objectList">Item list</param>
        public static void GetItemsRecursive<T>(DependencyObject parents, ref List<T> objectList) where T : DependencyObject
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(parents);

            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parents, i);

                if (child is T)
                {
                    objectList.Add(child as T);
                }

                GetItemsRecursive<T>(child, ref objectList);
            }

            return;
        }

        private void GroupMemberAdded(GroupMemberAddedEvent added)
        {
            Messenger.Default.Unregister<GroupMemberAddedEvent>(this);

            DispatcherHelper.InvokeOnUiThread(() =>
            {
                foreach (var button in ApplicationBar.Buttons)
                {
                    ((ApplicationBarIconButton)button).IsEnabled = true; // disables the button
                }

                this.ApplicationBar = (ApplicationBar)this.Resources["ContactDetailsAppBar"];
                this.ApplicationBar.IsVisible = true;

                this.SelectUserPopup.Visibility = System.Windows.Visibility.Collapsed;
                this.SelectUserPopup.IsOpen = false;
            });
        }

        private void RemoveUserButton_Click(object sender, EventArgs e)
        {
            if (((ConversationMessagesViewModel)this.DataContext).Recipient.IsGroupOwner)
            {
                foreach (var button in ApplicationBar.Buttons)
                {
                    ((ApplicationBarIconButton)button).IsEnabled = false; // disables the button
                }

                GroupDetailsViewModel gdvm = ((GroupDetailsViewModel)((ConversationMessagesViewModel)this.DataContext).GroupDetail);
                gdvm.RemoveMember((UserModel)this.GroupMemberListSelector.SelectedItem);

                Messenger.Default.Register<GroupMemberRemovedEvent>(this, this.GroupMemberRemoved);
            }
            else
            {
                MessageBox.Show(Strings.OnlyOwnerCanModifyGroup);
            }
        }

        private void GroupMemberRemoved(GroupMemberRemovedEvent groupEvent)
        {
            Messenger.Default.Unregister<GroupMemberRemovedEvent>(this);

            foreach (var button in ApplicationBar.Buttons)
            {
                ((ApplicationBarIconButton)button).IsEnabled = true; // disables the button
            }

            this.SelectApplicationBar();
        }

        private void CancelRemoveUserButton_Click(object sender, EventArgs e)
        {
            this.GroupMemberListSelector.SelectedItem = null;
        }

        private void LocationImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Image img = sender as Image;
            double? lat = (img.DataContext as MessageModel).LocationLatitude;
            double? lon = (img.DataContext as MessageModel).LocationLongitude;
            
            if (lat != null)
            {
                NavigationService.Navigate(new Uri(string.Format("/Views/DisplayLocation.xaml?Latitude={0}&Longitude={1}", lat.Value, lon.Value), UriKind.Relative));
            }
        }

        private void Cal_SelectionChanged(object sender, WPControls.SelectionChangedEventArgs e)
        {

        }

        /// <summary>
        /// Handles the Click event for the Contacts page application bar 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InviteFriends_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/PhoneContactsJumpListView.xaml"), UriKind.Relative));
        }

        private void MessageListBox_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            MessageModel message = e.Container.Content as MessageModel;
            if (message != null)
            {
                var items = this.messagesListBox.ItemsSource;
                var index = items.IndexOf(message);

                if (index < 1)
                {
                    ((ConversationMessagesViewModel)this.DataContext).LoadMoreMessages();
                }
            }
        }

        private void HandleScrollToBottomEvent(ScrollToEvent scroll)
        {
            if (this.messagesListBox.ItemsSource.Count > 0)
            {
                this.messagesListBox.ScrollTo(this.messagesListBox.ItemsSource[this.messagesListBox.ItemsSource.Count - 1]);
            }
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            if (((ConversationMessagesViewModel)this.DataContext).Recipient.IsGroupOwner)
            {
                this.SelectUserPopup.Visibility = Visibility.Visible;
                this.SelectUserPopup.IsOpen = true;

                this.ContactsListSelector.ItemsSource = GroupingHelper.GroupUsers(DataSync.Instance.GetGroupNonMembers(((ConversationMessagesViewModel)this.DataContext).Recipient));
                this.ApplicationBar = (ApplicationBar)this.Resources["AddMemberPageApplicationBar"];
            }
            else
            {
                MessageBox.Show(Strings.OnlyOwnerCanModifyGroup);
            }
        }

        /// <summary>
        /// Handles the Pivot page navigation. This is used to set the correct ApplicationBar.
        /// The app bar for Chat page shows the "New conversation" button
        /// The app bar for the contacts page shows the "Invite Friends" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void ConversationPagePivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SelectApplicationBar();
        }

        private void SelectApplicationBar()
        {
            string pivotResource;

            switch (this.ConversationPagePivot.SelectedIndex)
            {
                case 0:
                    pivotResource = "MessagesAppBar";
                    break;

                case 1:
                    ConversationMessagesViewModel vm = (ConversationMessagesViewModel)this.DataContext;

                    if (vm.IsGroup)
                    {
                        pivotResource = "ContactDetailsAppBar";
                    }
                    else
                    {
                        pivotResource = null;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            this.ApplicationBar = pivotResource == null ? null : (ApplicationBar)this.Resources[pivotResource];
        }

        private void BuildLocalizedApplicationBar()
        {
            ApplicationBar appbar = (ApplicationBar)this.Resources["AddMemberPageApplicationBar"];
            ((ApplicationBarIconButton)appbar.Buttons[0]).Text = YapperChat.Resources.Strings.AddMemberText;

            appbar = (ApplicationBar)this.Resources["ContactDetailsAppBar"];
            ((ApplicationBarIconButton)appbar.Buttons[0]).Text = YapperChat.Resources.Strings.AddMemberText;

            appbar = (ApplicationBar)this.Resources["MessagesAppBar"];
            ((ApplicationBarIconButton)appbar.Buttons[0]).Text = YapperChat.Resources.Strings.SendText;

            appbar = (ApplicationBar)this.Resources["RemoveMemberAppBar"];
            ((ApplicationBarIconButton)appbar.Buttons[0]).Text = YapperChat.Resources.Strings.RemoveUserText;
            ((ApplicationBarIconButton)appbar.Buttons[1]).Text = YapperChat.Resources.Strings.CancelText;

            appbar = (ApplicationBar)this.Resources["PollResponsesAppBar"];
            ((ApplicationBarIconButton)appbar.Buttons[0]).Text = YapperChat.Resources.Strings.DoneText;

        }

        SpeechRecognizerUI speechRecognizer;
        private async void MicrophoneImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.speechRecognizer = new SpeechRecognizerUI();
            this.speechRecognizer.Recognizer.Grammars.Clear();
            this.speechRecognizer.Recognizer.Grammars.AddGrammarFromPredefinedType("search", SpeechPredefinedGrammar.WebSearch);
            await this.speechRecognizer.Recognizer.PreloadGrammarsAsync();
            try
            {
                // Use the built-in UI to prompt the user and get the result.  
                SpeechRecognitionUIResult recognitionResult = await this.speechRecognizer.RecognizeWithUIAsync();

                if (recognitionResult.ResultStatus == SpeechRecognitionUIStatus.Succeeded)
                {
                    // Output the speech recognition result. 
                    NewMessageTextBox.Text = recognitionResult.RecognitionResult.Text.Trim();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}