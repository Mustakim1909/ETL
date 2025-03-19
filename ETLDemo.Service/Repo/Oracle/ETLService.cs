using Common.Config;
using Common.DataAccess.Oracle;
using Common.Security;
using ETL.Service.Model;
using ETL.Service.Repo.Interface;
using ETL_Demo.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETL.Service.Repo.Oracle
{
    public class ETLService : IETLService
    {
        private readonly QueryHelper _queryHelper = null;
        private readonly string _connectionString = null;
        public ETLService(DbConfig dbConfig)
        {
            var encPassword = string.Empty;
            encPassword = dbConfig.ConnectionString.Split(';')[3].Substring(10);
            var password = SecurityHelper.DecryptWithEmbedKey(encPassword);
            dbConfig.ConnectionString = dbConfig.ConnectionString.Replace(encPassword, password);
            _connectionString = dbConfig.ConnectionString;
            _queryHelper = new QueryHelper(dbConfig.ConnectionString);
        }

        public Task<int> ExecStorProcForInsert(List<InvoiceData> invoicedatajson, List<List<InvoiceLineItems>> invoicelineitemsjson, List<DocTaxSubTotal> doctaxsubtotaljson)
        {
            throw new NotImplementedException();
        }

        public Task<int> ExecStoreProc(List<InvoiceData> invoicedata, List<List<InvoiceLineItems>> invoicelineitemsjson, List<DocTaxSubTotal> doctaxsubtotaljson, List<string> filepath)
        {
            throw new NotImplementedException();
        }

        public Task<TenantDetails> GetConnectionString(string domain)
        {
            throw new NotImplementedException();
        }

        public Task<List<InvoiceData>> GetInvoiceData(string einvoicenumber, string invoicetypecode)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceData> InsertCreditNoteData(InvoiceData invoiceData)
        {
            throw new NotImplementedException();
        }

        public Task<DocTaxSubTotal> InsertCreditNoteDocTaxSubTotal(DocTaxSubTotal docTaxSubTotal)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceLineItems> InsertCreditNotelineData(InvoiceLineItems invoiceLineItems)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceData> InsertDebitNoteData(InvoiceData invoiceData)
        {
            throw new NotImplementedException();
        }

        public Task<DocTaxSubTotal> InsertDebitNoteDocTaxSubTotal(DocTaxSubTotal docTaxSubTotal)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceLineItems> InsertDebitNotelineData(InvoiceLineItems invoiceLineItems)
        {
            throw new NotImplementedException();
        }

        public Task<DocTaxSubTotal> InsertDocTaxSubTotal(DocTaxSubTotal docTaxSubTotal)
        {
            throw new NotImplementedException();
        }

        public Task<ETLProcess> InsertETLProcess(ETLProcess invoiceProcess)
        {
            throw new NotImplementedException();
        }

        public Task<ETLStatus> InsertETLStatus(ETLStatus eTLStatus)
        {
            throw new NotImplementedException();
        }

        public Task InsertInvoiceData(string invoiceCsvFile)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceData> InsertInvoiceData(InvoiceData invoiceData)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceLineItems> InsertInvoicelineData(InvoiceLineItems invoiceLineItems)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceData> InsertRefundNoteData(InvoiceData invoiceData)
        {
            throw new NotImplementedException();
        }

        public Task<DocTaxSubTotal> InsertRefundNoteDocTaxSubTotal(DocTaxSubTotal docTaxSubTotal)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceLineItems> InsertRefundNotelineData(InvoiceLineItems invoiceLineItems)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceData> InsertSBCreditNoteData(InvoiceData invoiceData)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceLineItems> InsertSBCreditNotelineData(InvoiceLineItems invoiceLineItems)
        {
            throw new NotImplementedException();
        }

        public Task<DocTaxSubTotal> InsertSBCreditNoteTaxSubTotal(DocTaxSubTotal docTaxSubTotal)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceData> InsertSBDebitNoteData(InvoiceData invoiceData)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceLineItems> InsertSBDebitNotelineData(InvoiceLineItems invoiceLineItems)
        {
            throw new NotImplementedException();
        }

        public Task<DocTaxSubTotal> InsertSBDebitNoteTaxSubTotal(DocTaxSubTotal docTaxSubTotal)
        {
            throw new NotImplementedException();
        }

        public Task<DocTaxSubTotal> InsertSBDocTaxSubTotal(DocTaxSubTotal docTaxSubTotal)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceData> InsertSBInvoiceData(InvoiceData invoiceData)
        {
            throw new NotImplementedException();
        }

        public Task<DocTaxSubTotal> InsertSBInvoiceDocTaxSubTotal(DocTaxSubTotal docTaxSubTotal)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceLineItems> InsertSBInvoicelineData(InvoiceLineItems invoiceLineItems)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceData> InsertSBRefundNoteData(InvoiceData invoiceData)
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceLineItems> InsertSBRefundNotelineData(InvoiceLineItems invoiceLineItems)
        {
            throw new NotImplementedException();
        }

        public Task<DocTaxSubTotal> InsertSBRefundNoteTaxSubTotal(DocTaxSubTotal docTaxSubTotal)
        {
            throw new NotImplementedException();
        }

        public Task SetConnectionString(string connectionstring)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateInvoiceDataStatus(string Status, int Id, string InvoiceCode)
        {
            throw new NotImplementedException();
        }

        public Task<int> ValidateETL(List<InvoiceData> invoicedatajson, List<List<InvoiceLineItems>> invoicelineitemsjson)
        {
            throw new NotImplementedException();
        }

        public Task<int> InsertStoreProc(List<InvoiceData> invoicedata, List<string> filepath)
        {
            throw new NotImplementedException();
        }

        public Task<int> TempInsertStoreProc(List<InvoiceData> invoicedata)
        {
            throw new NotImplementedException();
        }

        public Task<int> InsertInvData(string InvoiceNumber, List<string> filepath)
        {
            throw new NotImplementedException();
        }

        public Task<int> TempInsertStoreProc2(string InvoiceNumber, string TotalAmount, string TotalLines)
        {
            throw new NotImplementedException();
        }

        public Task<int> InsertInvoiceData(string InvoiceNumber,  string TotalAmount, string TotalLines, List<string> filepath)
        {
            throw new NotImplementedException();
        }

        public Task<List<CsvFieldConfiguration>> GetInvoiceMappingColumns()
        {
            throw new NotImplementedException();
        }
    }
}
