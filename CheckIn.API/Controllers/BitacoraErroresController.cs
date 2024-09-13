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
    public class BitacoraErroresController: ApiController
    {
        ModelCliente db;
        G G = new G();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                G.AbrirConexionAPP(out db);
                var time = DateTime.Now;
                var FecIni = time.AddDays(-5);
                var Errores = db.BitacoraErrores.Where(a => a.Fecha >= FecIni && a.Fecha <= time ).ToList();

               

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Errores);

            }
            catch (Exception ex)
            {
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}