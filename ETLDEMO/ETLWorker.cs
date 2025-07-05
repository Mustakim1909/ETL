using DocumentFormat.OpenXml.Office2010.Excel;
using ETL_Demo.Models;
using ETL.Service.Model;
using ETL.Service.Repo.Interface;
using ETLDEMO.ETLHelperProcess;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System.Data;
using Common.DataAccess.MsSql;
using Common.Config;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using CsvHelper.Configuration;
using CsvHelper;
using FastReport;
using DocumentFormat.OpenXml.Office2013.Excel;
using System.Diagnostics;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Drawing.Charts;
using static ETL.Service.Repo.MSSQL.ETLService;
using System.Text.Json.Serialization;
using System.Text.Json;
using RestSharp.Serialization.Json;
using DocumentFormat.OpenXml.Office2013.ExcelAc;
using GroupDocs.Viewer.Options;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Azure.Core;
using Npgsql;
using Polly;
using Common.Security;
using System;
using ETL.ETLHelperProcess;
using System.Reflection;

namespace ETLDEMO
{
    public class ETLWorker : IHostedService
    {
        private readonly IETLService _etlDemoService;
        private readonly ETLHelper _eTLHelper;
        private readonly ETLAppSettings _appSettings;
        private readonly PDFMappingService pDFMappingService;
        private string _connectionString = null;
        private static readonly object _logLock = new object();
        public decimal totalInvoiceLineAmount = 0;
        public int totalLineItems = 0;
        public int temp = 0;
        public int isLast=0;
        public List<List<InvoiceData>> finalinvoicedata = new List<List<InvoiceData>>();

        public ETLWorker(IOptions<DbConfig> dbConfig, IETLService eTLDemoService, IOptions<ETLAppSettings> appsettings, ETLHelper eTLHelper,PDFMappingService pDFMapping)
        {
            _etlDemoService = eTLDemoService;
            _eTLHelper = eTLHelper;
            _appSettings = appsettings.Value;
            pDFMappingService = pDFMapping;
            var initialConnectionString = ConnectionStringManager.IsConnectionStringCached()
                                   ? ConnectionStringManager.GetConnectionString()
                                   : dbConfig.Value.ConnectionString;
            var encPassword = string.Empty;
            var password = string.Empty;
            if(dbConfig.Value.DataProvider.ToLower() == "sqlserver")
            {
                encPassword = initialConnectionString.Split(';')[3].Substring(10);
            }
           else
            {
                encPassword = initialConnectionString.Split(';')[3].Substring(9);
            }
                password = SecurityHelper.DecryptWithEmbedKey(encPassword);

            initialConnectionString = initialConnectionString.Replace(encPassword, password);
            _connectionString = initialConnectionString;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("Start");

                var commandArguments = Environment.GetCommandLineArgs();
                Console.WriteLine($"Command Arguments : {commandArguments[1]}");
                string filePathsFilePath = commandArguments[1];

                // Check if the file exists
                if (!File.Exists(filePathsFilePath))
                {
                    Console.WriteLine("Error: The file with file paths does not exist.");
                    return;
                }
                // Read the contents of the file

                string  filePathsJson = File.ReadAllText(filePathsFilePath);
                Console.WriteLine($"Filepathjson : {filePathsJson}");
                string[] filePaths = null;
            try
            {
                // Deserialize the JSON content back into the original file path array  
                 filePaths = JsonConvert.DeserializeObject<string[]>(filePathsJson);
           }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in filePathsJson : {ex.ToString()}");
                }
                var arg = filePaths;

                //List<string> args = JsonConvert.DeserializeObject<List<string>>(commandArguments[1]);
      //          List<string> taskTypes = arg
      //.Select(path => path.Contains("\\\\") ? path.Replace("\\\\", "\\") : path)
      //.ToList()
     
      List<string> taskTypes = arg.ToList();

                LogThreadSafe($"Main thread ID: {Thread.CurrentThread.ManagedThreadId}");

                // Log all arguments
                foreach (var taskType in arg)
                {
                    LogThreadSafe($"ARGS:- {taskType}");
                }

                // Group files by domain name and invoice type
                var domainFileGroups = taskTypes
                    .GroupBy(taskType =>
                    {
                        var path = taskType.Split(Path.DirectorySeparatorChar).ToList();
                        return new
                        {
                            //DomainName = path[path.Count - 5],  // Get the domain name
                            //InvoiceType = path[path.Count - 4]  // Extract the invoice type

                            DomainName = path[path.Count - 6],  // Get the domain name
                            InvoiceType = path[path.Count - 5]  // Extract the invoice type
                        };
                    })
                    .ToDictionary(g => g.Key, g => g.ToList());

                var failedTasks = new List<string>();

                var domainCheck = false;
                // Iterate over each domain and process the files concurrently
                foreach (var domainGroup in domainFileGroups)
                {
                    var domainName = domainGroup.Key.DomainName;
                    var invoiceType = domainGroup.Key.InvoiceType;
                    var domainFiles = domainGroup.Value;

                    LogThreadSafe($"Processing files for domain: {domainName}, invoiceType: {invoiceType}");

                    try
                    {
                        // Read all lines from the CSV file into a list
                        if (!domainCheck)
                        {
                            await _etlDemoService.GetConnectionString(domainName);
                            domainCheck = true;
                        }
                            

                            await ProcessFileAsync(domainFiles, domainName, invoiceType, temp);
                            // Pass all files for the domain and invoice type in a single call to ProcessFileAsync

                        
                    }
                    catch (Exception ex)
                    {   
                        LogThreadSafe($"Error processing files for domain {domainName}: {ex.Message}");
                        failedTasks.AddRange(domainFiles);  // Add all files from this domain group to failed tasks
                    }
                }

