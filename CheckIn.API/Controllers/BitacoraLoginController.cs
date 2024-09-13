using CheckIn.API.Models;
using CheckIn.API.Models.ModelCliente;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace CheckIn.API.Controllers
{
    [Authorize]

    public class BitacoraLoginController: ApiController
    {
        ModelCliente db;
        G G = new G();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                G.AbrirConexionAPP(out db);
                DateTime time = new DateTime();


                if(filtro.FechaFinal != time)
                {
                    filtro.FechaFinal = filtro.FechaFinal.AddDays(1);
                }

                var BL = db.BitacoraLogin.Where(a => (filtro.Codigo1 != 0 ? a.idUsuario == filtro.Codigo1 : true) && (filtro.FechaInicio == time ? true : a.Fecha >= filtro.FechaInicio) && (filtro.FechaFinal == time ? true : a.Fecha <= filtro.FechaFinal)).ToList();

                 
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, BL);

            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Error de GET BITACORA LOGIN";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}