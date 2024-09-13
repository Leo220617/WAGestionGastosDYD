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
    public class RolesController: ApiController
    {
        ModelCliente db;
        G G = new G();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                G.AbrirConexionAPP(out db);
                var Roles = db.Roles.ToList();

                if(!string.IsNullOrEmpty(filtro.Texto))
                {
                    Roles = Roles.Where(a => a.NombreRol.ToUpper().Contains(filtro.Texto.ToUpper())).ToList();
                }
                

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Roles);

            }
            catch (Exception ex)
            {
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/Roles/Consultar")]
        public HttpResponseMessage GetOne([FromUri]int id)
        {
            try
            {
                G.AbrirConexionAPP(out db);


                var Rol = db.Roles.Where(a => a.idRol == id).FirstOrDefault();


                if (Rol == null)
                {
                    throw new Exception("Este rol no se encuentra registrado");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Rol);
            }
            catch (Exception ex)
            {
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage Post([FromBody] Roles rol)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Rol = db.Roles.Where(a => a.idRol == rol.idRol).FirstOrDefault();

                if (Rol == null)
                {
                    Rol = new Roles();
                    Rol.NombreRol = rol.NombreRol;
                  


                    db.Roles.Add(Rol);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Este rol  YA existe");
                }

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Rol);
            }
            catch (Exception ex)
            {
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPut]
        [Route("api/Roles/Actualizar")]
        public HttpResponseMessage Put([FromBody] Roles rol)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Rol = db.Roles.Where(a => a.idRol == rol.idRol).FirstOrDefault();

                if (Rol != null)
                {
                    db.Entry(Rol).State = EntityState.Modified;
                    Rol.NombreRol = rol.NombreRol;
                   
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Rol no existe");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Rol);
            }
            catch (Exception ex)
            {
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpDelete]
        [Route("api/Roles/Eliminar")]
        public HttpResponseMessage Delete([FromUri] int id)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Rol = db.Roles.Where(a => a.idRol == id).FirstOrDefault();

                if (Rol != null)
                {


                    db.Roles.Remove(Rol);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Rol no existe");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

    }
}