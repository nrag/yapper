using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YapperChat.ViewModels;
using YapperUnitTest.Mock;
using YapperChat.Models;
using System.IO;
using System.Windows;
using System.Runtime.Serialization.Json;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.EventMessages;

namespace YapperUnitTest
{
    [TestClass]
    public class AllConversationsViewModelTests
    {
        public AllConversationsViewModelTests()
        {
            // To ensure that the Dispatcher calls are executed in the same thread
            DispatcherHelper.TestHook = true;
        }

        /// <summary>
        /// Basic load from database test
        /// </summary>
        [TestMethod]
        public void NewAllConversationsViewModelTest()
        {
            List<List<ConversationModel>> conversations = new List<List<ConversationModel>>();
            List<UserModel> owners = new List<UserModel>();
            this.LoadConversations(conversations, owners);

            for (int i = 0; i < conversations.Count; i++)
            {
                MockServiceProxy serviceProxy = new MockServiceProxy() { };
                MockUserSettings userSettings = new MockUserSettings();
                MockDataContextWrapper dataContextWrapper = new MockDataContextWrapper(new MockDatabase() { Conversations = conversations[i] });
                userSettings.Save(owners[i]);

                using (AllConversationsViewModel allConversations = new AllConversationsViewModel(serviceProxy, userSettings, dataContextWrapper))
                {
                    NotifyPropertyChangedTester propertyChangedTester = new NotifyPropertyChangedTester(allConversations);
                    NotifyCollectionChangedTester<ConversationModel> collectionChangedTester = new NotifyCollectionChangedTester<ConversationModel>(allConversations.Conversations);
                    allConversations.LoadInitialConversations();

                    while (!allConversations.IsLoaded)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }

                    Assert.AreEqual(1, propertyChangedTester.Changes.Count, "IsLoaded property changed event was not generated");
                    propertyChangedTester.AssertChange(0, "IsLoaded");

                    Assert.AreEqual(conversations[i].Count, collectionChangedTester.Count, "Number of conversations in notify collection changed doesn't match");
                }
            }
        }

        /// <summary>
        /// Basic load from database. Service does not have any new changes at load.
        /// Push notification for a new conversation is handled after the app starts up.
        /// </summary>
        [TestMethod]
        public void HandlePushNotificationNewConversationTest()
        {
            List<List<ConversationModel>> conversations = new List<List<ConversationModel>>();
            List<UserModel> owners = new List<UserModel>();
            this.LoadConversations(conversations, owners);

            for (int i = 0; i < conversations.Count; i++)
            {
                MockServiceProxy serviceProxy = new MockServiceProxy() { Conversations = conversations[i] };
                MockUserSettings userSettings = new MockUserSettings();
                MockDataContextWrapper dataContextWrapper = new MockDataContextWrapper(new MockDatabase() { Conversations = conversations[i] });
                userSettings.Save(owners[i]);

                using (AllConversationsViewModel allConversations = new AllConversationsViewModel(serviceProxy, userSettings, dataContextWrapper))
                {
                    allConversations.LoadInitialConversations();

                    NotifyCollectionChangedTester<ConversationModel> collectionChangedTester = new NotifyCollectionChangedTester<ConversationModel>(allConversations.Conversations);
                    int newUserId = 5000;
                    long newConversationId = long.MaxValue;
                    Messenger.Default.Send<PushNotificationEvent>(new PushNotificationEvent(this, (long)2000, newConversationId, "test", DateTime.Now.Ticks, "HandlePushNotificationNewConversationTest" + i.ToString(), newUserId));

                    // We should be able to retrieve the new conversation by recipient and by conversation id
                    Assert.AreNotEqual(null, allConversations.GetConversation(newUserId));
                    Assert.AreNotEqual(null, allConversations.GetConversationFromConversationId(newConversationId));

                    // There should be one collection changed event
                    Assert.AreEqual(collectionChangedTester.Count, 1, "Push notification was not handled. The new conversation was not added to the collection");
                }
            }
        }

