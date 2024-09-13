using CheckIn.API.Models;
using CheckIn.API.Models.ModelCliente;
using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using PdfSharp;
using PdfSharp.Pdf;
using S22.Imap;
using SelectPdf;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Xml.Linq;
using TheArtOfDev.HtmlRenderer.PdfSharp;

namespace CheckIn.API.Controllers
{
    [Authorize]
    public class ProveedoresController : ApiController
    {
        ModelCliente db;
        G G = new G();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Proveedores = db.Proveedores.ToList();

                if(!string.IsNullOrEmpty(filtro.Texto))
                {

                 Proveedores = Proveedores.Where(a => a.RUC.Replace("-", "").Replace("-", "") == filtro.Texto.Replace("-", "").Replace("-", "")).ToList();
                }
                
                if(!string.IsNullOrEmpty(filtro.Texto2))
                {
                    Proveedores = Proveedores.Where(a =>  a.DV == filtro.Texto2).ToList();

                }

                if(!string.IsNullOrEmpty(filtro.Texto3))
                {
                    Proveedores = Proveedores.Where(a => a.Nombre.ToUpper().Contains(filtro.Texto3.ToUpper())).ToList();
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Proveedores);

            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "GET ALL PROVEEDOR";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/Proveedores/Consultar")]
        public HttpResponseMessage GetOne([FromUri]int id)
        {
            try
            {
                G.AbrirConexionAPP(out db);


                var Proveedor = db.Proveedores.Where(a => a.id == id).FirstOrDefault();


                if (Proveedor == null)
                {
                    throw new Exception("Esta Proveedor no se encuentra registrada");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Proveedor);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "GET ONE PROVEEDOR";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage Post([FromBody] Proveedores proveedor)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Proveedor = db.Proveedores.Where(a => a.id == proveedor.id).FirstOrDefault();

                if (Proveedor == null)
                {
                    Proveedor = new Proveedores();
                    Proveedor.RUC = proveedor.RUC;
                    Proveedor.DV = proveedor.DV;
                    Proveedor.Nombre = proveedor.Nombre;

                    db.Proveedores.Add(Proveedor);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Esta Proveedor YA existe");
                }

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Proveedor);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Insercion de Proveedor";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPut]
        [Route("api/Proveedores/Actualizar")]
        public HttpResponseMessage Put([FromBody] Proveedores proveedor)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Proveedor = db.Proveedores.Where(a => a.id == proveedor.id).FirstOrDefault();

                if (Proveedor != null)
                {
                    db.Entry(Proveedor).State = EntityState.Modified;
                    Proveedor.RUC = proveedor.RUC;
                    Proveedor.DV = proveedor.DV;
                    Proveedor.Nombre = proveedor.Nombre;
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Proveedor no existe");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Proveedor);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Actualizar Proveedor";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpDelete]
        [Route("api/Proveedores/Eliminar")]
        public HttpResponseMessage Delete([FromUri] int id)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Proveedor = db.Proveedores.Where(a => a.id == id).FirstOrDefault();

                if (Proveedor != null)
                {


                    db.Proveedores.Remove(Proveedor);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Proveedor no existe");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Eliminar Proveedor";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}