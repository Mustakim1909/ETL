using Common.Config;
using Common.DataAccess.PostgreSql;
using Common.Security;
using ETL.Service.Model;
using ETL.Service.Repo.Interface;
using ETL_Demo.Models;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ETL.Service.Repo.MSSQL.ETLService;

namespace ETL.Service.Repo.PostgreSql
{
    public class ETLService : IETLService
    {
        private QueryHelper _queryHelper = null;
        private string _connectionString = null;
        public int count = 0;
        public ETLService(DbConfig dbConfig)    
        {
            //_queryHelper = new QueryHelper(databaseConfig.ConnectionString);
            var initialConnectionString = ConnectionStringManager.IsConnectionStringCached()
                                    ? ConnectionStringManager.GetConnectionString()
                                    : dbConfig.ConnectionString;
            _connectionString = initialConnectionString;
            _queryHelper = new QueryHelper(initialConnectionString);
        }

        public Task<int> ExecStorProcForInsert(List<InvoiceData> invoicedatajson, List<List<InvoiceLineItems>> invoicelineitemsjson, List<DocTaxSubTotal> doctaxsubtotaljson)
        {
            
            Log.Information("ExecStorProcForInsert service called.");
            Console.WriteLine("ExecStorProcForInsert service called.");
            var invoicelineitemsJsonString = JsonConvert.SerializeObject(invoicelineitemsjson);

            var invoicedatajsonstring = JsonConvert.SerializeObject(invoicedatajson);
            var lineitemjsonstring = JsonConvert.SerializeObject(invoicelineitemsJsonString);
            var doctaxjsonstring = JsonConvert.SerializeObject(doctaxsubtotaljson);
            var paramName1 = "invoicedata";
            var paramName2 = "invoicelineitems";
            var paramName3 = "doctaxsubtotal";
            var parameters1 = new List<NpgsqlParameter>
                              {
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName1, invoicedatajsonstring, NpgsqlDbType.Json),
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName2, invoicelineitemsJsonString, NpgsqlDbType.Json),
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName3, doctaxjsonstring, NpgsqlDbType.Json),
                               };

            var exec = _queryHelper.ExecuteStoredProc("staging_insert", parameters1);
            count++;
            Console.WriteLine($"Count = {count}");
            return exec;
        }

        public Task<int> ExecStoreProc(List<InvoiceData> invoicedatajson, List<List<InvoiceLineItems>> invoicelineitemsjson, List<DocTaxSubTotal> doctaxsubtotaljson, List<string> filepath)
        {
            
            var invoicelineitemsJsonString = JsonConvert.SerializeObject(invoicelineitemsjson);
            var invoicedatajsonstring = JsonConvert.SerializeObject(invoicedatajson);
            var lineitemjsonstring = JsonConvert.SerializeObject(invoicelineitemsJsonString);
            var doctaxjsonstring = JsonConvert.SerializeObject(doctaxsubtotaljson);
            var filepathjson = JsonConvert.SerializeObject(filepath);
            var paramName1 = "invoicedata";
            var paramName2 = "invoicelineitems";
            var paramName3 = "doctaxsubtotal";
            var paramName4 = "filepath";
            var parameters1 = new List<NpgsqlParameter>
                              {
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName1, invoicedatajsonstring, NpgsqlDbType.Json),
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName2, invoicelineitemsJsonString, NpgsqlDbType.Json),
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName3, doctaxjsonstring, NpgsqlDbType.Json),
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName4, filepathjson, NpgsqlDbType.Json)
                               };
            var exec = _queryHelper.ExecuteStoredProc("Validation", parameters1);
            return exec;
        }

        public Task<int> InsertStoreProc(List<InvoiceData> invoicedata, List<string> filepath)
        {
           var invoicedatajson = JsonConvert.SerializeObject(invoicedata);
            var filepathjson = JsonConvert.SerializeObject(filepath);
            var paramName1 = "invoicedata";
            var paramName2 = "filepath";
            var parameters1 = new List<NpgsqlParameter>
                              {
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName1, invoicedatajson, NpgsqlDbType.Json),
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName2, filepathjson, NpgsqlDbType.Json)
                               };
            var exec = _queryHelper.ExecuteStoredProc("insert_invoices", parameters1);
            count++;
            Console.WriteLine($"Count = {count}");
            return exec;
        }

        public async Task<int> TempInsertStoreProc(List<InvoiceData> invoicedata)
        {
            int totalCount = 0; // Initialize the total counter
            int chunkSize = 1000; // Adjust the chunk size as needed

            // Define a retry policy
            var retryPolicy = Policy
                .Handle<NpgsqlException>() // Retry on database connection issues
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} sec due to: {exception.Message}");
                    });

            // Process each invoice in the list
            foreach (var invoice in invoicedata)
            {
                int invoiceCount = 0; // Counter for the current invoice

                // Split the InvoiceLineItems into smaller chunks
                var lineItemsChunks = invoice.InvoiceLineItems
                    .Select((x, i) => new { Index = i, Value = x })
                    .GroupBy(x => x.Index / chunkSize)
                    .Select(x => x.Select(v => v.Value).ToList())
                    .ToList();

                // Process each chunk of InvoiceLineItems for the current invoice
                foreach (var chunk in lineItemsChunks)
                {
                    // Temporarily replace the InvoiceLineItems with the current chunk
                    var originalLineItems = invoice.InvoiceLineItems; // Save the original list
                    invoice.InvoiceLineItems = chunk; // Assign the current chunk

                    // Serialize the modified invoice data to JSON
                    var invoicedatajson = JsonConvert.SerializeObject(new List<InvoiceData> {invoice});

                    // Create the parameter for the stored procedure
                    var paramName1 = "invoicedata";
                    var parameters1 = new List<NpgsqlParameter>
                    {
                        (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName1, invoicedatajson, NpgsqlDbType.Json),
                    };


                    // Execute the stored procedure for the current chunk
                    var exec = await _queryHelper.ExecuteStoredProc("insert_temp_invoices", parameters1);
                    invoiceCount += exec; // Increment the count for the current invoice
                    totalCount += exec; // Increment the total count

                    // Restore the original InvoiceLineItems for the next iteration
                    invoice.InvoiceLineItems = originalLineItems;
                }

                Console.WriteLine("Finished processing Invoice");
            }

            Console.WriteLine("All invoices processed");
            return totalCount;
        }
        public Task<int> TempInsertStoreProc2(string InvoiceNumber, string TotalAmount, string TotalLines)
        {
            // Define a retry policy
            var retryPolicy = Policy
                .Handle<NpgsqlException>() // Retry on database connection issues
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} sec due to: {exception.Message}");
                    });

            var paramName1 = "p_invoice_number";
            var paramName3 = "p_totalamount";
            var paramName4 = "p_totallineitems";
            var parameters1 = new List<NpgsqlParameter>
              {
                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName1, InvoiceNumber, NpgsqlDbType.Text),
                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName3, TotalAmount, NpgsqlDbType.Text),
                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName4, TotalLines, NpgsqlDbType.Text)
               };
            var exec = _queryHelper.ExecuteStoredProc("split_and_store_invoice", parameters1);
            count++;
            Console.WriteLine($"Count = {count}");
            return exec;
        }

        public Task<int> InsertInvData(string InvoiceNumber, List<string> filepath)
        {
            // Define a retry policy
            var retryPolicy = Policy
                .Handle<NpgsqlException>() // Retry on database connection issues
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} sec due to: {exception.Message}");
                    });


            var filepathjson = JsonConvert.SerializeObject(filepath);
            var paramName1 = "p_invoice_number";
            var paramName2 = "filepath";
            var parameters1 = new List<NpgsqlParameter>
                              {
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName1, InvoiceNumber, NpgsqlDbType.Text),
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName2, filepathjson, NpgsqlDbType.Json)
                               };
            var exec = _queryHelper.ExecuteStoredProc("invoice_validation", parameters1);
            count++;
            Console.WriteLine($"Count = {count}");
            return exec;
        }
        public Task<int> InsertInvoiceData(string InvoiceNumber, string TotalAmount, string TotalLines, List<string> filepath)
        {
            // Define a retry policy
            var retryPolicy = Policy
                .Handle<NpgsqlException>() // Retry on database connection issues
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} sec due to: {exception.Message}");
                    });

            var filepathjson = JsonConvert.SerializeObject(filepath);
            var paramName1 = "p_invoice_number";
            var paramName2 = "p_totalamount";
            var paramName3 = "p_totallineitems";
            var paramName4 = "filepath";
            var parameters1 = new List<NpgsqlParameter>
                              {
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName1, InvoiceNumber, NpgsqlDbType.Text),
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName2, TotalAmount, NpgsqlDbType.Text),
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName3, TotalLines, NpgsqlDbType.Text),
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName4, filepathjson, NpgsqlDbType.Json)
                               };
            var exec = _queryHelper.ExecuteStoredProc("insert_invoicedata", parameters1);
            count++;
            Console.WriteLine($"Count = {count}");
            return exec;
        }

        public Task<int> ValidateETL(List<InvoiceData> invoicedatajson, List<List<InvoiceLineItems>> invoicelineitemsjson)
        {
            // Define a retry policy
            var retryPolicy = Policy
                .Handle<NpgsqlException>() // Retry on database connection issues
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} sec due to: {exception.Message}");
                    });

            var invoicelineitemsJsonString = JsonConvert.SerializeObject(invoicelineitemsjson);
            var invoicedatajsonstring = JsonConvert.SerializeObject(invoicedatajson);
            var lineitemjsonstring = JsonConvert.SerializeObject(invoicelineitemsJsonString);
            var paramName1 = "invoicedata";
            var paramName2 = "invoicelineitems";
            var parameters1 = new List<NpgsqlParameter>
                              {
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName1, invoicedatajsonstring, NpgsqlDbType.Json),
                                (NpgsqlParameter)QueryHelper.CreateSqlParameter(paramName2, invoicelineitemsJsonString, NpgsqlDbType.Json)
                               };
            var exec = _queryHelper.ExecuteStoredProc("ETLProcess", parameters1);
            return exec;
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
        #region InvoiceData
        async Task<InvoiceData> IETLService.InsertInvoiceData(InvoiceData invoiceData)
        {
            try
            {
                Log.Information("InsertInvoiceData service called.");
                Console.WriteLine("InsertInvoiceData service called.");
                const string sql = @"INSERT [dbo].[S_InvoiceData] (
                        [cbcAmount],
                        [cacAddress],
                        [cacInvoicePeriod],
                        [cacPostalSellerAddress],
                        [cacPostalBuyerAddress],
                        [cacPrice],
                        [cacTaxTotal],
                        [cbcBaseAmount],
                        [cbcBaseQuantity],
                        [cbcBuyerReference],
                        [cbcSellerCompanyID],
                        [cbcBuyerCompanyID],
                        [cbcSellerVATID],
                        [cbcBuyerVATID],
                        [cbcCompanyLegalForm],
                        [cbcDescription],
                        [cbcDescriptionCode] ,
                        [cbcDocumentCurrencyCode] ,
                        [cbcSellerElectronicMail] ,
                        [cbcBuyerElectronicMail] ,
                        [cbcIDInvoiceNumber] ,
                        [cbcPrecedingInvoicenumber] ,
                        [cbcIDPaymentAccountIdentifier] ,
                        [cbcIDVATcategoryCode] ,
                        [cbcIDItemCountryOfOrigin] ,
                        [cbcIdentificationCode] ,
                        [cbcInvoiceTypeCode] ,
                        [cbcIssueDate] ,
                        [cbcLineExtensionAmount] ,
                        [cbcSellerName] ,
                        [cbcBuyerName] ,
                        [cbcNameDeliverToPartyName] ,
                        [cbcNote] ,
                        [cbcPayableAmount] ,
                        [cbcPaymentID] ,
                        [cbcPercent] ,
                        [cbcSellerRegnName] ,
                        [cbcBuyerRegnName] ,
                        [cbcTaxableAmount] ,
                        [cbcTaxCurrencyCode] ,
                        [cbcTaxExclusiveAmount] ,
                        [cbcTaxExemptionReason] ,
                        [cbcTaxInclusiveAmount] ,
                        [cbcTaxPoIntegerDate] ,
                        [cbcSellerTelephone] ,
                        [cbcBuyerTelephone] ,
                        [NamePaymentMeansText] ,
                        [schemeID] ,
                        [unitCode] ,
                        [IRBMUniqueNo] ,
                        [PaymentDueDate] ,
                        [cbcPaymentCurrencyCode] ,
                        [cbcBusinessActivityDesc] ,
                        [cbcMSICCode] ,
                        [TotalLineAmount] ,
                        [TotalChangeAmount] ,
                        [TotalAllowanceAmount] ,
                        [TotalTaxAmount] ,
                        [PayableRoundingAmount] ,
                        [PrePaidAmount] ,
                        [TotalAmountDue] ,
                        [mode] ,
                        [eInvoiceType] ,
                        [eTemplateId] ,
                        [templateId] ,
                        [Status] ,
                        [eInvoiceDate] ,
                        [invoiceValidator] ,
                        [taxOfficeSubmitter] ,
                        [WorkflowStatus] ,
                        [comments] ,
                        [bulkGuid],
                        [ApprovalType] ,
                        [LastDraftSaveDate] ,
                        [PendingProcessingDate] ,
                        [Checker1ActionDate] ,
                        [Checker2ActionDate] ,
                        [SubmittedToIRBDate] ,
                        [IRBResponseDate] ,
                        [BuyerSentDate] ,
                        [PDFBlob] ,
                        [PDFXml] ,
                        [Priority] ,
                        [CreatedDate] ,
                        [CreatedBy] ,
                        [UpdatedDate] ,
                        [UpdatedBy] ,
                        [PDFWithQRBlob] ,
                        [XMLWithQRBlob] ,
                        [JsonInvoiceBlob] ,
                        [JsonWithQRBlob] ,
                        [cacAddress2] ,
                        [cacAddress3] ,
                        [cacAddress4] ,
                        [cacSellerEmail] ,
                        [reltedInvoiceId] ,
                        [eInvoiceNumber],
                        [taxofficeschedulerid] ,
                        [InvoiceVersion] ,
                        [cbcSellerSSTRegistrationNumber] ,
                        [cbcSellerTourismTaxRegistrationNumber] ,
                        [cbcSStreetName] ,
                        [cbcSAdditionalStreetName1] ,
                        [cbcSAdditionalStreetName2] ,
                        [cbcSPostalZone] ,
                        [cbcSCityName] ,
                        [cbcSCountrySubentity] ,
                        [cbcSCountryIdentificationCode] ,
                        [cbcBStreetName] ,
                        [cbcBAdditionalStreetName1] ,
                        [cbcBAdditionalStreetName2] ,
                        [cbcBPostalZone] ,
                        [cbcBCityName] ,
                        [cbcBCountrySubentity] ,
                        [cbcBCountryIdentificationCode] ,
                        [cbcBSSTRegistrationNumber] ,
                        [cbcDStreetName] ,
                        [cbcDAdditionalStreetName1] ,
                        [cbcDAdditionalStreetName2] ,
                        [cbcDPostalZone] ,
                        [cbcDCityName] ,
                        [cbcDCountrySubentity] ,
                        [cbcDCountryIdentificationCode] ,
                        [cbcShipRecipientName] ,
                        [cbcShipRecipientVATID] ,
                        [cbcShipRecipientCompanyID] ,
                        [cbcShipRecipientStreetName] ,
                        [cbcShipRecipientAdditionalStreetName1] ,
                        [cbcShipRecipientAdditionalStreetName2] ,
                        [cbcShipRecipientPostalZone] ,
                        [cbcShipRecipientCityName] ,
                        [cbcShipRecipientCountrySubentity] ,
                        [cbcShipRecipientCountryIdentificationCode] ,
                        [cbcCalculationRate] ,
                        [cbcStartDate] ,
                        [cbcEndDate] ,
                        [cbcSCategory] ,
                        [cbcSSubCategory] ,
                        [cbcSBRNNumber] ,
                        [cbcSNRIC] ,
                        [cbcBCategory] ,
                        [cbcBSubCategory] ,
                        [cbcBBRNNumber] ,
                        [cbcBNRIC] ,
                        [cbcShipRecipientCategory] ,
                        [cbcShipRecipientSubCategory] ,
                        [cbcShipRecipientBRNNumber] ,
                        [cbcShipRecipientNRIC] ,
                        [cacPaymentTerms],
                        [cbcPaidDate] ,
                        [cbcPaidTime] ,
                        [cbcPaidId] ,
                        [cbcItemClassificationCodeClass] ,
                        [cbcItemClassificationCodePTC] ,
                        [cbcSourceInvoiceNumber] ,
                        [workFlowOption],
                        [RejectRequestDate] ,
                        [RejectionStatusReason] ,
                        [CancelDate] ,
                        [CancelStatusReason] ,
                        [CancelledsubmıttedID] ,
                        [IRBMUniqueIdentifierNumber] ,
                        [InvoiceDocumentReferenceNumber] ,
                        [cbcCustomizationID] ,
                        [cbcProfileID] ,
                        [cbcDueDate] ,
                        [cbcAccountingCost] ,
                        [cbcOrderReferenceId] ,
                        [cbcSalesOrderID] ,
                        [cbcEndpoIntegerId] ,
                        [cbcEndpoIntegerIdschemeID] ,
                        [cbcPartyTaxSchemeCompanyID] ,
                        [cbcPartyTaxSchemeID] ,
                        [cbcPartyLegalEntityCompanyID] ,
                        [cbcPartyLegalEntityCompanyLegalForm] ,
                        [cbcBuyerEndpoIntegerId] ,
                        [cbcBuyerEndpoIntegerIdschemeID] ,
                        [cbcBuyerPartyTaxSchemeCompanyID] ,
                        [cbcBuyerPartyTaxSchemeID] ,
                        [cbcBuyerPartyLegalEntityCompanyID] ,
                        [cbcBuyerPartyLegalEntityCompanyLegalForm] ,
                        [cbcActualDeliveryDate] ,
                        [cbcDeliveryLocationId] ,
                        [cbcDeliveryStreetName] ,
                        [cbcDeliveryAdditionalStreetName] ,
                        [cbcDeliveryCityName] ,
                        [cbcDeliveryPostalZone] ,
                        [cbcDeliveryAddressLine] ,
                        [cbcDeliveryCountryIdentificationCode] ,
                        [cacDeliveryPartyName] ,
                        [cbcIRBMValidationDate] ,
                        [billerId] ,
                        [ValidationDate] ,
                        [ValidityHours] ,
                        [InvoiceDocumentReference] ,
                        [QRCode] ,
                        [IRBMValidationDate] ,
                        [ValidityEndDate] ,
                        [IRBMValidationTime] ,
                        [RemainingHours] ,
                        [SourceFileName] ,
                        [SourceName] ,
                        [FıleName] ,
                        [DataTıme] ,
                        [FolderName],
                        [InvoıceCreatorName] ,
                        [SourceInvoıceNumber] ,
                        [InvoıceNumberStatus] ,
                        [ProcessType] ,
                        [WorkflowType] ,
                        [SchedulerName] ,
                        [TaskID] ,
                        [CreationDateTıme] ,
                        [CreatorID] ,
                        [CreationNotes] ,
                        [CreationSubmissionDate] ,
                        [CreationApprovalDateTıme] ,
                        [CreationApprovalID] ,
                        [CreationApprovalNotes] ,
                        [CreationApprovalStatus] ,
                        [VerifıcationApprovalDateTıme] ,
                        [CreationApproverlID] ,
                        [VerificationApprovalID] ,
                        [VerificationApproverlID] ,
                        [VerificationApprovalNotes] ,
                        [GenerationApprovalID] ,
                        [GenerationApproverlID] ,
                        [GenerationApprovalNotes] ,
                        [GenerationApprovalStatus] ,
                        [GenerationApprovalDateTıme] ,
                        [ValidationApprovalID] ,
                        [ValidationApproverlID] ,
                        [ValidationApprovalNotes] ,
                        [ValidationApprovalStatus] ,
                        [ValidationApprovalDateTıme] ,
                        [SubmissionDateTıme] ,
                        [SubmitterID] ,
                        [SubmissionNotes] ,
                        [SubmissionApprovalDateTıme] ,
                        [SubmissionApprovalSubmıtDateTıme] ,
                        [SubmissionApprovalID] ,
                        [SubmissionApprovalNotes] ,
                        [SubmissionApprovalStatus] ,
                        [RetryCount] ,
                        [ValidityEndDateTıme] ,
                        [ValidityStatus] ,
                        [RejectionDateTıme] ,
                        [RejectionReasons] ,
                        [RejectionWFCheckerID] ,
                        [RejectionWFCheckerStatus] ,
                        [RejectionWFCheckerSubmitDateTıme] ,
                        [RejectionWFApproverID] ,
                        [RejectionWFApprovalStatus] ,
                        [RejectionWFApprovalNotes] ,
                        [RejectionWFApprovalSubmitDateTıme] ,
                        [CancellationDateTıme] ,
                        [CancellationReasons] ,
                        [CancellationWFCheckerID] ,
                        [CancellationWFCheckerStatus] ,
                        [CancellationWFCheckerSubmıtDateTıme] ,
                        [CancellationWFApproverID] ,
                        [CancellationWFApprovalStatus] ,
                        [CancellationWFApprovalNotes] ,
                        [CancellationWFApprovalSubmıtDateTıme] ,
                        [ETLJobName] ,
                        [cbcPricingCurrencyCode] ,
                        [cbcCurrencyExchangeRate] ,
                        [cbcFrequencyofBilling] ,
                        [cbcBillingPeriodStartDate] ,
                        [cbcBillingPeriodEndDate] ,
                        [PaymentMode] ,
                        [cbcSupplierBankAccountNumber] ,
                        [cbcBillReferenceNumber] ,
                        [SourceCalculationMode] ,
                        [EtlCalculationMode] ,
                        [İnvoiceFactorycalcutionMode] ,
                        [cbcTaxRate] ,
                        [cbcTaxCategory] ,
                        [validationlink] ,
                        [CustomsForm19ID] ,
                        [CustomsForm19DocumentType] ,
                        [Incoterms] ,
                        [FTADocumentType] ,
                        [FTAID] ,
                        [FTADocumentDesc] ,
                        [schemeAgencyName] ,
                        [CustomsForm2ID] ,
                        [CustomsForm2DocumentType] ,
                        [OtherChargesID] ,
                        [OtherChargesChargeIndicator] ,
                        [OtherChargesAmount] ,
                        [OtherChargesAllowanceChargeReason] ,
                        [NotificationTemplateId] ,
                        [SMSTemplateId] ,
                        [OutputFormat])
                    OUTPUT Inserted.Id
                    VALUES (
                        @cbcAmount,
                        @cacAddress,
                        @cacInvoicePeriod,
                        @cacPostalSellerAddress,
                        @cacPostalBuyerAddress,
                        @cacPrice,
                        @cacTaxTotal,
                        @cbcBaseAmount,
                        @cbcBaseQuantity,
                        @cbcBuyerReference,
                        @cbcSellerCompanyID,
                        @cbcBuyerCompanyID,
                        @cbcSellerVATID,
                        @cbcBuyerVATID,
                        @cbcCompanyLegalForm,
                        @cbcDescription,
                        @cbcDescriptionCode ,
                        @cbcDocumentCurrencyCode ,
                        @cbcSellerElectronicMail ,
                        @cbcBuyerElectronicMail ,
                        @cbcIDInvoiceNumber ,
                        @cbcPrecedingInvoicenumber ,
                        @cbcIDPaymentAccountIdentifier ,
                        @cbcIDVATcategoryCode ,
                        @cbcIDItemCountryOfOrigin ,
                        @cbcIdentificationCode ,
                        @cbcInvoiceTypeCode ,
                        @cbcIssueDate ,
                        @cbcLineExtensionAmount ,
                        @cbcSellerName ,
                        @cbcBuyerName ,
                        @cbcNameDeliverToPartyName ,
                        @cbcNote ,
                        @cbcPayableAmount ,
                        @cbcPaymentID ,
                        @cbcPercent ,
                        @cbcSellerRegnName ,
                        @cbcBuyerRegnName ,
                        @cbcTaxableAmount ,
                        @cbcTaxCurrencyCode ,
                        @cbcTaxExclusiveAmount ,
                        @cbcTaxExemptionReason ,
                        @cbcTaxInclusiveAmount ,
                        @cbcTaxPoIntegerDate ,
                        @cbcSellerTelephone ,
                        @cbcBuyerTelephone ,
                        @NamePaymentMeansText ,
                        @schemeID ,
                        @unitCode ,
                        @IRBMUniqueNo ,
                        @PaymentDueDate ,
                        @cbcPaymentCurrencyCode ,
                        @cbcBusinessActivityDesc ,
                        @cbcMSICCode ,
                        @TotalLineAmount ,
                        @TotalChangeAmount ,
                        @TotalAllowanceAmount ,
                        @TotalTaxAmount ,
                        @PayableRoundingAmount ,
                        @PrePaidAmount ,
                        @TotalAmountDue ,
                        @mode ,
                        @eInvoiceType ,
                        @eTemplateId ,
                        @templateId ,
                        @Status ,
                        @eInvoiceDate ,
                        @invoiceValidator ,
                        @taxOfficeSubmitter ,
                        @WorkflowStatus ,
                        @comments ,
                        @bulkGuid,
                        @ApprovalType ,
                        @LastDraftSaveDate ,
                        @PendingProcessingDate ,
                        @Checker1ActionDate ,
                        @Checker2ActionDate ,
  
                        @SubmittedToIRBDate ,
                        @IRBResponseDate ,
                        @BuyerSentDate ,
                        @PDFBlob ,
                        @PDFXml ,
                        @Priority ,
                        @CreatedDate ,
                        @CreatedBy ,
                        @UpdatedDate ,
                        @UpdatedBy ,
                        @PDFWithQRBlob ,
                        @XMLWithQRBlob ,
                        @JsonInvoiceBlob ,
                        @JsonWithQRBlob ,
                        @cacAddress2 ,
                        @cacAddress3 ,
                        @cacAddress4 ,
                        @cacSellerEmail ,
                        @reltedInvoiceId ,
                        @eInvoiceNumber ,
                        @taxofficeschedulerid ,
                        @InvoiceVersion ,
                        @cbcSellerSSTRegistrationNumber ,
                        @cbcSellerTourismTaxRegistrationNumber ,
                        @cbcSStreetName ,
                        @cbcSAdditionalStreetName1 ,
                        @cbcSAdditionalStreetName2 ,
                        @cbcSPostalZone ,
                        @cbcSCityName ,
                        @cbcSCountrySubentity ,
                        @cbcSCountryIdentificationCode ,
                        @cbcBStreetName ,
                        @cbcBAdditionalStreetName1 ,
                        @cbcBAdditionalStreetName2 ,
                        @cbcBPostalZone ,
                        @cbcBCityName ,
                        @cbcBCountrySubentity ,
                        @cbcBCountryIdentificationCode ,
                        @cbcBSSTRegistrationNumber ,
                        @cbcDStreetName ,
                        @cbcDAdditionalStreetName1 ,
                        @cbcDAdditionalStreetName2 ,
                        @cbcDPostalZone ,
                        @cbcDCityName ,
                        @cbcDCountrySubentity ,
                        @cbcDCountryIdentificationCode ,
                        @cbcShipRecipientName ,
                        @cbcShipRecipientVATID ,
                        @cbcShipRecipientCompanyID ,
                        @cbcShipRecipientStreetName ,
                        @cbcShipRecipientAdditionalStreetName1 ,
                        @cbcShipRecipientAdditionalStreetName2 ,
                        @cbcShipRecipientPostalZone ,
                        @cbcShipRecipientCityName ,
                        @cbcShipRecipientCountrySubentity ,
                        @cbcShipRecipientCountryIdentificationCode ,
                        @cbcCalculationRate ,
                        @cbcStartDate ,
                        @cbcEndDate ,
                        @cbcSCategory ,
                        @cbcSSubCategory ,
                        @cbcSBRNNumber ,
                        @cbcSNRIC ,
                        @cbcBCategory ,
                        @cbcBSubCategory ,
                        @cbcBBRNNumber ,
                        @cbcBNRIC ,
                        @cbcShipRecipientCategory ,
                        @cbcShipRecipientSubCategory ,
                        @cbcShipRecipientBRNNumber ,
                        @cbcShipRecipientNRIC ,
                        @cacPaymentTerms ,
                        @cbcPaidDate ,
                        @cbcPaidTime ,
                        @cbcPaidId ,
                        @cbcItemClassificationCodeClass ,
                        @cbcItemClassificationCodePTC ,
                        @cbcSourceInvoiceNumber ,
                        @workFlowOption,
                        @RejectRequestDate ,
                        @RejectionStatusReason ,
                        @CancelDate ,
                        @CancelStatusReason ,
                        @CancelledsubmıttedID ,
                        @IRBMUniqueIdentifierNumber ,
                        @InvoiceDocumentReferenceNumber ,
                        @cbcCustomizationID ,
                        @cbcProfileID ,
                        @cbcDueDate ,
                        @cbcAccountingCost ,
                        @cbcOrderReferenceId ,
                        @cbcSalesOrderID ,
                        @cbcEndpoIntegerId ,
                        @cbcEndpoIntegerIdschemeID ,
                        @cbcPartyTaxSchemeCompanyID ,
                        @cbcPartyTaxSchemeID ,
                        @cbcPartyLegalEntityCompanyID ,
                        @cbcPartyLegalEntityCompanyLegalForm ,
                        @cbcBuyerEndpoIntegerId ,
                        @cbcBuyerEndpoIntegerIdschemeID ,
                        @cbcBuyerPartyTaxSchemeCompanyID ,
                        @cbcBuyerPartyTaxSchemeID ,
                        @cbcBuyerPartyLegalEntityCompanyID ,
                        @cbcBuyerPartyLegalEntityCompanyLegalForm ,
                        @cbcActualDeliveryDate ,
                        @cbcDeliveryLocationId ,
                        @cbcDeliveryStreetName ,
                        @cbcDeliveryAdditionalStreetName ,
                        @cbcDeliveryCityName ,
                        @cbcDeliveryPostalZone ,
                        @cbcDeliveryAddressLine ,
                        @cbcDeliveryCountryIdentificationCode ,
                        @cacDeliveryPartyName ,
                        @cbcIRBMValidationDate ,
                        @billerId ,
                        @ValidationDate ,
                        @ValidityHours ,
                        @InvoiceDocumentReference ,
                        @QRCode ,
                        @IRBMValidationDate ,
                        @ValidityEndDate ,
                        @IRBMValidationTime ,
                        @RemainingHours ,
                        @SourceFileName ,
                        @SourceName ,
                        @FıleName ,
                        @DataTıme ,
                        @FolderName,
                        @InvoıceCreatorName ,
                        @SourceInvoıceNumber ,
                        @InvoıceNumberStatus ,
                        @ProcessType ,
                        @WorkflowType ,
                        @SchedulerName ,
                        @TaskID ,
                        @CreationDateTıme ,
                        @CreatorID ,
                        @CreationNotes ,
                        @CreationSubmissionDate ,
                        @CreationApprovalDateTıme ,
                        @CreationApprovalID ,
                        @CreationApprovalNotes ,
                        @CreationApprovalStatus ,
                        @VerifıcationApprovalDateTıme ,
                        @CreationApproverlID ,
                        @VerificationApprovalID ,
                        @VerificationApproverlID ,
                        @VerificationApprovalNotes ,
                        @GenerationApprovalID ,
                        @GenerationApproverlID ,
                        @GenerationApprovalNotes ,
                        @GenerationApprovalStatus ,
                        @GenerationApprovalDateTıme ,
                        @ValidationApprovalID ,
                        @ValidationApproverlID ,
                        @ValidationApprovalNotes ,
                        @ValidationApprovalStatus ,
                        @ValidationApprovalDateTıme ,
                        @SubmissionDateTıme ,
                        @SubmitterID ,
                        @SubmissionNotes ,
                        @SubmissionApprovalDateTıme ,
                        @SubmissionApprovalSubmıtDateTıme ,
                        @SubmissionApprovalID ,
                        @SubmissionApprovalNotes ,
                        @SubmissionApprovalStatus ,
                        @RetryCount ,
                        @ValidityEndDateTıme ,
                        @ValidityStatus ,
                        @RejectionDateTıme ,
                        @RejectionReasons ,
                        @RejectionWFCheckerID ,
                        @RejectionWFCheckerStatus ,
                        @RejectionWFCheckerSubmitDateTıme ,
                        @RejectionWFApproverID ,
                        @RejectionWFApprovalStatus ,
                        @RejectionWFApprovalNotes ,
                        @RejectionWFApprovalSubmitDateTıme ,
                        @CancellationDateTıme ,
                        @CancellationReasons ,
                        @CancellationWFCheckerID ,
                        @CancellationWFCheckerStatus ,
                        @CancellationWFCheckerSubmıtDateTıme ,
                        @CancellationWFApproverID ,
                        @CancellationWFApprovalStatus ,
                        @CancellationWFApprovalNotes ,
                        @CancellationWFApprovalSubmıtDateTıme ,
                        @ETLJobName ,
                        @cbcPricingCurrencyCode ,
                        @cbcCurrencyExchangeRate ,
                        @cbcFrequencyofBilling ,
                        @cbcBillingPeriodStartDate ,
                        @cbcBillingPeriodEndDate ,
                        @PaymentMode ,
                        @cbcSupplierBankAccountNumber ,
                        @cbcBillReferenceNumber ,
                        @SourceCalculationMode ,
                        @EtlCalculationMode ,
                        @İnvoiceFactorycalcutionMode ,
                        @cbcTaxRate ,
                        @cbcTaxCategory ,
                        @validationlink ,
                        @CustomsForm19ID ,
                        @CustomsForm19DocumentType ,
                        @Incoterms ,
                        @FTADocumentType ,
                        @FTAID ,
                        @FTADocumentDesc ,
                        @schemeAgencyName ,
                        @CustomsForm2ID ,
                        @CustomsForm2DocumentType ,
                        @OtherChargesID ,
                        @OtherChargesChargeIndicator ,
                        @OtherChargesAmount ,
                        @OtherChargesAllowanceChargeReason ,
                        @NotificationTemplateId ,
                        @SMSTemplateId ,
                        @OutputFormat)";

                var id = await _queryHelper.ExecuteScalar(sql, InsertInvoiceDataTake(invoiceData));
                invoiceData.Id = int.Parse(id.ToString());
                Log.Information($"{invoiceData.EInvoiceNumber} Invoice Number Inserted");
                Console.WriteLine($"{invoiceData.EInvoiceNumber} Invoice Number Inserted");
                return invoiceData;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in InsertInvoiceData service.{ex.Message}");
                Console.WriteLine($"Exception in InsertInvoiceData service.{ex.Message}");
                throw;
            }
        }
        private List<IDataParameter> InsertInvoiceDataTake(InvoiceData field)
        {
            var parameters = new List<IDataParameter>
            {
            QueryHelper.CreateSqlParameter("@Id", field.Id, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@cbcAmount", field.CbcAmount, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cacAddress", field.CacAddress, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cacInvoicePeriod", field.CacInvoicePeriod, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cacPostalSellerAddress", field.CacPostalSellerAddress, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cacPostalBuyerAddress", field.CacPostalBuyerAddress, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cacPrice", field.CacPrice, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cacTaxTotal", field.CacTaxTotal, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBaseAmount", field.CbcBaseAmount, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBaseQuantity", field.CbcBaseQuantity, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBuyerReference", field.CbcBuyerReference, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSellerCompanyID", field.CbcSellerCompanyID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBuyerCompanyID", field.CbcBuyerCompanyID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSellerVATID", field.CbcSellerVATID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBuyerVATID", field.CbcBuyerVATID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcCompanyLegalForm", field.CbcCompanyLegalForm, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDescription", field.CbcDescription, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDescriptionCode", field.CbcDescriptionCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDocumentCurrencyCode", field.CbcDocumentCurrencyCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSellerElectronicMail", field.CbcSellerElectronicMail, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBuyerElectronicMail", field.CbcBuyerElectronicMail, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcIDInvoiceNumber", field.CbcIDInvoiceNumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcPrecedingInvoicenumber", field.CbcPrecedingInvoicenumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcIDPaymentAccountIdentifier", field.CbcIDPaymentAccountIdentifier, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcIDVATcategoryCode", field.CbcIDVATcategoryCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcIDItemCountryOfOrigin", field.CbcIDItemCountryOfOrigin, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcIdentificationCode", field.CbcIdentificationCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcInvoiceTypeCode", field.CbcInvoiceTypeCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcIssueDate", field.CbcIssueDate, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcLineExtensionAmount", field.CbcLineExtensionAmount, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSellerName", field.CbcSellerName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBuyerName", field.CbcBuyerName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcNameDeliverToPartyName", field.CbcNameDeliverToPartyName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcNote", field.CbcNote, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcPayableAmount", field.CbcPayableAmount, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcPaymentID", field.CbcPaymentID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcPercent", field.CbcPercent, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSellerRegnName", field.CbcSellerRegnName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBuyerRegnName", field.CbcBuyerRegnName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcTaxableAmount", field.CbcTaxableAmount, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcTaxCurrencyCode", field.CbcTaxCurrencyCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcTaxExclusiveAmount", field.CbcTaxExclusiveAmount, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcTaxExemptionReason", field.CbcTaxExemptionReason, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcTaxInclusiveAmount", field.CbcTaxInclusiveAmount, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcTaxPoIntegerDate", field.CbcTaxPointDate, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSellerTelephone", field.CbcSellerTelephone, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBuyerTelephone", field.CbcBuyerTelephone, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@NamePaymentMeansText", field.NamePaymentMeansText, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@schemeID", field.SchemeID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@unitCode", field.UnitCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@IRBMUniqueNo", field.IRBMUniqueNo, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@PaymentDueDate", field.PaymentDueDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@cbcPaymentCurrencyCode", field.CbcPaymentCurrencyCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBusinessActivityDesc", field.CbcBusinessActivityDesc, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcMSICCode", field.CbcMsicCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@TotalLineAmount", field.TotalLineAmount, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@TotalChangeAmount", field.TotalChangeAmount, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@TotalAllowanceAmount", field.TotalAllowanceAmount, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@TotalTaxAmount", field.TotalTaxAmount, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@PayableRoundingAmount", field.PayableRoundingAmount, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@PrePaidAmount", field.PrePaidAmount, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@TotalAmountDue", field.TotalAmountDue, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@mode", field.Mode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@eInvoiceType", field.EInvoiceType, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@eTemplateId", field.eTemplateId, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@templateId", field.templateId, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@Status", field.Status, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@eInvoiceDate", field.eInvoiceDateTime, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@invoiceValidator", field.invoiceValidator, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@taxOfficeSubmitter", field.TaxOfficeSubmitter, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@WorkflowStatus", field.WorkflowStatus, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@comments", field.Comments, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@bulkGuid", field.BulkGuid, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@ApprovalType", field.ApprovalType, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@LastDraftSaveDate", field.LastDraftSaveDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@PendingProcessingDate", field.PendingProcessingDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@Checker1ActionDate", field.Checker1ActionDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@Checker2ActionDate", field.Checker2ActionDate, NpgsqlDbType.Date),
            //QueryHelper.CreateSqlParameter("@Checke2ActionTime", field.Checke2ActionTime, NpgsqlDbType.Date2),
            QueryHelper.CreateSqlParameter("@SubmittedToIRBDate", field.SubmittedToIRBDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@IRBResponseDate", field.IRBResponseDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@BuyerSentDate", field.BuyerSentDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@PDFBlob", field.PdfBlob, NpgsqlDbType.Bytea),
            QueryHelper.CreateSqlParameter("@PDFXml", field.PdfXml, NpgsqlDbType.Bytea),
            QueryHelper.CreateSqlParameter("@Priority", field.Priority, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CreatedDate", field.CreatedDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@CreatedBy", field.CreatedBy, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@UpdatedDate", field.UpdatedDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@UpdatedBy", field.UpdatedBy, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@PDFWithQRBlob", field.PdfWithQRBlob, NpgsqlDbType.Bytea),
            QueryHelper.CreateSqlParameter("@XMLWithQRBlob", field.XmlWithQRBlob, NpgsqlDbType.Bytea),
            QueryHelper.CreateSqlParameter("@JsonInvoiceBlob", field.JsonInvoiceBlob, NpgsqlDbType.Bytea),
            QueryHelper.CreateSqlParameter("@JsonWithQRBlob", field.JsonWithQRBlob, NpgsqlDbType.Bytea),
            QueryHelper.CreateSqlParameter("@cacAddress2", field.CacAddress2, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cacAddress3", field.CacAddress3, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cacAddress4", field.CacAddress4, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cacSellerEmail", field.CacSellerEmail, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@reltedInvoiceId", field.ReltedInvoiceId, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@eInvoiceNumber", field.EInvoiceNumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@taxofficeschedulerid", field.TaxOfficeSchedulerId, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@InvoiceVersion", field.InvoiceVersion, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSellerSSTRegistrationNumber", field.CbcSellerSSTRegistrationNumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSellerTourismTaxRegistrationNumber", field.CbcSellerTourismTaxRegistrationNumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSStreetName", field.CbcSStreetName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSAdditionalStreetName1", field.CbcSAdditionalStreetName1, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSAdditionalStreetName2", field.CbcSAdditionalStreetName2, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSPostalZone", field.CbcSPostalZone, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSCityName", field.CbcSCityName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSCountrySubentity", field.CbcSCountrySubentity, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSCountryIdentificationCode", field.CbcSCountryIdentificationCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBStreetName", field.CbcBStreetName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBAdditionalStreetName1", field.CbcBAdditionalStreetName1, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBAdditionalStreetName2", field.CbcBAdditionalStreetName2, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBPostalZone", field.CbcBPostalZone, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBCityName", field.CbcBCityName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBCountrySubentity", field.CbcBCountrySubentity, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBCountryIdentificationCode", field.CbcBCountryIdentificationCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBSSTRegistrationNumber", field.CbcBSSTRegistrationNumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDStreetName", field.CbcDStreetName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDAdditionalStreetName1", field.CbcDAdditionalStreetName1, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDAdditionalStreetName2", field.CbcDAdditionalStreetName2, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDPostalZone", field.CbcDPostalZone, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDCityName", field.CbcDCityName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDCountrySubentity", field.CbcDCountrySubentity, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDCountryIdentificationCode", field.CbcDCountryIdentificationCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcShipRecipientName", field.CbcShipRecipientName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcShipRecipientVATID", field.CbcShipRecipientVATID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcShipRecipientCompanyID", field.CbcShipRecipientCompanyID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcShipRecipientStreetName", field.CbcShipRecipientStreetName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcShipRecipientAdditionalStreetName1", field.CbcShipRecipientAdditionalStreetName1, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcShipRecipientAdditionalStreetName2", field.CbcShipRecipientAdditionalStreetName2, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcShipRecipientPostalZone", field.CbcShipRecipientPostalZone, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcShipRecipientCityName", field.CbcShipRecipientCityName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcShipRecipientCountrySubentity", field.CbcShipRecipientCountrySubentity, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcShipRecipientCountryIdentificationCode", field.CbcShipRecipientCountryIdentificationCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcCalculationRate", field.CbcCalculationRate, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcStartDate", field.CbcStartDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@cbcEndDate", field.CbcEndDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@cbcSCategory", field.CbcSCategory, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSSubCategory", field.CbcSSubCategory, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSBRNNumber", field.CbcSBRNNumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSNRIC", field.CbcSNRIC, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBCategory", field.CbcBCategory, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBSubCategory", field.CbcBSubCategory, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBBRNNumber", field.CbcBBRNNumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBNRIC", field.CbcBNRIC, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcShipRecipientCategory", field.CbcShipRecipientCategory, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcShipRecipientSubCategory", field.CbcShipRecipientSubCategory, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcShipRecipientBRNNumber", field.CbcShipRecipientBRNNumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcShipRecipientNRIC", field.CbcShipRecipientNRIC, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cacPaymentTerms", field.CacPaymentTerms, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcPaidDate", field.CbcPaidDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@cbcPaidTime", field.CbcPaidTime, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcPaidId", field.CbcPaidId, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcItemClassificationCodeClass", field.CbcItemClassificationCodeClass, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcItemClassificationCodePTC", field.CbcItemClassificationCodePTC, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSourceInvoiceNumber", field.CbcSourceInvoiceNumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@workFlowOption", field.WorkFlowOption, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@RejectRequestDate", field.RejectRequestDateTime, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@RejectionStatusReason", field.RejectionStatusReason, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CancelDate", field.CancelDateTime, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@CancelStatusReason", field.CancelStatusReason, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CancelledsubmıttedID", field.CancelledsubmıttedID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@IRBMUniqueIdentifierNumber", field.IRBMUniqueIdentifierNumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@InvoiceDocumentReferenceNumber", field.InvoiceDocumentReferenceNumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcCustomizationID", field.CbcCustomizationID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcProfileID", field.CbcProfileID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDueDate", field.CbcDueDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@cbcAccountingCost", field.CbcAccountingCost, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcOrderReferenceId", field.CbcOrderReferenceId, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSalesOrderID", field.CbcSalesOrderID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcEndpoIntegerId", field.CbcEndpointId, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcEndpoIntegerIdschemeID", field.CbcEndpointIdschemeID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcPartyTaxSchemeCompanyID", field.CbcPartyTaxSchemeCompanyID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcPartyTaxSchemeID", field.CbcPartyTaxSchemeID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcPartyLegalEntityCompanyID", field.CbcPartyLegalEntityCompanyID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcPartyLegalEntityCompanyLegalForm", field.CbcPartyLegalEntityCompanyLegalForm, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBuyerEndpoIntegerId", field.CbcBuyerEndpointId, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBuyerEndpoIntegerIdschemeID", field.CbcBuyerEndpointIdschemeID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBuyerPartyTaxSchemeCompanyID", field.CbcBuyerPartyTaxSchemeCompanyID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBuyerPartyTaxSchemeID", field.CbcBuyerPartyTaxSchemeID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBuyerPartyLegalEntityCompanyID", field.CbcBuyerPartyLegalEntityCompanyID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBuyerPartyLegalEntityCompanyLegalForm", field.CbcBuyerPartyLegalEntityCompanyLegalForm, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcActualDeliveryDate", field.CbcActualDeliveryDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@cbcDeliveryLocationId", field.CbcDeliveryLocationId, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDeliveryStreetName", field.CbcDeliveryStreetName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDeliveryAdditionalStreetName", field.CbcDeliveryAdditionalStreetName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDeliveryCityName", field.CbcDeliveryCityName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDeliveryPostalZone", field.CbcDeliveryPostalZone, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDeliveryAddressLine", field.CbcDeliveryAddressLine, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcDeliveryCountryIdentificationCode", field.CbcDeliveryCountryIdentificationCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cacDeliveryPartyName", field.CacDeliveryPartyName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcIRBMValidationDate", field.CbcIRBMValidationDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@billerId", field.BillerId, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@ValidationDate", field.ValidationDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@ValidityHours", field.ValidityHours, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@InvoiceDocumentReference", field.InvoiceDocumentReference, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@QRCode", field.QRCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@IRBMValidationDate", field.IRBMValidationDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@ValidityEndDateTıme", field.ValidityEndDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@IRBMValidationTime", field.IRBMValidationTime, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@RemainingHours", field.RemainingHours, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@SourceFileName", field.SourceFileName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@SourceName", field.SourceName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@FıleName", field.FıleName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@DataTıme", field.DataTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@FolderName", field.FolderName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@InvoıceCreatorName", field.InvoıceCreatorName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@SourceInvoıceNumber", field.SourceInvoıceNumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@InvoıceNumberStatus", field.InvoıceNumberStatus, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@ProcessType", field.ProcessType, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@WorkflowType", field.WorkflowType, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@SchedulerName", field.SchedulerName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@TaskID", field.TaskID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CreationDateTıme", field.CreationDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@CreatorID", field.CreatorID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CreationNotes", field.CreationNotes, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CreationSubmissionDate", field.CreationSubmissionDateTime, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@CreationApprovalDateTıme", field.CreationApprovalDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@CreationApprovalID", field.CreationApprovalID, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@CreationApprovalNotes", field.CreationApprovalNotes, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CreationApprovalStatus", field.CreationApprovalStatus, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@VerifıcationApprovalDateTıme", field.VerifıcationApprovalDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@CreationApproverlID", field.CreationApprovalID, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@VerificationApprovalID", field.VerificationApprovalID, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@VerificationApproverlID", field.VerificationApprovalID, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@VerificationApprovalNotes", field.VerificationApprovalNotes, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@GenerationApprovalID", field.GenerationApprovalID, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@GenerationApproverlID", field.GenerationApproverlID, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@GenerationApprovalNotes", field.GenerationApprovalNotes, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@GenerationApprovalStatus", field.GenerationApprovalStatus, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@GenerationApprovalDateTıme", field.GenerationApprovalDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@ValidationApprovalID", field.ValidationApprovalID, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@ValidationApproverlID", field.ValidationApproverlID, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@ValidationApprovalNotes", field.ValidationApprovalNotes, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@ValidationApprovalStatus", field.ValidationApprovalStatus, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@ValidationApprovalDateTıme", field.ValidationApprovalDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@SubmissionDateTıme", field.SubmissionDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@SubmitterID", field.SubmitterID, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@SubmissionNotes", field.SubmissionNotes, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@SubmissionApprovalDateTıme", field.SubmissionApprovalDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@SubmissionApprovalSubmıtDateTıme", field.SubmissionApprovalSubmıtDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@SubmissionApprovalID", field.SubmissionApprovalID, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@SubmissionApprovalNotes", field.SubmissionApprovalNotes, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@SubmissionApprovalStatus", field.SubmissionApprovalStatus, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@RetryCount", field.RetryCount, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@ValidityEndDate", field.ValidityEndDateTime, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@ValidityStatus", field.ValidityStatus, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@RejectionDateTıme", field.RejectionDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@RejectionReasons", field.RejectionReasons, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@RejectionWFCheckerID", field.RejectionWFCheckerID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@RejectionWFCheckerStatus", field.RejectionWFCheckerStatus, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@RejectionWFCheckerSubmitDateTıme", field.RejectionWFCheckerSubmitDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@RejectionWFApproverID", field.RejectionWFApproverID, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@RejectionWFApprovalStatus", field.RejectionWFApprovalStatus, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@RejectionWFApprovalNotes", field.RejectionWFApprovalNotes, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@RejectionWFApprovalSubmitDateTıme", field.RejectionWFApprovalSubmitDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@CancellationDateTıme", field.CancellationDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@CancellationReasons", field.CancellationReasons, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CancellationWFCheckerID", field.CancellationWFCheckerID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CancellationWFCheckerStatus", field.CancellationWFCheckerStatus, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CancellationWFCheckerSubmıtDateTıme", field.CancellationWFCheckerSubmıtDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@CancellationWFApproverID", field.CancellationWFApproverID, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@CancellationWFApprovalStatus", field.CancellationWFApprovalStatus, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CancellationWFApprovalNotes", field.CancellationWFApprovalNotes, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CancellationWFApprovalSubmıtDateTıme", field.CancellationWFApprovalSubmıtDateTıme, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@ETLJobName", field.ETLJobName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcPricingCurrencyCode", field.CbcPricingCurrencyCode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcCurrencyExchangeRate", field.CbcCurrencyExchangeRate, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcFrequencyofBilling", field.CbcFrequencyofBilling, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBillingPeriodStartDate", field.CbcBillingPeriodStartDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@cbcBillingPeriodEndDate", field.CbcBillingPeriodEndDate, NpgsqlDbType.Date),
            QueryHelper.CreateSqlParameter("@PaymentMode", field.PaymentMode, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcSupplierBankAccountNumber", field.CbcSupplierBankAccountNumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcBillReferenceNumber", field.CbcBillReferenceNumber, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@SourceCalculationMode", field.SourceCalculationMode, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@EtlCalculationMode", field.EtlCalculationMode, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@İnvoiceFactorycalcutionMode", field.İnvoiceFactorycalcutionMode, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@cbcTaxRate", field.CbcTaxRate, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@cbcTaxCategory", field.CbcTaxCategory, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@validationlink", field.ValidationLink, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CustomsForm19ID", field.CustomsForm19ID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CustomsForm19DocumentType", field.CustomsForm19DocumentType, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@Incoterms", field.Incoterms, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@FTADocumentType", field.FTADocumentType, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@FTAID", field.FTAID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@FTADocumentDesc", field.FTADocumentDesc, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@schemeAgencyName", field.SchemeAgencyName, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CustomsForm2ID", field.CustomsForm2ID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@CustomsForm2DocumentType", field.CustomsForm2DocumentType, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@OtherChargesID", field.OtherChargesID, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@OtherChargesChargeIndicator", field.OtherChargesChargeIndicator, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@OtherChargesAmount", field.OtherChargesAmount, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@OtherChargesAllowanceChargeReason", field.OtherChargesAllowanceChargeReason, NpgsqlDbType.Varchar),
            QueryHelper.CreateSqlParameter("@NotificationTemplateId", field.NotificationTemplateId, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@SMSTemplateId", field.SMSTemplateId, NpgsqlDbType.Integer),
            QueryHelper.CreateSqlParameter("@OutputFormat", field.OutputFormat, NpgsqlDbType.Varchar)
            };
            return parameters;
        }

        async Task<InvoiceLineItems> IETLService.InsertInvoicelineData(InvoiceLineItems invoiceLineItems)
        {
            try
            {
                Log.Information("InsertInvoicelineData service called.");
                Console.WriteLine("InsertInvoicelineData service called.");

                // SQL Insert Query
                const string sql = @"
                    INSERT INTO [dbo].[S_InvoiceLineItems] (
                        [InvoiceId],
                        [CbcIDVATCategoryCode],
                        [CbcIDItemCountryOfOrigin],
                        [CbcDescription],
                        [CbcDescriptionCode],
                        [CbcBaseAmount],
                        [CbcAmount],
                        [CreatedBy],
                        [CreatedDate],
                        [UpdatedBy],
                        [UpdatedDate],
                        [LineId],
                        [CbcDiscountRate],
                        [CbcDiscountAmount],
                        [CbcTaxType],
                        [CbcTaxRate],
                        [CbcTaxAmount],
                        [CbcMeasure],
                        [CbcAllowanceType],
                        [CbcAllowanceReasonCode],
                        [CbcAllowanceText],
                        [CbcAllowanceBaseAmount],
                        [CbcAllowanceMultiplierFactor],
                        [CbcAllowanceAmount],
                        [CbcChargeType],
                        [CbcChargeReasonCode],
                        [CbcChargeText],
                        [CbcChargeBaseAmount],
                        [CbcChargeMultiplierFactor],
                        [CbcChargeAmount],
                        [CbcPrice],
                        [CbcTaxExemptionDetails],
                        [CbcTaxExemptedAmount],
                        [CbcTotalExcludingTax],
                        [CbcItemClassificationCode],
                        [CbcProductTariffClass],
                        [CbcTaxSchemeID],
                        [CbcTaxSchemeAgencyID],
                        [CbcTaxSchemeAgencyCode],
                        [CbcInvoiceLineNetAmount],
                        [CbcNetAmount],
                        [ProductId],
                        [CbcItemClassificationClass],
                        [CbcProductTariffCode],
                        [CbcSubtotal],
                        [CbcSSTTaxCategory],
                        [CbcBaseQuantity]
                    ) 
                    VALUES (
                        @InvoiceId,
                        @CbcIDVATCategoryCode,
                        @CbcIDItemCountryOfOrigin,
                        @CbcDescription,
                        @CbcDescriptionCode,
                        @CbcBaseAmount,
                        @CbcAmount,
                        @CreatedBy,
                        @CreatedDate,
                        @UpdatedBy,
                        @UpdatedDate,
                        @LineId,
                        @CbcDiscountRate,
                        @CbcDiscountAmount,
                        @CbcTaxType,
                        @CbcTaxRate,
                        @CbcTaxAmount,
                        @CbcMeasure,
                        @CbcAllowanceType,
                        @CbcAllowanceReasonCode,
                        @CbcAllowanceText,
                        @CbcAllowanceBaseAmount,
                        @CbcAllowanceMultiplierFactor,
                        @CbcAllowanceAmount,
                        @CbcChargeType,
                        @CbcChargeReasonCode,
                        @CbcChargeText,
                        @CbcChargeBaseAmount,
                        @CbcChargeMultiplierFactor,
                        @CbcChargeAmount,
                        @CbcPrice,
                        @CbcTaxExemptionDetails,
                        @CbcTaxExemptedAmount,
                        @CbcTotalExcludingTax,
                        @CbcItemClassificationCode,
                        @CbcProductTariffClass,
                        @CbcTaxSchemeID,
                        @CbcTaxSchemeAgencyID,
                        @CbcTaxSchemeAgencyCode,
                        @CbcInvoiceLineNetAmount,
                        @CbcNetAmount,
                        @ProductId,
                        @CbcItemClassificationClass,
                        @CbcProductTariffCode,
                        @CbcSubtotal,
                        @CbcSSTTaxCategory,
                        @CbcBaseQuantity
                    ); 
                    SELECT SCOPE_IDENTITY();";

                // Execute the query and get the inserted ID
                var id = await _queryHelper.ExecuteScalar(sql, InsertInvoiceLineItemDataTake(invoiceLineItems));
                invoiceLineItems.Id = int.Parse(id.ToString());
                Log.Information($"Line Item Inserted of {invoiceLineItems.InvoiceId} Invoicenumber");
                Console.WriteLine($"Line Item Inserted of {invoiceLineItems.InvoiceId} Invoicenumber");

                return invoiceLineItems;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in InsertInvoicelineData service. {ex.Message}");
                Console.WriteLine($"Exception in InsertInvoicelineData service. {ex.Message}");
                throw;
            }
        }
        private List<IDataParameter> InsertInvoiceLineItemDataTake(InvoiceLineItems field)
        {
            var parameters = new List<IDataParameter>
                {
                    QueryHelper.CreateSqlParameter("@InvoiceId", field.InvoiceId, NpgsqlDbType.Integer),
                    QueryHelper.CreateSqlParameter("@CbcIDVATCategoryCode", field.CbcIDVATCategoryCode, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcIDItemCountryOfOrigin", field.CbcIDItemCountryOfOrigin, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcDescription", field.CbcDescription, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcDescriptionCode", field.CbcDescriptionCode, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcBaseAmount", field.CbcBaseAmount, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcAmount", field.CbcAmount, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CreatedBy", field.CreatedBy, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CreatedDate", field.CreatedDate, NpgsqlDbType.Date),
                    QueryHelper.CreateSqlParameter("@UpdatedBy", field.UpdatedBy, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@UpdatedDate", field.UpdatedDate, NpgsqlDbType.Date),
                    QueryHelper.CreateSqlParameter("@LineId", field.LineId, NpgsqlDbType.Integer),
                    QueryHelper.CreateSqlParameter("@CbcDiscountRate", field.CbcDiscountRate, NpgsqlDbType.Double),
                    QueryHelper.CreateSqlParameter("@CbcDiscountAmount", field.CbcDiscountAmount, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcTaxType", field.CbcTaxType, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcTaxRate", field.CbcTaxRate, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcTaxAmount", field.CbcTaxAmount, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcMeasure", field.CbcMeasure, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcAllowanceType", field.CbcAllowanceType, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcAllowanceReasonCode", field.CbcAllowanceReasonCode, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcAllowanceText", field.CbcAllowanceText, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcAllowanceBaseAmount", field.CbcAllowanceBaseAmount, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcAllowanceMultiplierFactor", field.CbcAllowanceMultiplierFactor, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcAllowanceAmount", field.CbcAllowanceAmount, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcChargeType", field.CbcChargeType, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcChargeReasonCode", field.CbcChargeReasonCode, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcChargeText", field.CbcChargeText, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcChargeBaseAmount", field.CbcChargeBaseAmount, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcChargeMultiplierFactor", field.CbcChargeMultiplierFactor, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcChargeAmount", field.CbcChargeAmount, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcPrice", field.CbcPrice, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcTaxExemptionDetails", field.CbcTaxExemptionDetails, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcTaxExemptedAmount", field.CbcTaxExemptedAmount, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcTotalExcludingTax", field.CbcTotalExcludingTax, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcItemClassificationCode", field.CbcItemClassificationCode, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcProductTariffClass", field.CbcProductTariffClass, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcTaxSchemeID", field.CbcTaxSchemeID, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcTaxSchemeAgencyID", field.CbcTaxSchemeAgencyID, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcTaxSchemeAgencyCode", field.CbcTaxSchemeAgencyCode, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcInvoiceLineNetAmount", field.CbcInvoiceLineNetAmount, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcNetAmount", field.CbcNetAmount, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@ProductId", field.ProductId, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcItemClassificationClass", field.CbcItemClassificationClass, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcProductTariffCode", field.CbcProductTariffCode, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcSubtotal", field.CbcSubtotal, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcSSTTaxCategory", field.CbcSSTTaxCategory, NpgsqlDbType.Varchar),
                    QueryHelper.CreateSqlParameter("@CbcBaseQuantity", field.CbcBaseQuantity, NpgsqlDbType.Varchar)
                 };

            return parameters;
        }
        async Task<DocTaxSubTotal> IETLService.InsertDocTaxSubTotal(DocTaxSubTotal docTaxSubTotal)
        {
            try
            {
                Log.Information("InsertDocTaxSubTotal service called.");
                Console.WriteLine("InsertDocTaxSubTotal service called.");
                const string sql = @"
                        INSERT INTO [dbo].[S_DocTaxSubTotal] (
                            [InvoiceId],
                            [TaxAmount],
                            [CategoryTotalLines],
                            [CategoryTaxCategory],
                            [CategoryTaxableAmount],
                            [CategoryTaxAmount],
                            [CategoryTaxRate],
                            [CategoryTaxExemptionReason],
                            [InvoiceLineItemId],
                            [CategoryTaxSchemeId],
                            [AmountExemptedFromTax],
                            [CbcTaxSchemeAgencyId],
                            [CbcTaxSchemeAgencyCode]
                        ) 
                        VALUES (
                            @InvoiceId,
                            @TaxAmount,
                            @CategoryTotalLines,
                            @CategoryTaxCategory,
                            @CategoryTaxableAmount,
                            @CategoryTaxAmount,
                            @CategoryTaxRate,
                            @CategoryTaxExemptionReason,
                            @InvoiceLineItemId,
                            @CategoryTaxSchemeId,
                            @AmountExemptedFromTax,
                            @CbcTaxSchemeAgencyId,
                            @CbcTaxSchemeAgencyCode
                        );
                        SELECT SCOPE_IDENTITY();";

                // Execute the query and get the inserted DocumentSubTotalId
                var id = await _queryHelper.ExecuteScalar(sql, InsertDocTaxSubTotalDataTake(docTaxSubTotal));
                docTaxSubTotal.DocumentSubTotalId = int.Parse(id.ToString());
                Log.Information($"DoctaxSubTotal Inserted of {docTaxSubTotal.InvoiceId} Invoicenumber");
                Console.WriteLine($"DocTaxSubTotalInserted of {docTaxSubTotal.InvoiceId} Invoicenumber");

                return docTaxSubTotal;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in InsertDocTaxSubTotal service. {ex.Message}");
                Console.WriteLine($"Exception in InsertDocTaxSubTotal service. {ex.Message}");
                throw;
            }
        }
        private List<IDataParameter> InsertDocTaxSubTotalDataTake(DocTaxSubTotal field)
        {
            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@DocumentSubTotalId", field.DocumentSubTotalId, NpgsqlDbType.Integer),
                QueryHelper.CreateSqlParameter("@InvoiceId", field.InvoiceId, NpgsqlDbType.Integer),
                QueryHelper.CreateSqlParameter("@TaxAmount", field.TaxAmount, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@CategoryTotalLines", field.CategoryTotalLines, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@CategoryTaxCategory", field.CategoryTaxCategory, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@CategoryTaxableAmount", field.CategoryTaxableAmount, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@CategoryTaxAmount", field.CategoryTaxAmount, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@CategoryTaxRate", field.CategoryTaxRate, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@CategoryTaxExemptionReason", field.CategoryTaxExemptionReason, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@InvoiceLineItemId", field.InvoiceLineItemId, NpgsqlDbType.Integer),
                QueryHelper.CreateSqlParameter("@CategoryTaxSchemeId", field.CategoryTaxSchemeId, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@AmountExemptedFromTax", field.AmountExemptedFromTax, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@CbcTaxSchemeAgencyId", field.CbcTaxSchemeAgencyId, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@CbcTaxSchemeAgencyCode", field.CbcTaxSchemeAgencyCode, NpgsqlDbType.Varchar)
            };
            return parameters;
        }
        #endregion

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

        public Task<bool> UpdateInvoiceDataStatus(string Status, int Id, string InvoiceCode)
        {
            throw new NotImplementedException();
        }
        public async Task<TenantDetails> GetConnectionString(string Domain)
        {
            string sql = $"SELECT * FROM public.\"TenantDetails\" Where \"Domain\"=@Domain";
            var parameters = new List<IDataParameter>
        {
            QueryHelper.CreateSqlParameter("@Domain", Domain, NpgsqlDbType.Varchar)
        };

            var value = (await _queryHelper.Read(sql, parameters, ConnectionStringMake)).FirstOrDefault();
            if (value != null)
            {
                // Cache the connection string
                ConnectionStringManager.SetConnectionString(value.ConnectionString);

                var initialConnectionString = ConnectionStringManager.IsConnectionStringCached()
                                    ? ConnectionStringManager.GetConnectionString()
                                    : null;
                _queryHelper = new QueryHelper(initialConnectionString);
                // return connection string
                return value;
            }
            else
            {
                // return connection string
                return value;
            }
        }
        public static class ConnectionStringManager
        {
            private static string _cachedConnectionString;

            public static string GetConnectionString()
            {
                return _cachedConnectionString;
            }
            public static void SetConnectionString(string connectionString)
            {
                var encPassword = connectionString.Split(';')[3].Substring(9);
                var password = SecurityHelper.DecryptWithEmbedKey(encPassword);
                connectionString = connectionString.Replace(encPassword, password);
                _cachedConnectionString = connectionString;
            }
            public static bool IsConnectionStringCached()
            {
                return !string.IsNullOrEmpty(_cachedConnectionString);
            }
        }


        public async Task SetConnectionString(string connectionstring)
        {
            var encPassword = connectionstring.Split(';')[2].Substring(9);
            var password = SecurityHelper.DecryptWithEmbedKey(encPassword);
            connectionstring = connectionstring.Replace(encPassword, password);

            //// Cache the connection string
            //ConnectionStringManager.SetConnectionString(connectionString);

            // Update QueryHelper with the new connection string
            _queryHelper = new QueryHelper(connectionstring);
        }

     

        private readonly Func<IDataReader, InvoiceData> InvoiceByIdMake = reader =>
        new InvoiceData
        {
            EInvoiceNumber = reader["eInvoiceNumber"].ToString(),
            Status = reader["Status"].ToString(),
        };
        private readonly Func<IDataReader, TenantDetails> ConnectionStringMake = reader =>
        new TenantDetails
        {
            ConnectionString = reader["ConnectionString"].ToString(),
        };
    }
}
