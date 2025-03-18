using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETL.Service.Model
{
    public class ETLProcess
    {
        [Key]
        public int Id { get; set; }
        public string ETLJobName { get; set; }
        public string InvoiceNumber { get; set; }
        public string InvoiceCode { get; set; }
        public string FileName { get; set; }
        public DateTime? FileDateTime { get; set; }
        public DateTime? ETLProcessDatetime { get; set; }
        public string InvoiceVersion { get; set; }
        public string InvoiceTypeCode { get; set; }
        public string InvoiceDate { get; set; }
        public string BuyerName { get; set; }
        public string BuyerCategory { get; set; }
        public string BuyersTIN { get; set; }
        public string BuyerAddressLine0 { get; set; }
        public string BuyerCityName { get; set; }
        public string BuyersState { get; set; }
        public string BuyerCountry { get; set; }
        public string CustomerInvoicecode { get; set; }
        public string InvoiceUUID { get; set; }
        public string InvoiceDocumentReference { get; set; }
        public string EInvoiceNumber { get; set; }
        public string CurrencyCode { get; set; }
        public string UnitPrice { get; set; }
        public string Quantity { get; set; }
        public string SubTotal { get; set; }
        public string BuyerTelephone { get; set; }
        public string SumofInvoicelinenetAmount { get; set; }
        public string SumofAllowances { get; set; }
        public string TotalFeeorChargeAmount { get; set; }
        public string TotalNetAmount { get; set; }
        public string TotalTaxAmount { get; set; }
        public string TotalIncludingTax { get; set; }
        public string RoundingAmount { get; set; }
        public string TotalPayableAmount { get; set; }
        public string TotalDiscountValue { get; set; }
        public string ProductDescription { get; set; }
        public string Buyeremail { get; set; }
        public string BuyerBRNNumber { get; set; }
        public string SupplierName { get; set; }
        public string SupplierCategory { get; set; }
        public string SuppliersTIN { get; set; }
        public string SupplierAddressLine0 { get; set; }
        public string SupplierCityName { get; set; }
        public string SuppliersState { get; set; }
        public string SupplierCountry { get; set; }
        public string SupplierMSIC { get; set; }
        public DateTime? CreatedDate {  get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
