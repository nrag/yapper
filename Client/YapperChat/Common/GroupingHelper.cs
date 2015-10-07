using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YapperChat.Common;
using YapperChat.Models;

namespace YapperChat.Common
{
    class GroupingHelper
    {
        public static ObservableSortedList<ContactGroup<UserModel>> GroupUsers(IList<UserModel> users)
        {
            ObservableSortedList<ContactGroup<UserModel>> groups = new ObservableSortedList<ContactGroup<UserModel>>();
            var groupsDict = new Dictionary<char, ContactGroup<UserModel>>();

            foreach (UserModel user in users)
            {

                if (user.UserType == UserType.Group)
                {
                    if (!user.Name.Contains("(G)"))
                    {
                        user.Name += " (G)";
                    }
                }

                char firstLetter = char.ToLower(user.Name[0]);

                // show # for numbers
                if (firstLetter >= '0' && firstLetter <= '9')
                {
                    firstLetter = '#';
                }

                // create group for letter if it doesn't exist
                if (!groupsDict.ContainsKey(firstLetter))
                {
                    var group = new ContactGroup<UserModel>(firstLetter);
                    groups.Add(group);
                    groupsDict[firstLetter] = group;
                }

                // create a contact for item and add it to the relevant 
                groupsDict[firstLetter].Add(user);
            }

            return groups;
        }
    }
}
