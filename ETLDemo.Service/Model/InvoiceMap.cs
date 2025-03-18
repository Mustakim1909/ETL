using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;

namespace ETL_Demo.Models
{
    public class InvoiceMap
    {
        public class InvoiceDataMap : ClassMap<InvoiceCSVData>
        {
            public InvoiceDataMap()
            {
                // Mapping CSV columns to model properties

                // Start Invoice Data map
                Map(m => m.InvoiceVersion).Name("e-Invoice Version", "e - Invoice Version", "e-Invoice_Version").Optional();
                Map(m => m.CbcInvoiceTypeCode).Name("e-Invoice Type Code").Optional();
                Map(m => m.EInvoiceNumber).Name("e-Invoice Code / Number", "e-Invoice Number").Optional();
                //Map(m => m.InvoiceDate).Name("e-Invoice Date").Optional();
                Map(m => m.InvoiceTime).Name("e-Invoice Time").Optional();
                //Map(m => m.eInvoiceDateTime).Name("e-Invoice Date").TypeConverterOption.Format("dd-MM-yyyy");
                //Map(m => m.eInvoiceDateTime).Name("e-Invoice Date").TypeConverter<FlexibleDateTimeConverter>();
                Map(m => m.CbcDocumentCurrencyCode).Name("Invoice Currency Code").Optional();
                Map(m => m.CbcCurrencyExchangeRate).Name("Currency Exchange Rate").Optional();
                Map(m => m.PaymentMode).Name("Payment Mode").Optional();
                Map(m => m.CbcSupplierBankAccountNumber).Name("Supplier Bank Account Number", "Supplier’s Bank Account Number").Optional();
                Map(m => m.CacPaymentTerms).Name("Payment Terms").Optional();
                Map(m => m.CbcSellerName).Name("Supplier's Name").Optional();
                Map(m => m.CbcSellerVATID).Name("Supplier’s TIN").Optional();
                Map(m => m.CbcSBRNNumber).Name("Supplier’s Business Registration Number").Optional();
                Map(m => m.CbcSCategory).Name("Supplier’s Category").Optional();
                Map(m => m.CbcSSubCategory).Name("Supplier’s Subcategory").Optional();
                Map(m => m.CbcSellerSSTRegistrationNumber).Name("Supplier’s SST Registration Number").Optional();
                Map(m => m.CbcSellerTourismTaxRegistrationNumber).Name("Supplier’s Tourism Tax Registration Number").Optional();
                Map(m => m.CbcMsicCode).Name("Supplier’s Malaysia Standard Industrial Classification (MSIC) Code").Optional();
                Map(m => m.CbcBusinessActivityDesc).Name("Supplier’s Business Activity Description").Optional();
                Map(m => m.CbcSellerElectronicMail).Name("Supplier’s e-mail").Optional();
                Map(m => m.SellerContactPerson).Name("Supplier's contact point").Optional();
                Map(m => m.CbcSellerTelephone).Name("Supplier’s Contact Number").Optional();
                Map(m => m.CbcSStreetName).Name("Supplier’s Address Line 0").Optional();
                Map(m => m.CbcSAdditionalStreetName1).Name("Supplier’s Address Line 1").Optional();
                Map(m => m.CbcSAdditionalStreetName2).Name("Supplier’s Address Line 2").Optional();
                Map(m => m.CbcSPostalZone).Name("Supplier’s Postal Zone").Optional();
                Map(m => m.CbcSCityName).Name("Supplier’s City Name").Optional();
                Map(m => m.CbcSCountrySubentity).Name("Supplier’s State").Optional();
                Map(m => m.CbcSCountryIdentificationCode).Name("Supplier’s Country").Optional();
                Map(m => m.PaymentDueDate).Name("Payment due date").Optional();
                Map(m => m.CbcBillReferenceNumber).Name("Bill Reference Number").Optional();
                Map(m => m.BP_CODE).Name("BP_CODE").Optional();
                Map(m => m.CbcBuyerName).Name("Buyer Name", "Buyer’s Name").Optional();
                Map(m => m.CbcBuyerVATID).Name("Buyer TIN", "Buyer’s TIN").Optional();
                Map(m => m.CbcBCategory).Name("Buyer Category", "Buyer’s Category").Optional();
                Map(m => m.CbcBSubCategory).Name("Buyer Subcategory", "Buyer’s Subcategory").Optional();
                Map(m => m.CbcBBRNNumber).Name("BRN", "Buyer’s Business Registration Number").Optional();
                Map(m => m.CbcIdentificationCode).Name("Buyer Identification No", "Buyer’s Identification Number / Passport Number").Optional();
                Map(m => m.CbcBSSTRegistrationNumber).Name("Buyer SST", "Buyer’s SST Registration Number").Optional();
                Map(m => m.BuyerContactPerson).Name("Buyer contact point", "Buyer’s contact point (Person Name)").Optional();
                Map(m => m.CbcBuyerElectronicMail).Name("Buyer e-mail", "Buyer’s e-mail").Optional();
                Map(m => m.CbcBuyerTelephone).Name("Buyer Contact Number", "Buyer’s Contact Number").Optional();
                Map(m => m.CbcBStreetName).Name("Buyer Address Line 0", "Buyer’s Address Line 0").Optional();
                Map(m => m.CbcBAdditionalStreetName1).Name("Buyer Address Line 1", "Buyer’s Address Line 1").Optional();
                Map(m => m.CbcBAdditionalStreetName2).Name("Buyer Address Line 2", "Buyer’s Address Line 2").Optional();
                Map(m => m.CbcBPostalZone).Name("Buyer Postal Zone", "Buyer’s Postal Zone").Optional();
                Map(m => m.CbcBCityName).Name("Buyer City Name", "Buyer’s City Name").Optional();
                Map(m => m.CbcBCountrySubentity).Name("Buyer State", "Buyer’s State").Optional();
                Map(m => m.CbcBCountryIdentificationCode).Name("Buyer Country", "Buyer’s Country").Optional();
                Map(m => m.CbcInvoiceLineNetAmount).Name("Invoice line net amount ", "Invoice line net amount").Optional();
                Map(m => m.TotalAllowanceAmount).Name("Sum of allowances").Optional();
                Map(m => m.CbcSumOfInvoiceLineNetAmount).Name("Sum of Invoice line net amount").Optional();
                Map(m => m.TotalFeeChargeAmount).Name("Total Fee / Charge Amount").Optional();
                Map(m => m.NetAmount).Name("Total Net Amount").Optional();
                Map(m => m.TotalTaxAmount).Name("Total Tax Amount").Optional();
                Map(m => m.TotalIncludingTax).Name("Total Including Tax").Optional();
                Map(m => m.PrePaidAmount).Name("Paid amount").Optional();
                Map(m => m.PayableRoundingAmount).Name("Rounding amount").Optional();
                Map(m => m.TotalPayableAmount).Name("Total Payable Amount").Optional();
                Map(m => m.TotalDiscountValue).Name("Total Discount Value").Optional();
                Map(m => m.InvoiceAdditionalDiscount).Name("Invoice Additional Discount").Optional();
                Map(m => m.InvoiceAdditionalFee).Name("Invoice Additional Fee").Optional();
                //// Map(m => m.SupplierContactPerson).Name("Supplier contact point");
                ///Map(m => m.TotalNumberOfInvoiceLines).Name("Total number of invoice lines");
                //// Map(m => m.CustomerAccount).Name("Customer Account");
                //// Map(m => m.BuyerFaxNo).Name("Buyer Fax No");
                //// Map(m => m.InvoicePreparedDate).Name("Invoice Prepared Date");
                ////   Map(m => m.InvoicePreparedTime).Name("Invoice Prepared Time");
                ////  Map(m => m.SupplierBankAccountName).Name("Supplier Bank Account Name");
                /////  Map(m => m.SupplierBankSWIFTCode).Name("Supplier Bank SWIFT Code");
                ////  Map(m => m.InvoiceLevelTaxSummary).Name("Invoice Level Tax Summary");
                ////  Map(m => m.InvoiceLevelTaxBaseAmountperTaxCode).Name("Invoice Level Tax Base Amount per Tax Code");
                //// Map(m => m.InvoiceLevelTaxAmountperTaxCode).Name("Invoice Level Tax Amount per Tax Code");
                ////  Map(m => m.InvoiceLevelTotalAmountDueperTaxCode).Name("Invoice Level Total Amount Due per Tax Code");
                // ... Add all the necessary columns

                // End Invoice Data map

                // Start InvoiceLineItems Map data 

                // Mapping CSV columns to model properties
                // Map(m => m.InvoiceId).Name("REFFNO");
                // Map(m => m.BPCode).Name("BP_CODE");
                Map(m => m.CbcItemClassificationClass).Name("Classification Name", "ClassificationClass").Optional();
                Map(m => m.CbcItemClassificationCode).Name("Classification", "ClassificationCode").Optional();
                Map(m => m.ProductId).Name("Product ID").Optional();
                Map(m => m.CbcDescription).Name("Description", "Description of Product or Service").Optional();
                Map(m => m.CbcProductTariffCode).Name("Product Tariff Code");
                Map(m => m.CbcIDItemCountryOfOrigin).Name("Country of Origin").Optional();
                Map(m => m.CbcPrice).Name("Unit Price").Optional();
                Map(m => m.CbcBaseQuantity).Name("Quantity").Optional();
                Map(m => m.CbcMeasure).Name("Measurement").Optional();
                Map(m => m.CbcSubtotal).Name("Subtotal").Optional();
                Map(m => m.TaxCategoryUNECE5153).Name("Tax Category UNECE5153").Optional();
                Map(m => m.TaxCategoryUNCL5305).Name("Tax Category UNCL5305").Optional();
                Map(m => m.CbcSSTTaxCategory).Name("SST Tax Category").Optional();
                Map(m => m.CbcTaxType).Name("Tax Type").Optional();
                Map(m => m.CbcTaxRate).Name("Tax Rate").Optional();
                Map(m => m.TaxRateApplicable).Name("Tax Rate Applicable").Optional();
                Map(m => m.CbcTaxAmount).Name("Tax Amount").Optional();
                Map(m => m.CbcTaxExemptionDetails).Name("Details of Tax Exemption").Optional();
                Map(m => m.CbcTaxExemptedAmount).Name("Amount Exempted from Tax").Optional();
                Map(m => m.CbcDiscountRate).Name("Discount Rate").Optional();
                Map(m => m.CbcDiscountAmount).Name("Discount Amount").Optional();
                Map(m => m.CbcTotalExcludingTax).Name("Total Excluding Tax").Optional();
                Map(m => m.CbcAllowanceReasonCode).Name("allowance reason code").Optional();
                Map(m => m.CbcAllowanceText).Name("allowance reason").Optional();
                Map(m => m.CbcAllowanceBaseAmount).Name("allowance base amount").Optional();
                Map(m => m.AllowancePercentage).Name("allowance percentage").Optional();
                Map(m => m.CbcAllowanceAmount).Name("allowance amount").Optional();
                Map(m => m.CbcChargeReasonCode).Name("charge reason code").Optional();
                Map(m => m.CbcChargeText).Name("charge reason").Optional();
                Map(m => m.CbcChargeBaseAmount).Name("charge base amount").Optional();
                Map(m => m.CbcChargeRate).Name("Fee / Charge Rate").Optional();
                Map(m => m.CbcChargeAmount).Name("Fee / Charge Amount").Optional();
                Map(m => m.CbcInvoiceLineNetAmount).Name("Invoice line net amount ", "Invoice line net amount").Optional();
                Map(m => m.CbcNetAmount).Name("Nett Amount").Optional();
                ////  Map(m => m.LineItemTaxCode).Name("Line Item Tax Code");

                // End InvoiceLineItems Map data 

                // Start DocTaxSubtotal Map data


                Map(m => m.CategoryTaxAmount).Name("TAX category tax amount", "TAX category tax amount in accounting currency").Optional();
                Map(m => m.TaxCatCodeForTaxAmount).Name("TAX cat code for tax amount").Optional();
                Map(m => m.CategoryTaxCategory).Name("Taxable Amount per tax type").Optional();
                Map(m => m.TaxAmountPerTaxType).Name("Tax Amount Per Tax Type", "Total Tax Amount Per Tax Type").Optional();
                Map(m => m.CategoryTaxRate).Name("TAX category rate");
                ////  Map(m => m.FrequencyOfBilling).Name("Frequency of Billing");
                ////   Map(m => m.BillingPeriodStartDate).Name("Billing Period Start Date");
                ////  Map(m => m.BillingPeriodEndDate).Name("Billing Period End Date");
                //// Map(m => m.RefNo).Name("REFFNO");

                /*     Map(m => m.DeliveryNote).Name("DELIVERY_NOTE");
                     Map(m => m.Term).Name("TERM");
                     Map(m => m.CustCode).Name("CUST_CODE");
                     Map(m => m.AccountNo).Name("ACCOUNTNO");
                     Map(m => m.ServiceCenter).Name("SERVICE_CENTER");
                     Map(m => m.ReqType).Name("REQ_TYPE");*/

                //  End DocTaxSubtotal Map data

            }            
        }
        //public class InvoiceLineItemDataMap : ClassMap<Invoice.InvoiceLineItems>
        //{
        //    public InvoiceLineItemDataMap()
        //    {
        //        // Mapping CSV columns to model properties
        //        // Map(m => m.InvoiceId).Name("REFFNO");
        //        // Map(m => m.BPCode).Name("BP_CODE");
        //        Map(m => m.CbcItemClassificationClass).Name("Classification Name");
        //        Map(m => m.CbcItemClassificationCode).Name("Classification");
        //        Map(m => m.ProductId).Name("Product ID");
        //        Map(m => m.CbcDescription).Name("Description");
        //        Map(m => m.CbcProductTariffCode).Name("Product Tariff Code");
        //        Map(m => m.CbcIDItemCountryOfOrigin).Name("Country of Origin");
        //        Map(m => m.CbcPrice).Name("Unit Price");
        //        Map(m => m.CbcBaseQuantity).Name("Quantity");
        //        Map(m => m.CbcMeasure).Name("Measurement");
        //        Map(m => m.CbcSubtotal).Name("Subtotal");
        //        //    Map(m => m.TaxCategoryUNECE5153).Name("Tax Category UNECE5153");
        //        // Map(m => m.TaxCategoryUNCL5305).Name("Tax Category UNCL5305");
        //        Map(m => m.CbcSSTTaxCategory).Name("SST Tax Category");
        //        Map(m => m.CbcTaxType).Name("Tax Type");
        //        Map(m => m.CbcTaxRate).Name("Tax Rate");
        //        //.  Map(m => m.TaxRateApplicable).Name("Tax Rate Applicable");
        //        Map(m => m.CbcTaxAmount).Name("Tax Amount");
        //        Map(m => m.CbcTaxExemptionDetails).Name("Details of Tax Exemption");
        //        Map(m => m.CbcTaxExemptedAmount).Name("Amount Exempted from Tax");
        //        Map(m => m.CbcDiscountRate).Name("Discount Rate");
        //        Map(m => m.CbcDiscountAmount).Name("Discount Amount");
        //        Map(m => m.CbcTotalExcludingTax).Name("Total Excluding Tax");
        //        Map(m => m.CbcAllowanceReasonCode).Name("allowance reason code");
        //        Map(m => m.CbcAllowanceText).Name("allowance reason");
        //        Map(m => m.CbcAllowanceBaseAmount).Name("allowance base amount");
        //        //     Map(m => m.AllowancePercentage).Name("allowance percentage");
        //        Map(m => m.CbcAllowanceAmount).Name("allowance amount");
        //        Map(m => m.CbcChargeReasonCode).Name("charge reason code");
        //        Map(m => m.CbcChargeText).Name("charge reason");
        //        Map(m => m.CbcChargeBaseAmount).Name("charge base amount");
        //        Map(m => m.CbcChargeAmount).Name("Fee / Charge Rate");
        //        Map(m => m.CbcChargeAmount).Name("Fee / Charge Amount");
        //        Map(m => m.CbcInvoiceLineNetAmount).Name("Invoice line net amount");
        //        Map(m => m.CbcNetAmount).Name("Nett Amount");
        //    }
        //}
        //public class DocTaxSubTotalMap : ClassMap<Invoice.DocTaxSubTotal>
        //{
        //    public DocTaxSubTotalMap()
        //    {
        //        Map(m => m.CategoryTaxAmount).Name("TAX category tax amount");
        //        //    Map(m => m.TaxCatCodeForTaxAmount).Name("TAX cat code for tax amount");
        //        Map(m => m.CategoryTaxCategory).Name("Taxable Amount per tax type");
        //        //      Map(m => m.TaxAmountPerTaxType).Name("Tax Amount Per Tax Type");
        //        Map(m => m.CategoryTaxRate).Name("TAX category rate");
        //        /*     Map(m => m.DeliveryNote).Name("DELIVERY_NOTE");
        //             Map(m => m.Term).Name("TERM");
        //             Map(m => m.CustCode).Name("CUST_CODE");
        //             Map(m => m.AccountNo).Name("ACCOUNTNO"); 
        //             Map(m => m.ServiceCenter).Name("SERVICE_CENTER");
        //             Map(m => m.ReqType).Name("REQ_TYPE");*/
        //    }
        //}
    }
    public class FlexibleDateTimeConverter : ITypeConverter
    {
        private static readonly string[] DateFormats = new[]
        {
        "yyyy-dd-MM",   // Year-Day-Month
        "dd-MM-yyyy",   // Day-Month-Year            
        "MM-dd-yyyy",   // Month-Day-Year
        "dd/MM/yyyy",   // Alternative separators
        "yyyy/dd/MM",
        "MM/dd/yyyy",
        "dd.MM.yyyy",   // Dot separator
        "yyyy.dd.MM",
        "MM.dd.yyyy"
    };

        public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            DateTime result;

            // If text is null or empty, return null
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            // Try to parse the date with the given formats
            foreach (var format in DateFormats)
            {
                if (DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                {
                    return result; // Successfully parsed, return the DateTime
                }
            }

            throw new FormatException($"Date string '{text}' did not match any of the expected formats.");
        }

        public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            // If the value is null, return an empty string
            if (value == null)
            {
                return string.Empty;
            }

            // Assuming value is of DateTime type
            DateTime dateTime = (DateTime)value;

            // You can choose the format you want to store or display
            return dateTime.ToString("yyyy-MM-dd"); // Example format
        }
    }



    public class CSVFieldConfiguration
    {
        public int id { get; set; }
        public string CSVFieldName { get; set; }
        public string TableFieldName { get; set; }
        public string status { get; set; }
        public string TableName { get; set; }

    }
}


