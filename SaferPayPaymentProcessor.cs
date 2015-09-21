using com.saferpay.Client;
using Merchello.Core.Gateways.Payment;
using Merchello.Core.Models;
using Opten.Umbraco.Merchello.Plugins.Payment.SaferPay.Models;
using System;
using System.Diagnostics;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Logging;
using SaferPayConstants = Opten.Umbraco.Merchello.Plugins.Payment.SaferPay.Constants;

namespace Opten.Umbraco.Merchello.Plugins.Payment.SaferPay
{
	public class SaferPayPaymentProcessor
	{
		public MessageFactory _messageFactory;
		public SaferPayProcessorSettings _settings;

		public SaferPayPaymentProcessor(SaferPayProcessorSettings settings)
		{
			_messageFactory = new MessageFactory();
			_messageFactory.Open(SaferPayConstants.Config);
			_settings = settings;
		}

		public MessageObject VerifyResponse(string data, string signature)
		{
			return _messageFactory.VerifyPayConfirm(data, signature);
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
		public string GetWebsiteUrl(string actionName, Guid invoiceKey, Guid paymentKey)
		{
			var url = HttpContext.Current.Request.Url;
			var baseUrl = String.Format("{0}://{1}{2}", url.Scheme, url.Host, url.IsDefaultPort ? "" : ":" + url.Port);

			if (string.IsNullOrEmpty(actionName) || invoiceKey.Equals(Guid.Empty) || paymentKey.Equals(Guid.Empty))
			{
				LogHelper.Error<SaferPayPaymentProcessor>(string.Format("Couldn't generate the url for {0} paymentKey: {1} invoiceKey: {2}", actionName, paymentKey, invoiceKey), null);
				return baseUrl;
			}
				

			baseUrl += SaferPayConstants.Api.BaseUrl;
			baseUrl += actionName;
			baseUrl += string.Format("?{0}={1}", SaferPayConstants.MessageAttributes.Invoice, invoiceKey);
			baseUrl += string.Format("&{0}={1}", SaferPayConstants.MessageAttributes.Payment, paymentKey);

			Debug.WriteLine("Generated Url" + baseUrl);
			LogHelper.Info<SaferPayPaymentProcessor>(string.Format("Generated Url: {0}", baseUrl));

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
			MessageObject payComplete = _messageFactory.CreateRequest(SaferPayConstants.PayCompleteKey);
			payComplete.SetAttribute(SaferPayConstants.MessageAttributes.AccountId, _settings.AccountId);
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

			MessageObject pay = _messageFactory.CreatePayInit();
			pay.SetAttribute(SaferPayConstants.MessageAttributes.AccountId, _settings.AccountId);
			pay.SetAttribute(SaferPayConstants.MessageAttributes.Amount, (invoice.Total * 100).ToString("##"));
			pay.SetAttribute(SaferPayConstants.MessageAttributes.Currency, "CHF");
			pay.SetAttribute(SaferPayConstants.MessageAttributes.Description, "Gidor");
			pay.SetAttribute(SaferPayConstants.MessageAttributes.OrderId, invoice.Key.ToString());

			// return urls
			pay.SetAttribute(SaferPayConstants.MessageAttributes.FailLink, GetWebsiteUrl(SaferPayConstants.Api.FailMethodName, invoice.Key, payment.Key));
			pay.SetAttribute(SaferPayConstants.MessageAttributes.BackLink, GetWebsiteUrl(SaferPayConstants.Api.BackMethodName, invoice.Key, payment.Key));
			pay.SetAttribute(SaferPayConstants.MessageAttributes.SuccessLink, GetWebsiteUrl(SaferPayConstants.Api.SuccessMethodName, invoice.Key, payment.Key));

			// todo: get them settings
			pay.SetAttribute(SaferPayConstants.MessageAttributes.NotifyAddress, _settings.NotifyAddress);
			pay.SetAttribute(SaferPayConstants.MessageAttributes.LangId, "de");
			pay.SetAttribute(SaferPayConstants.MessageAttributes.ShowLanguages, "yes");

			if (invoice != null)
			{
				IAddress address = invoice.GetBillingAddress();
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Delivery, "no");
				pay.SetAttribute(SaferPayConstants.MessageAttributes.UserNotify, invoice.BillToEmail);
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Company, invoice.BillToCompany);
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Firstname, invoice.GetBillingAddress().TrySplitFirstName());
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Lastname, invoice.GetBillingAddress().TrySplitLastName());
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Street, invoice.BillToAddress1);
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Zip, invoice.BillToPostalCode);
				pay.SetAttribute(SaferPayConstants.MessageAttributes.City, invoice.BillToLocality);
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Country, invoice.BillToCountryCode);
				pay.SetAttribute(SaferPayConstants.MessageAttributes.EMail, invoice.BillToEmail);
				pay.SetAttribute(SaferPayConstants.MessageAttributes.Phone, invoice.BillToPhone);
			}

			return pay;
		}
	}
}
