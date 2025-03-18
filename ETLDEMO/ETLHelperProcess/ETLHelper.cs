using CsvHelper.Configuration;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETL_Demo.Models;
using ETL.Service.Model;
using ETL.Service.Repo.MSSQL;
using ETL.Service.Repo.Interface;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Common.Config;
using FastReport.Barcode;
using Serilog;
using Microsoft.IdentityModel.Tokens;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using ETL.Service.Model;
using System.Text.RegularExpressions;
using CsvHelper.TypeConversion;
using System.ComponentModel.DataAnnotations;
using DocumentFormat.OpenXml.Office2010.Excel;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.VisualBasic.FileIO;
using GroupDocs.Viewer.Results;
using static ClosedXML.Excel.XLPredefinedFormat;
using DateTime = System.DateTime;

namespace ETLDEMO.ETLHelperProcess
{
    public class ETLHelper
    {
        private readonly IETLService _etlDemoService;
        private readonly ETLAppSettings _appSettings;
        public ETLHelper(IETLService eTLDemoService, IOptions<ETLAppSettings> appsettings)
        {
            _etlDemoService = eTLDemoService;
            _appSettings = appsettings?.Value ?? throw new ArgumentNullException(nameof(appsettings));
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        { 

        }
    
        public async Task<List<T>> ReadCsv<T>(string filePath, ClassMap classMap)
        {
            try
            {         
                var records = new List<T>();
                int rowNumber = 1; // Start from row 1
                string[] headers = null; // Store headers for reference

                        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            BadDataFound = args =>
                            {
                                string error = null;
                                string header = null;

                                // Get the raw record (the entire row as a string)
                                string rawRecord = args.RawRecord;
                                var columns = rawRecord.Split(',');                           
                                rowNumber++;
                                // Increment the row number after each record
                            },
                            MissingFieldFound = null,
                        };
       
                        using (var reader = new StreamReader(filePath))
                        using (var csv = new CsvReader(reader, config))
                        {
                            // Register the ClassMap to handle CSV mappings
                            csv.Context.RegisterClassMap(classMap);

                            // Read and validate headers first
                            csv.Read();
                            csv.ReadHeader();
                            headers = csv.HeaderRecord;       // Read all records into a list 
                            while (csv.Read())
                            {
                                try
                                {
                                    // Try to get record, if missing field then it won't throw an exception
                                    var record = csv.GetRecord<T>();
                                    records.Add(record);
                                }
                                catch (CsvHelperException ex)
                                {
                                    // Log the exception and continue to the next record
                                    Log.Warning($"Skipping invalid record at row {rowNumber}: {ex.Message}");
                                    rowNumber++;
                                    continue;
                                }
                            }
                        }
                        return records;
                    }
            catch (Exception ex)
            {
                Log.Error($"Exception In ReadCsv");
                Console.WriteLine($"Exception In ReadCsv");
                throw;
            }
        }


        /* public async Task<List<string>> SplitCsvFile(string inputFilePath)
         {
             int recordLimit = 250;  // Limit of records in each file
             decimal finaltotalamount = 0;
             int finaltotallines = 0;
             // Read all lines from the input CSV
             List<string> allLines = new List<string>(File.ReadAllLines(inputFilePath));
             var headers = allLines[0].Split(',').Select(x=>x.Trim()).ToArray();

             // Check if the file has any records to process
             if (allLines.Count <= 1) // If only the header is present
             {
                 Console.WriteLine("No data to process in the file.");
                 return new List<string>();  // Return empty list if no records to process
             }

             // Get the directory of the input file
             string directory = Path.GetDirectoryName(inputFilePath);
             string baseFileName = Path.GetFileNameWithoutExtension(inputFilePath); // Get base file name without extension

             // Extract header (first line)
             string header = allLines.FirstOrDefault();
             var header1 = allLines.First().Split(',').ToList();

             // Process records
             int currentFileSuffix = 1; // Start with the suffix "_001"

             // Update the first 250 records and write back to the same file
             List<string> updatedRecords = new List<string>();
             updatedRecords.Add(header); // Add header first

             List<string> firstRecords = allLines.Skip(1).Take(recordLimit).ToList();
             var regex = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

             // Use regex to split the record, which ignores commas inside quotes
             var invoicedata = allLines.Skip(1).Take(1).ToList();// Skip the header for the first 250 records
             var invoiceDictionary = new Dictionary<string, string>();
             // Iterate over each header and data value and map them
             var dataValues = ParseCsvLine(invoicedata.First()); // Again assuming data is comma-separated
             for (int i = 0; i < header1.Count; i++)
             {
                 if (i < dataValues.Length)
                 {
                     invoiceDictionary[header1[i]] = dataValues[i];
                 }
             }

             // Sum for the first 250 records
             decimal totalInvoiceLineAmount = CalculateInvoiceLineAmountSum(firstRecords,headers); // Calculate sum for the first records
             int totalnooflines = CalculateInvoiceLineSum (firstRecords,headers); // Calculate sum for the first records
             finaltotalamount = finaltotalamount + totalInvoiceLineAmount;
             finaltotallines = finaltotallines + totalnooflines;
             // Update EInvoiceNumber and add sum to the first 250 records
             bool isFirstRecord = true;
             foreach (var record in firstRecords)
             {
                 string updatedRecord = UpdateEInvoiceNumberAndSum(record, currentFileSuffix, totalInvoiceLineAmount, isFirstRecord,headers, invoiceDictionary, totalnooflines);  // Add suffix and sum
                 updatedRecords.Add(updatedRecord);
                 isFirstRecord = false;  // Only add the sum for the first record
             }

             // Write first 'recordLimit' records back to the original file (with updated EInvoiceNumber)
             File.WriteAllLines(inputFilePath, updatedRecords);

             // Process remaining records and split them into separate files
             List<string> remainingRecords = allLines.Skip(recordLimit + 1).ToList(); // Skip the header and the first 250 records

             int totalChunks = (int)Math.Ceiling((double)remainingRecords.Count / recordLimit); // Calculate number of chunks

             // Create a list to hold all the file paths and their content
             List<(string FilePath, List<string> Content)> filesToCreate = new List<(string, List<string>)>();

             // Update remaining records with incremented EInvoiceNumber and calculate sum
             for (int i = 0; i < totalChunks; i++)
             {
                 List<string> chunkRecords = remainingRecords.Skip(i * recordLimit).Take(recordLimit).ToList();
                 List<string> chunkWithHeader = new List<string> { header };

                 // Reset sum for each chunk
                 totalInvoiceLineAmount = CalculateInvoiceLineAmountSum(chunkRecords,headers);// Calculate sum for the chunk
                 totalnooflines = CalculateInvoiceLineSum(chunkRecords, headers);

                 // Update each chunk's EInvoiceNumber and add sum to it
                 isFirstRecord = true;
                 foreach (var record in chunkRecords)
                 {
                     string updatedRecord = UpdateEInvoiceNumberAndSum(record, currentFileSuffix + 1, totalInvoiceLineAmount, isFirstRecord,headers,invoiceDictionary, totalnooflines); // Increment suffix and add sum
                     chunkWithHeader.Add(updatedRecord);
                     isFirstRecord = false;  // Only add the sum for the first record in the chunk
                 }

                 // Generate output file name for each chunk (part1, part2, etc.)
                 string outputFilePath = Path.Combine(directory, $"{baseFileName}_split_part{currentFileSuffix + 1:D3}.csv");

                 // Add the file path and content to the list
                 filesToCreate.Add((outputFilePath, chunkWithHeader));
                 finaltotalamount = finaltotalamount + totalInvoiceLineAmount;
                 finaltotallines = finaltotallines + totalnooflines;
                 // Increment Invoice Number suffix for the next file
                 currentFileSuffix++;

             }
             Console.WriteLine($"Final total Amount : {finaltotalamount}");
             Console.WriteLine($"Final total Lines : {finaltotallines}");
             // Write all files at once
             List<string> createdFiles = new List<string>();  // List to store the paths of created files
             foreach (var file in filesToCreate)
             {
                 File.WriteAllLines(file.FilePath, file.Content);
                 Console.WriteLine($"Records split successfully. Data moved to: {file.FilePath}");
                 createdFiles.Add(file.FilePath);  // Add the file path to the list
             }

             return createdFiles;  // Return the list of created file paths
         }*/

