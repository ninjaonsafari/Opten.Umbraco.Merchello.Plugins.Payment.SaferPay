using com.saferpay.Client;
using Merchello.Core;
using Merchello.Core.Gateways.Payment;
using Merchello.Core.Models;
using Merchello.Core.Services;
using Merchello.Web;
using Opten.Umbraco.Merchello.Web.Models.Checkout;
using System;
using System.Web;
using Umbraco.Core;
using Umbraco.Web.Models;

namespace Opten.Umbraco.Merchello.Web.Gateways.Payment.SaferPay
{
	public class SaferPayPaymentProcessor
	{
		private string baseUrl = "/umbraco/v1/ResponseApi/";
		private string configFile = @"C:\Program Files\Saferpay\Client\";
		public MessageFactory messageFactory;

		public SaferPayPaymentProcessor(string configPath = null)
		{
			if (string.IsNullOrWhiteSpace(configPath) == false)
			{
				this.configFile = configPath;
			}

			messageFactory = new MessageFactory();
			messageFactory.Open(configFile);
		}

		public MessageObject VerifyResponse(string data, string signature)
		{
			return messageFactory.VerifyPayConfirm(data, signature);
		}

		public string GetPostUrl(IInvoice invoice, IPayment payment)
		{
			MessageObject pay = messageFactory.CreatePayInit();
			pay.SetAttribute("ACCOUNTID", "99867-94913159");
			pay.SetAttribute("AMOUNT", "700");
			pay.SetAttribute("CURRENCY", "CHF");
			pay.SetAttribute("DESCRIPTION", "SWAGERSTAN");
			pay.SetAttribute("ORDERID", "001");
			pay.SetAttribute("FAILLINK", GetWebsiteUrl() + baseUrl + "GetFail");
			pay.SetAttribute("BACKLINK", GetWebsiteUrl() + baseUrl + "GetBack");

			pay.SetAttribute("NOTIFYADDRESS", "tobias.lopez@opten.ch");

			pay.SetAttribute("LANGID", "de");
			pay.SetAttribute("SHOWLANGUAGES", "yes");

			pay.SetAttribute("SUCCESSLINK", string.Format("{0}{1}GetSuccess?inv={2}&pay={3}", GetWebsiteUrl(), baseUrl, invoice.Key, payment.Key));
			

			if (invoice != null)
			{
				IAddress address = invoice.GetBillingAddress();
				pay.SetAttribute("USERNOTIFY", invoice.BillToEmail);
				pay.SetAttribute("COMPANY", invoice.BillToCompany);
				pay.SetAttribute("FIRSTNAME", invoice.GetBillingAddress().TrySplitFirstName());
				pay.SetAttribute("LASTNAME", invoice.GetBillingAddress().TrySplitLastName());
				pay.SetAttribute("STREET", invoice.BillToAddress1);
				pay.SetAttribute("ZIP", invoice.BillToPostalCode);
				pay.SetAttribute("CITY", invoice.BillToRegion);
				pay.SetAttribute("COUNTRY", invoice.BillToCountryCode);
				pay.SetAttribute("EMAIL", invoice.BillToEmail);
				pay.SetAttribute("PHONE", invoice.BillToPhone);
				pay.SetAttribute("DELIVERY", "no");
			}
			
			string postUrl = pay.GetUrl();

			if (string.IsNullOrWhiteSpace(postUrl))
			{
				/* ToDo: log this */
				/* couldnt created the url */
				throw new Exception();
			}

			return postUrl;
		}

		/// <summary>
		/// Get the absolute base URL for this website
		/// </summary>
		/// <returns></returns>
		private static string GetWebsiteUrl()
		{
			var url = HttpContext.Current.Request.Url;
			var baseUrl = String.Format("{0}://{1}{2}", url.Scheme, url.Host, url.IsDefaultPort ? "" : ":" + url.Port);
			return baseUrl;
		}

