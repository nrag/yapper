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
using YapperUnitTest.Mock;
using YapperChat.Models;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Collections.Generic;
using YapperChat.ViewModels;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.EventMessages;

namespace YapperUnitTest.ViewModels
{
    [TestClass]
    public class RegisteredUsersViewModelTests
    {
        private List<UserModel> users;

        public RegisteredUsersViewModelTests()
        {
            this.LoadUsers();
        }

        /// <summary>
        /// ServiceProxy doesn't return any users. All users are loaded from database.
        /// 1. Ensure Contacts collection is changed.
        /// 2. Contacts are not deleted from database.
        /// </summary>
        [TestMethod]
        public void LoadContactsFromDatabase()
        {
            MockServiceProxy serviceProxy = new MockServiceProxy() { };
            MockUserSettings userSettings = new MockUserSettings();
            MockDataContextWrapper dataContextWrapper = new MockDataContextWrapper(new MockDatabase() {Users = this.users});
            MockContactSearchController searchController = new MockContactSearchController();

            RegisteredUsersViewModel ruvm = new RegisteredUsersViewModel(serviceProxy, userSettings, dataContextWrapper, searchController);

            // start loading the users from database
            NotifyCollectionOfCollectionChangedTester<UserModel> collectionChanged = new NotifyCollectionOfCollectionChangedTester<UserModel>(ruvm.RegisteredUsers);
            ruvm.Search();

            Assert.AreEqual(this.users.Count, collectionChanged.Count, "The users were not read from the database correctly");
            Assert.AreEqual(this.users.Count, ((MockTable<UserModel>)dataContextWrapper.Table<UserModel>()).Count, "The database was not supposed to be modified.");
        }

        /// <summary>
        /// Database is empty. All contacts are loaded from service. Verify the following:
        /// 1. Contacts collection is changed.
        /// 2. Database is updated.
        /// </summary>
        [TestMethod]
        public void LoadContactsFromService()
        {
            MockServiceProxy serviceProxy = new MockServiceProxy() { Users = this.users };
            MockUserSettings userSettings = new MockUserSettings();
            MockDataContextWrapper dataContextWrapper = new MockDataContextWrapper(new MockDatabase());
            MockContactSearchController searchController = new MockContactSearchController() { Users = this.users };

            RegisteredUsersViewModel ruvm = new RegisteredUsersViewModel(serviceProxy, userSettings, dataContextWrapper, searchController);

            // start loading the users from database
            NotifyCollectionOfCollectionChangedTester<UserModel> collectionChanged = new NotifyCollectionOfCollectionChangedTester<UserModel>(ruvm.RegisteredUsers);
            ruvm.Search();

            while (ruvm.IsLoading)
            {
                System.Threading.Thread.Sleep(1000);
            }

            Assert.AreEqual(this.users.Count, collectionChanged.Count, "The users were not read from the database correctly");
            Assert.AreEqual(this.users.Count, ((MockTable<UserModel>)dataContextWrapper.Table<UserModel>()).Count, "The database was not supposed to be modified.");
        }

        /// <summary>
        /// Some contacts are loaded from database. More contacts are added from the service call. Ensure the following
        /// 1. Contacts collection changes appropriately.
        /// 2. Database is updated.
        /// </summary>
        [TestMethod]
        public void LoadContactsFromDatabaseAndNewServiceContacts()
        {
            List<UserModel> newUsers = new List<UserModel>();
            Random random = new Random();
            for (int i = 0; i < 5; i++)
            {
                newUsers.Add(new UserModel(){ Id = 1000 + 100*i, Name = "LoadContactsFromDatabaseAndNewServiceContacts" + i, PhoneNumber = "+1 100-200-300" + i});
            }

            MockServiceProxy serviceProxy = new MockServiceProxy() { Users =  newUsers};
            MockUserSettings userSettings = new MockUserSettings();
            MockDataContextWrapper dataContextWrapper = new MockDataContextWrapper(new MockDatabase() { Users = this.users });
            MockContactSearchController searchController = new MockContactSearchController() { Users = newUsers };

            RegisteredUsersViewModel ruvm = new RegisteredUsersViewModel(serviceProxy, userSettings, dataContextWrapper, searchController);

            // start loading the users from database
            NotifyCollectionOfCollectionChangedTester<UserModel> collectionChanged = new NotifyCollectionOfCollectionChangedTester<UserModel>(ruvm.RegisteredUsers);
            ruvm.Search();

            while (ruvm.IsLoading)
            {
                System.Threading.Thread.Sleep(1000);
            }

            Assert.AreEqual(this.users.Count + newUsers.Count, collectionChanged.Count, "The users were not read from the database correctly");
            Assert.AreEqual(this.users.Count + newUsers.Count, ((MockTable<UserModel>)dataContextWrapper.Table<UserModel>()).Count, "The database was not supposed to be modified.");
        }

