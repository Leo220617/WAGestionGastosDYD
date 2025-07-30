using CheckIn.API.Models;
using CheckIn.API.Models.ModelCliente;
using CheckIn.API.Models.ModelMain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using S22.Imap;
using SAPbobsCOM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
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

        public class LoginRequest
        {
            public string CompanyDB { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
        }
        public class LoginResponse
        {
            public string odatametadata { get; set; }
            public string SessionId { get; set; }
            public string Version { get; set; }
            public int SessionTimeout { get; set; }
        }
        private string Login(ConexionServiceLayer conexion)
        {
            try
            {


                var baseUrl = conexion.baseUrl;
                // Request details
                string url = $"{baseUrl}Login";
                LoginRequest loginRequest = new LoginRequest()
                {
                    UserName = conexion.userName,
                    Password = conexion.password,
                    CompanyDB = conexion.companyDB
                };

                // Serialize request body to JSON
                string jsonRequestBody = JsonConvert.SerializeObject(loginRequest);

                // Make the request
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.KeepAlive = true;
                httpWebRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                httpWebRequest.ServicePoint.Expect100Continue = false;

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(jsonRequestBody);
                }

                try
                {
                    // Call Service Layer
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();

                        // Deserialize success response
                        var responseInstance = JsonConvert.DeserializeObject<LoginResponse>(result);

                        return responseInstance.SessionId;


                    }
                }
                catch (Exception ex)
                {
                    // Unauthorized, etc.
                    G.GuardarTxt("ErrorServiceLayer.txt", ex.Message);
                    return "";
                }

            }
            catch (Exception ex)
            {

                return "";
            }
        }
        private bool Logout(ConexionServiceLayer conexion, string sessionId)
        {
            var baseUrl = conexion.baseUrl;
            string logoutUrl = $"{baseUrl}Logout";

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(logoutUrl);
                request.Method = "POST";
                request.Accept = "application/json";
                request.Headers.Add("Cookie", $"B1SESSION={sessionId}");
                request.KeepAlive = true;
                request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                request.ServicePoint.Expect100Continue = false;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.NoContent
                        || response.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                    else
                    {
                        G.GuardarTxt("ErrorServiceLayer.txt", "Logout failed. Status code: " + response.StatusCode);

                        return false;
                    }
                }
            }
            catch (Exception ex)
            {


                return false;
            }
        }
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
                    if (!param.serviceLayer)
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
                        if (!item.RegimenSimplificado || !string.IsNullOrEmpty(item.PdfFactura))
                        {
                            oInvoice.UserFields.Fields.Item("U_PDF").Value = param.UrlImagenesApp + item.PdfFactura;

                        }

                        var DetCompras = db.DetCompras.Where(a => a.NumFactura == item.NumFactura && a.ClaveHacienda == item.ClaveHacienda && a.ConsecutivoHacienda == item.ConsecutivoHacienda).ToList();
                        var i = 0;

                        foreach (var item2 in DetCompras)
                        {
                            Gastos TipoGasto = new Gastos();


                            TipoGasto = db.Gastos.Where(a => a.idTipoGasto == item.idTipoGasto).FirstOrDefault();


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
                            oInvoice.Lines.UserFields.Fields.Item("U_DYD_CodigoMH").Value = string.IsNullOrEmpty(item2.CodCabys) ? "6332000000000" : item2.CodCabys;
                        

                            oInvoice.Lines.Add();

                            i++;

                        }

                        if (item.TotalOtrosCargos > 0)
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
                            oInvoice.Lines.UserFields.Fields.Item("U_DYD_CodigoMH").Value = "6332000000000";
                         

                            oInvoice.Lines.Add();

                            i++;
                        }

                       


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
                    else
                    {
                        var conexionServiceLayer = db.ConexionServiceLayer.FirstOrDefault();
                        var baseUrl = conexionServiceLayer.baseUrl;
                        string postingUrl = baseUrl +   "Drafts" ;

                        // JSON del documento

                        
                        var cardCode = item.CardCode ?? "";
                        var docDate = item.FecFactura.Value;
                        var docDueDate = item.FecFactura.Value;
                        var currency = Cierre.CodMoneda == "CRC" ? "COL" : Cierre.CodMoneda;
                        var DocObjectCode =    "oPurchaseInvoices";
                        var NumAtCard = item.ConsecutivoHacienda;
                        var U_Pagar_a  = login.CardCode;
                        var U_Liquidacion  = idCierre.ToString();
                        var U_PDF = "";
                        if (!item.RegimenSimplificado || !string.IsNullOrEmpty(item.PdfFactura))
                        {
                            U_PDF = param.UrlImagenesApp + item.PdfFactura;

                        }
                        var DetCompras = db.DetCompras.Where(a => a.NumFactura == item.NumFactura && a.ClaveHacienda == item.ClaveHacienda && a.ConsecutivoHacienda == item.ConsecutivoHacienda).ToList();

                        var documentLines = new JArray();
                        var i = 0;
                        foreach (var item2 in DetCompras)
                        {
                            Gastos TipoGasto = new Gastos();


                            TipoGasto = db.Gastos.Where(a => a.idTipoGasto == item.idTipoGasto).FirstOrDefault(); 

                            var tipoGasto = db.Gastos.FirstOrDefault(a => a.idTipoGasto == item.idTipoGasto);
                            var cuenta = db.CuentasContables.FirstOrDefault(a => a.idCuentaContable == tipoGasto.idCuentaContable);
                            var norma = db.NormasReparto.FirstOrDefault(a => a.id == item.idNormaReparto);
                            var dimension = db.Dimensiones.FirstOrDefault(a => a.id == norma.idDimension);

                            var montoSinImpuesto = Convert.ToDecimal(item2.SubTotal.Value); 

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

                            


                            imp1 += item.Impuesto1;
                            imp2 += item.Impuesto2;
                            imp4 += item.Impuesto4;
                            imp8 += item.Impuesto8;
                            imp13 += item.Impuesto13;

                             
                            var U_DYD_CodigoMH = string.IsNullOrEmpty(item2.CodCabys) ? "6332000000000" : item2.CodCabys;

                            var dl = new JObject
                            {
                                { "LineNum", i },
                                { "ItemCode", null },
                                { "ItemDescription", item2.NomPro },
                                { "Quantity", 0 },
                                { "Currency", currency },
                                { "Rate", 0 },
                                { "AccountCode", cuenta.CodSAP },
                                { "LineTotal", montoSinImpuesto },
                                { "Price", montoSinImpuesto },
                                { "UnitPrice", montoSinImpuesto },
                                { "TaxCode", taxCode }
                            };
                            documentLines.Add(dl);
                            i++;
                        }

                        if (item.TotalOtrosCargos > 0)
                        {
                            Gastos TipoGasto = new Gastos();

                            TipoGasto = db.Gastos.Where(a => a.Nombre.ToUpper().Contains("Alimentacion".ToUpper())).FirstOrDefault();



                            var Cuenta = db.CuentasContables.Where(a => a.idCuentaContable == TipoGasto.idCuentaContable).FirstOrDefault();
                            var Norma = db.NormasReparto.Where(a => a.id == item.idNormaReparto).FirstOrDefault();
                            var Dimension = db.Dimensiones.Where(a => a.id == Norma.idDimension).FirstOrDefault();
 

                            var taxCode = param.IMP0;
                             

                            var montoSinImpuesto = Convert.ToDouble(item.TotalOtrosCargos);
                            var U_DYD_CodigoMH  = "6332000000000";
                             
                            var dl = new JObject
                            {
                                { "LineNum", i },
                                { "ItemCode", null },
                                { "ItemDescription", "Otros Cargos"},
                                { "Quantity", 0 },
                                { "Currency", currency },
                                { "Rate", 0 },
                                { "AccountCode", Cuenta.CodSAP },
                                { "LineTotal", montoSinImpuesto },
                                { "Price", montoSinImpuesto },
                                { "UnitPrice", montoSinImpuesto },
                                { "TaxCode", taxCode }
                            };
                            documentLines.Add(dl);
                            i++;
                        }
                      
                        JObject payload;
                        payload = new JObject
                        {
                            { "CardCode", cardCode },
                            { "DocDate", docDate.ToString("yyyy-MM-dd") },
                            { "DocObjectCode", DocObjectCode },
                            { "DocDueDate", docDueDate.ToString("yyyy-MM-dd") },
                            { "DocCurrency", currency },
                            { "DocType", "dDocument_Service" },
                            { "NumAtCard", NumAtCard },
                            { "Comments", "LIQUIDACION # " + U_Liquidacion },
                            { "DocumentLines", documentLines }
                        };

                        // Enviar factura
                        var sessionId = Login(conexionServiceLayer);
                        if (string.IsNullOrEmpty(sessionId))
                        {
                            throw new Exception("No se ha podido realizar login con servicelayer. Revisar bitacora");
                        }
                        try
                        {
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(postingUrl);
                            request.Method = "POST";
                            request.ContentType = "application/json";
                            request.Accept = "application/json";
                            request.Headers.Add("Cookie", $"B1SESSION={sessionId}");
                            request.KeepAlive = true;
                            request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                            request.ServicePoint.Expect100Continue = false;

                            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                            {
                                streamWriter.Write(payload.ToString());
                                streamWriter.Flush();
                                streamWriter.Close();
                            }

                            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                            {
                                var result = "";
                                using (var streamReader = new StreamReader(response.GetResponseStream()))
                                {
                                    result = streamReader.ReadToEnd();
                                }

                                if (response.StatusCode == HttpStatusCode.Created)
                                {
                                    JObject jsonObject = JObject.Parse(result);

                                    // Extract the meaningful values
                                    int docEntry = (int)jsonObject["DocEntry"];
                                    int docNum = (int)jsonObject["DocNum"];

                                   

                                    var respLogout = Logout(conexionServiceLayer, sessionId); 



                                }
                                else
                                {
                                    BitacoraErrores be = new BitacoraErrores();
                                    be.Descripcion = "Factura #" + item.NumFactura + " " ;
                                    be.StackTrace = "";
                                    be.Metodo = "Insercion de Asiento en la factura #" + item.id;
                                    be.Fecha = DateTime.Now;
                                    db.BitacoraErrores.Add(be);
                                    db.SaveChanges();
                                    contador++;
                                    Errores = Errores + " ******* " + be.Descripcion;

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            BitacoraErrores be = new BitacoraErrores();
                            be.Descripcion = "Factura #" + item.NumFactura + " " ;
                            be.StackTrace = "";
                            be.Metodo = "Insercion de Asiento en la factura #" + item.id;
                            be.Fecha = DateTime.Now;
                            db.BitacoraErrores.Add(be);
                            db.SaveChanges();
                            contador++;
                            Errores = Errores + " ******* " + be.Descripcion;

                        }

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
                        User = param.serviceLayer ? "": Conexion.Company.UserName
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
                    User = param.serviceLayer ? "" : Conexion.Company.UserName
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
                    User = ""
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