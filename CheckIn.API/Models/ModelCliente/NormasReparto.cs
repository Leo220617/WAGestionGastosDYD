namespace CheckIn.API.Models.ModelCliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("NormasReparto")]
    public partial class NormasReparto
    {
        public int id { get; set; }

        public int idLogin { get; set; }

        [StringLength(4)]
        public string CodSAP { get; set; }

        [StringLength(100)]
        public string Nombre { get; set; }
        public int idDimension { get; set; }
    }
}
