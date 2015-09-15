using Merchello.Core;
using Opten.Umbraco.Merchello.Plugins.Payment.SaferPay;
using Opten.Umbraco.Merchello.Plugins.Payment.SaferPay.Providers;
using Opten.Umbraco.Merchello.Plugins.Payment.SaferPay.Extensions;
using Opten.Umbraco.Merchello.Web.WebApi;
using System;
using System.Linq;
using System.Web;
using System.Web.Http;
using Umbraco.Core.Logging;
using Umbraco.Web.Mvc;
using SaferPayConstants = Opten.Umbraco.Merchello.Plugins.Payment.SaferPay.Constants;
using Opten.Umbraco.Merchello.Plugins.Payment.SaferPay.Models;
using System.Net.Http;
using System.Net;
using System.Text;

namespace Opten.Umbraco.Merchello.Web.Gateways.Payment.SaferPay.Controllers
{
	[PluginController("v1")]
	public class ResponseApiController : CommerceApiController
	{
		private readonly IMerchelloContext _merchelloContext;
		private readonly SaferPayPaymentProcessor _processor;
		private readonly SaferPayProcessorSettings _settings;

		public ResponseApiController() : this(MerchelloContext.Current) {}

		/// <summary>
		/// Initializes a new instance of the <see cref="PayPalApiController"/> class.
		/// </summary>
		/// <param name="merchelloContext">
		/// The <see cref="IMerchelloContext"/>.
		/// </param>
		public ResponseApiController(IMerchelloContext merchelloContext)
		{
			if (merchelloContext == null) throw new ArgumentNullException("merchelloContext");

			var providerKey = new Guid(SaferPayConstants.Key);
			var provider = (SaferPayPaymentGatewayProvider)merchelloContext.Gateways.Payment.GetProviderByKey(providerKey);

			if (provider == null)
			{
				var ex = new NullReferenceException("The PayPalPaymentGatewayProvider could not be resolved.  The provider must be activiated");
				LogHelper.Error<ResponseApiController>("PayPalPaymentGatewayProvider not activated.", ex);
				throw ex;
			}

			_merchelloContext = merchelloContext;
			_settings = provider.ExtendedData.GetSaferPayProcessorSettings();
			_processor = new SaferPayPaymentProcessor(_settings);
		}

		[ActionName(SaferPayConstants.Api.SuccessMethodName)]
		public HttpResponseMessage GetSuccess()
		{
			string dataAsXml = HttpContext.Current.Request.QueryString.Get(SaferPayConstants.MessageAttributes.Data);
			string signatur = HttpContext.Current.Request.QueryString.Get(SaferPayConstants.MessageAttributes.Signature);
			string invoiceKey = HttpContext.Current.Request.QueryString.Get(SaferPayConstants.MessageAttributes.Invoice);
			string paymentKey = HttpContext.Current.Request.QueryString.Get(SaferPayConstants.MessageAttributes.Payment);

			if (string.IsNullOrEmpty(invoiceKey) || string.IsNullOrEmpty(paymentKey))
				return ShowError(string.Format("Invalid argument exception. Arguments: invoiceKey={0}, paymentKey={1}.", invoiceKey, paymentKey));

			var invoice = _merchelloContext.Services.InvoiceService.GetByKey(new Guid(invoiceKey));
			var payment = _merchelloContext.Services.PaymentService.GetByKey(new Guid(paymentKey));

			var providerKeyGuid = new Guid(SaferPayConstants.Key);
			var paymentGatewayMethod = _merchelloContext.Gateways.Payment
				.GetPaymentGatewayMethods()
				.First(item => item.PaymentMethod.ProviderKey == providerKeyGuid);

			var authorizeResult = _processor.AuthorizePayment(invoice, payment, signatur, dataAsXml);

			_merchelloContext.Services.GatewayProviderService.Save(payment);
			if (!authorizeResult.Payment.Success)
			{
				LogHelper.Error<ResponseApiController>("Payment is not authorized.", authorizeResult.Payment.Exception);
				_merchelloContext.Services.GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Denied, "SaferPay: request capture authorization error: " + authorizeResult.Payment.Exception.Message, 0);
				//return ShowError(authorizeResult.Payment.Exception.Message);
			}

			//_merchelloContext.Services.GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, "SaferPay: capture authorized", invoice.Total);

			// The basket can be empty
			var customerContext = new global::Merchello.Web.CustomerContext(this.UmbracoContext);
			var currentCustomer = customerContext.CurrentCustomer;
			if (currentCustomer != null)
			{
				var basket = global::Merchello.Web.Workflow.Basket.GetBasket(currentCustomer);
				basket.Empty();
			}

			// Capture
			var captureResult = paymentGatewayMethod.CapturePayment(invoice, payment, payment.Amount, null);
			if (!captureResult.Payment.Success)
			{
				LogHelper.Error<ResponseApiController>("Payment is not captured.", captureResult.Payment.Exception);
				return ShowError(captureResult.Payment.Exception.Message);
			}

			return GetResponseMessage(_settings.ReturnUrl);
		}

		[ActionName(SaferPayConstants.Api.FailMethodName)]
		public HttpResponseMessage GetFail()
		{
			return HandleAbortion();
		}

		[ActionName(SaferPayConstants.Api.BackMethodName)]
		public HttpResponseMessage GetBack()
		{
			return HandleAbortion();
		}

		private HttpResponseMessage HandleAbortion()
		{
			string dataAsXml = HttpContext.Current.Request.QueryString.Get(SaferPayConstants.MessageAttributes.Data);
			string signatur = HttpContext.Current.Request.QueryString.Get(SaferPayConstants.MessageAttributes.Signature);
			string invoiceKey = HttpContext.Current.Request.QueryString.Get(SaferPayConstants.MessageAttributes.Invoice);
			string paymentKey = HttpContext.Current.Request.QueryString.Get(SaferPayConstants.MessageAttributes.Payment);

			var invoiceService = _merchelloContext.Services.InvoiceService;
			var paymentService = _merchelloContext.Services.PaymentService;

			var invoice = invoiceService.GetByKey(new Guid(invoiceKey));
			var payment = paymentService.GetByKey(new Guid(paymentKey));
			if (invoice == null || payment == null)
			{
				var ex = new NullReferenceException(string.Format("Invalid argument exception. Arguments: invoiceKey={0}, paymentKey={1}", invoiceKey, paymentKey));
				LogHelper.Error<ResponseApiController>("Payment is not authorized.", ex);
				return ShowError(ex.Message);
			}

			// Delete invoice
			invoiceService.Delete(invoice);

			return GetResponseMessage(_settings.CancelUrl);
		}

		private HttpResponseMessage GetResponseMessage(string path)
		{
			var url = _processor.GetWebsiteUrl(null, Guid.Empty, Guid.Empty);
			url += path;

			var response = Request.CreateResponse(HttpStatusCode.Moved);
			response.Headers.Location = new Uri(url);

			return response;
		}

		private HttpResponseMessage ShowError(string message)
		{
			var resp = new HttpResponseMessage(HttpStatusCode.OK);
			resp.Content = new StringContent("Error: " + message, Encoding.UTF8, "text/plain");
			return resp;
		}
	}
}
