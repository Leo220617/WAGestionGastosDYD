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
    public class CierreController : ApiController
    {
        ModelCliente db;
        G G = new G();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                G.AbrirConexionAPP(out db);
                DateTime time = new DateTime();
             

                var EncCierre = db.EncCierre.Select(a => new
                {

                    a.idCierre,
                    a.idLogin,
                    NombreUsuario = db.Login.Where(d => d.id == a.idLogin).FirstOrDefault() == null ? "" : db.Login.Where(d => d.id == a.idLogin).FirstOrDefault().Nombre,
                    a.Periodo,
                    a.FechaCierre,
                    a.FechaInicial,
                    a.FechaFinal,
                    a.SubTotal,
                    a.Descuento,
                    a.Impuestos,
                    a.Impuesto1,
                    a.Impuesto2,
                    a.Impuesto4,
                    a.Impuesto8,
                    a.Impuesto13,
                    a.Total,
                    a.Estado,
                    a.Observacion,
                    a.idLoginAceptacion,
                    a.CodMoneda,
                    a.TotalOtrosCargos,
                    a.ProcesadaSAP,
                    Detalle = db.DetCierre.Where(d => d.idCierre == a.idCierre).Select(s => new
                    {
                        s.id,
                        s.idCierre,
                        s.NumLinea,
            

                    }).ToList()



                }).Where(a => (filtro.FechaInicio != time ? a.FechaCierre >= filtro.FechaInicio : true)).ToList();

                if (!string.IsNullOrEmpty(filtro.Texto)) //Busca por el periodo 
                {
                    EncCierre = EncCierre.Where(a => a.Periodo.ToLower().Contains(filtro.Texto.ToLower())).ToList();
                }


                if (filtro.FechaInicio != time) // Busca por un rango de fechas
                {
                    filtro.FechaFinal = filtro.FechaFinal.AddDays(1);
                    EncCierre = EncCierre.Where(a => a.FechaCierre >= filtro.FechaInicio && a.FechaCierre <= filtro.FechaFinal).ToList();
                }

                if (!string.IsNullOrEmpty(filtro.Estado) && filtro.Estado != "NULL") // Busca por estado
                {
                    EncCierre = EncCierre.Where(a => a.Estado == filtro.Estado).ToList();
                }

                if (filtro.Codigo1 > 0) //Busca las facturas que fueron creadas por el liquidador
                {
                    EncCierre = EncCierre.Where(a => a.idLogin == filtro.Codigo1).ToList();
                }

                if (filtro.Codigo2 > 0) //Si El codigo 2 > 0 y ademas el codigo 1 viene en 0 entonces busca las liquidaciones que yo acepte o que yo haya hecho
                {

                    if (filtro.Codigo1 == 0)
                    {

                        EncCierre = EncCierre.Where(a => a.idLoginAceptacion == filtro.Codigo2 || a.idLogin == filtro.Codigo2).ToList();
                    }


                }

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, EncCierre);

            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Error de GET Cierre";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }




        [Route("api/Cierre/Consultar")]
        public HttpResponseMessage GetOne([FromUri]int id)
        {
            try
            {
                G.AbrirConexionAPP(out db);


                var EncCierre = db.EncCierre.Where(a => a.idCierre == id).FirstOrDefault();
                var DetCierre = db.DetCierre.Where(a => a.idCierre == id).ToList();

                var resp = new
                {
                    EncCierre,
                    DetCierre
                };



                if (resp == null)
                {
                    throw new Exception("Esta liquidación no se encuentra registrada");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, resp);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Error de GET Cierre";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/Cierre/EnviarCorreo")]
        public HttpResponseMessage GetEnviarCorreo([FromBody]InfoLiquid item)
        {
            try
            {
                G.AbrirConexionAPP(out db);
                List<Attachment> adjuntos = new List<Attachment>();
   
                SendGridEmail.EmailSender emailsender = new SendGridEmail.EmailSender();
                var parametros = db.Parametros.FirstOrDefault();

                var Cierre = db.DetCierre.Where(a => a.idCierre == item.idCierre).ToList();


                foreach (var det in Cierre)
                {
                    var Compra = db.EncCompras.Where(a => a.id == det.idFactura).FirstOrDefault();
                    if (Compra.PdfFac != null)
                    {
                        if (Compra.ImagenB64 != null)
                        {
                            Attachment att = new Attachment(new MemoryStream(Compra.PdfFac), Compra.PdfFactura + ".png");
                            adjuntos.Add(att);
                        }
                        else
                        {
                            Attachment att = new Attachment(new MemoryStream(Compra.PdfFac), Compra.PdfFactura);
                            adjuntos.Add(att);
                        }

                    }

                }

                var bodyH = item.body;
                HtmlToPdf converter = new HtmlToPdf();

           
                converter.Options.PdfPageSize = PdfPageSize.A4;
                converter.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
                converter.Options.MarginLeft = 5;
                converter.Options.MarginRight = 5;
           
                SelectPdf.PdfDocument doc = converter.ConvertHtmlString(bodyH);

                var bytes = doc.Save();
                doc.Close();

                Attachment att3 = new Attachment(new MemoryStream(bytes), "Liquidacion.pdf");
                adjuntos.Add(att3);





                var resp = emailsender.SendV2(item.emailDest, item.emailCC, "", parametros.RecepcionEmail, "Liquidación", "Liquidación por revisar", item.body, parametros.RecepcionHostName, parametros.EnvioPort, parametros.RecepcionUseSSL.Value, parametros.RecepcionEmail, parametros.RecepcionPassword, adjuntos);

                if (!resp)
                {
                    throw new Exception("No se ha podido enviar el correo con la liquidación");
                }

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {


                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Error de Correo";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();

                G.GuardarTxt("ErrorCorreo.txt", ex.Message + " -> " + ex.StackTrace);
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }



        [Route("api/Cierre/Estado")]
        public HttpResponseMessage GetEstado([FromUri]int id, string Estado, string comentario = "", int idLoginAceptacion = 0)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var EncCierre = db.EncCierre.Where(a => a.idCierre == id).FirstOrDefault();

                db.Entry(EncCierre).State = EntityState.Modified;

                EncCierre.Estado = Estado;
                if (!string.IsNullOrEmpty(comentario))
                {

                    EncCierre.Observacion = comentario;
                }

                if (Estado == "E")
                {
                    SendGridEmail.EmailSender emailsender = new SendGridEmail.EmailSender();
                    var Roles = db.Roles.Where(a => a.NombreRol.ToUpper().Contains("APROBADOR")).FirstOrDefault();

                    var Login = db.Login.Where(a => a.idRol == Roles.idRol).ToList();
                    var AsignadoCierre = db.Login.Where(a => a.id == EncCierre.idLogin).FirstOrDefault();
                    var parametros = db.Parametros.FirstOrDefault();

                    var html = "<h3 style='text-align: center; '><strong>Liquidaci&oacute;n pendiente de revisi&oacute;n</strong></h3>";
                    html += "<p style='text-align: justify; '>Se ha recibido una nueva liquidaci&oacute;n de gastos, a continuaci&oacute;n los detalles:</p>";
                    html += "<ul>";
                    html += "<li style = 'text-align: justify;' ><strong> ID Cierre </strong>: " + EncCierre.idCierre + "</li>";
                    html += "<li style = 'text-align: justify;' ><strong> Nombre </strong>: " + AsignadoCierre.Nombre + "</li>";
                    html += "<li style='text-align: justify; '><strong>Periodo</strong>: " + EncCierre.Periodo + "</li>";
                    html += "<li style='text-align: justify; '><strong>Cantidad de Facturas: </strong>" + EncCierre.CantidadRegistros + "</li>";
                    html += "<li style='text-align: justify; '><strong>Total</strong>: " + decimal.Round(EncCierre.Total.Value, 2) + "</li>";
                    html += "</ul><p></p> ";
                    html += "<p>Favor revisar en la plataforma <a href='" + parametros.UrlSitioPublicado + "'>" + parametros.UrlSitioPublicado + "</a>&nbsp;para aceptar o denegar dicha liquidaci&oacute;n.</p>";

                    foreach (var item in Login)
                    {


                        emailsender.SendV2(item.Email, parametros.RecepcionEmail, "", parametros.RecepcionEmail, "Liquidación", "Liquidación pendiente de revisión", html, parametros.RecepcionHostName, parametros.EnvioPort, parametros.RecepcionUseSSL.Value, parametros.RecepcionEmail, parametros.RecepcionPassword);
                    }

                    try
                    {
                        BitacoraCierres bc = new BitacoraCierres();
                        bc.idUsuarioEnviador = EncCierre.idLogin;
                        bc.idUsuarioAceptador = EncCierre.idLoginAceptacion.Value;
                        bc.idCierre = EncCierre.idCierre;
                        bc.IP = HttpContext.Current.Request.UserHostAddress;
                        bc.Detalle = "Se manda a aprobacion la liquidacion # " + EncCierre.idCierre + ", el usuario: " + AsignadoCierre.Nombre + ", con el id: " + AsignadoCierre.id;
                        bc.Fecha = DateTime.Now;
                        db.BitacoraCierres.Add(bc);
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        BitacoraErrores be = new BitacoraErrores();
                        be.Descripcion = ex.Message;
                        be.StackTrace = ex.StackTrace;
                        be.Metodo = "Cambio de estado";
                        be.Fecha = DateTime.Now;
                        db.BitacoraErrores.Add(be);
                        db.SaveChanges();
                    }

                }


                if (Estado == "A" || Estado == "R")
                {
                    SendGridEmail.EmailSender emailsender = new SendGridEmail.EmailSender();
         
                    var login = db.Login.Where(a => a.id == EncCierre.idLogin).FirstOrDefault();
                    var AR = Estado;

                    var Login = db.Login.Where(a => a.id == login.id).FirstOrDefault();
                    var AsignadoCierre = db.Login.Where(a => a.id == EncCierre.idLogin).FirstOrDefault();
                    var parametros = db.Parametros.FirstOrDefault();
               
                    var html = "";
                    if (AR == "A")
                    {
                        var LoginAceptacion = db.Login.Where(a => a.id == EncCierre.idLoginAceptacion).FirstOrDefault();
                        html = "<h3 style='text-align: center; '><strong>Liquidaci&oacute;n  Aprobada</strong></h3>";
                        html += "<p style='text-align: justify; '>Se ha aprobado tú liquidaci&oacute;n de gastos, a continuaci&oacute;n los detalles:</p>";
                        html += "<ul>";
                        html += "<li style = 'text-align: justify;' ><strong> ID Cierre </strong>: " + EncCierre.idCierre + "</li>";
                        html += "<li style = 'text-align: justify;' ><strong> Nombre </strong>: " + AsignadoCierre.Nombre + "</li>";
                        html += "<li style='text-align: justify; '><strong>Periodo</strong>: " + EncCierre.Periodo + "</li>";
                        html += "<li style='text-align: justify; '><strong>Cantidad de Facturas: </strong>" + EncCierre.CantidadRegistros + "</li>";
                        html += "<li style='text-align: justify; '><strong>Total</strong>: " + decimal.Round(EncCierre.Total.Value, 2) + "</li>";
                        html += "<li style='text-align: justify; '><strong>Usuario Aprobador</strong>: " + (LoginAceptacion == null ? "" : LoginAceptacion.Nombre) + "</li>";
                        html += "<li style='text-align: justify; '><strong>Comentarios de la Liquidación</strong>: " + EncCierre.Observacion + "</li>";
                        html += "</ul><p></p> ";
                        html += "<p>Favor revisar en la plataforma <a href='" + parametros.UrlSitioPublicado + "'>" + parametros.UrlSitioPublicado + "</a>&nbsp;para ver más detalles de dicha liquidaci&oacute;n.</p>";

                        try
                        {
                            var LoginBitacora = db.Login.Where(a => a.id == idLoginAceptacion).FirstOrDefault();
                            BitacoraCierres bc = new BitacoraCierres();
                            bc.idUsuarioEnviador = EncCierre.idLogin;
                            bc.idUsuarioAceptador = EncCierre.idLoginAceptacion.Value;
                            bc.idCierre = EncCierre.idCierre;
                            bc.IP = HttpContext.Current.Request.UserHostAddress;
                            bc.Detalle = "Se ha aprobado la liquidacion # " + EncCierre.idCierre + ", del usuario: " + AsignadoCierre.Nombre + ", con el id: " + AsignadoCierre.id + ", lo aprobo el usuario: " + (LoginBitacora == null ? LoginAceptacion.Nombre : LoginBitacora.Nombre ) + ", con el id: " + idLoginAceptacion;
                            bc.Fecha = DateTime.Now;
                            db.BitacoraCierres.Add(bc);
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            BitacoraErrores be = new BitacoraErrores();
                            be.Descripcion = ex.Message;
                            be.StackTrace = ex.StackTrace;
                            be.Metodo = "Cambio de estado";
                            be.Fecha = DateTime.Now;
                            db.BitacoraErrores.Add(be);
                            db.SaveChanges();
                        }

                    }
                    else
                    {
                        var LoginBitacora = db.Login.Where(a => a.id == idLoginAceptacion).FirstOrDefault();
                        var LoginAceptacion = db.Login.Where(a => a.id == EncCierre.idLoginAceptacion).FirstOrDefault();
                        html = "<h3 style='text-align: center; '><strong>Liquidaci&oacute;n  Rechazada</strong></h3>";
                        html += "<p style='text-align: justify; '>Se ha rechazado tú liquidaci&oacute;n de gastos, a continuaci&oacute;n los detalles:</p>";
                        html += "<ul>";
                        html += "<li style = 'text-align: justify;' ><strong> ID Cierre </strong>: " + EncCierre.idCierre + "</li>";
                        html += "<li style = 'text-align: justify;' ><strong> Nombre </strong>: " + AsignadoCierre.Nombre + "</li>";
                        html += "<li style='text-align: justify; '><strong>Periodo</strong>: " + EncCierre.Periodo + "</li>";
                        html += "<li style='text-align: justify; '><strong>Cantidad de Facturas: </strong>" + EncCierre.CantidadRegistros + "</li>";
                        html += "<li style='text-align: justify; '><strong>Total</strong>: " + decimal.Round(EncCierre.Total.Value, 2) + "</li>";
                        html += "<li style='text-align: justify; '><strong>Usuario Aprobador</strong>: " + (LoginAceptacion == null ? "" : LoginAceptacion.Nombre) + "</li>";
                        html += "<li style='text-align: justify; '><strong>Comentarios de la Liquidación</strong>: " + EncCierre.Observacion + "</li>";
                        html += "</ul><p></p> ";
                        html += "<p>Favor revisar en la plataforma <a href='" + parametros.UrlSitioPublicado + "'>" + parametros.UrlSitioPublicado + "</a>&nbsp;para ver más detalles de dicha liquidaci&oacute;n.</p>";

                        try
                        {
                            BitacoraCierres bc = new BitacoraCierres();
                            bc.idUsuarioEnviador = EncCierre.idLogin;
                            bc.idUsuarioAceptador = EncCierre.idLoginAceptacion.Value;
                            bc.idCierre = EncCierre.idCierre;
                            bc.IP = HttpContext.Current.Request.UserHostAddress;
                            bc.Detalle = "Se ha rechazado la liquidacion # " + EncCierre.idCierre + ", del usuario: " + AsignadoCierre.Nombre + ", con el id: " + AsignadoCierre.id + ", lo rechazo el usuario: " + (LoginBitacora == null ? LoginAceptacion.Nombre : LoginBitacora.Nombre) + ", con el id: " + idLoginAceptacion;
                            bc.Fecha = DateTime.Now;
                            db.BitacoraCierres.Add(bc);
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            BitacoraErrores be = new BitacoraErrores();
                            be.Descripcion = ex.Message;
                            be.StackTrace = ex.StackTrace;
                            be.Metodo = "Cambio de estado";
                            be.Fecha = DateTime.Now;
                            db.BitacoraErrores.Add(be);
                            db.SaveChanges();
                        }


                    }




                    emailsender.SendV2(Login.Email, parametros.RecepcionEmail, "", parametros.RecepcionEmail, "Liquidación", "Respuesta de la liquidación", html, parametros.RecepcionHostName, parametros.EnvioPort, parametros.RecepcionUseSSL.Value, parametros.RecepcionEmail, parametros.RecepcionPassword);



                }



         

                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {

                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Cambio de estado";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();


                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        [HttpPost]
        public HttpResponseMessage Post([FromBody] CierreViewModel gastos)
        {

            G.AbrirConexionAPP(out db);
            var t = db.Database.BeginTransaction();
            try
            {
                gastos.EncCierre.FechaCierre = DateTime.Now.Date;
                var Candado = db.EncCierre.Where(a => a.Periodo.ToUpper().Trim().Contains(gastos.EncCierre.Periodo.ToUpper().Trim()) && a.Estado != "A" && a.CodMoneda.Trim() == gastos.EncCierre.CodMoneda.Trim() && a.FechaInicial <= gastos.EncCierre.FechaCierre && a.FechaFinal >= gastos.EncCierre.FechaCierre && a.idLogin == gastos.EncCierre.idLogin).FirstOrDefault();
                if (Candado != null)
                {
                    throw new Exception("Ya existe una liquidacion con la moneda " + gastos.EncCierre.CodMoneda + " en este periodo " + gastos.EncCierre.Periodo + " idlogin: " + gastos.EncCierre.idLogin);
                }


                if (gastos.DetCierre.Count() == 0)
                {
                    throw new Exception("No se puede insertar una liquidacion con ninguna factura");
                }
                var Cierre = new EncCierre();
                Cierre.Periodo = gastos.EncCierre.Periodo;
                Cierre.FechaInicial = gastos.EncCierre.FechaInicial;
                Cierre.FechaFinal = gastos.EncCierre.FechaFinal;
                Cierre.idLogin = gastos.EncCierre.idLogin;
                Cierre.SubTotal = gastos.EncCierre.SubTotal;
                Cierre.Total = gastos.EncCierre.Total;
                Cierre.CantidadRegistros = 0;
                Cierre.Descuento = gastos.EncCierre.Descuento;
                Cierre.FechaCierre = DateTime.Now;
                Cierre.Impuestos = gastos.EncCierre.Impuestos;
                Cierre.Impuesto1 = gastos.EncCierre.Impuesto1;
                Cierre.Impuesto2 = gastos.EncCierre.Impuesto2;
                Cierre.Impuesto4 = gastos.EncCierre.Impuesto4;
                Cierre.Impuesto8 = gastos.EncCierre.Impuesto8;
                Cierre.Impuesto13 = gastos.EncCierre.Impuesto13;
                var login = db.Login.Where(a => a.id == Cierre.idLogin).FirstOrDefault();
                Cierre.idLoginAceptacion = login.idLoginAceptacion;
                Cierre.Estado = gastos.EncCierre.Estado;
                Cierre.Observacion = gastos.EncCierre.Observacion;
                Cierre.CodMoneda = gastos.EncCierre.CodMoneda;
                Cierre.TotalOtrosCargos = gastos.EncCierre.TotalOtrosCargos;
                Cierre.ProcesadaSAP = false;
                Cierre.Observacion = gastos.EncCierre.Observacion;

                db.EncCierre.Add(Cierre);
                db.SaveChanges();

                var FecIni = Cierre.FechaInicial.AddDays(-1);
                var FecFin = Cierre.FechaFinal.AddDays(1);

               // var Facturas = db.EncCompras.Where(a => a.FecFactura >= FecIni && a.FecFactura <= FecFin).ToList();
                var Logins = db.Login.ToList();
                var Normas = db.NormasReparto.ToList();



                int i = 1;
                foreach (var item in gastos.DetCierre)
                {
                    var DetalleExistente = db.DetCierre.Where(a => a.idFactura == item.idFactura).FirstOrDefault();
                    if (DetalleExistente != null)
                    {
                        throw new Exception("Esta factura " + (db.EncCompras.Where(a => a.id == item.idFactura).FirstOrDefault() == null ? "0" : db.EncCompras.Where(a => a.id == item.idFactura).FirstOrDefault().ConsecutivoHacienda) + " ya ha sido asignada por otro usuario");
                    }
                    DetCierre det = new DetCierre();
                    det.idCierre = Cierre.idCierre;
                    det.NumLinea = i;
                    det.idFactura = item.idFactura;
                    det.Comentario = item.Comentario;
                    i++;
                    db.DetCierre.Add(det);
                    var Factura = db.EncCompras.Where(a => a.id == item.idFactura).FirstOrDefault();
                    if (Normas.Where(a => a.idLogin == Cierre.idLogin).FirstOrDefault() == null)
                    {
                        throw new Exception("Este usuario " + login.Nombre + "  no contiene una norma de reparto asignada");
                    }
                    db.Entry(Factura).State = EntityState.Modified;
                    Factura.idLoginAsignado = Cierre.idLogin;
                    Factura.FecAsignado = DateTime.Now;



                    Factura.idNormaReparto = Normas.Where(a => a.idLogin == Cierre.idLogin).FirstOrDefault().id;
                    Factura.idCierre = det.idCierre;
                    Factura.idTipoGasto = item.idTipoGasto;
                    Factura.Comentario = Factura.Comentario != item.Comentario ? item.Comentario : Factura.Comentario;
                    db.SaveChanges();


                }
                db.SaveChanges();

                db.Entry(Cierre).State = EntityState.Modified;
                Cierre.CantidadRegistros = i - 1;
                db.SaveChanges();


                try
                {
                    BitacoraCierres bc = new BitacoraCierres();
                    bc.idUsuarioEnviador = Cierre.idLogin;
                    bc.idUsuarioAceptador = Cierre.idLoginAceptacion.Value;
                    bc.idCierre = Cierre.idCierre;
                    bc.IP = HttpContext.Current.Request.UserHostAddress;
                    bc.Detalle = "Se  ha creado la liquidacion # " + Cierre.idCierre + ", el usuario: " + login.Nombre + ", con el id: " + login.id;
                    bc.Fecha = DateTime.Now;
                    db.BitacoraCierres.Add(bc);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    BitacoraErrores be = new BitacoraErrores();
                    be.Descripcion = ex.Message;
                    be.StackTrace = ex.StackTrace;
                    be.Metodo = "Cambio de estado";
                    be.Fecha = DateTime.Now;
                    db.BitacoraErrores.Add(be);
                    db.SaveChanges();
                }


                if (gastos.EncCierre.Estado == "E")
                {
                    SendGridEmail.EmailSender emailsender = new SendGridEmail.EmailSender();
        

                    var Login = db.Login.Where(a => a.id == login.idLoginAceptacion).FirstOrDefault();
                    var AsignadoCierre = db.Login.Where(a => a.id == Cierre.idLogin).FirstOrDefault();
                    var parametros = db.Parametros.FirstOrDefault();

                    var html = "<h3 style='text-align: center; '><strong>Liquidaci&oacute;n pendiente de revisi&oacute;n</strong></h3>";
                    html += "<p style='text-align: justify; '>Se ha recibido una nueva liquidaci&oacute;n de gastos, a continuaci&oacute;n los detalles:</p>";
                    html += "<ul>";
                    html += "<li style = 'text-align: justify;' ><strong> ID Cierre </strong>: " + Cierre.idCierre + "</li>";
                    html += "<li style = 'text-align: justify;' ><strong> Nombre </strong>: " + AsignadoCierre.Nombre + "</li>";
                    html += "<li style='text-align: justify; '><strong>Periodo</strong>: " + Cierre.Periodo + "</li>";
                    html += "<li style='text-align: justify; '><strong>Cantidad de Facturas: </strong>" + Cierre.CantidadRegistros + "</li>";
                    html += "<li style='text-align: justify; '><strong>Total</strong>: " + decimal.Round(Cierre.Total.Value, 2) + "</li>";
                    html += "</ul><p></p> ";
                    html += "<p>Favor revisar en la plataforma <a href='" + parametros.UrlSitioPublicado + "'>" + parametros.UrlSitioPublicado + "</a>&nbsp;para aceptar o denegar dicha liquidaci&oacute;n.</p>";




                    emailsender.SendV2(Login.Email, parametros.RecepcionEmail, "", parametros.RecepcionEmail, "Liquidación", "Liquidación pendiente de revisión", html, parametros.RecepcionHostName, parametros.EnvioPort, parametros.RecepcionUseSSL.Value, parametros.RecepcionEmail, parametros.RecepcionPassword);

                    try
                    {
                        BitacoraCierres bc = new BitacoraCierres();
                        bc.idUsuarioEnviador = Cierre.idLogin;
                        bc.idUsuarioAceptador = Cierre.idLoginAceptacion.Value;
                        bc.idCierre = Cierre.idCierre;
                        bc.IP = HttpContext.Current.Request.UserHostAddress;
                        bc.Detalle = "Se manda a aprobacion la liquidacion # " + Cierre.idCierre + ", el usuario: " + AsignadoCierre.Nombre + ", con el id: " + AsignadoCierre.id;
                        bc.Fecha = DateTime.Now;
                        db.BitacoraCierres.Add(bc);
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        BitacoraErrores be = new BitacoraErrores();
                        be.Descripcion = ex.Message;
                        be.StackTrace = ex.StackTrace;
                        be.Metodo = "Cambio de estado";
                        be.Fecha = DateTime.Now;
                        db.BitacoraErrores.Add(be);
                        db.SaveChanges();
                    }

                }





                t.Commit();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {

                t.Rollback();
                G.GuardarTxt("ErrorCierre" + DateTime.Now.Day + "" + DateTime.Now.Month + "" + DateTime.Now.Year + ".txt", ex.ToString());

                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Insercion de Cierre";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, be);
            }
        }

        [HttpPut]
        [Route("api/Cierre/Actualizar")]
        public HttpResponseMessage Put([FromBody] CierreViewModel gastos)
        {

            G.AbrirConexionAPP(out db);
            var t = db.Database.BeginTransaction();
            try
            {


                if (db.EncCierre.Where(a => a.idCierre == gastos.EncCierre.idCierre).FirstOrDefault() != null)
                {




                    var Cierre = db.EncCierre.Where(a => a.idCierre == gastos.EncCierre.idCierre).FirstOrDefault();


                    db.Entry(Cierre).State = EntityState.Modified;
                    Cierre.Periodo = Cierre.Periodo;
                    Cierre.FechaInicial = gastos.EncCierre.FechaInicial;
                    Cierre.FechaFinal = gastos.EncCierre.FechaFinal;
                    Cierre.idLogin = gastos.EncCierre.idLogin;
                    Cierre.SubTotal = gastos.EncCierre.SubTotal;
                    Cierre.Total = gastos.EncCierre.Total;
                    Cierre.CantidadRegistros = 0;
                    Cierre.Descuento = gastos.EncCierre.Descuento;
                    Cierre.FechaCierre = Cierre.FechaCierre;
                    Cierre.Impuestos = gastos.EncCierre.Impuestos;
                    Cierre.Impuesto1 = gastos.EncCierre.Impuesto1;
                    Cierre.Impuesto2 = gastos.EncCierre.Impuesto2;
                    Cierre.Impuesto4 = gastos.EncCierre.Impuesto4;
                    Cierre.Impuesto8 = gastos.EncCierre.Impuesto8;
                    Cierre.Impuesto13 = gastos.EncCierre.Impuesto13;
                    var login = db.Login.Where(a => a.id == Cierre.idLogin).FirstOrDefault();
                    Cierre.idLoginAceptacion = login.idLoginAceptacion;
                    Cierre.Estado = gastos.EncCierre.Estado;
                    Cierre.Observacion = Cierre.Observacion;
                    Cierre.CodMoneda = gastos.EncCierre.CodMoneda;
                    Cierre.TotalOtrosCargos = gastos.EncCierre.TotalOtrosCargos;
                    Cierre.Observacion = gastos.EncCierre.Observacion;
                    db.SaveChanges();

                    var FecInicial = Cierre.FechaInicial.AddMonths(-1);
                    var FechaFinal = Cierre.FechaFinal.AddMonths(1);
                    //var Facturas = db.EncCompras.Where(a => a.FecFactura >= FecInicial && a.FecFactura <= FechaFinal && a.CodMoneda == Cierre.CodMoneda && (a.idLoginAsignado == 0 || a.idLoginAsignado == Cierre.idLogin || a.idLoginAsignado == null) && (a.idCierre == 0 || a.idCierre == gastos.EncCierre.idCierre || a.idCierre == null)).ToList();
                    var Logins = db.Login.ToList();
                    var Normas = db.NormasReparto.ToList();


                    var Detalle = db.DetCierre.Where(a => a.idCierre == Cierre.idCierre).ToList();

                    foreach (var item in Detalle)
                    {
                        var Factura = db.EncCompras.Where(a => a.id == item.idFactura).FirstOrDefault();
                        db.Entry(Factura).State = EntityState.Modified;
                        Factura.idLoginAsignado = 0;
                        Factura.FecAsignado = null; 
                        Factura.idNormaReparto = 0;
                        Factura.idCierre = 0;

                        db.DetCierre.Remove(item);
                        db.SaveChanges();
                    }




                    int i = 1;
                    foreach (var item in gastos.DetCierre)
                    {

                        var DetalleExistente = db.DetCierre.Where(a => a.idFactura == item.idFactura).FirstOrDefault();
                        if (DetalleExistente != null)
                        {
                            throw new Exception("Esta factura " + (db.EncCompras.Where(a => a.id == item.idFactura).FirstOrDefault() == null ? "0" : db.EncCompras.Where(a => a.id == item.idFactura).FirstOrDefault().ConsecutivoHacienda) +" ya ha sido asignada por otro usuario");
                        }


                        DetCierre det = new DetCierre();
                        det.idCierre = Cierre.idCierre;
                        det.NumLinea = i;
                        det.idFactura = item.idFactura;
                        det.Comentario = item.Comentario;
                        i++;
                        db.DetCierre.Add(det);
                        var Factura = db.EncCompras.Where(a => a.id == item.idFactura).FirstOrDefault();
                        db.Entry(Factura).State = EntityState.Modified;
                        Factura.idLoginAsignado = Cierre.idLogin;
                        Factura.FecAsignado = DateTime.Now;

                        if (Normas.Where(a => a.idLogin == Cierre.idLogin).FirstOrDefault() == null)
                        {
                            throw new Exception("Este usuario no contiene una norma de reparto asignada");
                        }

                        Factura.idNormaReparto = Normas.Where(a => a.idLogin == Cierre.idLogin).FirstOrDefault().id;
                        Factura.idCierre = det.idCierre;
                        Factura.idTipoGasto = item.idTipoGasto;
                        Factura.Comentario = Factura.Comentario != item.Comentario ? item.Comentario : Factura.Comentario;
                        db.SaveChanges();


                    }
                    db.SaveChanges();

                    db.Entry(Cierre).State = EntityState.Modified;
                    Cierre.CantidadRegistros = i - 1;
                    db.SaveChanges();


                    if (gastos.EncCierre.Estado == "E")
                    {
                        SendGridEmail.EmailSender emailsender = new SendGridEmail.EmailSender();
                    

                        var Login = db.Login.Where(a => a.id == login.idLoginAceptacion).FirstOrDefault();
                        var AsignadoCierre = db.Login.Where(a => a.id == Cierre.idLogin).FirstOrDefault();
                        var parametros = db.Parametros.FirstOrDefault();

                        var html = "<h3 style='text-align: center; '><strong>Liquidaci&oacute;n pendiente de revisi&oacute;n</strong></h3>";
                        html += "<p style='text-align: justify; '>Se ha recibido una nueva liquidaci&oacute;n de gastos, a continuaci&oacute;n los detalles:</p>";
                        html += "<ul>";
                        html += "<li style = 'text-align: justify;' ><strong> ID Cierre </strong>: " + Cierre.idCierre + "</li>";
                        html += "<li style = 'text-align: justify;' ><strong> Nombre </strong>: " + AsignadoCierre.Nombre + "</li>";
                        html += "<li style='text-align: justify; '><strong>Periodo</strong>: " + Cierre.Periodo + "</li>";
                        html += "<li style='text-align: justify; '><strong>Cantidad de Facturas: </strong>" + Cierre.CantidadRegistros + "</li>";
                        html += "<li style='text-align: justify; '><strong>Total</strong>: " + decimal.Round(Cierre.Total.Value, 2) + "</li>";
                        html += "</ul><p></p> ";
                        html += "<p>Favor revisar en la plataforma <a href='" + parametros.UrlSitioPublicado + "'>" + parametros.UrlSitioPublicado + "</a>&nbsp;para aceptar o denegar dicha liquidaci&oacute;n.</p>";




                        emailsender.SendV2(Login.Email, parametros.RecepcionEmail, "", parametros.RecepcionEmail, "Liquidación", "Liquidación pendiente de revisión", html, parametros.RecepcionHostName, parametros.EnvioPort, parametros.RecepcionUseSSL.Value, parametros.RecepcionEmail, parametros.RecepcionPassword);

                        try
                        {
                            BitacoraCierres bc = new BitacoraCierres();
                            bc.idUsuarioEnviador = Cierre.idLogin;
                            bc.idUsuarioAceptador = Cierre.idLoginAceptacion.Value;
                            bc.idCierre = Cierre.idCierre;
                            bc.IP = HttpContext.Current.Request.UserHostAddress;
                            bc.Detalle = "Se manda a aprobacion la liquidacion # " + Cierre.idCierre + ", el usuario: " + AsignadoCierre.Nombre + ", con el id: " + AsignadoCierre.id;
                            bc.Fecha = DateTime.Now;
                            db.BitacoraCierres.Add(bc);
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            BitacoraErrores be = new BitacoraErrores();
                            be.Descripcion = ex.Message;
                            be.StackTrace = ex.StackTrace;
                            be.Metodo = "Cambio de estado";
                            be.Fecha = DateTime.Now;
                            db.BitacoraErrores.Add(be);
                            db.SaveChanges();
                        }

                    }

                    if (gastos.EncCierre.Estado == "A" || gastos.EncCierre.Estado == "R")
                    {
                        SendGridEmail.EmailSender emailsender = new SendGridEmail.EmailSender();
                   

                        var AR = gastos.EncCierre.Estado;

                        var Login = db.Login.Where(a => a.id == login.id).FirstOrDefault();
                        var AsignadoCierre = db.Login.Where(a => a.id == Cierre.idLogin).FirstOrDefault();
                        var parametros = db.Parametros.FirstOrDefault();
                      
                        var html = "";
                        if (AR == "A")
                        {
                            var LoginAceptacion = db.Login.Where(a => a.id == Cierre.idLoginAceptacion).FirstOrDefault();
                            html = "<h3 style='text-align: center; '><strong>Liquidaci&oacute;n  Aprobada</strong></h3>";
                            html += "<p style='text-align: justify; '>Se ha aprobado tú liquidaci&oacute;n de gastos, a continuaci&oacute;n los detalles:</p>";
                            html += "<ul>";
                            html += "<li style = 'text-align: justify;' ><strong> ID Cierre </strong>: " + Cierre.idCierre + "</li>";
                            html += "<li style = 'text-align: justify;' ><strong> Nombre </strong>: " + AsignadoCierre.Nombre + "</li>";
                            html += "<li style='text-align: justify; '><strong>Periodo</strong>: " + Cierre.Periodo + "</li>";
                            html += "<li style='text-align: justify; '><strong>Cantidad de Facturas: </strong>" + Cierre.CantidadRegistros + "</li>";
                            html += "<li style='text-align: justify; '><strong>Total</strong>: " + decimal.Round(Cierre.Total.Value, 2) + "</li>";
                            html += "<li style='text-align: justify; '><strong>Usuario Aprobador</strong>: " + LoginAceptacion == null ? "" : LoginAceptacion.Nombre + "</li>";
                            html += "<li style='text-align: justify; '><strong>Comentarios de la Liquidación</strong>: " + Cierre.Observacion + "</li>";
                            html += "</ul><p></p> ";
                            html += "<p>Favor revisar en la plataforma <a href='" + parametros.UrlSitioPublicado + "'>" + parametros.UrlSitioPublicado + "</a>&nbsp;para ver más detalles de dicha liquidaci&oacute;n.</p>";

                            try
                            {
                                BitacoraCierres bc = new BitacoraCierres();
                                bc.idUsuarioEnviador = Cierre.idLogin;
                                bc.idUsuarioAceptador = Cierre.idLoginAceptacion.Value;
                                bc.idCierre = Cierre.idCierre;
                                bc.IP = HttpContext.Current.Request.UserHostAddress;
                                bc.Detalle = "Se ha aprobado la liquidacion # " + Cierre.idCierre + ", del usuario: " + AsignadoCierre.Nombre + ", con el id: " + AsignadoCierre.id + ", lo aprobo el usuario: " + LoginAceptacion.Nombre + ", con el id: " + LoginAceptacion.id;
                                bc.Fecha = DateTime.Now;
                                db.BitacoraCierres.Add(bc);
                                db.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                BitacoraErrores be = new BitacoraErrores();
                                be.Descripcion = ex.Message;
                                be.StackTrace = ex.StackTrace;
                                be.Metodo = "Cambio de estado";
                                be.Fecha = DateTime.Now;
                                db.BitacoraErrores.Add(be);
                                db.SaveChanges();
                            }
                        }
                        else
                        {
                            var LoginAceptacion = db.Login.Where(a => a.id == Cierre.idLoginAceptacion).FirstOrDefault();
                            html = "<h3 style='text-align: center; '><strong>Liquidaci&oacute;n  Rechazada</strong></h3>";
                            html += "<p style='text-align: justify; '>Se ha rechazado tú liquidaci&oacute;n de gastos, a continuaci&oacute;n los detalles:</p>";
                            html += "<ul>";
                            html += "<li style = 'text-align: justify;' ><strong> ID Cierre </strong>: " + Cierre.idCierre + "</li>";
                            html += "<li style = 'text-align: justify;' ><strong> Nombre </strong>: " + AsignadoCierre.Nombre + "</li>";
                            html += "<li style='text-align: justify; '><strong>Periodo</strong>: " + Cierre.Periodo + "</li>";
                            html += "<li style='text-align: justify; '><strong>Cantidad de Facturas: </strong>" + Cierre.CantidadRegistros + "</li>";
                            html += "<li style='text-align: justify; '><strong>Total</strong>: " + decimal.Round(Cierre.Total.Value, 2) + "</li>";
                            html += "<li style='text-align: justify; '><strong>Usuario Aprobador</strong>: " + LoginAceptacion == null ? "" : LoginAceptacion.Nombre + "</li>";
                            html += "<li style='text-align: justify; '><strong>Comentarios de la Liquidación</strong>: " + Cierre.Observacion + "</li>";
                            html += "</ul><p></p> ";
                            html += "<p>Favor revisar en la plataforma <a href='" + parametros.UrlSitioPublicado + "'>" + parametros.UrlSitioPublicado + "</a>&nbsp;para ver más detalles de dicha liquidaci&oacute;n.</p>";


                            try
                            {
                                BitacoraCierres bc = new BitacoraCierres();
                                bc.idUsuarioEnviador = Cierre.idLogin;
                                bc.idUsuarioAceptador = Cierre.idLoginAceptacion.Value;
                                bc.idCierre = Cierre.idCierre;
                                bc.IP = HttpContext.Current.Request.UserHostAddress;
                                bc.Detalle = "Se ha rechazado la liquidacion # " + Cierre.idCierre + ", del usuario: " + AsignadoCierre.Nombre + ", con el id: " + AsignadoCierre.id + ", lo rechazo el usuario: " + LoginAceptacion.Nombre + ", con el id: " + LoginAceptacion.id;
                                bc.Fecha = DateTime.Now;
                                db.BitacoraCierres.Add(bc);
                                db.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                BitacoraErrores be = new BitacoraErrores();
                                be.Descripcion = ex.Message;
                                be.StackTrace = ex.StackTrace;
                                be.Metodo = "Cambio de estado";
                                be.Fecha = DateTime.Now;
                                db.BitacoraErrores.Add(be);
                                db.SaveChanges();
                            }
                        }




                        emailsender.SendV2(Login.Email, parametros.RecepcionEmail, "", parametros.RecepcionEmail, "Liquidación", "Respuesta de la liquidación", html, parametros.RecepcionHostName, parametros.EnvioPort, parametros.RecepcionUseSSL.Value, parametros.RecepcionEmail, parametros.RecepcionPassword);



                    }



                    t.Commit();
                    G.CerrarConexionAPP(db);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                else
                {
                    throw new Exception("Este cierre no existe");
                }

            }
            catch (Exception ex)
            {
                t.Rollback();
                G.GuardarTxt("ErrorCierre" + DateTime.Now.Day + "" + DateTime.Now.Month + "" + DateTime.Now.Year + ".txt", ex.ToString());

                BitacoraErrores be = new BitacoraErrores();

                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace; 
                be.Metodo = "Actualizacion de Cierre";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, be);
            }
        }


    }
}