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
using System.Collections.Generic;
using System.Collections;
using YapperChat.Models;

namespace YapperUnitTest.Mock
{
    /// <summary>
    /// Abstract Template class that represents our in memory database. We can create different implementations of this class that contain different
    /// tables and data.
    /// </summary>
    public class MockDatabase
    {
        public MockDatabase()
        {
            InitializeDataBase();
        }

        public List<MessageModel> Messages
        {
            get
            {
                return (List<MessageModel>)this.Tables[typeof(MessageModel)];
            }

            set
            {
                List<MessageModel> messageModel = (List<MessageModel>)this.Tables[typeof(MessageModel)];
                List<UserModel> users = (List<UserModel>)this.Tables[typeof(UserModel)];
                for (int i = 0; i < value.Count; i++)
                {
                    if (!users.Contains(value[i].Sender))
                    {
                        users.Add(value[i].Sender);
                    }

                    if (!users.Contains(value[i].Recipient))
                    {
                        users.Add(value[i].Recipient);
                    }

                    messageModel.Add(value[i]);
                }
            }
        }

        public List<UserModel> Users
        {
            get
            {
                return (List<UserModel>)this.Tables[typeof(UserModel)];
            }

            set
            {
                List<UserModel> currentUsers = (List<UserModel>)this.Tables[typeof(UserModel)];

                for (int i = 0; i < value.Count; i++)
                {
                    currentUsers.Add(value[i]);
                }
            }
        }

        public Dictionary<Type, IList> Tables 
        { 
            get; 
            set; 
        }

        private void InitializeDataBase()
        {
            Tables = new Dictionary<Type, IList>();
            CreateTables();
            PopulateTables();
        }

        protected void CreateTables()
        {
            this.Tables.Add(typeof(MessageModel), new List<MessageModel>());
            this.Tables.Add(typeof(UserModel), new List<UserModel>());
        }

        protected void PopulateTables()
        {
        }

        protected void AddTable<T>()
        {
            var table = new List<T>();
            Tables.Add(typeof(T), table);
        }

        protected List<T> GetTable<T>()
        {
            return (List<T>)Tables[typeof(T)];
        }
    }
}
