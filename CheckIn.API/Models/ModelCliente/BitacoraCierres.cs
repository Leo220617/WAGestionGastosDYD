 

namespace CheckIn.API.Models.ModelCliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("BitacoraCierres")]
    public class BitacoraCierres
    {
        public int id { get; set; }
        public int idUsuarioEnviador { get; set; }
        public int idUsuarioAceptador { get; set; }
        public int idCierre { get; set; }
        public string Detalle { get; set; }
        public DateTime Fecha { get; set; }
        public string IP { get; set; }
    }
}