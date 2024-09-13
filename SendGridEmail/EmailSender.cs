using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

namespace SendGridEmail
{
    public class EmailSender
    {
        /// <summary>
        /// Envia Email por sendgrid (utiliza la clave de sicsoft para el envío)
        /// </summary>
        /// <param name="para">Destinatario de Email (pueden ser varios separados por punto y coma (;))</param>
        /// <param name="copia">Emails a los que se desea enviar copia (pueden ser varios separados por punto y coma (;))</param>
        /// <param name="copiaOculta">Emails a los que se desea enviar copia oculta (pueden ser varios separados por punto y coma (;))</param>
        /// <param name="de">Email que se desea aparezca como remitente </param>
        /// <param name="displayName">Nombre que se desea mostrar como remitente</param>
        /// <param name="asunto">Asunto del Email</param>
        /// <param name="html">Html del body del email a enviar</param>
        /// <returns>Retorna true si el envío fué correcto o false si falla</returns>
        public bool Send(string para, string copia, string copiaOculta, string de, string displayName, string asunto,
            string html, string[] ArchivosAdjuntos = null)
        {
            try
            {
                string usaSendgrid = "0";

                try
                {
                    usaSendgrid = WebConfigurationManager.AppSettings["UsaSendgrid"].ToString();
                }
                catch { usaSendgrid = "0"; }

                if (usaSendgrid == "1")
                {

                    var myMessage = new SendGrid.SendGridMessage();

                    var paraList = para.Split(';');
                    foreach (var p in paraList)
                    {
                        if (p.Trim().Length > 0)
                            myMessage.AddTo(p.Trim());
                    }
                    var ccList = copia.Split(';');
                    foreach (var cc in ccList)
                    {
                        if (cc.Trim().Length > 0)
                            myMessage.AddCc(cc.Trim());
                    }
                    var ccoList = copiaOculta.Split(';');
                    foreach (var cco in ccoList)
                    {
                        if (cco.Trim().Length > 0)
                            myMessage.AddBcc(cco.Trim());
                    }

                    myMessage.From = new MailAddress(de, displayName);
                    myMessage.Subject = asunto;
                    myMessage.Html = html;
                    if (ArchivosAdjuntos != null)
                    {
                        foreach (var archivo in ArchivosAdjuntos)
                        {
                            if (!string.IsNullOrEmpty(archivo))
                                myMessage.AddAttachment(archivo);
                        }
                    }


                    //SendGrid.Web transportWeb = new SendGrid.Web("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");
                    string sendgridKey = "";
                    try
                    {
                        // Intenta utilizar el SendGridKey del config, la idea es que cada cliente
                        // tenga su propio sendgrid
                        sendgridKey = WebConfigurationManager.AppSettings["SendGridKey"];
                    }
                    catch (Exception)
                    {
                        // si da error utilizar el key de SICSOFT
                        sendgridKey = "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";
                    }

                    SendGrid.Web transportWeb = new SendGrid.Web(sendgridKey);

                    transportWeb.DeliverAsync(myMessage);

                }
                else
                {
                    MailMessage mail = new MailMessage();
                    mail.Subject = asunto;
                    mail.Body = html;
                    mail.IsBodyHtml = true;
                    //mail.From = new MailAddress(WebConfigurationManager.AppSettings["UserName"], displayName);
                    mail.From = new MailAddress(WebConfigurationManager.AppSettings["UserName"], displayName);

                    var paraList = para.Split(';');
                    foreach (var p in paraList)
                    {
                        if (p.Trim().Length > 0)
                            mail.To.Add(p.Trim());
                    }
                    var ccList = copia.Split(';');
                    foreach (var cc in ccList)
                    {
                        if (cc.Trim().Length > 0)
                            mail.CC.Add(cc.Trim());
                    }
                    var ccoList = copiaOculta.Split(';');
                    foreach (var cco in ccoList)
                    {
                        if (cco.Trim().Length > 0)
                            mail.Bcc.Add(cco.Trim());
                    }

                    if (ArchivosAdjuntos != null)
                    {
                        foreach (var archivo in ArchivosAdjuntos)
                        {
                            if (!string.IsNullOrEmpty(archivo))
                                mail.Attachments.Add(new Attachment(archivo));
                        }
                    }



                    SmtpClient client = new SmtpClient();
                    client.Host = WebConfigurationManager.AppSettings["HostName"];
                    client.Port = int.Parse(WebConfigurationManager.AppSettings["Port"].ToString());
                    client.UseDefaultCredentials = false;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.EnableSsl = bool.Parse(WebConfigurationManager.AppSettings["EnableSsl"]);
                    client.Credentials = new NetworkCredential(WebConfigurationManager.AppSettings["UserName"], WebConfigurationManager.AppSettings["Password"]);

                    client.Send(mail);
                    client.Dispose();
                    mail.Dispose();
                }

                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }



        /// <summary>
        /// Envia Email por sendgrid  
        /// </summary>
        /// <param name="para">Destinatario de Email (pueden ser varios separados por punto y coma (;))</param>
        /// <param name="copia">Emails a los que se desea enviar copia (pueden ser varios separados por punto y coma (;))</param>
        /// <param name="copiaOculta">Emails a los que se desea enviar copia oculta (pueden ser varios separados por punto y coma (;))</param>
        /// <param name="de">Email que se desea aparezca como remitente </param>
        /// <param name="displayName">Nombre que se desea mostrar como remitente</param>
        /// <param name="asunto">Asunto del Email</param>
        /// <param name="html">Html del body del email a enviar</param>
        /// <returns>Retorna true si el envío fué correcto o false si falla</returns>
        public bool SendV2(string para, string copia, string copiaOculta, string de, string displayName, string asunto,
            string html, string HostServer, int Puerto, bool EnableSSL, string UserName, string Password , List<Attachment> ArchivosAdjuntos = null)
        {
            try
            {
                
                    MailMessage mail = new MailMessage();
                    mail.Subject = asunto;
                    mail.Body = html;
                    mail.IsBodyHtml = true;
                   
                    // * mail.From = new MailAddress(WebConfigurationManager.AppSettings["UserName"], displayName);
                    mail.From = new MailAddress(de, displayName);

                    var paraList = para.Split(';');
                    foreach (var p in paraList)
                    {
                        if (p.Trim().Length > 0)
                            mail.To.Add(p.Trim());
                    }
                    var ccList = copia.Split(';');
                    foreach (var cc in ccList)
                    {
                        if (cc.Trim().Length > 0)
                            mail.CC.Add(cc.Trim());
                    }
                    var ccoList = copiaOculta.Split(';');
                    foreach (var cco in ccoList)
                    {
                        if (cco.Trim().Length > 0)
                            mail.Bcc.Add(cco.Trim());
                    }

           

                if (ArchivosAdjuntos != null)
                {
                    foreach (var archivo in ArchivosAdjuntos)
                    {
                        //if (!string.IsNullOrEmpty(archivo))
                        mail.Attachments.Add(archivo);
                    }
                }


                SmtpClient client = new SmtpClient();
                    client.Host = HostServer; // WebConfigurationManager.AppSettings["HostName"];
                    client.Port = Puerto; // int.Parse(WebConfigurationManager.AppSettings["Port"].ToString());
                    client.UseDefaultCredentials = false;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.EnableSsl = EnableSSL; // bool.Parse(WebConfigurationManager.AppSettings["EnableSsl"]);
                    client.Credentials = new NetworkCredential(UserName, Password);

                    client.Send(mail);
                    client.Dispose();
                    mail.Dispose();
                
                return true;

            }
            catch (Exception ex )
            {
               

                return false;
            }
        }


        public bool SendV3(string para, string copia, string copiaOculta, string de, string displayName, string asunto,
           string html, List<Object[]> ArchivosAdjuntos = null)
        {
            try
            {
                if (WebConfigurationManager.AppSettings["UsaSendgrid"].ToString() == "1")
                {

                    var myMessage = new SendGrid.SendGridMessage();

                    var paraList = para.Split(';');
                    foreach (var p in paraList)
                    {
                        if (p.Trim().Length > 0)
                            myMessage.AddTo(p.Trim());
                    }
                    var ccList = copia.Split(';');
                    foreach (var cc in ccList)
                    {
                        if (cc.Trim().Length > 0)
                            myMessage.AddCc(cc.Trim());
                    }
                    var ccoList = copiaOculta.Split(';');
                    foreach (var cco in ccoList)
                    {
                        if (cco.Trim().Length > 0)
                            myMessage.AddBcc(cco.Trim());
                    }

                    myMessage.From = new MailAddress(de, displayName);
                    myMessage.Subject = asunto;
                    myMessage.Html = html;
                    if (ArchivosAdjuntos != null)
                    {
                        foreach (var archivo in ArchivosAdjuntos)
                        {
                            //if (!string.IsNullOrEmpty(archivo))
                            myMessage.AddAttachment((Stream)archivo[0], archivo[1].ToString());
                        }
                    }


                    //SendGrid.Web transportWeb = new SendGrid.Web("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");
                    string sendgridKey = "";
                    try
                    {
                        // Intenta utilizar el SendGridKey del config, la idea es que cada cliente
                        // tenga su propio sendgrid
                        sendgridKey = WebConfigurationManager.AppSettings["SendGridKey"];
                    }
                    catch (Exception)
                    {
                        // si da error utilizar el key de SICSOFT
                        sendgridKey = "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";
                    }

                    SendGrid.Web transportWeb = new SendGrid.Web(sendgridKey);

                    transportWeb.DeliverAsync(myMessage);

                }
                else
                {
                    MailMessage mail = new MailMessage();
                    mail.Subject = asunto;
                    mail.Body = html;
                    mail.IsBodyHtml = true;
                    mail.From = new MailAddress(WebConfigurationManager.AppSettings["UserName"], displayName);

                    var paraList = para.Split(';');
                    foreach (var p in paraList)
                    {
                        if (p.Trim().Length > 0)
                            mail.To.Add(p.Trim());
                    }
                    var ccList = copia.Split(';');
                    foreach (var cc in ccList)
                    {
                        if (cc.Trim().Length > 0)
                            mail.CC.Add(cc.Trim());
                    }
                    var ccoList = copiaOculta.Split(';');
                    foreach (var cco in ccoList)
                    {
                        if (cco.Trim().Length > 0)
                            mail.Bcc.Add(cco.Trim());
                    }

                    if (ArchivosAdjuntos != null)
                    {
                        foreach (var archivo in ArchivosAdjuntos)
                        {
                            //if (!string.IsNullOrEmpty(archivo))
                            mail.Attachments.Add(new Attachment((Stream)archivo[0], archivo[1].ToString()));
                        }
                    }



                    SmtpClient client = new SmtpClient();
                    client.Host = WebConfigurationManager.AppSettings["HostName"];
                    client.Port = int.Parse(WebConfigurationManager.AppSettings["Port"].ToString());
                    client.UseDefaultCredentials = false;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.EnableSsl = bool.Parse(WebConfigurationManager.AppSettings["EnableSsl"]);
                    client.Credentials = new NetworkCredential(WebConfigurationManager.AppSettings["UserName"], WebConfigurationManager.AppSettings["Password"]);

                    client.Send(mail);
                    client.Dispose();
                    mail.Dispose();
                }

                return true;

            }
            catch (Exception ex)
            {
                GuardarTxt("ErrorEmail.txt", ex.Message);
                return false;
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


    }
}
