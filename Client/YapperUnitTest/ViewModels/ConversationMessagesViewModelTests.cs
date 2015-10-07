using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using YapperChat.Models;
using YapperChat.Common;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using YapperUnitTest.Mock;
using YapperChat.ViewModels;
using YapperChat.EventMessages;
using GalaSoft.MvvmLight.Messaging;
using System.Text;

namespace YapperUnitTest.ViewModels
{
    [TestClass]
    public class ConversationMessagesViewModelTests
    {
        List<List<Tuple<ConversationModel, List<MessageModel>>>> conversationMessages;

        List<UserModel> owners;


        public ConversationMessagesViewModelTests()
        {
            // To ensure that the Dispatcher calls are executed in the same thread
            DispatcherHelper.TestHook = true;

            this.conversationMessages = new List<List<Tuple<ConversationModel, List<MessageModel>>>>();
            this.owners = new List<UserModel>();
            this.LoadConversationsAndMessages();
        }

        [TestMethod]
        public void CreateConversationMessagesViewModelTest()
        {
            for (int i = 0; i < this.conversationMessages.Count; i++)
            {
                for (int j = 0; j < this.conversationMessages[i].Count; j++)
                {

                    MockServiceProxy serviceProxy = new MockServiceProxy() { Messages = this.conversationMessages[i][j].m_Item2};
                    MockDataContextWrapper dataContext = new MockDataContextWrapper(new MockDatabase() { });
                    MockUserSettings userSettings = new MockUserSettings();
                    userSettings.Save(owners[i]);

                    UserModel recipient = null;
                    foreach (UserModel user in this.conversationMessages[i][j].m_Item1.ConversationParticipants)
                    {
                        if (!user.Equals(userSettings.Me))
                        {
                            recipient = user;
                        }
                    }

                    ConversationMessagesViewModel messages = new ConversationMessagesViewModel(serviceProxy, dataContext, userSettings, this.conversationMessages[i][j].m_Item1.ConversationId, recipient);
                    NotifyPropertyChangedTester propertyChangedTester = new NotifyPropertyChangedTester(messages);
                    NotifyCollectionChangedTester<MessageModel> collectionChangedTester = new NotifyCollectionChangedTester<MessageModel>(messages.Messages);
                    messages.LoadMessagesForConversations();

                    Assert.AreEqual(1, propertyChangedTester.Changes.Count, "IsLoaded property changed event was not generated");
                    propertyChangedTester.AssertChange(0, "IsLoading");

                    Assert.AreEqual(this.conversationMessages[i][j].m_Item2.Count, collectionChangedTester.Count, "Number of messages in notify collection changed doesn't match");
                }
            }
        }

        [TestMethod]
        public void HandlePushNotificationTest()
        {
            for (int i = 0; i < this.conversationMessages.Count; i++)
            {
                for (int j = 0; j < this.conversationMessages[i].Count; j++)
                {

                    MockServiceProxy serviceProxy = new MockServiceProxy() { Messages = this.conversationMessages[i][j].m_Item2 };
                    MockUserSettings userSettings = new MockUserSettings();
                    userSettings.Save(owners[i]);

                    UserModel recipient = null;
                    foreach (UserModel user in this.conversationMessages[i][j].m_Item1.ConversationParticipants)
                    {
                        if (!user.Equals(userSettings.Me))
                        {
                            recipient = user;
                        }
                    }

                    ConversationMessagesViewModel messages = new ConversationMessagesViewModel(serviceProxy, userSettings, this.conversationMessages[i][j].m_Item1.ConversationId, recipient);
                    
                    messages.LoadMessagesForConversations();

                    Random random = new Random((int)DateTime.Now.Ticks);
                    long existingConversationId = this.conversationMessages[i][j].m_Item2.Count > 0 ? this.conversationMessages[i][j].m_Item2[0].ConversationId : long.MaxValue;
                    int existingConversationUserId = this.conversationMessages[i][j].m_Item2.Count > 0 ? this.conversationMessages[i][j].m_Item2[0].Sender.Id : random.Next(1000, 2000) ;

                    PushNotificationEvent pushEvent = new PushNotificationEvent(this, (long)2000, existingConversationId, "test", DateTime.Now.Ticks, "ConversationMessagesHandlePushNotificationTest" + i.ToString(), existingConversationUserId);

                    int messageCountBeforePush = messages.Messages.Count;
                    NotifyCollectionChangedTester<MessageModel> collectionChangedTester = new NotifyCollectionChangedTester<MessageModel>(messages.Messages);
                    Messenger.Default.Send<PushNotificationEvent>(pushEvent);

                    Assert.AreEqual(1, collectionChangedTester.Count, "Collection changed event was not generated");
                    Assert.AreEqual(messageCountBeforePush + 1, messages.Messages.Count, "Push notification was not handled properly");
                }
            }
        }

