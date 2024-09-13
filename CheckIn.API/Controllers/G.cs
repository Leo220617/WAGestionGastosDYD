using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

using static System.Collections.Specialized.BitVector32;

using System.Web.Configuration;
using System.Security.Claims;
using CheckIn.API.Models.ModelCliente;
using CheckIn.API.Models.ModelMain;
using CheckIn.API.Models;
using CheckIn.API.ViewModels;
using System.Web;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml;

namespace CheckIn.API.Controllers
{
    public class G
    {
        private static byte[] key = { };
        private static byte[] IV = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
        internal void AbrirConexionAPP(out ModelCliente db)
        {
            try
            {
             
                var claims1 = HttpContext.Current.User.Identity.Name.ToString();

                


                db = new ModelCliente(claims1);


            }
            catch (Exception)
            {
                throw;
            }
        }


        internal string ObtenerCedulaJuridia()
        {
            try
            {
                var claims1 = "";
                var ClaimsIdentity = HttpContext.Current.User.Identity as ClaimsIdentity;
                if (ClaimsIdentity != null)
                {
                    var Claim = ClaimsIdentity.FindFirst("Compania");
                    if (Claim != null && !String.IsNullOrEmpty(Claim.Value))
                    {
                        claims1 = Claim.Value;
                    }
                }
                 


                return claims1;


            }
            catch (Exception)
            {
                return "";
            }
        }

        internal void CerrarConexionAPP( ModelCliente db)
        {
            try
            {




                db.Database.Connection.Close();
                db.Database.Connection.Dispose();


            }
            catch (Exception)
            {
                throw;
            }
        }

