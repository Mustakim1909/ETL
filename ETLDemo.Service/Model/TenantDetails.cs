using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETL.Service.Model
{
    public class TenantDetails
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Domain { get; set; }
        public string ConnectionString { get; set; }
        public string DbType { get; set; }
        public string State { get; set; }
    }
}
