using Merchello.Core;
using Merchello.Core.Gateways.Payment;
using Merchello.Core.Models;
using Merchello.Core.Services;

using System;

using SaferPayConstants = Opten.Umbraco.Merchello.Plugins.Payment.SaferPay.Constants;

namespace Opten.Umbraco.Merchello.Web.Gateways.Payment.SaferPay.Providers
{
	public class SaferPayPaymentGatewayMethod : PaymentGatewayMethodBase
	{
		private readonly SaferPayPaymentProcessor _processor;

		public SaferPayPaymentGatewayMethod(IGatewayProviderService gatewayProviderService, IPaymentMethod paymentMethod, ExtendedDataCollection providerExtendedData)
			: base(gatewayProviderService, paymentMethod)
		{
			_processor = new SaferPayPaymentProcessor();
		}

		protected override IPaymentResult PerformAuthorizePayment(global::Merchello.Core.Models.IInvoice invoice, ProcessorArgumentCollection args)
		{
			return InitializePayment(invoice, args, -1);
		}

		protected override IPaymentResult PerformCapturePayment(global::Merchello.Core.Models.IInvoice invoice, global::Merchello.Core.Models.IPayment payment, decimal amount, ProcessorArgumentCollection args)
		{
			payment.Collected = true;
			payment.Authorized = true;

			GatewayProviderService.Save(payment);
			GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, string.Format("To show promise of a {0} payment", PaymentMethod.Name), amount);

			return _processor.CapturePayment(invoice, payment, amount, false);
		}

		private IPaymentResult InitializePayment(IInvoice invoice, ProcessorArgumentCollection args, decimal captureAmount)
		{
			var payment = GatewayProviderService.CreatePayment(PaymentMethodType.CreditCard, invoice.Total, PaymentMethod.Key);
			payment.CustomerKey = invoice.CustomerKey;
			payment.Authorized = true;
			payment.Collected = false;
			payment.PaymentMethodName = SaferPayConstants.Name;
			payment.ExtendedData.SetValue("CaptureAmount", captureAmount.ToString(System.Globalization.CultureInfo.InvariantCulture));
			GatewayProviderService.Save(payment);

			var result = _processor.InitializePayment(invoice, payment, args);

			if (!result.Payment.Success)
			{
				GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Denied, "SaferPay: request initialization error: " + result.Payment.Exception.Message, 0);
			}
			else
			{
				GatewayProviderService.Save(payment);
				GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, "SaferPay: initialized", 0);
			}

			return result;
		}

		protected override IPaymentResult PerformRefundPayment(global::Merchello.Core.Models.IInvoice invoice, global::Merchello.Core.Models.IPayment payment, decimal amount, ProcessorArgumentCollection args)
		{
			throw new NotImplementedException();
		}

		protected override IPaymentResult PerformVoidPayment(global::Merchello.Core.Models.IInvoice invoice, global::Merchello.Core.Models.IPayment payment, ProcessorArgumentCollection args)
		{
			throw new NotImplementedException();
		}

		protected override IPaymentResult PerformAuthorizeCapturePayment(global::Merchello.Core.Models.IInvoice invoice, decimal amount, ProcessorArgumentCollection args)
		{
			throw new NotImplementedException();
		}
	}
}
