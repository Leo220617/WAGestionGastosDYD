using CheckIn.API.Models;
using CheckIn.API.Models.ModelCliente;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;



namespace CheckIn.API.Controllers
{
    [Authorize]
    public class SeguridadModulosController: ApiController
    {
        ModelCliente db;
        G G = new G();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                G.AbrirConexionAPP(out db);
                var modulos = db.SeguridadModulos.ToList();

                if (!string.IsNullOrEmpty(filtro.Texto))
                {
                    modulos = modulos.Where(a => a.Descripcion.ToUpper().Contains(filtro.Texto.ToUpper())).ToList();
                }



                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, modulos);

            }
            catch (Exception ex)
            {
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/SeguridadModulos/Consultar")]
        public HttpResponseMessage GetOne([FromUri]int id)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Rol = db.SeguridadModulos.Where(a => a.CodModulo == id).FirstOrDefault();
               
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Rol);
            }
            catch (Exception ex)
            {
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}