using Merchello.Core.Models;
using Newtonsoft.Json;
using Opten.Umbraco.Merchello.Plugins.Payment.SaferPay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SaferPayConstants = Opten.Umbraco.Merchello.Plugins.Payment.SaferPay.Constants;

namespace Opten.Umbraco.Merchello.Plugins.Payment.SaferPay.Extensions
{
	public static class ExtendedDataExtensions
	{
		/// <summary>
		/// Serializes the <see cref="FixedRateTaxationProviderSettings" /> and saves them in the extend data collection.
		/// </summary>
		/// <param name="extendedData">The extended data.</param>
		/// <param name="settings">The settings.</param>
		public static void SaveProviderSettings(this ExtendedDataCollection extendedData, SaferPayProcessorSettings settings)
		{
			extendedData.SetValue(SaferPayConstants.ExtendedDataKey, JsonConvert.SerializeObject(settings));
		}

		/// <summary>
		/// Deserializes Tax and Rounding tax provider settings from the gateway provider's extended data collection
		/// </summary>
		/// <param name="extendedData">The extended data.</param>
		/// <returns>
		/// The <see cref="FixedRateTaxationProviderSettings" />.
		/// </returns>
		public static SaferPayProcessorSettings GetSaferPayProcessorSettings(this ExtendedDataCollection extendedData)
		{
			return JsonConvert.DeserializeObject<SaferPayProcessorSettings>(extendedData.GetValue(SaferPayConstants.ExtendedDataKey));
		}
	}
}
