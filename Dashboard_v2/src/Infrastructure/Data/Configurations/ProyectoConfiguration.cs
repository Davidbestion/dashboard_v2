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
        builder.HasOne(p => p.JefeUsuario)
            .WithMany(u => u.ProyectosComoJefe)
            .HasForeignKey(p => p.JefeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Clasificacion)
            .WithMany(c => c.Proyectos)
            .HasForeignKey(p => p.ClasificacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Participantes)
            .WithMany(u => u.ProyectosParticipante)
            .UsingEntity(j => j.ToTable("ProyectoParticipantes"));
    }
}

public class ProyectoEnRevisionConfiguration : IEntityTypeConfiguration<ProyectoEnRevision>
{
    public void Configure(EntityTypeBuilder<ProyectoEnRevision> builder)
    {
        builder.ToTable("ProyectosEnRevision");
        builder.Property(p => p.Tipo).IsRequired().HasMaxLength(200);

        builder.HasMany(p => p.Situaciones)
            .WithMany()
            .UsingEntity(j => j.ToTable("ProyectoRevisionSituaciones"));
    }
}

public class ProyectoEnEjecucionConfiguration : IEntityTypeConfiguration<ProyectoEnEjecucion>
{
    public void Configure(EntityTypeBuilder<ProyectoEnEjecucion> builder)
    {
        builder.ToTable("ProyectosEnEjecucion");
        builder.Property(p => p.CodigoProyecto).IsRequired().HasMaxLength(100);

        builder.HasMany(p => p.EstadosDeEjecucion)
            .WithMany()
            .UsingEntity(j => j.ToTable("ProyectoEstados"));

        // Dos M:N hacia Institution desde la misma entidad → tablas distintas obligatorias
        builder.HasMany(p => p.EntidadesEjecutorasPrincipales)
            .WithMany()
            .UsingEntity(j => j.ToTable("ProyectoEntidadesPrincipales"));

        builder.HasMany(p => p.EntidadesEjecutorasParticipantes)
            .WithMany()
            .UsingEntity(j => j.ToTable("ProyectoEntidadesParticipantes"));

        builder.HasMany(p => p.SectoresEstrategicos)
            .WithMany()
            .UsingEntity(j => j.ToTable("ProyectoSectores"));

        builder.HasMany(p => p.EjesEstrategicos)
            .WithMany()
            .UsingEntity(j => j.ToTable("ProyectoEjes"));
    }
}

public class ProyectoEmpresarialConfiguration : IEntityTypeConfiguration<ProyectoEmpresarial>
{
    public void Configure(EntityTypeBuilder<ProyectoEmpresarial> builder)
    {
        builder.ToTable("ProyectosEmpresariales");

        builder.HasMany(p => p.Empresas)
            .WithMany()
            .UsingEntity(j => j.ToTable("ProyectoEmpresas"));
    }
}

public class ProyectoApoyoProgramaConfiguration : IEntityTypeConfiguration<ProyectoApoyoPrograma>
{
    public void Configure(EntityTypeBuilder<ProyectoApoyoPrograma> builder)
    {
        builder.ToTable("ProyectosApoyoPrograma");

        builder.HasMany(p => p.Programas)
            .WithMany()
            .UsingEntity(j => j.ToTable("ProyectoProgramas"));
    }
}

public class ProyectoDesarrolloLocalConfiguration : IEntityTypeConfiguration<ProyectoDesarrolloLocal>
{
    public void Configure(EntityTypeBuilder<ProyectoDesarrolloLocal> builder)
    {
        builder.ToTable("ProyectosDesarrolloLocal");

        builder.HasOne(p => p.Municipio)
            .WithMany()
            .HasForeignKey(p => p.MunicipioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProyectoNoEmpresarialConfiguration : IEntityTypeConfiguration<ProyectoNoEmpresarial>
{
    public void Configure(EntityTypeBuilder<ProyectoNoEmpresarial> builder)
    {
        builder.ToTable("ProyectosNoEmpresariales");

        builder.HasMany(p => p.Entidades)
            .WithMany()
            .UsingEntity(j => j.ToTable("ProyectoEntidades"));
    }
}

public class ProyectoColabInternacionalConfiguration : IEntityTypeConfiguration<ProyectoColabInternacional>
{
    public void Configure(EntityTypeBuilder<ProyectoColabInternacional> builder)
    {
        builder.ToTable("ProyectosColaboracionInternacional");
        builder.Property(p => p.TerminosReferencia).IsRequired().HasMaxLength(1000);

        builder.HasMany(p => p.FuentesFinanciacion)
            .WithMany()
            .UsingEntity(j => j.ToTable("ProyectoPRCIFuentes"));
    }
}

public class ProyectoPNAPConfiguration : IEntityTypeConfiguration<ProyectoPNAP>
{
    public void Configure(EntityTypeBuilder<ProyectoPNAP> builder)
    {
        builder.ToTable("ProyectosPNAP");

        builder.HasMany(p => p.FuentesFinanciacion)
            .WithMany()
            .UsingEntity(j => j.ToTable("ProyectoPNAPFuentes"));
    }
}

public class ProvinciaConfiguration : IEntityTypeConfiguration<Provincia>
{
    public void Configure(EntityTypeBuilder<Provincia> builder)
    {
        builder.ToTable("Provincias");
        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
    }
}

public class MunicipioConfiguration : IEntityTypeConfiguration<Municipio>
{
    public void Configure(EntityTypeBuilder<Municipio> builder)
    {
        builder.ToTable("Municipios");
        builder.Property(m => m.Nombre).IsRequired().HasMaxLength(200);

        builder.HasOne(m => m.Provincia)
            .WithMany(p => p.Municipios)
            .HasForeignKey(m => m.ProvinciaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SectorEstrategicoConfiguration : IEntityTypeConfiguration<SectorEstrategico>
{
    public void Configure(EntityTypeBuilder<SectorEstrategico> builder)
    {
        builder.ToTable("SectoresEstrategicos");
        builder.Property(s => s.Nombre).IsRequired().HasMaxLength(300);
    }
}

public class EjeEstrategicoConfiguration : IEntityTypeConfiguration<EjeEstrategico>
{
    public void Configure(EntityTypeBuilder<EjeEstrategico> builder)
    {
        builder.ToTable("EjesEstrategicos");
        builder.Property(e => e.Nombre).IsRequired().HasMaxLength(300);
    }
}

public class FuenteFinanciacionConfiguration : IEntityTypeConfiguration<FuenteFinanciacion>
{
    public void Configure(EntityTypeBuilder<FuenteFinanciacion> builder)
    {
        builder.ToTable("FuentesFinanciacion");
        builder.Property(f => f.Nombre).IsRequired().HasMaxLength(300);
    }
}

public class EstadoProyectoConfiguration : IEntityTypeConfiguration<EstadoProyecto>
{
    public void Configure(EntityTypeBuilder<EstadoProyecto> builder)
    {
        builder.ToTable("EstadosProyecto");
        builder.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
    }
}

public class SituacionProyectoConfiguration : IEntityTypeConfiguration<SituacionProyecto>
{
    public void Configure(EntityTypeBuilder<SituacionProyecto> builder)
    {
        builder.ToTable("SituacionesProyecto");
        builder.Property(s => s.Nombre).IsRequired().HasMaxLength(200);
    }
}

public class ProgramaConfiguration : IEntityTypeConfiguration<Programa>
{
    public void Configure(EntityTypeBuilder<Programa> builder)
    {
        builder.ToTable("Programas");
        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(300);
    }
}
