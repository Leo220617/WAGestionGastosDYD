using CheckIn.API.Models;
using CheckIn.API.Models.ModelCliente;
using CheckIn.API.Models.ModelMain;
using iTextSharp.text.pdf;
using S22.Imap;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Data;
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
using System.Xml;
using System.Xml.Linq;

namespace CheckIn.API.Controllers
{
    [Authorize]
    public class ComprasController : ApiController
    {
        ModelCliente db;
        ModelLicencias dbLogin = new ModelLicencias();

        G G = new G();

        public string GuardaImagenBase64(string ImagenBase64, string CarpetaImagen, string NomImagen, System.Drawing.Imaging.ImageFormat FormatoImagen)
        {
            Parametros Params = db.Parametros.FirstOrDefault();

            string NombreImagen = "";
            string rutaImagen = "";

            if (NomImagen == "")
            {
                NombreImagen = "NoImage.png";
            }


            Random i = new Random();
            int o = i.Next(0, 10000);
            NombreImagen = o + "_" + NomImagen;

            var _bytes = Convert.FromBase64String(ImagenBase64);
            string pathImage = $"~/Temp/{G.ObtenerCedulaJuridia()}/{NombreImagen}";
            var fullpath = System.Web.HttpContext.Current.Server.MapPath(pathImage);
            using (System.Drawing.Image image = System.Drawing.Image.FromStream(new MemoryStream(_bytes)))
            {
                try
                {
                    image.Save(fullpath, FormatoImagen);  // aqui seria en base al tipo de imagen

                }
                catch (Exception ex)
                {
                    G.GuardarTxt("ErrorImagen.txt", ex.ToString());
                }
                image.Save(fullpath, FormatoImagen);

            }
            rutaImagen = Params.UrlImagenesApp + pathImage;
            rutaImagen = rutaImagen.Replace("~/Temp/", "");

            return NombreImagen;
        }






        [Route("api/Compras/RealizarLecturaEmail")]

        public async Task<HttpResponseMessage> GetRealizarLecturaEmailsAsync()
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Parametros = db.Parametros.FirstOrDefault();
                var Correos = db.CorreosRecepcion.ToList();
                var Compañia = G.ObtenerCedulaJuridia();

                var Licencia = dbLogin.LicEmpresas.Where(a => a.CedulaJuridica == Compañia).FirstOrDefault();