        [TestMethod]
        public void HandlePushNotificationForDifferentConversationTest()
        {
            for (int i = 0; i < this.conversationMessages.Count; i++)
            {
                for (int j = 0; j < this.conversationMessages[i].Count; j++)
                {

                    MockServiceProxy serviceProxy = new MockServiceProxy() { Messages = this.conversationMessages[i][j].m_Item2 };
                    MockUserSettings userSettings = new MockUserSettings();
                    userSettings.Save(owners[i]);

                    UserModel recipient = null;
                    foreach (UserModel user in this.conversationMessages[i][j].m_Item1.ConversationParticipants)
                    {
                        if (!user.Equals(userSettings.Me))
                        {
                            recipient = user;
                        }
                    }

                    ConversationMessagesViewModel messages = new ConversationMessagesViewModel(serviceProxy, userSettings, this.conversationMessages[i][j].m_Item1.ConversationId, recipient);

                    messages.LoadMessagesForConversations();

                    Random random = new Random((int)DateTime.Now.Ticks);
                    long differentConversationId = long.MaxValue;
                    int differentConversationUserId = random.Next(1000, 2000);

                    PushNotificationEvent pushEvent = new PushNotificationEvent(this, (long)2000, differentConversationId, "test", DateTime.Now.Ticks, "ConversationMessagesHandlePushNotificationTest" + i.ToString(), differentConversationUserId);

                    int messageCountBeforePush = messages.Messages.Count;
                    NotifyCollectionChangedTester<MessageModel> collectionChangedTester = new NotifyCollectionChangedTester<MessageModel>(messages.Messages);
                    Messenger.Default.Send<PushNotificationEvent>(pushEvent);

                    Assert.AreEqual(0, collectionChangedTester.Count, "Collection changed was generated when it should not be");
                    Assert.AreEqual(messageCountBeforePush, messages.Messages.Count, "Push notification was not handled properly");
                }
            }
        }

