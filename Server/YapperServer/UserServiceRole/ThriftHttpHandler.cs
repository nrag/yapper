using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thrift.Protocol;
using Thrift.Transport;

namespace UserServiceRole
{
    public class ThriftHttpHandler : THttpHandler
    {
        public ThriftHttpHandler()
            : base(CreateProcessor(), CreateJsonFactory())
        {
        }

        private static UserService.Processor CreateProcessor()
        {
            return new UserService.Processor(new UserServiceImplementation());
        }

        private static TJSONProtocol.Factory CreateJsonFactory()
        {
            return new TJSONProtocol.Factory();
        }
    }
}
