using CheckIn.API.Models.ModelCliente;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckIn.API.Models
{
    public class CierreViewModel
    {
        public EncCierre EncCierre { get; set; }
        public DetCierreViewModel[] DetCierre { get; set; }
    }
}