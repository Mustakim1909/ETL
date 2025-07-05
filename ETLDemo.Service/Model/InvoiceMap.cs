using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Serilog;
using ETL.Service.Repo.Interface;
using ETL_Demo.Models;

public class InvoiceDataMap : ClassMap<InvoiceCSVData>
{
    private readonly IETLService _etlDemoService;
    private Dictionary<string, string> _columnMappings;

    public InvoiceDataMap(IETLService eTLDemoService, string documentType)
    {
        _etlDemoService = eTLDemoService;
        _columnMappings = new Dictionary<string, string>();
        InitializeMapping(documentType).Wait(); // Ensure mappings are loaded
        ConfigureMapping(); // Apply mappings to CsvHelper
    }

    private async Task InitializeMapping(string documentType)
    {
        try
        {
            var columnNames = await _etlDemoService.GetInvoiceMappingColumns(documentType);
            if (columnNames.Count > 0)
            {
                foreach (var col in columnNames)
                {
                    _columnMappings[col.TableFieldName.Trim()] = col.CsvFieldName.Trim();
                }
            }
            else
            {
                Log.Error($"Mapping is have for : {documentType}");
                Console.WriteLine($"Mapping is have for : {documentType}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Exception in InvoiceDataMapping: {ex.Message}");
            Console.WriteLine($"Exception in InvoiceDataMapping: {ex.Message}");
        }
    }

    private void ConfigureMapping()
    {
        try
        {

            if (_columnMappings == null || !_columnMappings.Any())
                return;

            foreach (var mapping in _columnMappings)
            {
                var csvField = mapping.Value;     // CSV column name
                var modelField = mapping.Key;
                // Use reflection to check if the property exists in InvoiceCSVData
                var property = typeof(InvoiceCSVData).GetProperty(modelField, BindingFlags.Public | BindingFlags.Instance);

                if (property != null)
                {
                    var memberMap = Map(typeof(InvoiceCSVData), property).Name(csvField);
                    if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                    {
                        memberMap.TypeConverter(new MultiFormatDateTimeConverter());
                    }
                    // If it's a DateTime or Nullable<DateTime>, set format
                    /* if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                     {
                         if (property.Name.ToLower().Contains("datetime"))
                         {
                             memberMap.TypeConverterOption.Format("dd-MM-yyyy hh:mm:ss tt");
                         }
                         else if (property.Name.ToLower().Contains("date"))
                         {
                             memberMap.TypeConverterOption.Format("dd-MM-yyyy");
                         }
                         else if (property.Name.ToLower().Contains("time"))
                         {
                             memberMap.TypeConverterOption.Format("hh:mm:ss");
                         }
                         else
                         {
                             memberMap.TypeConverterOption.Format("dd-MM-yyyy hh:mm:ss tt");
                         }
                     }*/
                }
            }
        }catch(Exception ex)
        {
            Log.Information($"Exception in ConfigureMapping {ex.Message}");
            Console.WriteLine($"Exception in ConfigureMapping {ex.Message}");
        }
    }
}
