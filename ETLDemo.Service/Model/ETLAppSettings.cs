namespace ETL.Service.Model
{
    public class ETLAppSettings
    {
        public string InvoiceVersion { get; set; }
        public string ProcessedFolderPath { get; set; }
        public int Tenant {  get; set; }   
        public string LogDirectory { get; set; }
        public string PythonExe { get; set; }
        public string PythonScript { get; set; }
    }
}
