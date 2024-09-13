using CheckIn.API.Models.ModelCliente;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckIn.API.Controllers
{
    public class Conexion
    {
        public readonly static Conexion _instance = new Conexion();
        public static Company _company = null;
        ModelCliente db;
        G G = new G();

        private static Conexion Instance
        {
            get
            {
                return _instance;
            }
        }

        public static Company Company
        {
            get
            {

                if (_company == null)
                    new Conexion().DoSapConnection();

                

                var ins = Instance;
                return _company;
            }
        }

        public  int DoSapConnection()
        {
            try
            {
                G.AbrirConexionAPP(out db);
                var Datos = db.ConexionSAP.FirstOrDefault();
                _company = new Company
                {
                    Server = Datos.ServerSQL,
                    LicenseServer = Datos.ServerLicense,
                    DbServerType = getBDType(Datos.SQLType),
                    language = BoSuppLangs.ln_English,
                    CompanyDB = Datos.SQLBD,
                    UserName = Datos.SAPUser,
                    Password = Datos.SAPPass
                };

                var resp = _company.Connect();


                G.GuardarTxt("BitacoraConexion.txt", _company.DbServerType + _company.LicenseServer + _company.CompanyDB + _company.UserName);

                if (resp != 0)
                {
                    var msg = _company.GetLastErrorDescription();
                    return -1;
                }
                G.CerrarConexionAPP(db);
                return resp;
            }
            catch (Exception ex)
            {

                G.GuardarTxt("ErrorConexionSAP.txt", ex.Message);
                return -1;
            }
           
        }


        private BoDataServerTypes getBDType(string sql)
        {
            switch (sql)
            {
                case "2005":
                    return BoDataServerTypes.dst_MSSQL2005;
                case "2008":
                    return BoDataServerTypes.dst_MSSQL2008;
                case "2012":
                    return BoDataServerTypes.dst_MSSQL2012;
                case "2014":
                    return BoDataServerTypes.dst_MSSQL2014; 
                case "2016":
                    return  BoDataServerTypes.dst_MSSQL2016;
                case "HANA":
                    return BoDataServerTypes.dst_HANADB;
                default:
                    return BoDataServerTypes.dst_MSSQL;
            }
        }

        public static bool Desconectar()
        {
            try
            {
                if(_company != null)
                {
                    _company = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                
            }
        }

        public string DevuelveCadena()
        {
            G.AbrirConexionAPP(out db);
            var Datos = db.ConexionSAP.FirstOrDefault();

            var sql = "server=" + Datos.ServerSQL + "; database=" + Datos.SQLBD + "; uid=" + Datos.SQLUser + "; pwd=" + Datos.SQLPass + ";";
            G.CerrarConexionAPP(db);
            return sql;
        }
    }
}