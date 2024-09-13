using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckIn.API.Models
{
    public class LoginViewModel
    {
   

        public int id { get; set; }


       
        public string CedulaJuridica { get; set; }

        public int idRol { get; set; }
        public string Email { get; set; }
    
        public string Nombre { get; set; }
    
        public string Clave { get; set; }
        public bool Activo { get; set; }
        public int idLoginAceptacion { get; set; }
        public string CardCode { get; set; }
    }
}