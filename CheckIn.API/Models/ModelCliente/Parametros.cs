namespace CheckIn.API.Models.ModelCliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Parametros
    {
        public int id { get; set; }

        [StringLength(500)]
        public string RecepcionEmail { get; set; }

        [StringLength(500)]
        public string RecepcionPassword { get; set; }

        [StringLength(50)]
        public string RecepcionHostName { get; set; }

        public bool? RecepcionUseSSL { get; set; }

        public int RecepcionPort { get; set; }

        public DateTime? RecepcionUltimaLecturaImap { get; set; }
        public string UrlSitioPublicado { get; set; }
        public int EnvioPort { get; set; }
        public string UrlImagenesApp { get; set; }
        public string UrlLogo { get; set; }
        public string CI1 { get; set; }
        public string CI2 { get; set; }
        public string CI4 { get; set; }
        public string CI8 { get; set; }
        public string CI13 { get; set; }
        public string IMPEX { get; set; }
        public int Mes { get; set; }
        public bool SetearManual { get; set; }
        public string IMP0 { get; set; }
        public string IMP1 { get; set; }
        public string IMP2 { get; set; }
        public string IMP4 { get; set; }
        public string IMP8 { get; set; }
        public string IMP13 { get; set; }
        public int DiasVencimiento { get; set; }
        public bool serviceLayer { get; set; }
        public bool es365 { get; set; }
        public int SerieCliente { get; set; }
        public int GrupoCliente { get; set; }
        public string SQLValidacionProveedor { get; set; }
    }
}
