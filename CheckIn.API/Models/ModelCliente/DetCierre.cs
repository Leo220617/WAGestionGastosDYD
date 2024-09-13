 

namespace CheckIn.API.Models.ModelCliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("DetCierre")]
    public class DetCierre
    {
        [Key]
        public int id { get; set; }

        public int idCierre { get; set; }

        public int NumLinea { get; set; }

        public int idFactura { get; set; }
        public string Comentario { get; set; }
    }
}