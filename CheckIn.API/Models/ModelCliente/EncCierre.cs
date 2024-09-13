namespace CheckIn.API.Models.ModelCliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("EncCierre")]
    public partial class EncCierre
    {
        [Key]
        public int idCierre { get; set; }

        public string Periodo { get; set; }

        public DateTime FechaInicial { get; set; }

        public DateTime FechaFinal { get; set; }

        public int idLogin { get; set; }

        [Column(TypeName = "money")]
        public decimal? SubTotal { get; set; }

        [Column(TypeName = "money")]
        public decimal? Descuento { get; set; }

        [Column(TypeName = "money")]
        public decimal? Impuestos { get; set; }


        [Column(TypeName = "money")]
        public decimal? Total { get; set; }

       

        public decimal Impuesto1 { get; set; }
        public decimal Impuesto2 { get; set; }
        public decimal Impuesto4 { get; set; }
        public decimal Impuesto8 { get; set; }
        public decimal Impuesto13 { get; set; }
        public int? CantidadRegistros { get; set; }

        public DateTime? FechaCierre { get; set; }
        public int? idLoginAceptacion { get; set; }
        [StringLength(1)]
        public string Estado { get; set; }

        [StringLength(500)]
        public string Observacion { get; set; }
        public string CodMoneda { get; set; }
        public decimal TotalOtrosCargos { get; set; }
        public bool ProcesadaSAP { get; set; }
    }
}
