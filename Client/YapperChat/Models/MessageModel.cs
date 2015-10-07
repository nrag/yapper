using Microsoft.Phone;
using Microsoft.Phone.UserData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using System.Windows.Media.Imaging;

using YapperChat.Common;
using System.Text;
using System.Runtime.Serialization.Json;
using YapperChat.Sync;
using System.Runtime.CompilerServices;
using System.Windows.Resources;
using Microsoft.Phone.Data.Linq.Mapping;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.EventMessages;
using YapperChat.Resources;
using YapperChat.Controls.Interactions;
using System.Collections.ObjectModel;
using ProtoBuf;

namespace YapperChat.Models
{
    /// <summary>
    /// The serialized MessageModel is auto-generated.
    /// This partial implementation contains difference between the over-the-wire version
    /// and the version in the local database.
    /// </summary>
    [Index(Columns = "ConversationId, MessageType, PostDateTimeUtcTicks, IsPollResponseMessage DESC", IsUnique = false, Name = "conversation_Messages")]
    [Index(Columns = "PollMessageId, SenderId", IsUnique = false, Name = "poll_Responses")]
    [Index(Columns = "IsTaskMessage, SenderId, RecipientId", IsUnique = false, Name = "task_Message")]
    public partial class MessageModel : INotifyPropertyChanged, IComparable, IItem
    {
        private static string hourGlass = "\u231B ";

        public MessageModel()
        {
            this.MessageType = MessageType.Message;
        }

        public MessageModel(MessageModel m)
        {
            this.ConversationId = m.ConversationId;
            this.IsFullyDownloaded = m.IsFullyDownloaded;
            this.LastReadTime = m.LastReadTime;
            this.MessageId = m.MessageId;
            this.MessageFlags = m.MessageFlags;
            this.PostDateTime = m.PostDateTime;
            this.LastUpdateTime = m.LastUpdateTime;
            this.Recipient = m.Recipient;
            this.RecipientId = m.RecipientId;
            this.Sender = m.Sender;
            this.SenderId = m.SenderId;
            this.TextMessage = m.TextMessage;
            this.MessageFlags = m.MessageFlags;
            this.ClientMessageId = m.ClientMessageId;
        }

        public static string CalendarAcceptStringNonLocalized
        {
            get
            {
                return "Accept";
            }
        }

        public static string CalendarDeclineStringNonLocalized
        {
            get
            {
                return "Decline";
            }
        }

        public string MessageIdString
        {
            get
            {
                return MessageId.ToString();
            }
        }

        [DataMember]
        public UserModel Sender
        {
            get;
            set;
        }

        [DataMember]
        public UserModel Recipient
        {
            get;
            set;
        }

        [Column]
        [ProtoMember(4)]
        public int SenderId
        {
            get;
            set;
        }

        [Column]
        [ProtoMember(5)]
        public int RecipientId
        {
            get;
            set;
        }

        /// <summary>
        /// This is a client only property that is set only on
        /// conversation messages. This is to track the number of new
        /// messages in a conversation
        /// </summary>
        [Column]
        public DateTime LastReadTime
        {
            get;
            set;
        }

        /// <summary>
        /// Whether the row represents a conversation or an actual message
        /// </summary>
        [Column]
        public MessageType MessageType
        {
            get;
            set;
        }

        public DateTime PostDateTime
        {
            get
            {
                return new DateTime(this.PostDateTimeUtcTicks, DateTimeKind.Utc);
            }

            set
            {
                this.PostDateTimeUtcTicks = value.ToUniversalTime().Ticks;
            }
        }

        private long? clientVisibleTimeTicks;

        [Column]
        public long? ClientVisibleTimeTicks
        {
            get
            {
                if (!clientVisibleTimeTicks.HasValue)
                {
                    clientVisibleTimeTicks = DateTime.UtcNow.Ticks;
                }

                return clientVisibleTimeTicks.Value;
            }

            set
            {
                this.clientVisibleTimeTicks = value;
            }
        }

        public DateTime ClientVisibleTime
        {
            get
            {
                if (!clientVisibleTimeTicks.HasValue)
                {
                    clientVisibleTimeTicks = DateTime.UtcNow.Ticks;
                }

                return new DateTime(clientVisibleTimeTicks.Value);
            }
        }

