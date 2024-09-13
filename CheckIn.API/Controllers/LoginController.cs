using CheckIn.API.Models;
using CheckIn.API.Models.ModelCliente;
using CheckIn.API.Models.ModelMain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.UI.WebControls;
using Login = CheckIn.API.Models.ModelCliente.Login;

namespace CheckIn.API.Controllers
{
    [EnableCors("*", "*", "*")]
    public class LoginController: ApiController
    {
        ModelLicencias dbLogin = new ModelLicencias();
        ModelCliente db;
        G G = new G();


        [Route("api/Login/Compañias")]
        public async Task<HttpResponseMessage> GetCompañiasAsync([FromUri]  string Email = "")
        {
            try
            {
                if (string.IsNullOrEmpty(Email))
                {
                    throw new Exception("Se debe indicar el email");
                }

                var LicenciaUsuarios = dbLogin.LicUsuarios.Where(a => a.Email.ToUpper().Contains(Email.ToUpper())).ToList();
                if (LicenciaUsuarios == null)
                {
                    throw new Exception("Usuario no existe");
                }
                List<LicEmpresas> empresas = new List<LicEmpresas>();

                foreach(var item in LicenciaUsuarios)
                {
                    var emp = dbLogin.LicEmpresas.Where(a => a.CedulaJuridica == item.CedulaJuridica && a.Activo == true).FirstOrDefault();
                    empresas.Add(emp);
                }

                return Request.CreateResponse(HttpStatusCode.OK, empresas);

            }
            catch (Exception ex)
            {
                 

                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }


        [Route("api/Login/Conectar")]
        public async Task<HttpResponseMessage> GetLoginAsync([FromUri] string email, string clave, string CedulaJuridica = "")
        {
            try
            {
                if(string.IsNullOrEmpty(CedulaJuridica))
                {
                    throw new Exception("Se debe indicar el número de compañía a la que perteneces");
                }

                var LicenciaUsuarios = dbLogin.LicUsuarios.Where(a => a.Email.ToUpper().Contains(email.ToUpper()) && a.CedulaJuridica == CedulaJuridica).FirstOrDefault();
                if(LicenciaUsuarios == null)
                {
                    throw new Exception("Usuario no existe");
                }
                var Licencia = dbLogin.LicEmpresas.Where(a => a.CedulaJuridica == LicenciaUsuarios.CedulaJuridica).FirstOrDefault();

                if(Licencia == null)
                {
                    throw new Exception("Empresa no existe");
                }

                if(!Licencia.Activo.Value)
                {
                    throw new Exception("Empresa no se encuentra activa");
                }
                
                if(!LicenciaUsuarios.Activo)
                {
                    throw new Exception("Este usuario no esta activo");
                }

               if(! BCrypt.Net.BCrypt.Verify(clave, LicenciaUsuarios.Clave))
                {
                    throw new Exception("Clave o Usuario incorrectos");
                }


                var BD = Licencia.CadenaConexionBD;

                db = new ModelCliente(BD);
                var token = TokenGenerator.GenerateTokenJwt(Licencia.CedulaJuridica, BD);

                DevolucionLogin de = new DevolucionLogin();
                var user = db.Login.Where(a => a.Email.ToUpper().Contains(LicenciaUsuarios.Email.ToUpper())).FirstOrDefault();

                if(user == null)
                {
                    throw new Exception("Usuario no existe");
                }


                var SeguridadModulos = db.SeguridadRolesModulos.Where(a => a.CodRol == user.idRol).ToList();
                var param = db.Parametros.FirstOrDefault();


                de.idLogin = user.id ;
                de.NombreUsuario = LicenciaUsuarios.Nombre;
                de.Email = LicenciaUsuarios.Email;
                de.CedulaJuridica = LicenciaUsuarios.CedulaJuridica;
                de.FechaVencimiento = Licencia.FechaVencimiento.Value;
                de.token = token;
                de.idRol = user.idRol.Value;
                de.Seguridad = SeguridadModulos;
                de.UrlLogo = param.UrlImagenesApp + param.UrlLogo;
                de.CambiarClave = user.FechaVencimientoClave.Date <= DateTime.Now.Date ? true : user.CambiarClave;
                de.Pais = Licencia.CadenaConexionSAP;

                BitacoraLogin bl = new BitacoraLogin();
                bl.idUsuario = de.idLogin;
                bl.IP =  HttpContext.Current.Request.UserHostAddress;
                bl.Detalle = "El usuario " + de.NombreUsuario + ", con el id: " + de.idLogin + " se ha logueado";
                bl.Fecha = DateTime.Now;
                db.BitacoraLogin.Add(bl);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, de);

            }
            catch (Exception ex)
            {
                //BitacoraErrores be = new BitacoraErrores();
                //be.Descripcion = ex.Message;
                //be.StackTrace = ex.StackTrace;
                //be.Metodo = "LOGIN de Usuario";
                //be.Fecha = DateTime.Now;
                //db.BitacoraErrores.Add(be);
                //db.SaveChanges();
                //G.CerrarConexionAPP(db);
                G.GuardarTxt("ErrorLogin.txt", ex.Message + " => " + ex.StackTrace);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                G.AbrirConexionAPP(out db);
                var Login = db.Login.ToList();

                if (!string.IsNullOrEmpty(filtro.Texto))
                {
                    Login = Login.Where(a => a.Nombre.ToUpper().Contains(filtro.Texto.ToUpper()) || a.Email.ToUpper().Contains(filtro.Texto.ToUpper())).ToList();
                }
                

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Login);

            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "GET de Usuario";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [Route("api/Login/Consultar")]
        public async Task<HttpResponseMessage> GetOne([FromUri] int id)
        {
            try
            {
                G.AbrirConexionAPP(out db);
                var Login = db.Login.Where(a => a.id == id).FirstOrDefault();

                if(Login == null)
                {
                    throw new Exception("Usuario no existe");
                }


                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK, Login);

            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "GET ONE de Usuario";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage Post([FromBody] LoginViewModel usuario)
        {
            try
            {
                var Empresa = dbLogin.LicEmpresas.Where(a => a.CedulaJuridica == usuario.CedulaJuridica).FirstOrDefault();
                if(Empresa == null)
                {
                    throw new Exception("Empresa no existe");
                }

                db = new ModelCliente(Empresa.CadenaConexionBD);
            }
            catch (Exception ex)
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
            var t = db.Database.BeginTransaction();
            var d = dbLogin.Database.BeginTransaction();
            try
            {

                var User = dbLogin.LicUsuarios.Where(a => a.Email.ToUpper().Contains(usuario.Email.ToUpper()) && a.Activo == true).FirstOrDefault();
                var usuario1 = db.Login.Where(a => a.Email.ToUpper().Contains(usuario.Email.ToUpper()) && a.Activo == true).FirstOrDefault();
                if (usuario1 == null)
                {
                    User = new LicUsuarios();
                    User.Nombre = usuario.Nombre;
                    User.Clave = BCrypt.Net.BCrypt.HashPassword(usuario.Clave);
                    User.Email = usuario.Email;
                    User.Activo = true;
                    User.CedulaJuridica = usuario.CedulaJuridica;

                    Login login = new Login();
                    login.Nombre = User.Nombre;
                    login.Clave = User.Clave;
                    login.Activo = true;
                    login.idRol = usuario.idRol;
                    login.Email = User.Email;
                    login.CardCode = usuario.CardCode;
                    login.idLoginAceptacion = usuario.idLoginAceptacion;
                    login.CambiarClave = true;
                    login.CambioFecha = false;
                    var Parametros = db.Parametros.FirstOrDefault();
                    login.FechaVencimientoClave = Parametros.DiasVencimiento != 0 ? DateTime.Now.AddDays(Parametros.DiasVencimiento) : DateTime.Now.AddYears(99);
                    db.Login.Add(login);

                    dbLogin.LicUsuarios.Add(User);
                    dbLogin.SaveChanges();
                    db.SaveChanges();

                    if(login.idLoginAceptacion == 0)
                    {

                        db.Entry(login).State = EntityState.Modified;
                        login.idLoginAceptacion = login.id;
                        db.SaveChanges();

                    }

                    d.Commit();
                    t.Commit();
                }
                else
                {
                    throw new Exception("Este usuario YA existe");
                }

                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message ;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Insercion de Usuario";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();

                t.Rollback();
                d.Rollback();
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        //Actualiza la contraseña del usuario 
        [HttpPut]
        [Route("api/Login/Actualizar")]
        public HttpResponseMessage Put([FromBody] LoginViewModel usuario)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var User = db.Login.Where(a => a.id == usuario.id).FirstOrDefault(); //a.Email.ToUpper().Contains(usuario.Email.ToUpper())
                var Usuario = dbLogin.LicUsuarios.Where(a => a.Email.ToUpper().Contains(User.Email.ToUpper()) && a.CedulaJuridica == usuario.CedulaJuridica).FirstOrDefault();

                if (Usuario != null && User != null)
                {
                    dbLogin.Entry(Usuario).State = EntityState.Modified;
                    db.Entry(User).State = EntityState.Modified;

                    if (!string.IsNullOrEmpty(usuario.Clave))
                    {
                        if (BCrypt.Net.BCrypt.Verify(usuario.Clave, Usuario.Clave))
                        {
                            throw new Exception("La clave a cambiar no debe ser igual a la anterior");
                        }
                        Usuario.Clave = BCrypt.Net.BCrypt.HashPassword(usuario.Clave);
                        User.Clave = Usuario.Clave;
                        User.CambiarClave = false;
                        var Parametros = db.Parametros.FirstOrDefault();
                        User.FechaVencimientoClave = DateTime.Now.AddDays(Parametros.DiasVencimiento);
                    }
       
                    if (!string.IsNullOrEmpty(usuario.Nombre))
                    {
                        Usuario.Nombre = usuario.Nombre;
                        User.Nombre = Usuario.Nombre;
                    }

                    if (!string.IsNullOrEmpty(usuario.Email))
                    {
                        Usuario.Email = usuario.Email;
                        User.Email = Usuario.Email;
                    }

                    if (usuario.idRol > 0)
                    {
                        User.idRol = usuario.idRol;
                    }
                    if(usuario.idLoginAceptacion > 0)
                    {
                        User.idLoginAceptacion = usuario.idLoginAceptacion;
                    }

                    if(!string.IsNullOrEmpty(usuario.CardCode))
                    {
                        User.CardCode = usuario.CardCode;
                    }
                    User.CambioFecha = User.CambioFecha;
                    dbLogin.SaveChanges();
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Usuario no existe");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {

                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message + " -> " + usuario.CedulaJuridica;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Actualizacion de Usuario";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();

                G.CerrarConexionAPP(db);
                G.GuardarTxt("ErrorEditarUsuario.txt", ex.Message + " => " + ex.StackTrace);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpDelete]
        [Route("api/Login/Eliminar")]
        public HttpResponseMessage Delete([FromUri] int id, string CedulaJuridica)
        {
            try
            {
                G.AbrirConexionAPP(out db);

                var User = db.Login.Where(a => a.id == id).FirstOrDefault();
                var Usuario = dbLogin.LicUsuarios.Where(a => a.Email.ToUpper().Contains(User.Email.ToUpper()) && a.CedulaJuridica == CedulaJuridica).FirstOrDefault();


                if (Usuario != null && User != null)
                {

                    db.Entry(User).State = EntityState.Modified;
                    dbLogin.Entry(Usuario).State = EntityState.Modified;

                    if(Usuario.Activo)
                    {
                        Usuario.Activo = false;
                        User.Activo = Usuario.Activo;

                    }
                    else
                    {
                        Usuario.Activo = true;
                        User.Activo = Usuario.Activo;
                    }



                    dbLogin.SaveChanges();
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Usuario no existe");
                }
                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {


                BitacoraErrores be = new BitacoraErrores();
                be.Descripcion = ex.Message;
                be.StackTrace = ex.StackTrace;
                be.Metodo = "Desactivar usuario";
                be.Fecha = DateTime.Now;
                db.BitacoraErrores.Add(be);
                db.SaveChanges();


                G.CerrarConexionAPP(db);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }

    internal class DevolucionLogin
    {
        public DevolucionLogin()
        {
        }
        public int idLogin { get; set; }
        public string NombreUsuario { get; set; }
        public string CedulaJuridica { get; set; }
        public string Email { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int idRol { get; set; }
        public string token { get; set; }
        public string UrlLogo { get; set; }
        public bool CambiarClave { get; set; }
        public string Pais { get; set; }
        public List<SeguridadRolesModulos> Seguridad { get; set; }
    }
}