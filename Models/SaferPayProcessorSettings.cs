namespace Opten.Umbraco.Merchello.Plugins.Payment.SaferPay.Models
{
	public class PayPalProcessorSettings
	{
		public string AccountId { get; set; }
		public string NotifyAddress { get; set; }
		public string ReturnUrl { get; set; }
		public string CancelUrl { get; set; }
	}
}