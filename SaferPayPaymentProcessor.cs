using com.saferpay.Client;

using Merchello.Core.Gateways.Payment;
using Merchello.Core.Models;

using System;
using System.Web;

using Umbraco.Core;

using SaferPayConstants = Opten.Umbraco.Merchello.Plugins.Payment.SaferPay.Constants;

namespace Opten.Umbraco.Merchello.Web.Gateways.Payment.SaferPay
{
	public class SaferPayPaymentProcessor
	{
		public MessageFactory messageFactory;

		public SaferPayPaymentProcessor(string configPath = null)
		{
			messageFactory = new MessageFactory();
			messageFactory.Open(SaferPayConstants.Config);
		}

		public MessageObject VerifyResponse(string data, string signature)
		{
			return messageFactory.VerifyPayConfirm(data, signature);
		}

		public string GetPostUrl(IInvoice invoice, IPayment payment)
		{
			MessageObject pay = GetMessage(invoice, payment);
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
			var id = messageObject.GetAttribute(SaferPayConstants.MessageAttributes.Id);
			var token = messageObject.GetAttribute(SaferPayConstants.MessageAttributes.Token);

			if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(token))
			{
				return new PaymentResult(Attempt<IPayment>.Fail(payment), invoice, false);
			}

			// save this values to payment
			payment.ExtendedData.SetValue(SaferPayConstants.MessageAttributes.Id, id);
			payment.ExtendedData.SetValue(SaferPayConstants.MessageAttributes.Token, id);

			// Complete order for saferpay
			MessageObject payComplete = messageFactory.CreateRequest(SaferPayConstants.PayCompleteKey);
			payComplete.SetAttribute(SaferPayConstants.MessageAttributes.AccountId, "99867-94913159");
			payComplete.SetAttribute(SaferPayConstants.MessageAttributes.Id, id);
			payComplete.SetAttribute(SaferPayConstants.MessageAttributes.Token, token);
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
			MessageObject pay = GetMessage(invoice, payment);
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

		private MessageObject GetMessage(IInvoice invoice, IPayment payment)
		{
			if (invoice == null || payment == null)
				return null;

			MessageObject pay = messageFactory.CreatePayInit();
			pay.SetAttribute(SaferPayConstants.MessageAttributes.AccountId, "99867-94913159");
			pay.SetAttribute(SaferPayConstants.MessageAttributes.Amount, (invoice.Total * 100).ToString("##"));
			pay.SetAttribute(SaferPayConstants.MessageAttributes.Currency, "CHF");
			pay.SetAttribute(SaferPayConstants.MessageAttributes.Description, "Gidor");
			pay.SetAttribute(SaferPayConstants.MessageAttributes.OrderId, invoice.Key.ToString());
			pay.SetAttribute(SaferPayConstants.MessageAttributes.SuccessLink, GetWebsiteUrl() + SaferPayConstants.Api.BaseUrl + SaferPayConstants.Api.SuccessMethodName);
			pay.SetAttribute(SaferPayConstants.MessageAttributes.FailLink, GetWebsiteUrl() + SaferPayConstants.Api.BaseUrl + SaferPayConstants.Api.FailMethodName);
			pay.SetAttribute(SaferPayConstants.MessageAttributes.BackLink, GetWebsiteUrl() + SaferPayConstants.Api.BaseUrl + SaferPayConstants.Api.BackMethodName);

			pay.SetAttribute(SaferPayConstants.MessageAttributes.NotifyAddress, "tobias.lopez@opten.ch");

			pay.SetAttribute(SaferPayConstants.MessageAttributes.LangId, "de");
			pay.SetAttribute(SaferPayConstants.MessageAttributes.ShowLanguages, "yes");

			if (invoice != null)
			{
				IAddress address = invoice.GetBillingAddress();
				pay.SetAttribute(SaferPayConstants.MessageAttributes.UserNotify, invoice.BillToEmail);
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Company, invoice.BillToCompany);
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Firstname, invoice.GetBillingAddress().TrySplitFirstName());
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Lastname, invoice.GetBillingAddress().TrySplitLastName());
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Street, invoice.BillToAddress1);
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Zip, invoice.BillToPostalCode);
				pay.SetAttribute(SaferPayConstants.MessageAttributes.City, invoice.BillToRegion);
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Country, invoice.BillToCountryCode);
				pay.SetAttribute(SaferPayConstants.MessageAttributes.EMail, invoice.BillToEmail);
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Phone, invoice.BillToPhone);
			}

			return pay;
		}
	}
}
