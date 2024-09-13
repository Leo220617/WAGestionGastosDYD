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
    public class VCierreController: ApiController
    {
        ModelCliente db;
        G G = new G();

        public async Task<HttpResponseMessage> Get([FromUri] int idLogin, string Periodo, DateTime fechaCierre, string CodMoneda)
        {
            var resultado = true;
            try
            {
                G.AbrirConexionAPP(out db);

                fechaCierre = fechaCierre.Date;
                var Candado = db.EncCierre.Where(a => a.Periodo.ToUpper().Contains(Periodo.ToUpper()) && a.Estado != "A" && a.CodMoneda == CodMoneda && a.FechaInicial <= fechaCierre && a.FechaFinal >= fechaCierre && a.idLogin == idLogin).FirstOrDefault();
                if (Candado != null)
                {
                    resultado = false;
                }


                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, resultado);

            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Error de GET VCierre";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, resultado);
            }
        }
    }
}