		public IPaymentResult InitializePayment(IInvoice invoice, IPayment payment, ProcessorArgumentCollection args)
		{
			string postUrl = GetPostUrl(invoice, payment);
			payment.ExtendedData.SetValue("RedirectUrl", postUrl);
			HttpContext.Current.Response.Redirect(postUrl);
			return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, true);
		}

		public IPaymentResult AuthorizePayment(IInvoice invoice, IPayment payment, string signature, string data)
		{
			MessageObject messageObject = VerifyResponse(data, signature);
			var id = messageObject.GetAttribute("ID");
			var token = messageObject.GetAttribute("TOKEN");

			if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(token))
			{
				return new PaymentResult(Attempt<IPayment>.Fail(payment), invoice, false);
			}

			// save this values to payment
			payment.ExtendedData.SetValue("ID", id);
			payment.ExtendedData.SetValue("TOKEN", id);

			// Complete order for saferpay
			MessageObject payComplete = messageFactory.CreateRequest("PayComplete");
			payComplete.SetAttribute("ACCOUNTID", "99867-94913159");
			payComplete.SetAttribute("ID", id);
			payComplete.SetAttribute("TOKEN", token);
			MessageObject payCompleteResult = payComplete.Capture();

			// authorize in merchello
			payment.Authorized = true;


			return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, true);
		}

		public IPaymentResult CapturePayment(IInvoice invoice, IPayment payment, decimal amount, bool isPartialPayment)
		{
			payment.Authorized = true;
			payment.Collected = true;
			return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, true);
		}

		internal IPaymentResult ProcessPayment(IInvoice invoice, IPayment payment, ProcessorArgumentCollection args)
		{
			MessageObject pay = messageFactory.CreatePayInit();
			pay.SetAttribute("ACCOUNTID", "99867-94913159");
			pay.SetAttribute("AMOUNT", "700");
			pay.SetAttribute("CURRENCY", "CHF");
			pay.SetAttribute("DESCRIPTION", "SWAGERSTAN");
			pay.SetAttribute("ORDERID", "001");
			pay.SetAttribute("SUCCESSLINK", GetWebsiteUrl() + baseUrl + "GetSuccess");
			pay.SetAttribute("FAILLINK", GetWebsiteUrl() + baseUrl + "GetFail");
			pay.SetAttribute("BACKLINK", GetWebsiteUrl() + baseUrl + "GetBack");

			pay.SetAttribute("NOTIFYADDRESS", "tobias.lopez@opten.ch");

			pay.SetAttribute("LANGID", "de");
			pay.SetAttribute("SHOWLANGUAGES", "yes");

			if (invoice != null)
			{
				IAddress address = invoice.GetBillingAddress();
				pay.SetAttribute("USERNOTIFY", invoice.BillToEmail);
				pay.SetAttribute("COMPANY", invoice.BillToCompany);
				pay.SetAttribute("FIRSTNAME", invoice.GetBillingAddress().TrySplitFirstName());
				pay.SetAttribute("LASTNAME", invoice.GetBillingAddress().TrySplitLastName());
				pay.SetAttribute("STREET", invoice.BillToAddress1);
				pay.SetAttribute("ZIP", invoice.BillToPostalCode);
				pay.SetAttribute("CITY", invoice.BillToRegion);
				pay.SetAttribute("COUNTRY", invoice.BillToCountryCode);
				pay.SetAttribute("EMAIL", invoice.BillToEmail);
				pay.SetAttribute("PHONE", invoice.BillToPhone);
			}

			string postUrl = pay.GetUrl();
			

			if (string.IsNullOrWhiteSpace(postUrl))
			{
				/*ToDo: log this */
				/* couldnt created the url */
				//throw new Exception();
				return new PaymentResult(Attempt<IPayment>.Fail(payment), invoice, false);
			}

			//payment.ExtendedData.SetValue("RedirectUrl", postUrl);

			return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, false);
		}
	}
}