                var Pais = Licencia.CadenaConexionSAP;
                foreach (var item in Correos)
                {


                    using (ImapClient client = new ImapClient(item.RecepcionHostName, (int)(item.RecepcionPort),
                               item.RecepcionEmail, item.RecepcionPassword, AuthMethod.Login, (bool)(item.RecepcionUseSSL)))
                    {

                        IEnumerable<uint> uids = client.Search(SearchCondition.Unseen());

                        DateTime recepcionUltimaLecturaImap = DateTime.Now;
                        if (item.RecepcionUltimaLecturaImap != null)
                            recepcionUltimaLecturaImap = item.RecepcionUltimaLecturaImap.Value;

                        uids.Concat(client.Search(SearchCondition.SentSince(recepcionUltimaLecturaImap)));

                        foreach (var uid in uids)
                        {
                            System.Net.Mail.MailMessage message = client.GetMessage(uid);

                            if (message.Attachments.Count > 0)
                            {
                                try
                                {
                                    byte[] ByteArrayPDF = null;
                                    int i = 1;

                                    decimal idGeneral = 0;
                                    foreach (var attachment in message.Attachments)
                                    {

                                        try
                                        {
                                            System.IO.StreamReader sr = new System.IO.StreamReader(attachment.ContentStream);



                                            string texto = sr.ReadToEnd();

                                            if (texto.Substring(0, 3) == "???")
                                                texto = texto.Substring(3);

                                            if (texto.Contains("PDF"))
                                            {

                                                ByteArrayPDF = ((MemoryStream)attachment.ContentStream).ToArray();



                                            }


                                            if ((texto.Contains("FacturaElectronica") || texto.Contains("<comprobante>"))
                                                    && !texto.Contains("TiqueteElectronico")
                                                    && !texto.Contains("NotaCreditoElectronica")
                                                    && !texto.Contains("NotaDebitoElectronica"))
                                            {
                                                var emailByteArray = G.Zip(texto);

                                                decimal id = db.Database.SqlQuery<decimal>("Insert Into BandejaEntrada(XmlFactura, Procesado, Asunto, Remitente,Pdf) " +
                                                        " VALUES (@EmailJson, 0, @Asunto, @Remitente, @Pdf); SELECT SCOPE_IDENTITY(); ",
                                                        new SqlParameter("@EmailJson", emailByteArray),
                                                        new SqlParameter("@Asunto", message.Subject),
                                                        new SqlParameter("@Remitente", message.From.ToString()),
                                                        new SqlParameter("@Pdf", (ByteArrayPDF == null ? new byte[0] : ByteArrayPDF))).First();
                                                idGeneral = id;
                                                try
                                                {

                                                    var datos = Pais == "E" ? G.ObtenerDatosXmlRechazadoEcuador(texto) : G.ObtenerDatosXmlRechazado(texto);

                                                    db.Database.ExecuteSqlCommand("Update BandejaEntrada set NumeroConsecutivo=@NumeroConsecutivo, " +
                                                        " TipoDocumento = @TipoDocumento, FechaEmision = @FechaEmision , " +
                                                        " NombreEmisor = @NombreEmisor,IdEmisor = @IdEmisor ,CodigoMoneda = @CodigoMoneda , " +
                                                        " TotalComprobante = @TotalComprobante " +
                                                        " WHERE Id=@Id ",
                                                         new SqlParameter("@NumeroConsecutivo", datos.NumeroConsecutivo),
                                                         new SqlParameter("@TipoDocumento", datos.TipoDocumento),
                                                         new SqlParameter("@FechaEmision", datos.FechaEmision),
                                                         new SqlParameter("@NombreEmisor", datos.NombreEmisor),
                                                         new SqlParameter("@IdEmisor", datos.Numero),
                                                         new SqlParameter("@CodigoMoneda", datos.CodigoMoneda),
                                                         new SqlParameter("@TotalComprobante", datos.TotalComprobante),
                                                         new SqlParameter("@Id", id));
                                                }
                                                catch { }
                                            }

                                            if (i == message.Attachments.Count())
                                            {
                                                if (idGeneral > 0)
                                                {
                                                    var bandeja = db.BandejaEntrada.Where(a => a.Id == idGeneral).FirstOrDefault();

                                                    if (bandeja.Pdf.Count() == 0)
                                                    {
                                                        db.Database.ExecuteSqlCommand("Update BandejaEntrada set Pdf=@Pdf " +

                                                   " WHERE Id=@Id ",
                                                    new SqlParameter("@Pdf", ByteArrayPDF),

                                                    new SqlParameter("@Id", idGeneral));
                                                    }

                                                }
                                            }

                                            i++;
                                        }
                                        catch (Exception ex)
                                        {


                                        }
                                    }
                                }
                                catch (Exception ex)
                                {


                                }
                            }
                            message.Dispose();

                            await System.Threading.Tasks.Task.Delay(100);
                        }
                        db.Entry(item).State = EntityState.Modified;
                        item.RecepcionUltimaLecturaImap = DateTime.Now;
                        db.SaveChanges();

                    }

                }


                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {

                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Lectura de emails";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        [Route("api/Compras/LeerBandejaEntrada")]
        public async Task<HttpResponseMessage> GetLeerBandejaEntradaAsync()
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var Lista = db.BandejaEntrada.Where(a => a.Procesado == "0" && string.IsNullOrEmpty(a.Mensaje)).ToList();
                var Compañia = G.ObtenerCedulaJuridia();

                var Licencia = dbLogin.LicEmpresas.Where(a => a.CedulaJuridica == Compañia).FirstOrDefault();

                var Pais = Licencia.CadenaConexionSAP;
                foreach (var item in Lista)
                {
                    try
                    {
                        var attachmentBody = G.Unzip(item.XmlFactura);
                        EncCompras factura = new EncCompras();
                        string xmlBase64 = attachmentBody;
                        xmlBase64 = xmlBase64.Replace("\n", "");
                        if (Pais == "E")
                        {
                            string palabraInicio = "<comprobante><![CDATA[";
                            string palabraFin = "]]></comprobante>";

                            // Encontrar las posiciones de las palabras clave
                            int indiceInicio = xmlBase64.IndexOf(palabraInicio);
                            if (indiceInicio == -1)
                            {
                                palabraInicio = "<comprobante> <![CDATA[";
                                indiceInicio = xmlBase64.IndexOf(palabraInicio);
                            }
                            int indiceFin = xmlBase64.IndexOf(palabraFin);
                            string subcadena = "";
                            // Verificar si se encontraron ambas palabras clave
                            if (indiceInicio != -1 && indiceFin != -1)
                            {
                                // Calcular la longitud de la subcadena
                                int longitudSubcadena = indiceFin - (indiceInicio + palabraInicio.Length);

                                // Extraer la subcadena
                                subcadena = xmlBase64.Substring(indiceInicio + palabraInicio.Length, longitudSubcadena);
                                xmlBase64 = subcadena;

                            }
                        }

                        string pdfBase64 = "";



                        var xml = G.ConvertirArchivoaXElement(xmlBase64.Trim(), G.ObtenerCedulaJuridia());


                        if (!xmlBase64.Contains("FacturaElectronica") && !xmlBase64.Contains("comprobante")
                            && !xmlBase64.Contains("TiqueteElectronico")
                            && !xmlBase64.Contains("NotaCreditoElectronica")
                            && !xmlBase64.Contains("NotaDebitoElectronica"))
                            throw new Exception("No es un documento electrónico");

                        factura.ClaveHacienda = Pais == "E" ? (G.ExtraerValorDeNodoXml(xml, "infoTributaria/estab") + "-" + G.ExtraerValorDeNodoXml(xml, "infoTributaria/ptoEmi") + "-" + G.ExtraerValorDeNodoXml(xml, "infoTributaria/secuencial")) : G.ExtraerValorDeNodoXml(xml, "Clave");
                        factura.ConsecutivoHacienda = Pais == "E" ? (G.ExtraerValorDeNodoXml(xml, "infoTributaria/estab") + "-" + G.ExtraerValorDeNodoXml(xml, "infoTributaria/ptoEmi") + "-" + G.ExtraerValorDeNodoXml(xml, "infoTributaria/secuencial")) : G.ExtraerValorDeNodoXml(xml, "NumeroConsecutivo");
                        if (Pais == "E")
                        {
                            string _FechaEmision = G.ExtraerValorDeNodoXml(xml, "infoFactura/fechaEmision");
                            string[] Array_FechaEmision = _FechaEmision.Split('/');
                            var FechaEmision2 = "";
                            if (Array_FechaEmision.Length == 3)
                            {
                                FechaEmision2 = Array_FechaEmision[2] + "/" + Array_FechaEmision[1] + "/" + Array_FechaEmision[0];
                            }

                            factura.FecFactura = DateTime.Parse(FechaEmision2);

                        }
                        else
                        {
                            factura.FecFactura = DateTime.Parse(G.ExtraerValorDeNodoXml(xml, "FechaEmision"));

                        }
                        factura.CodigoActividadEconomica = Pais == "E" ? "" : G.ExtraerValorDeNodoXml(xml, "CodigoActividad");
                        factura.FechaGravado = DateTime.Now;
                        factura.CodEmpresa = G.ObtenerCedulaJuridia();

                        try
                        {
                            factura.NumFactura = Pais == "E" ? int.Parse(G.ExtraerValorDeNodoXml(xml, "infoTributaria/secuencial")) : int.Parse(factura.ConsecutivoHacienda.Substring(10, 10));
                        }
                        catch (Exception ex)
                        {
                            factura.NumFactura = Pais == "E" ? int.Parse(G.ExtraerValorDeNodoXml(xml, "infoTributaria/secuencial")) : int.Parse(factura.ConsecutivoHacienda.Substring(11, 9));

                        }

                        factura.TipoDocumento = Pais == "E" ? "01" : factura.ConsecutivoHacienda.Substring(8, 2);
                        if (factura.TipoDocumento == "04")
                            throw new Exception($"El documento es un Tiquete Electrónico, que no puede ser utilizado como gasto deducible. Debe solicitar al proveedor que genere una Factura Electrónica.");


                        //Informacion del Proveedor o emisor de la factura

                        factura.CodProveedor = Pais == "E" ? G.ExtraerValorDeNodoXml(xml, "infoTributaria/ruc") : G.ExtraerValorDeNodoXml(xml, "Emisor/Identificacion/Numero");

                        // si el nombre se pasa de 80 caracteres debemos cortarlo
                        factura.ConsecutivoHacienda = factura.ConsecutivoHacienda.TrimEnd();
                        if (db.EncCompras.Where(m => m.CodEmpresa == factura.CodEmpresa
                   && m.CodProveedor == factura.CodProveedor
                   && m.ConsecutivoHacienda.Contains(factura.ConsecutivoHacienda)
                   && m.TipoDocumento == factura.TipoDocumento).Count() > 0)
                        {
                            throw new Exception($"El documento ya existe [Clave={factura.ClaveHacienda}] [Consecutivo={factura.ConsecutivoHacienda}]");
                        }

                        //Información del Cliente o Receptor de la factura
                        factura.TipoIdentificacionCliente = Pais == "E" ? "04" : G.ExtraerValorDeNodoXml(xml, "Receptor/Identificacion/Tipo");
                        factura.CodCliente = Pais == "E" ? G.ExtraerValorDeNodoXml(xml, "infoFactura/identificacionComprador") : G.ExtraerValorDeNodoXml(xml, "Receptor/Identificacion/Numero");
                        factura.NomCliente = Pais == "E" ? G.ExtraerValorDeNodoXml(xml, "infoFactura/razonSocialComprador") : G.ExtraerValorDeNodoXml(xml, "Receptor/Nombre");
                        if (factura.NomCliente.Length > 50)
                            factura.NomCliente = factura.NomCliente.Substring(0, 50);
                        factura.EmailCliente = Pais == "E" ? "" : G.ExtraerValorDeNodoXml(xml, "Receptor/CorreoElectronico");

                        factura.CondicionVenta = Pais == "E" ? "" : G.ExtraerValorDeNodoXml(xml, "CondicionVenta");

                        if (factura.CodCliente != factura.CodEmpresa)
                        {
                            throw new Exception($"El documento no fue dirigido para esta compañia [Empresa={factura.CodEmpresa}] [Cliente de Factura={factura.CodCliente}]");
                        }

                        try
                        {
                            factura.DiasCredito = Pais == "E" ? 0 : int.Parse(G.ExtraerValorDeNodoXml(xml, "PlazoCredito", true));
                        }
                        catch
                        {
                            factura.DiasCredito = 0;
                        }

                        factura.MedioPago = Pais == "E" ? "" : G.ExtraerValorDeNodoXml(xml, "MedioPago");
                        if (attachmentBody.Contains("xml-schemas/v4.3"))
                        {
                            factura.CodMoneda = G.ExtraerValorDeNodoXml(xml, "ResumenFactura/CodigoTipoMoneda/CodigoMoneda");

                        }
                        else
                        {
                            factura.CodMoneda = Pais == "E" ? "USD" : G.ExtraerValorDeNodoXml(xml, "ResumenFactura/CodigoMoneda");
                            if (Pais == "E" && factura.CodMoneda == "DOLAR")
                            {
                                factura.CodMoneda = "USD";
                            }

                            if (Pais == "E" && string.IsNullOrWhiteSpace(factura.CodMoneda))
                            {
                                factura.CodMoneda = "USD";
                            }
                        }

                        if (string.IsNullOrWhiteSpace(factura.CodMoneda))
                        {
                            factura.CodMoneda = "CRC";

                        }

                        factura.TotalServGravados = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalServGravados", true));
                        factura.TotalServExentos = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalServExentos", true));
                        factura.TotalMercanciasGravadas = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalMercanciasGravadas", true));
                        factura.TotalMercanciasExentas = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalMercanciasExentas", true));

                        factura.TotalServExonerado = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalServExonerado", true));
                        factura.TotalMercExonerada = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalMercExonerada", true));
                        factura.TotalExonerado = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalExonerado", true));
                        factura.TotalIVADevuelto = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalIVADevuelto", true));
                        factura.TotalOtrosCargos = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "OtrosCargos/MontoCargo", true));
                        try
                        {

                        }
                        catch (Exception)
                        {

                            throw;
                        }

                        factura.TotalExento = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalExento", true));

                        if (Pais == "E")
                        {
                            try
                            {
                                factura.TotalVenta = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/importeTotal", true));
                            }
                            catch (Exception)
                            {

                                factura.TotalVenta = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/importeTotal", true).Replace(".", ","));
                            }

                        }
                        else
                        {
                            factura.TotalVenta = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalVenta", true));
                        }

                        if (Pais == "E")
                        {
                            try
                            {
                                factura.TotalDescuentos = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/totalDescuento", true));

                            }
                            catch (Exception)
                            {

                                factura.TotalDescuentos = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/totalDescuento", true).Replace(".", ","));

                            }

                        }
                        else
                        {
                            factura.TotalDescuentos = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalDescuentos", true));

                        }

                        if (Pais == "E")
                        {
                            try
                            {
                                factura.TotalVentaNeta = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/importeTotal", true));


                            }
                            catch (Exception)
                            {

                                factura.TotalVentaNeta = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/importeTotal", true).Replace(".", ","));


                            }

                        }
                        else
                        {
                            factura.TotalVentaNeta = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalVentaNeta", true));


                        }


                        if (Pais == "E")
                        {
                            try
                            {
                                factura.TotalImpuesto = (factura.TotalVentaNeta - decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/totalSinImpuestos", true)));



                            }
                            catch (Exception)
                            {


                                factura.TotalImpuesto = (factura.TotalVentaNeta - decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/totalSinImpuestos", true).Replace(".", ",")));


                            }

                        }
                        else
                        {


                            factura.TotalImpuesto = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalImpuesto", true));

                        }

                        if (Pais == "E")
                        {
                            try
                            {
                                factura.TotalComprobante = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/importeTotal", true));




                            }
                            catch (Exception)
                            {



                                factura.TotalComprobante = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/importeTotal", true).Replace(".", ","));


                            }

                        }
                        else
                        {



                            factura.TotalComprobante = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalComprobante", true));

                        }


                        var NomProveedor = Pais == "E" ? G.ExtraerValorDeNodoXml(xml, "infoTributaria/razonSocial") : G.ExtraerValorDeNodoXml(xml, "Emisor/Nombre");
                        factura.XmlFacturaRecibida = G.StringToBase64(xmlBase64);
                        factura.NomProveedor = NomProveedor;
                        Random i = new Random();
                        int o = i.Next(0, 10000);
                        var pdfResp = G.GuardarPDF(item.Pdf, G.ObtenerCedulaJuridia(), o + "_" + factura.NumFactura.ToString());

                        factura.PdfFactura = pdfResp;
                        factura.PdfFac = item.Pdf;
                        decimal iva1 = 0;
                        decimal iva2 = 0;
                        decimal iva4 = 0;
                        decimal iva8 = 0;
                        decimal iva13 = 0;

                        if (Pais == "E")
                        {
                            try
                            {
                                iva4 += decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/propina", true));



                            }
                            catch (Exception)
                            {

                                try
                                {
                                    iva4 += decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/propina", true).Replace(".", ","));

                                }
                                catch (Exception)
                                {


                                }


                            }

                        }
                        if (Pais == "E")
                        {
                            try
                            {
                                iva8 += decimal.Parse(G.ExtraerValorDeNodoXml(xml, "otrosRubrosTerceros/rubro/total", true));



                            }
                            catch (Exception)
                            {

                                try
                                {
                                    iva8 += decimal.Parse(G.ExtraerValorDeNodoXml(xml, "otrosRubrosTerceros/rubro/total", true).Replace(".", ","));

                                }
                                catch (Exception)
                                {


                                }


                            }

                        }
                        List<DetCompras> detCpmpras = new List<DetCompras>();

                        if (Pais == "E")
                        {
                            var NumLinea = 0;
                            foreach (var item2 in xml.Elements().Where(m => m.Name.LocalName == "detalles").Elements())
                            {
                                var det = new DetCompras();
                                det.CodEmpresa = factura.CodEmpresa;
                                det.NumFactura = factura.NumFactura;
                                det.CodProveedor = factura.CodProveedor;
                                det.TipoDocumento = factura.TipoDocumento;
                                det.ClaveHacienda = factura.ClaveHacienda;
                                det.ConsecutivoHacienda = factura.ConsecutivoHacienda;
                                det.NomProveedor = NomProveedor;
                                det.NumLinea = NumLinea;
                                NumLinea++;



                                det.CodPro = G.ExtraerValorDeNodoXml(item2, "codigoPrincipal");
                                if (det.CodPro.Length > 20)
                                    det.CodPro = det.CodPro.Substring(0, 20);


                                det.NomPro = G.ExtraerValorDeNodoXml(item2, "descripcion");
                                det.CodCabys = "";
                                if (det.NomPro.Length > 60)
                                    det.NomPro = det.NomPro.Substring(0, 60);


                                det.UnidadMedida = "Unid";
                                try
                                {
                                    var Decimal = Convert.ToDecimal(G.ExtraerValorDeNodoXml(item2, "cantidad", true));
                                    det.Cantidad = Convert.ToInt32(Decimal);
                                }
                                catch (Exception)
                                {

                                    var Decimal = Convert.ToDecimal(G.ExtraerValorDeNodoXml(item2, "cantidad", true).Replace(".", ","));

                                    det.Cantidad = Convert.ToInt32(Decimal);
                                }

                                try
                                {
                                    det.PrecioUnitario = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "precioUnitario", true));

                                }
                                catch (Exception)
                                {

                                    det.PrecioUnitario = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "precioUnitario", true).Replace(".", ","));

                                }
                                decimal MontoTotalImpuestos = 0;
                                var Tarifa = "";
                                foreach (var item3 in item2.Elements().Where(a => a.Name.LocalName == "impuestos").Elements())
                                {
                                    try
                                    {
                                        MontoTotalImpuestos += decimal.Parse(G.ExtraerValorDeNodoXml(item3, "valor", true));

                                    }
                                    catch (Exception)
                                    {

                                        MontoTotalImpuestos += decimal.Parse(G.ExtraerValorDeNodoXml(item3, "valor", true).Replace(".", ","));

                                    }
                                    Tarifa = G.ExtraerValorDeNodoXml(item3, "tarifa");
                                }

                                try
                                {
                                    det.MontoTotal = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "MontoTotal", true)) + MontoTotalImpuestos;

                                }
                                catch (Exception)
                                {

                                    det.MontoTotal = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "MontoTotal", true).Replace(".", ",")) + MontoTotalImpuestos;

                                }

                                try
                                {
                                    det.MontoDescuento = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "descuento", true));

                                }
                                catch (Exception)
                                {

                                    det.MontoDescuento = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "descuento", true).Replace(".", ","));

                                }
                                try
                                {
                                    det.SubTotal = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "precioTotalSinImpuesto", true));

                                }
                                catch (Exception)
                                {

                                    det.SubTotal = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "precioTotalSinImpuesto", true).Replace(".", ","));

                                }

                                //Impuesto

                                try
                                {
                                    det.ImpuestoTarifa = decimal.Parse(Tarifa);

                                }
                                catch (Exception)
                                {
                                    det.ImpuestoTarifa = decimal.Parse(Tarifa.Replace(".", ","));

                                }
                                det.ImpuestoMonto = MontoTotalImpuestos;



                                det.idTipoGasto = 0;


                                det.MontoTotalLinea = det.MontoTotal;


                                var ExoneracionPorcentajeCompra = 0;

                                int opcion = Convert.ToInt32(det.ImpuestoTarifa);
                                decimal cantidadImpuesto = 0;
                                bool bandera = false;
                                if (ExoneracionPorcentajeCompra > 0)
                                {
                                    bandera = true;
                                    cantidadImpuesto = opcion - ExoneracionPorcentajeCompra;
                                }
                                switch (opcion)
                                {
                                    case 12:
                                        {
                                            if (!bandera)
                                            {
                                                iva13 += det.ImpuestoMonto.Value;
                                            }
                                            else
                                            {
                                                if (cantidadImpuesto > 0)
                                                {
                                                    iva13 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                                }
                                            }
                                            break;
                                        }
                                    case 15:
                                        {
                                            if (!bandera)
                                            {
                                                iva1 += det.ImpuestoMonto.Value;
                                            }
                                            else
                                            {
                                                if (cantidadImpuesto > 0)
                                                {
                                                    iva1 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                                }
                                            }
                                            break;
                                        }

                                }


                                db.DetCompras.Add(det);
                                detCpmpras.Add(det);
                            }
                        }
                        else
                        {
                            foreach (var item2 in xml.Elements().Where(m => m.Name.LocalName == "DetalleServicio").Elements())
                            {
                                var det = new DetCompras();
                                det.CodEmpresa = factura.CodEmpresa;
                                det.NumFactura = factura.NumFactura;
                                det.CodProveedor = factura.CodProveedor;
                                det.TipoDocumento = factura.TipoDocumento;
                                det.ClaveHacienda = factura.ClaveHacienda;
                                det.ConsecutivoHacienda = factura.ConsecutivoHacienda;
                                det.NomProveedor = NomProveedor;
                                det.NumLinea = short.Parse(G.ExtraerValorDeNodoXml(item2, "NumeroLinea"));

                                if (attachmentBody.Contains("xml-schemas/v4.3"))
                                {
                                    det.CodPro = G.ExtraerValorDeNodoXml(item2, "CodigoComercial/Codigo");
                                    if (det.CodPro.Length > 20)
                                        det.CodPro = det.CodPro.Substring(0, 20);
                                }
                                else
                                {
                                    det.CodPro = G.ExtraerValorDeNodoXml(item2, "Codigo/Codigo");
                                    if (det.CodPro.Length > 20)
                                        det.CodPro = det.CodPro.Substring(0, 20);
                                }

                                det.NomPro = G.ExtraerValorDeNodoXml(item2, "Detalle");
                                det.CodCabys = G.ExtraerValorDeNodoXml(item2, "Codigo");
                                if (det.NomPro.Length > 60)
                                    det.NomPro = det.NomPro.Substring(0, 60);


                                det.UnidadMedida = G.ExtraerValorDeNodoXml(item2, "UnidadMedida");
                                var Decimal = Convert.ToDecimal(G.ExtraerValorDeNodoXml(item2, "Cantidad", true));
                                det.Cantidad = Convert.ToInt32(Decimal);
                                det.PrecioUnitario = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "PrecioUnitario", true));
                                det.MontoTotal = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "MontoTotal", true));

                                det.MontoDescuento = decimal.Parse(G.ExtraerValorDeNodoXml(item2.Elements().Where(a => a.Name.LocalName == "Descuento").FirstOrDefault(), "MontoDescuento", true));
                                det.SubTotal = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "SubTotal", true));

                                //Impuesto

                                det.ImpuestoTarifa = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "Impuesto/Tarifa", true));
                                det.ImpuestoMonto = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "Impuesto/Monto", true));



                                det.idTipoGasto = EncontrarGasto(db, det.CodCabys);


                                det.MontoTotalLinea = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "MontoTotalLinea", true));


                                var ExoneracionPorcentajeCompra = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "Impuesto/Exoneracion/PorcentajeCompra", true));

                                int opcion = Convert.ToInt32(det.ImpuestoTarifa);
                                decimal cantidadImpuesto = 0;
                                bool bandera = false;
                                if (ExoneracionPorcentajeCompra > 0)
                                {
                                    bandera = true;
                                    cantidadImpuesto = opcion - ExoneracionPorcentajeCompra;
                                }
                                switch (opcion)
                                {
                                    case 1:
                                        {
                                            if (!bandera)
                                            {
                                                iva1 += det.ImpuestoMonto.Value;
                                            }
                                            else
                                            {
                                                if (cantidadImpuesto > 0)
                                                {
                                                    iva1 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                                }
                                            }
                                            break;
                                        }
                                    case 2:
                                        {
                                            if (!bandera)
                                            {
                                                iva2 += det.ImpuestoMonto.Value;
                                            }
                                            else
                                            {
                                                if (cantidadImpuesto > 0)
                                                {
                                                    iva2 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                                }
                                            }
                                            break;
                                        }
                                    case 4:
                                        {
                                            if (!bandera)
                                            {
                                                iva4 += det.ImpuestoMonto.Value;

                                            }
                                            else
                                            {
                                                if (cantidadImpuesto > 0)
                                                {
                                                    iva4 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                                }
                                            }
                                            break;
                                        }
                                    case 8:
                                        {
                                            if (!bandera)
                                            {
                                                iva8 += det.ImpuestoMonto.Value;
                                            }
                                            else
                                            {
                                                if (cantidadImpuesto > 0)
                                                {
                                                    iva8 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                                }
                                            }
                                            break;
                                        }
                                    case 13:
                                        {
                                            if (!bandera)
                                            {
                                                iva13 += det.ImpuestoMonto.Value;
                                            }
                                            else
                                            {
                                                if (cantidadImpuesto > 0)
                                                {
                                                    iva13 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                                }
                                            }
                                            break;
                                        }
                                }


                                db.DetCompras.Add(det);
                                detCpmpras.Add(det);
                            }
                        }



                        factura.Impuesto1 = iva1;
                        factura.Impuesto2 = iva2;
                        factura.Impuesto4 = iva4;
                        factura.Impuesto8 = iva8;
                        factura.Impuesto13 = iva13;
                        factura.idCierre = 0;
                        factura.RegimenSimplificado = false;
                        factura.FacturaExterior = false;
                        factura.GastosVarios = false;
                        factura.FacturaNoRecibida = false;
                        factura.Comentario = "";
                        try
                        {
                            string SQL = " Select top 1 t0.LicTradNum , t0.cardcode,t0.CardName, ";
                            SQL += " case when LicTradNum Like '0%' then SUBSTRING( Replace(LicTradNum, '-',''),2,LEN(Replace(LicTradNum, '-','')) - 1)  ";
                            SQL += " else Replace(LicTradNum, '-','') end  Cedula from ocrd t0  where t0.CardType = 's' and case when LicTradNum Like '0%' then SUBSTRING( Replace(LicTradNum, '-',''),2,LEN(Replace(LicTradNum, '-','')) - 1)  ";
                            SQL += " else Replace(LicTradNum, '-','') end = '" + factura.CodProveedor + "' ";

                            Conexion g = new Conexion();
                            SqlConnection Cn = new SqlConnection(g.DevuelveCadena());
                            SqlCommand Cmd = new SqlCommand(SQL, Cn);
                            SqlDataAdapter Da = new SqlDataAdapter(Cmd);
                            DataSet Ds = new DataSet();
                            Cn.Open();
                            Da.Fill(Ds, "Proveedor");

                            factura.CardCode = Ds.Tables["Proveedor"].Rows[0]["cardcode"].ToString();
                            Cn.Close();
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                Conexion.Desconectar();
                                var client = (SAPbobsCOM.BusinessPartners)Conexion.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);
                                client.CardName = factura.NomProveedor;
                                client.Valid = BoYesNoEnum.tYES;
                                client.CardType = BoCardTypes.cSupplier;
                                client.Currency = "##";
                                client.FederalTaxID = factura.CodProveedor;
                                client.Series = 76;
                                client.GroupCode = 101;
                                client.EmailAddress = G.ExtraerValorDeNodoXml(xml, "Emisor/CorreoElectronico");
                                client.Phone1 = G.ExtraerValorDeNodoXml(xml, "Emisor/Telefono/NumTelefono");
                                client.UserFields.Fields.Item("U_DYD_Actividad").Value = G.ExtraerValorDeNodoXml(xml, "CodigoActividad").ToString();


                                var Provincia = Convert.ToInt32(G.ExtraerValorDeNodoXml(xml, "Emisor/Ubicacion/Provincia").ToString());
                                var Canton = Convert.ToInt32(G.ExtraerValorDeNodoXml(xml, "Emisor/Ubicacion/Canton").ToString());
                                var Distrito = Convert.ToInt32(G.ExtraerValorDeNodoXml(xml, "Emisor/Ubicacion/Distrito").ToString());
                                var Barrio = Convert.ToInt32(G.ExtraerValorDeNodoXml(xml, "Emisor/Ubicacion/Barrio").ToString());

                                var C = "";
                                var D = "";
                                var B = "";


                                Conexion g = new Conexion();
                                SqlConnection Cn = new SqlConnection(g.DevuelveCadena());
                                SqlCommand Cmd = new SqlCommand("Select * from Cantones where CodCanton = '" + Canton + "' and CodProvincia = '" + Provincia + "'", Cn);
                                SqlDataAdapter Da = new SqlDataAdapter(Cmd);
                                DataSet Ds = new DataSet();
                                Cn.Open();
                                Da.Fill(Ds, "Cantones1");
                                C = Ds.Tables["Cantones1"].Rows[0]["NomCanton"].ToString();

                                Cn.Close();

                                SqlCommand CmdD = new SqlCommand("Select * from Distritos where CodCanton = '" + Canton + "' and CodProvincia = '" + Provincia + "'" + " and CodDistrito = '" + Distrito + "'", Cn);
                                SqlDataAdapter DaD = new SqlDataAdapter(CmdD);
                                DataSet DsD = new DataSet();
                                Cn.Open();
                                DaD.Fill(DsD, "Distritos1");
                                D = DsD.Tables["Distritos1"].Rows[0]["NomDistrito"].ToString();

                                Cn.Close();


                                SqlCommand CmdB = new SqlCommand("Select * from Barrios where CodCanton = '" + Canton + "' and CodProvincia = '" + Provincia + "'" + " and CodDistrito = '" + Distrito + "' and CodBarrio = '" + Barrio + "'", Cn);
                                SqlDataAdapter DaB = new SqlDataAdapter(CmdB);
                                DataSet DsB = new DataSet();
                                Cn.Open();
                                DaB.Fill(DsB, "Barrios1");
                                B = DsB.Tables["Barrios1"].Rows[0]["NomBarrio"].ToString();

                                Cn.Close();


                                client.Addresses.Add();
                                client.Addresses.SetCurrentLine(0);



                                client.City = C;
                                client.County = C;
                                client.Block = D;
                                client.Address = G.ExtraerValorDeNodoXml(xml, "Emisor/Ubicacion/OtrasSenas").ToString().Length > 49 ? G.ExtraerValorDeNodoXml(xml, "Emisor/Ubicacion/OtrasSenas").ToString().Substring(0, 49) : G.ExtraerValorDeNodoXml(xml, "Emisor/Ubicacion/OtrasSenas").ToString();

                                client.Addresses.AddressName = client.Address;
                                client.Addresses.City = C;
                                client.Addresses.County = C;
                                client.Addresses.State = G.ExtraerValorDeNodoXml(xml, "Emisor/Ubicacion/Provincia").ToString();
                                client.Addresses.Street = G.ExtraerValorDeNodoXml(xml, "Emisor/Ubicacion/OtrasSenas").ToString().Length > 99 ? G.ExtraerValorDeNodoXml(xml, "Emisor/Ubicacion/OtrasSenas").ToString().Substring(0, 99) : G.ExtraerValorDeNodoXml(xml, "Emisor/Ubicacion/OtrasSenas").ToString();
                                client.Addresses.Block = D;

                                client.Addresses.TypeOfAddress = "S";

                                var respuest = client.Add();

                                if (respuest != 0)
                                {
                                    BitacoraErrores error = new BitacoraErrores();
                                    error.Descripcion = Conexion.Company.GetLastErrorDescription();
                                    error.StackTrace = "Insercion del proveedor en la factura " + factura.ConsecutivoHacienda;
                                    error.Fecha = DateTime.Now;
                                    db.BitacoraErrores.Add(error);
                                    db.SaveChanges();
                                    Conexion.Desconectar();
                                    factura.CardCode = "";
                                }
                                else
                                {
                                    try
                                    {
                                        string SQL = " Select t0.LicTradNum , t0.cardcode,t0.CardName, ";
                                        SQL += " case when LicTradNum Like '0%' then SUBSTRING( Replace(LicTradNum, '-',''),2,LEN(Replace(LicTradNum, '-','')) - 1)  ";
                                        SQL += " else Replace(LicTradNum, '-','') end  Cedula from ocrd t0  where t0.CardType = 's' and case when LicTradNum Like '0%' then SUBSTRING( Replace(LicTradNum, '-',''),2,LEN(Replace(LicTradNum, '-','')) - 1)  ";
                                        SQL += " else Replace(LicTradNum, '-','') end = '" + factura.CodProveedor + "' ";


                                        SqlConnection Cn2 = new SqlConnection(g.DevuelveCadena());
                                        SqlCommand Cmd2 = new SqlCommand(SQL, Cn2);
                                        SqlDataAdapter Da2 = new SqlDataAdapter(Cmd2);
                                        DataSet Ds2 = new DataSet();
                                        Cn2.Open();
                                        Da2.Fill(Ds2, "Proveedor");

                                        factura.CardCode = Ds2.Tables["Proveedor"].Rows[0]["CardCode"].ToString();
                                        Cn2.Close();
                                    }
                                    catch (Exception rr)
                                    {
                                        BitacoraErrores error = new BitacoraErrores();
                                        error.Descripcion = rr.Message + " " + " No se pudo crear el proveedor";
                                        error.StackTrace = "NO se encontro el proveedor en la factura " + factura.ConsecutivoHacienda;
                                        error.Fecha = DateTime.Now;
                                        db.BitacoraErrores.Add(error);
                                        db.SaveChanges();
                                        factura.CardCode = "";
                                    }
                                }

                            }
                            catch (Exception e)
                            {
                                try
                                {
                                    BitacoraErrores error = new BitacoraErrores();
                                    error.Descripcion = e.Message + " " + " al hacer un proveedor " + Conexion.Company.GetLastErrorDescription();
                                    error.StackTrace = e.StackTrace;
                                    error.Fecha = DateTime.Now;

                                    db.BitacoraErrores.Add(error);
                                    db.SaveChanges();
                                    Conexion.Desconectar();

                                }
                                catch (Exception ex4)
                                {


                                }

                            }


                        }
                        factura.idTipoGasto = detCpmpras.Where(a => a.NumFactura == factura.NumFactura && a.ClaveHacienda == factura.ClaveHacienda && a.ConsecutivoHacienda == factura.ConsecutivoHacienda).FirstOrDefault() == null ? 0 : detCpmpras.Where(a => a.NumFactura == factura.NumFactura && a.ClaveHacienda == factura.ClaveHacienda && a.ConsecutivoHacienda == factura.ConsecutivoHacienda).FirstOrDefault().idTipoGasto;
                        db.EncCompras.Add(factura);
                        db.Database.ExecuteSqlCommand("Update BandejaEntrada SET Procesado=1 WHERE Id=@Id",
                           new SqlParameter("@Id", item.Id));
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            string procesado = "1";

                            db.Database.ExecuteSqlCommand("Update BandejaEntrada SET Mensaje=@Mensaje, Procesado=@Procesado WHERE Id=@Id",
                                 new SqlParameter("@Mensaje", ex.Message + " -> " + ex.StackTrace),
                                 new SqlParameter("@Procesado", procesado),
                                 new SqlParameter("@Id", item.Id));


                            db.SaveChanges();


                        }
                        catch
                        {
                        }

                    }

                }

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {


                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Lectura de Bandeja";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        [Route("api/Compras/RealizarLecturaXMLCarpeta")]

        public async Task<HttpResponseMessage> GetRealizarLecturaXMLCarpetaAsync()
        {
            try
            {
                G.AbrirConexionAPP(out db);
                var Compañia = G.ObtenerCedulaJuridia();

                var Licencia = dbLogin.LicEmpresas.Where(a => a.CedulaJuridica == Compañia).FirstOrDefault();

                var Pais = Licencia.CadenaConexionSAP;

                string carpeta = G.ObtenerConfig("CarpetaXML"); // Ruta de la carpeta que contiene los archivos XML

                // Enumerar los archivos XML en la carpeta
                string[] archivosXml = Directory.GetFiles(carpeta, "*.xml");

                foreach (string archivoXml in archivosXml)
                {
                    var nombre = "";
                    byte[] ByteArrayPDF;



                    try
                    {
                        // Leer el archivo XML
                        XmlDocument documentoXml = new XmlDocument();
                        documentoXml.Load(archivoXml);
                        nombre = Path.GetFileName(documentoXml.BaseURI);




                        string xmlBase64 = documentoXml.InnerXml.ToString();
                        xmlBase64 = xmlBase64.Replace("\n", "");

                        if (Pais == "E")
                        {
                            string palabraInicio = "<comprobante><![CDATA[";
                            string palabraFin = "]]></comprobante>";

                            // Encontrar las posiciones de las palabras clave
                            int indiceInicio = xmlBase64.IndexOf(palabraInicio);
                            if (indiceInicio == -1)
                            {
                                palabraInicio = "<comprobante> <![CDATA[";
                                indiceInicio = xmlBase64.IndexOf(palabraInicio);
                            }
                            int indiceFin = xmlBase64.IndexOf(palabraFin);
                            string subcadena = "";
                            // Verificar si se encontraron ambas palabras clave
                            if (indiceInicio != -1 && indiceFin != -1)
                            {
                                // Calcular la longitud de la subcadena
                                int longitudSubcadena = indiceFin - (indiceInicio + palabraInicio.Length);

                                // Extraer la subcadena
                                subcadena = xmlBase64.Substring(indiceInicio + palabraInicio.Length, longitudSubcadena);
                                xmlBase64 = subcadena;

                            }
                        }


                        var xml = G.ConvertirArchivoaXElement(xmlBase64.Trim(), G.ObtenerCedulaJuridia());


                        if (!xmlBase64.Contains("FacturaElectronica") && !xmlBase64.Contains("comprobante")
                            && !xmlBase64.Contains("TiqueteElectronico")
                            && !xmlBase64.Contains("NotaCreditoElectronica")
                            && !xmlBase64.Contains("NotaDebitoElectronica"))
                            throw new Exception("No es un documento electrónico");


                        EncCompras factura = new EncCompras();
                        factura.ClaveHacienda = Pais == "E" ? (G.ExtraerValorDeNodoXml(xml, "infoTributaria/estab") + "-" + G.ExtraerValorDeNodoXml(xml, "infoTributaria/ptoEmi") + "-" + G.ExtraerValorDeNodoXml(xml, "infoTributaria/secuencial")) : G.ExtraerValorDeNodoXml(xml, "Clave");
                        factura.ConsecutivoHacienda = Pais == "E" ? (G.ExtraerValorDeNodoXml(xml, "infoTributaria/estab") + "-" + G.ExtraerValorDeNodoXml(xml, "infoTributaria/ptoEmi") + "-" + G.ExtraerValorDeNodoXml(xml, "infoTributaria/secuencial")) : G.ExtraerValorDeNodoXml(xml, "NumeroConsecutivo");
                        if (Pais == "E")
                        {
                            string _FechaEmision = G.ExtraerValorDeNodoXml(xml, "infoFactura/fechaEmision");
                            string[] Array_FechaEmision = _FechaEmision.Split('/');
                            var FechaEmision2 = "";
                            if (Array_FechaEmision.Length == 3)
                            {
                                FechaEmision2 = Array_FechaEmision[2] + "/" + Array_FechaEmision[1] + "/" + Array_FechaEmision[0];
                            }

                            factura.FecFactura = DateTime.Parse(FechaEmision2);

                        }
                        else
                        {
                            factura.FecFactura = DateTime.Parse(G.ExtraerValorDeNodoXml(xml, "FechaEmision"));

                        }
                        factura.CodigoActividadEconomica = Pais == "E" ? "" : G.ExtraerValorDeNodoXml(xml, "CodigoActividad");
                        factura.FechaGravado = DateTime.Now;
                        factura.CodEmpresa = G.ObtenerCedulaJuridia();

                        try
                        {
                            factura.NumFactura = Pais == "E" ? int.Parse(G.ExtraerValorDeNodoXml(xml, "infoTributaria/secuencial")) : int.Parse(factura.ConsecutivoHacienda.Substring(10, 10));
                        }
                        catch (Exception ex)
                        {
                            factura.NumFactura = Pais == "E" ? int.Parse(G.ExtraerValorDeNodoXml(xml, "infoTributaria/secuencial")) : int.Parse(factura.ConsecutivoHacienda.Substring(11, 9));

                        }

                        factura.TipoDocumento = Pais == "E" ? "01" : factura.ConsecutivoHacienda.Substring(8, 2);
                        if (factura.TipoDocumento == "04")
                            throw new Exception($"El documento es un Tiquete Electrónico, que no puede ser utilizado como gasto deducible. Debe solicitar al proveedor que genere una Factura Electrónica.");


                        //Informacion del Proveedor o emisor de la factura

                        factura.CodProveedor = Pais == "E" ? G.ExtraerValorDeNodoXml(xml, "infoTributaria/ruc") : G.ExtraerValorDeNodoXml(xml, "Emisor/Identificacion/Numero");

                        // si el nombre se pasa de 80 caracteres debemos cortarlo
                        factura.ConsecutivoHacienda = factura.ConsecutivoHacienda.TrimEnd();
                        if (db.EncCompras.Where(m => m.CodEmpresa == factura.CodEmpresa
                   && m.CodProveedor == factura.CodProveedor
                   && m.ConsecutivoHacienda == factura.ConsecutivoHacienda
                   && m.TipoDocumento == factura.TipoDocumento).Count() > 0)
                        {
                            throw new Exception($"El documento ya existe [Clave={factura.ClaveHacienda}] [Consecutivo={factura.ConsecutivoHacienda}]");
                        }

                        //Información del Cliente o Receptor de la factura
                        factura.TipoIdentificacionCliente = Pais == "E" ? "04" : G.ExtraerValorDeNodoXml(xml, "Receptor/Identificacion/Tipo");
                        factura.CodCliente = Pais == "E" ? G.ExtraerValorDeNodoXml(xml, "infoFactura/identificacionComprador") : G.ExtraerValorDeNodoXml(xml, "Receptor/Identificacion/Numero");
                        factura.NomCliente = Pais == "E" ? G.ExtraerValorDeNodoXml(xml, "infoFactura/razonSocialComprador") : G.ExtraerValorDeNodoXml(xml, "Receptor/Nombre");
                        if (factura.NomCliente.Length > 50)
                            factura.NomCliente = factura.NomCliente.Substring(0, 50);
                        factura.EmailCliente = Pais == "E" ? "" : G.ExtraerValorDeNodoXml(xml, "Receptor/CorreoElectronico");

                        factura.CondicionVenta = Pais == "E" ? "" : G.ExtraerValorDeNodoXml(xml, "CondicionVenta");

                        if (factura.CodCliente != factura.CodEmpresa)
                        {
                            throw new Exception($"El documento no fue dirigido para esta compañia [Empresa={factura.CodEmpresa}] [Cliente de Factura={factura.CodCliente}]");
                        }

                        try
                        {
                            factura.DiasCredito = Pais == "E" ? 0 : int.Parse(G.ExtraerValorDeNodoXml(xml, "PlazoCredito", true));
                        }
                        catch
                        {
                            factura.DiasCredito = 0;
                        }

                        factura.MedioPago = Pais == "E" ? "" : G.ExtraerValorDeNodoXml(xml, "MedioPago");
                        if (xmlBase64.Contains("xml-schemas/v4.3"))
                        {
                            factura.CodMoneda = G.ExtraerValorDeNodoXml(xml, "ResumenFactura/CodigoTipoMoneda/CodigoMoneda");

                        }
                        else
                        {
                            factura.CodMoneda = Pais == "E" ? "USD" : G.ExtraerValorDeNodoXml(xml, "ResumenFactura/CodigoMoneda");
                            if (Pais == "E" && factura.CodMoneda == "DOLAR")
                            {
                                factura.CodMoneda = "USD";
                            }

                            if (Pais == "E" && string.IsNullOrWhiteSpace(factura.CodMoneda))
                            {
                                factura.CodMoneda = "USD";
                            }
                        }

                        if (string.IsNullOrWhiteSpace(factura.CodMoneda))
                        {
                            factura.CodMoneda = "CRC";

                        }

                        factura.TotalServGravados = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalServGravados", true));
                        factura.TotalServExentos = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalServExentos", true));
                        factura.TotalMercanciasGravadas = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalMercanciasGravadas", true));
                        factura.TotalMercanciasExentas = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalMercanciasExentas", true));

                        factura.TotalServExonerado = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalServExonerado", true));
                        factura.TotalMercExonerada = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalMercExonerada", true));
                        factura.TotalExonerado = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalExonerado", true));
                        factura.TotalIVADevuelto = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalIVADevuelto", true));
                        factura.TotalOtrosCargos = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "OtrosCargos/MontoCargo", true));
                        try
                        {

                        }
                        catch (Exception)
                        {

                            throw;
                        }

                        factura.TotalExento = Pais == "E" ? 0 : decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalExento", true));

                        if (Pais == "E")
                        {
                            try
                            {
                                factura.TotalVenta = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/importeTotal", true));
                            }
                            catch (Exception)
                            {

                                factura.TotalVenta = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/importeTotal", true).Replace(".", ","));
                            }

                        }
                        else
                        {
                            factura.TotalVenta = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalVenta", true));
                        }

                        if (Pais == "E")
                        {
                            try
                            {
                                factura.TotalDescuentos = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/totalDescuento", true));

                            }
                            catch (Exception)
                            {

                                factura.TotalDescuentos = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/totalDescuento", true).Replace(".", ","));

                            }

                        }
                        else
                        {
                            factura.TotalDescuentos = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalDescuentos", true));

                        }

                        if (Pais == "E")
                        {
                            try
                            {
                                factura.TotalVentaNeta = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/importeTotal", true));


                            }
                            catch (Exception)
                            {

                                factura.TotalVentaNeta = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/importeTotal", true).Replace(".", ","));


                            }

                        }
                        else
                        {
                            factura.TotalVentaNeta = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalVentaNeta", true));


                        }


                        if (Pais == "E")
                        {
                            try
                            {
                                factura.TotalImpuesto = (factura.TotalVentaNeta - decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/totalSinImpuestos", true)));



                            }
                            catch (Exception)
                            {


                                factura.TotalImpuesto = (factura.TotalVentaNeta - decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/totalSinImpuestos", true).Replace(".", ",")));


                            }

                        }
                        else
                        {


                            factura.TotalImpuesto = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalImpuesto", true));

                        }

                        if (Pais == "E")
                        {
                            try
                            {
                                factura.TotalComprobante = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/importeTotal", true));




                            }
                            catch (Exception)
                            {



                                factura.TotalComprobante = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/importeTotal", true).Replace(".", ","));


                            }

                        }
                        else
                        {



                            factura.TotalComprobante = decimal.Parse(G.ExtraerValorDeNodoXml(xml, "ResumenFactura/TotalComprobante", true));

                        }


                        var NomProveedor = Pais == "E" ? G.ExtraerValorDeNodoXml(xml, "infoTributaria/razonSocial") : G.ExtraerValorDeNodoXml(xml, "Emisor/Nombre");
                        factura.XmlFacturaRecibida = G.StringToBase64(xmlBase64);
                        factura.NomProveedor = NomProveedor;
                        Random i = new Random();
                        int o = i.Next(0, 10000);
                        var pdfResp = "";
                        try
                        {
                            string[] archivosPdf = Directory.GetFiles(carpeta, nombre.Replace(".xml", "") + ".pdf");

                            if (archivosPdf.Length > 0)
                            {
                                using (MemoryStream stream = new MemoryStream())
                                {
                                    using (PdfReader reader = new PdfReader(archivosPdf[0]))
                                    {
                                        // Crear un escritor de PDF para el stream
                                        using (PdfStamper stamper = new PdfStamper(reader, stream))
                                        {
                                            // No se hacen cambios en el documento, simplemente se copia
                                        }
                                    }

                                    // Convertir el contenido del MemoryStream a un arreglo de bytes
                                    ByteArrayPDF = stream.ToArray();
                                    pdfResp = G.GuardarPDF(ByteArrayPDF, G.ObtenerCedulaJuridia(), o + "_" + factura.NumFactura.ToString());
                                    factura.PdfFac = ByteArrayPDF;
                                    try
                                    {
                                        // Eliminar el archivo
                                        File.Delete(archivosPdf[0]);
                                    }
                                    catch (Exception)
                                    {


                                    }
                                }
                            }

                        }
                        catch (Exception)
                        {


                        }

                        factura.PdfFactura = pdfResp;
                        if (pdfResp == "")
                        {
                            factura.PdfFac = null;

                        }
                        decimal iva1 = 0;
                        decimal iva2 = 0;
                        decimal iva4 = 0;
                        decimal iva8 = 0;
                        decimal iva13 = 0;


                        if (Pais == "E")
                        {
                            try
                            {
                                iva4 += decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/propina", true));



                            }
                            catch (Exception)
                            {

                                try
                                {
                                    iva4 += decimal.Parse(G.ExtraerValorDeNodoXml(xml, "infoFactura/propina", true).Replace(".", ","));

                                }
                                catch (Exception)
                                {


                                }


                            }

                        }
                        if (Pais == "E")
                        {
                            try
                            {
                                iva8 += decimal.Parse(G.ExtraerValorDeNodoXml(xml, "otrosRubrosTerceros/rubro/total", true));



                            }
                            catch (Exception)
                            {

                                try
                                {
                                    iva8 += decimal.Parse(G.ExtraerValorDeNodoXml(xml, "otrosRubrosTerceros/rubro/total", true).Replace(".", ","));

                                }
                                catch (Exception)
                                {


                                }


                            }

                        }
                        List<DetCompras> detCpmpras = new List<DetCompras>();

                        if (Pais == "E")
                        {
                            var NumLinea = 0;
                            foreach (var item2 in xml.Elements().Where(m => m.Name.LocalName == "detalles").Elements())
                            {
                                var det = new DetCompras();
                                det.CodEmpresa = factura.CodEmpresa;
                                det.NumFactura = factura.NumFactura;
                                det.CodProveedor = factura.CodProveedor;
                                det.TipoDocumento = factura.TipoDocumento;
                                det.ClaveHacienda = factura.ClaveHacienda;
                                det.ConsecutivoHacienda = factura.ConsecutivoHacienda;
                                det.NomProveedor = NomProveedor;
                                det.NumLinea = NumLinea;
                                NumLinea++;



                                det.CodPro = G.ExtraerValorDeNodoXml(item2, "codigoPrincipal");
                                if (det.CodPro.Length > 20)
                                    det.CodPro = det.CodPro.Substring(0, 20);


                                det.NomPro = G.ExtraerValorDeNodoXml(item2, "descripcion");
                                det.CodCabys = "";
                                if (det.NomPro.Length > 60)
                                    det.NomPro = det.NomPro.Substring(0, 60);


                                det.UnidadMedida = "Unid";
                                try
                                {
                                    var Decimal = Convert.ToDecimal(G.ExtraerValorDeNodoXml(item2, "cantidad", true));
                                    det.Cantidad = Convert.ToInt32(Decimal);
                                }
                                catch (Exception)
                                {

                                    var Decimal = Convert.ToDecimal(G.ExtraerValorDeNodoXml(item2, "cantidad", true).Replace(".", ","));

                                    det.Cantidad = Convert.ToInt32(Decimal);
                                }

                                try
                                {
                                    det.PrecioUnitario = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "precioUnitario", true));

                                }
                                catch (Exception)
                                {

                                    det.PrecioUnitario = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "precioUnitario", true).Replace(".", ","));

                                }
                                decimal MontoTotalImpuestos = 0;
                                var Tarifa = "";
                                foreach (var item3 in item2.Elements().Where(a => a.Name.LocalName == "impuestos").Elements())
                                {
                                    try
                                    {
                                        MontoTotalImpuestos += decimal.Parse(G.ExtraerValorDeNodoXml(item3, "valor", true));

                                    }
                                    catch (Exception)
                                    {

                                        MontoTotalImpuestos += decimal.Parse(G.ExtraerValorDeNodoXml(item3, "valor", true).Replace(".", ","));

                                    }
                                    Tarifa = G.ExtraerValorDeNodoXml(item3, "tarifa");
                                }

                                try
                                {
                                    det.MontoTotal = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "MontoTotal", true)) + MontoTotalImpuestos;

                                }
                                catch (Exception)
                                {

                                    det.MontoTotal = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "MontoTotal", true).Replace(".", ",")) + MontoTotalImpuestos;

                                }

                                try
                                {
                                    det.MontoDescuento = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "descuento", true));

                                }
                                catch (Exception)
                                {

                                    det.MontoDescuento = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "descuento", true).Replace(".", ","));

                                }
                                try
                                {
                                    det.SubTotal = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "precioTotalSinImpuesto", true));

                                }
                                catch (Exception)
                                {

                                    det.SubTotal = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "precioTotalSinImpuesto", true).Replace(".", ","));

                                }

                                //Impuesto

                                try
                                {
                                    det.ImpuestoTarifa = decimal.Parse(Tarifa);

                                }
                                catch (Exception)
                                {
                                    det.ImpuestoTarifa = decimal.Parse(Tarifa.Replace(".", ","));

                                }
                                det.ImpuestoMonto = MontoTotalImpuestos;



                                det.idTipoGasto = 0;


                                det.MontoTotalLinea = det.MontoTotal;


                                var ExoneracionPorcentajeCompra = 0;

                                int opcion = Convert.ToInt32(det.ImpuestoTarifa);
                                decimal cantidadImpuesto = 0;
                                bool bandera = false;
                                if (ExoneracionPorcentajeCompra > 0)
                                {
                                    bandera = true;
                                    cantidadImpuesto = opcion - ExoneracionPorcentajeCompra;
                                }
                                switch (opcion)
                                {
                                    case 12:
                                        {
                                            if (!bandera)
                                            {
                                                iva13 += det.ImpuestoMonto.Value;
                                            }
                                            else
                                            {
                                                if (cantidadImpuesto > 0)
                                                {
                                                    iva13 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                                }
                                            }
                                            break;
                                        }
                                    case 15:
                                        {
                                            if (!bandera)
                                            {
                                                iva1 += det.ImpuestoMonto.Value;
                                            }
                                            else
                                            {
                                                if (cantidadImpuesto > 0)
                                                {
                                                    iva1 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                                }
                                            }
                                            break;
                                        }

                                }


                                db.DetCompras.Add(det);
                                detCpmpras.Add(det);
                            }
                        }
                        else
                        {
                            foreach (var item2 in xml.Elements().Where(m => m.Name.LocalName == "DetalleServicio").Elements())
                            {
                                var det = new DetCompras();
                                det.CodEmpresa = factura.CodEmpresa;
                                det.NumFactura = factura.NumFactura;
                                det.CodProveedor = factura.CodProveedor;
                                det.TipoDocumento = factura.TipoDocumento;
                                det.ClaveHacienda = factura.ClaveHacienda;
                                det.ConsecutivoHacienda = factura.ConsecutivoHacienda;
                                det.NomProveedor = NomProveedor;
                                det.NumLinea = short.Parse(G.ExtraerValorDeNodoXml(item2, "NumeroLinea"));

                                if (xmlBase64.Contains("xml-schemas/v4.3"))
                                {
                                    det.CodPro = G.ExtraerValorDeNodoXml(item2, "CodigoComercial/Codigo");
                                    if (det.CodPro.Length > 20)
                                        det.CodPro = det.CodPro.Substring(0, 20);
                                }
                                else
                                {
                                    det.CodPro = G.ExtraerValorDeNodoXml(item2, "Codigo/Codigo");
                                    if (det.CodPro.Length > 20)
                                        det.CodPro = det.CodPro.Substring(0, 20);
                                }

                                det.NomPro = G.ExtraerValorDeNodoXml(item2, "Detalle");
                                det.CodCabys = G.ExtraerValorDeNodoXml(item2, "Codigo");
                                if (det.NomPro.Length > 60)
                                    det.NomPro = det.NomPro.Substring(0, 60);


                                det.UnidadMedida = G.ExtraerValorDeNodoXml(item2, "UnidadMedida");
                                var Decimal = Convert.ToDecimal(G.ExtraerValorDeNodoXml(item2, "Cantidad", true));
                                det.Cantidad = Convert.ToInt32(Decimal);
                                det.PrecioUnitario = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "PrecioUnitario", true));
                                det.MontoTotal = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "MontoTotal", true));

                                det.MontoDescuento = decimal.Parse(G.ExtraerValorDeNodoXml(item2.Elements().Where(a => a.Name.LocalName == "Descuento").FirstOrDefault(), "MontoDescuento", true));
                                det.SubTotal = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "SubTotal", true));

                                //Impuesto

                                det.ImpuestoTarifa = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "Impuesto/Tarifa", true));
                                det.ImpuestoMonto = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "Impuesto/Monto", true));



                                det.idTipoGasto = EncontrarGasto(db, det.CodCabys);


                                det.MontoTotalLinea = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "MontoTotalLinea", true));


                                var ExoneracionPorcentajeCompra = decimal.Parse(G.ExtraerValorDeNodoXml(item2, "Impuesto/Exoneracion/PorcentajeCompra", true));

                                int opcion = Convert.ToInt32(det.ImpuestoTarifa);
                                decimal cantidadImpuesto = 0;
                                bool bandera = false;
                                if (ExoneracionPorcentajeCompra > 0)
                                {
                                    bandera = true;
                                    cantidadImpuesto = opcion - ExoneracionPorcentajeCompra;
                                }
                                switch (opcion)
                                {
                                    case 1:
                                        {
                                            if (!bandera)
                                            {
                                                iva1 += det.ImpuestoMonto.Value;
                                            }
                                            else
                                            {
                                                if (cantidadImpuesto > 0)
                                                {
                                                    iva1 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                                }
                                            }
                                            break;
                                        }
                                    case 2:
                                        {
                                            if (!bandera)
                                            {
                                                iva2 += det.ImpuestoMonto.Value;
                                            }
                                            else
                                            {
                                                if (cantidadImpuesto > 0)
                                                {
                                                    iva2 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                                }
                                            }
                                            break;
                                        }
                                    case 4:
                                        {
                                            if (!bandera)
                                            {
                                                iva4 += det.ImpuestoMonto.Value;

                                            }
                                            else
                                            {
                                                if (cantidadImpuesto > 0)
                                                {
                                                    iva4 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                                }
                                            }
                                            break;
                                        }
                                    case 8:
                                        {
                                            if (!bandera)
                                            {
                                                iva8 += det.ImpuestoMonto.Value;
                                            }
                                            else
                                            {
                                                if (cantidadImpuesto > 0)
                                                {
                                                    iva8 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                                }
                                            }
                                            break;
                                        }
                                    case 13:
                                        {
                                            if (!bandera)
                                            {
                                                iva13 += det.ImpuestoMonto.Value;
                                            }
                                            else
                                            {
                                                if (cantidadImpuesto > 0)
                                                {
                                                    iva13 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                                }
                                            }
                                            break;
                                        }
                                }


                                db.DetCompras.Add(det);
                                detCpmpras.Add(det);
                            }
                        }



                        factura.Impuesto1 = iva1;
                        factura.Impuesto2 = iva2;
                        factura.Impuesto4 = iva4;
                        factura.Impuesto8 = iva8;
                        factura.Impuesto13 = iva13;
                        factura.idCierre = 0;
                        factura.RegimenSimplificado = false;
                        factura.FacturaExterior = false;
                        factura.GastosVarios = false;
                        factura.FacturaNoRecibida = false;
                        factura.Comentario = "";
                        factura.idTipoGasto = detCpmpras.Where(a => a.NumFactura == factura.NumFactura && a.ClaveHacienda == factura.ClaveHacienda && a.ConsecutivoHacienda == factura.ConsecutivoHacienda).FirstOrDefault() == null ? 0 : detCpmpras.Where(a => a.NumFactura == factura.NumFactura && a.ClaveHacienda == factura.ClaveHacienda && a.ConsecutivoHacienda == factura.ConsecutivoHacienda).FirstOrDefault().idTipoGasto;
                        db.EncCompras.Add(factura);
                        db.SaveChanges();

                        try
                        {
                            // Eliminar el archivo
                            File.Delete(archivoXml);
                        }
                        catch (Exception)
                        {


                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            var destino = G.ObtenerConfig("CarpetaXMLError") + "\\" + nombre;
                            // Mover el archivo
                            File.Move(archivoXml, destino);
                        }
                        catch (Exception)
                        {


                        }

                        BitacoraErrores be = new BitacoraErrores();
                        be.Descripcion = ex.Message;
                        be.StackTrace = ex.StackTrace;
                        be.Metodo = "Lectura de XML en Carpeta";
                        be.Fecha = DateTime.Now;
                        db.BitacoraErrores.Add(be);
                        db.SaveChanges();

                    }




                }

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {

                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Lectura de XML en Carpeta";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        [Route("api/Compras/RealizarLecturaXMLCarpetaGuatemala")]

        public async Task<HttpResponseMessage> GetRealizarLecturaXMLCarpetaGuatemalaAsync()
        {
            try
            {
                G.AbrirConexionAPP(out db);
                var Compañia = G.ObtenerCedulaJuridia();

                var Licencia = dbLogin.LicEmpresas.Where(a => a.CedulaJuridica == Compañia).FirstOrDefault();

                var Pais = Licencia.CadenaConexionSAP;

                string carpeta = G.ObtenerConfig("CarpetaXMLGuatemala"); // Ruta de la carpeta que contiene los archivos XML

                // Enumerar los archivos XML en la carpeta
                string[] archivosXml = Directory.GetFiles(carpeta, "*.xml");

                foreach (string archivoXml in archivosXml)
                {
                    var nombre = "";
                    byte[] ByteArrayPDF;



                    try
                    {
                        // Leer el archivo XML
                        XmlDocument documentoXml = new XmlDocument();
                        documentoXml.Load(archivoXml);
                        nombre = Path.GetFileName(documentoXml.BaseURI);




                        string xmlBase64 = documentoXml.InnerXml.ToString();
                        xmlBase64 = xmlBase64.Replace("\n", "");

                        if (Pais == "G")
                        {

                        }


                        var xml = G.ConvertirArchivoaXElement(xmlBase64.Trim(), G.ObtenerCedulaJuridia());


                        if (!xmlBase64.Contains("FacturaElectronica") && !xmlBase64.Contains("GTDocumento")
                            && !xmlBase64.Contains("TiqueteElectronico")
                            && !xmlBase64.Contains("NotaCreditoElectronica")
                            && !xmlBase64.Contains("NotaDebitoElectronica"))
                            throw new Exception("No es un documento electrónico");

                        // Acceder a elementos específicos
                        XmlNamespaceManager nsmgr = new XmlNamespaceManager(documentoXml.NameTable);
                        nsmgr.AddNamespace("dte", "http://www.sat.gob.gt/dte/fel/0.2.0");

                        EncCompras factura = new EncCompras();

                        // Buscar el nodo NumeroAutorizacion
                        XmlNode autorizacionNode = documentoXml.SelectSingleNode("//dte:NumeroAutorizacion", nsmgr);

                        factura.ClaveHacienda = autorizacionNode.Attributes["Numero"].Value;
                        factura.ConsecutivoHacienda = factura.ClaveHacienda;

                        XmlNode datosEmisionNode = documentoXml.SelectSingleNode("//dte:DatosEmision", nsmgr);
                        if (Pais == "G")
                        {
                            string _FechaEmision = datosEmisionNode.SelectSingleNode("dte:DatosGenerales", nsmgr)?.Attributes["FechaHoraEmision"]?.Value;
                            //string[] Array_FechaEmision = _FechaEmision.Split('-');
                            //var FechaEmision2 = "";
                            //if (Array_FechaEmision.Length >= 3)
                            //{
                            //    FechaEmision2 = Array_FechaEmision[0] + "/" + Array_FechaEmision[1] + "/" + Array_FechaEmision[2];
                            //}

                            factura.FecFactura = DateTime.Parse(_FechaEmision);

                        }

                        factura.CodigoActividadEconomica = "";
                        factura.FechaGravado = DateTime.Now;
                        factura.CodEmpresa = G.ObtenerCedulaJuridia();


                        try
                        {
                            factura.NumFactura = int.Parse(autorizacionNode.Attributes["Numero"].Value);
                        }
                        catch (Exception ex)
                        {
                            factura.NumFactura = 0;

                        }

                        factura.TipoDocumento = "01";
                        if (factura.TipoDocumento == "04")
                            throw new Exception($"El documento es un Tiquete Electrónico, que no puede ser utilizado como gasto deducible. Debe solicitar al proveedor que genere una Factura Electrónica.");


                        //Informacion del Proveedor o emisor de la factura

                        factura.CodProveedor = datosEmisionNode.SelectSingleNode("dte:Emisor", nsmgr)?.Attributes["NITEmisor"]?.Value;

                        // si el nombre se pasa de 80 caracteres debemos cortarlo
                        factura.ConsecutivoHacienda = factura.ConsecutivoHacienda.TrimEnd();
                        if (db.EncCompras.Where(m => m.CodEmpresa == factura.CodEmpresa
                   && m.CodProveedor == factura.CodProveedor
                   && m.ConsecutivoHacienda == factura.ConsecutivoHacienda
                   && m.TipoDocumento == factura.TipoDocumento).Count() > 0)
                        {
                            throw new Exception($"El documento ya existe [Clave={factura.ClaveHacienda}] [Consecutivo={factura.ConsecutivoHacienda}]");
                        }

                        //Información del Cliente o Receptor de la factura
                        factura.TipoIdentificacionCliente = "04";
                        factura.CodCliente = datosEmisionNode.SelectSingleNode("dte:Receptor", nsmgr)?.Attributes["IDReceptor"]?.Value;
                        factura.NomCliente = datosEmisionNode.SelectSingleNode("dte:Receptor", nsmgr)?.Attributes["NombreReceptor"]?.Value;
                        if (factura.NomCliente.Length > 50)
                            factura.NomCliente = factura.NomCliente.Substring(0, 50);
                        factura.EmailCliente = datosEmisionNode.SelectSingleNode("dte:Receptor", nsmgr)?.Attributes["CorreoReceptor"]?.Value;

                        factura.CondicionVenta = "";

                        if (factura.CodCliente != factura.CodEmpresa)
                        {
                            throw new Exception($"El documento no fue dirigido para esta compañia [Empresa={factura.CodEmpresa}] [Cliente de Factura={factura.CodCliente}]");
                        }

                        try
                        {
                            factura.DiasCredito = 0;
                        }
                        catch
                        {
                            factura.DiasCredito = 0;
                        }

                        factura.MedioPago = "";

                        factura.CodMoneda = datosEmisionNode.SelectSingleNode("dte:DatosGenerales", nsmgr)?.Attributes["CodigoMoneda"]?.Value;



                        if (string.IsNullOrWhiteSpace(factura.CodMoneda))
                        {
                            factura.CodMoneda = "CRC";

                        }

                        factura.TotalServGravados = 0;
                        factura.TotalServExentos = 0;
                        factura.TotalMercanciasGravadas = 0;
                        factura.TotalMercanciasExentas = 0;

                        factura.TotalServExonerado = 0;
                        factura.TotalMercExonerada = 0;
                        factura.TotalExonerado = 0;
                        factura.TotalIVADevuelto = 0;
                        factura.TotalOtrosCargos = 0;


                        factura.TotalExento = 0;




                        var NomProveedor = datosEmisionNode.SelectSingleNode("dte:Emisor", nsmgr)?.Attributes["NombreEmisor"]?.Value;
                        factura.XmlFacturaRecibida = G.StringToBase64(xmlBase64);
                        factura.NomProveedor = NomProveedor;
                        Random i = new Random();
                        int o = i.Next(0, 10000);
                        var pdfResp = "";
                        try
                        {
                            string[] archivosPdf = Directory.GetFiles(carpeta, nombre.Replace(".xml", "") + ".pdf");

                            if (archivosPdf.Length > 0)
                            {
                                using (MemoryStream stream = new MemoryStream())
                                {
                                    using (PdfReader reader = new PdfReader(archivosPdf[0]))
                                    {
                                        // Crear un escritor de PDF para el stream
                                        using (PdfStamper stamper = new PdfStamper(reader, stream))
                                        {
                                            // No se hacen cambios en el documento, simplemente se copia
                                        }
                                    }

                                    // Convertir el contenido del MemoryStream a un arreglo de bytes
                                    ByteArrayPDF = stream.ToArray();
                                    pdfResp = G.GuardarPDF(ByteArrayPDF, G.ObtenerCedulaJuridia(), o + "_" + factura.NumFactura.ToString());
                                    factura.PdfFac = ByteArrayPDF;
                                    try
                                    {
                                        // Eliminar el archivo
                                        File.Delete(archivosPdf[0]);
                                    }
                                    catch (Exception ex)
                                    {

                                        G.GuardarTxt("BitacoraBorradoCarpeta.txt", ex.ToString());
                                    }
                                }
                            }

                        }
                        catch (Exception)
                        {


                        }

                        factura.PdfFactura = pdfResp;
                        if (pdfResp == "")
                        {
                            factura.PdfFac = null;

                        }
                        decimal iva1 = 0;
                        decimal iva2 = 0;
                        decimal iva4 = 0;
                        decimal iva8 = 0;
                        decimal iva13 = 0;




                        List<DetCompras> detCpmpras = new List<DetCompras>();

                        if (Pais == "G")
                        {
                            var NumLinea = 0;
                            // Seleccionar todos los nodos de Item
                            XmlNodeList itemNodes = documentoXml.SelectNodes("//dte:Item", nsmgr);
                            foreach (XmlNode itemNode in itemNodes)
                            {
                                var det = new DetCompras();
                                det.CodEmpresa = factura.CodEmpresa;
                                det.NumFactura = factura.NumFactura;
                                det.CodProveedor = factura.CodProveedor;
                                det.TipoDocumento = factura.TipoDocumento;
                                det.ClaveHacienda = factura.ClaveHacienda;
                                det.ConsecutivoHacienda = factura.ConsecutivoHacienda;
                                det.NomProveedor = NomProveedor;
                                det.NumLinea = NumLinea;
                                NumLinea++;



                                det.CodPro = "";
                                if (det.CodPro.Length > 20)
                                    det.CodPro = det.CodPro.Substring(0, 20);


                                det.NomPro = itemNode.SelectSingleNode("dte:Descripcion", nsmgr)?.InnerText;
                                det.CodCabys = "";
                                if (det.NomPro.Length > 60)
                                    det.NomPro = det.NomPro.Substring(0, 60);


                                det.UnidadMedida = itemNode.SelectSingleNode("dte:UnidadMedida", nsmgr)?.InnerText;
                                try
                                {
                                    var Decimal = Convert.ToDecimal(itemNode.SelectSingleNode("dte:Cantidad", nsmgr)?.InnerText);
                                    det.Cantidad = Convert.ToInt32(Decimal);
                                }
                                catch (Exception)
                                {

                                    var Decimal = Convert.ToDecimal(itemNode.SelectSingleNode("dte:Cantidad", nsmgr)?.InnerText.Replace(".", ","));

                                    det.Cantidad = Convert.ToInt32(Decimal);
                                }

                                try
                                {
                                    det.PrecioUnitario = decimal.Parse(itemNode.SelectSingleNode("dte:PrecioUnitario", nsmgr)?.InnerText);

                                }
                                catch (Exception)
                                {

                                    det.PrecioUnitario = decimal.Parse(itemNode.SelectSingleNode("dte:PrecioUnitario", nsmgr)?.InnerText.Replace(".", ","));

                                }
                                decimal MontoTotalImpuestos = 0;
                                var Tarifa = "12";
                                XmlNodeList impuestoNodes = itemNode.SelectNodes("dte:Impuestos/dte:Impuesto", nsmgr);
                                foreach (XmlNode impuestoNode in impuestoNodes)
                                {
                                    try
                                    {

                                        var NombreIVA = impuestoNode.SelectSingleNode("dte:NombreCorto", nsmgr)?.InnerText;
                                        switch(NombreIVA)
                                        {
                                            case "IVA":
                                                {
                                                    iva13 += decimal.Parse(impuestoNode.SelectSingleNode("dte:MontoImpuesto", nsmgr)?.InnerText);
                                                    break;
                                                }
                                            case "PETROLEO":
                                                {
                                                    iva8 += decimal.Parse(impuestoNode.SelectSingleNode("dte:MontoImpuesto", nsmgr)?.InnerText);
                                                    break;
                                                }
                                            case "TURISMO HOSPEDAJE":
                                                {
                                                    iva4 += decimal.Parse(impuestoNode.SelectSingleNode("dte:MontoImpuesto", nsmgr)?.InnerText);

                                                    break;
                                                }
                                        }
                                        MontoTotalImpuestos += decimal.Parse(impuestoNode.SelectSingleNode("dte:MontoImpuesto", nsmgr)?.InnerText);

                                    }
                                    catch (Exception)
                                    {
                                        var NombreIVA = impuestoNode.SelectSingleNode("dte:NombreCorto", nsmgr)?.InnerText;
                                        switch (NombreIVA)
                                        {
                                            case "IVA":
                                                {
                                                    iva13 += decimal.Parse(impuestoNode.SelectSingleNode("dte:MontoImpuesto", nsmgr)?.InnerText.Replace(".", ","));
                                                    break;
                                                }
                                            case "PETROLEO":
                                                {
                                                    iva8 += decimal.Parse(impuestoNode.SelectSingleNode("dte:MontoImpuesto", nsmgr)?.InnerText.Replace(".", ","));
                                                    break;
                                                }
                                            case "TURISMO HOSPEDAJE":
                                                {
                                                    iva4 += decimal.Parse(impuestoNode.SelectSingleNode("dte:MontoImpuesto", nsmgr)?.InnerText.Replace(".", ","));

                                                    break;
                                                }
                                        }
                                        MontoTotalImpuestos += decimal.Parse(impuestoNode.SelectSingleNode("dte:MontoImpuesto", nsmgr)?.InnerText.Replace(".", ","));

                                    }
                                    // Tarifa = G.ExtraerValorDeNodoXml(item3, "tarifa");
                                }

                                try
                                {
                                    det.MontoTotal = decimal.Parse(itemNode.SelectSingleNode("dte:Total", nsmgr)?.InnerText);

                                }
                                catch (Exception)
                                {

                                    det.MontoTotal = decimal.Parse(itemNode.SelectSingleNode("dte:Total", nsmgr)?.InnerText.Replace(".", ","));

                                }

                                try
                                {
                                    det.MontoDescuento = decimal.Parse(itemNode.SelectSingleNode("dte:Descuento", nsmgr)?.InnerText);

                                }
                                catch (Exception)
                                {

                                    det.MontoDescuento = decimal.Parse(itemNode.SelectSingleNode("dte:Descuento", nsmgr)?.InnerText.Replace(".", ","));

                                }
                                try
                                {
                                    det.SubTotal = det.MontoTotal - MontoTotalImpuestos;

                                }
                                catch (Exception)
                                {

                                    det.SubTotal = det.MontoTotal - MontoTotalImpuestos;

                                }

                                //Impuesto

                                try
                                {
                                    det.ImpuestoTarifa = decimal.Parse(Tarifa);

                                }
                                catch (Exception)
                                {
                                    det.ImpuestoTarifa = decimal.Parse(Tarifa.Replace(".", ","));

                                }
                                det.ImpuestoMonto = MontoTotalImpuestos;



                                det.idTipoGasto = 0;


                                det.MontoTotalLinea = det.MontoTotal;


                                //var ExoneracionPorcentajeCompra = 0;

                                //int opcion = Convert.ToInt32(det.ImpuestoTarifa);
                                //decimal cantidadImpuesto = 0;
                                //bool bandera = false;
                                //if (ExoneracionPorcentajeCompra > 0)
                                //{
                                //    bandera = true;
                                //    cantidadImpuesto = opcion - ExoneracionPorcentajeCompra;
                                //}
                                //switch (opcion)
                                //{
                                //    case 12:
                                //        {
                                //            if (!bandera)
                                //            {
                                //                iva13 += det.ImpuestoMonto.Value;
                                //            }
                                //            else
                                //            {
                                //                if (cantidadImpuesto > 0)
                                //                {
                                //                    iva13 += ((det.SubTotal.Value - det.MontoDescuento.Value) * (cantidadImpuesto / 100));
                                //                }
                                //            }
                                //            break;
                                //        }


                                //}


                                db.DetCompras.Add(det);
                                detCpmpras.Add(det);
                            }
                        }




                        factura.Impuesto1 = iva1;
                        factura.Impuesto2 = iva2;
                        factura.Impuesto4 = iva4;
                        factura.Impuesto8 = iva8;
                        factura.Impuesto13 = iva13;
                        factura.idCierre = 0;
                        factura.RegimenSimplificado = false;
                        factura.FacturaExterior = false;
                        factura.GastosVarios = false;
                        factura.FacturaNoRecibida = false;
                        factura.Comentario = "";
                        factura.idTipoGasto = detCpmpras.Where(a => a.NumFactura == factura.NumFactura && a.ClaveHacienda == factura.ClaveHacienda && a.ConsecutivoHacienda == factura.ConsecutivoHacienda).FirstOrDefault() == null ? 0 : detCpmpras.Where(a => a.NumFactura == factura.NumFactura && a.ClaveHacienda == factura.ClaveHacienda && a.ConsecutivoHacienda == factura.ConsecutivoHacienda).FirstOrDefault().idTipoGasto;

                        XmlNode granTotalNode = documentoXml.SelectSingleNode("//dte:GranTotal", nsmgr);
                        try
                        {
                            factura.TotalVenta = decimal.Parse(granTotalNode.InnerText);
                        }
                        catch (Exception)
                        {

                            factura.TotalVenta = decimal.Parse(granTotalNode.InnerText.Replace(".", ","));
                        }

                        factura.TotalDescuentos = detCpmpras.Sum(a => a.MontoDescuento).Value;

                        factura.TotalVentaNeta = detCpmpras.Sum(a => a.MontoTotal).Value;

                        factura.TotalImpuesto = detCpmpras.Sum(a => a.ImpuestoMonto).Value;

                        factura.TotalComprobante = detCpmpras.Sum(a => a.MontoTotal).Value; 
                        db.EncCompras.Add(factura);
                        db.SaveChanges();

                        try
                        {
                            // Eliminar el archivo
                            File.Delete(archivoXml);
                        }
                        catch (Exception ex)
                        {

                            G.GuardarTxt("BitacoraBorradoCarpeta.txt", ex.ToString());

                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            var destino = G.ObtenerConfig("CarpetaXMLError") + "\\" + nombre;
                            // Mover el archivo
                            File.Move(archivoXml, destino);
                        }
                        catch (Exception ex2)
                        {
                            G.GuardarTxt("BitacoraBorradoCarpeta.txt", ex2.ToString());


                        }

                        BitacoraErrores be = new BitacoraErrores();
                        be.Descripcion = ex.Message;
                        be.StackTrace = ex.StackTrace;
                        be.Metodo = "Lectura de XML en Carpeta Guatemala";
                        be.Fecha = DateTime.Now;
                        db.BitacoraErrores.Add(be);
                        db.SaveChanges();

                    }




                }

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {

                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Lectura de XML en Carpeta Guatemala";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }



        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                G.AbrirConexionAPP(out db);
                DateTime time = new DateTime();
                if (filtro.FechaInicio != time)
                {
                    filtro.FechaFinal = filtro.FechaFinal.AddDays(1);

                }
                var EncCompras = db.EncCompras.Select(a => new
                {
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
                    a.XmlFacturaRecibida

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
                    PdfFactura = db.Parametros.FirstOrDefault().UrlImagenesApp + a.PdfFactura
                 ,
                    a.idNormaReparto
                 ,
                    a.idTipoGasto
                 ,
                    TipoGasto = (a.idTipoGasto == 0 ? "Sin Asignar" : db.Gastos.Where(z => z.idTipoGasto == a.idTipoGasto).FirstOrDefault().Nombre),
                    a.idCierre,
                    a.Impuesto1,
                    a.Impuesto2,
                    a.Impuesto4,
                    a.Impuesto8,
                    a.Impuesto13,
                    PdfFac = "",
                    a.Comentario,
                    a.ImagenB64,
                    DetCompras = db.DetCompras.Where(d => d.NumFactura == a.NumFactura && d.TipoDocumento == a.TipoDocumento && d.ClaveHacienda == a.ClaveHacienda && d.ConsecutivoHacienda == a.ConsecutivoHacienda).ToList()

                }).Where(a => (filtro.FechaInicio != time ? a.FecFactura >= filtro.FechaInicio && a.FecFactura <= filtro.FechaFinal : true) && (!string.IsNullOrEmpty(filtro.Texto) ? a.ConsecutivoHacienda.ToString().Contains(filtro.Texto.ToUpper()) ||
                    a.ClaveHacienda.ToString().Contains(filtro.Texto.ToUpper()) : true) && (filtro.Asignados ? (filtro.Codigo2 > 0 ? a.idLoginAsignado == null || a.idLoginAsignado == 0 || a.idLoginAsignado == filtro.Codigo2 : a.idLoginAsignado == null || a.idLoginAsignado == 0) : true)).ToList();

                //if (!string.IsNullOrEmpty(filtro.Texto))
                //{


                //    EncCompras = EncCompras.Where(a => a.ConsecutivoHacienda.ToString().Contains(filtro.Texto.ToUpper()) ||
                //    a.ClaveHacienda.ToString().Contains(filtro.Texto.ToUpper())
                //    ).ToList();
                //}



                //if (filtro.Asignados)

                //{
                //    if (filtro.Codigo2 > 0)
                //    {

                //        EncCompras = EncCompras.Where(a => a.idLoginAsignado == null || a.idLoginAsignado == 0 || a.idLoginAsignado == filtro.Codigo2).ToList();



                //    }
                //    else
                //    {
                //        EncCompras = EncCompras.Where(a => a.idLoginAsignado == null || a.idLoginAsignado == 0).ToList();
                //    }
                //}

                if (filtro.Codigo3 > 0)
                {
                    //EncCompras = EncCompras.Where(a => a.idCierre == filtro.Codigo3).ToList();
                }






                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, EncCompras);

            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Error de GET Compras";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/Compras/Listado")]
        public async Task<HttpResponseMessage> GetComprasModulo([FromUri] Filtros filtro)
        {
            try
            {
                G.AbrirConexionAPP(out db);
                DateTime time = new DateTime();

                if (filtro.FechaInicio != time)
                {
                    filtro.FechaFinal = filtro.FechaFinal.AddDays(1);
                    // EncCompras = EncCompras.Where(a => a.FecFactura >= filtro.FechaInicio && a.FecFactura <= filtro.FechaFinal).ToList();
                }


                var EncCompras = db.EncCompras.Select(a => new
                {
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
                    PdfFactura = db.Parametros.FirstOrDefault().UrlImagenesApp + a.PdfFactura
                 ,
                    a.idNormaReparto
                 ,
                    a.idTipoGasto
                 ,
                    TipoGasto = (a.idTipoGasto == 0 ? "Sin Asignar" : db.Gastos.Where(z => z.idTipoGasto == a.idTipoGasto).FirstOrDefault().Nombre),
                    a.idCierre,
                    a.Impuesto1,
                    a.Impuesto2,
                    a.Impuesto4,
                    a.Impuesto8,
                    a.Impuesto13,
                    PdfFac = "",
                    a.RegimenSimplificado,
                    a.FacturaExterior,
                    a.GastosVarios,
                    a.FacturaNoRecibida,
                    a.Comentario,
                    a.ImagenB64,
                    Usuario = (a.idCierre == 0 ? 0 : db.EncCierre.Where(z => z.idCierre == a.idCierre).FirstOrDefault().idLogin),
                    DetCompras = db.DetCompras.Where(d => d.NumFactura == a.NumFactura && d.TipoDocumento == a.TipoDocumento && d.ClaveHacienda == a.ClaveHacienda && d.ConsecutivoHacienda == a.ConsecutivoHacienda).ToList()

                }).Where(a => (filtro.FechaInicio != time ? a.FecFactura >= filtro.FechaInicio && a.FecFactura <= filtro.FechaFinal : true) && (filtro.NumCierre > 0 ? a.idCierre == filtro.NumCierre : true) && (!string.IsNullOrEmpty(filtro.Texto) ? a.ConsecutivoHacienda.ToString().Contains(filtro.Texto.ToUpper()) ||
                    a.ClaveHacienda.ToString().Contains(filtro.Texto.ToUpper()) : true) && (filtro.Asignados ? a.idLoginAsignado == null || a.idLoginAsignado == 0 : true)).ToList();

                //if (!string.IsNullOrEmpty(filtro.Texto))
                //{


                //    EncCompras = EncCompras.Where(a => a.ConsecutivoHacienda.ToString().Contains(filtro.Texto.ToUpper()) ||
                //    a.ClaveHacienda.ToString().Contains(filtro.Texto.ToUpper())

                //    ).ToList();
                //}

                if (!string.IsNullOrEmpty(filtro.Texto2))
                {
                    EncCompras = EncCompras.Where(a => a.NomProveedor.ToString().ToUpper().Contains(filtro.Texto2.ToUpper())

                   ).ToList();
                }




                //if (filtro.Asignados)

                //{

                //    EncCompras = EncCompras.Where(a => a.idLoginAsignado == null || a.idLoginAsignado == 0).ToList();

                //}

                if (!string.IsNullOrEmpty(filtro.CodMoneda) && filtro.CodMoneda != "NULL")
                {
                    EncCompras = EncCompras.Where(a => a.CodMoneda == filtro.CodMoneda).ToList();
                }

                if (filtro.RegimeSimplificado)
                {
                    EncCompras = EncCompras.Where(a => a.RegimenSimplificado == filtro.RegimeSimplificado).ToList();
                }


                if (filtro.FacturaExterior)
                {
                    EncCompras = EncCompras.Where(a => a.FacturaExterior == filtro.FacturaExterior).ToList();
                }

                if (filtro.FacturaNoRecibida)
                {
                    EncCompras = EncCompras.Where(a => a.FacturaNoRecibida == filtro.FacturaNoRecibida).ToList();
                }

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, EncCompras);

            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Error de GET Compras";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        //Este metodo consulta una por una las facturas
        [Route("api/Compras/Consultar")]
        public HttpResponseMessage GetOne([FromUri]int id)
        {
            try
            {
                G.AbrirConexionAPP(out db);


                var Cuentas = db.EncCompras.Where(a => a.id == id).Select(a => new
                {
                    a.id,
                    a.CodEmpresa
                 ,
                    a.CodProveedor
                 ,
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
                    a.XmlFacturaRecibida


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
                    UsuarioAsignado = db.Login.Where(d => d.id == a.idLoginAsignado).FirstOrDefault() == null ? "" : db.Login.Where(d => d.id == a.idLoginAsignado).FirstOrDefault().Nombre,
                    a.FecAsignado

                 ,
                    PdfFactura = db.Parametros.FirstOrDefault().UrlImagenesApp + a.PdfFactura
                 ,
                    a.idNormaReparto
                 ,
                    a.idTipoGasto
                 ,
                    TipoGasto = (a.idTipoGasto == 0 ? "Sin Asignar" : db.Gastos.Where(z => z.idTipoGasto == a.idTipoGasto).FirstOrDefault().Nombre),
                    a.idCierre,
                    a.Impuesto1,
                    a.Impuesto2,
                    a.Impuesto4,
                    a.Impuesto8,
                    a.Impuesto13,
                    a.PdfFac,
                    a.Comentario,

                    a.RegimenSimplificado,
                    a.FacturaExterior,
                    a.GastosVarios,
                    a.ImagenB64,
                    DetCompras = db.DetCompras.Where(d => d.NumFactura == a.NumFactura && d.ConsecutivoHacienda == a.ConsecutivoHacienda && d.ClaveHacienda == a.ClaveHacienda && d.CodProveedor == a.CodProveedor).ToList()

                }).FirstOrDefault();


                if (Cuentas == null)
                {
                    throw new Exception("Esta cuenta no se encuentra registrada");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Cuentas);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Error de GET Compras";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage Post([FromBody] ComprasViewModel compra)

        {
            G.AbrirConexionAPP(out db);
            var t = db.Database.BeginTransaction();
            try
            {

                var EncCompras = db.EncCompras.Where(a => a.NumFactura == compra.EncCompras.NumFactura && a.CodProveedor == compra.EncCompras.CodProveedor).FirstOrDefault();

                if (EncCompras == null)
                {
                    EncCompras = new EncCompras();
                    if (compra.EncCompras.FacturaExterior)
                    {
                        EncCompras.ClaveHacienda = compra.EncCompras.NumFactura.ToString();
                        EncCompras.ConsecutivoHacienda = compra.EncCompras.NumFactura.ToString();
                    }
                    else
                    {

                        EncCompras.ClaveHacienda = compra.EncCompras.ClaveHacienda;
                        EncCompras.ConsecutivoHacienda = compra.EncCompras.ConsecutivoHacienda;
                    }
                    EncCompras.NumFactura = compra.EncCompras.NumFactura;
                    EncCompras.FecFactura = compra.EncCompras.FecFactura;
                    EncCompras.FechaGravado = DateTime.Now;
                    EncCompras.CodProveedor = compra.EncCompras.CodProveedor;
                    EncCompras.NomProveedor = compra.EncCompras.NomProveedor;
                    try
                    {
                        var CodProv = EncCompras.CodProveedor.Split('[')[0];
                        var DV = "";
                        var CR = false;
                        try
                        {
                            DV = EncCompras.CodProveedor.Split('[')[1];
                        }
                        catch (Exception ex)
                        {

                            CR = true;
                            DV = "0";
                        }

                        var Proveedor = db.Proveedores.Where(a => a.RUC.Replace("-", "").Replace("-", "") == CodProv.Replace("-", "").Replace("-", "") && a.DV == DV).FirstOrDefault();

                        if (Proveedor != null)
                        {
                            if (!CR)
                            {
                                EncCompras.CodProveedor = Proveedor.RUC + "[" + Proveedor.DV;
                            }




                        }
                        else
                        {
                            Proveedor = new Proveedores();
                            Proveedor.Nombre = EncCompras.NomProveedor;
                            Proveedor.RUC = CodProv;
                            Proveedor.DV = DV;
                            db.Proveedores.Add(Proveedor);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception)
                    {


                    }


                    EncCompras.CodEmpresa = G.ObtenerCedulaJuridia();
                    EncCompras.CodCliente = compra.EncCompras.CodCliente;
                    EncCompras.NomCliente = compra.EncCompras.NomCliente;
                    EncCompras.CodigoActividadEconomica = compra.EncCompras.CodigoActividadEconomica;
                    EncCompras.CodMoneda = compra.EncCompras.CodMoneda;
                    EncCompras.DiasCredito = compra.EncCompras.DiasCredito;
                    EncCompras.Impuesto1 = compra.EncCompras.Impuesto1;
                    EncCompras.Impuesto2 = compra.EncCompras.Impuesto2;
                    EncCompras.Impuesto4 = compra.EncCompras.Impuesto4;
                    EncCompras.Impuesto8 = compra.EncCompras.Impuesto8;
                    EncCompras.Impuesto13 = compra.EncCompras.Impuesto13;
                    EncCompras.TotalComprobante = compra.EncCompras.TotalComprobante;
                    EncCompras.TotalDescuentos = compra.EncCompras.TotalDescuentos;
                    EncCompras.TotalImpuesto = compra.EncCompras.TotalImpuesto;
                    EncCompras.TotalVenta = compra.EncCompras.TotalVenta;
                    EncCompras.TotalVentaNeta = compra.EncCompras.TotalVentaNeta;
                    EncCompras.TotalOtrosCargos = 0;
                    EncCompras.TipoDocumento = "01";
                    EncCompras.EmailCliente = "";
                    EncCompras.FacturaExterior = compra.EncCompras.FacturaExterior;
                    EncCompras.RegimenSimplificado = compra.EncCompras.RegimenSimplificado;
                    EncCompras.GastosVarios = compra.EncCompras.GastosVarios;
                    EncCompras.FacturaNoRecibida = compra.EncCompras.FacturaNoRecibida;
                    EncCompras.idTipoGasto = compra.DetCompras.FirstOrDefault().idTipoGasto;
                    EncCompras.idCierre = 0;


                    if (!String.IsNullOrEmpty(compra.EncCompras.ImagenBase64))
                    {

                        EncCompras.PdfFactura = "";
                        var _bytes = Convert.FromBase64String(compra.EncCompras.ImagenBase64);
                        EncCompras.PdfFac = _bytes;
                        byte[] hex = Convert.FromBase64String(compra.EncCompras.ImagenBase64.Replace("data:image/jpeg;base64,", "").Replace("data:image/png;base64,", ""));
                        EncCompras.ImagenB64 = hex;

                    }
                    else
                    {
                        EncCompras.PdfFactura = EncCompras.PdfFactura;
                    }
                    EncCompras.Comentario = compra.EncCompras.Comentario;
                    db.EncCompras.Add(EncCompras);
                    db.SaveChanges();

                    var i = 1;
                    decimal totalVenta = 0;
                    decimal totalCompr = 0;
                    foreach (var item in compra.DetCompras)
                    {
                        var Det = new DetCompras();
                        Det.CodProveedor = EncCompras.CodProveedor;
                        Det.CodEmpresa = EncCompras.CodEmpresa;
                        Det.TipoDocumento = "01";
                        Det.ClaveHacienda = EncCompras.ClaveHacienda;
                        Det.ConsecutivoHacienda = EncCompras.ConsecutivoHacienda;
                        Det.NomProveedor = EncCompras.NomProveedor;
                        Det.NumFactura = EncCompras.NumFactura;
                        Det.NumLinea = i;
                        Det.CodPro = item.CodPro;
                        Det.UnidadMedida = item.UnidadMedida;
                        Det.NomPro = item.NomPro;
                        Det.PrecioUnitario = item.PrecioUnitario;
                        Det.Cantidad = item.Cantidad;
                        Det.MontoTotal = item.MontoTotal;
                        Det.MontoDescuento = item.MontoDescuento;
                        Det.SubTotal = item.SubTotal;
                        Det.ImpuestoTarifa = item.ImpuestoTarifa;
                        Det.ImpuestoMonto = item.ImpuestoMonto;
                        Det.MontoTotalLinea = item.MontoTotalLinea;
                        var TipoGasto = db.Gastos.Where(a => a.Nombre.ToUpper().Contains("Regimen Simplificado".ToUpper())).FirstOrDefault();

                        if (item.idTipoGasto == 0)
                        {
                            Det.idTipoGasto = TipoGasto.idTipoGasto;
                        }
                        else
                        {
                            Det.idTipoGasto = item.idTipoGasto;

                        }

                        totalCompr += item.MontoTotalLinea.Value;
                        totalVenta += item.SubTotal.Value;

                        db.DetCompras.Add(Det);
                        db.SaveChanges();
                        i++;
                    }
                    if (EncCompras.TotalVenta != totalVenta)
                    {
                        db.Entry(EncCompras).State = EntityState.Modified;
                        EncCompras.TotalVenta = totalVenta;
                        db.SaveChanges();
                    }

                    if (EncCompras.TotalComprobante != totalCompr)
                    {
                        db.Entry(EncCompras).State = EntityState.Modified;
                        EncCompras.TotalComprobante = totalCompr;
                        db.SaveChanges();
                    }
                    compra.EncCompras.id = EncCompras.id;
                }
                else
                {
                    if (EncCompras.idCierre == 0)
                    {
                        db.Entry(EncCompras).State = EntityState.Modified;
                        if (compra.EncCompras.FacturaExterior)
                        {
                            EncCompras.ClaveHacienda = compra.EncCompras.NumFactura.ToString();
                            EncCompras.ConsecutivoHacienda = compra.EncCompras.NumFactura.ToString();
                        }
                        else
                        {

                            EncCompras.ClaveHacienda = compra.EncCompras.ClaveHacienda;
                            EncCompras.ConsecutivoHacienda = compra.EncCompras.ConsecutivoHacienda;
                        }
                        EncCompras.NumFactura = compra.EncCompras.NumFactura;
                        EncCompras.FecFactura = compra.EncCompras.FecFactura;
                        EncCompras.FechaGravado = DateTime.Now;
                        EncCompras.CodProveedor = compra.EncCompras.CodProveedor;
                        EncCompras.NomProveedor = compra.EncCompras.NomProveedor;

                        try
                        {
                            var CodProv = EncCompras.CodProveedor.Split('[')[0];
                            var DV = EncCompras.CodProveedor.Split('[')[1];
                            var Proveedor = db.Proveedores.Where(a => a.RUC.Replace("-", "").Replace("-", "") == CodProv.Replace("-", "").Replace("-", "") && a.DV == DV).FirstOrDefault();

                            if (Proveedor != null)
                            {
                                EncCompras.CodProveedor = Proveedor.RUC + "[" + Proveedor.DV;
                            }
                            else
                            {
                                Proveedor = new Proveedores();
                                Proveedor.Nombre = EncCompras.NomProveedor;
                                Proveedor.RUC = CodProv;
                                Proveedor.DV = DV;
                                db.Proveedores.Add(Proveedor);
                                db.SaveChanges();
                            }
                        }
                        catch (Exception)
                        {


                        }
                        EncCompras.CodEmpresa = G.ObtenerCedulaJuridia();
                        EncCompras.CodCliente = compra.EncCompras.CodCliente;
                        EncCompras.NomCliente = compra.EncCompras.NomCliente;
                        EncCompras.CodigoActividadEconomica = compra.EncCompras.CodigoActividadEconomica;
                        EncCompras.CodMoneda = compra.EncCompras.CodMoneda;
                        EncCompras.DiasCredito = compra.EncCompras.DiasCredito;
                        EncCompras.Impuesto1 = compra.EncCompras.Impuesto1;
                        EncCompras.Impuesto2 = compra.EncCompras.Impuesto2;
                        EncCompras.Impuesto4 = compra.EncCompras.Impuesto4;
                        EncCompras.Impuesto8 = compra.EncCompras.Impuesto8;
                        EncCompras.Impuesto13 = compra.EncCompras.Impuesto13;
                        EncCompras.TotalComprobante = compra.EncCompras.TotalComprobante;
                        EncCompras.TotalDescuentos = compra.EncCompras.TotalDescuentos;
                        EncCompras.TotalImpuesto = compra.EncCompras.TotalImpuesto;
                        EncCompras.TotalVenta = compra.EncCompras.TotalVenta;
                        EncCompras.TotalVentaNeta = compra.EncCompras.TotalVentaNeta;
                        EncCompras.TotalOtrosCargos = 0;
                        EncCompras.TipoDocumento = "01";
                        EncCompras.EmailCliente = "";
                        EncCompras.FacturaExterior = compra.EncCompras.FacturaExterior;
                        EncCompras.RegimenSimplificado = compra.EncCompras.RegimenSimplificado;
                        EncCompras.GastosVarios = compra.EncCompras.GastosVarios;
                        EncCompras.FacturaNoRecibida = compra.EncCompras.FacturaNoRecibida;
                        EncCompras.idTipoGasto = compra.DetCompras.FirstOrDefault().idTipoGasto;
                        EncCompras.idCierre = 0;
                        if (!String.IsNullOrEmpty(compra.EncCompras.ImagenBase64))
                        {

                            EncCompras.PdfFactura = "";
                            var _bytes = Convert.FromBase64String(compra.EncCompras.ImagenBase64);
                            EncCompras.PdfFac = _bytes;
                            byte[] hex = Convert.FromBase64String(compra.EncCompras.ImagenBase64.Replace("data:image/jpeg;base64,", "").Replace("data:image/png;base64,", ""));
                            EncCompras.ImagenB64 = hex;

                        }
                        else
                        {
                            EncCompras.PdfFactura = EncCompras.PdfFactura;
                        }
                        EncCompras.Comentario = compra.EncCompras.Comentario;

                        db.SaveChanges();

                        var DetallesAnteriores = db.DetCompras.Where(a => a.NumFactura == compra.EncCompras.NumFactura && a.CodProveedor == compra.EncCompras.CodProveedor).ToList();

                        foreach (var item in DetallesAnteriores)
                        {
                            db.DetCompras.Remove(item);
                            db.SaveChanges();
                        }



                        var i = 1;
                        decimal totalVenta = 0;
                        decimal totalCompr = 0;
                        foreach (var item in compra.DetCompras)
                        {
                            var Det = new DetCompras();
                            Det.CodProveedor = EncCompras.CodProveedor;
                            Det.CodEmpresa = EncCompras.CodEmpresa;
                            Det.TipoDocumento = "01";
                            Det.ClaveHacienda = EncCompras.ClaveHacienda;
                            Det.ConsecutivoHacienda = EncCompras.ConsecutivoHacienda;
                            Det.NomProveedor = EncCompras.NomProveedor;
                            Det.NumFactura = EncCompras.NumFactura;
                            Det.NumLinea = i;
                            Det.CodPro = item.CodPro;
                            Det.UnidadMedida = item.UnidadMedida;
                            Det.NomPro = item.NomPro;
                            Det.PrecioUnitario = item.PrecioUnitario;
                            Det.Cantidad = item.Cantidad;
                            Det.MontoTotal = item.MontoTotal;
                            Det.MontoDescuento = item.MontoDescuento;
                            Det.SubTotal = item.SubTotal;
                            Det.ImpuestoTarifa = item.ImpuestoTarifa;
                            Det.ImpuestoMonto = item.ImpuestoMonto;
                            Det.MontoTotalLinea = item.MontoTotalLinea;
                            var TipoGasto = db.Gastos.Where(a => a.Nombre.ToUpper().Contains("Regimen Simplificado".ToUpper())).FirstOrDefault();

                            if (item.idTipoGasto == 0)
                            {
                                Det.idTipoGasto = TipoGasto.idTipoGasto;
                            }
                            else
                            {
                                Det.idTipoGasto = item.idTipoGasto;

                            }

                            totalCompr += item.MontoTotalLinea.Value;
                            totalVenta += item.SubTotal.Value;

                            db.DetCompras.Add(Det);
                            db.SaveChanges();
                            i++;
                        }
                        if (EncCompras.TotalVenta != totalVenta)
                        {
                            db.Entry(EncCompras).State = EntityState.Modified;
                            EncCompras.TotalVenta = totalVenta;
                            db.SaveChanges();
                        }

                        if (EncCompras.TotalComprobante != totalCompr)
                        {
                            db.Entry(EncCompras).State = EntityState.Modified;
                            EncCompras.TotalComprobante = totalCompr;
                            db.SaveChanges();
                        }
                        compra.EncCompras.id = EncCompras.id;


                    }
                    else
                    {

                        throw new Exception("Esta factura YA existe con el mismo numero de factura y proveedor; ademas de estar asignada por el usuario con id: " + EncCompras.idLoginAsignado);
                    }

                }
                t.Commit();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, compra);

            }
            catch (Exception ex)
            {
                t.Rollback();
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Insertar Factura Manual";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);

                G.GuardarTxt("ErrorFactura.txt", ex.ToString());
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [Route("api/Compras/Prueba")] //Metodo para cuando el pdf no fue leido correctamente

        public HttpResponseMessage GetPrueba()
        {

            try
            {
                G.AbrirConexionAPP(out db);

                var Facturas = db.EncCompras.Where(a => a.PdfFactura == "" || a.PdfFactura == null).ToList();


                foreach (var item in Facturas)
                {
                    var pdfResp = G.GuardarPDF(item.PdfFac, G.ObtenerCedulaJuridia(), item.NumFactura.ToString());

                    db.Entry(item).State = EntityState.Modified;

                    item.PdfFactura = pdfResp;

                    db.SaveChanges();
                }



                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, HttpContext.Current.Server.MapPath("~").ToString());
            }
            catch (Exception ex)
            {

                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Lectura de Bandeja";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();

                G.CerrarConexionAPP(db);

                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);


            }



        }

        [HttpPut]
        [Route("api/Compras/Actualizar")] //Para cambiar la norma de reparto
        public HttpResponseMessage Put([FromBody] AsignacionViewModel asig)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var User = db.Login.Where(a => a.id == asig.idLogin).FirstOrDefault();



                if (User != null && User.Activo == true)
                {
                    var Compra = db.EncCompras.Where(a => a.id == asig.idFac).FirstOrDefault();

                    if (Compra == null)
                    {
                        throw new Exception("Compra no existe");
                    }

                    db.Entry(Compra).State = EntityState.Modified;

                    if (asig.idNorma > 0)
                    {
                        Compra.idNormaReparto = asig.idNorma;
                    }
                    else
                    {

                        Compra.idNormaReparto = db.NormasReparto.Where(a => a.idLogin == asig.idLogin).FirstOrDefault().id;
                    }
                    Compra.FecAsignado = DateTime.Now;

                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Usuario no existe o está inactivo");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Error de PUT Compras";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                G.GuardarTxt("ErrorFactura.txt", ex.ToString());
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPut]
        [Route("api/Compras/ActualizarFacturaManual")]
        public HttpResponseMessage PutActualizarFacturaManual([FromBody] ComprasViewModel compra)
        {
            try
            {
                G.AbrirConexionAPP(out db);
                var Compañia = G.ObtenerCedulaJuridia();
                var Licencia = dbLogin.LicEmpresas.Where(a => a.CedulaJuridica == Compañia).FirstOrDefault();

                var Pais = Licencia.CadenaConexionSAP;
                var Compra = db.EncCompras.Where(a => a.id == compra.EncCompras.id).FirstOrDefault();
                var NumFacturaAnterior = Compra.NumFactura;
                var ProveedorAnterior = Compra.CodProveedor;
                if (Compra == null)
                {
                    throw new Exception("Compra no existe");
                }

                db.Entry(Compra).State = EntityState.Modified;

                if (compra.EncCompras.CodProveedor != Compra.CodProveedor)
                {
                    Compra.CodProveedor = compra.EncCompras.CodProveedor;
                    try
                    {
                        var CodProv = Compra.CodProveedor.Split('[')[0];
                        var CR = false;
                        var DV = "";

                        try
                        {
                            DV = Compra.CodProveedor.Split('[')[1];
                        }
                        catch (Exception ex)
                        {

                            CR = true;
                            DV = "0";
                        }



                        var Proveedor = db.Proveedores.Where(a => a.RUC.Replace("-", "").Replace("-", "") == CodProv.Replace("-", "").Replace("-", "") && a.DV == DV).FirstOrDefault();

                        if (Proveedor != null)
                        {
                            if (!CR)
                            {
                                Compra.CodProveedor = Proveedor.RUC + "[" + Proveedor.DV;
                            }


                        }
                        else
                        {
                            Proveedor = new Proveedores();
                            Proveedor.Nombre = Compra.NomProveedor;
                            Proveedor.RUC = CodProv;
                            Proveedor.DV = DV;
                            db.Proveedores.Add(Proveedor);
                            db.SaveChanges();
                        }


                    }
                    catch (Exception)
                    {


                    }
                }

                if (compra.EncCompras.NomProveedor != Compra.NomProveedor)
                {
                    Compra.NomProveedor = compra.EncCompras.NomProveedor;
                }

                if (compra.EncCompras.NumFactura != Compra.NumFactura)
                {
                    Compra.NumFactura = compra.EncCompras.NumFactura;
                    Compra.ClaveHacienda = Pais == "H" ? compra.EncCompras.ClaveHacienda : compra.EncCompras.NumFactura.ToString();
                    Compra.ConsecutivoHacienda = Pais == "H" ? compra.EncCompras.ConsecutivoHacienda : compra.EncCompras.NumFactura.ToString();
                }

                if (compra.EncCompras.FecFactura.Value.Date != Compra.FecFactura.Value.Date)
                {
                    Compra.FecFactura = compra.EncCompras.FecFactura;
                }
                Compra.Comentario = Compra.Comentario != compra.EncCompras.Comentario ? compra.EncCompras.Comentario : Compra.Comentario;
                Compra.RegimenSimplificado = compra.EncCompras.RegimenSimplificado;
                Compra.FacturaExterior = compra.EncCompras.FacturaExterior;
                Compra.GastosVarios = compra.EncCompras.GastosVarios;

                if (!String.IsNullOrEmpty(compra.EncCompras.ImagenBase64))
                {

                    Compra.PdfFactura = "";
                    byte[] hex = Convert.FromBase64String(compra.EncCompras.ImagenBase64.Replace("data:image/jpeg;base64,", "").Replace("data:image/png;base64,", ""));
                    Compra.ImagenB64 = hex;

                    var _bytes = Convert.FromBase64String(compra.EncCompras.ImagenBase64);
                    Compra.PdfFac = _bytes;

                }


                Compra.Impuesto1 = compra.EncCompras.Impuesto1;
                Compra.Impuesto2 = compra.EncCompras.Impuesto2;
                Compra.Impuesto4 = compra.EncCompras.Impuesto4;
                Compra.Impuesto8 = compra.EncCompras.Impuesto8;
                Compra.Impuesto13 = compra.EncCompras.Impuesto13;
                Compra.TotalComprobante = compra.EncCompras.TotalComprobante;
                Compra.TotalDescuentos = compra.EncCompras.TotalDescuentos;
                Compra.TotalImpuesto = compra.EncCompras.TotalImpuesto;
                Compra.TotalVenta = compra.EncCompras.TotalVenta;
                Compra.TotalVentaNeta = compra.EncCompras.TotalVentaNeta;
                Compra.TotalOtrosCargos = 0;
                db.SaveChanges();


                foreach (var item in compra.DetCompras)
                {
                    var Detalle = db.DetCompras.Where(a => a.NumFactura == NumFacturaAnterior && a.CodProveedor == ProveedorAnterior).FirstOrDefault();
                    db.Entry(Detalle).State = EntityState.Modified;

                    Detalle.CodProveedor = Compra.CodProveedor;
                    Detalle.CodEmpresa = Compra.CodEmpresa;
                    Detalle.ClaveHacienda = Compra.ClaveHacienda;
                    Detalle.ConsecutivoHacienda = Compra.ConsecutivoHacienda;
                    Detalle.NomProveedor = Compra.NomProveedor;
                    Detalle.NumFactura = Compra.NumFactura;

                    Detalle.NomPro = item.NomPro;
                    Detalle.PrecioUnitario = item.PrecioUnitario;
                    Detalle.Cantidad = item.Cantidad;
                    Detalle.MontoTotal = item.MontoTotal;
                    Detalle.MontoDescuento = item.MontoDescuento;
                    Detalle.SubTotal = item.SubTotal;
                    Detalle.ImpuestoTarifa = item.ImpuestoTarifa;
                    Detalle.ImpuestoMonto = item.ImpuestoMonto;
                    Detalle.MontoTotalLinea = item.MontoTotalLinea;
                    var TipoGasto = db.Gastos.Where(a => a.Nombre.ToUpper().Contains("Regimen Simplificado".ToUpper())).FirstOrDefault();

                    if (item.idTipoGasto == 0)
                    {
                        Detalle.idTipoGasto = TipoGasto.idTipoGasto;
                    }
                    else
                    {
                        Detalle.idTipoGasto = item.idTipoGasto;

                    }

                    db.SaveChanges();
                }


                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, compra);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Error de Actualizar Factura";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                G.GuardarTxt("ErrorFactura.txt", ex.ToString());
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        public int EncontrarGasto(ModelCliente db, string NomPro)
        {
            try
            {


                var Gastos = db.Gastos.ToList();

                foreach (var gasto in Gastos)
                {
                    var palabrasClaves = gasto.PalabrasClave.Split(';').ToList();
                    palabrasClaves.Remove(palabrasClaves[palabrasClaves.Count() - 1]);

                    foreach (var item in palabrasClaves)
                    {
                        if (QuitarTilde(NomPro).ToUpper().Contains(QuitarTilde(item).Replace(" ", string.Empty).ToUpper()))
                        {
                            return gasto.idTipoGasto;
                        }
                    }

                }




                return 0;

            }
            catch (Exception)
            {

                return 0;
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