using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

/// <summary>
/// Configura el mapeo EF Core de la entidad <see cref="FileDeletionJob"/> a la tabla
/// <c>FileDeletionJobs</c>.
///
/// <para>Decisiones de diseño relevantes:</para>
/// <list type="bullet">
///   <item>
///     La FK a <see cref="StoredFile"/> tiene <c>ON DELETE SET NULL</c>: si alguien elimina
///     el registro <see cref="StoredFile"/> antes de que el job se procese, el job queda
///     con <c>StoredFileId = null</c> pero sus campos <c>ObjectKey</c> / <c>BucketName</c>
///     (copiados al encolar) permiten igualmente borrar el objeto en MinIO.
///   </item>
///   <item>
///     El índice en <c>ScheduledAt</c> acelera la consulta del background service,
///     que carga todos los jobs ordenados por fecha de programación para procesar
///     los más antiguos primero.
///   </item>
/// </list>
/// </summary>
public sealed class FileDeletionJobConfiguration : IEntityTypeConfiguration<FileDeletionJob>
{
    public void Configure(EntityTypeBuilder<FileDeletionJob> builder)
    {
        builder.ToTable("FileDeletionJobs");
        builder.HasKey(j => j.Id);

        // ObjectKey y BucketName son obligatorios; se copian del StoredFile al encolar
        builder.Property(j => j.ObjectKey).IsRequired().HasMaxLength(1024);
        builder.Property(j => j.BucketName).IsRequired().HasMaxLength(256);

        builder.Property(j => j.ScheduledAt).IsRequired();
        builder.Property(j => j.Attempts).IsRequired().HasDefaultValue(0);
        builder.Property(j => j.LastAttemptAt).IsRequired(false);

        // FK opcional a StoredFile con ON DELETE SET NULL:
        // el job puede sobrevivir al StoredFile usando ObjectKey/BucketName copiados.
        builder.HasOne<StoredFile>()
            .WithMany()
            .HasForeignKey(j => j.StoredFileId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Índice para que el background service ordene eficientemente por fecha de programación
        builder.HasIndex(j => j.ScheduledAt)
            .HasDatabaseName("IX_FileDeletionJobs_ScheduledAt");
    }
}
