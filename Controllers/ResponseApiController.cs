using Merchello.Core;
using Opten.Umbraco.Merchello.Web.Gateways.Payment.SaferPay.Providers;
using Opten.Umbraco.Merchello.Web.WebApi;
using System;
using System.Linq;
using System.Web;
using System.Web.Http;
using Umbraco.Core.Logging;
using Umbraco.Web.Mvc;
using SaferPayConstants = Opten.Umbraco.Merchello.Plugins.Payment.SaferPay.Constants;

namespace Opten.Umbraco.Merchello.Web.Gateways.Payment.SaferPay.Controllers
{
	[PluginController("v1")]
	public class ResponseApiController : CommerceApiController
	{
		/// <summary>
		/// Merchello context
		/// </summary>
		private readonly IMerchelloContext _merchelloContext;

		/// <summary>
		/// The PayPal payment processor.
		/// </summary>
		private readonly SaferPayPaymentProcessor _processor;

		/// <summary>
		/// Initializes a new instance of the <see cref="PayPalApiController"/> class.
		/// </summary>
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
			_processor = new SaferPayPaymentProcessor();
		}

		[ActionName(SaferPayConstants.Api.SuccessMethodName)]
		public bool GetSuccess()
		{
			string dataAsXml = HttpContext.Current.Request.QueryString.Get(SaferPayConstants.MessageAttributes.Data);
			string signatur = HttpContext.Current.Request.QueryString.Get(SaferPayConstants.MessageAttributes.Signature);
			string invoiceKey = HttpContext.Current.Request.QueryString.Get(SaferPayConstants.MessageAttributes.Invoice);
			string paymentKey = HttpContext.Current.Request.QueryString.Get(SaferPayConstants.MessageAttributes.Payment);

			if (string.IsNullOrEmpty(invoiceKey) || string.IsNullOrEmpty(paymentKey))
				return false;

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
				//return ShowError(captureResult.Payment.Exception.Message);
			}

			// ToDo: redirect to success page

			return true;
		}

		[ActionName(SaferPayConstants.Api.FailMethodName)]
		public bool GetFail()
		{
			return true;
		}

		[ActionName(SaferPayConstants.Api.BackMethodName)]
		public bool GetBack()
		{
			return true;
		}
	}
}
