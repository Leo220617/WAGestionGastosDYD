namespace CheckIn.API.Models.ModelMain
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class ModelLicencias : DbContext
    {
        public ModelLicencias()
            : base("name=ModelLicencias")
        {
        }

        public virtual DbSet<LicEmpresas> LicEmpresas { get; set; }
        public virtual DbSet<LicUsuarios> LicUsuarios { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LicEmpresas>()
                .Property(e => e.CedulaJuridica)
                .IsUnicode(false);

            modelBuilder.Entity<LicEmpresas>()
                .Property(e => e.NombreEmpresa)
                .IsUnicode(false);

            modelBuilder.Entity<LicEmpresas>()
                .Property(e => e.CadenaConexionBD)
                .IsUnicode(false);

            modelBuilder.Entity<LicEmpresas>()
                .Property(e => e.CadenaConexionSAP)
                .IsUnicode(false);
        }
    }
}
