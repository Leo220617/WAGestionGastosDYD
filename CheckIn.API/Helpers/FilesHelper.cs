
namespace CheckIn.API.Helpers
{
    using System;
    using System.IO;
    using System.Web;

    public class FilesHelper
    {
        public static string UploadPhoto(HttpPostedFileBase file, string folder)
        {
            string path = string.Empty;
            string pic = string.Empty;

            if (file != null)
            {
                pic = Path.GetFileName(file.FileName);
                path = Path.Combine(HttpContext.Current.Server.MapPath(folder), pic);
                file.SaveAs(path);
            }

            return pic;
        }

        public static bool UploadPhoto(MemoryStream stream, string folder, string name)
        {
            try
            {
                stream.Position = 0;
                var path = Path.Combine(HttpContext.Current.Server.MapPath(folder), name);
                File.WriteAllBytes(path, stream.ToArray());
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static string GuardarImagen(
            byte[] imagenBase64, 
            string oldValue, 
            string carpeta,
            string extesion="jpg")
        {
            try
            {
                //ahora verificar si viene imagen de perfil para guardarla
                if (imagenBase64 != null && imagenBase64.Length > 0)
                {
                    var stream = new MemoryStream(imagenBase64);
                    var guid = Guid.NewGuid().ToString();
                    var file = $"{guid}.{extesion}";
                    var folder = $"~/Imagenes/{carpeta}";

                    //Verificamos si no existe la carpeta y la creamos.
                    if (!System.IO.Directory.Exists( HttpContext.Current.Server.MapPath(folder)))
                    {
                        Directory.CreateDirectory(HttpContext.Current.Server.MapPath(folder));
                    }

                    var fullPath = $"{folder}/{file}";
                    var result = FilesHelper.UploadPhoto(stream, folder, file);

                    if (result)
                    {
                        return $"Imagenes/{carpeta}/{file}";
                    }
                }
                else
                {
                    return oldValue;
                }
            }
            catch (Exception)
            {
            }

            return "";
        }

        public static string ReadFileAsString(string relativeRoute)
        {
            try
            {
                StreamReader sr = new StreamReader(HttpContext.Current.Server.MapPath(relativeRoute));
                string content = sr.ReadToEndAsync().Result;
                return content;
            }
            catch
            {
                return "";
            }
        }
    }
}