        public DateTime LastUpdateTime
        {
            get
            {
                return new DateTime(this.LastUpdateTimeUtcTicks, DateTimeKind.Utc);
            }

            set
            {
                this.LastUpdateTimeUtcTicks = value.ToUniversalTime().Ticks;
            }
        }

        [Column]
        public bool IsFullyDownloaded
        {
            get;
            set;
        }

        [Column(CanBeNull = true)]
        public string SerializedPollOptions
        {
            get
            {
                MemoryStream stream = new MemoryStream();
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<string>));
                serializer.WriteObject(stream, this.PollOptions);
                return System.Text.Encoding.UTF8.GetString(stream.ToArray(), 0, stream.ToArray().Length);
            }

            set
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(List<string>));
                this.PollOptions = (List<string>)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(value)));
            }
        }

        private string myPollResponse;
        [Column(CanBeNull = true)]
        public string MyPollResponse
        {
            get
            {
                return this.myPollResponse;
            }

            set
            {
                this.myPollResponse = value;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.NotifyPropertyChanged();
                    this.NotifyPropertyChanged("HasResponded");
                    this.NotifyPropertyChanged("HasNotResponded");
                    this.NotifyPropertyChanged("CalendarHasResponded");
                    this.NotifyPropertyChanged("PollHasResponded");
                    this.NotifyPropertyChanged("HasAcceptedCalendarMessage");
                    this.NotifyPropertyChanged("HasDeclinedCalendarMessage");
                    this.NotifyPropertyChanged("ShouldShowPollOptions");
                    this.NotifyPropertyChanged("ShouldShowGroupPollResponses");
                    this.NotifyPropertyChanged("ShouldShowNonGroupPollResponse");
                    this.NotifyPropertyChanged("PollResponseMessage");
                });
            }
        }

        private string otherPollResponse;
        [Column(CanBeNull = true)]
        public string OtherPollResponse
        {
            get
            {
                return this.otherPollResponse;
            }

            set
            {
                this.otherPollResponse = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("OtherHasResponded");
                this.NotifyPropertyChanged("CalendarHasResponded");
                this.NotifyPropertyChanged("PollHasResponded");
                this.NotifyPropertyChanged("HasAcceptedCalendarMessage");
                this.NotifyPropertyChanged("HasDeclinedCalendarMessage");
                this.NotifyPropertyChanged("ShouldShowPollOptions");
                this.NotifyPropertyChanged("ShouldShowGroupPollResponses"); 
                this.NotifyPropertyChanged("ShouldShowNonGroupPollResponse");
                this.NotifyPropertyChanged("PollResponseMessage");
            }
        }

        [Column]
        public int? UnreadCount
        {
            get;
            set;
        }

        public List<string> PollOptionsForDisplay
        {
            get
            {
                if (this.PollOptions != null &&
                    this.PollOptions.Count != 0)
                {
                    return this.PollOptions;
                }

                if ((this.MessageFlags & MessageFlags.Calendar) == MessageFlags.Calendar)
                {
                    List<string> calendarOptions = new List<string>();
                    calendarOptions.Add(Resources.Strings.CalendarAddOption);
                    if (!this.IsMine || this.IsGroup)
                    {
                        calendarOptions.Add(Resources.Strings.CalendarDeclineOption);
                    }

                    return calendarOptions;
                }

                if ((this.MessageFlags & MessageFlags.Task) == MessageFlags.Task)
                {
                    List<string> taskOptions = new List<string>();
                    taskOptions.Add(Resources.Strings.TaskAcceptOption);
                    taskOptions.Add(Resources.Strings.TaskDeclineOption);

                    return taskOptions;
                }

                return this.PollOptions;
            }
        }

        private WriteableBitmap messageImage = null;
        public WriteableBitmap MessageImage
        {
            get
            {
                if (this.IsImage && this.messageImage == null)
                {
                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += (o, f) =>
                        {
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                try
                                {
                                    this.messageImage = this.GetImageFromIsolatedStorage(this.MessageId.ToString(), 0, 0);
                                    if (this.messageImage != null)
                                    {
                                        this.messageImage.Invalidate();

                                        this.NotifyPropertyChanged("MessageImage");
                                        this.NotifyPropertyChanged("ImageHeight");
                                        this.NotifyPropertyChanged("ImageWidth");
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            });
                        };

                    worker.RunWorkerAsync();

                    //StreamResourceInfo resourceInfo = Application.GetResourceStream(new Uri("Images/appbar.image.portrait.png", UriKind.Relative));
                    //BitmapImage img = new BitmapImage();
                    //img.SetSource(resourceInfo.Stream);
                    return null;
                }

                return this.messageImage;
            }
        }

        public bool IsImage
        {
            get
            {
                bool res = (this.MessageFlags & MessageFlags.Image) == MessageFlags.Image;
                return res;
            }
        }

        public int ImageHeight
        {
            get
            {
                return this.MessageImage == null ? 0 : (int)Math.Floor(this.MessageImage.PixelHeight * this.ImageScaleForChatBubble);
            }
        }

        public int ImageWidth
        {
            get
            {
                return this.MessageImage == null ? 0 : (int)Math.Floor(this.MessageImage.PixelWidth * this.ImageScaleForChatBubble);
            }
        }

        public float ImageScaleForChatBubble
        {
            get
            {
                if (this.MessageImage == null)
                {
                    return 1;
                }

                if (this.MessageImage.PixelWidth < 320)
                {
                    return 1;
                }

                return ((float)320) / this.MessageImage.PixelWidth;
            }
        }

        public string IsImageMessage
        {
            get
            {
                if (this.IsImage)
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        public string IsTextMessage
        {
            get
            {
                if (!string.IsNullOrEmpty(TextMessage))
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        [Column]
        public bool? IsTaskMessage
        {
            get { return (this.MessageFlags & MessageFlags.Task) == MessageFlags.Task; }
            set { }
        }

        private int? _LastTaskUpdaterId;

        [Column]
        public int? LastTaskUpdaterId
        {
            get
            {
                return this._LastTaskUpdaterId;
            }

            set
            {
                this._LastTaskUpdaterId = value;
                this.NotifyPropertyChanged("LastTaskUpdaterName");
                this.NotifyPropertyChanged("ShowTaskUpdatedBy");
            }
        }

        public string LastTaskUpdaterName
        {
            get
            {
                if (this.LastTaskUpdaterId == null)
                {
                    return null;
                }

                UserModel updater = DataSync.Instance.GetUser(this.LastTaskUpdaterId.Value);
                if (updater != null)
                {
                    return updater.Name;
                }

                return null;
            }
        }

        public bool ShowTaskUpdatedBy
        {
            get
            {
                if (this.LastTaskUpdaterId == null)
                {
                    return false;
                }

                if (this.IsCompleted)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsTaskItemMessage
        {
            get { return (this.MessageFlags & MessageFlags.TaskItem) == MessageFlags.TaskItem; }
        }

        public bool IsTaskInfoMessage
        {
            get { return (this.MessageFlags & MessageFlags.TaskInfo) == MessageFlags.TaskInfo; }
        }

        public string Hourglass
        {
            get
            {
                return MessageModel.hourGlass;
            }
        }

        public bool PollClosedMessage
        {
            get
            {
                // if it is two days since the poll, mark it as responded or closed
                //
                if (this.IsPollMessage && DateTime.Now.Subtract(this.PostDateTime).Days > 2)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public int CompletedTaskCount
        {
            get
            {
                this.LoadTaskList();
                if (this.TaskItemList != null)
                {
                    return this.TaskItemList.Count((m) => { return m.IsCompleted == true; });
                }

                return 0;
            }
        }

        public int IncompleteTaskCount
        {
            get
            {
                this.LoadTaskList();
                if (this.TaskItemList != null)
                {
                    return this.TaskItemList.Count((m) => { return m.IsCompleted == false; });
                }

                return 0;
            }
        }

        public bool IsTaskShared
        {
            get
            {
                return this.Recipient != null;
            }
        }

        public string TaskSharedWithString
        {
            get
            {
                if (this.Sender == null || this.Recipient == null)
                {
                    return null;
                }

                if (this.IsGroup)
                {
                    return String.Format(Strings.TaskSharedWithGroup, this.Recipient.Name, this.Sender.Name);
                }

                if (this.Sender.Id == UserSettingsModel.Instance.Me.Id)
                {
                    return string.Format(Strings.TaskSharedWith, this.Recipient.Name);
                }

                if (this.Recipient.Id == UserSettingsModel.Instance.Me.Id)
                {
                    return string.Format(Strings.TaskSharedBy, this.Sender.Name);
                }

                return null;
            }
        }

        public int TaskItemCount
        {
            get
            {
                this.LoadTaskList();
                if (this.TaskItemList != null)
                {
                    return this.TaskItemList.Count;
                }

                return 0;
            }
        }

        public bool ShowTaskCompletionButton
        {
            get
            {
                return !CanUpdateTask;
            }
        }

        public bool CanUpdateTask
        {
            get
            {
                return !this.IsMine && !(this.TaskIsCompleted.HasValue && this.TaskIsCompleted.Value);
            }
        }

        public string TextMessageForDisplay
        {
            get
            {
                return this.TextMessage;
            }
        }

        public bool IsEncrypted
        {
            get
            {
                return false;
            }
        }

        public bool IsGroup
        {
            get
            {
                if ((this.Sender!= null && this.Sender.UserType == UserType.Group) || (this.Recipient!= null && this.Recipient.UserType == UserType.Group))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsMine
        {
            get
            {
                if (this.Sender != null && (this.Sender.Id == UserSettingsModel.Instance.UserId))
                {
                    return true;
                }

                return false;
            }
        }


        public bool IsLocationAvailable
        {
            get
            {
                return this.LocationLongitude.HasValue;
            }
        }

        public string SimpleDateTime
        {
            get
            {
                return this.PostDateTime.SimpleDateTime();
            }
        }

        public DateTime? AppointmentDateTime
        {
            get
            {
                if (!this.AppointmentDateTimeTicks.HasValue)
                {
                    return null;
                }

                return (new DateTime(this.AppointmentDateTimeTicks.Value,DateTimeKind.Utc)).ToLocalTime();
            }
        }

        public string SimpleAppointmentDate
        {
            get
            {
                if (this.AppointmentDateTime.HasValue)
                {
                    return this.AppointmentDateTime.Value.GetCalendarDate();
                }

                return Resources.Strings.ThisIsNotAnAppointment;
            }
        }

        public string SimpleAppointmentStartTime
        {
            get
            {
                if (this.AppointmentDateTime.HasValue)
                {
                    return this.AppointmentDateTime.Value.GetCalendarTime();
                }

                return Resources.Strings.ThisIsNotAnAppointment;
            }
        }

        public string SimpleAppointmentEndTime
        {
            get
            {
                if (this.AppointmentDateTime.HasValue)
                {
                    long duration = this.AppointmentDuration.HasValue ? this.AppointmentDuration.Value : (new TimeSpan(1, 0, 0)).Ticks;
                    return this.AppointmentDateTime.Value.AddTicks(duration).GetCalendarTime();
                }

                return Resources.Strings.ThisIsNotAnAppointment;
            }
        }

        public string SimpleAppointmentStartEndTime
        {
            get
            {
                return string.Format(Strings.AppointmentStartToEndTime, SimpleAppointmentStartTime, SimpleAppointmentEndTime);
            }
        }

        public string SenderAndDateTime
        {
            get
            {
                return string.Format("{0} @ {1}", this.Sender.Name, this.PostDateTime.SimpleDateTime());
            }
        }

        [Column]
        public bool IsPollMessage
        {
            get
            {
                return ((this.MessageFlags & MessageFlags.PollMessage) == MessageFlags.PollMessage);
            }

            set
            {
            }
        }

        [Column]
        public bool IsCalendarMessage
        {
            get
            {
                return ((this.MessageFlags & MessageFlags.Calendar) == MessageFlags.Calendar);
            }

            set
            {
            }
        }

        [Column]
        public bool IsPollMessageButNotCalendar
        {
            get
            {
                return ((this.MessageFlags & (MessageFlags.PollMessage | MessageFlags.Calendar)) == MessageFlags.PollMessage);
            }

            set
            {
            }
        }

        [Column]
        public bool IsPollResponseMessage
        {
            get
            {
                return ((this.MessageFlags & MessageFlags.PollResponseMessage) == MessageFlags.PollResponseMessage);
            }

            set
            {
            }
        }

        [Column]
        public bool HasImage
        {
            get
            {
                return ((this.MessageFlags & MessageFlags.Image) == MessageFlags.Image);
            }

            set
            {
            }
        }

        [Column(IsPrimaryKey = false, CanBeNull = true)]
        public Guid? PollClientMessageId
        {
            get;
            set;
        }

        private List<KeyValuePair<string, int>> _responses;

        public List<KeyValuePair<string, int>> Responses
        {
            get
            {
                // We are reading all the poll responses.
                // In ConversationMessagesViewModel we read only 20 messages
                // That may not contain all the poll responses. To get the count right
                // we read all the responses.
                this.LoadPollResponses();

                return this._responses;
            }

            set
            {
                this._responses = value;
            }
        }


        /// <summary>
        /// Calendar message has been accepted if
        /// 1. if the message is mine and other response is accept 
        /// 2. if the message is not mine and my response is accept
        /// </summary>
        public bool HasAcceptedCalendarMessage
        {
            get
            {
                if (!this.IsCalendarMessage)
                {
                    return false;
                }

                if ((this.IsGroup || !this.IsMine) &&
                    string.Compare(this.MyPollResponse, MessageModel.CalendarAcceptStringNonLocalized, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }

                if (this.IsMine && string.Compare(this.OtherPollResponse, MessageModel.CalendarAcceptStringNonLocalized, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }

                return false;
            }
        }

        public bool HasDeclinedCalendarMessage
        {
            get
            {

                if (!this.IsCalendarMessage)
                {
                    return false;
                }

                if ((this.IsGroup || !this.IsMine) && 
                    string.Compare(this.MyPollResponse, MessageModel.CalendarDeclineStringNonLocalized, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }

                if (this.IsMine && string.Compare(this.OtherPollResponse, MessageModel.CalendarDeclineStringNonLocalized, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }

                return false;
            }
        }

        public string PollResponseMessage
        {
            get
            {
                if (!this.ShouldShowNonGroupPollResponse)
                {
                    return null;
                }

                if (this.IsCalendarMessage && this.IsMine && !this.IsGroup)
                {
                    if (this.HasAcceptedCalendarMessage)
                    {
                        return string.Format(Strings.OtherCalendarAccept, this.Recipient.Name);
                    }

                    if (this.HasDeclinedCalendarMessage)
                    {
                        return string.Format(Strings.OtherCalendarDecline, this.Recipient.Name);
                    }

                    return string.Format(Strings.OtherCalendarNoResponse, this.Recipient.Name);
                }

                if (this.IsCalendarMessage && (!this.IsMine || this.IsGroup))
                {
                    if (this.HasAcceptedCalendarMessage)
                    {
                        return Strings.MyCalendarAccept;
                    }

                    if (this.HasDeclinedCalendarMessage)
                    {
                        return Strings.MyCalendarDecline;
                    }
                }

                if (this.IsPollMessage && this.IsMine && !this.IsGroup)
                {
                    if (this.OtherHasResponded)
                    {
                        return string.Format(Strings.OtherPollResponse, this.Recipient.Name, this.OtherPollResponse);
                    }

                    return string.Format(Strings.OtherPollNoResponse, this.Recipient.Name);
                }

                if (this.IsPollMessage && (!this.IsMine || this.IsGroup))
                {
                    if (this.HasResponded)
                    {
                        return string.Format(Strings.MyPollResponse, this.MyPollResponse);
                    }

                    return null;
                }

                return null;
            }
        }

        public List<KeyValuePair<string, int>> DisplayPollResponses
        {
            get
            {
                if (!this.IsPollMessage)
                {
                    return null;
                }

                // We are reading all the poll responses.
                // In ConversationMessagesViewModel we read only 20 messages
                // That may not contain all the poll responses. To get the count right
                // we read all the responses.
                this.LoadPollResponses();

                List<KeyValuePair<string, int>> displayPollResponses = new List<KeyValuePair<string, int>>(this.PollOptionsForDisplay.Count);
                if (this.PollOptionsForDisplay != null)
                {
                    if (!this.IsCalendarMessage)
                    {
                        foreach (string option in this.PollOptionsForDisplay)
                        {
                            int count = 0;
                            if (this._responses != null)
                            {
                                count = this._responses.Count(x => x.Key == option);
                            }

                            displayPollResponses.Add(new KeyValuePair<string, int>(option, count));
                        }
                    }
                    else
                    {
                        int acceptCount = 0;
                        int declineCount = 0;
                        if (this._responses != null)
                        {
                            acceptCount = this._responses.Count(x => x.Key == MessageModel.CalendarAcceptStringNonLocalized);
                            declineCount = this._responses.Count(x => x.Key == MessageModel.CalendarDeclineStringNonLocalized);
                        }

                        displayPollResponses.Add(new KeyValuePair<string, int>(Strings.CalendarAcceptMessage, acceptCount));
                        displayPollResponses.Add(new KeyValuePair<string, int>(Strings.CalendarDeclineOption, declineCount));
                    }
                }

                return displayPollResponses;
            }
        }

        public bool OtherHasResponded
        {
            get
            {
                if (this.OtherPollResponse == null)
                {
                    return false;
                }

                return true;
            }
        }

        public bool HasResponded
        {
            get
            {
                if (!this.IsPollMessage)
                {
                    return false;
                }

                if (this.MyPollResponse == null)
                {
                    return false;
                }

                return true;
            }
        }

        public bool HasNotResponded
        {
            get
            {
                if (!this.IsPollMessage)
                {
                    return false;
                }

                return !this.HasResponded;
            }
        }

        /// <summary>
        /// Show  the options iff this is a pollmessage and
        /// 1. this is a group message or 
        /// 2. this is not a group message and message is not sent by me and message is not a calendar message
        /// </summary>
        public bool ShouldShowPollOptions
        {
            get
            {
                if (!this.IsPollMessage)
                {
                    return false;
                }

                // If this is not a group and the poll is from me,
                // we do not show the poll options
                if (!this.IsGroup && this.IsMine && this.IsPollMessageButNotCalendar)
                {
                    return false;
                }

                // If it's been responded to already, don't show the poll options
                if (this.HasResponded)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Disable the poll options if i've responded already
        /// or if the poll is older than 2 days
        /// or if the poll is calendar with the event is in the past
        /// </summary>
        public bool ShouldEnablePollButtons
        {
            get
            {
                if (!this.IsPollMessage)
                {
                    return false;
                }

                // if it is two days since the poll mark it as responded or closed
                //
                if (!this.IsCalendarMessage && this.PollClosedMessage)
                {
                    return false;
                }

                // If the appointment is in the past or if the poll is old,
                // do not show the poll options
                if (this.IsCalendarMessage)
                {
                    if (this.AppointmentDateTime < DateTime.Now)
                    {
                        return false;
                    }
                }

                // If it's been responded to already, don't show the poll options
                if (this.HasResponded)
                {
                    return false;
                }

                return true;
            }
        }

        public bool ShouldShowNonGroupPollResponse
        {
            get
            {
                if (!this.IsPollMessage)
                {
                    return false;
                }

                if (!this.IsGroup ||
                    this.IsGroup && this.HasResponded)
                {
                    return true;
                }

                return false;
            }
        }

        public bool ShouldShowGroupPollResponses
        {
            get
            {
                // if it is two days since the poll mark it as responded or closed
                //
                if (this.IsPollMessage && this.IsGroup && this.HasResponded)
                {
                    return true;
                }

                return false;
            }
        }

        public bool ShouldShowSeeTaskButton
        {
            get
            {
                if (!this.IsTaskInfoMessage)
                {
                    return false;
                }

                if (this.TaskId == null)
                {
                    return false;
                }

                MessageModel m = DataSync.Instance.GetMessageFromClientId(this.TaskId.Value);
                if (m == null)
                {
                    return false;
                }

                return true;
            }
        }

        private ObservableSortedList<MessageModel> taskItemList;

        [DataMember]
        [ProtoMember(19, IsRequired = false)]
        public ObservableSortedList<MessageModel> TaskItemList
        {
            get
            {
                return this.taskItemList;
            }

            set
            {
                this.taskItemList = value;
                if (this.taskItemList != null)
                {
                    this.taskItemList.CollectionChanged += this.TaskCollectionChanged;
                }
            }
        }

        public bool IsTaskNew
        {
            get
            {
                if (this.SenderId != UserSettingsModel.Instance.Me.Id && this.LastReadTime == new DateTime(1970, 1, 1))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsTaskUpdated
        {
            get
            {
                if (this.LastReadTime != new DateTime(1970, 1, 1) &&
                    this.LastReadTime < this.ClientVisibleTime.ToLocalTime())
                {
                    return true;
                }

                return false;
            }
        }

        private void TaskCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.NotifyPropertyChanged("TaskItemCount");
            this.NotifyPropertyChanged("IsCompleted");
            this.NotifyPropertyChanged("CompletedTaskCount");
            this.NotifyPropertyChanged("IncompleteTaskCount");
        }

        public void AddResponse(MessageModel pollResponse)
        {
            this.InternalAddResponse(pollResponse);

            this.NotifyPropertyChanged("Responses");
            this.NotifyPropertyChanged("DisplayPollResponses");
            this.NotifyPropertyChanged("HasResponded");
            this.NotifyPropertyChanged("HasNotResponded");
            this.NotifyPropertyChanged("CalendarHasResponded");
            this.NotifyPropertyChanged("PollHasResponded");
            this.NotifyPropertyChanged("HasAcceptedCalendarMessage");
            this.NotifyPropertyChanged("HasDeclinedCalendarMessage");
        }

        public ObservableSortedList<PollResponseGroup<UserModel>> GetGroupedPollResponses()
        {
            ObservableSortedList<PollResponseGroup<UserModel>> groupedResponses = new ObservableSortedList<PollResponseGroup<UserModel>>();
            Dictionary<string, PollResponseGroup<UserModel>> responseToGroup = new Dictionary<string, PollResponseGroup<UserModel>>();

            this.LoadPollResponses();

            if (this.PollOptions != null)
            {
                foreach (string option in this.PollOptions)
                {
                    PollResponseGroup<UserModel> group = new PollResponseGroup<UserModel>(option);
                    responseToGroup.Add(option, group);
                    groupedResponses.Add(group);
                }
            }

            if (this.IsCalendarMessage)
            {
                PollResponseGroup<UserModel> acceptGroup = new PollResponseGroup<UserModel>(Strings.CalendarAcceptMessage);
                responseToGroup.Add(Strings.CalendarAcceptMessage, acceptGroup);
                groupedResponses.Add(acceptGroup);

                PollResponseGroup<UserModel> declineGroup = new PollResponseGroup<UserModel>(Strings.CalendarDeclineMessage);
                responseToGroup.Add(Strings.CalendarDeclineMessage, declineGroup);
                groupedResponses.Add(declineGroup);
            }

            foreach (KeyValuePair<string, int> response in this.Responses)
            {
                PollResponseGroup<UserModel> group = null;
                string key = response.Key;

                if (this.IsCalendarMessage)
                {
                    if (string.Compare(response.Key, MessageModel.CalendarAcceptStringNonLocalized, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        key = Strings.CalendarAcceptMessage;
                    }

                    if (string.Compare(response.Key, MessageModel.CalendarDeclineStringNonLocalized, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        key = Strings.CalendarDeclineMessage;
                    }
                }

                if (!responseToGroup.TryGetValue(key, out group))
                {
                    group = new PollResponseGroup<UserModel>(response.Key);
                    groupedResponses.Add(group);
                    responseToGroup.Add(response.Key, group);
                }

                group.Add(DataSync.Instance.GetUser(response.Value));
            }

            return groupedResponses;
        }

        public void LoadTaskList()
        {
            if (this.MessageId == Guid.Empty ||
                !this.IsTaskMessage.Value ||
                (this.taskItemList != null))
            {
                return;
            }

            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "MessageModel", "start load poll messages"));
                this.taskItemList = new ObservableSortedList<MessageModel>(4, new TaskListComparer<MessageModel>());

                IEnumerable<MessageModel> responses = DataSync.Instance.GetTaskItems(this.ClientMessageId);
                foreach (MessageModel m in responses)
                {
                    this.taskItemList.Add(m);
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "MessageModel", "end load poll messages"));
            }
        }

        private WriteableBitmap GetImageFromIsolatedStorage(string imagename, int width, int length)
        {
            FileStream imageStream = null;
            try
            {
                var isoFile = IsolatedStorageFile.GetUserStoreForApplication();
                using (imageStream = isoFile.OpenFile(imagename, FileMode.Open, FileAccess.Read))
                {
                    if (width == 0 && length == 0)
                    {
                        var imageSource = PictureDecoder.DecodeJpeg(imageStream);
                        return new WriteableBitmap(imageSource);
                    }
                    else
                    {
                        var imageSource = PictureDecoder.DecodeJpeg(imageStream, width, length);
                        return new WriteableBitmap(imageSource);
                    }

                }
            }
            catch (Exception)
            {
                // if the message is corrupt
                return null;
            }
            finally
            {
                if (imageStream != null)
                {
                    imageStream.Dispose();
                }
            }
        }

        private void InternalAddResponse(MessageModel pollResponse)
        {
            if (this._responses == null)
            {
                this._responses = new List<KeyValuePair<string, int>>();
            }

            if (!pollResponse.IsPollResponseMessage)
            {
                return;
            }

            if (IsGroup)
            {
                this._responses.Add(new KeyValuePair<string, int>(pollResponse.PollResponse, pollResponse.SenderId));
            }
            else
            {
                // Add response when 
                // 1. Recipient question and my answer or
                // 2. My question and Recipient answer
                if ((IsMine && pollResponse.SenderId != UserSettingsModel.Instance.UserId) ||
                    (!IsMine && pollResponse.SenderId == UserSettingsModel.Instance.UserId))
                {
                    this._responses.Add(new KeyValuePair<string, int>(pollResponse.PollResponse, pollResponse.SenderId));
                }
            }
        }

        private void LoadPollResponses()
        {
            if (this.MessageId == Guid.Empty ||
                !this.IsPollMessage ||
                (this._responses != null))
            {
                return;
            }

            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "MessageModel", "start load poll messages"));
                this._responses = new List<KeyValuePair<string, int>>();

                IEnumerable<MessageModel> responses = DataSync.Instance.GetPollResponses(this.MessageId, this.ClientMessageId);
                foreach (MessageModel m in responses)
                {
                    this.InternalAddResponse(m);
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "MessageModel", "end load poll messages"));
            }
        }

        public override bool Equals(object obj)
        {
            MessageModel otherMessage = obj as MessageModel;

            if (otherMessage == null)
            {
                return false;
            }

            return this.CompareTo(otherMessage) == 0;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public object Clone()
        {
            MessageModel m = this.CloneInternal();
            m.Sender = this.Sender;
            m.Recipient = this.Recipient;
            return m;
        }
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        public int CompareTo(object obj)
        {
            MessageModel other = obj as MessageModel;
            if (other == null)
            {
                return 1;
            }

            int val = this.MessageId.CompareTo(other.MessageId);

            if (val == 0)
            {
                return val;
            }

            val = this.ClientMessageId.CompareTo(other.ClientMessageId);
            
            if (val == 0)
            {
                return val;
            }

            val = this.ClientVisibleTime.CompareTo(other.ClientVisibleTime);

            if (val != 0)
            {
                return val;
            }

            return this.PostDateTimeUtcTicks.CompareTo(other.PostDateTimeUtcTicks);
        }

        #region IItem implementation
        public bool IsCompleted
        {
            get
            {
                if (!this.IsTaskMessage.Value)
                {
                    return this.TaskIsCompleted ?? false;
                }

                return (this.TaskItemCount > 0 && this.TaskItemCount == this.CompletedTaskCount);
                /*
                bool isCompleted = this.TaskIsCompleted ?? false;

                return isCompleted || (this.TaskItemCount > 0 && this.TaskItemCount == this.CompletedTaskCount);
                 * */
            }

            set
            {
                this.TaskIsCompleted = value;
                this.LastTaskUpdaterId = UserSettingsModel.Instance.Me.Id;
                this.NotifyPropertyChanged("IsCompleted");
            }
        }

        public bool IsPullDown
        {
            get;
            set;
        }

        public void SetTaskName(string name)
        {
            this.TaskName = name;
        }
        #endregion
    }

    class TaskListComparer<T> : IComparer<T> where T : IItem
    {
        public int Compare(T x, T y)
        {
            if (x.IsPullDown)
            {
                return -1;
            }

            if (y.IsPullDown)
            {
                return 1;
            }

            if (x.IsCompleted && !y.IsCompleted)
            {
                return 1;
            }
            
            if (!x.IsCompleted && y.IsCompleted)
            {
                return -1;
            }

            if (x.ItemOrder != null && y.ItemOrder != null)
            {
                return x.ItemOrder.CompareTo(y.ItemOrder);
            }

            return 1;
        }
    }
}