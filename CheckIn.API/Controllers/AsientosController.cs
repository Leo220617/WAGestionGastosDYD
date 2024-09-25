using CheckIn.API.Models;
using CheckIn.API.Models.ModelCliente;
using CheckIn.API.Models.ModelMain;
using Newtonsoft.Json;
using S22.Imap;
using SAPbobsCOM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Xml.Linq;

namespace CheckIn.API.Controllers
{
    [Authorize]
    public class AsientosController : ApiController
    {
        ModelLicencias dbLogin = new ModelLicencias();
        ModelCliente db;
        G G = new G();


        public string Get()
        {


            try
            {

                int resp = Conexion.Company.Connect();
                if (resp != 0)
                {


                    return Conexion.Company.GetLastErrorDescription();
                }
                else
                {

                    return resp.ToString();
                }

            }
            catch (Exception ex)
            {

                return ex.Message;
            }


        }


        [Route("api/Asientos/Insertar")]
        public HttpResponseMessage GetAsientos([FromUri] int idCierre = 0)
        {

            object resp;
            decimal imp1 = 0;
            decimal imp2 = 0;
            decimal imp4 = 0;
            decimal imp8 = 0;
            decimal imp13 = 0;
            try
            {
                G.AbrirConexionAPP(out db);
                var Cierre = db.EncCierre.Where(a => a.idCierre == idCierre).FirstOrDefault(); //nos traemos el encabezado del cierre

                if (Cierre.ProcesadaSAP == true)
                {
                    throw new Exception("Esta liquidación ya fue procesada");
                }

                var Detalle = db.DetCierre.Where(a => a.idCierre == Cierre.idCierre).ToList(); //Nos raemos el detalle del cierre donde vienen los numeros de las facturas

                List<EncCompras> enc = new List<EncCompras>();
                var Encabezados = db.EncCompras.Where(a => a.idCierre == Cierre.idCierre).ToList();
                foreach (var item in Detalle)
                {
                    var compra = Encabezados.Where(a => a.id == item.idFactura).FirstOrDefault();
                    enc.Add(compra);
                }

                var login = db.Login.Where(a => a.id == Cierre.idLogin).FirstOrDefault();
                var param = db.Parametros.FirstOrDefault();

                var contador = 0;
                var Errores = "";
                foreach (var item in enc)
                {

                    var oInvoice = (Documents)Conexion.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oDrafts);



                    oInvoice.DocObjectCode = BoObjectTypes.oPurchaseInvoices;
                    oInvoice.CardCode = item.CardCode; //CardCode que viene de encabezado
                    oInvoice.DocDate = item.FecFactura.Value;//Cierre.FechaFinal; //Inicio del periodo de cierre
                    oInvoice.DocDueDate = item.FecFactura.Value; //Final del periodo de cierre
                    oInvoice.DocCurrency = (Cierre.CodMoneda == "CRC" ? "COL" : Cierre.CodMoneda); //Moneda de la liquidacion
                    oInvoice.DocType = BoDocumentTypes.dDocument_Service;
                    oInvoice.NumAtCard = item.ConsecutivoHacienda;
                    oInvoice.UserFields.Fields.Item("U_Pagar_a").Value = login.CardCode;
                    oInvoice.UserFields.Fields.Item("U_Liquidacion").Value = idCierre.ToString();
                    oInvoice.UserFields.Fields.Item("U_PDF").Value = param.UrlImagenesApp + item.PdfFactura;

                    var DetCompras = db.DetCompras.Where(a => a.NumFactura == item.NumFactura && a.ClaveHacienda == item.ClaveHacienda && a.ConsecutivoHacienda == item.ConsecutivoHacienda).ToList();
                    var i = 0;

                    foreach (var item2 in DetCompras)
                    {
                        Gastos TipoGasto = new Gastos();
                        if (item.RegimenSimplificado)
                        {
                            TipoGasto = db.Gastos.Where(a => a.Nombre.ToUpper().Contains("Regimen Simplificado".ToUpper())).FirstOrDefault();

                        }
                        else
                        {
                            TipoGasto = db.Gastos.Where(a => a.idTipoGasto == item.idTipoGasto).FirstOrDefault();
                        }

                        var Cuenta = db.CuentasContables.Where(a => a.idCuentaContable == TipoGasto.idCuentaContable).FirstOrDefault();
                        var Norma = db.NormasReparto.Where(a => a.id == item.idNormaReparto).FirstOrDefault();
                        var Dimension = db.Dimensiones.Where(a => a.id == Norma.idDimension).FirstOrDefault();

                        oInvoice.Lines.SetCurrentLine(i);
                        oInvoice.Lines.ItemDescription = item2.NomPro; //"3102751358 - D y D Consultores"; // Factura -> Cedula 
                        oInvoice.Lines.AccountCode = Cuenta.CodSAP; //"6-01-02-05-000"; //Cuenta contable del gasto

                        var taxCode = "";

                        switch (Convert.ToInt32(item2.ImpuestoTarifa).ToString())
                        {
                            case "0":
                                {
                                    taxCode = param.IMP0;
                                    break;
                                }
                            case "1":
                                {
                                    taxCode = param.IMP1;
                                    break;
                                }
                            case "2":
                                {
                                    taxCode = param.IMP2;
                                    break;
                                }
                            case "4":
                                {
                                    taxCode = param.IMP4;
                                    break;
                                }
                            case "8":
                                {
                                    taxCode = param.IMP8;
                                    break;
                                }
                            case "13":
                                {
                                    taxCode = param.IMP13;
                                    break;
                                }
                            default:
                                {
                                    taxCode = param.IMP13;
                                    break;
                                }
                        }

                        oInvoice.Lines.TaxCode = taxCode; //param.IMPEX;


                        imp1 += item.Impuesto1;
                        imp2 += item.Impuesto2;
                        imp4 += item.Impuesto4;
                        imp8 += item.Impuesto8;
                        imp13 += item.Impuesto13;

                          
                        oInvoice.Lines.LineTotal = Convert.ToDouble(item2.SubTotal.Value);
                        oInvoice.Lines.UserFields.Fields.Item("U_DYD_CodigoMH").Value = item2.CodCabys;
                        //if (TipoGasto.Nombre.ToUpper().Contains("Combustible".ToUpper()))
                        //{
                        //    var DetalleFac = db.DetCompras.Where(a => a.NumFactura == item.NumFactura && a.ClaveHacienda == item.ClaveHacienda && a.ConsecutivoHacienda == item.ConsecutivoHacienda).FirstOrDefault();
                        //    oInvoice.Lines.UserFields.Fields.Item("U_CantLitrosKw").Value = DetalleFac.Cantidad;
                        //    oInvoice.Lines.UserFields.Fields.Item("U_Tipo").Value = (DetalleFac.NomPro.ToUpper().Contains("Diesel".ToUpper()) ? "Diesel" : QuitarTilde(DetalleFac.NomPro).ToUpper().Contains("Super".ToUpper()) ? "Gasolina Super" : QuitarTilde(DetalleFac.NomPro).ToUpper().Contains("Regular".ToUpper()) ? "Gasolina Regular" : "Diesel");
                        //}

                        //   oInvoice.Lines.UserFields.Fields.Item("U_NumFactura").Value = item.NumFactura.ToString();
                        // oInvoice.Lines.UserFields.Fields.Item("U_FechaFactura").Value = item.FecFactura;

                        oInvoice.Lines.Add();

                        i++;

                    }

                    if(item.TotalOtrosCargos > 0)
                    {
                        Gastos TipoGasto = new Gastos();
                        
                        TipoGasto = db.Gastos.Where(a => a.Nombre.ToUpper().Contains("Alimentacion".ToUpper())).FirstOrDefault();

                       

                        var Cuenta = db.CuentasContables.Where(a => a.idCuentaContable == TipoGasto.idCuentaContable).FirstOrDefault();
                        var Norma = db.NormasReparto.Where(a => a.id == item.idNormaReparto).FirstOrDefault();
                        var Dimension = db.Dimensiones.Where(a => a.id == Norma.idDimension).FirstOrDefault();

                        oInvoice.Lines.SetCurrentLine(i);
                        oInvoice.Lines.ItemDescription = "Otros Cargos"; //"3102751358 - D y D Consultores"; // Factura -> Cedula 
                        oInvoice.Lines.AccountCode = Cuenta.CodSAP; //"6-01-02-05-000"; //Cuenta contable del gasto

                        var taxCode = param.IMP0;

                         

                        oInvoice.Lines.TaxCode = taxCode; //param.IMPEX; 

                        oInvoice.Lines.LineTotal = Convert.ToDouble(item.TotalOtrosCargos);
                        oInvoice.Lines.UserFields.Fields.Item("U_DYD_CodigoMH").Value = "";
                        //if (TipoGasto.Nombre.ToUpper().Contains("Combustible".ToUpper()))
                        //{
                        //    var DetalleFac = db.DetCompras.Where(a => a.NumFactura == item.NumFactura && a.ClaveHacienda == item.ClaveHacienda && a.ConsecutivoHacienda == item.ConsecutivoHacienda).FirstOrDefault();
                        //    oInvoice.Lines.UserFields.Fields.Item("U_CantLitrosKw").Value = DetalleFac.Cantidad;
                        //    oInvoice.Lines.UserFields.Fields.Item("U_Tipo").Value = (DetalleFac.NomPro.ToUpper().Contains("Diesel".ToUpper()) ? "Diesel" : QuitarTilde(DetalleFac.NomPro).ToUpper().Contains("Super".ToUpper()) ? "Gasolina Super" : QuitarTilde(DetalleFac.NomPro).ToUpper().Contains("Regular".ToUpper()) ? "Gasolina Regular" : "Diesel");
                        //}

                        //   oInvoice.Lines.UserFields.Fields.Item("U_NumFactura").Value = item.NumFactura.ToString();
                        // oInvoice.Lines.UserFields.Fields.Item("U_FechaFactura").Value = item.FecFactura;

                        oInvoice.Lines.Add();

                        i++;
                    }

                    //if (imp1 > 0)
                    //{
                    //    oInvoice.Lines.SetCurrentLine(i);
                    //    oInvoice.Lines.ItemDescription = "Impuesto 1";
                    //    oInvoice.Lines.LineTotal = Convert.ToDouble(imp1);
                    //    oInvoice.Lines.TaxCode = param.IMPEX;
                    //    oInvoice.Lines.AccountCode = param.CI1;

                    //    oInvoice.Lines.Add();
                    //    i++;
                    //}

                    //if (imp2 > 0)
                    //{
                    //    oInvoice.Lines.SetCurrentLine(i);
                    //    oInvoice.Lines.ItemDescription = "Impuesto 2";
                    //    oInvoice.Lines.LineTotal = Convert.ToDouble(imp2);
                    //    oInvoice.Lines.TaxCode = param.IMPEX;
                    //    oInvoice.Lines.AccountCode = param.CI2;
                    //    oInvoice.Lines.Add();
                    //    i++;
                    //}

                    //if (imp4 > 0)
                    //{
                    //    oInvoice.Lines.SetCurrentLine(i);
                    //    oInvoice.Lines.ItemDescription = "Impuesto 4";
                    //    oInvoice.Lines.LineTotal = Convert.ToDouble(imp4);
                    //    oInvoice.Lines.TaxCode = param.IMPEX;
                    //    oInvoice.Lines.AccountCode = param.CI4;
                    //    oInvoice.Lines.Add();
                    //    i++;
                    //}

                    //if (imp8 > 0)
                    //{
                    //    oInvoice.Lines.SetCurrentLine(i);
                    //    oInvoice.Lines.ItemDescription = "Impuesto 8";
                    //    oInvoice.Lines.LineTotal = Convert.ToDouble(imp8);
                    //    oInvoice.Lines.TaxCode = param.IMPEX;
                    //    oInvoice.Lines.AccountCode = param.CI8;
                    //    oInvoice.Lines.Add();
                    //    i++;
                    //}

                    //if (imp13 > 0)
                    //{
                    //    oInvoice.Lines.SetCurrentLine(i);
                    //    oInvoice.Lines.ItemDescription = "Impuesto 13";
                    //    oInvoice.Lines.LineTotal = Convert.ToDouble(imp13);
                    //    oInvoice.Lines.TaxCode = param.IMPEX;
                    //    oInvoice.Lines.AccountCode = param.CI13;
                    //    oInvoice.Lines.Add();
                    //    i++;
                    //}


                    var respuesta = oInvoice.Add();
                    if (respuesta != 0)
                    {
                        BitacoraErrores be = new BitacoraErrores();
                        be.Descripcion = "Factura #" + item.NumFactura + " " + Conexion.Company.GetLastErrorDescription();
                        be.StackTrace = Conexion.Company.UserName;
                        be.Metodo = "Insercion de Asiento en la factura #" + item.id;
                        be.Fecha = DateTime.Now;
                        db.BitacoraErrores.Add(be);
                        db.SaveChanges();
                        contador++;
                        Errores = Errores + " ******* " + be.Descripcion;
                    }


                }



                if (contador == 0)
                {


                    db.Entry(Cierre).State = EntityState.Modified;
                    Cierre.ProcesadaSAP = true;
                    db.SaveChanges();
                    resp = new
                    {

                        DocEntry = 0,
                        //  Series = pedido.Series.ToString(),
                        Type = "oPurchaiseInvoice",
                        Status = 1,
                        Message = "Facturas creadas exitosamente",
                        User = Conexion.Company.UserName
                    };
                    G.CerrarConexionAPP(db);
                    Conexion.Desconectar();
                    return Request.CreateResponse(HttpStatusCode.OK, resp);
                }

                resp = new
                {
                    //   Series = pedido.Series.ToString(),
                    DocEntry = 0,
                    Type = "oPurchaiseInvoice",
                    Status = 0,
                    Message = Errores, //Conexion.Company.GetLastErrorDescription(),
                    User = Conexion.Company.UserName
                };





                Conexion.Desconectar();
                G.CerrarConexionAPP(db);

                return Request.CreateResponse(HttpStatusCode.OK, resp);
            }
            catch (Exception ex)
            {
                resp = new
                {
                    DocEntry = 0,
                    Type = "oPurchaiseInvoice",
                    Status = 0,
                    Message = "[Stack] -> " + ex.StackTrace + " -- [Message] --> " + ex.Message,
                    User = Conexion.Company.UserName
                };

                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Insercion de Asiento";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();


                Conexion.Desconectar();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, resp);
            }


        }

        public static string QuitarTilde(string inputString)
        {
            string normalizedString = inputString.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < normalizedString.Length; i++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(normalizedString[i]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(normalizedString[i]);
                }
            }
            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }

    }
}