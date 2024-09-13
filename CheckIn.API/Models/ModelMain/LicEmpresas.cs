namespace CheckIn.API.Models.ModelMain
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class LicEmpresas
    {
        [Key]
        [StringLength(20)]
        public string CedulaJuridica { get; set; }

        [StringLength(500)]
        public string NombreEmpresa { get; set; }

        public DateTime? FechaVencimiento { get; set; }

        public bool? Activo { get; set; }

        public string CadenaConexionBD { get; set; }

        public string CadenaConexionSAP { get; set; }
    }
}
