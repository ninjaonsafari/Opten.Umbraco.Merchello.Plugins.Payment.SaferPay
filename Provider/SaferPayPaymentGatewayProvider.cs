using Merchello.Core.Gateways;
using Merchello.Core.Gateways.Payment;
using Merchello.Core.Models;
using Merchello.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;

namespace Opten.Umbraco.Merchello.Web.Gateways.Payment.SaferPay.Provider
{
	[GatewayProviderActivation("da27fd6f-a314-47dc-8bed-c28f011c9855", "SaferPay Payment Provider", "SaferPay Payment Provider")]
	public class SaferPayPaymentGatewayProvider : PaymentGatewayProviderBase
	{
		#region AvailableResources

		internal static readonly IEnumerable<IGatewayResource> AvailableResources = new List<IGatewayResource>()
		{
			new GatewayResource("SaferPay", "SaferPay")
		};

		#endregion

		public SaferPayPaymentGatewayProvider(IGatewayProviderService gatewayProviderService, IGatewayProviderSettings gatewayProviderSettings, IRuntimeCacheProvider runtimeCacheProvider)
			: base(gatewayProviderService, gatewayProviderSettings, runtimeCacheProvider)
		{}

		/// <summary>
		/// Returns a list of remaining available resources
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IGatewayResource> ListResourcesOffered()
		{
			// PaymentMethods is created in PaymentGatewayProviderBase.  It is a list of all previously saved payment methods
			return AvailableResources.Where(x => PaymentMethods.All(y => y.PaymentCode != x.ServiceCode));
		}

		/// <summary>
		/// Creates a <see cref="IPaymentGatewayMethod"/>
		/// </summary>
		/// <param name="gatewayResource"></param>
		/// <param name="name">The name of the payment method</param>
		/// <param name="description">The description of the payment method</param>
		/// <returns>A <see cref="IPaymentGatewayMethod"/></returns>
		public override IPaymentGatewayMethod CreatePaymentMethod(IGatewayResource gatewayResource, string name, string description)
		{
			// assert gateway resource is still available
			var available = ListResourcesOffered().FirstOrDefault(x => x.ServiceCode == gatewayResource.ServiceCode);
			if (available == null) throw new InvalidOperationException("GatewayResource has already been assigned");

			var attempt = GatewayProviderService.CreatePaymentMethodWithKey(GatewayProviderSettings.Key, name, description, available.ServiceCode);


			if (attempt.Success)
			{
				PaymentMethods = null;

				return new SaferPayPaymentGatewayMethod(GatewayProviderService, attempt.Result, GatewayProviderSettings.ExtendedData);
			}

			LogHelper.Error<SaferPayPaymentGatewayProvider>(string.Format("Failed to create a payment method name: {0}, description {1}, paymentCode {2}", name, description, available.ServiceCode), attempt.Exception);

			throw attempt.Exception;
		}

		/// <summary>
		/// Gets a <see cref="IPaymentGatewayMethod"/> by it's unique 'key'
		/// </summary>
		/// <param name="paymentMethodKey">The key of the <see cref="IPaymentMethod"/></param>
		/// <returns>A <see cref="IPaymentGatewayMethod"/></returns>
		public override IPaymentGatewayMethod GetPaymentGatewayMethodByKey(Guid paymentMethodKey)
		{
			var paymentMethod = PaymentMethods.FirstOrDefault(x => x.Key == paymentMethodKey);

			if (paymentMethod == null) throw new NullReferenceException("PaymentMethod not found");

			return new SaferPayPaymentGatewayMethod(GatewayProviderService, paymentMethod, GatewayProviderSettings.ExtendedData);
		}

		/// <summary>
		/// Gets a <see cref="IPaymentGatewayMethod"/> by it's payment code
		/// </summary>
		/// <param name="paymentCode">The payment code of the <see cref="IPaymentGatewayMethod"/></param>
		/// <returns>A <see cref="IPaymentGatewayMethod"/></returns>
		public override IPaymentGatewayMethod GetPaymentGatewayMethodByPaymentCode(string paymentCode)
		{
			var paymentMethod = PaymentMethods.FirstOrDefault(x => x.PaymentCode == paymentCode);

			if (paymentMethod == null) throw new NullReferenceException("PaymentMethod not found");

			return new SaferPayPaymentGatewayMethod(GatewayProviderService, paymentMethod, GatewayProviderSettings.ExtendedData);
		}
	}
}
