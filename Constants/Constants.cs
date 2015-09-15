using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Opten.Umbraco.Merchello.Plugins.Payment.SaferPay
{
	public static partial class Constants
	{
		public static string Config
		{
			get { return @"C:\Program Files\Saferpay\Client\"; }
		}

		public static string PayCompleteKey
		{
			get { return "PayComplete"; }
		}

		public const string Name = "SaferPay";
		public const string Key = "da27fd6f-a314-47dc-8bed-c28f011c9855";
		public const string Description = "SaferPay Payment Provider";
	}
}
