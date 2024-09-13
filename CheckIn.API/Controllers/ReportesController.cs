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
    public class ReportesController: ApiController
    {
        ModelCliente db;
        G G = new G();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var time = new DateTime();
                var Liquidaciones = db.EncCierre.Where(a => a.Estado != "R" && (filtro.FechaInicio != time ? a.FechaCierre >= filtro.FechaInicio : true)).ToList();

                if(filtro.FechaInicio.Date != new DateTime().Date )
                {
                    filtro.FechaFinal = filtro.FechaFinal.AddDays(1);
                    Liquidaciones = Liquidaciones.Where(a => a.FechaCierre >= filtro.FechaInicio && a.FechaCierre <= filtro.FechaFinal).ToList();
                }

                if(filtro.Codigo1 > 0)
                {
                    Liquidaciones = Liquidaciones.Where(a => a.idLogin == filtro.Codigo1).ToList();
                }

                if(!string.IsNullOrEmpty(filtro.CodMoneda))
                {
                    Liquidaciones = Liquidaciones.Where(a => a.CodMoneda == filtro.CodMoneda).ToList();
                }

                var MontoAcumulado = Liquidaciones.Sum(a => a.Total);

           
                List<EncCompras> comp = new List<EncCompras>();
                List<EncCompras> compa = new List<EncCompras>();

                var DetCierre = db.DetCierre.ToList();
                var EncComprasDB = db.EncCompras.ToList();

                foreach (var item in Liquidaciones)
                {
                    var detalle = DetCierre.Where(a => a.idCierre == item.idCierre).ToList();
                    var EncCompras = EncComprasDB.Where(a => a.idCierre == item.idCierre).ToList();
                    foreach (var ite in detalle)
                    {
                        var fac = EncCompras.Where(a => a.id == ite.idFactura).FirstOrDefault();
                        comp.Add(fac);
                    }

                }


                var Normas = db.NormasReparto.ToList();
                var Login = db.Login.ToList();


                foreach (var item in comp)
                {
                    var login = Login.Where(a => a.id == item.idLoginAsignado).FirstOrDefault();
                    var NormaRepartoActual = Normas.Where(a => a.idLogin == login.id).FirstOrDefault();

                    if (NormaRepartoActual.id != item.idNormaReparto)
                    {
                        compa.Add(item);
                    }
                }

              


                decimal Total = 0;

                foreach(var item in compa)
                {
                    Total += ((item.TotalVenta - item.TotalDescuentos) + (item.TotalImpuesto + item.TotalOtrosCargos)).Value;
                }

                MontoAcumulado = MontoAcumulado - Total;
                HeaderReportViewModel he = new HeaderReportViewModel();
                he.MontoAcumulado = MontoAcumulado.Value;
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, he);

            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Error totalizado";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
             
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [Route("api/Reportes/Graficos")]
        public async Task<HttpResponseMessage> GetGraficos([FromUri] Filtros filtro)
        {
            try
            {
                G.AbrirConexionAPP(out db);

               

                var Liquidaciones = db.EncCierre.Where(a => a.Estado != "R").Select( a => new {

                    a.idCierre,
                    a.FechaCierre,
                    a.idLogin,
                    a.CodMoneda
                    

                }).ToList();
                if (filtro != null)
                {

                    if (filtro.FechaInicio.Date != new DateTime().Date)
                    {
                        filtro.FechaFinal = filtro.FechaFinal.AddDays(1);
                        Liquidaciones = Liquidaciones.Where(a => a.FechaCierre >= filtro.FechaInicio && a.FechaCierre <= filtro.FechaFinal).ToList();
                    }

                    if (filtro.Codigo1 > 0)
                    {
                        Liquidaciones = Liquidaciones.Where(a => a.idLogin == filtro.Codigo1).ToList();
                    }


                    if (!string.IsNullOrEmpty(filtro.CodMoneda))
                    {
                        Liquidaciones = Liquidaciones.Where(a => a.CodMoneda == filtro.CodMoneda).ToList();
                    }
                }
                 
                List<EncCompras> comp = new List<EncCompras>();
                List<EncCompras> compa = new List<EncCompras>();

                var time1 = filtro.FechaInicio.AddMonths(-1);
                var time2 = filtro.FechaFinal.AddMonths(1);
                var EncCompras = db.EncCompras.Where(a => a.FecFactura >= time1 && a.FecFactura <= time2).ToList();
                var DetCierre = db.DetCierre.ToList();
                foreach (var item in Liquidaciones)
                {
                    var detalle = DetCierre.Where(a => a.idCierre == item.idCierre).ToList();
                    var Compras = EncCompras.Where(a => a.idCierre == item.idCierre).ToList();
                    foreach(var ite in detalle)
                    {
                        var fac = Compras.Where(a => a.id == ite.idFactura).FirstOrDefault();
                        comp.Add(fac);
                    }

                }

                var Normas = db.NormasReparto.ToList();
                var Gastos = db.Gastos.ToList();
                var Login = db.Login.ToList();


                foreach(var item2 in comp)
                {

                    if(item2.idLoginAsignado == 0 || item2.idLoginAsignado == null)
                    {
                        G.GuardarTxt("Login.txt", "factura => " + item2.id + " idCierre => " + item2.idCierre);
                    }

                    var login = Login.Where(a => a.id == item2.idLoginAsignado).FirstOrDefault();
                    var NormaRepartoActual = Normas.Where(a => a.idLogin == login.id).FirstOrDefault();

                    if(NormaRepartoActual.id == item2.idNormaReparto)
                    {
                        compa.Add(item2);
                    }
                }



                var envio = compa.Select(a => new {

                    a.id,
                    a.CodEmpresa
                  ,
                    a.CodProveedor,
                    a.NomProveedor
                  ,
                    a.TipoDocumento
                  ,
                    a.NumFactura
                  ,
                    a.FecFactura
                  ,
                    a.TipoIdentificacionCliente
                  ,
                    a.CodCliente
                  ,
                    a.NomCliente
                  ,
                    a.EmailCliente
                  ,
                    a.DiasCredito
                  ,
                    a.CondicionVenta
                  ,
                    a.ClaveHacienda
                  ,
                    a.ConsecutivoHacienda
                  ,
                    a.MedioPago
                  ,
                    a.Situacion
                  ,
                    a.CodMoneda
                  ,
                    a.TotalServGravados
                  ,
                    a.TotalServExentos
                  ,
                    a.TotalMercanciasGravadas
                  ,
                    a.TotalMercanciasExentas
                  ,
                    a.TotalExento
                  ,
                    a.TotalVenta
                  ,
                    a.TotalDescuentos
                  ,
                    a.TotalVentaNeta
                  ,
                    a.TotalImpuesto
                  ,
                    a.TotalComprobante
                  ,

                    a.FechaGravado
                  ,
                    a.TotalServExonerado
                  ,
                    a.TotalMercExonerada
                  ,
                    a.TotalExonerado
                  ,
                    a.TotalIVADevuelto
                  ,
                    a.TotalOtrosCargos
                  ,
                    a.CodigoActividadEconomica
                  ,
                    a.idLoginAsignado
                  ,
                    a.FecAsignado

                  ,

                    a.idNormaReparto
                  ,
                    a.idTipoGasto
                  ,
                    TipoGasto = (Gastos.Where(d => d.idTipoGasto == a.idTipoGasto).FirstOrDefault() == null ? "Sin Asignar": Gastos.Where(d => d.idTipoGasto == a.idTipoGasto).FirstOrDefault().Nombre),
                    a.idCierre

                }).ToList();

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, envio );

            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Error de Graficos";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}