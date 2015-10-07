using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageStore.Database
{
    interface IDatabaseConnectionFactory
    {
        IDatabaseConnection CreateDatabaseConnection();
    }
}
