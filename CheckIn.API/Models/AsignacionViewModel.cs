using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckIn.API.Models
{
    public class AsignacionViewModel
    {
        public int idLogin { get; set; }
        public int idFac { get; set; }
        public int idNorma { get; set; } = 0;
    }
}