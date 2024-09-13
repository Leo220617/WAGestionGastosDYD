using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CheckIn.API.Models
{
    public class DetCierreViewModel
    {
        [Key]
        public int id { get; set; }

        public int idCierre { get; set; }

        public int NumLinea { get; set; }

        public int idFactura { get; set; }
        public int idTipoGasto { get; set; }
        public string Comentario { get; set; }
    }
}