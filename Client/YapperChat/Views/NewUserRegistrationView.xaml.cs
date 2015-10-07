using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Runtime.Serialization.Json;
using YapperChat.Models;
using System.IO;
using YapperChat.ServiceProxy;
using Microsoft.Phone.Shell;
using YapperChat.ViewModels;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.EventMessages;
using System.Windows.Data;
using PhoneNumbers;
using YapperChat.Resources;
using System.Globalization;
using System.ComponentModel;

namespace YapperChat.Views
{
    public partial class NewUserRegistration : PhoneApplicationPage
    {
        private CollectionViewSource contactsCollection;
        //private static HashSet<string> hs = PhoneNumberUtil.GetInstance().GetSupportedRegions();

        #region country codes
        private static Dictionary<string, string> CountryNameToCode = new Dictionary<string, string>();

        private static Dictionary<string, string> CodeToCountry = new Dictionary<string, string>();
        #endregion
        public NewUserRegistration()
        {
            InitializeComponent();
            this.contactsCollection = new System.Windows.Data.CollectionViewSource();

            this.contactsCollection.Source = ((NewUserRegistrationViewModel)this.DataContext).RegisteredUsers;
            System.ComponentModel.SortDescription contactsSort = new System.ComponentModel.SortDescription("FirstLetter", System.ComponentModel.ListSortDirection.Ascending);
            this.contactsCollection.SortDescriptions.Add(contactsSort);
            BackgroundWorker worker = new BackgroundWorker();
            DispatcherHelper.InvokeOnUiThread(() =>
                {
                    NewUserRegistration.InitializeDictionaries();
                    RegionInfo region = RegionInfo.CurrentRegion;
                    this.CountryCodeListPicker.ItemsSource = NewUserRegistration.CountryNameToCode.Keys.ToArray();
                    if (NewUserRegistration.CountryNameToCode.ContainsKey(region.TwoLetterISORegionName))
                    {
                        this.CountryCodeListPicker.SelectedItem = (string)NewUserRegistration.CountryNameToCode[region.TwoLetterISORegionName];
                    }
                    else
                    {
                        this.CountryCodeListPicker.SelectedItem = (string)NewUserRegistration.CodeToCountry["US"];
                    }
                },
                true);

            worker.RunWorkerAsync();
        }

