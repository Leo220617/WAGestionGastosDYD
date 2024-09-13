

namespace CheckIn.API.Models.ModelCliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("BitacoraLogin")]
    public class BitacoraLogin
    {
        public int id { get; set; }
        public int idUsuario { get; set; }
        public string IP { get; set; }
        public DateTime Fecha { get; set; }
        public string Detalle { get; set; }
    }
}