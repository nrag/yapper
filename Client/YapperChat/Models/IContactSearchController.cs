using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YapperChat.Models
{
    public interface IContactSearchController
    {
        void StartSearch(ContactSearchArguments search);
    }
}
