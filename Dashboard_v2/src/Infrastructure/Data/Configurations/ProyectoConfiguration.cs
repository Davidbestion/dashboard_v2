using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

/// <summary>
/// TPT: tabla "Proyectos" contiene solo los campos comunes a toda la jerarquía.
/// Cada tipo concreto (y la clase intermedia ProyectoEnEjecucion) tiene su propia tabla.
/// </summary>
public class ProyectoConfiguration : IEntityTypeConfiguration<Proyecto>
{
    public void Configure(EntityTypeBuilder<Proyecto> builder)
    {
        builder.ToTable("Proyectos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasMaxLength(450);
        builder.Property(p => p.Titulo).IsRequired().HasMaxLength(500);
        builder.Property(p => p.JefeId).IsRequired().HasMaxLength(450);
        builder.Property(p => p.ClasificacionId).IsRequired().HasMaxLength(450);
        builder.Property(p => p.AreaId).IsRequired().HasMaxLength(450);

        // FK: Proyecto → User (jefe). Restrict: no se puede eliminar un usuario que sea jefe de un proyecto.
        builder.HasOne(p => p.JefeUsuario)
            .WithMany(u => u.ProyectosComoJefe)
            .HasForeignKey(p => p.JefeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Clasificacion)
            .WithMany(c => c.Proyectos)
            .HasForeignKey(p => p.ClasificacionId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK: Proyecto → Area (1:N). Restrict: no se puede eliminar un área que tenga proyectos.
        builder.HasOne(p => p.Area)
            .WithMany(a => a.Proyectos)
            .HasForeignKey(p => p.AreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProyectoEnRevisionConfiguration : IEntityTypeConfiguration<ProyectoEnRevision>
{
    public void Configure(EntityTypeBuilder<ProyectoEnRevision> builder)
    {
        builder.ToTable("ProyectosEnRevision");
        builder.Property(p => p.Situacion).IsRequired().HasMaxLength(300);
        builder.Property(p => p.Tipo).IsRequired().HasMaxLength(200);
    }
}

public class ProyectoEnEjecucionConfiguration : IEntityTypeConfiguration<ProyectoEnEjecucion>
{
    public void Configure(EntityTypeBuilder<ProyectoEnEjecucion> builder)
    {
        builder.ToTable("ProyectosEnEjecucion");
        builder.Property(p => p.EstadoDeEjecucion).IsRequired().HasMaxLength(200);
        builder.Property(p => p.CodigoProyecto).IsRequired().HasMaxLength(100);
        builder.Property(p => p.EntidadEjecutoraPrincipal).IsRequired().HasMaxLength(500);
        builder.Property(p => p.EntidadEjecutoraParticipante).HasMaxLength(500);
        builder.Property(p => p.ContribucionSectoresEstrategicos).HasMaxLength(1000);
        builder.Property(p => p.ContribucionEjesEstrategicos).HasMaxLength(1000);
    }
}

public class ProyectoEmpresarialConfiguration : IEntityTypeConfiguration<ProyectoEmpresarial>
{
    public void Configure(EntityTypeBuilder<ProyectoEmpresarial> builder)
    {
        builder.ToTable("ProyectosEmpresariales");
        builder.Property(p => p.Empresa).IsRequired().HasMaxLength(300);
    }
}

public class ProyectoApoyoProgramaConfiguration : IEntityTypeConfiguration<ProyectoApoyoPrograma>
{
    public void Configure(EntityTypeBuilder<ProyectoApoyoPrograma> builder)
    {
        builder.ToTable("ProyectosApoyoPrograma");
        builder.Property(p => p.NombrePrograma).IsRequired().HasMaxLength(300);
    }
}

public class ProyectoDesarrolloLocalConfiguration : IEntityTypeConfiguration<ProyectoDesarrolloLocal>
{
    public void Configure(EntityTypeBuilder<ProyectoDesarrolloLocal> builder)
    {
        builder.ToTable("ProyectosDesarrolloLocal");
        builder.Property(p => p.Municipio).IsRequired().HasMaxLength(200);
    }
}

public class ProyectoNoEmpresarialConfiguration : IEntityTypeConfiguration<ProyectoNoEmpresarial>
{
    public void Configure(EntityTypeBuilder<ProyectoNoEmpresarial> builder)
    {
        builder.ToTable("ProyectosNoEmpresariales");
        builder.Property(p => p.EntidadNoEmpresarial).IsRequired().HasMaxLength(300);
    }
}

public class ProyectoColabInternacionalConfiguration : IEntityTypeConfiguration<ProyectoColabInternacional>
{
    public void Configure(EntityTypeBuilder<ProyectoColabInternacional> builder)
    {
        builder.ToTable("ProyectosColaboracionInternacional");
        builder.Property(p => p.FuenteFinanciacion).IsRequired().HasMaxLength(300);
        builder.Property(p => p.TerminosReferencia).IsRequired().HasMaxLength(1000);
    }
}

public class ProyectoPNAPConfiguration : IEntityTypeConfiguration<ProyectoPNAP>
{
    public void Configure(EntityTypeBuilder<ProyectoPNAP> builder)
    {
        builder.ToTable("ProyectosPNAP");
        builder.Property(p => p.FinanciamientoUH).IsRequired().HasMaxLength(300);
    }
}
