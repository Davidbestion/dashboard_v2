using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Dashboard_v2.Application.FunctionalTests.Infrastructure;

using static Testing;

/// <summary>
/// Tests de infraestructura/esquema: verifican que el modelo EF Core refleja
/// correctamente la estructura esperada de la BD y que los comportamientos
/// de cascada están configurados según la especificación del dominio.
/// </summary>
[TestFixture]
public class SchemaTests : BaseTestFixture
{
    // ── Entidades en el modelo ────────────────────────────────────────────────

    [TestCase(typeof(Publication))]
    [TestCase(typeof(Norma))]
    [TestCase(typeof(TipoNorma))]
    [TestCase(typeof(Award))]
    [TestCase(typeof(Patente))]
    [TestCase(typeof(Registro))]
    [TestCase(typeof(ProductoComercializado))]
    [TestCase(typeof(TipoProductoComercializado))]
    [TestCase(typeof(Red))]
    [TestCase(typeof(Universidad))]
    [TestCase(typeof(Area))]
    [TestCase(typeof(Clasificacion))]
    [TestCase(typeof(User))]
    public async Task EntityType_IsRegisteredInEfCoreModel(Type entityType)
    {
        await ExecuteDbContextAsync(db =>
        {
            var metadata = db.Model.FindEntityType(entityType);
            metadata.ShouldNotBeNull($"El tipo {entityType.Name} debe estar registrado en el modelo EF Core");
            return Task.CompletedTask;
        });
    }

    // ── Nombres de tabla ──────────────────────────────────────────────────────

    [Test]
    public async Task Norma_MappedToTable_Normas()
    {
        await ExecuteDbContextAsync(db =>
        {
            var tableName = db.Model.FindEntityType(typeof(Norma))!.GetTableName();
            tableName.ShouldBe("Normas");
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task TipoNorma_MappedToTable_TiposNorma()
    {
        await ExecuteDbContextAsync(db =>
        {
            var tableName = db.Model.FindEntityType(typeof(TipoNorma))!.GetTableName();
            tableName.ShouldBe("TiposNorma");
            return Task.CompletedTask;
        });
    }

    // ── Configuración de propiedades ──────────────────────────────────────────

    [Test]
    public async Task Norma_TipoNormaId_IsNullable()
    {
        await ExecuteDbContextAsync(db =>
        {
            var entityType = db.Model.FindEntityType(typeof(Norma))!;
            var property = entityType.FindProperty(nameof(Norma.TipoNormaId))!;
            property.IsNullable.ShouldBeTrue("TipoNormaId debe ser nullable para permitir normas sin tipo");
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task Norma_TipoNormaFk_HasSetNullDeleteBehavior()
    {
        await ExecuteDbContextAsync(db =>
        {
            var entityType = db.Model.FindEntityType(typeof(Norma))!;
            var fk = entityType.GetForeignKeys()
                .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(TipoNorma));

            fk.ShouldNotBeNull("Debe existir una FK de Norma hacia TipoNorma");
            fk!.DeleteBehavior.ShouldBe(DeleteBehavior.SetNull,
                "Al borrar un TipoNorma, las Normas vinculadas deben quedar con TipoNormaId = null");
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task TipoNorma_Nombre_IsRequired()
    {
        await ExecuteDbContextAsync(db =>
        {
            var entityType = db.Model.FindEntityType(typeof(TipoNorma))!;
            var property = entityType.FindProperty(nameof(TipoNorma.Nombre))!;
            property.IsNullable.ShouldBeFalse("TipoNorma.Nombre debe ser obligatorio");
            return Task.CompletedTask;
        });
    }

    // ── Cascada real en la BD ─────────────────────────────────────────────────

    [Test]
    public async Task DeleteTipoNorma_SetsNullOnLinkedNorma()
    {
        // Arrange: crear institución, tipo de norma y norma vinculada
        var (institutionId, tipoNormaId, normaId) = await ExecuteDbContextAsync(async db =>
        {
            var inst = new Institution { Id = Guid.NewGuid().ToString(), Nombre = "Inst Schema Test" };
            db.Institutions.Add(inst);

            var tipo = new TipoNorma { Nombre = "Tipo Cascade Test" };
            db.TiposNorma.Add(tipo);

            await db.SaveChangesAsync();

            var norma = new Norma
            {
                Titulo = "Norma Cascade Test",
                TipoNormaId = tipo.Id,
                InstitutionId = inst.Id
            };
            db.Normas.Add(norma);
            await db.SaveChangesAsync();

            return (inst.Id, tipo.Id, norma.Id);
        });

        // Verificar que la norma tiene el tipo asignado
        var tipoAntes = await ExecuteDbContextAsync(db =>
            db.Normas.Where(n => n.Id == normaId).Select(n => n.TipoNormaId).FirstAsync());
        tipoAntes.ShouldBe(tipoNormaId);

        // Act: eliminar el TipoNorma
        await ExecuteDbContextAsync(async db =>
        {
            var tipo = await db.TiposNorma.FindAsync(tipoNormaId);
            if (tipo != null)
            {
                db.TiposNorma.Remove(tipo);
                await db.SaveChangesAsync();
            }
        });

        // Assert: la norma debe tener TipoNormaId = null (SetNull cascade)
        var tipoDespues = await ExecuteDbContextAsync(db =>
            db.Normas.Where(n => n.Id == normaId).Select(n => n.TipoNormaId).FirstAsync());
        tipoDespues.ShouldBeNull("El TipoNormaId debe ser null tras borrar el TipoNorma (DeleteBehavior.SetNull)");
    }
}