        /// <summary>
        /// Load conversations from database. Service does not have new messages.
        /// Handle push notification for an existing conversation.
        /// </summary>
        [TestMethod]
        public void HandlePushNotificationExistingConversationTest()
        {
            List<List<ConversationModel>>  conversations = new List<List<ConversationModel>>();
            List<UserModel>  owners = new List<UserModel>();
            this.LoadConversations(conversations, owners);

            for (int i = 0; i < conversations.Count; i++)
            {
                if (conversations[i].Count == 0)
                {
                    return;
                }

                MockServiceProxy serviceProxy = new MockServiceProxy() { Conversations = conversations[i] };
                MockUserSettings userSettings = new MockUserSettings();
                MockDataContextWrapper dataContextWrapper = new MockDataContextWrapper(new MockDatabase() { Conversations = conversations[i] });
                userSettings.Save(owners[i]);

                using (AllConversationsViewModel allConversations = new AllConversationsViewModel(serviceProxy, userSettings, dataContextWrapper))
                {
                    allConversations.LoadInitialConversations();

                    NotifyCollectionChangedTester<ConversationModel> collectionChangedTester = new NotifyCollectionChangedTester<ConversationModel>(allConversations.Conversations);
                    long existingConversationId = conversations[i].Count > 0 ? conversations[i][0].ConversationId : long.MaxValue;
                    int existingConversationUserId = -1;

                    foreach (UserModel user in conversations[i][0].ConversationParticipants)
                    {
                        if (user.Id != owners[i].Id)
                        {
                            existingConversationUserId = user.Id;
                        }
                    }

                    foreach (ConversationModel conversation in conversations[i])
                    {
                        foreach (UserModel user in conversation.ConversationParticipants)
                        {
                            if (user.Id != owners[i].Id)
                            {
                                Assert.AreEqual(conversation, allConversations.GetConversationFromConversationId(conversation.ConversationId), "Conversation cannot be retrieved by guid");
                                Assert.AreEqual(conversation, allConversations.GetConversation(user.Id), "Conversation cannot be retrieved by recipient");
                            }
                        }
                    }

                    PushNotificationEvent pushEvent = new PushNotificationEvent(this, (long)2000, existingConversationId, "test", DateTime.Now.Ticks, "HandlePushNotificationExistingConversationTest" + i.ToString(), existingConversationUserId);
                    Messenger.Default.Send<PushNotificationEvent>(pushEvent);

                    Assert.AreEqual(collectionChangedTester.Count, 1, "Push notification was not handled. The new conversation was not added to the collection");
                }
            }
        }

