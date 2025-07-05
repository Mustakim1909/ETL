using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETL.Service.Model
{
    public class DocumentPDFModel
    {

    }

    public class INTELInvLPDFModel
    {
        public string AccountNumber { get; set; }
        public string SettlingDate { get; set; }
    }
    public class INTELLineLPDFModel
    {
        public string LabNo { get; set; }
        public string MRNNo { get; set; }
        public string HVANo { get; set; }
        public string PatientName { get; set; }
        public string Register { get; set; }
    }
    public class INSUNInvLPDFModel
    {
        public string AccountNo { get; set; }
        public string CreditLimit { get; set; }
        public string CreditTerm { get; set; }
        public string SalesPerson { get; set; }
        public string IssuedBy { get; set; }
        public string TotalForInvoice { get; set; }

    }

    public class INSUNLineLPDFModel
    {
        public string ItemCode { get; set; }
        public string TaxCode { get; set; }
    } 
    public class CNTELInvLPDFModel
    {
        public string AccountNo { get; set; }
        public string OriginalInvoiceDate { get; set; }
        public string CreditLimit { get; set; }
        public string CreditTerm { get; set; }
        public string SalesPerson { get; set; }
        public string IssuedBy { get; set; }
        public string TotalForInvoice { get; set; }
        public string ReasonForCN { get; set; }

    }

    public class CNTELLineLPDFModel
    {
        public string ItemCode { get; set; }
        public string TaxCode { get; set; }
    } 
    public class CNSUNInvLPDFModel
    {
        public string AccountNo { get; set; }
        public string OriginalInvoiceDate { get; set; }
        public string CreditLimit { get; set; }
        public string CreditTerm { get; set; }
        public string SalesPerson { get; set; }
        public string IssuedBy { get; set; }
        public string TotalForInvoice { get; set; }
        public string ReasonForCN { get; set; }

    }

    public class CNSUNLineLPDFModel
    {
        public string ItemCode { get; set; }
        public string TaxCode { get; set; }
    } 
    
    public class DNTELInvLPDFModel
    {
        public string AccountNo { get; set; }
        public string OriginalInvoiceDate { get; set; }
        public string CreditLimit { get; set; }
        public string CreditTerm { get; set; }
        public string SalesPerson { get; set; }
        public string IssuedBy { get; set; }
        public string TotalForInvoice { get; set; }
        public string ReasonForDN { get; set; }

    }

    public class DNTELLineLPDFModel
    {
        public string ItemCode { get; set; }
        public string TaxCode { get; set; }
    } 
    public class DNSUNInvLPDFModel
    {
        public string AccountNo { get; set; }
        public string OriginalInvoiceDate { get; set; }
        public string CreditLimit { get; set; }
        public string CreditTerm { get; set; }
        public string SalesPerson { get; set; }
        public string IssuedBy { get; set; }
        public string TotalForInvoice { get; set; }
        public string ReasonForDN { get; set; }

    }

    public class DNSUNLineLPDFModel
    {
        public string ItemCode { get; set; }
        public string TaxCode { get; set; }
    }

    public class SBINSUNInvLPDFModel
    {
        public string AccountNo { get; set; }
        public string CreditLimit { get; set; }
        public string CreditTerm { get; set; }
        public string SalesPerson { get; set; }
        public string IssuedBy { get; set; }
        public string TotalForInvoice { get; set; }

    }

    public class SBINSUNLineLPDFModel
    {
        public string ItemCode { get; set; }
        public string TaxCode { get; set; }
    }

    public class SBCNSUNInvLPDFModel
    {
        public string AccountNo { get; set; }
        public string OriginalInvoiceDate { get; set; }
        public string CreditLimit { get; set; }
        public string CreditTerm { get; set; }
        public string SalesPerson { get; set; }
        public string IssuedBy { get; set; }
        public string TotalForInvoice { get; set; }
        public string ReasonForCN { get; set; }

    }

    public class SBCNSUNLineLPDFModel
    {
        public string ItemCode { get; set; }
        public string TaxCode { get; set; }
    }
    public class SBDNSUNInvLPDFModel
    {
        public string AccountNo { get; set; }
        public string OriginalInvoiceDate { get; set; }
        public string CreditLimit { get; set; }
        public string CreditTerm { get; set; }
        public string SalesPerson { get; set; }
        public string IssuedBy { get; set; }
        public string TotalForInvoice { get; set; }
        public string ReasonForDN { get; set; }

    }

    public class SBDNSUNLineLPDFModel
    {
        public string ItemCode { get; set; }
        public string TaxCode { get; set; }
    }
}
