 

namespace CheckIn.API.Models.ModelCliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    [Table("ConexionSAP")]
    public class ConexionSAP
    {
        public int id { get; set; }
        [StringLength(50)]
        public string SAPUser { get; set; }

        [StringLength(100)]
        public string SAPPass { get; set; }

        [StringLength(50)]
        public string SQLUser { get; set; }

        [StringLength(100)]
        public string ServerSQL { get; set; }

        [StringLength(50)]
        public string ServerLicense { get; set; }

        [StringLength(100)]
        public string SQLPass { get; set; }

        [StringLength(50)]
        public string SQLType { get; set; }

        [StringLength(50)]
        public string SQLBD { get; set; }
    }
}