using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Opten.Umbraco.Merchello.Web.Gateways.Payment.SaferPay.Helper
{
	public static class ParseHelper
	{
		public static T ParseXML<T>(this string xml) where T : class
		{
			Type type = typeof(T);
			T returnedXmlClass = (T)Activator.CreateInstance(type);

			xml = xml.Replace("<IDP ", string.Empty);
			xml = xml.Replace(" />", string.Empty);
			foreach (string item in xml.Split(new string[] { "\" " }, StringSplitOptions.RemoveEmptyEntries))
			{
				if (item == "<IDP" || item == "/>") continue;

				try
				{
					var key = item.Substring(0, item.IndexOf("="));
					var value = item.Substring(key.Length, item.Length - key.Length);

					if (string.IsNullOrWhiteSpace(value) == false)
					{
						value = value.Replace("=\"", string.Empty);
					}

					var property = type.GetProperty(key);
					if (property != null)
					{
						property.SetValue(returnedXmlClass, value);
					}
					else
					{
						//Debug.WriteLine("Couldnt find property(" + key + ")");
					}

				}
				catch (Exception exc)
				{
					
				}
			}

			return returnedXmlClass;
		}
	}
}
