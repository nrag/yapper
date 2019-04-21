using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using RestSharp.Contrib;

namespace Authenticator
{
    class NexmoAPI
    {

        public NexmoResponse SendSMS(string to, string text)
        {
            var wc = new WebClient() { BaseAddress = "http://rest.nexmo.com/sms/json" };
            wc.QueryString.Add("username", HttpUtility.UrlEncode(NexmoAPI.Username));
            wc.QueryString.Add("password", HttpUtility.UrlEncode(NexmoAPI.Password));
            wc.QueryString.Add("from", HttpUtility.UrlEncode(NexmoAPI.Sender));
            wc.QueryString.Add("to", HttpUtility.UrlEncode(to));
            wc.QueryString.Add("text", HttpUtility.UrlEncode(text));
            return ParseSmsResponseJson(wc.DownloadString(""));
        }

        private NexmoResponse ParseSmsResponseJson(string json)
        {
            json = json.Replace("-", "");
            return (NexmoResponse)(new DataContractJsonSerializer(typeof(NexmoResponse)).ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json))));
        }
    }

    public class NexmoResponse
    {
        public string Messagecount { get; set; }
        public List<NexmoMessageStatus> Messages { get; set; }
    }

    public class NexmoMessageStatus
    {
        public string MessageId { get; set; }
        public string To { get; set; }
        public string clientRef;
        public string Status { get; set; }
        public string ErrorText { get; set; }
        public string RemainingBalance { get; set; }
        public string MessagePrice { get; set; }
        public string Network;
    }
}
