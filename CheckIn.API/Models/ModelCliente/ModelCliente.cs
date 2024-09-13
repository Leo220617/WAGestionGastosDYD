namespace CheckIn.API.Models.ModelCliente
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class ModelCliente : DbContext
    {
        public ModelCliente(string connectionString, bool lazyLoadinEnabled = true)
            : base("name=ModelCliente")
        {
            this.Database.Connection.ConnectionString = connectionString;
            
            try
            {
                this.Database.Connection.Open();
                this.Database.CommandTimeout = 300;
            
                this.Configuration.LazyLoadingEnabled = lazyLoadinEnabled;
            }
            catch { }
        }

        public virtual DbSet<CuentasContables> CuentasContables { get; set; }
        public virtual DbSet<DetCompras> DetCompras { get; set; }
        public virtual DbSet<EncCierre> EncCierre { get; set; }
        public virtual DbSet<DetCierre> DetCierre { get; set; }
        public virtual DbSet<EncCompras> EncCompras { get; set; }
        public virtual DbSet<Gastos> Gastos { get; set; }
        public virtual DbSet<Login> Login { get; set; }
        public virtual DbSet<NormasReparto> NormasReparto { get; set; }
        public virtual DbSet<Parametros> Parametros { get; set; }
        public virtual DbSet<Roles> Roles { get; set; }
        public virtual DbSet<BandejaEntrada> BandejaEntrada { get; set; }
        public virtual DbSet<SeguridadModulos> SeguridadModulos { get; set; }
        public virtual DbSet<SeguridadRolesModulos> SeguridadRolesModulos { get; set; }
        public virtual DbSet<Dimensiones> Dimensiones { get; set; }
        public virtual DbSet<CorreosRecepcion> CorreosRecepcion { get; set; }
        public virtual DbSet<ConexionSAP> ConexionSAP { get; set; }
        public virtual DbSet<BitacoraErrores> BitacoraErrores { get; set; }
        public virtual DbSet<Proveedores> Proveedores { get; set; }
        public virtual DbSet<BitacoraLogin> BitacoraLogin { get; set; }
        public virtual DbSet<BitacoraCierres> BitacoraCierres { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CuentasContables>()
                .Property(e => e.Nombre)
                .IsUnicode(false);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.CodEmpresa)
                .IsUnicode(false);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.CodProveedor)
                .IsUnicode(false);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.TipoDocumento)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.NomProveedor)
                .IsUnicode(false);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.CodPro)
                .IsUnicode(false);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.UnidadMedida)
                .IsUnicode(false);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.NomPro)
                .IsUnicode(false);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.PrecioUnitario)
                .HasPrecision(19, 4);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.CodCliente)
                .IsUnicode(false);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.NomCliente)
                .IsUnicode(false);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.MontoTotal)
                .HasPrecision(19, 4);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.MontoDescuento)
                .HasPrecision(19, 4);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.SubTotal)
                .HasPrecision(19, 4);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.ImpuestoTarifa)
                .HasPrecision(12, 4);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.ImpuestoMonto)
                .HasPrecision(19, 4);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.MontoTotalLinea)
                .HasPrecision(19, 4);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.Estado)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<DetCompras>()
                .Property(e => e.Observacion)
                .IsUnicode(false);

            modelBuilder.Entity<EncCierre>()
                .Property(e => e.SubTotal)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCierre>()
                .Property(e => e.Descuento)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCierre>()
                .Property(e => e.Impuestos)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.CodEmpresa)
                .IsUnicode(false);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.CodProveedor)
                .IsUnicode(false);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TipoDocumento)
                .IsUnicode(false);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TipoIdentificacionCliente)
                .IsUnicode(false);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.CodCliente)
                .IsUnicode(false);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.NomCliente)
                .IsUnicode(false);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.EmailCliente)
                .IsUnicode(false);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.CondicionVenta)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.ClaveHacienda)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.ConsecutivoHacienda)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.MedioPago)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.CodMoneda)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalServGravados)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalServExentos)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalMercanciasGravadas)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalMercanciasExentas)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalExento)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalVenta)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalDescuentos)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalVentaNeta)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalImpuesto)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalComprobante)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.XmlFacturaRecibida)
                .IsUnicode(false);
 
            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalServExonerado)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalMercExonerada)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalExonerado)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalIVADevuelto)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.TotalOtrosCargos)
                .HasPrecision(19, 4);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.CodigoActividadEconomica)
                .IsUnicode(false);

            modelBuilder.Entity<EncCompras>()
                .Property(e => e.PdfFactura)
                .IsUnicode(false);

            modelBuilder.Entity<Gastos>()
                .Property(e => e.Nombre)
                .IsUnicode(false);

            modelBuilder.Entity<Gastos>()
                .Property(e => e.PalabrasClave)
                .IsUnicode(false);

            modelBuilder.Entity<Login>()
                .Property(e => e.Email)
                .IsUnicode(false);

            modelBuilder.Entity<Login>()
                .Property(e => e.Nombre)
                .IsUnicode(false);

            modelBuilder.Entity<Login>()
                .Property(e => e.Clave)
                .IsUnicode(false);

            modelBuilder.Entity<NormasReparto>()
                .Property(e => e.Nombre)
                .IsUnicode(false);

            modelBuilder.Entity<Parametros>()
                .Property(e => e.RecepcionEmail)
                .IsUnicode(false);

            modelBuilder.Entity<Parametros>()
                .Property(e => e.RecepcionPassword)
                .IsUnicode(false);

            modelBuilder.Entity<Parametros>()
                .Property(e => e.RecepcionHostName)
                .IsUnicode(false);

            modelBuilder.Entity<Roles>()
                .Property(e => e.NombreRol)
                .IsUnicode(false);
        }
    }
}
