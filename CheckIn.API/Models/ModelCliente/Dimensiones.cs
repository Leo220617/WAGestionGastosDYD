 
namespace CheckIn.API.Models.ModelCliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Dimensiones")]
    public class Dimensiones
    {
        public int id { get; set; }
        public string codigoSAP { get; set; }
        public string Nombre { get; set; }
    }
}