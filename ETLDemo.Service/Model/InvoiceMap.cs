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

    public InvoiceDataMap(IETLService eTLDemoService)
    {
        _etlDemoService = eTLDemoService;
        _columnMappings = new Dictionary<string, string>();
        InitializeMapping().Wait(); // Ensure mappings are loaded
        ConfigureMapping(); // Apply mappings to CsvHelper
    }

    private async Task InitializeMapping()
    {
        try
        {
            var columnNames = await _etlDemoService.GetInvoiceMappingColumns();
            if (columnNames.Count > 0)
            {
                foreach (var col in columnNames)
                {
                    _columnMappings[col.CsvFieldName] = col.TableFieldName;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Information($"Exception in InvoiceDataMapping: {ex.Message}");
            Console.WriteLine($"Exception in InvoiceDataMapping: {ex.Message}");
        }
    }

    private void ConfigureMapping()
    {

        if (_columnMappings == null || !_columnMappings.Any())
            return;

        foreach (var mapping in _columnMappings)
        {
            var csvField = mapping.Key;     // CSV column name
            var modelField = mapping.Value; // Model property name

            // Use reflection to check if the property exists in InvoiceCSVData
            var property = typeof(InvoiceCSVData).GetProperty(modelField, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property != null)
            {
                Map(typeof(InvoiceCSVData), property).Name(csvField);
            }
        }
    }
}
