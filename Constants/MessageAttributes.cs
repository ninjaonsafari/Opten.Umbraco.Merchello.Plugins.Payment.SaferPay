using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Opten.Umbraco.Merchello.Plugins.Payment.SaferPay
{
	public static partial class Constants
	{
		public static partial class MessageAttributes
		{
			public static string AccountId
			{
				get { return "ACCOUNTID"; }
			}

			public static string Id
			{
				get { return "ID"; }
			}

			public static string Token
			{
				get { return "TOKEN"; }
			}

			public static string Amount
			{
				get { return "AMOUNT"; }
			}

			public static string Currency
			{
				get { return "CURRENCY"; }
			}

			public static string Description
			{
				get { return "DESCRIPTION"; }
			}

			public static string OrderId
			{
				get { return "ORDERID"; }
			}

			public static string SuccessLink
			{
				get { return "SUCCESSLINK"; }
			}

			public static string FailLink
			{
				get { return "FAILLINK"; }
			}

			public static string BackLink
			{
				get { return "BACKLINK"; }
			}

			public static string NotifyAddress
			{
				get { return "NOTIFYADDRESS"; }
			}

			public static string LangId
			{
				get { return "LANGID"; }
			}

			public static string ShowLanguages
			{
				get { return "SHOWLANGUAGES"; }
			}

			public static string UserNotify
			{
				get { return "USERNOTIFY"; }
			}

			public static string Company
			{
				get { return "COMPANY"; }
			}

			public static string Firstname
			{
				get { return "FIRSTNAME"; }
			}

			public static string Lastname
			{
				get { return "LASTNAME"; }
			}

			public static string Street
			{
				get { return "STREET"; }
			}

			public static string Zip
			{
				get { return "ZIP"; }
			}

			public static string City
			{
				get { return "CITY"; }
			}

			public static string Country
			{
				get { return "COUNTRY"; }
			}

			public static string EMail
			{
				get { return "EMAIL"; }
			}

			public static string Phone
			{
				get { return "PHONE"; }
			}

			public static string Data
			{
				get { return "DATA"; }
			}

			public static string Signature
			{
				get { return "SIGNATURE"; }
			}

			public static string Invoice
			{
				get { return "inv"; }
			}

			public static string Payment
			{
				get { return "pay"; }
			}
		}
	}
}