        private static void InitializeDictionaries()
        {
            #region Country to code
            if (CountryNameToCode.Count == 0)
            {
                CountryNameToCode.Add(Strings.Afghanistan, "AF");
                CountryNameToCode.Add(Strings.Albania, "AL");
                CountryNameToCode.Add(Strings.Algeria, "DZ");
                CountryNameToCode.Add(Strings.AmericanSamoa, "AS");
                CountryNameToCode.Add(Strings.Andorra, "AD");
                CountryNameToCode.Add(Strings.Angola, "AO");
                CountryNameToCode.Add(Strings.Anguilla, "AI");
                CountryNameToCode.Add(Strings.AntiguaAndBarbuda, "AG");
                CountryNameToCode.Add(Strings.Argentina, "AR");
                CountryNameToCode.Add(Strings.Armenia, "AM");
                CountryNameToCode.Add(Strings.Aruba, "AW");
                CountryNameToCode.Add(Strings.Australia, "AU");
                CountryNameToCode.Add(Strings.Austria, "AT");
                CountryNameToCode.Add(Strings.Azerbaijan, "AZ");
                CountryNameToCode.Add(Strings.Bahamas, "BS");
                CountryNameToCode.Add(Strings.Bahrain, "BH");
                CountryNameToCode.Add(Strings.Bangladesh, "BD");
                CountryNameToCode.Add(Strings.Barbados, "BB");
                CountryNameToCode.Add(Strings.Belarus, "BY");
                CountryNameToCode.Add(Strings.Belgium, "BE");
                CountryNameToCode.Add(Strings.Belize, "BZ");
                CountryNameToCode.Add(Strings.Benin, "BJ");
                CountryNameToCode.Add(Strings.Bermuda, "BM");
                CountryNameToCode.Add(Strings.Bhutan, "BT");
                CountryNameToCode.Add(Strings.Bolivia, "BO");
                CountryNameToCode.Add(Strings.BosniaAndHerzegovina, "BA");
                CountryNameToCode.Add(Strings.Botswana, "BW");
                CountryNameToCode.Add(Strings.Brazil, "BR");
                CountryNameToCode.Add(Strings.Brunei, "BN");
                CountryNameToCode.Add(Strings.Bulgaria, "BG");
                CountryNameToCode.Add(Strings.BurkinaFaso, "BF");
                CountryNameToCode.Add(Strings.Burundi, "BI");
                CountryNameToCode.Add(Strings.Cambodia, "KH");
                CountryNameToCode.Add(Strings.Cameroon, "CM");
                CountryNameToCode.Add(Strings.Canada, "CA");
                CountryNameToCode.Add(Strings.CaymanIslands, "KY");
                CountryNameToCode.Add(Strings.CentralAfricanRepublic, "CF");
                CountryNameToCode.Add(Strings.Chad, "TD");
                CountryNameToCode.Add(Strings.Chile, "CL");
                CountryNameToCode.Add(Strings.China, "CN");
                CountryNameToCode.Add(Strings.Colombia, "CO");
                CountryNameToCode.Add(Strings.Comoros, "KM");
                CountryNameToCode.Add(Strings.Congo, "CG");
                CountryNameToCode.Add(Strings.DemocraticRepublicCongo, "CD");
                CountryNameToCode.Add(Strings.CostaRica, "CR");
                CountryNameToCode.Add(Strings.IvoryCoast, "CI");
                CountryNameToCode.Add(Strings.Croatia, "HR");
                CountryNameToCode.Add(Strings.Cuba, "CU");
                CountryNameToCode.Add(Strings.Curacao, "CW");
                CountryNameToCode.Add(Strings.Cyprus, "CY");
                CountryNameToCode.Add(Strings.CzechRepublic, "CZ");
                CountryNameToCode.Add(Strings.Denmark, "DK");
                CountryNameToCode.Add(Strings.Djibouti, "DJ");
                CountryNameToCode.Add(Strings.Dominica, "DM");
                CountryNameToCode.Add(Strings.DominicanRepublic, "DO");
                CountryNameToCode.Add(Strings.Ecuador, "EC");
                CountryNameToCode.Add(Strings.Egypt, "EG");
                CountryNameToCode.Add(Strings.ElSalvador, "SV");
                CountryNameToCode.Add(Strings.EquatorialGuinea, "GQ");
                CountryNameToCode.Add(Strings.Eritrea, "ER");
                CountryNameToCode.Add(Strings.Estonia, "EE");
                CountryNameToCode.Add(Strings.Ethiopia, "ET");
                CountryNameToCode.Add(Strings.Fiji, "FJ");
                CountryNameToCode.Add(Strings.Finland, "FI");
                CountryNameToCode.Add(Strings.France, "FR");
                CountryNameToCode.Add(Strings.FrenchGuiana, "GF");
                CountryNameToCode.Add(Strings.FrenchPolynesia, "PF");
                CountryNameToCode.Add(Strings.Gabon, "GA");
                CountryNameToCode.Add(Strings.Gambia, "GM");
                CountryNameToCode.Add(Strings.Georgia, "GE");
                CountryNameToCode.Add(Strings.Germany, "DE");
                CountryNameToCode.Add(Strings.Ghana, "GH");
                CountryNameToCode.Add(Strings.Gibraltar, "GI");
                CountryNameToCode.Add(Strings.Greece, "GR");
                CountryNameToCode.Add(Strings.Greenland, "GL");
                CountryNameToCode.Add(Strings.Grenada, "GD");
                CountryNameToCode.Add(Strings.Guadeloupe, "GP");
                CountryNameToCode.Add(Strings.Guam, "GU");
                CountryNameToCode.Add(Strings.Guatemala, "GT");
                CountryNameToCode.Add(Strings.Guernsey, "GG");
                CountryNameToCode.Add(Strings.Guinea, "GN");
                CountryNameToCode.Add(Strings.GuineaBissau, "GW");
                CountryNameToCode.Add(Strings.Guyana, "GY");
                CountryNameToCode.Add(Strings.Haiti, "HT");
                CountryNameToCode.Add(Strings.Honduras, "HN");
                CountryNameToCode.Add(Strings.HongKong, "HK");
                CountryNameToCode.Add(Strings.Hungary, "HU");
                CountryNameToCode.Add(Strings.Iceland, "IS");
                CountryNameToCode.Add(Strings.India, "IN");
                CountryNameToCode.Add(Strings.Indonesia, "ID");
                CountryNameToCode.Add(Strings.Iran, "IR");
                CountryNameToCode.Add(Strings.Iraq, "IQ");
                CountryNameToCode.Add(Strings.Ireland, "IE");
                CountryNameToCode.Add(Strings.Israel, "IL");
                CountryNameToCode.Add(Strings.Italy, "IT");
                CountryNameToCode.Add(Strings.Jamaica, "JM");
                CountryNameToCode.Add(Strings.Japan, "JP");
                CountryNameToCode.Add(Strings.Jersey, "JE");
                CountryNameToCode.Add(Strings.Jordan, "JO");
                CountryNameToCode.Add(Strings.Kazakhstan, "KZ");
                CountryNameToCode.Add(Strings.Kenya, "KE");
                CountryNameToCode.Add(Strings.Kiribati, "KI");
                CountryNameToCode.Add(Strings.NorthKorea, "KP");
                CountryNameToCode.Add(Strings.SouthKorea, "KR");
                CountryNameToCode.Add(Strings.Kuwait, "KW");
                CountryNameToCode.Add(Strings.Kyrgyzstan, "KG");
                CountryNameToCode.Add(Strings.Latvia, "LV");
                CountryNameToCode.Add(Strings.Lebanon, "LB");
                CountryNameToCode.Add(Strings.Lesotho, "LS");
                CountryNameToCode.Add(Strings.Liberia, "LR");
                CountryNameToCode.Add(Strings.Libya, "LY");
                CountryNameToCode.Add(Strings.Liechtenstein, "LI");
                CountryNameToCode.Add(Strings.Lithuania, "LT");
                CountryNameToCode.Add(Strings.Luxembourg, "LU");
                CountryNameToCode.Add(Strings.Macao, "MO");
                CountryNameToCode.Add(Strings.Macedonia, "MK");
                CountryNameToCode.Add(Strings.Madagascar, "MG");
                CountryNameToCode.Add(Strings.Malawi, "MW");
                CountryNameToCode.Add(Strings.Malaysia, "MY");
                CountryNameToCode.Add(Strings.Maldives, "MV");
                CountryNameToCode.Add(Strings.Mali, "ML");
                CountryNameToCode.Add(Strings.Malta, "MT");
                CountryNameToCode.Add(Strings.Martinique, "MQ");
                CountryNameToCode.Add(Strings.Mauritania, "MR");
                CountryNameToCode.Add(Strings.Mauritius, "MU");
                CountryNameToCode.Add(Strings.Mayotte, "YT");
                CountryNameToCode.Add(Strings.Mexico, "MX");
                CountryNameToCode.Add(Strings.Moldova, "MD");
                CountryNameToCode.Add(Strings.Monaco, "MC");
                CountryNameToCode.Add(Strings.Mongolia, "MN");
                CountryNameToCode.Add(Strings.Montenegro, "ME");
                CountryNameToCode.Add(Strings.Montserrat, "MS");
                CountryNameToCode.Add(Strings.Morocco, "MA");
                CountryNameToCode.Add(Strings.Mozambique, "MZ");
                CountryNameToCode.Add(Strings.Myanmar, "MM");
                CountryNameToCode.Add(Strings.Namibia, "NA");
                CountryNameToCode.Add(Strings.Nauru, "NR");
                CountryNameToCode.Add(Strings.Nepal, "NP");
                CountryNameToCode.Add(Strings.Netherlands, "NL");
                CountryNameToCode.Add(Strings.NewCaledonia, "NC");
                CountryNameToCode.Add(Strings.NewZealand, "NZ");
                CountryNameToCode.Add(Strings.Nicaragua, "NI");
                CountryNameToCode.Add(Strings.Niger, "NE");
                CountryNameToCode.Add(Strings.Nigeria, "NG");
                CountryNameToCode.Add(Strings.Niue, "NU");
                CountryNameToCode.Add(Strings.Norway, "NO");
                CountryNameToCode.Add(Strings.Oman, "OM");
                CountryNameToCode.Add(Strings.Pakistan, "PK");
                CountryNameToCode.Add(Strings.Palau, "PW");
                CountryNameToCode.Add(Strings.Palestine, "PS");
                CountryNameToCode.Add(Strings.Panama, "PA");
                CountryNameToCode.Add(Strings.PapuaNewGuinea, "PG");
                CountryNameToCode.Add(Strings.Paraguay, "PY");
                CountryNameToCode.Add(Strings.Peru, "PE");
                CountryNameToCode.Add(Strings.Philippines, "PH");
                CountryNameToCode.Add(Strings.Pitcairn, "PN");
                CountryNameToCode.Add(Strings.Poland, "PL");
                CountryNameToCode.Add(Strings.Portugal, "PT");
                CountryNameToCode.Add(Strings.PuertoRico, "PR");
                CountryNameToCode.Add(Strings.Qatar, "QA");
                CountryNameToCode.Add(Strings.Romania, "RO");
                CountryNameToCode.Add(Strings.Russia, "RU");
                CountryNameToCode.Add(Strings.Rwanda, "RW");
                CountryNameToCode.Add(Strings.Samoa, "WS");
                CountryNameToCode.Add(Strings.SanMarino, "SM");
                CountryNameToCode.Add(Strings.SaoTomeAndPrincipe, "ST");
                CountryNameToCode.Add(Strings.SaudiArabia, "SA");
                CountryNameToCode.Add(Strings.Senegal, "SN");
                CountryNameToCode.Add(Strings.Serbia, "RS");
                CountryNameToCode.Add(Strings.Seychelles, "SC");
                CountryNameToCode.Add(Strings.SierraLeone, "SL");
                CountryNameToCode.Add(Strings.Singapore, "SG");
                CountryNameToCode.Add(Strings.Slovakia, "SK");
                CountryNameToCode.Add(Strings.Slovenia, "SI");
                CountryNameToCode.Add(Strings.Somalia, "SO");
                CountryNameToCode.Add(Strings.SouthAfrica, "ZA");
                CountryNameToCode.Add(Strings.SouthSudan, "SS");
                CountryNameToCode.Add(Strings.Spain, "ES");
                CountryNameToCode.Add(Strings.SriLanka, "LK");
                CountryNameToCode.Add(Strings.Sudan, "SD");
                CountryNameToCode.Add(Strings.Suriname, "SR");
                CountryNameToCode.Add(Strings.Swaziland, "SZ");
                CountryNameToCode.Add(Strings.Sweden, "SE");
                CountryNameToCode.Add(Strings.Switzerland, "CH");
                CountryNameToCode.Add(Strings.Syria, "SY");
                CountryNameToCode.Add(Strings.Taiwan, "TW");
                CountryNameToCode.Add(Strings.Tajikistan, "TJ");
                CountryNameToCode.Add(Strings.Tanzania, "TZ");
                CountryNameToCode.Add(Strings.Thailand, "TH");
                CountryNameToCode.Add(Strings.TimorLeste, "TL");
                CountryNameToCode.Add(Strings.Togo, "TG");
                CountryNameToCode.Add(Strings.Tokelau, "TK");
                CountryNameToCode.Add(Strings.Tonga, "TO");
                CountryNameToCode.Add(Strings.TrinidadAndTobago, "TT");
                CountryNameToCode.Add(Strings.Tunisia, "TN");
                CountryNameToCode.Add(Strings.Turkey, "TR");
                CountryNameToCode.Add(Strings.Turkmenistan, "TM");
                CountryNameToCode.Add(Strings.Tuvalu, "TV");
                CountryNameToCode.Add(Strings.Uganda, "UG");
                CountryNameToCode.Add(Strings.Ukraine, "UA");
                CountryNameToCode.Add(Strings.UnitedArabEmirates, "AE");
                CountryNameToCode.Add(Strings.UnitedKingdom, "GB");
                CountryNameToCode.Add(Strings.UnitedStates, "US");
                CountryNameToCode.Add(Strings.Uruguay, "UY");
                CountryNameToCode.Add(Strings.Uzbekistan, "UZ");
                CountryNameToCode.Add(Strings.Vanuatu, "VU");
                CountryNameToCode.Add(Strings.Venezuela, "VE");
                CountryNameToCode.Add(Strings.Vietnam, "VN");
                CountryNameToCode.Add(Strings.Yemen, "YE");
                CountryNameToCode.Add(Strings.Zambia, "ZM");
                CountryNameToCode.Add(Strings.Zimbabwe, "ZW");
            }
            #endregion

            #region code to country
            if (CodeToCountry.Count == 0)
            {
                CodeToCountry.Add("AF", Strings.Afghanistan);
                CodeToCountry.Add("AL", Strings.Albania);
                CodeToCountry.Add("DZ", Strings.Algeria);
                CodeToCountry.Add("AS", Strings.AmericanSamoa);
                CodeToCountry.Add("AD", Strings.Andorra);
                CodeToCountry.Add("AO", Strings.Angola);
                CodeToCountry.Add("AI", Strings.Anguilla);
                CodeToCountry.Add("AG", Strings.AntiguaAndBarbuda);
                CodeToCountry.Add("AR", Strings.Argentina);
                CodeToCountry.Add("AM", Strings.Armenia);
                CodeToCountry.Add("AW", Strings.Aruba);
                CodeToCountry.Add("AU", Strings.Australia);
                CodeToCountry.Add("AT", Strings.Austria);
                CodeToCountry.Add("AZ", Strings.Azerbaijan);
                CodeToCountry.Add("BS", Strings.Bahamas);
                CodeToCountry.Add("BH", Strings.Bahrain);
                CodeToCountry.Add("BD", Strings.Bangladesh);
                CodeToCountry.Add("BB", Strings.Barbados);
                CodeToCountry.Add("BY", Strings.Belarus);
                CodeToCountry.Add("BE", Strings.Belgium);
                CodeToCountry.Add("BZ", Strings.Belize);
                CodeToCountry.Add("BJ", Strings.Benin);
                CodeToCountry.Add("BM", Strings.Bermuda);
                CodeToCountry.Add("BT", Strings.Bhutan);
                CodeToCountry.Add("BO", Strings.Bolivia);
                CodeToCountry.Add("BA", Strings.BosniaAndHerzegovina);
                CodeToCountry.Add("BW", Strings.Botswana);
                CodeToCountry.Add("BR", Strings.Brazil);
                CodeToCountry.Add("BN", Strings.Brunei);
                CodeToCountry.Add("BG", Strings.Bulgaria);
                CodeToCountry.Add("BF", Strings.BurkinaFaso);
                CodeToCountry.Add("BI", Strings.Burundi);
                CodeToCountry.Add("KH", Strings.Cambodia);
                CodeToCountry.Add("CM", Strings.Cameroon);
                CodeToCountry.Add("CA", Strings.Canada);
                CodeToCountry.Add("KY", Strings.CaymanIslands);
                CodeToCountry.Add("CF", Strings.CentralAfricanRepublic);
                CodeToCountry.Add("TD", Strings.Chad);
                CodeToCountry.Add("CL", Strings.Chile);
                CodeToCountry.Add("CN", Strings.China);
                CodeToCountry.Add("CO", Strings.Colombia);
                CodeToCountry.Add("KM", Strings.Comoros);
                CodeToCountry.Add("CG", Strings.Congo);
                CodeToCountry.Add("CD", Strings.DemocraticRepublicCongo);
                CodeToCountry.Add("CR", Strings.CostaRica);
                CodeToCountry.Add("CI", Strings.IvoryCoast);
                CodeToCountry.Add("HR", Strings.Croatia);
                CodeToCountry.Add("CU", Strings.Cuba);
                CodeToCountry.Add("CW", Strings.Curacao);
                CodeToCountry.Add("CY", Strings.Cyprus);
                CodeToCountry.Add("CZ", Strings.CzechRepublic);
                CodeToCountry.Add("DK", Strings.Denmark);
                CodeToCountry.Add("DJ", Strings.Djibouti);
                CodeToCountry.Add("DM", Strings.Dominica);
                CodeToCountry.Add("DO", Strings.DominicanRepublic);
                CodeToCountry.Add("EC", Strings.Ecuador);
                CodeToCountry.Add("EG", Strings.Egypt);
                CodeToCountry.Add("SV", Strings.ElSalvador);
                CodeToCountry.Add("GQ", Strings.EquatorialGuinea);
                CodeToCountry.Add("ER", Strings.Eritrea);
                CodeToCountry.Add("EE", Strings.Estonia);
                CodeToCountry.Add("ET", Strings.Ethiopia);
                CodeToCountry.Add("FJ", Strings.Fiji);
                CodeToCountry.Add("FI", Strings.Finland);
                CodeToCountry.Add("FR", Strings.France);
                CodeToCountry.Add("GF", Strings.FrenchGuiana);
                CodeToCountry.Add("PF", Strings.FrenchPolynesia);
                CodeToCountry.Add("GA", Strings.Gabon);
                CodeToCountry.Add("GM", Strings.Gambia);
                CodeToCountry.Add("GE", Strings.Georgia);
                CodeToCountry.Add("DE", Strings.Germany);
                CodeToCountry.Add("GH", Strings.Ghana);
                CodeToCountry.Add("GI", Strings.Gibraltar);
                CodeToCountry.Add("GR", Strings.Greece);
                CodeToCountry.Add("GL", Strings.Greenland);
                CodeToCountry.Add("GD", Strings.Grenada);
                CodeToCountry.Add("GP", Strings.Guadeloupe);
                CodeToCountry.Add("GU", Strings.Guam);
                CodeToCountry.Add("GT", Strings.Guatemala);
                CodeToCountry.Add("GG", Strings.Guernsey);
                CodeToCountry.Add("GN", Strings.Guinea);
                CodeToCountry.Add("GW", Strings.GuineaBissau);
                CodeToCountry.Add("GY", Strings.Guyana);
                CodeToCountry.Add("HT", Strings.Haiti);
                CodeToCountry.Add("HN", Strings.Honduras);
                CodeToCountry.Add("HK", Strings.HongKong);
                CodeToCountry.Add("HU", Strings.Hungary);
                CodeToCountry.Add("IS", Strings.Iceland);
                CodeToCountry.Add("IN", Strings.India);
                CodeToCountry.Add("ID", Strings.Indonesia);
                CodeToCountry.Add("IR", Strings.Iran);
                CodeToCountry.Add("IQ", Strings.Iraq);
                CodeToCountry.Add("IE", Strings.Ireland);
                CodeToCountry.Add("IL", Strings.Israel);
                CodeToCountry.Add("IT", Strings.Italy);
                CodeToCountry.Add("JM", Strings.Jamaica);
                CodeToCountry.Add("JP", Strings.Japan);
                CodeToCountry.Add("JE", Strings.Jersey);
                CodeToCountry.Add("JO", Strings.Jordan);
                CodeToCountry.Add("KZ", Strings.Kazakhstan);
                CodeToCountry.Add("KE", Strings.Kenya);
                CodeToCountry.Add("KI", Strings.Kiribati);
                CodeToCountry.Add("KP", Strings.NorthKorea);
                CodeToCountry.Add("KR", Strings.SouthKorea);
                CodeToCountry.Add("KW", Strings.Kuwait);
                CodeToCountry.Add("KG", Strings.Kyrgyzstan);
                CodeToCountry.Add("LV", Strings.Latvia);
                CodeToCountry.Add("LB", Strings.Lebanon);
                CodeToCountry.Add("LS", Strings.Lesotho);
                CodeToCountry.Add("LR", Strings.Liberia);
                CodeToCountry.Add("LY", Strings.Libya);
                CodeToCountry.Add("LI", Strings.Liechtenstein);
                CodeToCountry.Add("LT", Strings.Lithuania);
                CodeToCountry.Add("LU", Strings.Luxembourg);
                CodeToCountry.Add("MO", Strings.Macao);
                CodeToCountry.Add("MK", Strings.Macedonia);
                CodeToCountry.Add("MG", Strings.Madagascar);
                CodeToCountry.Add("MW", Strings.Malawi);
                CodeToCountry.Add("MY", Strings.Malaysia);
                CodeToCountry.Add("MV", Strings.Maldives);
                CodeToCountry.Add("ML", Strings.Mali);
                CodeToCountry.Add("MT", Strings.Malta);
                CodeToCountry.Add("MQ", Strings.Martinique);
                CodeToCountry.Add("MR", Strings.Mauritania);
                CodeToCountry.Add("MU", Strings.Mauritius);
                CodeToCountry.Add("YT", Strings.Mayotte);
                CodeToCountry.Add("MX", Strings.Mexico);
                CodeToCountry.Add("MD", Strings.Moldova);
                CodeToCountry.Add("MC", Strings.Monaco);
                CodeToCountry.Add("MN", Strings.Mongolia);
                CodeToCountry.Add("ME", Strings.Montenegro);
                CodeToCountry.Add("MS", Strings.Montserrat);
                CodeToCountry.Add("MA", Strings.Morocco);
                CodeToCountry.Add("MZ", Strings.Mozambique);
                CodeToCountry.Add("MM", Strings.Myanmar);
                CodeToCountry.Add("NA", Strings.Namibia);
                CodeToCountry.Add("NR", Strings.Nauru);
                CodeToCountry.Add("NP", Strings.Nepal);
                CodeToCountry.Add("NL", Strings.Netherlands);
                CodeToCountry.Add("NC", Strings.NewCaledonia);
                CodeToCountry.Add("NZ", Strings.NewZealand);
                CodeToCountry.Add("NI", Strings.Nicaragua);
                CodeToCountry.Add("NE", Strings.Niger);
                CodeToCountry.Add("NG", Strings.Nigeria);
                CodeToCountry.Add("NU", Strings.Niue);
                CodeToCountry.Add("NO", Strings.Norway);
                CodeToCountry.Add("OM", Strings.Oman);
                CodeToCountry.Add("PK", Strings.Pakistan);
                CodeToCountry.Add("PW", Strings.Palau);
                CodeToCountry.Add("PS", Strings.Palestine);
                CodeToCountry.Add("PA", Strings.Panama);
                CodeToCountry.Add("PG", Strings.PapuaNewGuinea);
                CodeToCountry.Add("PY", Strings.Paraguay);
                CodeToCountry.Add("PE", Strings.Peru);
                CodeToCountry.Add("PH", Strings.Philippines);
                CodeToCountry.Add("PN", Strings.Pitcairn);
                CodeToCountry.Add("PL", Strings.Poland);
                CodeToCountry.Add("PT", Strings.Portugal);
                CodeToCountry.Add("PR", Strings.PuertoRico);
                CodeToCountry.Add("QA", Strings.Qatar);
                CodeToCountry.Add("RO", Strings.Romania);
                CodeToCountry.Add("RU", Strings.Russia);
                CodeToCountry.Add("RW", Strings.Rwanda);
                CodeToCountry.Add("WS", Strings.Samoa);
                CodeToCountry.Add("SM", Strings.SanMarino);
                CodeToCountry.Add("ST", Strings.SaoTomeAndPrincipe);
                CodeToCountry.Add("SA", Strings.SaudiArabia);
                CodeToCountry.Add("SN", Strings.Senegal);
                CodeToCountry.Add("RS", Strings.Serbia);
                CodeToCountry.Add("SC", Strings.Seychelles);
                CodeToCountry.Add("SL", Strings.SierraLeone);
                CodeToCountry.Add("SG", Strings.Singapore);
                CodeToCountry.Add("SK", Strings.Slovakia);
                CodeToCountry.Add("SI", Strings.Slovenia);
                CodeToCountry.Add("SO", Strings.Somalia);
                CodeToCountry.Add("ZA", Strings.SouthAfrica);
                CodeToCountry.Add("SS", Strings.SouthSudan);
                CodeToCountry.Add("ES", Strings.Spain);
                CodeToCountry.Add("LK", Strings.SriLanka);
                CodeToCountry.Add("SD", Strings.Sudan);
                CodeToCountry.Add("SR", Strings.Suriname);
                CodeToCountry.Add("SZ", Strings.Swaziland);
                CodeToCountry.Add("SE", Strings.Sweden);
                CodeToCountry.Add("CH", Strings.Switzerland);
                CodeToCountry.Add("SY", Strings.Syria);
                CodeToCountry.Add("TW", Strings.Taiwan);
                CodeToCountry.Add("TJ", Strings.Tajikistan);
                CodeToCountry.Add("TZ", Strings.Tanzania);
                CodeToCountry.Add("TH", Strings.Thailand);
                CodeToCountry.Add("TL", Strings.TimorLeste);
                CodeToCountry.Add("TG", Strings.Togo);
                CodeToCountry.Add("TK", Strings.Tokelau);
                CodeToCountry.Add("TO", Strings.Tonga);
                CodeToCountry.Add("TT", Strings.TrinidadAndTobago);
                CodeToCountry.Add("TN", Strings.Tunisia);
                CodeToCountry.Add("TR", Strings.Turkey);
                CodeToCountry.Add("TM", Strings.Turkmenistan);
                CodeToCountry.Add("TV", Strings.Tuvalu);
                CodeToCountry.Add("UG", Strings.Uganda);
                CodeToCountry.Add("UA", Strings.Ukraine);
                CodeToCountry.Add("AE", Strings.UnitedArabEmirates);
                CodeToCountry.Add("GB", Strings.UnitedKingdom);
                CodeToCountry.Add("US", Strings.UnitedStates);
                CodeToCountry.Add("UY", Strings.Uruguay);
                CodeToCountry.Add("UZ", Strings.Uzbekistan);
                CodeToCountry.Add("VU", Strings.Vanuatu);
                CodeToCountry.Add("VE", Strings.Venezuela);
                CodeToCountry.Add("VN", Strings.Vietnam);
                CodeToCountry.Add("YE", Strings.Yemen);
                CodeToCountry.Add("ZM", Strings.Zambia);
                CodeToCountry.Add("ZW", Strings.Zimbabwe);
            }

            #endregion
        }