        public void GuardarTxt(string nombreArchivo, string texto)
        {
            try
            {
                texto = (DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " " + texto + Environment.NewLine + "------------------------------------------" + Environment.NewLine);
                System.IO.File.AppendAllText(HttpContext.Current.Server.MapPath("~") + @"\Bitacora\" + nombreArchivo, texto);


            }
            catch { }
        }

        public static string ObtenerConfig(string v)
        {
            try
            {
                return WebConfigurationManager.AppSettings[v];
            }
            catch
            {
                return "";
            }
        }

        public string Encrypt(string stringToEncrypt, bool UsuarioAdmin = false)
        {
            try
            {
                string SEncryptionKey = HttpContext.Current.User.Identity.Name.ToString();

                if(UsuarioAdmin)
                {
                    SEncryptionKey = G.ObtenerConfig("SicKey");
                }

                key = System.Text.Encoding.UTF8.GetBytes(SEncryptionKey.Substring(0, 8));
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                Byte[] inputByteArray = Encoding.UTF8.GetBytes(stringToEncrypt);
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(key, IV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionLevel.Optimal))
                {
                    //msi.CopyTo(gs);

                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }
        
        public XElement ConvertirArchivoaXElement(string result, string CodEmpresa)
        {
            string codEmpresa = CodEmpresa;
            XElement xml = null;
            try
            {
                xml = XDocument.Parse(result).Elements().FirstOrDefault();
            }

            catch (Exception e)
            {
                // Error por UTF8 BOM .
                // leer el archivo original y convertirlo sin UTF8
               
                

                string rutaTemp = HttpContext.Current.Server.MapPath("~") + "\\Temp\\";

                if (!System.IO.Directory.Exists(rutaTemp))
                    System.IO.Directory.CreateDirectory(rutaTemp);

                string fic = HttpContext.Current.Server.MapPath("~") + $"\\Temp\\{codEmpresa}{TimeStamp(DateTime.Now)}.txt";
                string texto = result;

                System.IO.StreamWriter sw = new System.IO.StreamWriter(fic);
                sw.WriteLine(texto);
                sw.Close();
                // HttpContext.Current.Server.MapPath("~") + @"\" + nombreArchivo

                System.IO.StreamReader objReader = new System.IO.StreamReader(fic);
                texto = objReader.ReadToEnd();
                objReader.Close();

                // borrar archivod espues de utilizado
                System.IO.File.Delete(fic);

                xml = XDocument.Parse(texto).Elements().FirstOrDefault();
            }

            return xml;
        }

        public string GuardarPDF(byte[] result, string CodEmpresa, string idFac)
        {
            string codEmpresa = CodEmpresa;
          
            try
            {
                byte[] bytes = result;

                string path = HttpContext.Current.Server.MapPath("~") + $"\\Temp\\{codEmpresa}\\{idFac}.pdf";
                //System.IO.FileStream stream = new FileStream(path, FileMode.CreateNew);
                //System.IO.BinaryWriter writer =
                //    new BinaryWriter(stream);
                //writer.Write(bytes, 0, bytes.Length);
                //writer.Close();


                System.IO.File.WriteAllBytes(path, bytes);
              
                
                 return idFac+".pdf";

            }

            catch (Exception e)
            {
                return "";
            }

        }


        public string TimeStamp(DateTime fechaActual)
        {
            long ticks = fechaActual.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000000; //Convert windows ticks to seconds
            return ticks.ToString();
           
        }
        public string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        public static FacturaXml ObtenerDatosXmlRechazado(string xml)
        {

            Regex GetXmlNumeroConsecutivo = new Regex("<NumeroConsecutivo>([^\"]*)</NumeroConsecutivo>");
            Regex GetXmlFechaEmision = new Regex("<FechaEmision>([^\"]*)</FechaEmision>");
            Regex GetXmlEmisor = new Regex("<Emisor>([^\"]*)</Emisor>");
            Regex GetXmlEmisorNombre = new Regex("<Nombre>([^\"]*)</Nombre>");
            Regex GetXmlNumero = new Regex("<Numero>([^\"]*)</Numero>");
            Regex GetXmlCodigoMoneda = new Regex("<CodigoMoneda>([^\"]*)</CodigoMoneda>");
            Regex GetXmlTotalComprobante = new Regex("<TotalComprobante>([^\"]*)</TotalComprobante>");

            Regex GetXmlReceptor = new Regex("<Receptor>([^\"]*)</Receptor>");
            Regex GetXmlIdReceptor = new Regex("<Numero>([^\"]*)</Numero>");

            FacturaXml facturaxml = new FacturaXml();

            try
            {
                facturaxml.NumeroConsecutivo = GetXmlNumeroConsecutivo.Match(xml).ToString().Replace("<NumeroConsecutivo>", "").Replace("</NumeroConsecutivo>", "");
                facturaxml.TipoDocumento = facturaxml.NumeroConsecutivo.Substring(8, 2);
                if (facturaxml.TipoDocumento == "01")
                    facturaxml.TipoDocumentoDescripcion = "Factura Electrónica";
                else if (facturaxml.TipoDocumento == "02")
                    facturaxml.TipoDocumentoDescripcion = "Nota de Débito";
                else if (facturaxml.TipoDocumento == "03")
                    facturaxml.TipoDocumentoDescripcion = "Nota de Crédito";
                else
                    facturaxml.TipoDocumento = "Tiquete Electrónico";
                string _FechaEmision = GetXmlFechaEmision.Match(xml).ToString().Replace("<FechaEmision>", "").Replace("</FechaEmision>", "").Substring(0, 10);
                string[] Array_FechaEmision = _FechaEmision.Split('-');
                if (Array_FechaEmision.Length == 3)
                    facturaxml.FechaEmision = Array_FechaEmision[2] + "/" + Array_FechaEmision[1] + "/" + Array_FechaEmision[0];
                facturaxml.NombreEmisor = GetXmlEmisorNombre.Match(GetXmlEmisor.Match(xml).ToString().Replace("<Emisor>", "").Replace("</Emisor>", "")).ToString().Replace("<Nombre>", "").Replace("</Nombre>", "");
                facturaxml.Numero = GetXmlNumero.Match(GetXmlEmisor.Match(xml).ToString().Replace("<Emisor>", "").Replace("</Emisor>", "")).ToString().Replace("<Numero>", "").Replace("</Numero>", "");
                facturaxml.CodigoMoneda = GetXmlCodigoMoneda.Match(xml).ToString().Replace("<CodigoMoneda>", "").Replace("</CodigoMoneda>", "");
                var Total = GetXmlTotalComprobante.Match(xml).ToString().Replace("<TotalComprobante>", "").Replace("</TotalComprobante>", "");
           
                facturaxml.TotalComprobante = Convert.ToDecimal(Total);
                

                facturaxml.IdReceptor = GetXmlIdReceptor.Match(GetXmlReceptor.Match(xml).ToString().Replace("<Receptor>", "").Replace("</Receptor>", "")).ToString().Replace("<Numero>", "").Replace("</Numero>", "");
            }
            catch (Exception ex)
            {
                facturaxml = null;
            }
            return facturaxml;
        }

        public static FacturaXml ObtenerDatosXmlRechazadoEcuador(string xml)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string palabraInicio = "<comprobante><![CDATA[";
            string palabraFin = "]]></comprobante>";

            // Encontrar las posiciones de las palabras clave
            int indiceInicio = xml.IndexOf(palabraInicio);
            if(indiceInicio == -1)
            {
                palabraInicio = "<comprobante> <![CDATA[";
                indiceInicio = xml.IndexOf(palabraInicio);
            }
            int indiceFin = xml.IndexOf(palabraFin);
            string subcadena = "";
            // Verificar si se encontraron ambas palabras clave
            if (indiceInicio != -1 && indiceFin != -1)
            {
                // Calcular la longitud de la subcadena
                int longitudSubcadena = indiceFin - (indiceInicio + palabraInicio.Length);

                // Extraer la subcadena
                 subcadena = xml.Substring(indiceInicio + palabraInicio.Length, longitudSubcadena);

                // Mostrar la subcadena
                Console.WriteLine("Subcadena extraída:");
                Console.WriteLine(subcadena);
            }

            string xmlOriginal = subcadena;
            try
            {
                xmlOriginal = xmlOriginal.Trim();
                // Cargar la cadena XML en el documento XML
                xmlDoc.LoadXml(xmlOriginal);
                XmlNode nodo = xmlDoc.SelectSingleNode("comprobante");
                // Mostrar el documento XML en la consola (opcional)
                Console.WriteLine("Documento XML creado:");
                Console.WriteLine(xmlDoc.OuterXml);
            }
            catch (XmlException ex)
            {
                Console.WriteLine("Error al cargar la cadena XML: " + ex.Message);
            }
            Regex GETXMLGENERAL = new Regex("<secuencial>([^\"]*)</secuencial>");
            Regex GetXmlNumeroConsecutivo = new Regex("<secuencial>([^\"]*)</secuencial>");
            Regex GetXmlFechaEmision = new Regex("<fechaEmision>([^\"]*)</fechaEmision>");
            Regex GetXmlEmisor = new Regex("<Emisor>([^\"]*)</Emisor>");
            Regex GetXmlEmisorNombre = new Regex("<razonSocial>([^\"]*)</razonSocial>");
            Regex GetXmlNumero = new Regex("<ruc>([^\"]*)</ruc>");
            Regex GetXmlCodigoMoneda = new Regex("<moneda>([^\"]*)</moneda>");
            Regex GetXmlTotalComprobante = new Regex("<importeTotal>([^\"]*)</importeTotal>");

            Regex GetXmlReceptor = new Regex("<Receptor>([^\"]*)</Receptor>");
            Regex GetXmlIdReceptor = new Regex("<identificacionComprador>([^\"]*)</identificacionComprador>");

            FacturaXml facturaxml = new FacturaXml();
            string xmlString = GETXMLGENERAL.Match(xmlOriginal).ToString();
            try
            {
                facturaxml.NumeroConsecutivo = GETXMLGENERAL.Match(xmlOriginal).ToString().Replace("<secuencial>", "").Replace("</secuencial>", "");
                facturaxml.TipoDocumento = "01";
                 
                facturaxml.TipoDocumentoDescripcion = "Factura Electrónica";
                
                string _FechaEmision = GetXmlFechaEmision.Match(xmlOriginal).ToString().Replace("<fechaEmision>", "").Replace("</fechaEmision>", "");
                string[] Array_FechaEmision = _FechaEmision.Split('/');
                if (Array_FechaEmision.Length == 3)
                    facturaxml.FechaEmision = Array_FechaEmision[2] + "/" + Array_FechaEmision[1] + "/" + Array_FechaEmision[0];
                facturaxml.NombreEmisor = GetXmlEmisorNombre.Match(xmlOriginal).ToString().Replace("<razonSocial>", "").Replace("</razonSocial>", "");
                facturaxml.Numero = GetXmlNumero.Match(xmlOriginal).ToString().Replace("<ruc>", "").Replace("</ruc>", "");
                facturaxml.CodigoMoneda = GetXmlCodigoMoneda.Match(xmlOriginal).ToString().Replace("<moneda>", "").Replace("</moneda>", "");
                var Total = GetXmlTotalComprobante.Match(xmlOriginal).ToString().Replace("<importeTotal>", "").Replace("</importeTotal>", "");

                facturaxml.TotalComprobante = Convert.ToDecimal(Total);


                facturaxml.IdReceptor = GetXmlIdReceptor.Match(xmlOriginal).ToString().Replace("<identificacionComprador>", "").Replace("</identificacionComprador>", "");
            }
            catch (Exception ex)
            {
                facturaxml = null;
            }
            return facturaxml;
        }

        internal string StringToBase64(string xmlStringFirmado)
        {
            try
            {
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(xmlStringFirmado);
                return Convert.ToBase64String(plainTextBytes);
            }
            catch (Exception)
            {
                return "";
            }
        }

        public string ExtraerValorDeNodoXml(System.Xml.Linq.XElement elemento, string nombre, bool retornarCero = false)
        {
            try
            {
                string[] nombres = nombre.Split('/');
                string valor = "";

                if (nombres.Length == 1)
                    valor = elemento.Elements().Where(m => m.Name.LocalName == nombres[0]).FirstOrDefault().Value;
                else if (nombres.Length == 2)
                    valor = elemento.Elements().Where(m => m.Name.LocalName == nombres[0]).FirstOrDefault()
                        .Elements().Where(m => m.Name.LocalName == nombres[1]).FirstOrDefault().Value;
                else if (nombres.Length == 3)
                    valor = elemento.Elements().Where(m => m.Name.LocalName == nombres[0]).FirstOrDefault()
                        .Elements().Where(m => m.Name.LocalName == nombres[1]).FirstOrDefault()
                        .Elements().Where(m => m.Name.LocalName == nombres[2]).FirstOrDefault().Value;
                else if (nombres.Length == 4)
                    valor = elemento.Elements().Where(m => m.Name.LocalName == nombres[0]).FirstOrDefault()
                        .Elements().Where(m => m.Name.LocalName == nombres[1]).FirstOrDefault()
                        .Elements().Where(m => m.Name.LocalName == nombres[2]).FirstOrDefault()
                        .Elements().Where(m => m.Name.LocalName == nombres[3]).FirstOrDefault().Value;

                return valor;
            }
            catch (Exception ex)
            {
                if (retornarCero)
                    return "0";
                else
                    return "";
            }
        }
    }
    public partial class FacturaXml
    {
        public FacturaXml()
        {
            NumeroConsecutivo = "";
            TipoDocumento = "";
            TipoDocumentoDescripcion = "";
            FechaEmision = "";
            NombreEmisor = "";
            Numero = "";
            CodigoMoneda = "";
            TotalComprobante = 0;
            IdReceptor = "";
        }

        public string NumeroConsecutivo { get; set; }
        public string TipoDocumento { get; set; }
        public string TipoDocumentoDescripcion { get; set; }
        public string FechaEmision { get; set; }
        public string NombreEmisor { get; set; }
        public string Numero { get; set; }
        public string CodigoMoneda { get; set; }
        public decimal TotalComprobante { get; set; }
        public string IdReceptor { get; set; }
    }
}