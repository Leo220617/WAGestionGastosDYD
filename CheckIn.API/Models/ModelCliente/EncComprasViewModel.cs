using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Spatial;

namespace CheckIn.API.Models.ModelCliente
{
    public class EncComprasViewModel
    {
        [Key]
        public int id { get; set; }
        [Required]
 
        public string CodEmpresa { get; set; }

        [Required]
     
        public string CodProveedor { get; set; }

        public string NomProveedor { get; set; }

        [Required]
        [StringLength(10)]
        public string TipoDocumento { get; set; }


        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int NumFactura { get; set; }

        public DateTime? FecFactura { get; set; }

        [StringLength(10)]
        public string TipoIdentificacionCliente { get; set; }

        [StringLength(20)]
        public string CodCliente { get; set; }

        [StringLength(500)]
        public string NomCliente { get; set; }

        [StringLength(500)]
        public string EmailCliente { get; set; }

        public int? DiasCredito { get; set; }

        [StringLength(2)]
        public string CondicionVenta { get; set; }

        [StringLength(50)]
        public string ClaveHacienda { get; set; }

        [StringLength(20)]
        public string ConsecutivoHacienda { get; set; }

        [StringLength(2)]
        public string MedioPago { get; set; }

        public byte? Situacion { get; set; }

        [StringLength(3)]
        public string CodMoneda { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalServGravados { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalServExentos { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalMercanciasGravadas { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalMercanciasExentas { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalExento { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalVenta { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalDescuentos { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalVentaNeta { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalImpuesto { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalComprobante { get; set; }

        public string XmlFacturaRecibida { get; set; }



        public DateTime? FechaGravado { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalServExonerado { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalMercExonerada { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalExonerado { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalIVADevuelto { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalOtrosCargos { get; set; }

        [StringLength(6)]
        public string CodigoActividadEconomica { get; set; }

        public int? idLoginAsignado { get; set; }

        public DateTime? FecAsignado { get; set; }



        public string PdfFactura { get; set; }

        public int? idNormaReparto { get; set; }

        public int? idTipoGasto { get; set; }

        public int? idCierre { get; set; }
        public decimal Impuesto1 { get; set; }
        public decimal Impuesto2 { get; set; }
        public decimal Impuesto4 { get; set; }
        public decimal Impuesto8 { get; set; }
        public decimal Impuesto13 { get; set; }
        public byte[] PdfFac { get; set; }
        public string ImagenBase64 { get; set; }
        public bool RegimenSimplificado { get; set; }
        public bool FacturaExterior { get; set; }
        public bool GastosVarios { get; set; }
        public bool FacturaNoRecibida { get; set; }
        public string Comentario { get; set; }

        public string ImagenB64 { get; set; }
    }
}