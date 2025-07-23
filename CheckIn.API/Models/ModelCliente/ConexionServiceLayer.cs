 
namespace CheckIn.API.Models.ModelCliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    [Table("ConexionServiceLayer")]
    public class ConexionServiceLayer
    {
        public int id { get; set; }
        public string baseUrl { get; set; }
        public string companyDB { get; set; }
        public string userName { get; set; }
        public string password { get; set; }
    }
}