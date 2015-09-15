using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Opten.Umbraco.Merchello.Plugins.Payment.SaferPay
{
	public static partial class Constants
	{
		public static partial class Api
		{
			public static string BaseUrl
			{
				get { return "/umbraco/v1/ResponseApi/"; }
			}

			public const string SuccessMethodName = "GetSuccess";
			public const string FailMethodName = "GetFail";
			public const string BackMethodName = "GetBack";
		}
	}
}