        /// <summary>
        /// Load conversations from database. Service updates existing conversation.
        /// </summary>
        [TestMethod]
        public void ServiceUpdateExistingConversationTest()
        {
            List<List<ConversationModel>> conversations = new List<List<ConversationModel>>();
            List<UserModel> owners = new List<UserModel>();
            this.LoadConversations(conversations, owners);

            for (int i = 0; i < conversations.Count; i++)
            {
                if (conversations[i].Count == 0)
                {
                    return;
                }

                List<ConversationModel> serviceConversation = new List<ConversationModel>();
                for (int j = 0; j < conversations[i].Count; j++)
                {
                    ConversationModel newConversation = new ConversationModel();
                    newConversation.ConversationId = conversations[i][j].ConversationId;
                    newConversation.ConversationParticipants = conversations[i][j].ConversationParticipants;
                    newConversation.LastPostUtcTime = DateTime.Now;
                    newConversation.LastPostPreview = "New message for conversation" + j.ToString();
                    serviceConversation.Add(newConversation);
                }

                MockServiceProxy serviceProxy = new MockServiceProxy() { Conversations = serviceConversation };
                MockUserSettings userSettings = new MockUserSettings();
                MockDataContextWrapper dataContextWrapper = new MockDataContextWrapper(new MockDatabase() { Conversations = conversations[i] });
                userSettings.Save(owners[i]);

                using (AllConversationsViewModel allConversations = new AllConversationsViewModel(serviceProxy, userSettings, dataContextWrapper))
                {
                    allConversations.LoadInitialConversations();

                    NotifyCollectionChangedTester<ConversationModel> collectionChangedTester = new NotifyCollectionChangedTester<ConversationModel>(allConversations.Conversations);

                    while (!allConversations.IsLoaded)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }

                    Assert.AreEqual(collectionChangedTester.Count, serviceConversation.Count, "Service proxy changes weren't generated");

                    for (int j = 0; j < allConversations.Conversations.Count; j++)
                    {
                        Assert.AreEqual(allConversations.Conversations[j].LastPostUtcTime, serviceConversation[j].LastPostUtcTime, "Date didn't match with the service proxy update");
                        Assert.AreEqual(allConversations.Conversations[j].LastPostPreview, serviceConversation[j].LastPostPreview, "preview didn't match with the service proxy update");
                    }
                }
            }
        }

        /// <summary>
        /// Load conversations from database. Service updates existing conversation.
        /// </summary>
        [TestMethod]
        public void ServiceAddNewConversationTest()
        {
            List<List<ConversationModel>> conversations = new List<List<ConversationModel>>();
            List<UserModel> owners = new List<UserModel>();
            this.LoadConversations(conversations, owners);

            for (int i = 0; i < conversations.Count; i++)
            {
                if (conversations[i].Count == 0)
                {
                    return;
                }

                int newConversations = 3;
                MockUserSettings userSettings = new MockUserSettings();
                MockDataContextWrapper dataContextWrapper = new MockDataContextWrapper(new MockDatabase() { Conversations = conversations[i] });
                userSettings.Save(owners[i]);
                
                Random random = new Random((int)DateTime.Now.Ticks);
                List<ConversationModel> serviceConversation = new List<ConversationModel>();
                serviceConversation.AddRange(conversations[i]);
                long conversationIdGenerator = long.MaxValue;
                for (int j = 0; j < newConversations; j++)
                {
                    ConversationModel newConversation = new ConversationModel();
                    newConversation.ConversationId = conversationIdGenerator--;
                    newConversation.ConversationParticipants = new List<UserModel>();
                    newConversation.ConversationParticipants.Add(userSettings.Me);
                    newConversation.ConversationParticipants.Add(new UserModel() { Id = random.Next(100, 2000), Name = "ServiceAddNewConversationTestUser" + j.ToString(), PhoneNumber = "425 111 1111" });
                    newConversation.LastPostUtcTime = DateTime.Now;
                    newConversation.LastPostPreview = "New message for conversation" + j.ToString();
                    serviceConversation.Add(newConversation);
                }

                MockServiceProxy serviceProxy = new MockServiceProxy() { Conversations = serviceConversation }; 
                
                using (AllConversationsViewModel allConversations = new AllConversationsViewModel(serviceProxy, userSettings, dataContextWrapper))
                {
                    allConversations.LoadInitialConversations();

                    NotifyCollectionChangedTester<ConversationModel> collectionChangedTester = new NotifyCollectionChangedTester<ConversationModel>(allConversations.Conversations);

                    while (!allConversations.IsLoaded)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }

                    Assert.AreEqual(newConversations, collectionChangedTester.Count, "Service proxy changes weren't generated");

                    for (int j = 0; j < allConversations.Conversations.Count; j++)
                    {
                        Assert.AreEqual(allConversations.Conversations[j].LastPostUtcTime, serviceConversation[j].LastPostUtcTime, "Date didn't match with the service proxy update");
                        Assert.AreEqual(allConversations.Conversations[j].LastPostPreview, serviceConversation[j].LastPostPreview, "preview didn't match with the service proxy update");
                    }

                    Assert.AreEqual(((MockTable<ConversationModel>)dataContextWrapper.Table<ConversationModel>()).Count, serviceConversation.Count, "New conversation was not inserted in to the database");
                }
            }
        }

        private void LoadConversations(List<List<ConversationModel>> conversations, List<UserModel> owners)
        {
            //Replace 'MyProject' with the name of your XAP/Project
            Stream txtStream = Application.GetResourceStream(new Uri("Data/conversation.txt", UriKind.Relative)).Stream;

            using (StreamReader sr = new StreamReader(txtStream))
            {
                StringBuilder json = new StringBuilder();
                string line = null;
                while((line = sr.ReadLine()) != null)
                {
                    json.Append(line);
                }

                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(List<OwnerAndConversations>));
                List<OwnerAndConversations> result = (List<OwnerAndConversations>)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json.ToString())));

                foreach (OwnerAndConversations oc in result)
                {
                    conversations.Add(oc.Conversations);
                    owners.Add(oc.Owner);
                }
            }
        }
    }
}