        /// <summary>
        /// Contacts are loaded from database. Service returns some contacts but all of them are already in the database.
        /// 1. Contacts collection doesn't change
        /// 2. Database is not updated.
        /// </summary>
        [TestMethod]
        public void LoadContactsFromDatabaseAndNoNewServiceContacts()
        {
            MockServiceProxy serviceProxy = new MockServiceProxy() { Users = this.users };
            MockUserSettings userSettings = new MockUserSettings();
            MockDataContextWrapper dataContextWrapper = new MockDataContextWrapper(new MockDatabase() { Users = this.users });
            MockContactSearchController searchController = new MockContactSearchController() { Users = this.users };

            RegisteredUsersViewModel ruvm = new RegisteredUsersViewModel(serviceProxy, userSettings, dataContextWrapper, searchController);

            // start loading the users from database
            NotifyCollectionOfCollectionChangedTester<UserModel> collectionChanged = new NotifyCollectionOfCollectionChangedTester<UserModel>(ruvm.RegisteredUsers);
            ruvm.Search();

            while (ruvm.IsLoading)
            {
                System.Threading.Thread.Sleep(1000);
            }

            Assert.AreEqual(this.users.Count, collectionChanged.Count, "The users were not read from the database correctly");
            Assert.AreEqual(this.users.Count, ((MockTable<UserModel>)dataContextWrapper.Table<UserModel>()).Count, "The database was not supposed to be modified.");
        }

        /// <summary>
        /// Contacts are loaded. Add new contact using the event. Ensure
        /// 1. Contacts collection changes.
        /// 2. Database is updated.
        /// </summary>
        [TestMethod]
        public void LoadContactsFromDatabaseAndAddNewContact()
        {
            MockServiceProxy serviceProxy = new MockServiceProxy() { Users = this.users };
            MockUserSettings userSettings = new MockUserSettings();
            MockDataContextWrapper dataContextWrapper = new MockDataContextWrapper(new MockDatabase() { Users = this.users });
            MockContactSearchController searchController = new MockContactSearchController() { Users = this.users };

            RegisteredUsersViewModel ruvm = new RegisteredUsersViewModel(serviceProxy, userSettings, dataContextWrapper, searchController);

            // start loading the users from database
            ruvm.Search();

            while (ruvm.IsLoading)
            {
                System.Threading.Thread.Sleep(1000);
            }

            Random random = new Random();
            UserModel user = new UserModel() { Id = random.Next(100, 500), Name = "LoadContactsFromDatabaseAndNewServiceContacts", PhoneNumber = "+1 100-200-3000"};
            NotifyCollectionOfCollectionChangedTester<UserModel> collectionChanged = new NotifyCollectionOfCollectionChangedTester<UserModel>(ruvm.RegisteredUsers);

            Messenger.Default.Send<NewContactEvent>(new NewContactEvent() { Contact = user }, userSettings);

            Assert.AreEqual(1, collectionChanged.Count, "The contact was not added");
            Assert.AreEqual(this.users.Count + 1, ((MockTable<UserModel>)dataContextWrapper.Table<UserModel>()).Count, "The database was not supposed to be modified.");
        }

        /// <summary>
        /// Contacts are loaded. Add new contact using the event. Ensure
        /// 1. Contacts collection changes.
        /// 2. Database is updated.
        /// </summary>
        [TestMethod]
        public void LoadContactsFromDatabaseAndAddExistingContact()
        {
            MockServiceProxy serviceProxy = new MockServiceProxy() { Users = this.users };
            MockUserSettings userSettings = new MockUserSettings();
            MockDataContextWrapper dataContextWrapper = new MockDataContextWrapper(new MockDatabase() { Users = this.users });
            MockContactSearchController searchController = new MockContactSearchController() { Users = this.users };

            RegisteredUsersViewModel ruvm = new RegisteredUsersViewModel(serviceProxy, userSettings, dataContextWrapper, searchController);

            // start loading the users from database
            ruvm.Search();

            while (ruvm.IsLoading)
            {
                System.Threading.Thread.Sleep(1000);
            }

            NotifyCollectionOfCollectionChangedTester<UserModel> collectionChanged = new NotifyCollectionOfCollectionChangedTester<UserModel>(ruvm.RegisteredUsers);

            Messenger.Default.Send<NewContactEvent>(new NewContactEvent() { Contact = this.users[0] });

            Assert.AreEqual(0, collectionChanged.Count, "The contact should not be added");
            Assert.AreEqual(this.users.Count, ((MockTable<UserModel>)dataContextWrapper.Table<UserModel>()).Count, "The database was not supposed to be modified.");
        }