        /// <summary>
        /// This test case covers the following:
        /// 1. Send a new message. Ensure that it's added.
        /// 2. Send a new message. Enable chat heads. Ensure message is added and quit application event is generated.
        /// 3. Send a new message. Simulate failure in web service call. Ensure message collection is not changed. Ensure quit application event is not generated.
        /// </summary>
        [TestMethod]
        public void SendNewMessageTest()
        {
            for (int i = 0; i < this.conversationMessages.Count; i++)
            {
                for (int j = 0; j < this.conversationMessages[i].Count; j++)
                {

                    MockServiceProxy serviceProxy = new MockServiceProxy() { Messages = this.conversationMessages[i][j].m_Item2 };
                    MockUserSettings userSettings = new MockUserSettings();
                    userSettings.Save(owners[i]);

                    UserModel recipient = null;
                    foreach (UserModel user in this.conversationMessages[i][j].m_Item1.ConversationParticipants)
                    {
                        if (!user.Equals(userSettings.Me))
                        {
                            recipient = user;
                        }
                    }

                    ConversationMessagesViewModel messages = new ConversationMessagesViewModel(serviceProxy, userSettings, this.conversationMessages[i][j].m_Item1.ConversationId, recipient);

                    messages.LoadMessagesForConversations();

                    NotifyCollectionChangedTester<MessageModel> collectionChangedTester = new NotifyCollectionChangedTester<MessageModel>(messages.Messages);

                    /*************************************************
                     * SendMessage chat heads enabled
                     * ***********************************************/

                    int messageCountBeforeNewMessage = messages.Messages.Count;

                    // Send a new message.
                    // The mock proxy will invoke the callback immediately
                    messages.SendNewMessage("SendNewMessageTest message chatheads disabled" + DateTime.Now.Ticks);

                    Assert.AreEqual(1, collectionChangedTester.Count, "Collection changed event was not generated");
                    Assert.AreEqual(messageCountBeforeNewMessage + 1, messages.Messages.Count, "New message was not added to the collection");

                    /*************************************************
                     * SendMessage chat heads enabled
                     * ***********************************************/

                    // Send another message but this time with chatheads enabled.
                    // Ensure that quit application event is generated.
                    messageCountBeforeNewMessage = messages.Messages.Count;
                    userSettings.ChatHeadEnabled = true;
                    bool quitEventGenerated = false;
                    collectionChangedTester.Count = 0;
                    Messenger.Default.Register<QuitApplicationEvent>(this, (QuitApplicationEvent e) => { quitEventGenerated = true; });

                    messages.SendNewMessage("SendNewMessageTest message chatheads disabled" + DateTime.Now.Ticks);

                    Assert.AreEqual(1, collectionChangedTester.Count, "IsLoaded property changed event was not generated");
                    Assert.AreEqual(messageCountBeforeNewMessage + 1, messages.Messages.Count, "New messaged was not added to the collection");

                    /*************************************************
                     * SendMessage simulate failure
                     * ***********************************************/
                    // Remember the count before new message
                    messageCountBeforeNewMessage = messages.Messages.Count;

                    // Set chat heads to enabled to true and 
                    userSettings.ChatHeadEnabled = true;
                    // Simulate failure to true
                    serviceProxy.SimulateSendMessageFailure = true;

                    // Set quitEventGenerated to false
                    quitEventGenerated = false;

                    collectionChangedTester.Count = 0;
                    Messenger.Default.Register<QuitApplicationEvent>(this, (QuitApplicationEvent e) => { quitEventGenerated = true; });

                    messages.SendNewMessage("SendNewMessageTest message chatheads disabled" + DateTime.Now.Ticks);

                    Assert.AreEqual(0, collectionChangedTester.Count, "IsLoaded property changed event was not generated");
                    Assert.AreEqual(messageCountBeforeNewMessage, messages.Messages.Count, "New messaged was not added to the collection");
                    Assert.AreEqual(false, quitEventGenerated, "Quit Application event was not generated");
                }
            }
        }

        private void LoadConversationsAndMessages()
        {
            //Replace 'MyProject' with the name of your XAP/Project
            Stream txtStream = Application.GetResourceStream(new Uri("Data/messages.txt", UriKind.Relative)).Stream;

            using (StreamReader sr = new StreamReader(txtStream))
            {
                StringBuilder json = new StringBuilder();
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    json.Append(line);
                }
                
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(OwnerConversationAndMessages));
                OwnerConversationAndMessages result = (OwnerConversationAndMessages)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json.ToString())));

                this.conversationMessages.Add(result.ConversationsAndMessages);
                this.owners.Add(result.Owner);
                
            }
        }
    }

    [DataContract()]
    public class OwnerConversationAndMessages
    {
        [DataMember]
        public UserModel Owner
        {
            get;
            set;
        }

        [DataMember]
        public List<Tuple<ConversationModel, List<MessageModel>>> ConversationsAndMessages
        {
            get;
            set;
        }
    }
}
