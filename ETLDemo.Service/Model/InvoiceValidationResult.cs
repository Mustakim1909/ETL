using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETL.Service.Model
{
    public class InvoiceValidationResult
    {
        public string EInvoiceNumber { get; set; }
        public string InvoiceTypeCode { get; set; }
        public string InvoiceVersion { get; set; }
        public string InvoiceDate { get; set; }
        public string BuyerName { get; set; }
        public string BuyerCategory { get; set; }
        public string BuyerBRNNumber { get; set; }
        public string BuyersTIN { get; set; }
        public string BuyerTelephone { get; set; }
        public string BuyerAddressLine0 { get; set; }
        public string BuyerCityName { get; set; }
        public string BuyersState { get; set; }
        public string BuyerCountry { get; set; }
        public string CurrencyCode { get; set; }
        public string UnitPrice { get; set; }
        public string SubTotal { get; set; }
        public string Quantity { get; set; }
        public string ProductDescription { get; set; }
        public string Buyeremail { get; set; }
    }

}
