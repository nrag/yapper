using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using Twilio;

namespace Authenticator
{
    public class SmsSender
    {
        private static string SID = "ACae58b3128e48c0cb138780f27e218dc7";

        private static string AuthToken = "50778b46ffea7bda3c432a831d55e31d";

        private static Dictionary<string, string> SmsMessageFormat = new Dictionary<string, string>()
        {
                {"AF", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"AL", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"DZ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"AS", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"AD", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"AO", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"AI", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"AG", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"AR", "Código de confirmación ordinaria es {0}. Gracias por usar ordinaria. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"AM", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"AW", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"AU", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"AT", "Stimmt Bestätigungscode ist {0}. Danke, dass du mit Klappe. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"AZ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BS", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BH", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BD", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BB", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BY", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BE", "Code de confirmation Yapper is {0}. Nous vous remercions d'utiliser Yapper. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BZ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BJ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BM", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BT", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BO", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BA", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BW", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BR", "Código de confirmação tagarelas é 111111. Obrigado por usar o tagarela. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BN", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BG", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BF", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"BI", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"KH", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CM", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CA", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"KY", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CF", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"TD", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CL", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CN", "Yapper 确认代码是 {0}。谢谢你使用 Yapper。Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CO", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"KM", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CG", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CD", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CR", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CI", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"HR", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CU", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CW", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CY", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CZ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"DK", "Yapper bekræftelseskoden er {0}. Tak for at bruge Yapper. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"DJ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"DM", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"DO", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"EC", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"EG", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SV", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GQ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"ER", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"EE", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"ET", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"FJ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"FI", "Yapper bekräftelsekoden är {0}. Tack för att använda Yapper. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"FR", "Code de confirmation Yapper is {0}. Nous vous remercions d'utiliser Yapper. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GF", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"PF", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GA", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GM", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GE", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"DE", "Stimmt Bestätigungscode ist {0}. Danke, dass du mit Klappe. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GH", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GI", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GR", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GL", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GD", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GP", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GU", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GT", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GG", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GN", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GW", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GY", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"HT", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"HN", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"HK", "Yapper 確認代碼是 {0}。謝謝你使用 Yapper。Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"HU", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"IS", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"IN", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"ID", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"IR", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"IQ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"IE", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"IL", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"IT", "Il codice di conferma Yapper è {0}. Grazie per usare Yapper. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"JM", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"JP", "Yapper 確認コードは {0} 件です。Yapper を使用していただきありがとうございます。Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"JE", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"JO", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"KZ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"KE", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"KI", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"KP", "Yapper 확인 코드는 {0}입니다. Yapper를 사용 하 여 주셔서 감사. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"KR", "Yapper 확인 코드는 {0}입니다. Yapper를 사용 하 여 주셔서 감사. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"KW", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"KG", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"LV", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"LB", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"LS", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"LR", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"LY", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"LI", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"LT", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"LU", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MO", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MK", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MG", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MW", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MY", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MV", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"ML", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MT", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MQ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MR", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MU", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"YT", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MX", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MD", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MC", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MN", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"ME", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MS", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MA", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MZ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"MM", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"NA", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"NR", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"NP", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"NL", "Yapper bevestigingscode is {0}. Bedankt voor het gebruik van Yapper. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"NC", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"NZ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"NI", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"NE", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"NG", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"NU", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"NO", "Yapper bekreftelseskoden er {0}. Takk for bruk av Yapper. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"OM", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"PK", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"PW", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"PS", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"PA", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"PG", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"PY", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"PE", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"PH", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"PN", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"PL", "Yapper kod potwierdzający to {0}. Dzięki za Yapper. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"PT", "Código de confirmação tagarelas é {0}. Obrigado por usar o tagarela. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"PR", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"QA", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"RO", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"RU", "Код подтверждения yapper — {0}. Спасибо за использование Yapper. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"RW", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"WS", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SM", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"ST", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SA", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SN", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"RS", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SC", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SL", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SG", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SK", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SI", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SO", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"ZA", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SS", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"ES", "Código de confirmación ordinaria es {0}. Gracias por usar ordinaria. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"LK", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SD", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SR", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SZ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SE", "Yapper bekräftelsekoden är {0}. Tack för att använda Yapper. Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"CH", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"SY", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"TW", "Yapper 確認代碼是 {0}。謝謝你使用 Yapper。Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"TJ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"TZ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"TH", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"TL", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"TG", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"TK", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"TO", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"TT", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"TN", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"TR", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"TM", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"TV", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"UG", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"UA", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"AE", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"GB", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"US", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"UY", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"UZ", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"VU", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"VE", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"VN", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"YE", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"ZM", "Yapper confirmation code is {0}. Thanks for using Yapper."},
                {"ZW", "Yapper confirmation code is {0}. Thanks for using Yapper."},

        };

        private static string DefaultSmsMessageFormat = "Yapper confirmation code is {0}. Thanks for using Yapper.";

        private static string SmsFrom = "Yapper";

        //public void SendSMS(string number, int code)
        //{
        //    string message = string.Format(SmsSender.SmsMessageFormat, code.ToString("D6"));
        //    var twilio = new TwilioRestClient(SmsSender.SID, SmsSender.AuthToken);
        //    SMSMessage sms = twilio.SendSmsMessage("(253) 218-3535", number, message);
        //}

        public void SendSMS(string number, int code)
        {
            PhoneNumbers.PhoneNumber phone = PhoneNumbers.PhoneNumberUtil.GetInstance().Parse(number, "US");
            string numericPhone = PhoneNumbers.PhoneNumberUtil.GetInstance().Format(phone, PhoneNumbers.PhoneNumberFormat.E164);
            numericPhone.TrimStart('+');

            // Try to send the message in the local language and English
            string regionCode = PhoneNumbers.PhoneNumberUtil.GetInstance().GetRegionCodeForNumber(phone);
            string messageFormat = SmsSender.SmsMessageFormat.ContainsKey(regionCode) ? SmsSender.SmsMessageFormat[regionCode] : SmsSender.DefaultSmsMessageFormat;
            string message = string.Format(messageFormat, code.ToString("D6"));

            var nexmo = new NexmoAPI();
            NexmoResponse response = nexmo.SendSMS(numericPhone, message);
        }

        private string GetPhoneNumberWithJustNumbers(string number)
        {
            PhoneNumbers.PhoneNumber phone = PhoneNumbers.PhoneNumberUtil.GetInstance().Parse(number, "US");

            if (!PhoneNumbers.PhoneNumberUtil.GetInstance().IsValidNumber(phone))
            {
                return null;
            }

            return PhoneNumbers.PhoneNumberUtil.GetInstance().Format(phone, PhoneNumbers.PhoneNumberFormat.E164);
        }
    }
}
