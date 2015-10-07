using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YapperChat.ViewModels;
using YapperTest.Mock;
using YapperChat.Models;
using System.IO;

namespace YapperTest
{
    [TestClass]
    public class AllConversationsViewModelTests
    {
        List<string> conversations;

        public AllConversationsViewModelTests()
        {
            this.LoadConversations();
        }

        [TestMethod]
        public void NewAllConversationsViewModelTest()
        {
            for (int i = 0; i < this.conversations.Count; i++)
            {
                MockServiceProxy serviceProxy = new MockServiceProxy(this.conversations[i]);
                AllConversationsViewModel allConversations = new AllConversationsViewModel(serviceProxy);
                NotifyPropertyChangedTester propertyChangedTester = new NotifyPropertyChangedTester(allConversations);
                allConversations.LoadInitialConversations();

                Assert.AreEqual(1, propertyChangedTester.Changes.Count, "IsLoaded property changed event was not generated");
                propertyChangedTester.AssertChange(0, "IsLoaded");
            }
        }

        private static ICollection<UserModel> GetParticipants()
        {
            throw new NotImplementedException();
        }

        private void LoadConversations()
        {
            string[] conversationFiles = Directory.GetFiles("..\\Data", "conversation.*");

          this.conversations = new List<string>();

            for (int i = 0; i < conversationFiles.Length; i++)
            {
                string[] allLines = File.ReadAllLines(conversationFiles[i]);
                if (allLines != null && allLines.Length != 0)
                {
                    this.conversations.Add(allLines[0]);
                }
                else
                {
                    this.conversations.Add(null);
                }
            }
        }
    }
}