        /// <summary>
        /// No contacts are loaded. Add contacts one by one in non-sorted order.
        /// Ensure that contacts are saved appropriately.
        /// </summary>
        [TestMethod]
        public void TestAddContactReverseAlphabeticalOrder()
        {
            MockServiceProxy serviceProxy = new MockServiceProxy() { Users = new List<UserModel>() };
            MockUserSettings userSettings = new MockUserSettings();
            MockDataContextWrapper dataContextWrapper = new MockDataContextWrapper(new MockDatabase() { Users = new List<UserModel>() });
            MockContactSearchController searchController = new MockContactSearchController() { Users = new List<UserModel>() };

            RegisteredUsersViewModel ruvm = new RegisteredUsersViewModel(serviceProxy, userSettings, dataContextWrapper, searchController);

            // start loading the users from database
            ruvm.Search();

            while (ruvm.IsLoading)
            {
                System.Threading.Thread.Sleep(1000);
            }

            NotifyCollectionOfCollectionChangedTester<UserModel> collectionChanged = new NotifyCollectionOfCollectionChangedTester<UserModel>(ruvm.RegisteredUsers);

            // Sort it in reverse order
            this.users.Sort((x, y) => { return -1 * string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase); });

            for (int i = 0; i < this.users.Count; i++)
            {
                Messenger.Default.Send<NewContactEvent>(new NewContactEvent() { Contact = this.users[i] }, userSettings);
            }

            Assert.AreEqual(this.users.Count, collectionChanged.Count, "The contact should not be added");
            Assert.AreEqual(this.users.Count, ((MockTable<UserModel>)dataContextWrapper.Table<UserModel>()).Count, "The database was not supposed to be modified.");

            // Sort it in alphabetical order and attempt to insert again
            this.users.Sort((x, y) => { return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase); });

            for (int i = 0; i < this.users.Count; i++)
            {
                Messenger.Default.Send<NewContactEvent>(new NewContactEvent() { Contact = this.users[i] }, userSettings);
            }