        private void YapperSignupButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty((string)NameTextBox.Text))
            {
                MessageBox.Show(Strings.EnterValidNameText);
                return;
            }

            if (YapperSignupButton.Content != null && !string.IsNullOrEmpty((string)YapperSignupButton.Content))
            {
                try
                {
                    string countryCode = NewUserRegistration.CountryNameToCode[(string)CountryCodeListPicker.SelectedItem];
                    PhoneNumberUtil pn = PhoneNumberUtil.GetInstance();
                    PhoneNumber phone = pn.Parse((string)PhoneNumberTextBox.Text, countryCode);
                    if (pn.IsValidNumber(phone))
                    {
                        ((NewUserRegistrationViewModel)this.DataContext).PhoneNumberEntered = true;
                        PhoneNumberTextBox.Text = PhoneNumberUtil.GetInstance().Format(phone, PhoneNumberFormat.INTERNATIONAL);
                    }
                    else
                    {
                        MessageBox.Show(Strings.InvalidPhoneNumberText);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(Strings.InvalidPhoneNumberText);
                }
            }
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            while (NavigationService.CanGoBack)
            {
                NavigationService.RemoveBackEntry();
            }
        }

        public void RegistrationComplete(RegistrationCompleteEvent register)
        {

            if (register.User != null)
            {
                NavigationService.Navigate(new Uri("/Views/TutorialPageWelcome.xaml", UriKind.Relative));
            }
            else
            {
                NavigationService.Navigate(new Uri("/Views/EnterConfirmationCodeView.xaml", UriKind.Relative));
            }
        }

        private void MessagePrompt_Completed(object sender, Coding4Fun.Toolkit.Controls.PopUpEventArgs<string, Coding4Fun.Toolkit.Controls.PopUpResult> e)
        {
            if (e.PopUpResult == Coding4Fun.Toolkit.Controls.PopUpResult.Ok)
            {
                ((NewUserRegistrationViewModel)this.DataContext).Register(this.PhoneNumberTextBox.Text, this.NameTextBox.Text);
                Messenger.Default.Register<RegistrationCompleteEvent>(this, this.RegistrationComplete);
            }
            else
            {
                ((NewUserRegistrationViewModel)this.DataContext).PhoneNumberEntered = false;
            }                                                                                                                                   
        }
    }
}