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
    public class DimensionesController: ApiController
    {
        ModelCliente db;
        G G = new G();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                G.AbrirConexionAPP(out db);
                var Dimension = db.Dimensiones.ToList();

                if (!string.IsNullOrEmpty(filtro.Texto))
                {
                    Dimension = Dimension.Where(a => a.Nombre.ToUpper().Contains(filtro.Texto.ToUpper())).ToList();
                }

               

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Dimension);

            }
            catch (Exception ex)
            {
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/Dimensiones/Consultar")]
        public HttpResponseMessage GetOne([FromUri]int id)
        {
            try
            {
                G.AbrirConexionAPP(out db);


                var Dimensiones = db.Dimensiones.Where(a => a.id == id).FirstOrDefault();


                if (Dimensiones == null)
                {
                    throw new Exception("Esta dimensión no se encuentra registrada");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Dimensiones);
            }
            catch (Exception ex)
            {
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage Post([FromBody] Dimensiones dimension)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Dimension = db.Dimensiones.Where(a => a.id == dimension.id).FirstOrDefault();

                if (Dimension == null)
                {
                    Dimension = new Dimensiones();
                    Dimension.codigoSAP = dimension.codigoSAP;
                    Dimension.Nombre = dimension.Nombre;
                     

                    db.Dimensiones.Add(Dimension);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Esta dimensión YA existe");
                }

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Dimension);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Insertar Dimension";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPut]
        [Route("api/Dimensiones/Actualizar")]
        public HttpResponseMessage Put([FromBody] Dimensiones dimension)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Dimension = db.Dimensiones.Where(a => a.id == dimension.id).FirstOrDefault();

                if (Dimension != null)
                {
                    db.Entry(Dimension).State = EntityState.Modified;
                    Dimension.codigoSAP = dimension.codigoSAP;
                    Dimension.Nombre = dimension.Nombre;
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Dimension no existe");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Dimension);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Actualizar Dimension";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpDelete]
        [Route("api/Dimensiones/Eliminar")]
        public HttpResponseMessage Delete([FromUri] int id)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Dimension = db.Dimensiones.Where(a => a.id == id).FirstOrDefault();

                if (Dimension != null)
                {


                    db.Dimensiones.Remove(Dimension);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Dimension no existe");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Eliminar Dimension";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


    }
}