using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETL.Service.Model
{
    public class CsvFieldConfiguration
    {
        public int Id { get; set; }
        public string CsvFieldName { get; set; }
        public string TableFieldName { get; set; }
        public string Status { get; set; }
        public string TableName { get; set; }
    }
}
