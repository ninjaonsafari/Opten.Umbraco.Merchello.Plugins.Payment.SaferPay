using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Opten.Umbraco.Merchello.Web.Gateways.Payment.SaferPay.Models
{
	public class ResponseSuccess
	{
		public string MSGTYPE { get; set; }
		public string VTVERIFY { get; set; }
		public string KEYID { get; set; }
		public string ID { get; set; }
		public string TOKEN { get; set; }
		public string ACCOUNTID { get; set; }
		public string AMOUNT { get; set; }
		public string CURRENCY { get; set; }
		public string DCCAMOUNT { get; set; }
		public string DCCCURRENCY { get; set; }
		public string CARDREFID { get; set; }
		public string SCDRESULT { get; set; }
		public string PROVIDERID { get; set; }
		public string PROVIDERNAME { get; set; }
		public string PAYMENTMETHOD { get; set; }
		public string ORDERID { get; set; }
		public string IP { get; set; }
		public string IPCOUNTRY { get; set; }
		public string CCCOUNTRY { get; set; }
		public string MPI_LIABILITYSHIFT { get; set; }
		public string ECI { get; set; }
		public string XID { get; set; }
		public string CAVV { get; set; }
		public string IBAN { get; set; }
		public string MANDATEID { get; set; }
		public string CREDITORID { get; set; }
		public string MPI_TX_CAVV { get; set; }
		public string MPI_XID { get; set; }
	}
}
