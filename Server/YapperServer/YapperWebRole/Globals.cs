using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace YapperWebRole
{
    public class Globals
    {
        public static string FacebookAuthUrl = "https://graph.facebook.com/me?fields=id,name&access_token={0}";

        /// <summary>
        /// 
        /// </summary>
        public static string AuthTokenCookie = "AuthTokenCookie";

        /// <summary>
        /// 
        /// </summary>
        public static string SqlConnectionString = "";
    }
}
