using System;
using System.Collections.Generic;
using YapperChat.Models;
using System.Text;

namespace YapperUnitTest.Mock
{
    public class MockContactSearchController : IContactSearchController
    {
        public List<UserModel> Users
        {
            get;
            set;
        }

        public List<string> PhoneNumbers
        {
            get
            {
                List<string> phones = new List<string>();
                if (this.Users != null)
                {
                    foreach (UserModel user in this.Users)
                    {
                        phones.Add(user.PhoneNumber);
                    }
                }

                return phones;
            }
        }

        public void StartSearch(ContactSearchArguments search)
        {
            YapperContactsSearchEventArgs args = new YapperContactsSearchEventArgs();
            args.Filter = search.Filter;
            args.FilterKind = search.FilterKind;
            args.State = search.State;

            if (search.SearchKind == SearchKind.AllPhoneNumbers)
            {
                args.Results = this.PhoneNumbers;
                search.SearchCompleted(this, args);
            }
        }
    }
}
