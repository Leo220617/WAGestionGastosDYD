namespace CheckIn.API.Models.ModelCliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Login")]
    public partial class Login
    {
        public int id { get; set; }

        public int? idRol { get; set; }

        [StringLength(200)]
        public string Email { get; set; }

        [StringLength(100)]
        public string Nombre { get; set; }

        public bool? Activo { get; set; }

        [StringLength(500)]
        public string Clave { get; set; }

        public int idLoginAceptacion { get; set; }
        public string CardCode { get; set; }
        public bool CambiarClave { get; set; }
        public bool CambioFecha { get; set; }
        public DateTime FechaVencimientoClave { get; set; }
    }
}
