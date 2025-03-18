using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETL.Service.Model
{
    public class ETLStatus
    {
        [Key]
        public int Id { get; set; }
        public string ETLJobName { get; set; }
        public string InvoiceNumber { get; set; }
        public string InvoiceCode { get; set; }
        public string FileName { get; set; }
        public DateTime? FileDateTime { get; set; }
        public DateTime? ETLProcessDatetime { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
