using ETL.Service.Model;
using ETL.Service.Repo.Interface;
using ETL_Demo.Models;
using ETLDEMO.ETLHelperProcess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETL.ETLHelperProcess
{
    public class PDFMappingService
    {
        private readonly ETLHelper _eTLHelper;
        private readonly IETLService _etlDemoService;
        public PDFMappingService(ETLHelper eTLHelper, IETLService eTLDemoService)
        {
            _eTLHelper = eTLHelper;
            _etlDemoService = eTLDemoService;
        }
        #region INSUN
        public async Task<INSUNInvLPDFModel> INSUNInvLMapping(List<InvoiceCSVData> invoiceCSVData)
        {


            var INSUNmap = new INSUNInvLPDFModel()
            {
                AccountNo = invoiceCSVData.Select(x => x.AccountNo).FirstOrDefault(),
                CreditLimit = invoiceCSVData.Select(x => x.CreditLimit).FirstOrDefault(),
                CreditTerm = invoiceCSVData.Select(x => x.CreditTerm).FirstOrDefault(),
                IssuedBy = invoiceCSVData.Select(x => x.IssuedBy).FirstOrDefault(),
                SalesPerson = invoiceCSVData.Select(x => x.SalesPerson).FirstOrDefault(),
                TotalForInvoice = invoiceCSVData.Select(x => x.TotalForInvoice).FirstOrDefault(),
            };
            return INSUNmap;
        }
        public async Task<List<INSUNLineLPDFModel>> INSUNLineMapping(List<InvoiceCSVData> invoiceCSVData)
        {

            var insunlist=invoiceCSVData.ToList();
            var INSUNmap = insunlist.Select(itemline => new INSUNLineLPDFModel
            {
                ItemCode = itemline.ItemCode,
                TaxCode = itemline.TaxCode
            }).ToList();
            return INSUNmap;
        }
        #endregion INSUN
        #region INTEL
        public async Task<INTELInvLPDFModel> INTELInvLMapping(List<InvoiceCSVData> invoiceCSVData)
        {


            var INSUNmap = new INTELInvLPDFModel()
            {
                AccountNumber = invoiceCSVData.Select(x => x.AccountNumber).FirstOrDefault(),
                SettlingDate = invoiceCSVData.Select(x => x.SettlingDate).FirstOrDefault()
            };
            return INSUNmap;
        }
        public async Task<List<INTELLineLPDFModel>> INTELLineMapping(List<InvoiceCSVData> invoiceCSVData)
        {

            var insunlist = invoiceCSVData.ToList();
            var INSUNmap = insunlist.Select(itemline => new INTELLineLPDFModel
            {
               LabNo = itemline.LabNo,
               HVANo = itemline.HVANo,
               MRNNo = itemline.MRNNo,
               PatientName = itemline.PatientName,
               Register = itemline.Register,
            }).ToList();
            return INSUNmap;
        }
        #endregion INTEL

        #region CNTEL
        public async Task<CNTELInvLPDFModel> CNTELInvLMapping(List<InvoiceCSVData> invoiceCSVData)
        {


            var CNTELmap = new CNTELInvLPDFModel()
            {
                AccountNo = invoiceCSVData.Select(x => x.AccountNo).FirstOrDefault(),
                OriginalInvoiceDate = invoiceCSVData.Select(x => x.OriginalInvoiceDate).FirstOrDefault(),
                CreditLimit = invoiceCSVData.Select(x => x.CreditLimit).FirstOrDefault(),
                CreditTerm = invoiceCSVData.Select(x => x.CreditTerm).FirstOrDefault(),
                IssuedBy = invoiceCSVData.Select(x => x.IssuedBy).FirstOrDefault(),
                SalesPerson = invoiceCSVData.Select(x => x.SalesPerson).FirstOrDefault(),
                TotalForInvoice = invoiceCSVData.Select(x => x.TotalForInvoice).FirstOrDefault(),
                ReasonForCN = invoiceCSVData.Select(x => x.ReasonforCN).FirstOrDefault()
            };
            return CNTELmap;
        }
        public async Task<List<CNTELLineLPDFModel>> CNTELLineMapping(List<InvoiceCSVData> invoiceCSVData)
        {

            var cntellist = invoiceCSVData.ToList();
            var CNTELmap = cntellist.Select(itemline => new CNTELLineLPDFModel
            {
                ItemCode = itemline.ItemCode,
                TaxCode = itemline.TaxCode
            }).ToList();
            return CNTELmap;
        }
        #endregion CNTEL  
        
        #region CNSUN
        public async Task<CNSUNInvLPDFModel> CNSUNInvLMapping(List<InvoiceCSVData> invoiceCSVData)
        {


            var CNSUNmap = new CNSUNInvLPDFModel()
            {
                AccountNo = invoiceCSVData.Select(x => x.AccountNo).FirstOrDefault(),
                OriginalInvoiceDate = invoiceCSVData.Select(x => x.OriginalInvoiceDate).FirstOrDefault(),
                CreditLimit = invoiceCSVData.Select(x => x.CreditLimit).FirstOrDefault(),
                CreditTerm = invoiceCSVData.Select(x => x.CreditTerm).FirstOrDefault(),
                IssuedBy = invoiceCSVData.Select(x => x.IssuedBy).FirstOrDefault(),
                SalesPerson = invoiceCSVData.Select(x => x.SalesPerson).FirstOrDefault(),
                TotalForInvoice = invoiceCSVData.Select(x => x.TotalForInvoice).FirstOrDefault(),
                ReasonForCN = invoiceCSVData.Select(x => x.ReasonforCN).FirstOrDefault()
            };
            return CNSUNmap;
        }
        public async Task<List<CNSUNLineLPDFModel>> CNSUNLineMapping(List<InvoiceCSVData> invoiceCSVData)
        {

            var cnsunlist = invoiceCSVData.ToList();
            var CNSUNmap = cnsunlist.Select(itemline => new CNSUNLineLPDFModel
            {
                ItemCode = itemline.ItemCode,
                TaxCode = itemline.TaxCode
            }).ToList();
            return CNSUNmap;
        }
        #endregion CNSUN 
        
        #region DNTEL
        public async Task<DNTELInvLPDFModel> DNTELInvLMapping(List<InvoiceCSVData> invoiceCSVData)
        {


            var DNTELmap = new DNTELInvLPDFModel()
            {
                AccountNo = invoiceCSVData.Select(x => x.AccountNo).FirstOrDefault(),
                OriginalInvoiceDate = invoiceCSVData.Select(x => x.OriginalInvoiceDate).FirstOrDefault(),
                CreditLimit = invoiceCSVData.Select(x => x.CreditLimit).FirstOrDefault(),
                CreditTerm = invoiceCSVData.Select(x => x.CreditTerm).FirstOrDefault(),
                IssuedBy = invoiceCSVData.Select(x => x.IssuedBy).FirstOrDefault(),
                SalesPerson = invoiceCSVData.Select(x => x.SalesPerson).FirstOrDefault(),
                TotalForInvoice = invoiceCSVData.Select(x => x.TotalForInvoice).FirstOrDefault(),
                ReasonForDN = invoiceCSVData.Select(x => x.ReasonForDN).FirstOrDefault()
            };
            return DNTELmap;
        }
        public async Task<List<DNTELLineLPDFModel>> DNTELLineMapping(List<InvoiceCSVData> invoiceCSVData)
        {

            var dntellist = invoiceCSVData.ToList();
            var DNTELmap = dntellist.Select(itemline => new DNTELLineLPDFModel
            {
                ItemCode = itemline.ItemCode,
                TaxCode = itemline.TaxCode
            }).ToList();
            return DNTELmap;
        }
        #endregion DNTEL  
        
        #region DNSUN
        public async Task<DNSUNInvLPDFModel> DNSUNInvLMapping(List<InvoiceCSVData> invoiceCSVData)
        {


            var DNSUNmap = new DNSUNInvLPDFModel()
            {
                AccountNo = invoiceCSVData.Select(x => x.AccountNo).FirstOrDefault(),
                OriginalInvoiceDate = invoiceCSVData.Select(x => x.OriginalInvoiceDate).FirstOrDefault(),
                CreditLimit = invoiceCSVData.Select(x => x.CreditLimit).FirstOrDefault(),
                CreditTerm = invoiceCSVData.Select(x => x.CreditTerm).FirstOrDefault(),
                IssuedBy = invoiceCSVData.Select(x => x.IssuedBy).FirstOrDefault(),
                SalesPerson = invoiceCSVData.Select(x => x.SalesPerson).FirstOrDefault(),
                TotalForInvoice = invoiceCSVData.Select(x => x.TotalForInvoice).FirstOrDefault(),
                ReasonForDN = invoiceCSVData.Select(x => x.ReasonForDN).FirstOrDefault()
            };
            return DNSUNmap;
        }
        public async Task<List<DNSUNLineLPDFModel>> DNSUNLineMapping(List<InvoiceCSVData> invoiceCSVData)
        {

            var dnsunlist = invoiceCSVData.ToList();
            var DNSUNmap = dnsunlist.Select(itemline => new DNSUNLineLPDFModel
            {
                ItemCode = itemline.ItemCode,
                TaxCode = itemline.TaxCode
            }).ToList();
            return DNSUNmap;
        }
        #endregion DNSUN

        #region SBINSUN
        public async Task<SBINSUNInvLPDFModel> SBINSUNInvLMapping(List<InvoiceCSVData> invoiceCSVData)
        {


            var SBINSUNmap = new SBINSUNInvLPDFModel()
            {
                AccountNo = invoiceCSVData.Select(x => x.AccountNo).FirstOrDefault(),
                CreditLimit = invoiceCSVData.Select(x => x.CreditLimit).FirstOrDefault(),
                CreditTerm = invoiceCSVData.Select(x => x.CreditTerm).FirstOrDefault(),
                IssuedBy = invoiceCSVData.Select(x => x.IssuedBy).FirstOrDefault(),
                SalesPerson = invoiceCSVData.Select(x => x.SalesPerson).FirstOrDefault(),
                TotalForInvoice = invoiceCSVData.Select(x => x.TotalForInvoice).FirstOrDefault(),
            };
            return SBINSUNmap;
        }
        public async Task<List<SBINSUNLineLPDFModel>> SBINSUNLineMapping(List<InvoiceCSVData> invoiceCSVData)
        {

            var sbinsunlist = invoiceCSVData.ToList();
            var SBINSUNmap = sbinsunlist.Select(itemline => new SBINSUNLineLPDFModel
            {
                ItemCode = itemline.ItemCode,
                TaxCode = itemline.TaxCode
            }).ToList();
            return SBINSUNmap;
        }
        #endregion SBINSUN

        #region SBCNSUN
        public async Task<SBCNSUNInvLPDFModel> SBCNSUNInvLMapping(List<InvoiceCSVData> invoiceCSVData)
        {


            var SBCNSUNmap = new SBCNSUNInvLPDFModel()
            {
                AccountNo = invoiceCSVData.Select(x => x.AccountNo).FirstOrDefault(),
                OriginalInvoiceDate = invoiceCSVData.Select(x => x.OriginalInvoiceDate).FirstOrDefault(),
                CreditLimit = invoiceCSVData.Select(x => x.CreditLimit).FirstOrDefault(),
                CreditTerm = invoiceCSVData.Select(x => x.CreditTerm).FirstOrDefault(),
                IssuedBy = invoiceCSVData.Select(x => x.IssuedBy).FirstOrDefault(),
                SalesPerson = invoiceCSVData.Select(x => x.SalesPerson).FirstOrDefault(),
                TotalForInvoice = invoiceCSVData.Select(x => x.TotalForInvoice).FirstOrDefault(),
                ReasonForCN = invoiceCSVData.Select(x => x.ReasonforCN).FirstOrDefault()
            };
            return SBCNSUNmap;
        }
        public async Task<List<SBCNSUNLineLPDFModel>> SBCNSUNLineMapping(List<InvoiceCSVData> invoiceCSVData)
        {

            var sbcnsunlist = invoiceCSVData.ToList();
            var SBCNSUNmap = sbcnsunlist.Select(itemline => new SBCNSUNLineLPDFModel
            {
                ItemCode = itemline.ItemCode,
                TaxCode = itemline.TaxCode
            }).ToList();
            return SBCNSUNmap;
        }
        #endregion SBCNSUN 

        #region SBDNSUN
        public async Task<SBDNSUNInvLPDFModel> SBDNSUNInvLMapping(List<InvoiceCSVData> invoiceCSVData)
        {


            var SBDNSUNmap = new SBDNSUNInvLPDFModel()
            {
                AccountNo = invoiceCSVData.Select(x => x.AccountNo).FirstOrDefault(),
                OriginalInvoiceDate = invoiceCSVData.Select(x => x.OriginalInvoiceDate).FirstOrDefault(),
                CreditLimit = invoiceCSVData.Select(x => x.CreditLimit).FirstOrDefault(),
                CreditTerm = invoiceCSVData.Select(x => x.CreditTerm).FirstOrDefault(),
                IssuedBy = invoiceCSVData.Select(x => x.IssuedBy).FirstOrDefault(),
                SalesPerson = invoiceCSVData.Select(x => x.SalesPerson).FirstOrDefault(),
                TotalForInvoice = invoiceCSVData.Select(x => x.TotalForInvoice).FirstOrDefault(),
                ReasonForDN = invoiceCSVData.Select(x => x.ReasonForDN).FirstOrDefault()
            };
            return SBDNSUNmap;
        }
        public async Task<List<SBDNSUNLineLPDFModel>> SBDNSUNLineMapping(List<InvoiceCSVData> invoiceCSVData)
        {

            var sbdnsunlist = invoiceCSVData.ToList();
            var SBDNSUNmap = sbdnsunlist.Select(itemline => new SBDNSUNLineLPDFModel
            {
                ItemCode = itemline.ItemCode,
                TaxCode = itemline.TaxCode
            }).ToList();
            return SBDNSUNmap;
        }
        #endregion DNSUN

    }
}
