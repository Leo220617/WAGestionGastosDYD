namespace CheckIn.API.Models.ModelCliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class DetCompras
    {
        [Key]
        public int id { get; set; }
        [StringLength(20)]
        public string CodEmpresa { get; set; }

     
        public string CodProveedor { get; set; }

        [StringLength(2)]
        public string TipoDocumento { get; set; }
        [StringLength(50)]
        public string ClaveHacienda { get; set; }

        [StringLength(20)]
        public string ConsecutivoHacienda { get; set; }

        [StringLength(500)]
        public string NomProveedor { get; set; }

         
        public int NumFactura { get; set; }

        public int? NumLinea { get; set; }

        [StringLength(50)]
        public string CodPro { get; set; }

        [StringLength(50)]
        public string UnidadMedida { get; set; }

        [StringLength(500)]
        public string NomPro { get; set; }

        [Column(TypeName = "money")]
        public decimal? PrecioUnitario { get; set; }

        [StringLength(20)]
        public string CodCliente { get; set; }

        [StringLength(500)]
        public string NomCliente { get; set; }

        public decimal? Cantidad { get; set; }

        [Column(TypeName = "money")]
        public decimal? MontoTotal { get; set; }

        [Column(TypeName = "money")]
        public decimal? MontoDescuento { get; set; }

        [Column(TypeName = "money")]
        public decimal? SubTotal { get; set; }

        public decimal? ImpuestoTarifa { get; set; }

        [Column(TypeName = "money")]
        public decimal? ImpuestoMonto { get; set; }

        [Column(TypeName = "money")]
        public decimal? MontoTotalLinea { get; set; }

        [StringLength(1)]
        public string Estado { get; set; }

        [StringLength(500)]
        public string Observacion { get; set; }

        public int? idTipoGasto { get; set; }

        public int? idCierre { get; set; }
        [StringLength(50)]
        public string CodCabys { get; set; }
    }
}
