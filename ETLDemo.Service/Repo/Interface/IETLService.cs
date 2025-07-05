using ETL.Service.Model;
using ETL_Demo.Models;
using System.Data;

namespace ETL.Service.Repo.Interface
{
    public interface IETLService
    {
        Task<InvoiceData> InsertInvoiceData(InvoiceData invoiceData);
        Task<InvoiceLineItems> InsertInvoicelineData(InvoiceLineItems invoiceLineItems);
        Task<DocTaxSubTotal> InsertDocTaxSubTotal(DocTaxSubTotal docTaxSubTotal);
        Task<InvoiceData> InsertCreditNoteData(InvoiceData invoiceData);
        Task<InvoiceLineItems> InsertCreditNotelineData(InvoiceLineItems invoiceLineItems);
        Task<DocTaxSubTotal> InsertCreditNoteDocTaxSubTotal(DocTaxSubTotal docTaxSubTotal);
        Task<InvoiceData> InsertDebitNoteData(InvoiceData invoiceData);
        Task<InvoiceLineItems> InsertDebitNotelineData(InvoiceLineItems invoiceLineItems);
        Task<DocTaxSubTotal> InsertDebitNoteDocTaxSubTotal(DocTaxSubTotal docTaxSubTotal);
        Task<InvoiceData> InsertRefundNoteData(InvoiceData invoiceData);
        Task<InvoiceLineItems> InsertRefundNotelineData(InvoiceLineItems invoiceLineItems);
        Task<DocTaxSubTotal> InsertRefundNoteDocTaxSubTotal(DocTaxSubTotal docTaxSubTotal);
        Task<InvoiceData> InsertSBInvoiceData(InvoiceData invoiceData);
        Task<InvoiceLineItems> InsertSBInvoicelineData(InvoiceLineItems invoiceLineItems);
        Task<DocTaxSubTotal> InsertSBInvoiceDocTaxSubTotal(DocTaxSubTotal docTaxSubTotal);
        Task<InvoiceData> InsertSBCreditNoteData(InvoiceData invoiceData);
        Task<InvoiceLineItems> InsertSBCreditNotelineData(InvoiceLineItems invoiceLineItems);
        Task<DocTaxSubTotal> InsertSBCreditNoteTaxSubTotal(DocTaxSubTotal docTaxSubTotal);
        Task<InvoiceData> InsertSBDebitNoteData(InvoiceData invoiceData);
        Task<InvoiceLineItems> InsertSBDebitNotelineData(InvoiceLineItems invoiceLineItems);
        Task<DocTaxSubTotal> InsertSBDebitNoteTaxSubTotal(DocTaxSubTotal docTaxSubTotal);
        Task<InvoiceData> InsertSBRefundNoteData(InvoiceData invoiceData);
        Task<InvoiceLineItems> InsertSBRefundNotelineData(InvoiceLineItems invoiceLineItems);
        Task<DocTaxSubTotal> InsertSBRefundNoteTaxSubTotal(DocTaxSubTotal docTaxSubTotal);
        Task<List<InvoiceData>> GetInvoiceData(string einvoicenumber, string invoicetypecode);
        Task<ETLProcess> InsertETLProcess(ETLProcess invoiceProcess);
        Task<ETLStatus> InsertETLStatus(ETLStatus eTLStatus);
        Task<bool> UpdateInvoiceDataStatus(string Status, int Id, string InvoiceCode);
        Task<TenantDetails> GetConnectionString(string domain);
        Task SetConnectionString(string connectionstring);
        Task<int> ExecStorProcForInsert(List<InvoiceData> invoicedatajson, List<List<InvoiceLineItems>> lineitemsjson,List<DocTaxSubTotal> doctaxsubtotaljson);
        Task<int> ExecStoreProc(List<InvoiceData> invoicedata, List<List<InvoiceLineItems>> invoicelineitemsjson, List<DocTaxSubTotal> doctaxsubtotaljson,  List<string> filepath);
        Task<int> InsertStoreProc(List<InvoiceData> invoicedata, List<string> filepath);
        Task<int> ValidateETL(List<InvoiceData> invoicedatajson, List<List<InvoiceLineItems>> invoicelineitemsjson);
        Task<int> TempInsertStoreProc(List<InvoiceData> invoicedata, string documentType,object invfields,object invlinefields,string invoicetypecode);
        Task<int> InsertInvData(string InvoiceNumber, List<string> filepath, string documentType, string invoicetypecode);
        Task<int> TempInsertStoreProc2(string InvoiceNumber, string TotalAmount, string TotalLines, string documentType, string invoicetypecode);
        Task<int> InsertInvoiceData(string InvoiceNumber, string TotalAmount, string TotalLines, List<string> filepath, string documentType, string invoicetypecode);
        Task<List<CsvFieldConfiguration>> GetInvoiceMappingColumns(string documentType);
    }
}
 