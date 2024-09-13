 

namespace CheckIn.API.Models.ModelMain
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    public partial class LicUsuarios
    {
        [Key]
 
        public int idLogin { get; set; }

 
        [StringLength(20)]
        public string CedulaJuridica { get; set; }

        [StringLength(200)]
        public string Email { get; set; }
        [StringLength(100)]
        public string Nombre { get; set; }
        [StringLength(500)]
        public string Clave { get; set; }
        public bool Activo { get; set; }
    }
}