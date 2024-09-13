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
    public class GastosController: ApiController
    {
        ModelCliente db;
        G G = new G();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                G.AbrirConexionAPP(out db);
                var Gastos = db.Gastos.ToList();

                if (!string.IsNullOrEmpty(filtro.Texto))
                {
                    Gastos = Gastos.Where(a => a.Nombre.ToUpper().Contains(filtro.Texto.ToUpper())).ToList();
                }

                if(filtro.Codigo1 > 0)
                {
                    Gastos = Gastos.Where(a => a.idCuentaContable == filtro.Codigo1).ToList();
                }

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Gastos);

            }
            catch (Exception ex)
            {
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/Gastos/Consultar")]
        public HttpResponseMessage GetOne([FromUri]int id)
        {
            try
            {
                G.AbrirConexionAPP(out db);


                var Gasto = db.Gastos.Where(a => a.idTipoGasto == id).FirstOrDefault();


                if (Gasto == null )
                {
                    throw new Exception("Este gasto no se encuentra registrado");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Gasto);
            }
            catch (Exception ex)
            {
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        [HttpPost]
        public HttpResponseMessage Post([FromBody] Gastos gasto)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Gasto = db.Gastos.Where(a => a.idTipoGasto == gasto.idTipoGasto).FirstOrDefault();

                if (Gasto == null)
                {
                    Gasto = new Gastos();
                    Gasto.idCuentaContable = gasto.idCuentaContable;
                    Gasto.Nombre = gasto.Nombre;
                    Gasto.PalabrasClave = gasto.PalabrasClave;

                    db.Gastos.Add(Gasto);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Este gasto YA existe");
                }

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Gasto);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Insercion de Gastos";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPut]
        [Route("api/Gastos/Actualizar")]
        public HttpResponseMessage Put([FromBody] Gastos gasto)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Gasto = db.Gastos.Where(a => a.idTipoGasto == gasto.idTipoGasto).FirstOrDefault();

                if (Gasto != null)
                {
                    db.Entry(Gasto).State = EntityState.Modified;
                    Gasto.Nombre = gasto.Nombre;
                    Gasto.PalabrasClave = gasto.PalabrasClave;
                    Gasto.idCuentaContable = gasto.idCuentaContable;

                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Gasto no existe");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Gasto);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Actualizar Gastos";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpDelete]
        [Route("api/Gastos/Eliminar")]
        public HttpResponseMessage Delete([FromUri] int id)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Gastos = db.Gastos.Where(a => a.idTipoGasto == id).FirstOrDefault();

                if (Gastos != null)
                {


                    db.Gastos.Remove(Gastos);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Gasto no existe");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Eliminar Gasto";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

    }
}