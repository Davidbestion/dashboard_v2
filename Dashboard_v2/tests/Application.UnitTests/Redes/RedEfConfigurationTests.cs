using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Redes;

/// <summary>
/// Verifica que la configuración EF Core de Red y ParticipacionEnRed
/// (RedConfiguration / ParticipacionEnRedConfiguration) define correctamente
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
    public void Red_CoordinadorId_IsNullable()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var prop = et.FindProperty(nameof(Red.CoordinadorId))!;

        prop.IsNullable.ShouldBeTrue();
    }

    [Test]
    public void Red_CoordinadorId_HasMaxLength450()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var prop = et.FindProperty(nameof(Red.CoordinadorId))!;

        prop.GetMaxLength().ShouldBe(450);
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
    public void Red_Coordinador_ForeignKey_IsCoordinadorId()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var fk = et.GetForeignKeys()
            .SingleOrDefault(f => f.PrincipalEntityType.ClrType == typeof(User));

        fk.ShouldNotBeNull();
        fk!.Properties.Single().Name.ShouldBe(nameof(Red.CoordinadorId));
    }

    [Test]
    public void Red_Coordinador_DeleteBehavior_IsSetNull()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var fk = et.GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(User));

        fk.DeleteBehavior.ShouldBe(DeleteBehavior.SetNull);
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
    public void Red_Participaciones_Relationship_IsCollectionNavigation()
    {
        var et = _model.FindEntityType(typeof(Red))!;
        var nav = et.GetNavigations().SingleOrDefault(n => n.Name == nameof(Red.Participaciones));

        nav.ShouldNotBeNull();
        nav!.IsCollection.ShouldBeTrue();
    }

    // ── ParticipacionEnRed: tabla y clave ────────────────────────────────────

    [Test]
    public void ParticipacionEnRed_TableName_IsParticipacionesEnRed()
    {
        var et = _model.FindEntityType(typeof(ParticipacionEnRed))!;

        et.GetTableName().ShouldBe("ParticipacionesEnRed");
    }

    [Test]
    public void ParticipacionEnRed_PrimaryKey_IsComposite_RedId_AuthorId()
    {
        var et = _model.FindEntityType(typeof(ParticipacionEnRed))!;
        var pk = et.FindPrimaryKey()!;
        var names = pk.Properties.Select(p => p.Name).ToList();

        names.ShouldContain(nameof(ParticipacionEnRed.RedId));
        names.ShouldContain(nameof(ParticipacionEnRed.AuthorId));
        pk.Properties.Count.ShouldBe(2);
    }

    // ── ParticipacionEnRed: propiedades ──────────────────────────────────────

    [Test]
    public void ParticipacionEnRed_RedId_HasMaxLength450()
    {
        var et = _model.FindEntityType(typeof(ParticipacionEnRed))!;
        var prop = et.FindProperty(nameof(ParticipacionEnRed.RedId))!;

        prop.GetMaxLength().ShouldBe(450);
    }

    [Test]
    public void ParticipacionEnRed_AuthorId_HasMaxLength450()
    {
        var et = _model.FindEntityType(typeof(ParticipacionEnRed))!;
        var prop = et.FindProperty(nameof(ParticipacionEnRed.AuthorId))!;

        prop.GetMaxLength().ShouldBe(450);
    }

    // ── ParticipacionEnRed: relaciones ───────────────────────────────────────

    [Test]
    public void ParticipacionEnRed_Red_ForeignKey_IsRedId()
    {
        var et = _model.FindEntityType(typeof(ParticipacionEnRed))!;
        var fk = et.GetForeignKeys()
            .SingleOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Red));

        fk.ShouldNotBeNull();
        fk!.Properties.Single().Name.ShouldBe(nameof(ParticipacionEnRed.RedId));
    }

    [Test]
    public void ParticipacionEnRed_Red_DeleteBehavior_IsCascade()
    {
        var et = _model.FindEntityType(typeof(ParticipacionEnRed))!;
        var fk = et.GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(Red));

        fk.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }

    [Test]
    public void ParticipacionEnRed_Author_ForeignKey_IsAuthorId()
    {
        var et = _model.FindEntityType(typeof(ParticipacionEnRed))!;
        var fk = et.GetForeignKeys()
            .SingleOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Author));

        fk.ShouldNotBeNull();
        fk!.Properties.Single().Name.ShouldBe(nameof(ParticipacionEnRed.AuthorId));
    }

    [Test]
    public void ParticipacionEnRed_Author_DeleteBehavior_IsCascade()
    {
        var et = _model.FindEntityType(typeof(ParticipacionEnRed))!;
        var fk = et.GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(Author));

        fk.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }
}
