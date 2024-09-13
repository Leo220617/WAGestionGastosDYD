namespace CheckIn.API.Models.ModelCliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Gastos
    {
        [Key]
        public int idTipoGasto { get; set; }

        public int idCuentaContable { get; set; }

        [StringLength(50)]
        public string Nombre { get; set; }

        public string PalabrasClave { get; set; }
    }
}
