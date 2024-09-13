 

namespace CheckIn.API.Models.ModelCliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    public class Proveedores
    {
        public int id { get; set; }
        public string Nombre { get; set; }
        public string RUC { get; set; }
        public string DV { get; set; }

    }
}