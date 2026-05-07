using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Redes;

/// <summary>
/// Verifica que la configuración EF Core de Red y RedCoordinada
/// (RedConfiguration / RedCoordinadaConfiguration) define correctamente
/// las propiedades, restricciones y relaciones del modelo.
/// </summary>
public class RedEfConfigurationTests
{
    private static IModel BuildModel()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("model-inspection")
            .Options;
        using var db = new ApplicationDbContext(opts);
        return db.Model;
    }

    private static readonly IModel _model = BuildModel();

    // ── Red: tabla y clave ───────────────────────────────────────────────────

    [Test]
    public void Red_TableName_IsReds()
    {
        var et = _model.FindEntityType(typeof(Red))!;

        et.GetTableName().ShouldBe("Reds");
    }

    [Test]
    public void Red_PrimaryKey_IsId()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var pk = et.FindPrimaryKey()!;

        pk.Properties.Single().Name.ShouldBe(nameof(Red.Id));
    }

    // ── Red: propiedades ─────────────────────────────────────────────────────

    [Test]
    public void Red_Id_HasMaxLength450()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var prop = et.FindProperty(nameof(Red.Id))!;

        prop.GetMaxLength().ShouldBe(450);
    }

    [Test]
    public void Red_Nombre_IsRequired()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var prop = et.FindProperty(nameof(Red.Nombre))!;

        prop.IsNullable.ShouldBeFalse();
    }

    [Test]
    public void Red_Nombre_HasMaxLength500()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var prop = et.FindProperty(nameof(Red.Nombre))!;

        prop.GetMaxLength().ShouldBe(500);
    }

    [Test]
    public void Red_CountryId_IsNullable()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var prop = et.FindProperty(nameof(Red.CountryId))!;

        prop.IsNullable.ShouldBeTrue();
    }

    [Test]
    public void Red_Tipo_ExistsInModel()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var prop = et.FindProperty(nameof(Red.Tipo));

        prop.ShouldNotBeNull();
    }

    // ── Red: relaciones ──────────────────────────────────────────────────────

    [Test]
    public void Red_Country_ForeignKey_IsCountryId()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var fk = et.GetForeignKeys()
            .SingleOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Country));

        fk.ShouldNotBeNull();
        fk!.Properties.Single().Name.ShouldBe(nameof(Red.CountryId));
    }

    [Test]
    public void Red_Country_DeleteBehavior_IsRestrict()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var fk = et.GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(Country));

        fk.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Test]
    public void Red_Events_Relationship_IsOneToMany()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var nav = et.GetNavigations().SingleOrDefault(n => n.Name == nameof(Red.Events));

        nav.ShouldNotBeNull();
        nav!.IsCollection.ShouldBeTrue();
    }

    [Test]
    public void Red_Events_DeleteBehavior_IsSetNull()
    {
        var eventEt = _model.FindEntityType(typeof(Event))!;
        var fk = eventEt.GetForeignKeys()
            .SingleOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Red));

        fk.ShouldNotBeNull();
        fk!.DeleteBehavior.ShouldBe(DeleteBehavior.SetNull);
    }

    [Test]
    public void Red_Usuarios_Relationship_IsSkipNavigation()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var nav = et.GetSkipNavigations().SingleOrDefault(n => n.Name == nameof(Red.Usuarios));

        nav.ShouldNotBeNull();
        nav!.IsCollection.ShouldBeTrue();
    }

    [Test]
    public void Red_RedesCoordinadas_Relationship_IsCollectionNavigation()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var nav = et.GetNavigations().SingleOrDefault(n => n.Name == nameof(Red.RedesCoordinadas));

        nav.ShouldNotBeNull();
        nav!.IsCollection.ShouldBeTrue();
    }

    // ── RedCoordinada: tabla y clave ─────────────────────────────────────────

    [Test]
    public void RedCoordinada_TableName_IsRedesCoordinadas()
    {
        var et = _model.FindEntityType(typeof(RedCoordinada))!;

        et.GetTableName().ShouldBe("RedesCoordinadas");
    }

    [Test]
    public void RedCoordinada_PrimaryKey_IsId()
    {
        var et = _model.FindEntityType(typeof(RedCoordinada))!;
        var pk = et.FindPrimaryKey()!;

        pk.Properties.Single().Name.ShouldBe(nameof(RedCoordinada.Id));
    }

    // ── RedCoordinada: propiedades ───────────────────────────────────────────

    [Test]
    public void RedCoordinada_Id_HasMaxLength450()
    {
        var et = _model.FindEntityType(typeof(RedCoordinada))!;
        var prop = et.FindProperty(nameof(RedCoordinada.Id))!;

        prop.GetMaxLength().ShouldBe(450);
    }

    [Test]
    public void RedCoordinada_RedId_IsRequired()
    {
        var et = _model.FindEntityType(typeof(RedCoordinada))!;
        var prop = et.FindProperty(nameof(RedCoordinada.RedId))!;

        prop.IsNullable.ShouldBeFalse();
    }

    [Test]
    public void RedCoordinada_RedId_HasMaxLength450()
    {
        var et = _model.FindEntityType(typeof(RedCoordinada))!;
        var prop = et.FindProperty(nameof(RedCoordinada.RedId))!;

        prop.GetMaxLength().ShouldBe(450);
    }

    [Test]
    public void RedCoordinada_AreaId_IsRequired()
    {
        var et = _model.FindEntityType(typeof(RedCoordinada))!;
        var prop = et.FindProperty(nameof(RedCoordinada.AreaId))!;

        prop.IsNullable.ShouldBeFalse();
    }

    [Test]
    public void RedCoordinada_CoordinadorId_IsRequired()
    {
        var et = _model.FindEntityType(typeof(RedCoordinada))!;
        var prop = et.FindProperty(nameof(RedCoordinada.CoordinadorId))!;

        prop.IsNullable.ShouldBeFalse();
    }

    // ── RedCoordinada: relaciones ────────────────────────────────────────────

    [Test]
    public void RedCoordinada_Red_ForeignKey_IsRedId()
    {
        var et = _model.FindEntityType(typeof(RedCoordinada))!;
        var fk = et.GetForeignKeys()
            .SingleOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Red));

        fk.ShouldNotBeNull();
        fk!.Properties.Single().Name.ShouldBe(nameof(RedCoordinada.RedId));
    }

    [Test]
    public void RedCoordinada_Red_DeleteBehavior_IsCascade()
    {
        var et = _model.FindEntityType(typeof(RedCoordinada))!;
        var fk = et.GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(Red));

        fk.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }

    [Test]
    public void RedCoordinada_Coordinador_DeleteBehavior_IsRestrict()
    {
        var et = _model.FindEntityType(typeof(RedCoordinada))!;
        var fk = et.GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(User));

        fk.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Test]
    public void RedCoordinada_HasUniqueIndex_OnRedIdAndAreaId()
    {
        var et = _model.FindEntityType(typeof(RedCoordinada))!;
        var uniqueIndex = et.GetIndexes()
            .SingleOrDefault(ix => ix.IsUnique &&
                ix.Properties.Count == 2 &&
                ix.Properties.Any(p => p.Name == nameof(RedCoordinada.RedId)) &&
                ix.Properties.Any(p => p.Name == nameof(RedCoordinada.AreaId)));

        uniqueIndex.ShouldNotBeNull();
    }
}