        public async Task<List<List<string>>> SplitCsvData(string inputFilePath)
        {
            int recordLimit = 250;  // Limit of records in each chunk
            decimal finaltotalamount = 0;
            int finaltotallines = 0;

            // Read all lines from the input CSV
            List<string> allLines = new List<string>(File.ReadAllLines(inputFilePath));
            var headers = allLines[0].Split(',').Select(x => x.Trim()).ToArray();

            // Check if the file has any records to process
            if (allLines.Count <= 1) // If only the header is present
            {
                Console.WriteLine("No data to process in the file.");
                return new List<List<string>>();  // Return empty list if no records to process
            }

            // Extract header (first line)
            string header = allLines.FirstOrDefault();
            var header1 = allLines.First().Split(',').ToList();

            // Process records
            List<List<string>> allChunks = new List<List<string>>();  // Store all chunks here

            List<string> firstRecords = allLines.Skip(1).Take(recordLimit).ToList();
            var regex = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            // Use regex to split the record, which ignores commas inside quotes
            var invoicedata = allLines.Skip(1).Take(1).ToList();  // Skip the header for the first 250 records
            var invoiceDictionary = new Dictionary<string, string>();

            // Iterate over each header and data value and map them
            var dataValues = ParseCsvLine(invoicedata.First()); // Assuming data is comma-separated
            for (int i = 0; i < header1.Count; i++)
            {
                if (i < dataValues.Length)
                {
                    invoiceDictionary[header1[i]] = dataValues[i];
                }
            }

            // Sum for the first 250 records
            decimal totalInvoiceLineAmount = CalculateInvoiceLineAmountSum(firstRecords, headers); // Calculate sum for the first records
            int totalnooflines = CalculateInvoiceLineSum(firstRecords, headers); // Calculate sum for the first records
            finaltotalamount = finaltotalamount + totalInvoiceLineAmount;
            finaltotallines = finaltotallines + totalnooflines;

            // Add the first 250 records and their sums
            List<string> chunkWithHeader = new List<string> { header };
            bool isFirstRecord = true;
            foreach (var record in firstRecords)
            {
                string updatedRecord = UpdateEInvoiceNumberAndSum(record, 1, totalInvoiceLineAmount, isFirstRecord, headers, invoiceDictionary, totalnooflines); // Add sum and suffix
                chunkWithHeader.Add(updatedRecord);
                isFirstRecord = false;  // Only add the sum for the first record
            }

            allChunks.Add(chunkWithHeader); // Add this chunk to the final list

            // Process remaining records and split them into chunks in memory
            List<string> remainingRecords = allLines.Skip(recordLimit + 1).ToList(); // Skip the header and the first 250 records

            int totalChunks = (int)Math.Ceiling((double)remainingRecords.Count / recordLimit); // Calculate number of chunks

            // Update remaining records with incremented EInvoiceNumber and calculate sum
            for (int i = 0; i < totalChunks; i++)
            {
                List<string> chunkRecords = remainingRecords.Skip(i * recordLimit).Take(recordLimit).ToList();
                List<string> chunkWithHeaderPart = new List<string> { header };

                // Reset sum for each chunk
                totalInvoiceLineAmount = CalculateInvoiceLineAmountSum(chunkRecords, headers);  // Calculate sum for the chunk
                totalnooflines = CalculateInvoiceLineSum(chunkRecords, headers);

                // Update each chunk's EInvoiceNumber and add sum to it
                isFirstRecord = true;
                foreach (var record in chunkRecords)
                {
                    string updatedRecord = UpdateEInvoiceNumberAndSum(record, i + 2, totalInvoiceLineAmount, isFirstRecord, headers, invoiceDictionary, totalnooflines);  // Increment suffix and add sum
                    chunkWithHeaderPart.Add(updatedRecord);
                    isFirstRecord = false;  // Only add the sum for the first record in the chunk
                }

                allChunks.Add(chunkWithHeaderPart); // Add this chunk to the final list
                finaltotalamount = finaltotalamount + totalInvoiceLineAmount;
                finaltotallines = finaltotallines + totalnooflines;
            }

            Console.WriteLine($"Final total Amount : {finaltotalamount}");
            Console.WriteLine($"Final total Lines : {finaltotallines}");

            return allChunks;  // Return the list of chunks (each chunk is a list of strings)
        }


        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var inQuotes = false;
            var startIndex = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    inQuotes = !inQuotes; // Toggle the inQuotes flag
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    // Split at the comma if not inside quotes
                    result.Add(line.Substring(startIndex, i - startIndex));
                    startIndex = i + 1;
                }
            }

            // Add the last field
            result.Add(line.Substring(startIndex).Trim('"'));

            return result.ToArray();
        }
        // Method to calculate the sum of Invoice Line Amount for a list of records
        private decimal CalculateInvoiceLineAmountSum(List<string> records, string[] headers)
        {
            decimal totalInvoiceLineAmount = 0;

            // Create a StringReader to read all records
            using (StringReader reader = new StringReader(string.Join(Environment.NewLine, records)))
            using (TextFieldParser parser = new TextFieldParser(reader))
            {
                // Set the delimiter and specify that fields are enclosed in quotes
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;

                // Skip the header row (if it exists)
                // parser.ReadLine();

                // Read each line and process the records
                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();
                    int lineamount = Array.IndexOf(headers, "Invoice line net amount");
                    int nooflines = Array.IndexOf(headers, "Total number of ınvoıce lines");
                    // Ensure the fields array has sufficient columns (in your case at least 62 columns)
                    decimal invoiceLineAmount = 0;

                    // Try parsing Invoice Line Amount from the correct field (index 61)
                    if (decimal.TryParse(fields[lineamount].Trim(), out invoiceLineAmount))
                    {
                        totalInvoiceLineAmount += invoiceLineAmount;
                    }
                }
            }

            return totalInvoiceLineAmount;
        }
        private int CalculateInvoiceLineSum(List<string> records, string[] headers)
        {
            int totalnooflines = 0;
            

            // Create a StringReader to read all records
            using (StringReader reader = new StringReader(string.Join(Environment.NewLine, records)))
            using (TextFieldParser parser = new TextFieldParser(reader))
            {
                // Set the delimiter and specify that fields are enclosed in quotes
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;

                // Skip the header row (if it exists)
                // parser.ReadLine();

                // Read each line and process the records
                while (!parser.EndOfData)
                {
                    parser.ReadFields(); // Read the line (but don't process it)
                    totalnooflines++;
                }
            }

            return totalnooflines;
        }


        // Update EInvoiceNumber method to add suffix and also return updated record

        private string UpdateEInvoiceNumberAndSum(string record, int fileSuffix, decimal totalInvoiceLineAmount, bool isFirstRecord, string[] headers, Dictionary<string, string> invoiceDictionary,int nooflines)
    {
        // Regular expression to split CSV data while ignoring commas inside quotes
        var regex = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

        // Use regex to split the record, which ignores commas inside quotes
        string[] fields = regex.Split(record);
            string formattedSuffix = $"{fileSuffix:D2}";
            int invoiceversion = -1;
            int buyername = -1;
            int buyerTIN = -1;
            int buyeridentificationno = -1;
            int buyercontactno = -1;
            int buyeradd0 = -1;
            int buyeradd1 = -1;
            int buyeradd2 = -1;
            int buyerpostalzone = -1;
            int buyercity = -1;
            int buyerstate = -1;
            int buyercountry = -1;
            if (Array.IndexOf(headers, "e-Invoice Version") >= 0)
            {
                invoiceversion = Array.IndexOf(headers, "e-Invoice Version");
            }
            else if (Array.IndexOf(headers, "e - Invoice Version") >= 0)
            {
                invoiceversion = Array.IndexOf(headers, "e - Invoice Version");
            }
            int invoicetypecode = Array.IndexOf(headers, "e-Invoice Type Code");
            int invoicedate = Array.IndexOf(headers, "e-Invoice Date");
            int invoicetime = Array.IndexOf(headers, "e-Invoice Time");
            int currencycode = Array.IndexOf(headers, "Invoice Currency Code");
            int suppliername = Array.IndexOf(headers, "Supplier's Name");
            int supplierTIN = Array.IndexOf(headers, "Supplier’s TIN");
            int suppliercategory = Array.IndexOf(headers, "Supplier’s Category");
            int supplierBRN = Array.IndexOf(headers, "Supplier’s Busıness Registration Number");
            int supplierSST = Array.IndexOf(headers, "Supplier’s SST Registration Number");
            int supplieremail = Array.IndexOf(headers, "Supplier’s e-mail");
            int supplierMSIC = Array.IndexOf(headers, "Supplier’s Malaysia Standard Industrial Classification (MSIC) Code");
            int supplierBAD = Array.IndexOf(headers, "Supplier’s Business Activity Description");
            int supplierContactNumber = Array.IndexOf(headers, "Supplier’s Contact Number");
            int supplierAdd0 = Array.IndexOf(headers, "Supplier’s Address Line 0");
            int supplierAdd1 = Array.IndexOf(headers, "Supplier’s Address Line 1");
            int supplierAdd2 = Array.IndexOf(headers, "Supplier’s Address Line 2");
            int supplierpostalzone = Array.IndexOf(headers, "Supplier’s Postal Zone");
            int suppliercity = Array.IndexOf(headers, "Supplier’s City Name");
            int supplierstate = Array.IndexOf(headers, "Supplier’s State");
            int suppliercountry = Array.IndexOf(headers, "Supplier’s Country");
            int suminvlinenetamt = Array.IndexOf(headers, "Sum of Invoice line net amount");
            int totalnetamount = Array.IndexOf(headers, "Total Net Amount");
            int totalexcludingtax = Array.IndexOf(headers, "Total Excluding Tax");
            int totalincludingtax = Array.IndexOf(headers, "Total Including Tax");
            int totalpayableamt = Array.IndexOf(headers, "Total Payable Amount");
            int totalnooflines = Array.IndexOf(headers, "Total number of invoice lines");
            int debtorcode = Array.IndexOf(headers, "Debtor Code");
            int debtoraddress = Array.IndexOf(headers, "Debtor Address");
            if (Array.IndexOf(headers, "Buyer Name") >= 0)
            {
                buyername = Array.IndexOf(headers, "Buyer Name");
            }
            else if (Array.IndexOf(headers, "Buyer’s Name") >= 0)
            {
                buyername = Array.IndexOf(headers, "Buyer’s Name");
            }
            
            if (Array.IndexOf(headers, "Buyer TIN") >= 0)
            {
                buyerTIN = Array.IndexOf(headers, "Buyer TIN");
            }
            else if (Array.IndexOf(headers, "Buyer’s TIN") >= 0)
            {
                buyerTIN = Array.IndexOf(headers, "Buyer’s TIN");
            }
            
            if (Array.IndexOf(headers, "Buyer’s Identification Number / Passport Number") >= 0)
            {
                buyeridentificationno = Array.IndexOf(headers, "Buyer’s Identification Number / Passport Number");
            }
            else if (Array.IndexOf(headers, "Buyer Identification No") >= 0)
            {
                buyeridentificationno = Array.IndexOf(headers, "Buyer Identification No");
            }

            if (Array.IndexOf(headers, "Buyer’s Contact Number") >= 0)
            {
                buyercontactno = Array.IndexOf(headers, "Buyer’s Contact Number");
            }
            else if (Array.IndexOf(headers, "Buyer Contact Number") >= 0)
            {
                buyercontactno = Array.IndexOf(headers, "Buyer Contact Number");
            }
            
            if (Array.IndexOf(headers, "Buyer’s Address Line 0") >= 0)
            {
                buyeradd0 = Array.IndexOf(headers, "Buyer’s Address Line 0");
            }
            else if (Array.IndexOf(headers, "Buyer Address Line 0") >= 0)
            {
                buyeradd0 = Array.IndexOf(headers, "Buyer Address Line 0");
            }
            
            if (Array.IndexOf(headers, "Buyer’s Address Line 1") >= 0)
            {
                buyeradd1 = Array.IndexOf(headers, "Buyer’s Address Line 1");
            }
            else if (Array.IndexOf(headers, "Buyer Address Line 1") >= 0)
            {
                buyeradd1 = Array.IndexOf(headers, "Buyer Address Line 1");
            }
            
            if (Array.IndexOf(headers, "Buyer’s Address Line 2") >= 0)
            {
                buyeradd2 = Array.IndexOf(headers, "Buyer’s Address Line 2");
            }
            else if (Array.IndexOf(headers, "Buyer Address Line 2") >= 0)
            {
                buyeradd2 = Array.IndexOf(headers, "Buyer Address Line 2");
            }

             if (Array.IndexOf(headers, "Buyer’s Postal Zone") >= 0)
            {
                buyerpostalzone = Array.IndexOf(headers, "Buyer’s Postal Zone");
            }
            else if (Array.IndexOf(headers, "Buyer Postal Zone") >= 0)
            {
                buyerpostalzone = Array.IndexOf(headers, "Buyer Postal Zone");
            }
             if (Array.IndexOf(headers, "Buyer’s City Name") >= 0)
            {
                buyercity = Array.IndexOf(headers, "Buyer’s City Name");
            }
            else if (Array.IndexOf(headers, "Buyer City Name") >= 0)
            {
                buyercity = Array.IndexOf(headers, "Buyer City Name");
            }
             if (Array.IndexOf(headers, "Buyer’s State") >= 0)
            {
                buyerstate = Array.IndexOf(headers, "Buyer’s State");
            }
            else if (Array.IndexOf(headers, "Buyer State") >= 0)
            {
                buyerstate = Array.IndexOf(headers, "Buyer State");
            }
             if (Array.IndexOf(headers, "Buyer’s Country") >= 0)
            {
                buyercountry = Array.IndexOf(headers, "Buyer’s Country");
            }
            else if (Array.IndexOf(headers, "Buyer Country") >= 0)
            {
                buyercountry = Array.IndexOf(headers, "Buyer Country");
            }

            int invoicenumberindex = -1;
            if(Array.IndexOf(headers, "e-Invoice Code / Number") >= 0)
            {
                invoicenumberindex = Array.IndexOf(headers, "e-Invoice Code / Number");
            } else if(Array.IndexOf(headers, "e-Invoice Number") >= 0)
            {
                invoicenumberindex = Array.IndexOf(headers, "e-Invoice Number");
            }
            /*if (fields[invoicenumberindex].Length >0)
            {
                if (invoicenumberindex >= 0 && fields[invoicenumberindex].StartsWith("\"") && fields[invoicenumberindex].EndsWith("\""))
                {
                    // Add suffix inside the quotes, removing the quotes first, appending the suffix, and re-adding the quotes
                    fields[invoicenumberindex] = "\"" + fields[invoicenumberindex].Trim('"') + $"_{fileSuffix:D3}" + "\"";
                }
                else
                {
                    // If it's not quoted, just append the suffix normally
                    fields[invoicenumberindex] = $"{fields[invoicenumberindex]}_{fileSuffix:D3}";
                    Console.WriteLine(fields[invoicenumberindex]);
                }
            }*/
            if (isFirstRecord)
            {
                    if (invoicenumberindex >= 0 && fields.Length > invoicenumberindex)
                    {
                        if (Array.IndexOf(headers, "e-Invoice Code / Number") >= 0)
                        {
                            fields[invoicenumberindex] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "e-Invoice Code / Number").Value + "_" + formattedSuffix;
                        }
                        else if (Array.IndexOf(headers, "e-Invoice Number") >= 0)
                        {
                            fields[invoicenumberindex] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "e-Invoice Number").Value + "_" + formattedSuffix;
                        }
                    }
            }
            else
            {
                if (fields[invoicenumberindex].Length > 0)
                {
                    if (invoicenumberindex >= 0 && fields.Length > invoicenumberindex)
                    {
                        if (Array.IndexOf(headers, "e-Invoice Code / Number") >= 0)
                        {
                            fields[invoicenumberindex] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "e-Invoice Code / Number").Value + "_" + formattedSuffix;
                        }
                        else if (Array.IndexOf(headers, "e-Invoice Number") >= 0)
                        {
                            fields[invoicenumberindex] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "e-Invoice Number").Value + "_" + formattedSuffix;
                        }
                    }
                }
            }

            int invoiceLineAmountIndex = Array.IndexOf(headers, "Invoice line net amount");
            
            if (isFirstRecord)
            {
                if (invoiceversion >= 0 && fields.Length > invoiceversion)
                {
                    if (Array.IndexOf(headers, "e-Invoice Version") >= 0)
                    {
                        fields[invoiceversion] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "e-Invoice Version").Value;
                    }
                    else if (Array.IndexOf(headers, "e - Invoice Version") >= 0)
                    {
                        fields[invoiceversion] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "e - Invoice Version").Value;
                    }
                }
                if (invoicetypecode >= 0 && fields.Length > invoicetypecode)
                    fields[invoicetypecode] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "e-Invoice Type Code").Value;

                if (invoicedate >= 0 && fields.Length > invoicedate)
                    fields[invoicedate] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "e-Invoice Date").Value;

                if (invoicetime >= 0 && fields.Length > invoicetime)
                    fields[invoicetime] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "e-Invoice Time").Value;

                if (currencycode >= 0 && fields.Length > currencycode)
                    fields[currencycode] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Invoice Currency Code").Value;

                if (suppliername >= 0 && fields.Length > suppliername)
                    fields[suppliername] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier's Name").Value;

                if (supplierTIN >= 0 && fields.Length > supplierTIN)
                    fields[supplierTIN] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s TIN").Value;

                if (suppliercategory >= 0 && fields.Length > suppliercategory)
                    fields[suppliercategory] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s Category").Value;

                if (supplierBRN >= 0 && fields.Length > supplierBRN)
                    fields[supplierBRN] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s Busıness Registration Number").Value;

                if (supplierSST >= 0 && fields.Length > supplierSST)
                    fields[supplierSST] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s SST Registration Number").Value;

                if (supplieremail >= 0 && fields.Length > supplieremail)
                    fields[supplieremail] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s e-mail").Value;

                if (supplierMSIC >= 0 && fields.Length > supplierMSIC)
                    fields[supplierMSIC] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s Malaysia Standard Industrial Classification (MSIC) Code").Value;

                if (supplierBAD >= 0 && fields.Length > supplierBAD)
                    fields[supplierBAD] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s Business Activity Description").Value;

                if (supplierContactNumber >= 0 && fields.Length > supplierContactNumber)
                    fields[supplierContactNumber] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s Contact Number").Value;

                if (supplierAdd0 >= 0 && fields.Length > supplierAdd0)
                    fields[supplierAdd0] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s Address Line 0").Value;

                if (supplierAdd1 >= 0 && fields.Length > supplierAdd1)
                    fields[supplierAdd1] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s Address Line 1").Value;

                if (supplierAdd2 >= 0 && fields.Length > supplierAdd2)
                    fields[supplierAdd2] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s Address Line 2").Value;

                if (supplierpostalzone >= 0 && fields.Length > supplierpostalzone)
                    fields[supplierpostalzone] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s Postal Zone").Value;

                if (suppliercity >= 0 && fields.Length > suppliercity)
                    fields[suppliercity] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s City Name").Value;

                if (supplierstate >= 0 && fields.Length > supplierstate)
                    fields[supplierstate] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s State").Value;

                if (suppliercountry >= 0 && fields.Length > suppliercountry)
                    fields[suppliercountry] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Supplier’s Country").Value;

                if (suminvlinenetamt >= 0 && fields.Length > suminvlinenetamt)
                    fields[suminvlinenetamt] = totalInvoiceLineAmount.ToString();

                if (totalnetamount >= 0 && fields.Length > totalnetamount)
                    fields[totalnetamount] = totalInvoiceLineAmount.ToString();

                if (totalexcludingtax >= 0 && fields.Length > totalexcludingtax)
                    fields[totalexcludingtax] = totalInvoiceLineAmount.ToString();

                if (totalincludingtax >= 0 && fields.Length > totalincludingtax)
                    fields[totalincludingtax] = totalInvoiceLineAmount.ToString();

                if (totalpayableamt >= 0 && fields.Length > totalpayableamt)
                    fields[totalpayableamt] = totalInvoiceLineAmount.ToString(); 
                
                if (totalnooflines >= 0 && fields.Length > totalnooflines)
                    fields[totalnooflines] = nooflines.ToString();

                if (debtorcode >= 0 && fields.Length > debtorcode)
                    fields[debtorcode] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Debtor Code").Value;

                if (debtoraddress >= 0 && fields.Length > debtoraddress)
                    fields[debtoraddress] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Debtor Address").Value;

               

                if (buyername >= 0 && fields.Length > buyername)
                {
                    if (Array.IndexOf(headers, "Buyer Name") >= 0)
                    {
                        fields[buyername] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer Name").Value;
                    }
                    else if (Array.IndexOf(headers, "Buyer’s Name") >= 0)
                    {
                        fields[buyername] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer’s Name").Value;
                    }
                }

                if (buyerTIN >= 0 && fields.Length > buyerTIN)
                {
                    if (Array.IndexOf(headers, "Buyer TIN") >= 0)
                    {
                        fields[buyerTIN] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer TIN").Value;
                    }
                    else if (Array.IndexOf(headers, "Buyer’s TIN") >= 0)
                    {
                        fields[buyerTIN] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer’s TIN").Value;
                    }
                }

                if (buyeridentificationno >= 0 && fields.Length > buyeridentificationno)
                {
                    if (Array.IndexOf(headers, "Buyer’s Identification Number / Passport Number") >= 0)
                    {
                        fields[buyeridentificationno] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer’s Identification Number / Passport Number").Value;
                    }
                    else if (Array.IndexOf(headers, "Buyer Identification No") >= 0)
                    {
                        fields[buyeridentificationno] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer Identification No").Value;
                    }
                }

                if (buyercontactno >= 0 && fields.Length > buyercontactno)
                {
                    if (Array.IndexOf(headers, "Buyer’s Contact Number") >= 0)
                    {
                        fields[buyercontactno] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer’s Contact Number").Value;
                    }
                    else if (Array.IndexOf(headers, "Buyer Contact Number") >= 0)
                    {
                        fields[buyercontactno] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer Contact Number").Value;
                    }
                }

                if (buyeradd0 >= 0 && fields.Length > buyeradd0)
                {
                    if (Array.IndexOf(headers, "Buyer’s Address Line 0") >= 0)
                    {
                        fields[buyeradd0] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer’s Address Line 0").Value;
                    }
                    else if (Array.IndexOf(headers, "Buyer Address Line 0") >= 0)
                    {
                        fields[buyeradd0] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer Address Line 0").Value;
                    }
                }

                if (buyeradd1 >= 0 && fields.Length > buyeradd1)
                {
                    if (Array.IndexOf(headers, "Buyer’s Address Line 1") >= 0)
                    {
                        fields[buyeradd1] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer’s Address Line 1").Value;
                    }
                    else if (Array.IndexOf(headers, "Buyer Address Line 1") >= 0)
                    {
                        fields[buyeradd1] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer Address Line 1").Value;
                    }
                }

                if (buyeradd2 >= 0 && fields.Length > buyeradd2)
                {
                    if (Array.IndexOf(headers, "Buyer’s Address Line 2") >= 0)
                    {
                        fields[buyeradd2] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer’s Address Line 2").Value;
                    }
                    else if (Array.IndexOf(headers, "Buyer Address Line 2") >= 0)
                    {
                        fields[buyeradd2] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer Address Line 2").Value;
                    }
                } 
                
                if (buyerpostalzone >= 0 && fields.Length > buyerpostalzone)
                {
                    if (Array.IndexOf(headers, "Buyer’s Postal Zone") >= 0)
                    {
                        fields[buyerpostalzone] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer’s Postal Zone").Value;
                    }
                    else if (Array.IndexOf(headers, "Buyer Postal Zone") >= 0)
                    {
                        fields[buyerpostalzone] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer Postal Zone").Value;
                    }
                } 
                
                if (buyercity >= 0 && fields.Length > buyercity)
                {
                    if (Array.IndexOf(headers, "Buyer’s City Name") >= 0)
                    {
                        fields[buyercity] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer’s City Name").Value;
                    }
                    else if (Array.IndexOf(headers, "Buyer City Name") >= 0)
                    {
                        fields[buyercity] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer City Name").Value;
                    }
                } 
                
                if (buyerstate >= 0 && fields.Length > buyerstate)
                {
                    if (Array.IndexOf(headers, "Buyer’s State") >= 0)
                    {
                        fields[buyerstate] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer’s State").Value;
                    }
                    else if (Array.IndexOf(headers, "Buyer State") >= 0)
                    {
                        fields[buyerstate] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer State").Value;
                    }
                } 
                
                if (buyercountry >= 0 && fields.Length > buyercountry)
                {
                    if (Array.IndexOf(headers, "Buyer’s Country") >= 0)
                    {
                        fields[buyercountry] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer’s Country").Value;
                    }
                    else if (Array.IndexOf(headers, "Buyer Country") >= 0)
                    {
                        fields[buyercountry] = invoiceDictionary.FirstOrDefault(dic => dic.Key == "Buyer Country").Value;
                    }
                }
            }
            // If it's the first record, add the sum to the 63rd column (index 62)
            /*  if (isFirstRecord && fields.Length > 64)
          {
              fields[63] = totalInvoiceLineAmount.ToString();  // Sum of Invoice Line Amount to 63rd column
              fields[66] = totalInvoiceLineAmount.ToString();  // Sum of Invoice Line Amount to 63rd column
              fields[68] = totalInvoiceLineAmount.ToString();  // Sum of Invoice Line Amount to 63rd column
              fields[70] = totalInvoiceLineAmount.ToString();  // Sum of Invoice Line Amount to 63rd column
              fields[71] = totalInvoiceLineAmount.ToString();  // Sum of Invoice Line Amount to 63rd column
              //fields[88] = totalInvoiceLineAmount.ToString();  // Sum of Invoice Line Amount to 63rd column
          }
  */
            // Return updated record with Invoice Number appended and sum
            return string.Join(",", fields);
    }


  

    /* public async Task<List<T>> ReadCsv<T>(string filePath, ClassMap classMap)
     {
         try
         {
             var records = new List<T>();
             int rowNumber = 1; // Start from row 1
             string[] headers = null; // Store headers for reference

             var config = new CsvConfiguration(CultureInfo.InvariantCulture)
             {
                 BadDataFound = args =>
                 {
                     string error = null;
                     string header = null;
                     // Get the raw record (the entire row as a string)
                     string rawRecord = args.RawRecord;
                     var columns = rawRecord.Split(',');
                     // Iterate over the columns and check for issues
                     for (int i = 0; i < columns.Length; i++)
                     {
                         string column = columns[i].Trim();
                         string headerName = headers != null && i < headers.Length ? headers[i] : $"Column {i + 1}";

                         // Check for unescaped single quote (')
                         if (column.Contains("'"))
                         {
                             header = headerName;
                             error = "contains an unescaped single quote (')";
                             Log.Warning($"Row {rowNumber}, Header '{headerName}' contains an unescaped single quote (') at: {column}");
                             Console.WriteLine($"Row {rowNumber}, Header '{headerName}' contains an unescaped single quote (') at: {column}");
                             DataError(header, error);
                         }

                         // Check for unescaped double quote (")
                         if (column.Contains("\""))
                         {
                             header = headerName;
                             error = "contains an unescaped double quote (\")";
                             Log.Warning($"Row {rowNumber}, Header '{headerName}' contains an unescaped double quote (\") at: {column}");
                             Console.WriteLine($"Row {rowNumber}, Header '{headerName}' contains an unescaped double quote (\") at: {column}");
                             DataError(header, error);
                         }

                         // Check for commas that are not inside quotes (assuming unquoted fields with commas are problematic)
                         if (column.Contains(",") && !column.StartsWith("\"") && !column.EndsWith("\""))
                         {
                             header = headerName;
                             error = "contains a field with a comma but is not properly quoted";
                             Log.Warning($"Row {rowNumber}, Header '{headerName}' contains a field with a comma but is not properly quoted: {column}");
                             Console.WriteLine($"Row {rowNumber}, Header '{headerName}' contains a field with a comma but is not properly quoted: {column}");
                             DataError(header, error);
                         }
                     }

                     rowNumber++; // Increment the row number after each record
                 },
                 // Additional CsvReader configuration
                 PrepareHeaderForMatch = args => args.Header.Replace("’", "'")  // Replace curly apostrophe in headers
             };

             using (var reader = new StreamReader(filePath))
             using (var csv = new CsvReader(reader, config))
             {
                 // Register the ClassMap to handle CSV mappings
                 csv.Context.RegisterClassMap(classMap);

                 // Read and validate headers first
                 csv.Read();
                 csv.ReadHeader();
                 headers = csv.HeaderRecord;

                 // Read all records into a list and replace curly apostrophes in fields
                 var recordsList = csv.GetRecords<dynamic>().ToList(); // Read as dynamic

                 // Now iterate over each record and replace curly apostrophes in all fields
                 foreach (var record in recordsList)
                 {
                     // Iterate over each property of the record (fields)
                     var dictionary = record as IDictionary<string, object>;
                     if (dictionary != null)
                     {
                         foreach (var key in dictionary.Keys.ToList())
                         {
                             var value = dictionary[key]?.ToString();
                             if (!string.IsNullOrEmpty(value))
                             {
                                 // Replace curly apostrophe with straight apostrophe
                                 dictionary[key] = value.Replace("’", "'");
                             }
                         }
                     }
                 }

                 // Map the dynamic records to the desired T type
                 records = recordsList.Select(r => (T)Convert.ChangeType(r, typeof(T))).ToList();
             }
             return records;
         }
         catch (Exception ex)
         {
             Log.Error($"Exception In ReadCsv: {ex.Message}");
             Console.WriteLine($"Exception In ReadCsv: {ex.Message}");
             throw;
         }
     }*/





    public async Task<ETLProcess> ETLProcess(InvoiceCSVData data, InvoiceLineItems invoiceLineItems)
        {
            Log.Information("ETLProcess Service Called for Validation");
            Console.WriteLine("ETLProcess Service Called for Validation");

            // Python Validation
           /* try
            {
                // Serialize the data objects to JSON
                var jsonData = JsonConvert.SerializeObject(data);
                var jsonLineItems = JsonConvert.SerializeObject(invoiceLineItems);

                // Path to Python executable and the Python script
                string pythonExecutable = _appSettings.PythonExe;
                string pythonScriptPath = _appSettings.PythonScript;  // Update with your script path

                // Set up the process start info
                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = pythonExecutable,  // Path to python.exe
                    Arguments = pythonScriptPath, // Only the script path now (we will pass data via stdin)
                    UseShellExecute = false,      // Don't use shell execute
                    RedirectStandardInput = true, // Allow passing data via standard input
                    RedirectStandardOutput =    true, // We need to capture the output
                    RedirectStandardError = true, // Capture errors from Python
                    CreateNoWindow = true         // Don't show a window
                };

                // Start the process
                using (Process process = Process.Start(start))
                {
                    if (process == null)
                    {
                        Console.WriteLine("Failed to start the Python process.");
                        throw new InvalidOperationException("Failed to start the Python process.");
                    }

                    // Write JSON data to standard input (stdin) of the Python process
                    using (StreamWriter writer = process.StandardInput)
                    {
                        if (writer.BaseStream.CanWrite)
                        {
                            writer.WriteLine(jsonData);      // Pass the first JSON data
                            writer.WriteLine(jsonLineItems); // Pass the second JSON data (line items)
                        }
                    }

                    // Read the output and error streams from the Python process
                    using (StreamReader reader = process.StandardOutput)
                    using (StreamReader errorReader = process.StandardError)
                    {
                        // Capture output and error (if any)
                        string output = await reader.ReadToEndAsync();
                        string errors = await errorReader.ReadToEndAsync();

                        if (!string.IsNullOrEmpty(errors))
                        {
                            Console.WriteLine("Error in Python script: " + errors);
                            throw new InvalidOperationException("Error in Python validation.");
                        }

                        

                        // Parse the JSON result from Python
                        var validationResult = JsonConvert.DeserializeObject<Dictionary<string, string>>(output);

                        // Check if the output is valid and contains the expected keys
                        if (validationResult == null)
                        {
                            throw new InvalidOperationException("Invalid output from Python validation.");
                        }
                        var validationResult1 = new InvoiceValidationResult()
                        {
                            EInvoiceNumber = data.EInvoiceNumber,
                            InvoiceVersion = validationResult.GetValueOrDefault("InvoiceVersion", "Invoice Version Null"),
                            InvoiceTypeCode = validationResult.GetValueOrDefault("InvoiceTypeCode", "Invoice TypeCode Null"),
                            InvoiceDate = validationResult.GetValueOrDefault("InvoiceDate", "Invoice Date Null"),
                            BuyerName = validationResult.GetValueOrDefault("BuyerName", "Buyer Name Null"),
                            BuyerCategory = validationResult.GetValueOrDefault("BuyerCategory", "Buyer Category Null"),
                            BuyerBRNNumber = validationResult.GetValueOrDefault("BuyerBRNNumber", "Buyer BRN Number Null"),
                            BuyersTIN = validationResult.GetValueOrDefault("BuyersTIN", "Buyer Category Null"),
                            BuyerTelephone = validationResult.GetValueOrDefault("BuyerTelephone", "Buyer Telephone Null"),
                            BuyerAddressLine0 = validationResult.GetValueOrDefault("BuyerAddressLine0", "Buyer Address Null"),
                            BuyerCityName = validationResult.GetValueOrDefault("BuyerCityName", "Buyer City Null"),
                            BuyersState = validationResult.GetValueOrDefault("BuyersState", "Buyer State Null"),
                            BuyerCountry = validationResult.GetValueOrDefault("BuyerCountry", "Buyer Country Null"),
                            CurrencyCode = validationResult.GetValueOrDefault("CurrencyCode", "Currency Code Null"),
                            UnitPrice = validationResult.GetValueOrDefault("UnitPrice", "Unit Price Null"),
                            SubTotal = validationResult.GetValueOrDefault("SubTotal", "Subtotal Null"),
                            Quantity = validationResult.GetValueOrDefault("Quantity", "Quantity Null"),
                            ProductDescription = validationResult.GetValueOrDefault("ProductDescription", "Product Description Null"),
                            Buyeremail = validationResult.GetValueOrDefault("Buyeremail", "Buyer Email Null")
                        };
                        await WriteValidationResultsToCsv(validationResult1);
                        // Create the ETLProcess object based on Python validation result
                        var etlprocess = new ETLProcess
                        {
                            ETLJobName = null,
                            InvoiceNumber = data.EInvoiceNumber,
                            InvoiceCode = data.CbcInvoiceTypeCode,
                            FileName = null,
                            FileDateTime = null,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now,
                            ETLProcessDatetime = DateTime.Now,
                            EInvoiceNumber = validationResult.GetValueOrDefault("EInvoiceNumber", "Invoice Number Null"),
                            InvoiceVersion = validationResult.GetValueOrDefault("InvoiceVersion", "Invoice Version Null"),
                            InvoiceTypeCode = validationResult.GetValueOrDefault("InvoiceTypeCode", "Invoice TypeCode Null"),
                            InvoiceDate = validationResult.GetValueOrDefault("InvoiceDate", "Invoice Date Null"),
                            BuyerName = validationResult.GetValueOrDefault("BuyerName", "Buyer Name Null"),
                            BuyerCategory = validationResult.GetValueOrDefault("BuyerCategory", "Buyer Category Null"),
                            BuyerBRNNumber = validationResult.GetValueOrDefault("BuyerBRNNumber", "Buyer BRN Number Null"),
                            BuyersTIN = validationResult.GetValueOrDefault("BuyersTIN", "Buyer Category Null"),
                            BuyerTelephone = validationResult.GetValueOrDefault("BuyerTelephone", "Buyer Telephone Null"),
                            BuyerAddressLine0 = validationResult.GetValueOrDefault("BuyerAddressLine0", "Buyer Address Null"),
                            BuyerCityName = validationResult.GetValueOrDefault("BuyerCityName", "Buyer City Null"),
                            BuyersState = validationResult.GetValueOrDefault("BuyersState", "Buyer State Null"),
                            BuyerCountry = validationResult.GetValueOrDefault("BuyerCountry", "Buyer Country Null"),
                            CurrencyCode = validationResult.GetValueOrDefault("CurrencyCode", "Currency Code Null"),
                            UnitPrice = validationResult.GetValueOrDefault("UnitPrice", "Unit Price Null"),
                            SubTotal = validationResult.GetValueOrDefault("SubTotal", "Subtotal Null"),
                            Quantity = validationResult.GetValueOrDefault("Quantity", "Quantity Null"),
                            ProductDescription = validationResult.GetValueOrDefault("ProductDescription", "Product Description Null"),
                            Buyeremail = validationResult.GetValueOrDefault("Buyeremail", "Buyer Email Null")
                        };

                        return etlprocess;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in validating invoice: {ex.Message}");
                throw;
            }*/
        

            // .Net Validation
            
            try
            {
                var stopwatch = Stopwatch.StartNew();
                List<string> statecode = new List<string> { "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17" };
                List<string> invoicetypecode = new List<string> { "01", "02", "03", "04", "11", "12", "13", "14" };
                var validationResult = new InvoiceValidationResult()
                {
                    EInvoiceNumber = data.EInvoiceNumber,
                    InvoiceTypeCode = string.IsNullOrEmpty(data.CbcInvoiceTypeCode) ? "Invoice TypeCode Null" : invoicetypecode.Contains(data.CbcInvoiceTypeCode) ? "Success" : "Invalid Invoice Type Code",
                    InvoiceVersion = string.IsNullOrEmpty(data.InvoiceVersion) ? "Invoice Version Null" : data.InvoiceVersion == _appSettings.InvoiceVersion ? "Success" : "Invalid Invoice Version",
                    InvoiceDate = string.IsNullOrEmpty(data.InvoiceDateTime) ? "Invoice Date Null" : DateTime.TryParseExact(data.InvoiceDateTime, "yyyy-dd-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ? "Success" : "Invalid Date Format",
                    BuyerName = string.IsNullOrEmpty(data.CbcBuyerName) ? "Buyer Name Null" : data.CbcBuyerName.Length > 300 ? "Maximum 300 Character allowed" : "Success",
                    BuyerCategory = string.IsNullOrEmpty(data.CbcBCategory) ? "Buyer Category Null" : "Success",
                    BuyerBRNNumber = string.IsNullOrEmpty(data.CbcBBRNNumber) ? "Buyer BRN Number Null" : data.CbcBBRNNumber.Length < 20 ? "Must 20 Character" : "Success",
                    BuyersTIN = string.IsNullOrEmpty(data.CbcBuyerVATID) ? "Buyer TIN Null" : "Success",
                    BuyerTelephone = string.IsNullOrEmpty(data.CbcBuyerTelephone) ? "Buyer Telephone Null" : (Regex.IsMatch(data.CbcBuyerTelephone, @"^\+(\d+)$")) ? "Success" : "Invalid Buyer Telephone",
                    BuyerAddressLine0 = string.IsNullOrEmpty(data.CbcBStreetName) ? "Buyeraddress Null" : data.CbcBStreetName.Length < 150 ? "Maximum 150 Character allowed" : "Success",
                    BuyerCityName = string.IsNullOrEmpty(data.CbcBCityName) ? "Buyer City Null" : "Success",
                    BuyersState = string.IsNullOrEmpty(data.CbcBCountrySubentity) ? "Buyer State Null" : statecode.Contains(data.CbcBCountrySubentity) ? "Success" : "Invalid State",
                    BuyerCountry = string.IsNullOrEmpty(data.CbcBCountryIdentificationCode) ? "Buyer Country Null" : data.CbcBCountryIdentificationCode == "MYS" ? "Success" : "Invalid Country Code",
                    CurrencyCode = string.IsNullOrEmpty(data.CbcDocumentCurrencyCode) ? "Currency Code Null" : data.CbcDocumentCurrencyCode == "MYR" ? "Success" : "Invalid Currency Code",
                    UnitPrice = string.IsNullOrEmpty(invoiceLineItems.CbcPrice) ? "Unit Price Null" : (Regex.IsMatch(invoiceLineItems.CbcPrice, @"^[+-]?\d+(\.\d+)?$") && decimal.TryParse(invoiceLineItems.CbcPrice, out _)) ? "Success" : "Invalid Unit Price",
                    SubTotal = string.IsNullOrEmpty(invoiceLineItems.CbcSubtotal) ? "Subtotal Null" : (Regex.IsMatch(invoiceLineItems.CbcSubtotal, @"^[+-]?\d+(\.\d+)?$") && decimal.TryParse(invoiceLineItems.CbcSubtotal, out _)) ? "Success" : "Invalid Subtotal",
                    Quantity = string.IsNullOrEmpty(invoiceLineItems.CbcBaseQuantity) ? "Quantity Null" : (Regex.IsMatch(invoiceLineItems.CbcBaseQuantity, @"^[+-]?\d+(\.\d+)?$") && decimal.TryParse(invoiceLineItems.CbcBaseQuantity, out _)) ? "Success" : "Invalid Subtotal",
                    ProductDescription = string.IsNullOrEmpty(invoiceLineItems.CbcDescription) ? "Product Description Null" : "Success",
                    Buyeremail = string.IsNullOrEmpty(data.CbcBuyerElectronicMail) ? "Buyer  Email Null" : (Regex.IsMatch(data.CbcBuyerElectronicMail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) ? "Success" : "Invalid Buyer Email",
                };
               
               await Task.WhenAll(WriteValidationResultsToCsv(validationResult));
                var etlprocess = new ETLProcess()
                {
                    ETLJobName = null,
                    InvoiceNumber = data.EInvoiceNumber,
                    InvoiceCode = data.CbcInvoiceTypeCode,
                    FileName = null,
                    FileDateTime = null,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    ETLProcessDatetime = DateTime.Now,
                    EInvoiceNumber = string.IsNullOrEmpty(data.EInvoiceNumber) ? "Invoice Number Null" : "Success",
                    InvoiceVersion = string.IsNullOrEmpty(data.InvoiceVersion) ? "Invoice Version Null" : data.InvoiceVersion == _appSettings.InvoiceVersion ? "Success" : "Invalid Invoice Version",
                    InvoiceTypeCode = string.IsNullOrEmpty(data.CbcInvoiceTypeCode) ? "Invoice TypeCode Null" : invoicetypecode.Contains(data.CbcInvoiceTypeCode) ? "Success" : "Invalid Invoice Type Code",
                    //InvoiceDate = data.InvoiceDateTime == null ? "Invoice Date Null" : data.InvoiceDateTime == data.InvoiceDateTime.Value.ToString("yyyy-dd-MM") ? "Success" : "Invalid Date Format",
                    InvoiceDate = string.IsNullOrEmpty(data.InvoiceDateTime) ? "Invoice Date Null" : DateTime.TryParseExact(data.InvoiceDateTime, "yyyy-dd-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ? "Success" : "Invalid Date Format",
                    BuyerName = string.IsNullOrEmpty(data.CbcBuyerName) ? "Buyer Name Null" : data.CbcBuyerName.Length > 300 ? "Maximum 300 Characters allowed" : "Success",
                    BuyerCategory = string.IsNullOrEmpty(data.CbcBCategory) ? "Buyer Category Null" : "Success",
                    BuyerBRNNumber = string.IsNullOrEmpty(data.CbcBBRNNumber) ? "Buyer BRN Number Null" : data.CbcBBRNNumber.Length < 20 ? "Must 20 Character" : "Success",
                    BuyersTIN = string.IsNullOrEmpty(data.CbcBuyerVATID) ? "Buyer Category Null" : "Success",
                    BuyerTelephone = string.IsNullOrEmpty(data.CbcBuyerTelephone) ? "Buyer Telephone Null" : (Regex.IsMatch(data.CbcBuyerTelephone, @"^\+(\d+)$")) ? "Success" : "Invalid Buyer Telephone",
                    BuyerAddressLine0 = string.IsNullOrEmpty(data.CbcBStreetName) ? "Buyeraddress Null" : data.CbcBStreetName.Length > 150 ? "Maximum 150 Character allowed" : "Success",
                    BuyerCityName = string.IsNullOrEmpty(data.CbcBCityName) ? "Buyer City Null" : "Success",
                    BuyersState = string.IsNullOrEmpty(data.CbcBCountrySubentity) ? "Buyer State Null" : statecode.Contains(data.CbcBCountrySubentity) ? "Success" : "Invalid State",
                    BuyerCountry = string.IsNullOrEmpty(data.CbcBCountryIdentificationCode) ? "Buyer Country Null" : data.CbcBCountryIdentificationCode == "MYS" ? "Success" : "Invalid Country Code",
                    CurrencyCode = string.IsNullOrEmpty(data.CbcDocumentCurrencyCode) ? "Currency Code Null" : data.CbcDocumentCurrencyCode == "MYR" ? "Success" : "Invalid Currency Code",
                    UnitPrice = string.IsNullOrEmpty(invoiceLineItems.CbcPrice) ? "Unit Price Null" : (Regex.IsMatch(invoiceLineItems.CbcPrice, @"^[+-]?\d+(\.\d+)?$") && decimal.TryParse(invoiceLineItems.CbcPrice, out _)) ? "Success" : "Invalid Unit Price",
                    SubTotal = string.IsNullOrEmpty(invoiceLineItems.CbcSubtotal) ? "Subtotal Null" : (Regex.IsMatch(invoiceLineItems.CbcSubtotal, @"^[+-]?\d+(\.\d+)?$") && decimal.TryParse(invoiceLineItems.CbcSubtotal, out _)) ? "Success" : "Invalid Subtotal",
                    Quantity = string.IsNullOrEmpty(invoiceLineItems.CbcBaseQuantity) ? "Quantity Null" : (Regex.IsMatch(invoiceLineItems.CbcBaseQuantity, @"^[+-]?\d+(\.\d+)?$") && decimal.TryParse(invoiceLineItems.CbcBaseQuantity, out _)) ? "Success" : "Invalid Subtotal",
                    ProductDescription = string.IsNullOrEmpty(invoiceLineItems.CbcDescription) ? "Product Description Null" : "Success",
                    Buyeremail = string.IsNullOrEmpty(data.CbcBuyerElectronicMail) ? "Buyer  Email Null" : (Regex.IsMatch(data.CbcBuyerElectronicMail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) ? "Success" : "Invalid Buyer Email",

                };
                stopwatch.Stop();   
                Log.Information($"Total time taken in ETLprocess: {stopwatch.Elapsed.TotalSeconds} seconds");
                Console.WriteLine($"Total time taken in ETLprocess: {stopwatch.Elapsed.TotalSeconds} seconds");
                //InvoiceDate = string.IsNullOrEmpty(data.eInvoiceDateTime) ? "Invoice Date Null" : data.eInvoiceDateTime.  ? "Success" : "Invalid Invoice Type Code",
                //Console.WriteLine(data.InvoiceDateTime.Value.ToShortDateString());
                //Console.WriteLine(data.eInvoiceDateTime.Value.ToString());
                // Console.WriteLine(data.eInvoiceDateTime.Value.ToString("yyyy-dd-MM"));
                // Console.WriteLine(data.eInvoiceDateTime.Value.ToShortDateString()==data.eInvoiceDateTime.Value.ToString("yyyy-dd-MM"));
                return etlprocess;

            }

            catch (Exception ex)
            {
                Log.Information($"Exception in ETLProcess : {ex.Message}");
                Console.WriteLine($"Exception in ETLProcess : {ex.Message}");
                throw;
            }
           
        }

        public async Task<ETLStatus> ETLStatus(ETLProcess etlprocess)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var etlstatus = new ETLStatus
                {
                    ETLJobName = etlprocess.ETLJobName,
                    InvoiceNumber = etlprocess.InvoiceNumber,
                    InvoiceCode = etlprocess.InvoiceCode,
                    ETLProcessDatetime = etlprocess.ETLProcessDatetime,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    Status = await CheckETLProcess(etlprocess)
                };

                return etlstatus;   
            }
            catch (Exception ex)
            {
                Log.Information($"Exception in ETLStatus : {ex.Message}");
                Console.WriteLine($"Exception in ETLStatus : {ex.Message}");
                throw;
            }
            stopwatch.Stop();   Console.WriteLine($"Total time taken: {stopwatch.Elapsed.TotalSeconds} seconds");
        }

        public async Task<string> CheckETLProcess(ETLProcess etlprocess)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                Log.Information("CheckETLProcess Service called");
                Console.WriteLine("CheckETLProcess Service called");
                if (etlprocess.InvoiceVersion.ToLower() == "success" &&
                    etlprocess.InvoiceTypeCode.ToLower() == "success" &&
                    etlprocess.InvoiceDate.ToLower() == "success" &&
                    etlprocess.BuyerName.ToLower() == "success" &&
                    etlprocess.BuyerCategory.ToLower() == "success" &&
                    etlprocess.BuyersTIN.ToLower() == "success" &&
                    etlprocess.BuyerAddressLine0.ToLower() == "success" &&
                    etlprocess.BuyerCityName.ToLower() == "success" &&
                    etlprocess.BuyersState.ToLower() == "success" &&
                    etlprocess.BuyerCountry.ToLower() == "success" &&
                     //   etlprocess.CustomerInvoicecode.ToLower() == "Success" &&
                     //    etlprocess.InvoiceUUID.ToLower() == "success" &&
                     //   etlprocess.InvoiceDocumentReference.ToLower() == "success" &&
                     etlprocess.EInvoiceNumber.ToLower() == "success" &&
                     etlprocess.CurrencyCode.ToLower() == "success" &&
                    etlprocess.UnitPrice.ToLower() == "success" &&
                //    etlprocess.Quantity.ToLower() == "success" &&
                    etlprocess.SubTotal.ToLower() == "success" &&
                    etlprocess.BuyerTelephone.ToLower() == "success" &&
                    // etlprocess.SumofInvoicelinenetAmount.ToLower() == "success" &&
                    //    etlprocess.SumofAllowances.ToLower() == "success" &&
                    //  etlprocess.TotalFeeorChargeAmount.ToLower() == "success" &&
                    //   etlprocess.TotalNetAmount.ToLower() == "success" &&
                    //     etlprocess.TotalIncludingTax.ToLower() == "success" &&
                    //    etlprocess.RoundingAmount.ToLower() == "success" &&
                    //    etlprocess.TotalPayableAmount.ToLower() == "success" &&
                    //     etlprocess.TotalDiscountValue == "success" &&
                    etlprocess.ProductDescription.ToLower() == "success" &&
                    //   etlprocess.Buyeremail == "success" &&
                    etlprocess.BuyerBRNNumber.ToLower() == "success"
                //    etlprocess.SupplierName.ToLower() == "success" &&
                //    etlprocess.SupplierCategory.ToLower() == "success" &&
                //     etlprocess.SuppliersTIN.ToLower() == "success" &&
                //     etlprocess.SupplierAddressLine0.ToLower() == "success" &&
                //    etlprocess.SupplierCityName.ToLower() == "success" &&
                //    etlprocess.SuppliersState.ToLower() == "success" &&
                //    etlprocess.SupplierCountry.ToLower() == "success" &&
                //    etlprocess.SupplierMSIC.ToLower() == "success"
                )

                {
                    stopwatch.Stop(); 
                    Log.Information($"Total time taken in checketlprocess: {stopwatch.Elapsed.TotalSeconds} seconds");
                    Console.WriteLine($"Total time taken in checketlprocess: {stopwatch.Elapsed.TotalSeconds} seconds");
                    return "ReadyForIRB";
                    
                }
                else
                {
                    stopwatch.Stop();
                    Log.Information($"Total time taken in checketlprocess: {stopwatch.Elapsed.TotalSeconds} seconds");
                    Console.WriteLine($"Total time taken in checketlprocess: {stopwatch.Elapsed.TotalSeconds} seconds");
                    return "InputDataError";
                }

            }
            catch (Exception)
            {

                throw;
            }
            
        }

        public async Task WriteValidationResultsToCsv(InvoiceValidationResult result)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
               
                // Extract command-line arguments and set up paths
                var commandargument = Environment.GetCommandLineArgs();
                var taskType = commandargument[1];
                string filePathsJson = File.ReadAllText(taskType);

                // Deserialize the JSON content back into the original file path array
                string[] filePaths = JsonConvert.DeserializeObject<string[]>(filePathsJson);

                var arg = filePaths;

                //List<string> args = JsonConvert.DeserializeObject<List<string>>(commandArguments[1]);
                List<string> taskTypes = arg
           .Select(path => path.Replace("\\\\", "\\"))  // Replace escaped backslashes with single backslashes
           .Skip(1)  // Skip the first item if needed
           .ToList();
                var domainFileGroups = taskTypes
                    .GroupBy(taskType =>
                    {
                        var path = taskType.Split(Path.DirectorySeparatorChar).ToList();
                        return new
                        {
                            DomainName = path[path.Count - 5],  // Get the domain name
                            InvoiceType = path[path.Count - 4],// Extract the invoice type
                            FileName = path[path.Count-1]
                        };
                    })
                    .ToDictionary(g => g.Key, g => g.ToList());
                var failedTasks = new List<string>();

                // Iterate over each domain and process the files concurrently
                foreach (var domainGroup in domainFileGroups)
                {
                    var domainName = domainGroup.Key.DomainName;
                    var invoiceType = domainGroup.Key.InvoiceType;
                    var domainFiles = domainGroup.Value;
                    var fileName = domainGroup.Key.FileName;
                    var despath = Path.Combine(_appSettings.ProcessedFolderPath, domainName, invoiceType, "Input", "DataError", $"{result.EInvoiceNumber}");

                    // Ensure the directory exists
                    if (!Directory.Exists(despath))
                    {
                        Directory.CreateDirectory(despath);
                    }

                    // Create the file path
                    var filePath = Path.Combine(despath, fileName);
        
                    using (var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(fileStream, Encoding.UTF8, 1024, leaveOpen: true))
                    using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
                        {
                            // Write header only if the file doesn't exist (first time)
                            var fileExists = File.Exists(filePath);
                            if (!fileExists)
                            {
                                csv.WriteHeader<InvoiceValidationResult>();  // Write header row
                                csv.NextRecordAsync();// Add newline after header row

                                // Ensure data is written immediately
                            }

                            // Ensure result is not null before writing
                            if (result != null)
                            {
                                // csv.WriteHeader<InvoiceValidationResult>();
                                // Write the single record to the CSV file
                                csv.WriteHeader<InvoiceValidationResult>();
                                csv.NextRecordAsync();
                                csv.WriteRecord(result);
                                csv.NextRecordAsync();// Write the actual validation result

                                await writer.FlushAsync();  // Ensure data is flushed to the file immediately
                            }
                            else
                            {
                                Log.Warning("No valid validation result to write.");
                            }

                            // Flush StreamWriter to ensure everything is written
                            await writer.FlushAsync();
                         
                        }

                        // Log successful write
                        Log.Information($"Successfully wrote data for InvoiceNumber {result.EInvoiceNumber} to CSV.");
                    }
                
            }
            catch (Exception ex)
            {
                // Log the error if something goes wrong
                Log.Error($"Error while writing validation result to CSV: {ex.Message}");
                Console.WriteLine($"Error while writing validation result to CSV: {ex.Message}");
                throw;
            }
            stopwatch.Stop();   
            Log.Information($"Total time taken in create dataerror file: {stopwatch.Elapsed.TotalSeconds} seconds");
            Console.WriteLine($"Total time taken in create dataerror file: {stopwatch.Elapsed.TotalSeconds} seconds");

        }
        public async Task DataError(string headername ,string errormessage)
        {
            var commandargument = Environment.GetCommandLineArgs();
            var taskType = commandargument[1];
            string filePathsJson = File.ReadAllText(taskType);

            // Deserialize the JSON content back into the original file path array
            string[] filePaths = JsonConvert.DeserializeObject<string[]>(filePathsJson);

            var arg = filePaths;

            //List<string> args = JsonConvert.DeserializeObject<List<string>>(commandArguments[1]);
            List<string> taskTypes = arg
       .Select(path => path.Replace("\\\\", "\\"))  // Replace escaped backslashes with single backslashes
       .Skip(1)  // Skip the first item if needed
       .ToList();
            var domainFileGroups = taskTypes
                .GroupBy(taskType =>
                {
                    var path = taskType.Split(Path.DirectorySeparatorChar).ToList();
                    return new
                    {
                        DomainName = path[path.Count - 5],  // Get the domain name
                        InvoiceType = path[path.Count - 4],// Extract the invoice type
                        FileName = path[path.Count - 1]
                    };
                })
                .ToDictionary(g => g.Key, g => g.ToList());
            var failedTasks = new List<string>();

            // Iterate over each domain and process the files concurrently
            foreach (var domainGroup in domainFileGroups)
            {
                var domainName = domainGroup.Key.DomainName;
                var invoiceType = domainGroup.Key.InvoiceType;
                var domainFiles = domainGroup.Value;
                var fileName = domainGroup.Key.FileName;
                var despath = Path.Combine(_appSettings.ProcessedFolderPath, domainName, invoiceType, "Input", "DataError");

                // Ensure the directory exists
                if (!Directory.Exists(despath))
                {
                    Directory.CreateDirectory(despath);
                }

                // Create the file path
                var filePath = Path.Combine(despath, fileName);
                // Check if the file already exists
                var fileExists = File.Exists(filePath);
                var headers = new List<string>
                {
                    "Column",
                    "Error"
                 };

                using (var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(fileStream, Encoding.UTF8, 1024, leaveOpen: true))
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

                    
                        var record = new List<string>() {
                        headername,
                        errormessage,
                     };

                        // Write the actual validation result to the file
                        await writer.WriteLineAsync(string.Join(",", record));
                    

                    await writer.FlushAsync(); // Ensure the buffer is flushed
                }
        }

            }

    }
}

