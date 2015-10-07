using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Configuration;
using System.Web;

namespace YapperWebRole
{
    public class YapperServiceBehaviorExtensionElement : BehaviorExtensionElement
    {
        protected override object CreateBehavior()
        {
            return new YapperWebBehaviour();
        }

        public override Type BehaviorType
        {
            get
            {
                return typeof(YapperWebBehaviour);
            }
        }
    }
}