            Assert.AreEqual(this.users.Count, collectionChanged.Count, "The contact should not be added");
            Assert.AreEqual(this.users.Count, ((MockTable<UserModel>)dataContextWrapper.Table<UserModel>()).Count, "The database was not supposed to be modified.");
        }

        /// <summary>
        /// No contacts are loaded. Add contacts one by one in non-sorted order.
        /// Ensure that the number of contact groups created are correct.
        /// </summary>
        [TestMethod]
        public void TestContactGroups()
        {
            MockServiceProxy serviceProxy = new MockServiceProxy() { Users = new List<UserModel>() };
            MockUserSettings userSettings = new MockUserSettings();
            MockDataContextWrapper dataContextWrapper = new MockDataContextWrapper(new MockDatabase() { Users = new List<UserModel>() });
            MockContactSearchController searchController = new MockContactSearchController() { Users = new List<UserModel>() };

            RegisteredUsersViewModel ruvm = new RegisteredUsersViewModel(serviceProxy, userSettings, dataContextWrapper, searchController);

            // start loading the users from database
            ruvm.Search();

            while (ruvm.IsLoading)
            {
                System.Threading.Thread.Sleep(1000);
            }

            NotifyCollectionOfCollectionChangedTester<UserModel> collectionChanged = new NotifyCollectionOfCollectionChangedTester<UserModel>(ruvm.RegisteredUsers);
            int id = 1;

            // Add one user whose first letter is 't'
            UserModel tUser = new UserModel() { Name = "tUser", Id = id++, PhoneNumber = "123 456 789" + id };
            Messenger.Default.Send<NewContactEvent>(new NewContactEvent() { Contact = tUser }, userSettings);

            Assert.AreEqual(1, collectionChanged.Count, "The contact should not be added");
            Assert.AreEqual(1, ((MockTable<UserModel>)dataContextWrapper.Table<UserModel>()).Count, "The database was not supposed to be modified.");
            Assert.AreEqual(1, ruvm.RegisteredUsers.Count, "Wrong number of contact groups created");

            // Add another user whose first letter is 'd'
            UserModel dUser = new UserModel() { Name = "dUser", Id = id++, PhoneNumber = "123 456 789" + id };
            Messenger.Default.Send<NewContactEvent>(new NewContactEvent() { Contact = dUser }, userSettings);

            Assert.AreEqual(2, collectionChanged.Count, "The contact should not be added");
            Assert.AreEqual(2, ((MockTable<UserModel>)dataContextWrapper.Table<UserModel>()).Count, "The database was not supposed to be modified.");
            Assert.AreEqual(2, ruvm.RegisteredUsers.Count, "Wrong number of contact groups created");

            // Add another user whose first letter is 's'
            UserModel sUser = new UserModel() { Name = "sUser", Id = id++, PhoneNumber = "123 456 789" + id };
            Messenger.Default.Send<NewContactEvent>(new NewContactEvent() { Contact = sUser }, userSettings);

            Assert.AreEqual(3, collectionChanged.Count, "The contact should not be added");
            Assert.AreEqual(3, ((MockTable<UserModel>)dataContextWrapper.Table<UserModel>()).Count, "The database was not supposed to be modified.");
            Assert.AreEqual(3, ruvm.RegisteredUsers.Count, "Wrong number of contact groups created");

            // Add another user whose first letter is 'd'
            UserModel dSecondUser = new UserModel() { Name = "dSecondUser", Id = id++, PhoneNumber = "123 456 789" + id };
            Messenger.Default.Send<NewContactEvent>(new NewContactEvent() { Contact = dSecondUser }, userSettings);

            Assert.AreEqual(4, collectionChanged.Count, "The contact should not be added");
            Assert.AreEqual(4, ((MockTable<UserModel>)dataContextWrapper.Table<UserModel>()).Count, "The database was not supposed to be modified.");
            Assert.AreEqual(3, ruvm.RegisteredUsers.Count, "Wrong number of contact groups created");

            // Add another user whose first letter is 's'.
            UserModel sSecondUser = new UserModel() { Name = "sSecondUser", Id = id++, PhoneNumber = "123 456 789" + id };
            Messenger.Default.Send<NewContactEvent>(new NewContactEvent() { Contact = sSecondUser }, userSettings);

            Assert.AreEqual(5, collectionChanged.Count, "The contact should not be added");
            Assert.AreEqual(5, ((MockTable<UserModel>)dataContextWrapper.Table<UserModel>()).Count, "The database was not supposed to be modified.");
            Assert.AreEqual(3, ruvm.RegisteredUsers.Count, "Wrong number of contact groups created");

            // Add another user whose first letter is 's'. Try different position in
            // the 's' groups sorted list
            UserModel sZUser = new UserModel() { Name = "sZUser", Id = id++, PhoneNumber = "123 456 789" + id };
            Messenger.Default.Send<NewContactEvent>(new NewContactEvent() { Contact = sZUser }, userSettings);

            Assert.AreEqual(6, collectionChanged.Count, "The contact should not be added");
            Assert.AreEqual(6, ((MockTable<UserModel>)dataContextWrapper.Table<UserModel>()).Count, "The database was not supposed to be modified.");
            Assert.AreEqual(3, ruvm.RegisteredUsers.Count, "Wrong number of contact groups created");
        }

        private void LoadUsers()
        {
            //Replace 'MyProject' with the name of your XAP/Project
            Stream txtStream = Application.GetResourceStream(new Uri("Data/users.txt", UriKind.Relative)).Stream;

            using (StreamReader sr = new StreamReader(txtStream))
            {
                StringBuilder usersString = new StringBuilder();
                string nextLine;
                while ((nextLine = sr.ReadLine()) != null)
                {
                    usersString.Append(nextLine);
                }

                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(UserList));
                UserList result = (UserList)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(usersString.ToString())));
                this.users = result.Users;
            }
        }
    }


    [DataContract()]
    public class UserList
    {
        [DataMember]
        public List<UserModel> Users
        {
            get;
            set;
        }
    }
}