                // Log failed tasks if there are any
                if (failedTasks.Any())
                {
                    LogThreadSafe("The following files were not found and skipped:");
                    foreach (var failedFile in failedTasks)
                    {
                        LogThreadSafe(failedFile);
                    }   
                }
            }
            catch (Exception ex)
            {
                // Handle general exceptions
                LogThreadSafe($"Exception in StartAsync: {ex.Message}");
                throw;
            }
        }

        // New method that processes a batch of files
      
        private void LogThreadSafe(string message)
        {
            lock (_logLock)
            {
                Console.WriteLine(message);
                Log.Information(message); // Assuming Serilog is being used for logging
            }
        }
        private async Task ProcessFileAsync(List<string> tasktype, string domainname, string invoicetype, int temp)
        {
            try
            {
                // Fetch connection string asynchronously

                const int batchSize = 1000;  // You can adjust this based on your system's capacity
                var tasks = new List<Task>();

                // Process tasks in batches
                for (int i = 0; i < tasktype.Count; i += batchSize)
                {
                    var batch = tasktype.Skip(i).Take(batchSize);

                    // Check if this is the last batch
                    bool isLastBatch = (i + batchSize >= tasktype.Count);

                    // Create and start tasks for each batch
                    tasks.AddRange(batch.Select((task, index) =>
                    {
                        // If it's the last batch, set isLast to 1 for the last task
                        bool isLast = isLastBatch && (i + index + 1 == tasktype.Count);
                        return ProcessTaskAsync(task, domainname, invoicetype, isLast ? 1 : 0);
                    }));

                    // Wait for the batch to finish before starting the next one
                    if (tasks.Count >= batchSize)
                    {
                        await Task.WhenAll(tasks);  // Await all tasks concurrently
                        tasks.Clear();  // Clear the tasks list after processing the batch
                    }
                }

                // Await remaining tasks (if any)
                if (tasks.Any())
                {
                    await Task.WhenAll(tasks);
                }

            }
            catch (Exception ex)
            {
                // Handle general exceptions
                LogThreadSafe($"Exception in ProcessFileAsync : {ex.Message}");
                Console.WriteLine($"Exception in ProcessFileAsync : {ex.Message}");
                throw;
            }

        }


        private async Task ProcessTaskAsync(string tasktype, string domainname, string invoicetype,int isLast)
        {
            try
            {
                string invtypecode = string.Empty;
                int count = 0;
                string totalamount;
                HashSet<string> processedInvoiceNumbers = new HashSet<string>();
                var stopwatch = Stopwatch.StartNew();
                string path = string.Empty;
                string path1 = string.Empty;
                string message = string.Empty;
                List<string> files = new List<string>();
                var despath = String.Empty;
                string doctaxsubtotaljson = string.Empty;
                string invoicedatajson = string.Empty;
                List<InvoiceLineItems> invoicelineitemsjson = new List<InvoiceLineItems>();
                List<InvoiceData> invoicedata = new List<InvoiceData>();
                List<List<InvoiceLineItems>> invoicelineitems = new List<List<InvoiceLineItems>>();
                List<DocTaxSubTotal> doctaxsubtotal = new List<DocTaxSubTotal>();
                List<InvoiceCSVData> invoiceCSVDatas = new List<InvoiceCSVData>();
                invoicelineitemsjson.Clear();
                string finalJsonArray = string.Empty;
                string finalinvoice = string.Empty;
                string finallineitems = string.Empty;
                string finaldoctax = string.Empty;
                List<string> filepath = new List<string>();
                string file = string.Empty;
                List<string> csvRecords = new List<string>(File.ReadAllLines(tasktype));
                //await _etlDemoService.GetConnectionString(domainname);
                /*foreach (var task in tasktype)
                {*/
                var filename = Path.GetFileNameWithoutExtension(tasktype); // Get the file name without the extension
                var extractedInvoiceNumber = ExtractInvoiceNumberFromFileName(filename);
                var extractInvoiceTypeCode = ExtractInvoiceTypeFromFileName(filename);
                // var domainname = ExtractDomainNameFromFileName(taskType);

                // var con = _etlDemoService.GetConnectionString(domainname);
                Console.WriteLine($"Extracted Invoice Number: {extractedInvoiceNumber}");
                Console.WriteLine($"Extracted Invoice Type: {extractInvoiceTypeCode}");
                Console.WriteLine($"Extracted Domain Name: {domainname}");



                var invoiceCSVData = await _eTLHelper.ReadCsv<InvoiceCSVData>(tasktype, new InvoiceDataMap(_etlDemoService, invoicetype));


                if (invoiceCSVData.Count() == 0)
                {
                    Console.WriteLine("Not found csv data please check.");
                    Environment.Exit(1);
                }
                else
                {
                    Console.WriteLine("CSV Data Found");
                }
                // Get distinct InvoiceLineItems based on InvoiceNumber
                /* var invoiceData = invoiceCSVData
                       .GroupBy(invoice => invoice.EInvoiceNumber)
                       .Select(group => group.First())
                       .ToList();*/
                if (invoiceCSVData.Count() > 0)
                {

                    var lineItems = new InvoiceLineItems();
                    var Invoivcecsvdata = new InvoiceCSVData();
                    if (invoiceCSVData.Select(x => x.CbcInvoiceTypeCode).FirstOrDefault().Length < 2)
                    {
                        // Update CbcInvoiceTypeCode for all items
                        invoiceCSVData.ForEach(x => x.CbcInvoiceTypeCode = $"0{x.CbcInvoiceTypeCode}");
                    }
                  
                    if (invoiceCSVData.Any(item => item.EInvoiceNumber.Contains(extractedInvoiceNumber) && item.CbcInvoiceTypeCode == extractInvoiceTypeCode))
                    {
                        #region InvoiceData
                        var InvoiceCSVData = new InvoiceData()
                        {
                            Id = invoiceCSVData.Select(x => x.Id).FirstOrDefault(),
                            CbcAmount = invoiceCSVData.Select(x => x.CbcAmount).FirstOrDefault(),
                            CacAddress = invoiceCSVData.Select(x => x.CacAddress).FirstOrDefault(),
                            //InvoiceDate = item.InvoiceDate,
                            EInvoiceDateTime = invoiceCSVData.Select(x => x.EInvoiceDateTime).FirstOrDefault(),
                            //InvoiceDate = invoiceCSVData.Select(x => x.InvoiceDate).FirstOrDefault().Value.Date,
                            //InvoiceTime = invoiceCSVData.Select(x => x.InvoiceTime).FirstOrDefault(),
                            RefNo = invoiceCSVData.Select(x => x.RefNo).FirstOrDefault(),
                            BP_CODE = invoiceCSVData.Select(x => x.BP_CODE).FirstOrDefault(),
                            TaxCategoryUNECE5153 = invoiceCSVData.Select(x => x.TaxCategoryUNECE5153).FirstOrDefault(),
                            TaxCategoryUNCL5305 = invoiceCSVData.Select(x => x.TaxCategoryUNCL5305).FirstOrDefault(),
                            TaxRateApplicable = invoiceCSVData.Select(x => x.TaxRateApplicable).FirstOrDefault(),
                            AllowancePercentage = invoiceCSVData.Select(x => x.AllowancePercentage).FirstOrDefault(),
                            CbcChargeRate = invoiceCSVData.Select(x => x.CbcChargeRate).FirstOrDefault(),
                            CbcSumOfInvoiceLineNetAmount = invoiceCSVData.Select(x => x.CbcSumOfInvoiceLineNetAmount).FirstOrDefault(),
                            CacInvoicePeriod = invoiceCSVData.Select(x => x.CacInvoicePeriod).FirstOrDefault(),
                            TotalIncludingTax = invoiceCSVData.Select(x => x.TotalIncludingTax).FirstOrDefault(),
                            TotalPayableAmount = invoiceCSVData.Select(x => x.TotalPayableAmount).FirstOrDefault(),
                            TotalDiscountValue = invoiceCSVData.Select(x => x.TotalDiscountValue).FirstOrDefault(),
                            InvoiceAdditionalDiscount = invoiceCSVData.Select(x => x.InvoiceAdditionalDiscount).FirstOrDefault(),
                            InvoiceAdditionalFee = invoiceCSVData.Select(x => x.InvoiceAdditionalFee).FirstOrDefault(),
                            CacPostalSellerAddress = invoiceCSVData.Select(x => x.CacPostalSellerAddress).FirstOrDefault(),
                            CacPrice = invoiceCSVData.Select(x => x.CacPrice).FirstOrDefault(),
                            CacTaxTotal = invoiceCSVData.Select(x => x.CacTaxTotal).FirstOrDefault(),
                            CbcBaseAmount = invoiceCSVData.Select(x => x.CbcBaseAmount).FirstOrDefault(),
                            CbcBaseQuantity = invoiceCSVData.Select(x => x.CbcBaseQuantity).FirstOrDefault(),
                          
                            CbcCompanyLegalForm = invoiceCSVData.Select(x => x.CbcCompanyLegalForm).FirstOrDefault(),
                            CbcDescription = invoiceCSVData.Select(x => x.CbcDescription).FirstOrDefault(),
                            CbcDescriptionCode = invoiceCSVData.Select(x => x.CbcDescriptionCode).FirstOrDefault(),
                            CbcDocumentCurrencyCode = invoiceCSVData.Select(x => x.CbcDocumentCurrencyCode).FirstOrDefault(),
                            CbcIDInvoiceNumber = invoiceCSVData.Select(x => x.CbcIDInvoiceNumber).FirstOrDefault(),
                            CbcPrecedingInvoicenumber = invoiceCSVData.Select(x => x.CbcPrecedingInvoicenumber).FirstOrDefault(),
                            CbcIDPaymentAccountIdentifier = invoiceCSVData.Select(x => x.CbcIDPaymentAccountIdentifier).FirstOrDefault(),
                            CbcIDVATcategoryCode = invoiceCSVData.Select(x => x.CbcIDVATcategoryCode).FirstOrDefault(),
                            CbcIDItemCountryOfOrigin = invoiceCSVData.Select(x => x.CbcIDItemCountryOfOrigin).FirstOrDefault(),
                            CbcIdentificationCode = invoiceCSVData.Select(x => x.CbcIdentificationCode).FirstOrDefault(),
                            CbcInvoiceTypeCode = invoiceCSVData.Select(x => x.CbcInvoiceTypeCode).FirstOrDefault(),
                            CbcIssueDate = invoiceCSVData.Select(x => x.CbcIssueDate).FirstOrDefault(),
                            CbcLineExtensionAmount = invoiceCSVData.Select(x => x.CbcLineExtensionAmount).FirstOrDefault(),
                            CbcNameDeliverToPartyName = invoiceCSVData.Select(x => x.CbcNameDeliverToPartyName).FirstOrDefault(),
                            CbcNote = invoiceCSVData.Select(x => x.CbcNote).FirstOrDefault(),
                            CbcPayableAmount = invoiceCSVData.Select(x => x.CbcPayableAmount).FirstOrDefault(),
                            CbcPaymentID = invoiceCSVData.Select(x => x.CbcPaymentID).FirstOrDefault(),
                            CbcPercent = invoiceCSVData.Select(x => x.CbcPercent).FirstOrDefault(),
                            
                            CbcTaxableAmount = invoiceCSVData.Select(x => x.CbcTaxableAmount).FirstOrDefault(),
                            CbcTaxCurrencyCode = invoiceCSVData.Select(x => x.CbcTaxCurrencyCode).FirstOrDefault(),
                            CbcTaxExclusiveAmount = invoiceCSVData.Select(x => x.CbcTaxExclusiveAmount).FirstOrDefault(),
                            CbcTaxExemptionReason = invoiceCSVData.Select(x => x.CbcTaxExemptionReason).FirstOrDefault(),
                            CbcTaxInclusiveAmount = invoiceCSVData.Select(x => x.CbcTaxInclusiveAmount).FirstOrDefault(),
                            CbcTaxPointDate = invoiceCSVData.Select(x => x.CbcTaxPointDate).FirstOrDefault(),
                            NamePaymentMeansText = invoiceCSVData.Select(x => x.NamePaymentMeansText).FirstOrDefault(),
                            SchemeID = invoiceCSVData.Select(x => x.SchemeID).FirstOrDefault(),
                            UnitCode = invoiceCSVData.Select(x => x.UnitCode).FirstOrDefault(),
                            IRBMUniqueNo = invoiceCSVData.Select(x => x.IRBMUniqueNo).FirstOrDefault(),
                            PaymentDueDate = invoiceCSVData.Select(x => x.paymentDueDate).FirstOrDefault(),
                            CbcPaymentCurrencyCode = invoiceCSVData.Select(x => x.CbcPaymentCurrencyCode).FirstOrDefault(),
                            TotalLineAmount = invoiceCSVData.Select(x => x.CbcSumOfInvoiceLineNetAmount).FirstOrDefault(),
                            TotalChangeAmount = invoiceCSVData.Select(x => x.TotalChangeAmount).FirstOrDefault(),
                            TotalAllowanceAmount = invoiceCSVData.Select(x => x.TotalAllowanceAmount).FirstOrDefault(),
                            TotalTaxAmount = invoiceCSVData.Select(x => x.TotalTaxAmount).FirstOrDefault(),
                            PayableRoundingAmount = invoiceCSVData.Select(x => x.PayableRoundingAmount).FirstOrDefault(),
                            PrePaidAmount = invoiceCSVData.Select(x => x.PrePaidAmount).FirstOrDefault(),
                            TotalAmountDue = invoiceCSVData.Select(x => x.TotalAmountDue).FirstOrDefault(),
                            Mode = invoiceCSVData.Select(x => x.Mode).FirstOrDefault(),
                            EInvoiceType = invoiceCSVData.Select(x => x.EInvoiceType).FirstOrDefault(),
                            eTemplateId = invoiceCSVData.Select(x => x.eTemplateId).FirstOrDefault(),
                            templateId = invoiceCSVData.Select(x => x.templateId).FirstOrDefault(),
                            Status = invoiceCSVData.Select(x => x.Status).FirstOrDefault(),
                            //EInvoiceDateTime = Convert.ToDateTime(item.InvoiceDateTime),
                            invoiceValidator = invoiceCSVData.Select(x => x.invoiceValidator).FirstOrDefault(),
                            TaxOfficeSubmitter = invoiceCSVData.Select(x => x.TaxOfficeSubmitter).FirstOrDefault(),
                            WorkflowStatus = invoiceCSVData.Select(x => x.WorkflowStatus).FirstOrDefault(),
                            Comments = invoiceCSVData.Select(x => x.Comments).FirstOrDefault(),
                            BulkGuid = invoiceCSVData.Select(x => x.BulkGuid).FirstOrDefault(),
                            ApprovalType = invoiceCSVData.Select(x => x.ApprovalType).FirstOrDefault(),
                            LastDraftSaveDate = invoiceCSVData.Select(x => x.LastDraftSaveDate).FirstOrDefault(),
                            PendingProcessingDate = invoiceCSVData.Select(x => x.PendingProcessingDate).FirstOrDefault(),
                            Checker1ActionDate = invoiceCSVData.Select(x => x.Checker1ActionDate).FirstOrDefault(),
                            Checker2ActionDate = invoiceCSVData.Select(x => x.Checker2ActionDate).FirstOrDefault(),
                            Checke2ActionTime = invoiceCSVData.Select(x => x.Checke2ActionTime).FirstOrDefault(),
                            SubmittedToIRBDate = invoiceCSVData.Select(x => x.SubmittedToIRBDate).FirstOrDefault(),
                            IRBResponseDate = invoiceCSVData.Select(x => x.IRBResponseDate).FirstOrDefault(),
                            PdfBlob = invoiceCSVData.Select(x => x.PdfBlob).FirstOrDefault(),
                            PdfXml = invoiceCSVData.Select(x => x.PdfXml).FirstOrDefault(),
                            Priority = invoiceCSVData.Select(x => x.Priority).FirstOrDefault(),
                            CreatedDate = invoiceCSVData.Select(x => x.CreatedDate).FirstOrDefault(),
                            CreatedBy = invoiceCSVData.Select(x => x.CreatedBy).FirstOrDefault(),
                            UpdatedDate = invoiceCSVData.Select(x => x.UpdatedDate).FirstOrDefault(),
                            UpdatedBy = invoiceCSVData.Select(x => x.UpdatedBy).FirstOrDefault(),
                            PdfWithQRBlob = invoiceCSVData.Select(x => x.PdfWithQRBlob).FirstOrDefault(),
                            XmlWithQRBlob = invoiceCSVData.Select(x => x.XmlWithQRBlob).FirstOrDefault(),
                            JsonInvoiceBlob = invoiceCSVData.Select(x => x.JsonInvoiceBlob).FirstOrDefault(),
                            JsonWithQRBlob = invoiceCSVData.Select(x => x.JsonWithQRBlob).FirstOrDefault(),
                            CacAddress2 = invoiceCSVData.Select(x => x.CacAddress2).FirstOrDefault(),
                            CacAddress3 = invoiceCSVData.Select(x => x.CacAddress3).FirstOrDefault(),
                            CacAddress4 = invoiceCSVData.Select(x => x.CacAddress4).FirstOrDefault(),
                            CacSellerEmail = invoiceCSVData.Select(x => x.CbcSellerElectronicMail).FirstOrDefault(),
                            ReltedInvoiceId = invoiceCSVData.Select(x => x.ReltedInvoiceId).FirstOrDefault(),
                            EInvoiceNumber = invoiceCSVData.Select(x => x.EInvoiceNumber).FirstOrDefault(),
                            TaxOfficeSchedulerId = invoiceCSVData.Select(x => x.TaxOfficeSchedulerId).FirstOrDefault(),
                            InvoiceVersion = invoiceCSVData.Select(x => x.InvoiceVersion).FirstOrDefault(),
                            CbcDStreetName = invoiceCSVData.Select(x => x.CbcDStreetName).FirstOrDefault(),
                            CbcDAdditionalStreetName1 = invoiceCSVData.Select(x => x.CbcDAdditionalStreetName1).FirstOrDefault(),
                            CbcDAdditionalStreetName2 = invoiceCSVData.Select(x => x.CbcDAdditionalStreetName2).FirstOrDefault(),
                            CbcDPostalZone = invoiceCSVData.Select(x => x.CbcDPostalZone).FirstOrDefault(),
                            CbcDCityName = invoiceCSVData.Select(x => x.CbcDCityName).FirstOrDefault(),
                            CbcDCountrySubentity = invoiceCSVData.Select(x => x.CbcDCountrySubentity).FirstOrDefault(),
                            CbcDCountryIdentificationCode = invoiceCSVData.Select(x => x.CbcDCountryIdentificationCode).FirstOrDefault(),
                            CbcShipRecipientName = invoiceCSVData.Select(x => x.CbcShipRecipientName).FirstOrDefault(),
                            CbcShipRecipientVATID = invoiceCSVData.Select(x => x.CbcShipRecipientVATID).FirstOrDefault(),
                            CbcShipRecipientCompanyID = invoiceCSVData.Select(x => x.CbcShipRecipientCompanyID).FirstOrDefault(),
                            CbcShipRecipientStreetName = invoiceCSVData.Select(x => x.CbcShipRecipientStreetName).FirstOrDefault(),
                            CbcShipRecipientAdditionalStreetName1 = invoiceCSVData.Select(x => x.CbcShipRecipientAdditionalStreetName1).FirstOrDefault(),
                            CbcShipRecipientAdditionalStreetName2 = invoiceCSVData.Select(x => x.CbcShipRecipientAdditionalStreetName2).FirstOrDefault(),
                            CbcShipRecipientPostalZone = invoiceCSVData.Select(x => x.CbcShipRecipientPostalZone).FirstOrDefault(),
                            CbcShipRecipientCityName = invoiceCSVData.Select(x => x.CbcShipRecipientCityName).FirstOrDefault(),
                            CbcShipRecipientCountrySubentity = invoiceCSVData.Select(x => x.CbcShipRecipientCountrySubentity).FirstOrDefault(),
                            CbcShipRecipientCountryIdentificationCode = invoiceCSVData.Select(x => x.CbcShipRecipientCountryIdentificationCode).FirstOrDefault(),
                            CbcCalculationRate = invoiceCSVData.Select(x => x.CbcCalculationRate).FirstOrDefault(),
                            CbcStartDate = invoiceCSVData.Select(x => x.CbcStartDate).FirstOrDefault(),
                            CbcEndDate = invoiceCSVData.Select(x => x.CbcEndDate).FirstOrDefault(),
                           
                           
                            CbcShipRecipientCategory = invoiceCSVData.Select(x => x.CbcShipRecipientCategory).FirstOrDefault(),
                            CbcShipRecipientSubCategory = invoiceCSVData.Select(x => x.CbcShipRecipientSubCategory).FirstOrDefault(),
                            CbcShipRecipientBRNNumber = invoiceCSVData.Select(x => x.CbcShipRecipientBRNNumber).FirstOrDefault(),
                            CbcShipRecipientNRIC = invoiceCSVData.Select(x => x.CbcShipRecipientNRIC).FirstOrDefault(),
                            CacPaymentTerms = invoiceCSVData.Select(x => x.CacPaymentTerms).FirstOrDefault(),
                            CbcPaidDate = invoiceCSVData.Select(x => x.CbcPaidDate).FirstOrDefault(),
                            CbcPaidTime = invoiceCSVData.Select(x => x.CbcPaidTime).FirstOrDefault(),
                            CbcPaidId = invoiceCSVData.Select(x => x.CbcPaidId).FirstOrDefault(),
                            CbcItemClassificationCodeClass = invoiceCSVData.Select(x => x.CbcItemClassificationCodeClass).FirstOrDefault(),
                            CbcItemClassificationCodePTC = invoiceCSVData.Select(x => x.CbcItemClassificationCodePTC).FirstOrDefault(),
                            CbcSourceInvoiceNumber = invoiceCSVData.Select(x => x.CbcSourceInvoiceNumber).FirstOrDefault(),
                            WorkFlowOption = invoiceCSVData.Select(x => x.WorkFlowOption).FirstOrDefault(),
                            RejectRequestDateTime = invoiceCSVData.Select(x => x.RejectRequestDateTime).FirstOrDefault(),
                            RejectionStatusReason = invoiceCSVData.Select(x => x.RejectionStatusReason).FirstOrDefault(),
                            CancelDateTime = invoiceCSVData.Select(x => x.CancelDateTime).FirstOrDefault(),
                            CancelStatusReason = invoiceCSVData.Select(x => x.CancelStatusReason).FirstOrDefault(),
                            CancelledsubmıttedID = invoiceCSVData.Select(x => x.CancelledsubmıttedID).FirstOrDefault(),
                            IRBMUniqueIdentifierNumber = invoiceCSVData.Select(x => x.IRBMUniqueIdentifierNumber).FirstOrDefault(),
                            InvoiceDocumentReferenceNumber = invoiceCSVData.Select(x => x.InvoiceDocumentReference).FirstOrDefault(),
                            CbcCustomizationID = invoiceCSVData.Select(x => x.CbcCustomizationID).FirstOrDefault(),
                            CbcProfileID = invoiceCSVData.Select(x => x.CbcProfileID).FirstOrDefault(),
                            CbcDueDate = invoiceCSVData.Select(x => x.CbcDueDate).FirstOrDefault(),
                            CbcAccountingCost = invoiceCSVData.Select(x => x.CbcAccountingCost).FirstOrDefault(),
                            CbcOrderReferenceId = invoiceCSVData.Select(x => x.CbcOrderReferenceId).FirstOrDefault(),
                            CbcSalesOrderID = invoiceCSVData.Select(x => x.CbcSalesOrderID).FirstOrDefault(),
                            CbcEndpointId = invoiceCSVData.Select(x => x.CbcEndpointId).FirstOrDefault(),
                            CbcEndpointIdschemeID = invoiceCSVData.Select(x => x.CbcEndpointIdschemeID).FirstOrDefault(),
                            CbcPartyTaxSchemeCompanyID = invoiceCSVData.Select(x => x.CbcPartyTaxSchemeCompanyID).FirstOrDefault(),
                            CbcPartyTaxSchemeID = invoiceCSVData.Select(x => x.CbcPartyTaxSchemeID).FirstOrDefault(),
                            CbcPartyLegalEntityCompanyID = invoiceCSVData.Select(x => x.CbcPartyLegalEntityCompanyID).FirstOrDefault(),
                            CbcPartyLegalEntityCompanyLegalForm = invoiceCSVData.Select(x => x.CbcPartyLegalEntityCompanyLegalForm).FirstOrDefault(),
                           
                            CbcActualDeliveryDate = invoiceCSVData.Select(x => x.CbcActualDeliveryDate).FirstOrDefault(),
                            CbcDeliveryLocationId = invoiceCSVData.Select(x => x.CbcDeliveryLocationId).FirstOrDefault(),
                            CbcDeliveryStreetName = invoiceCSVData.Select(x => x.CbcDeliveryStreetName).FirstOrDefault(),
                            CbcDeliveryAdditionalStreetName = invoiceCSVData.Select(x => x.CbcDeliveryAdditionalStreetName).FirstOrDefault(),
                            CbcDeliveryCityName = invoiceCSVData.Select(x => x.CbcDeliveryCityName).FirstOrDefault(),
                            CbcDeliveryPostalZone = invoiceCSVData.Select(x => x.CbcDeliveryPostalZone).FirstOrDefault(),
                            CbcDeliveryAddressLine = invoiceCSVData.Select(x => x.CbcDeliveryAddressLine).FirstOrDefault(),
                            CbcDeliveryCountryIdentificationCode = invoiceCSVData.Select(x => x.CbcDeliveryCountryIdentificationCode).FirstOrDefault(),
                            CacDeliveryPartyName = invoiceCSVData.Select(x => x.CacDeliveryPartyName).FirstOrDefault(),
                            IRBMValidationDate = invoiceCSVData.Select(x => x.IRBMValidationDate).FirstOrDefault(),
                            BillerId = invoiceCSVData.Select(x => x.BillerId).FirstOrDefault(),
                            ValidationDate = invoiceCSVData.Select(x => x.ValidationDate).FirstOrDefault(),
                            ValidityHours = invoiceCSVData.Select(x => x.ValidityHours).FirstOrDefault(),
                            InvoiceDocumentReference = invoiceCSVData.Select(x => x.InvoiceDocumentReference).FirstOrDefault(),
                            QRCode = invoiceCSVData.Select(x => x.QRCode).FirstOrDefault(),
                            IRBMValidationTime = invoiceCSVData.Select(x => x.IRBMValidationTime).FirstOrDefault(),
                            RemainingHours = invoiceCSVData.Select(x => x.RemainingHours).FirstOrDefault(),
                            SourceFileName = invoiceCSVData.Select(x => x.SourceFileName).FirstOrDefault(),
                            SourceName = invoiceCSVData.Select(x => x.SourceName).FirstOrDefault(),
                            FıleName = invoiceCSVData.Select(x => x.FıleName).FirstOrDefault(),
                            DataTıme = invoiceCSVData.Select(x => x.DataTıme).FirstOrDefault(),
                            FolderName = invoiceCSVData.Select(x => x.FolderName).FirstOrDefault(),
                            InvoıceCreatorName = invoiceCSVData.Select(x => x.InvoıceCreatorName).FirstOrDefault(),
                            SourceInvoıceNumber = invoiceCSVData.Select(x => x.SourceInvoıceNumber).FirstOrDefault(),
                            InvoıceNumberStatus = invoiceCSVData.Select(x => x.InvoıceNumberStatus).FirstOrDefault(),
                            ProcessType = invoiceCSVData.Select(x => x.ProcessType).FirstOrDefault(),
                            WorkflowType = invoiceCSVData.Select(x => x.WorkflowType).FirstOrDefault(),
                            SchedulerName = invoiceCSVData.Select(x => x.SchedulerName).FirstOrDefault(),
                            TaskID = invoiceCSVData.Select(x => x.TaskID).FirstOrDefault(),
                            CreationDateTıme = invoiceCSVData.Select(x => x.CreationDateTıme).FirstOrDefault(),
                            CreatorID = invoiceCSVData.Select(x => x.CreatorID).FirstOrDefault(),
                            CreationNotes = invoiceCSVData.Select(x => x.CreationNotes).FirstOrDefault(),
                            CreationSubmissionDateTime = invoiceCSVData.Select(x => x.CreationSubmissionDateTime).FirstOrDefault(),
                            CreationApprovalDateTıme = invoiceCSVData.Select(x => x.CreationApprovalDateTıme).FirstOrDefault(),
                            CreationApprovalID = invoiceCSVData.Select(x => x.CreationApprovalID).FirstOrDefault(),
                            CreationApprovalNotes = invoiceCSVData.Select(x => x.CreationApprovalNotes).FirstOrDefault(),
                            CreationApprovalStatus = invoiceCSVData.Select(x => x.CreationApprovalStatus).FirstOrDefault(),
                            VerifıcationApprovalDateTıme = invoiceCSVData.Select(x => x.VerifıcationApprovalDateTıme).FirstOrDefault(),
                            CreationApproverlID = invoiceCSVData.Select(x => x.CreationApproverlID).FirstOrDefault(),
                            VerificationApprovalID = invoiceCSVData.Select(x => x.VerificationApprovalID).FirstOrDefault(),
                            VerificationApproverlID = invoiceCSVData.Select(x => x.VerificationApproverlID).FirstOrDefault(),
                            VerificationApprovalNotes = invoiceCSVData.Select(x => x.VerificationApprovalNotes).FirstOrDefault(),
                            GenerationApprovalID = invoiceCSVData.Select(x => x.GenerationApprovalID).FirstOrDefault(),
                            GenerationApproverlID = invoiceCSVData.Select(x => x.GenerationApproverlID).FirstOrDefault(),
                            GenerationApprovalNotes = invoiceCSVData.Select(x => x.GenerationApprovalNotes).FirstOrDefault(),
                            GenerationApprovalStatus = invoiceCSVData.Select(x => x.GenerationApprovalStatus).FirstOrDefault(),
                            GenerationApprovalDateTıme = invoiceCSVData.Select(x => x.GenerationApprovalDateTıme).FirstOrDefault(),
                            ValidationApproverlID = invoiceCSVData.Select(x => x.ValidationApproverlID).FirstOrDefault(),
                            ValidationApprovalNotes = invoiceCSVData.Select(x => x.ValidationApprovalNotes).FirstOrDefault(),
                            ValidationApprovalStatus = invoiceCSVData.Select(x => x.ValidationApprovalStatus).FirstOrDefault(),
                            ValidationApprovalDateTıme = invoiceCSVData.Select(x => x.ValidationApprovalDateTıme).FirstOrDefault(),
                            SubmissionDateTıme = invoiceCSVData.Select(x => x.SubmissionDateTıme).FirstOrDefault(),
                            SubmitterID = invoiceCSVData.Select(x => x.SubmitterID).FirstOrDefault(),
                            SubmissionNotes = invoiceCSVData.Select(x => x.SubmissionNotes).FirstOrDefault(),
                            SubmissionApprovalDateTıme = invoiceCSVData.Select(x => x.SubmissionApprovalDateTıme).FirstOrDefault(),
                            SubmissionApprovalSubmıtDateTıme = invoiceCSVData.Select(x => x.SubmissionApprovalSubmıtDateTıme).FirstOrDefault(),
                            SubmissionApprovalID = invoiceCSVData.Select(x => x.SubmissionApprovalID).FirstOrDefault(),
                            SubmissionApprovalNotes = invoiceCSVData.Select(x => x.SubmissionApprovalNotes).FirstOrDefault(),
                            SubmissionApprovalStatus = invoiceCSVData.Select(x => x.SubmissionApprovalStatus).FirstOrDefault(),
                            RetryCount = invoiceCSVData.Select(x => x.RetryCount).FirstOrDefault(),
                            ValidityEndDateTıme = invoiceCSVData.Select(x => x.ValidityEndDateTıme).FirstOrDefault(),
                            ValidityStatus = invoiceCSVData.Select(x => x.ValidityStatus).FirstOrDefault(),
                            RejectionDateTıme = invoiceCSVData.Select(x => x.RejectionDateTıme).FirstOrDefault(),
                            RejectionReasons = invoiceCSVData.Select(x => x.RejectionReasons).FirstOrDefault(),
                            RejectionWFCheckerID = invoiceCSVData.Select(x => x.RejectionWFCheckerID).FirstOrDefault(),
                            RejectionWFCheckerStatus = invoiceCSVData.Select(x => x.RejectionWFCheckerStatus).FirstOrDefault(),
                            RejectionWFCheckerSubmitDateTıme = invoiceCSVData.Select(x => x.RejectionWFCheckerSubmitDateTıme).FirstOrDefault(),
                            RejectionWFApproverID = invoiceCSVData.Select(x => x.RejectionWFApproverID).FirstOrDefault(),
                            RejectionWFApprovalStatus = invoiceCSVData.Select(x => x.RejectionWFApprovalStatus).FirstOrDefault(),
                            RejectionWFApprovalNotes = invoiceCSVData.Select(x => x.RejectionWFApprovalNotes).FirstOrDefault(),
                            RejectionWFApprovalSubmitDateTıme = invoiceCSVData.Select(x => x.RejectionWFApprovalSubmitDateTıme).FirstOrDefault(),
                            CancellationDateTıme = invoiceCSVData.Select(x => x.CancellationDateTıme).FirstOrDefault(),
                            CancellationReasons = invoiceCSVData.Select(x => x.CancellationReasons).FirstOrDefault(),
                            CancellationWFCheckerID = invoiceCSVData.Select(x => x.CancellationWFCheckerID).FirstOrDefault(),
                            CancellationWFCheckerStatus = invoiceCSVData.Select(x => x.CancellationWFCheckerStatus).FirstOrDefault(),
                            CancellationWFCheckerSubmıtDateTıme = invoiceCSVData.Select(x => x.CancellationWFCheckerSubmıtDateTıme).FirstOrDefault(),
                            CancellationWFApproverID = invoiceCSVData.Select(x => x.CancellationWFApproverID).FirstOrDefault(),
                            CancellationWFApprovalStatus = invoiceCSVData.Select(x => x.CancellationWFApprovalStatus).FirstOrDefault(),
                            CancellationWFApprovalNotes = invoiceCSVData.Select(x => x.CancellationWFApprovalNotes).FirstOrDefault(),
                            CancellationWFApprovalSubmıtDateTıme = invoiceCSVData.Select(x => x.CancellationWFApprovalSubmıtDateTıme).FirstOrDefault(),
                            ETLJobName = invoiceCSVData.Select(x => x.ETLJobName).FirstOrDefault(),
                            CbcPricingCurrencyCode = invoiceCSVData.Select(x => x.CbcPricingCurrencyCode).FirstOrDefault(),
                            CbcCurrencyExchangeRate = invoiceCSVData.Select(x => x.CbcCurrencyExchangeRate).FirstOrDefault(),
                            CbcFrequencyofBilling = invoiceCSVData.Select(x => x.CbcFrequencyofBilling).FirstOrDefault(),
                            CbcBillingPeriodStartDate = invoiceCSVData.Select(x => x.CbcBillingPeriodStartDate).FirstOrDefault(),
                            CbcBillingPeriodEndDate = invoiceCSVData.Select(x => x.CbcBillingPeriodEndDate).FirstOrDefault(),
                            PaymentMode = invoiceCSVData.Select(x => x.PaymentMode).FirstOrDefault(),
                            CbcSupplierBankAccountNumber = invoiceCSVData.Select(x => x.CbcSupplierBankAccountNumber).FirstOrDefault(),
                            CbcBillReferenceNumber = invoiceCSVData.Select(x => x.CbcBillReferenceNumber).FirstOrDefault(),
                            SourceCalculationMode = invoiceCSVData.Select(x => x.SourceCalculationMode).FirstOrDefault(),
                            EtlCalculationMode = invoiceCSVData.Select(x => x.EtlCalculationMode).FirstOrDefault(),
                            İnvoiceFactorycalcutionMode = invoiceCSVData.Select(x => x.İnvoiceFactorycalcutionMode).FirstOrDefault(),
                            CbcTaxRate = invoiceCSVData.Select(x => x.CbcTaxRate).FirstOrDefault(),
                            CbcTaxCategory = invoiceCSVData.Select(x => x.CbcTaxCategory).FirstOrDefault(),
                            ValidationLink = invoiceCSVData.Select(x => x.ValidationLink).FirstOrDefault(),
                            CustomsForm19ID = invoiceCSVData.Select(x => x.CustomsForm19ID).FirstOrDefault(),
                            CustomsForm19DocumentType = invoiceCSVData.Select(x => x.CustomsForm19DocumentType).FirstOrDefault(),
                            Incoterms = invoiceCSVData.Select(x => x.Incoterms).FirstOrDefault(),
                            FTADocumentType = invoiceCSVData.Select(x => x.FTADocumentType).FirstOrDefault(),
                            FTAID = invoiceCSVData.Select(x => x.FTAID).FirstOrDefault(),
                            FTADocumentDesc = invoiceCSVData.Select(x => x.FTADocumentDesc).FirstOrDefault(),
                            SchemeAgencyName = invoiceCSVData.Select(x => x.SchemeAgencyName).FirstOrDefault(),
                            CustomsForm2ID = invoiceCSVData.Select(x => x.CustomsForm2ID).FirstOrDefault(),
                            CustomsForm2DocumentType = invoiceCSVData.Select(x => x.CustomsForm2DocumentType).FirstOrDefault(),
                            OtherChargesID = invoiceCSVData.Select(x => x.OtherChargesID).FirstOrDefault(),
                            OtherChargesChargeIndicator = invoiceCSVData.Select(x => x.OtherChargesChargeIndicator).FirstOrDefault(),
                            OtherChargesAmount = invoiceCSVData.Select(x => x.OtherChargesAmount).FirstOrDefault(),
                            OtherChargesAllowanceChargeReason = invoiceCSVData.Select(x => x.OtherChargesAllowanceChargeReason).FirstOrDefault(),
                            NotificationTemplateId = invoiceCSVData.Select(x => x.NotificationTemplateId).FirstOrDefault(),
                            SMSTemplateId = invoiceCSVData.Select(x => x.SMSTemplateId).FirstOrDefault(),
                            OutputFormat = invoiceCSVData.Select(x => x.OutputFormat).FirstOrDefault(),
                            InputFormat = invoiceCSVData.Select(x => x.InputFormat).FirstOrDefault(),
                            Notifications = invoiceCSVData.Select(x => x.Notifications).FirstOrDefault(),
                            SourceDocumentDateTime = invoiceCSVData.Select(x => x.SourceDocumentDateTime).FirstOrDefault(),
                            SubmittedXml = invoiceCSVData.Select(x => x.SubmittedXml).FirstOrDefault(),
                            QRCodeImage = invoiceCSVData.Select(x => x.QRCodeImage).FirstOrDefault(),
                            InvoicePdf = invoiceCSVData.Select(x => x.InvoicePdf).FirstOrDefault(),
                            EmailInvoicePdf = invoiceCSVData.Select(x => x.EmailInvoicePdf).FirstOrDefault(),
                            OriginalInvoiceIRBMUniqueNo = invoiceCSVData.Select(x => x.OriginalInvoiceIRBMUniqueNo).FirstOrDefault(),
                            OriginalInvoiceNumber = invoiceCSVData.Select(x => x.OriginalInvoiceNumber).FirstOrDefault(),
                            ExportAuthorizationNumber = invoiceCSVData.Select(x => x.ExportAuthorizationNumber).FirstOrDefault(),
                            OutputFileName = invoiceCSVData.Select(x => x.OutputFileName).FirstOrDefault(),
                          
                            SellerContactPerson = invoiceCSVData.Select(x => x.SellerContactPerson).FirstOrDefault(),
                            //NetAmount = item.NetAmount,
                            NetAmount = invoiceCSVData.Select(x => x.NetAmount).FirstOrDefault(),
                            TotalInvoiceLines = invoiceCSVData.Count.ToString(),
                            TotalSourceLineItems = invoiceCSVData.Count.ToString(),
                            //TotalAmount = totalamount,
                            TotalSourceInvoiceAmount = totalInvoiceLineAmount.ToString(),
                        };
                        if(InvoiceCSVData.InvoiceVersion == null)
                        {
                            InvoiceCSVData.InvoiceVersion = "1.0";
                        }
                        if(InvoiceCSVData.EInvoiceDateTime != null)
                        {
                            InvoiceCSVData.InvoiceDate = InvoiceCSVData.EInvoiceDateTime.Value.Date;
    InvoiceCSVData.InvoiceTime = InvoiceCSVData.EInvoiceDateTime.Value;  
                        }
                        else
                        {
                            InvoiceCSVData.InvoiceDate = DateTime.Parse(invoiceCSVData.Select(x => x.InvoiceDate).FirstOrDefault()?.Date.ToString("yyyy-MM-dd"));
                            InvoiceCSVData.InvoiceTime = invoiceCSVData.Select(x => x.InvoiceTime).FirstOrDefault();
                        }

                            Console.WriteLine("Invoicedata");
                       // InvoiceCSVData.InvoiceDate = DateParser.TryParseDate(invoiceCSVData.Select(x => x.InvoiceDateTime).FirstOrDefault());
                        InvoiceCSVData.PaymentDueDate = DateParser.TryParseDate(invoiceCSVData.Select(x => x.PaymentDueDate).FirstOrDefault());
                        invtypecode = InvoiceCSVData.CbcInvoiceTypeCode;

                        if (invtypecode == "11" || invtypecode == "12" || invtypecode == "13" || invtypecode == "14")
                        {
                            InvoiceCSVData.CacPostalSellerAddress = invoiceCSVData.Select(x => x.CacPostalSellerAddress).FirstOrDefault();
                            InvoiceCSVData.CbcSellerCompanyID = invoiceCSVData.Select(x => x.CbcSellerCompanyID).FirstOrDefault();
                            InvoiceCSVData.CbcSellerVATID = invoiceCSVData.Select(x => x.CbcSellerVATID).FirstOrDefault();
                            InvoiceCSVData.CbcSellerElectronicMail = invoiceCSVData.Select(x => x.CbcSellerElectronicMail).FirstOrDefault();
                            InvoiceCSVData.CbcSellerName = invoiceCSVData.Select(x => x.CbcSellerName).FirstOrDefault();
                            InvoiceCSVData.CbcSellerRegnName = invoiceCSVData.Select(x => x.CbcSellerRegnName).FirstOrDefault();
                            InvoiceCSVData.CbcSellerTelephone = invoiceCSVData.Select(x => x.CbcSellerTelephone).FirstOrDefault();
                            InvoiceCSVData.CbcEndpointId = invoiceCSVData.Select(x => x.CbcEndpointId).FirstOrDefault();
                            InvoiceCSVData.CbcEndpointIdschemeID = invoiceCSVData.Select(x => x.CbcEndpointIdschemeID).FirstOrDefault();
                            InvoiceCSVData.CbcPartyTaxSchemeID = invoiceCSVData.Select(x => x.CbcPartyTaxSchemeID).FirstOrDefault();
                            InvoiceCSVData.CbcPartyTaxSchemeCompanyID = invoiceCSVData.Select(x => x.CbcPartyTaxSchemeCompanyID).FirstOrDefault();
                            InvoiceCSVData.CbcPartyLegalEntityCompanyID = invoiceCSVData.Select(x => x.CbcPartyLegalEntityCompanyID).FirstOrDefault();
                            InvoiceCSVData.CbcPartyLegalEntityCompanyLegalForm = invoiceCSVData.Select(x => x.CbcPartyLegalEntityCompanyLegalForm).FirstOrDefault();
                            InvoiceCSVData.SellerContactPerson = invoiceCSVData.Select(x => x.SellerContactPerson).FirstOrDefault();
                            InvoiceCSVData.CbcSStreetName = invoiceCSVData.Select(x => x.CbcSStreetName).FirstOrDefault();
                            InvoiceCSVData.CbcSAdditionalStreetName1 = invoiceCSVData.Select(x => x.CbcSAdditionalStreetName1).FirstOrDefault();
                            InvoiceCSVData.CbcSAdditionalStreetName2 = invoiceCSVData.Select(x => x.CbcSAdditionalStreetName2).FirstOrDefault();
                            InvoiceCSVData.CbcSPostalZone = invoiceCSVData.Select(x => x.CbcSPostalZone).FirstOrDefault();
                            InvoiceCSVData.CbcSCityName = invoiceCSVData.Select(x => x.CbcSCityName).FirstOrDefault();
                            InvoiceCSVData.CbcSCountrySubentity = invoiceCSVData.Select(x => x.CbcSCountrySubentity).FirstOrDefault();
                            InvoiceCSVData.CbcSCountryIdentificationCode = invoiceCSVData.Select(x => x.CbcSCountryIdentificationCode).FirstOrDefault();
                            InvoiceCSVData.CbcSCategory = invoiceCSVData.Select(x => x.CbcSCategory).FirstOrDefault();
                            InvoiceCSVData.CbcSellerSSTRegistrationNumber = invoiceCSVData.Select(x => x.CbcSellerSSTRegistrationNumber).FirstOrDefault();
                            InvoiceCSVData.CbcSBRNNumber = invoiceCSVData.Select(x => x.CbcSBRNNumber).FirstOrDefault();
                            InvoiceCSVData.CbcSSubCategory = invoiceCSVData.Select(x => x.CbcSSubCategory).FirstOrDefault();
                            InvoiceCSVData.CbcSellerTourismTaxRegistrationNumber = invoiceCSVData.Select(x => x.CbcSellerTourismTaxRegistrationNumber).FirstOrDefault();
                            InvoiceCSVData.CbcSellerRegnName = invoiceCSVData.Select(x => x.CbcSellerRegnName).FirstOrDefault();
                            InvoiceCSVData.CbcSellerCompanyID = invoiceCSVData.Select(x => x.CbcSellerCompanyID).FirstOrDefault();
                            InvoiceCSVData.CbcSNRIC = invoiceCSVData.Select(x => x.CbcSNRIC).FirstOrDefault();
                        }
                        else
                        {
                            InvoiceCSVData.CacPostalBuyerAddress = invoiceCSVData.Select(x => x.CacPostalBuyerAddress).FirstOrDefault();
                            InvoiceCSVData.CbcBuyerReference = invoiceCSVData.Select(x => x.CbcBuyerReference).FirstOrDefault();
                            InvoiceCSVData.CbcBuyerCompanyID = invoiceCSVData.Select(x => x.CbcBuyerCompanyID).FirstOrDefault();
                            InvoiceCSVData.CbcBuyerVATID = invoiceCSVData.Select(x => x.CbcBuyerVATID).FirstOrDefault();
                            InvoiceCSVData.CbcBuyerElectronicMail = invoiceCSVData.Select(x => x.CbcBuyerElectronicMail).FirstOrDefault();
                            InvoiceCSVData.CbcBuyerName = invoiceCSVData.Select(x => x.CbcBuyerName).FirstOrDefault();
                            InvoiceCSVData.CbcBuyerRegnName = invoiceCSVData.Select(x => x.CbcBuyerRegnName).FirstOrDefault();
                            InvoiceCSVData.CbcBuyerTelephone = invoiceCSVData.Select(x => x.CbcBuyerTelephone).FirstOrDefault();
                            InvoiceCSVData.BuyerSentDate = invoiceCSVData.Select(x => x.BuyerSentDate).FirstOrDefault();
                            InvoiceCSVData.CbcBuyerEndpointId = invoiceCSVData.Select(x => x.CbcBuyerEndpointId).FirstOrDefault();
                            InvoiceCSVData.CbcBuyerEndpointIdschemeID = invoiceCSVData.Select(x => x.CbcBuyerEndpointIdschemeID).FirstOrDefault();
                            InvoiceCSVData.CbcBuyerPartyTaxSchemeID = invoiceCSVData.Select(x => x.CbcBuyerPartyTaxSchemeID).FirstOrDefault();
                            InvoiceCSVData.CbcBuyerPartyTaxSchemeCompanyID = invoiceCSVData.Select(x => x.CbcBuyerPartyTaxSchemeCompanyID).FirstOrDefault();
                            InvoiceCSVData.CbcBuyerPartyLegalEntityCompanyID = invoiceCSVData.Select(x => x.CbcBuyerPartyLegalEntityCompanyID).FirstOrDefault();
                            InvoiceCSVData.CbcBuyerPartyLegalEntityCompanyLegalForm = invoiceCSVData.Select(x => x.CbcBuyerPartyLegalEntityCompanyLegalForm).FirstOrDefault();
                            InvoiceCSVData.BuyerContactPerson = invoiceCSVData.Select(x => x.BuyerContactPerson).FirstOrDefault();
                            InvoiceCSVData.CbcBCategory = invoiceCSVData.Select(x => x.CbcBCategory).FirstOrDefault();
                            InvoiceCSVData.CbcBSubCategory = invoiceCSVData.Select(x => x.CbcBSubCategory).FirstOrDefault();
                            InvoiceCSVData.CbcBBRNNumber = invoiceCSVData.Select(x => x.CbcBBRNNumber).FirstOrDefault();
                            InvoiceCSVData.CbcBNRIC = invoiceCSVData.Select(x => x.CbcBNRIC).FirstOrDefault();
                            InvoiceCSVData.CbcBStreetName = invoiceCSVData.Select(x => x.CbcBStreetName).FirstOrDefault();
                            InvoiceCSVData.CbcBAdditionalStreetName1 = invoiceCSVData.Select(x => x.CbcBAdditionalStreetName1).FirstOrDefault();
                            InvoiceCSVData.CbcBAdditionalStreetName2 = invoiceCSVData.Select(x => x.CbcBAdditionalStreetName2).FirstOrDefault();
                            InvoiceCSVData.CbcBPostalZone = invoiceCSVData.Select(x => x.CbcBPostalZone).FirstOrDefault();
                            InvoiceCSVData.CbcBCityName = invoiceCSVData.Select(x => x.CbcBCityName).FirstOrDefault();
                            InvoiceCSVData.CbcBCountrySubentity = invoiceCSVData.Select(x => x.CbcBCountrySubentity).FirstOrDefault();
                            InvoiceCSVData.CbcBCountryIdentificationCode = invoiceCSVData.Select(x => x.CbcBCountryIdentificationCode).FirstOrDefault();
                            InvoiceCSVData.CbcBSSTRegistrationNumber = invoiceCSVData.Select(x => x.CbcBSSTRegistrationNumber).FirstOrDefault();
                        }
                        //Create Json
                        if (invtypecode == "01")
                        {
                            invtypecode = "Invoice";
                        }
                        else if(invtypecode == "02")
                        {
                            invtypecode = "CreditNote";
                        }
                        else if(invtypecode == "03")
                        {
                            invtypecode = "DebitNote";
                        } 
                        else if(invtypecode == "11")
                        {
                            invtypecode = "SBInvoice";
                        }
                        else if(invtypecode == "12")
                        {
                            invtypecode = "SBCreditNote";
                        }
                        else if(invtypecode == "13")
                        {
                            invtypecode = "SBDebitNote";
                        }
                            invoicedatajson = JsonConvert.SerializeObject(InvoiceCSVData);

                        finalinvoice = JsonConvert.SerializeObject(invoicedata);
                        InvoiceData invoicedata1 = new InvoiceData();
                        /* if (item.CbcInvoiceTypeCode == "01")
                         {
                            // invoicedata1 = await _etlDemoService.InsertInvoiceData(InvoiceCSVData);
                         }
                         else if (item.CbcInvoiceTypeCode == "02")
                         {
                             invoicedata1 = await _etlDemoService.InsertCreditNoteData(InvoiceCSVData);
                         }
                         else if (item.CbcInvoiceTypeCode == "03")
                         {
                             invoicedata1 = await _etlDemoService.InsertDebitNoteData(InvoiceCSVData);
                         }
                         else if (item.CbcInvoiceTypeCode == "04")
                         {
                             invoicedata1 = await _etlDemoService.InsertRefundNoteData(InvoiceCSVData);
                         }
                         else if (item.CbcInvoiceTypeCode == "11")
                         {
                             invoicedata1 = await _etlDemoService.InsertSBInvoiceData(InvoiceCSVData);
                         }
                         else if (item.CbcInvoiceTypeCode == "12")
                         {
                             invoicedata1 = await _etlDemoService.InsertSBCreditNoteData(InvoiceCSVData);
                         }
                         else if (item.CbcInvoiceTypeCode == "13")
                         {
                             invoicedata1 = await _etlDemoService.InsertSBDebitNoteData(InvoiceCSVData);
                         }
                         else if (item.CbcInvoiceTypeCode == "14")
                         {
                             invoicedata1 = await _etlDemoService.InsertSBRefundNoteData(InvoiceCSVData);
                         }*/
                        #endregion InvoiceData

                        if (invoicedata1 != null)
                        {
                            #region InvoiceLineItems
                            var invoicelineitemdata = invoiceCSVData.ToList()/*.Where(x => x.EInvoiceNumber == InvoiceCSVData.EInvoiceNumber).ToList()*/;

                            if (invoicelineitemdata.Count() > 0)
                            {
                                int lineId = 0;
                                //List<string> taxtype = new List<string> { "01", "02", "03", "04", "05", "06", "E" };
                                Console.WriteLine("Invoice Line Items");
                                //invoiceCSVData.ForEach(x =>
                                //{
                                //    if (!string.IsNullOrEmpty(x.CbcTaxType) && x.CbcTaxType.Length < 2)
                                //    {
                                //        x.CbcTaxType = x.CbcTaxType.PadLeft(2, '0');
                                //    }
                                //});
                                //if (!invoiceCSVData.Any(x => taxtype.Contains(x.CbcTaxType)))
                                //{
                                //    invoiceCSVData.ForEach(x => x.CbcTaxType = null);
                                //    invoiceCSVData.ForEach(x => x.CbcTaxSchemeAgencyCode = null);
                                //    invoiceCSVData.ForEach(x => x.CbcTaxSchemeID = null);
                                //}
                                //else
                                //{
                                //    invoiceCSVData.ForEach(x => x.CbcTaxType = "6");
                                //    invoiceCSVData.ForEach(x => x.CbcTaxSchemeAgencyCode = "OTH");
                                //    invoiceCSVData.ForEach(x => x.CbcTaxSchemeID = "UN/ECE 5153");
                                //}
                                var invoicelineitem12 = invoicelineitemdata.Select(itemline => new InvoiceLineItems
                                {
                                    InvoiceId = invoicedata1.Id,
                                    CreditNoteId = invoicedata1.Id,
                                    DebitNoteId = invoicedata1.Id,
                                    RefundNoteId = invoicedata1.Id,
                                    SBInvoiceId = invoicedata1.Id,
                                    SBCreditNoteId = invoicedata1.Id,
                                    SBDebitNoteId = invoicedata1.Id,
                                    SBRefundNoteId = invoicedata1.Id,
                                    CbcIDVATCategoryCode = itemline.CbcIDVATcategoryCode,
                                    CbcIDItemCountryOfOrigin = itemline.CbcIDItemCountryOfOrigin,
                                    CbcDescription = itemline.CbcDescription,
                                    CbcDescriptionCode = itemline.CbcDescriptionCode,
                                    CbcBaseAmount = itemline.CbcBaseAmount,
                                    CbcAmount = itemline.CbcAmount,
                                    CreatedBy = itemline.CreatedBy,
                                    CreatedDate = itemline.CreatedDate,
                                    UpdatedBy = itemline.UpdatedBy,
                                    UpdatedDate = itemline.UpdatedDate,
                                    LineId = itemline.LineId    ,
                                    CbcDiscountRate = itemline.CbcDiscountRate,
                                    CbcDiscountAmount = itemline.CbcDiscountAmount,
                                    CbcTaxType = itemline.CbcTaxType,
                                    CbcTaxRate = itemline.CbcTaxRate,
                                    CbcTaxAmount = itemline.CbcTaxAmount,
                                    CbcMeasure = itemline.CbcMeasure,
                                    CbcAllowanceType = itemline.CbcAllowanceType,
                                    CbcAllowanceReasonCode = itemline.CbcAllowanceReasonCode,
                                    CbcAllowanceText = itemline.CbcAllowanceText,
                                    CbcAllowanceBaseAmount = itemline.CbcAllowanceBaseAmount,
                                    CbcAllowanceMultiplierFactor = itemline.CbcAllowanceMultiplierFactor,
                                    CbcAllowanceAmount = itemline.CbcAllowanceAmount,
                                    CbcChargeType = itemline.CbcChargeType,
                                    CbcChargeReasonCode = itemline.CbcChargeReasonCode,
                                    CbcChargeText = itemline.CbcChargeText,
                                    CbcChargeBaseAmount = itemline.CbcChargeBaseAmount,
                                    CbcChargeMultiplierFactor = itemline.CbcChargeMultiplierFactor,
                                    CbcChargeAmount = itemline.CbcChargeAmount,
                                    CbcPrice = itemline.UnitPrice,
                                    CbcTaxExemptionDetails = itemline.CbcTaxExemptionDetails,
                                    CbcTaxExemptedAmount = itemline.CbcTaxExemptedAmount,
                                    CbcTotalExcludingTax = itemline.CbcTotalExcludingTax,
                                    CbcItemClassificationCode = itemline.CbcItemClassificationCode,
                                    CbcProductTariffClass = itemline.CbcProductTariffClass,
                                   // CbcTaxSchemeID = invoiceCSVData.Select(x => x.CbcTaxSchemeID).FirstOrDefault(),
                                    CbcItemTaxCategory = invoiceCSVData.Select(x => x.CbcItemTaxCategory).FirstOrDefault(),
                                    //CbcTaxSchemeAgencyID = invoiceCSVData.Select(x => x.CbcTaxSchemeAgencyID).FirstOrDefault(),
                                    CbcItemTaxSchemeAgencyID = invoiceCSVData.Select(x => x.CbcItemTaxSchemeAgencyID).FirstOrDefault(),
                                    CbcItemTaxSchemeAgencyCode = invoiceCSVData.Select(x => x.CbcItemTaxSchemeAgencyCode).FirstOrDefault(),
                                    CbcInvoiceLineNetAmount = itemline.CbcInvoiceLineNetAmount,
                                    CbcNetAmount = itemline.CbcNetAmount,
                                    ProductId = itemline.ProductId,
                                    CbcItemClassificationClass = itemline.CbcItemClassificationClass,
                                    cbcItemClassificationCodeClass = itemline.CbcItemClassificationCodeClass,
                                    cbcItemClassificationCodePTC = itemline.CbcItemClassificationCodePTC,
                                    CbcProductTariffCode = itemline.CbcProductTariffCode,
                                    CbcSubtotal = itemline.CbcSubtotal,
                                    CbcSSTTaxCategory = itemline.CbcSSTTaxCategory,
                                    CbcBaseQuantity = itemline.CbcBaseQuantity,                                  
                                }).ToList();

                                /* foreach (var itemline in invoicelineitemdata)
                                     {
                                    /* Console.WriteLine("Invoice Line Items");
                                     if (!taxtype.Contains(item.CbcTaxType))
                                         {
                                             item.CbcTaxSchemeAgencyId = null;
                                             item.CbcTaxSchemeAgencyCode = null;
                                             item.CbcTaxSchemeID = null;
                                         }
                                         else
                                         {
                                             item.CbcTaxSchemeAgencyId = "6";
                                             item.CbcTaxSchemeAgencyCode = "OTH";
                                             item.CbcTaxSchemeID = "UN/ECE 5153";
                                         }
                                         lineItems = new InvoiceLineItems()
                                         {
                                             InvoiceId = invoicedata1.Id,
                                             CreditNoteId = invoicedata1.Id,
                                             DebitNoteId = invoicedata1.Id,
                                             RefundNoteId = invoicedata1.Id,
                                             SBInvoiceId = invoicedata1.Id,
                                             SBCreditNoteId = invoicedata1.Id,
                                             SBDebitNoteId = invoicedata1.Id,
                                             SBRefundNoteId = invoicedata1.Id,
                                             CbcIDVATCategoryCode = itemline.CbcIDVATcategoryCode,
                                             CbcIDItemCountryOfOrigin = itemline.CbcIDItemCountryOfOrigin,
                                             CbcDescription = itemline.CbcDescription,
                                             CbcDescriptionCode = itemline.CbcDescriptionCode,
                                             CbcBaseAmount = itemline.CbcBaseAmount,
                                             CbcAmount = itemline.CbcAmount,
                                             CreatedBy = itemline.CreatedBy,
                                             CreatedDate = itemline.CreatedDate,
                                             UpdatedBy = itemline.UpdatedBy,
                                             UpdatedDate = itemline.UpdatedDate,
                                             LineId = ++lineId,
                                             CbcDiscountRate = itemline.CbcDiscountRate,
                                             CbcDiscountAmount = itemline.CbcDiscountAmount,
                                             CbcTaxType = itemline.CbcTaxType,
                                             CbcTaxRate = itemline.CbcTaxRate,
                                             CbcTaxAmount = itemline.CbcTaxAmount,
                                             CbcMeasure = itemline.CbcMeasure,
                                             CbcAllowanceType = itemline.CbcAllowanceType,
                                             CbcAllowanceReasonCode = itemline.CbcAllowanceReasonCode,
                                             CbcAllowanceText = itemline.CbcAllowanceText,
                                             CbcAllowanceBaseAmount = itemline.CbcAllowanceBaseAmount,
                                             CbcAllowanceMultiplierFactor = itemline.CbcAllowanceMultiplierFactor,
                                             CbcAllowanceAmount = itemline.CbcAllowanceAmount,
                                             CbcChargeType = itemline.CbcChargeType,
                                             CbcChargeReasonCode = itemline.CbcChargeReasonCode,
                                             CbcChargeText = itemline.CbcChargeText,
                                             CbcChargeBaseAmount = itemline.CbcChargeBaseAmount,
                                             CbcChargeMultiplierFactor = itemline.CbcChargeMultiplierFactor,
                                             CbcChargeAmount = itemline.CbcChargeAmount,
                                             CbcPrice = itemline.CbcPrice,
                                             CbcTaxExemptionDetails = itemline.CbcTaxExemptionDetails,
                                             CbcTaxExemptedAmount = itemline.CbcTaxExemptedAmount,
                                             CbcTotalExcludingTax = itemline.CbcTotalExcludingTax,
                                             CbcItemClassificationCode = itemline.CbcItemClassificationCode,
                                             CbcProductTariffClass = itemline.CbcProductTariffClass,
                                             CbcTaxSchemeID = item.CbcTaxSchemeID,
                                             CbcTaxSchemeAgencyID = item.CbcTaxSchemeAgencyID,
                                             CbcTaxSchemeAgencyCode = item.CbcTaxSchemeAgencyCode,
                                             CbcInvoiceLineNetAmount = itemline.CbcInvoiceLineNetAmount,
                                             CbcNetAmount = itemline.CbcNetAmount,
                                             ProductId = itemline.ProductId,
                                             CbcItemClassificationClass = itemline.CbcItemClassificationClass,
                                             cbcItemClassificationCodeClass = itemline.CbcItemClassificationCodeClass,
                                             cbcItemClassificationCodePTC = itemline.CbcItemClassificationCodePTC,
                                             CbcProductTariffCode = itemline.CbcProductTariffCode,
                                             CbcSubtotal = itemline.CbcSubtotal,
                                             CbcSSTTaxCategory = itemline.CbcSSTTaxCategory,
                                             CbcBaseQuantity = itemline.CbcBaseQuantity,
                                         };
                                         string serializedLineItems = JsonConvert.SerializeObject(lineItems);
                                         invoicelineitemsjson.Add(lineItems);*/

                                InvoiceCSVData.InvoiceLineItems = invoicelineitem12;


                                // If you want to convert the entire list to a JSON array at the end
                                finalJsonArray = JsonConvert.SerializeObject(invoicelineitemsjson);

                                InvoiceLineItems invoicelineitemdata1 = new InvoiceLineItems();

                            }
                            else
                            {
                                Console.WriteLine("Invoice lineitem details not found please check and try again.");
                                Environment.Exit(0);
                            }

                        }
                        //invoicelineitems.Add(invoicelineitemsjson);

                        finallineitems = JsonConvert.SerializeObject(invoicelineitems);

                        invoicelineitemsjson = new List<InvoiceLineItems>();
                        #endregion InvoiceLineItems

                        #region DocTaxSubTotal
                        Console.WriteLine("DocTaxSubTotal");
                        var docTaxSubTotal1 = new DocTaxSubTotal()
                        {

                            InvoiceId = invoicedata1.Id,
                            CreditNoteId = invoicedata1.Id,
                            DebitNoteId = invoicedata1.Id,
                            RefundNoteId = invoicedata1.Id,
                            SBInvoiceId = invoicedata1.Id,
                            SBCreditNoteId = invoicedata1.Id,
                            SBDebitNoteId = invoicedata1.Id,
                            SBRefundNoteId = invoicedata1.Id,
                            TaxAmount = invoiceCSVData.Select(x => x.TaxAmount).FirstOrDefault(),
                            CategoryTotalLines = invoiceCSVData.Select(x => x.CategoryTotalLines).FirstOrDefault(),
                            CategoryTaxCategory = invoiceCSVData.Select(x => x.CategoryTaxCategory).FirstOrDefault(),
                            TaxCatCodeForTaxAmount = invoiceCSVData.Select(x => x.TaxCatCodeForTaxAmount).FirstOrDefault(),
                            CategoryTaxableAmount = invoiceCSVData.Select(x => x.CategoryTaxableAmount).FirstOrDefault(),
                            CategoryTaxAmount = invoiceCSVData.Select(x => x.CategoryTaxAmount).FirstOrDefault(),
                            TaxAmountPerTaxType = invoiceCSVData.Select(x => x.TaxAmountPerTaxType).FirstOrDefault(),
                            CategoryTaxRate = invoiceCSVData.Select(x => x.CategoryTaxRate).FirstOrDefault(),
                            CategoryTaxExemptionReason = invoiceCSVData.Select(x => x.CategoryTaxExemptionReason).FirstOrDefault(),
                            //InvoiceLineItemId = invoicelineitemdata1.Id,
                            //CreditNoteLineItemId=invoicelineitemdata1.Id,
                            //DebitNoteLineItemId=invoicelineitemdata1.Id,
                            //RefundNoteLineItemId=invoicelineitemdata1.Id, 
                            //SBInvoiceLineItemId=invoicelineitemdata1.Id, 
                            //SBCreditNoteLineItemId=invoicelineitemdata1.Id, 
                            //SBDebitNoteLineItemId=invoicelineitemdata1.Id, 
                            //SBRefundNoteLineItemId=invoicelineitemdata1.Id, 
                            CategoryTaxSchemeId = invoiceCSVData.Select(x => x.CategoryTaxSchemeId).FirstOrDefault(),
                            AmountExemptedFromTax = invoiceCSVData.Select(x => x.AmountExemptedFromTax).FirstOrDefault(),
                            CbcTaxSchemeAgencyId = invoiceCSVData.Select(x => x.CbcTaxSchemeAgencyID).FirstOrDefault(),
                            CbcTaxSchemeAgencyCode = invoiceCSVData.Select(x => x.CbcTaxSchemeAgencyCode).FirstOrDefault(),
                            CbcTaxSchemeID = invoiceCSVData.Select(x => x.CbcTaxSchemeID).FirstOrDefault(),
                        };
                        doctaxsubtotaljson = JsonConvert.SerializeObject(docTaxSubTotal1);
                        doctaxsubtotal.Add(docTaxSubTotal1);
                        finaldoctax = JsonConvert.SerializeObject(doctaxsubtotal);
                        InvoiceCSVData.DocTaxSubTotal = docTaxSubTotal1;
                        invoicedata.Add(InvoiceCSVData);
                        DocTaxSubTotal docTaxSubTotal = new DocTaxSubTotal();

                        #endregion DocTaxSubTotal

                        #region PDF Fields
                        string invmethod = $"{invoicetype}InvLMapping";
                        string invlinemethod = $"{invoicetype}LineMapping";
                    
                        var invfields = await InvokeDynamicMethodAsync(pDFMappingService, invmethod, invoiceCSVData);
                        var invlinefields = await InvokeDynamicMethodAsync(pDFMappingService, invlinemethod, invoiceCSVData);
            
                        #endregion PDF Fields

                        file = Path.Combine(_appSettings.ProcessedFolderPath, domainname, invoicetype, "Input", "DataError", $"{extractedInvoiceNumber}", $"{filename}_{DateTime.Now:yyyyMMdd_HHmmss}" + ".csv");
                        var directory = Path.GetDirectoryName(file);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        if (csvRecords.Count - 1 > _appSettings.LineSplitCount)
                        {
                            var totalamount1 = invoiceCSVData.Select(x => Convert.ToDecimal(x.CbcInvoiceLineNetAmount)).Sum().ToString();

                            var einvnum = invoiceCSVData.Select(x => x.EInvoiceNumber).FirstOrDefault();
                            var totallines = invoiceCSVData.Count.ToString();

                            int retryCount = 0;
                            bool success = false;

                            while (!success)
                            {
                                try
                                {
                                    await _etlDemoService.TempInsertStoreProc(invoicedata, invoicetype, invfields, invlinefields,invtypecode);
                                    await _etlDemoService.TempInsertStoreProc2(einvnum, totalamount1, totallines, invoicetype, invtypecode);
                                   await _etlDemoService.InsertInvData(invoiceCSVData.Select(x => x.EInvoiceNumber).FirstOrDefault().ToString(), filepath, invoicetype, invtypecode);

                                    success = true; // If all operations succeed, exit the loop
                                }
                                catch (Exception ex)
                                {
                                    retryCount++;
                                    Console.WriteLine($"Connection failed. Retrying in 5 seconds... Attempt {retryCount}");
                                    Log.Error($"Connection failed. Retrying in 5 seconds... Attempt {retryCount}");
                                    Console.WriteLine($"Exception: {ex.Message}");
                                    Log.Error($"Exception: {ex.Message}");
                                    await Task.Delay(5000); // Wait for 5 seconds before retrying
                                }
                            }


                            //int retryCount = 0;
                            //bool success = false;
                            //while (!success && retryCount < 2) // Allow one retry
                            //{
                            //    try
                            //    {
                            //        await _etlDemoService.TempInsertStoreProc(invoicedata);
                            //        success = true; // If execution succeeds, exit loop
                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        retryCount++;
                            //        await Task.Delay(5000);  // Wait for 5 second before retrying
                            //        if (retryCount >= 2)
                            //        {
                            //            throw; // Rethrow the exception after max retries
                            //        }
                            //    }



                            //    retryCount = 0;
                            //    success = false;
                            //    while (!success && retryCount < 2) // Allow one retry
                            //    {
                            //        try
                            //        {
                            //            await _etlDemoService.TempInsertStoreProc2(einvnum, totalamount1, totallines);
                            //            success = true; // If execution succeeds, exit loop
                            //        }
                            //        catch (Exception ex)
                            //        {
                            //            retryCount++;
                            //            await Task.Delay(5000);  // Wait for 5 second before retrying
                            //            if (retryCount >= 2)
                            //            {
                            //                throw; // Rethrow the exception after max retries
                            //            }
                            //        }
                            //    }
                            //}


                            // await _etlDemoService.InsertInvData(invoiceCSVData.Select(x => x.EInvoiceNumber).FirstOrDefault().ToString(), filepath);
                            //await _etlDemoService.InsertStoreProc(res, filepath);
                            //domainFiles.AddRange(res);
                        }
                        else
                        {
                            int retryCount = 0;
                            bool success = false;

                            while (!success)
                            {
                                try
                                {
                                    var totalamount1 = invoiceCSVData.Select(x => Convert.ToDecimal(x.CbcInvoiceLineNetAmount)).Sum().ToString();
                                    var totallines = invoiceCSVData.Count.ToString();

                                    await _etlDemoService.TempInsertStoreProc(invoicedata, invoicetype, invfields,invlinefields, invtypecode);
                                   await _etlDemoService.InsertInvoiceData(invoiceCSVData.Select(x => x.EInvoiceNumber).FirstOrDefault().ToString(), totalamount1, totallines, filepath, invoicetype, invtypecode);
                                    success = true; // If all operations succeed, exit the loop
                                }
                                catch (Exception ex)
                                {
                                    retryCount++;
                                    Console.WriteLine($"Connection failed. Retrying in 5 seconds... Attempt {retryCount}");
                                    Log.Error($"Connection failed. Retrying in 5 seconds... Attempt {retryCount}");
                                    Console.WriteLine($"Exception: {ex.Message}");
                                    Log.Error($"Exception: {ex.Message}");
                                    await Task.Delay(5000); // Wait for 5 seconds before retrying
                                }
                            }
                        }
                    }

                    else if (invoiceCSVData.Select(x => x.EInvoiceNumber).FirstOrDefault() != extractedInvoiceNumber && !processedInvoiceNumbers.Contains(invoiceCSVData.Select(x => x.EInvoiceNumber).FirstOrDefault()))
                    {
                        Console.WriteLine($"Invoicenumber :-{invoiceCSVData.Select(x => x.CbcInvoiceTypeCode).FirstOrDefault()}");
                        Log.Information("Invoice Number Not Match");
                        Console.WriteLine("Invoice Number Not Match");
                        var fileName1 = Path.GetFileName(tasktype);
                        message = "Invoice Number Not Match";
                        await WriteValidationResults(invoiceCSVData.Select(x => x.EInvoiceNumber).FirstOrDefault(), domainname, invoicetype, fileName1, extractedInvoiceNumber, message);
                        processedInvoiceNumbers.Add(invoiceCSVData.Select(x => x.EInvoiceNumber).FirstOrDefault());
                        count++;
                    }
                    else if (invoiceCSVData.Select(x => x.CbcInvoiceTypeCode).FirstOrDefault() != extractInvoiceTypeCode && !processedInvoiceNumbers.Contains(invoiceCSVData.Select(x => x.EInvoiceNumber).FirstOrDefault()))
                    {
                        Log.Information("Invoice Typecode Not Match");
                        Console.WriteLine("Invoice Typecode Not Match");
                        message = "Invoice Typecode Not Match";
                        var fileName1 = Path.GetFileName(tasktype);
                        await WriteValidationResults(invoiceCSVData.Select(x => x.EInvoiceNumber).FirstOrDefault(), domainname, invoicetype, fileName1, extractedInvoiceNumber, message);
                        processedInvoiceNumbers.Add(invoiceCSVData.Select(x => x.EInvoiceNumber).FirstOrDefault());
                        count++;
                    }


                }
                Console.WriteLine($"Process file path :- {tasktype}");
                Log.Information($"Process file path :- {tasktype}");
                var fileName = Path.GetFileName(tasktype);
                Log.Information($"Process file name :- {fileName}");
                Console.WriteLine($"Process file name :- {fileName}");
                despath = Path.Combine(_appSettings.ProcessedFolderPath, domainname, invoicetype, "Input", "Processed");
                if (!Directory.Exists(despath))
                {
                    Directory.CreateDirectory(despath);
                }
                //var destinationPath = Path.Combine(task, fileName);
                files.Add(tasktype);

                file = file.Replace(" ", "_");
                filepath.Add(file);



                //}
                stopwatch.Stop();
                Log.Information($"Total time taken in process: {stopwatch.Elapsed.TotalSeconds} seconds");
                Console.WriteLine($"Total time taken in process: {stopwatch.Elapsed.TotalSeconds} seconds");

                

                finalinvoicedata.Add(invoicedata);


                var stopwatch1 = Stopwatch.StartNew();
                // Process the files in parallel (moving them to the destination folder)
                var moveTasks = files.Select(async sourceFile =>
                {
                    try
                    {
                        string fileName = Path.GetFileNameWithoutExtension(sourceFile).Replace("WIP", "Processed");
                        string destinationFile = Path.Combine(despath, $"{fileName}{Path.GetExtension(sourceFile)}");

                        if (File.Exists(destinationFile))
                        {
                            File.Delete(destinationFile); 
                        }

                        if (File.Exists(sourceFile))
                        {
                            File.Move(sourceFile, destinationFile);
                            Console.WriteLine($"Successfully moved: {sourceFile} to {destinationFile}");
                        }
                        else
                        {
                            Console.WriteLine($"File not found: {sourceFile}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error moving file {sourceFile}: {ex.Message}");
                    }
                });

                // Wait for all file processing tasks to complete
                await Task.WhenAll(moveTasks);
                stopwatch1.Stop();
                Console.WriteLine($"Total time taken in moving: {stopwatch1.Elapsed.TotalSeconds} seconds");
                Log.Information($"Total time taken in moving: {stopwatch1.Elapsed.TotalSeconds} seconds");
            }
            catch (Exception ex)
            {
                // Handle general exceptions
                LogThreadSafe($"Exception in ProcessTaskAsync : {ex.Message}");
                Console.WriteLine($"Exception in ProcessTaskAsync : {ex.Message}");
                throw;
            }

        }

        public async Task<object?> InvokeDynamicMethodAsync(object service, string methodName, object parameter)
        {
            var methodInfo = service.GetType().GetMethod(methodName);

            if (methodInfo != null)
            {
                // Invoke the method dynamically
                var task = (Task)methodInfo.Invoke(service, new object[] { parameter });

                // Await the task result
                await task.ConfigureAwait(false);

                // Extract the result if the method returns a value
                var resultProperty = task.GetType().GetProperty("Result");
                return resultProperty?.GetValue(task);
            }

            Console.WriteLine($"Method {methodName} not found in {service.GetType().Name}.");
            return null;
        }


        private async Task<bool> EnsureDatabaseConnectionAsync()
        {
            var connectionString = _connectionString;

            using (var connection = new NpgsqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Database connection established successfully.");
                    return true;
                }
                catch (NpgsqlException ex)
                {                  
                    return false;
                }
            }
        }



        private string ExtractInvoiceNumberFromFileName(string filename)
        {
            var parts = filename.Split('_');
            if (parts.Length > 1)
            {
                // Extract the part that contains the invoice number, in your case the 4th part of the file name
                var invoiceNumber = parts[4]; // Adjust this if needed based on your file naming pattern
                return invoiceNumber;
            }
            else
            {
                return string.Empty; // Return empty if the pattern doesn't match
            }
        } private string ExtractInvoiceTypeFromFileName(string filename)
        {
            var parts = filename.Split('_');
            if (parts.Length > 1)
            {
                // Extract the part that contains the invoice number, in your case the 4th part of the file name
                var invoicetype = parts[1];// Adjust this if needed based on your file naming pattern
                return invoicetype;
            }
            else
            {
                return string.Empty; // Return empty if the pattern doesn't match
            }
        }


        public async Task WriteValidationResults(string invoicenumber,string domainname,string invoicetype,string fileName, string extractedInvoiceNumber,string message)
        {
            var despath = Path.Combine(_appSettings.ProcessedFolderPath, domainname, invoicetype, "Input", "DataError", $"{extractedInvoiceNumber}");

            if (!Directory.Exists(despath))
            {
                Directory.CreateDirectory(despath);
            }

            // Create a unique file name based on the InvoiceNumber or another unique field
            var filePath = Path.Combine(despath, $"{fileName}_{DateTime.Now:yyyyMMdd_HHmmss}" + ".csv"); // You can use more unique attributes if needed

            // Check if the file already exists
            var fileExists = File.Exists(filePath);
            var headers = new List<string>
    {
        "EInvoiceNumber",
        "Error"
    };

            using (var writer = new StreamWriter(filePath, append: true))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                // If the file doesn't exist, write the header (this is done once)
                if (!fileExists)
                {
                    foreach (var header in headers)
                    {
                        writer.Write(header + ",");
                    }
                    writer.WriteLine(); // Ensure a newline after the header
                }

                if (invoicenumber != null)
                {
                    var record = new List<string>() {
                invoicenumber,
                message
            };

                    // Write the actual validation result to the file
                    await writer.WriteLineAsync(string.Join(",", record));
                }

                await writer.FlushAsync(); // Ensure the buffer is flushed
            }
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

    } }

    public class DateParser
    {
        private static readonly string[] DateFormats = new[]
        {
        "yyyy-dd-MM",   // Year-Day-Month
        "dd-MM-yyyy",   // Day-Month-Year            
        "MM-dd-yyyy",   // Month-Day-Year
        "dd/MM/yyyy",   // Alternative separators
        "yyyy/dd/MM",   // Alternative formats
        "MM/dd/yyyy",
        "dd.MM.yyyy",   // Dot separator
        "yyyy.dd.MM",
        "MM.dd.yyyy"
    };

        public static DateTime? TryParseDate(string dateStr)
        {
            DateTime result;

            // If the input string is null or empty, return null
            if (string.IsNullOrWhiteSpace(dateStr))
            {
                return null;
            }

            // Try each format to parse the date
            foreach (var format in DateFormats)
            {
                if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                {
                    return result; // Return the successfully parsed date
                }
            }

            // If none of the formats matched, return null
            return null;
        }
    }
