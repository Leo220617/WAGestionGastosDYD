using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckIn.API.Models
{
    public class InfoLiquid
    {
        public string emailDest { get; set; }
        public string emailCC { get; set; }
        public int idCierre { get; set; }
        public string body { get; set; }

    }
}