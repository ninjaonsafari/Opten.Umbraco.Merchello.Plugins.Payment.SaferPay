using Newtonsoft.Json;

namespace Opten.Umbraco.Merchello.Plugins.Payment.SaferPay.Models
{
	public class SaferPayProcessorSettings
	{
		[JsonProperty("accountId")]
		public string AccountId { get; set; }

		[JsonProperty("notfiyAddress")]
		public string NotifyAddress { get; set; }

		[JsonProperty("returnUrl")]
		public string ReturnUrl { get; set; }

		[JsonProperty("cancelUrl")]
		public string CancelUrl { get; set; }

		[JsonProperty("description")]
		public string Desciption { get; set; }
	}
}