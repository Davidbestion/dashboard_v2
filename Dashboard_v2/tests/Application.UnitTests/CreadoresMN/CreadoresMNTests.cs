using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.CreadoresMN;

/// <summary>
/// Tests para la relación N:M entre <see cref="User"/> y las cuatro entidades de output:
/// <see cref="Registro"/>, <see cref="Norma"/>, <see cref="ProductoComercializado"/> y
/// <see cref="Patente"/>.
///
/// Cubre:
///   - Inserción y lectura de la tabla de unión.
///   - Navegación bidireccional (User→Entidad y Entidad→User).
///   - Clave compuesta previene creadores duplicados.
///   - Un usuario puede ser creador de varias entidades del mismo tipo.
///   - Una entidad puede tener varios creadores.
///   - Cascade delete: al eliminar el User se eliminan los registros de la join table.
///   - Cascade delete: al eliminar la entidad se eliminan los registros de la join table.
/// </summary>
[TestFixture]
public class CreadoresMNTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static ApplicationDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static User MakeUser(string id) => new()
    {
        Id = id,
        UserName = $"user_{id}",
        UserLastName1 = "Test",
        Email = $"{id}@test.cu",
        BirthDate = DateTime.UtcNow,
        IsActive = true
    };

    private static Country MakeCountry(int id) => new()
    {
        Id = id,
        Name = $"Country {id}"
    };

    private static Institution MakeInstitution(string id) => new()
    {
        Id = id,
        Nombre = $"Institution {id}"
    };

    private static Registro MakeRegistro(string id, int countryId, string institutionId) => new()
    {
        Id = id,
        Titulo = $"Registro {id}",
        NumeroCertificado = $"CERT-{id}",
        CountryId = countryId,
        InstitutionId = institutionId
    };

    private static Norma MakeNorma(string id, string institutionId) => new()
    {
        Id = id,
        Titulo = $"Norma {id}",
        Tipo = "Técnica",
        InstitutionId = institutionId
    };

    private static TipoProductoComercializado MakeTipo(string id) => new()
    {
        Id = id,
        Nombre = $"Tipo {id}"
    };

    private static ProductoComercializado MakeProducto(string id, string tipoId, string institutionId) => new()
    {
        Id = id,
        Titulo = $"Producto {id}",
        TipoProductoComercializadoId = tipoId,
        InstitutionId = institutionId
    };

    private static Patente MakePatente(string id) => new()
    {
        Id = id,
        Titulo = $"Patente {id}",
        NumeroSolicitudConcesion = $"SOL-{id}",
        EsNacional = true
    };

    // ═══════════════════════════════════════════════════════════════════════
    //  REGISTRO
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task UserRegistro_CanInsertAndRead_JoinRow()
    {
        await using var db = CreateDb();
        var user = MakeUser("u1");
        var country = MakeCountry(1);
        var institution = MakeInstitution("i1");
        var registro = MakeRegistro("r1", 1, "i1");

        db.Users.Add(user);
        db.Countries.Add(country);
        db.Institutions.Add(institution);
        db.Registros.Add(registro);
        await db.SaveChangesAsync();

        db.UserRegistros.Add(new UserRegistro { UserId = "u1", RegistroId = "r1" });
        await db.SaveChangesAsync();

        var row = await db.UserRegistros.SingleAsync(ur => ur.UserId == "u1" && ur.RegistroId == "r1");
        row.UserId.ShouldBe("u1");
        row.RegistroId.ShouldBe("r1");
    }

    [Test]
    public async Task UserRegistro_NavigationFromUser_ContainsRegistro()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Countries.Add(MakeCountry(1));
        db.Institutions.Add(MakeInstitution("i1"));
        db.Registros.Add(MakeRegistro("r1", 1, "i1"));
        await db.SaveChangesAsync();

        db.UserRegistros.Add(new UserRegistro { UserId = "u1", RegistroId = "r1" });
        await db.SaveChangesAsync();

        var user = await db.Users
            .Include(u => u.RegistrosCreados)
            .ThenInclude(ur => ur.Registro)
            .SingleAsync(u => u.Id == "u1");

        user.RegistrosCreados.ShouldHaveSingleItem();
        user.RegistrosCreados.First().Registro.Titulo.ShouldBe("Registro r1");
    }

    [Test]
    public async Task UserRegistro_NavigationFromRegistro_ContainsUser()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Countries.Add(MakeCountry(1));
        db.Institutions.Add(MakeInstitution("i1"));
        db.Registros.Add(MakeRegistro("r1", 1, "i1"));
        await db.SaveChangesAsync();

        db.UserRegistros.Add(new UserRegistro { UserId = "u1", RegistroId = "r1" });
        await db.SaveChangesAsync();

        var registro = await db.Registros
            .Include(r => r.Creadores)
            .ThenInclude(ur => ur.User)
            .SingleAsync(r => r.Id == "r1");

        registro.Creadores.ShouldHaveSingleItem();
        registro.Creadores.First().User.Id.ShouldBe("u1");
    }

    [Test]
    public async Task UserRegistro_OneRegistroCanHaveMultipleCreadores()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Users.Add(MakeUser("u2"));
        db.Countries.Add(MakeCountry(1));
        db.Institutions.Add(MakeInstitution("i1"));
        db.Registros.Add(MakeRegistro("r1", 1, "i1"));
        await db.SaveChangesAsync();

        db.UserRegistros.AddRange(
            new UserRegistro { UserId = "u1", RegistroId = "r1" },
            new UserRegistro { UserId = "u2", RegistroId = "r1" });
        await db.SaveChangesAsync();

        var creadores = await db.UserRegistros.Where(ur => ur.RegistroId == "r1").ToListAsync();
        creadores.Count.ShouldBe(2);
    }

    [Test]
    public async Task UserRegistro_OneUserCanCreateMultipleRegistros()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Countries.Add(MakeCountry(1));
        db.Institutions.Add(MakeInstitution("i1"));
        db.Registros.Add(MakeRegistro("r1", 1, "i1"));
        db.Registros.Add(MakeRegistro("r2", 1, "i1"));
        await db.SaveChangesAsync();

        db.UserRegistros.AddRange(
            new UserRegistro { UserId = "u1", RegistroId = "r1" },
            new UserRegistro { UserId = "u1", RegistroId = "r2" });
        await db.SaveChangesAsync();

        var created = await db.UserRegistros.Where(ur => ur.UserId == "u1").ToListAsync();
        created.Count.ShouldBe(2);
    }

    [Test]
    public async Task UserRegistro_DuplicateKey_ThrowsOnSave()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Countries.Add(MakeCountry(1));
        db.Institutions.Add(MakeInstitution("i1"));
        db.Registros.Add(MakeRegistro("r1", 1, "i1"));
        await db.SaveChangesAsync();

        db.UserRegistros.Add(new UserRegistro { UserId = "u1", RegistroId = "r1" });
        Should.Throw<InvalidOperationException>(() =>
            db.UserRegistros.Add(new UserRegistro { UserId = "u1", RegistroId = "r1" }));
    }

    [Test]
    public async Task UserRegistro_DeleteUser_CascadesJoinRow()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Countries.Add(MakeCountry(1));
        db.Institutions.Add(MakeInstitution("i1"));
        db.Registros.Add(MakeRegistro("r1", 1, "i1"));
        await db.SaveChangesAsync();

        db.UserRegistros.Add(new UserRegistro { UserId = "u1", RegistroId = "r1" });
        await db.SaveChangesAsync();

        var user = await db.Users.FindAsync("u1");
        db.Users.Remove(user!);
        await db.SaveChangesAsync();

        var rows = await db.UserRegistros.ToListAsync();
        rows.ShouldBeEmpty();
    }

    [Test]
    public async Task UserRegistro_DeleteRegistro_CascadesJoinRow()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Countries.Add(MakeCountry(1));
        db.Institutions.Add(MakeInstitution("i1"));
        db.Registros.Add(MakeRegistro("r1", 1, "i1"));
        await db.SaveChangesAsync();

        db.UserRegistros.Add(new UserRegistro { UserId = "u1", RegistroId = "r1" });
        await db.SaveChangesAsync();

        var registro = await db.Registros.FindAsync("r1");
        db.Registros.Remove(registro!);
        await db.SaveChangesAsync();

        var rows = await db.UserRegistros.ToListAsync();
        rows.ShouldBeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  NORMA
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task UserNorma_CanInsertAndRead_JoinRow()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Institutions.Add(MakeInstitution("i1"));
        db.Normas.Add(MakeNorma("n1", "i1"));
        await db.SaveChangesAsync();

        db.UserNormas.Add(new UserNorma { UserId = "u1", NormaId = "n1" });
        await db.SaveChangesAsync();

        var row = await db.UserNormas.SingleAsync(un => un.UserId == "u1" && un.NormaId == "n1");
        row.NormaId.ShouldBe("n1");
    }

    [Test]
    public async Task UserNorma_NavigationFromUser_ContainsNorma()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Institutions.Add(MakeInstitution("i1"));
        db.Normas.Add(MakeNorma("n1", "i1"));
        await db.SaveChangesAsync();

        db.UserNormas.Add(new UserNorma { UserId = "u1", NormaId = "n1" });
        await db.SaveChangesAsync();

        var user = await db.Users
            .Include(u => u.NormasCreadas)
            .ThenInclude(un => un.Norma)
            .SingleAsync(u => u.Id == "u1");

        user.NormasCreadas.ShouldHaveSingleItem();
        user.NormasCreadas.First().Norma.Titulo.ShouldBe("Norma n1");
    }

    [Test]
    public async Task UserNorma_NavigationFromNorma_ContainsUser()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Institutions.Add(MakeInstitution("i1"));
        db.Normas.Add(MakeNorma("n1", "i1"));
        await db.SaveChangesAsync();

        db.UserNormas.Add(new UserNorma { UserId = "u1", NormaId = "n1" });
        await db.SaveChangesAsync();

        var norma = await db.Normas
            .Include(n => n.Creadores)
            .ThenInclude(un => un.User)
            .SingleAsync(n => n.Id == "n1");

        norma.Creadores.ShouldHaveSingleItem();
        norma.Creadores.First().User.Id.ShouldBe("u1");
    }

    [Test]
    public async Task UserNorma_OneNormaCanHaveMultipleCreadores()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Users.Add(MakeUser("u2"));
        db.Institutions.Add(MakeInstitution("i1"));
        db.Normas.Add(MakeNorma("n1", "i1"));
        await db.SaveChangesAsync();

        db.UserNormas.AddRange(
            new UserNorma { UserId = "u1", NormaId = "n1" },
            new UserNorma { UserId = "u2", NormaId = "n1" });
        await db.SaveChangesAsync();

        var creadores = await db.UserNormas.Where(un => un.NormaId == "n1").ToListAsync();
        creadores.Count.ShouldBe(2);
    }

    [Test]
    public async Task UserNorma_DuplicateKey_ThrowsOnSave()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Institutions.Add(MakeInstitution("i1"));
        db.Normas.Add(MakeNorma("n1", "i1"));
        await db.SaveChangesAsync();

        db.UserNormas.Add(new UserNorma { UserId = "u1", NormaId = "n1" });
        Should.Throw<InvalidOperationException>(() =>
            db.UserNormas.Add(new UserNorma { UserId = "u1", NormaId = "n1" }));
    }

    [Test]
    public async Task UserNorma_DeleteUser_CascadesJoinRow()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Institutions.Add(MakeInstitution("i1"));
        db.Normas.Add(MakeNorma("n1", "i1"));
        await db.SaveChangesAsync();

        db.UserNormas.Add(new UserNorma { UserId = "u1", NormaId = "n1" });
        await db.SaveChangesAsync();

        db.Users.Remove((await db.Users.FindAsync("u1"))!);
        await db.SaveChangesAsync();

        (await db.UserNormas.ToListAsync()).ShouldBeEmpty();
    }

    [Test]
    public async Task UserNorma_DeleteNorma_CascadesJoinRow()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Institutions.Add(MakeInstitution("i1"));
        db.Normas.Add(MakeNorma("n1", "i1"));
        await db.SaveChangesAsync();

        db.UserNormas.Add(new UserNorma { UserId = "u1", NormaId = "n1" });
        await db.SaveChangesAsync();

        db.Normas.Remove((await db.Normas.FindAsync("n1"))!);
        await db.SaveChangesAsync();

        (await db.UserNormas.ToListAsync()).ShouldBeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PRODUCTO COMERCIALIZADO
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task UserProducto_CanInsertAndRead_JoinRow()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Institutions.Add(MakeInstitution("i1"));
        db.TipoProductosComercializados.Add(MakeTipo("tp1"));
        db.ProductosComercializados.Add(MakeProducto("p1", "tp1", "i1"));
        await db.SaveChangesAsync();

        db.UserProductosComercializados.Add(new UserProductoComercializado
        {
            UserId = "u1",
            ProductoComercializadoId = "p1"
        });
        await db.SaveChangesAsync();

        var row = await db.UserProductosComercializados
            .SingleAsync(up => up.UserId == "u1" && up.ProductoComercializadoId == "p1");
        row.ProductoComercializadoId.ShouldBe("p1");
    }

    [Test]
    public async Task UserProducto_NavigationFromUser_ContainsProducto()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Institutions.Add(MakeInstitution("i1"));
        db.TipoProductosComercializados.Add(MakeTipo("tp1"));
        db.ProductosComercializados.Add(MakeProducto("p1", "tp1", "i1"));
        await db.SaveChangesAsync();

        db.UserProductosComercializados.Add(new UserProductoComercializado
        {
            UserId = "u1",
            ProductoComercializadoId = "p1"
        });
        await db.SaveChangesAsync();

        var user = await db.Users
            .Include(u => u.ProductosComercializadosCreados)
            .ThenInclude(up => up.ProductoComercializado)
            .SingleAsync(u => u.Id == "u1");

        user.ProductosComercializadosCreados.ShouldHaveSingleItem();
        user.ProductosComercializadosCreados.First().ProductoComercializado.Titulo.ShouldBe("Producto p1");
    }

    [Test]
    public async Task UserProducto_NavigationFromProducto_ContainsUser()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Institutions.Add(MakeInstitution("i1"));
        db.TipoProductosComercializados.Add(MakeTipo("tp1"));
        db.ProductosComercializados.Add(MakeProducto("p1", "tp1", "i1"));
        await db.SaveChangesAsync();

        db.UserProductosComercializados.Add(new UserProductoComercializado
        {
            UserId = "u1",
            ProductoComercializadoId = "p1"
        });
        await db.SaveChangesAsync();

        var producto = await db.ProductosComercializados
            .Include(p => p.Creadores)
            .ThenInclude(up => up.User)
            .SingleAsync(p => p.Id == "p1");

        producto.Creadores.ShouldHaveSingleItem();
        producto.Creadores.First().User.Id.ShouldBe("u1");
    }

    [Test]
    public async Task UserProducto_OneProductoCanHaveMultipleCreadores()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Users.Add(MakeUser("u2"));
        db.Institutions.Add(MakeInstitution("i1"));
        db.TipoProductosComercializados.Add(MakeTipo("tp1"));
        db.ProductosComercializados.Add(MakeProducto("p1", "tp1", "i1"));
        await db.SaveChangesAsync();

        db.UserProductosComercializados.AddRange(
            new UserProductoComercializado { UserId = "u1", ProductoComercializadoId = "p1" },
            new UserProductoComercializado { UserId = "u2", ProductoComercializadoId = "p1" });
        await db.SaveChangesAsync();

        var creadores = await db.UserProductosComercializados
            .Where(up => up.ProductoComercializadoId == "p1").ToListAsync();
        creadores.Count.ShouldBe(2);
    }

    [Test]
    public async Task UserProducto_DuplicateKey_ThrowsOnSave()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Institutions.Add(MakeInstitution("i1"));
        db.TipoProductosComercializados.Add(MakeTipo("tp1"));
        db.ProductosComercializados.Add(MakeProducto("p1", "tp1", "i1"));
        await db.SaveChangesAsync();

        db.UserProductosComercializados.Add(new UserProductoComercializado
            { UserId = "u1", ProductoComercializadoId = "p1" });
        Should.Throw<InvalidOperationException>(() =>
            db.UserProductosComercializados.Add(new UserProductoComercializado
                { UserId = "u1", ProductoComercializadoId = "p1" }));
    }

    [Test]
    public async Task UserProducto_DeleteUser_CascadesJoinRow()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Institutions.Add(MakeInstitution("i1"));
        db.TipoProductosComercializados.Add(MakeTipo("tp1"));
        db.ProductosComercializados.Add(MakeProducto("p1", "tp1", "i1"));
        await db.SaveChangesAsync();

        db.UserProductosComercializados.Add(new UserProductoComercializado
            { UserId = "u1", ProductoComercializadoId = "p1" });
        await db.SaveChangesAsync();

        db.Users.Remove((await db.Users.FindAsync("u1"))!);
        await db.SaveChangesAsync();

        (await db.UserProductosComercializados.ToListAsync()).ShouldBeEmpty();
    }

    [Test]
    public async Task UserProducto_DeleteProducto_CascadesJoinRow()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Institutions.Add(MakeInstitution("i1"));
        db.TipoProductosComercializados.Add(MakeTipo("tp1"));
        db.ProductosComercializados.Add(MakeProducto("p1", "tp1", "i1"));
        await db.SaveChangesAsync();

        db.UserProductosComercializados.Add(new UserProductoComercializado
            { UserId = "u1", ProductoComercializadoId = "p1" });
        await db.SaveChangesAsync();

        db.ProductosComercializados.Remove((await db.ProductosComercializados.FindAsync("p1"))!);
        await db.SaveChangesAsync();

        (await db.UserProductosComercializados.ToListAsync()).ShouldBeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PATENTE
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task UserPatente_CanInsertAndRead_JoinRow()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Patentes.Add(MakePatente("pat1"));
        await db.SaveChangesAsync();

        db.UserPatentes.Add(new UserPatente { UserId = "u1", PatenteId = "pat1" });
        await db.SaveChangesAsync();

        var row = await db.UserPatentes.SingleAsync(up => up.UserId == "u1" && up.PatenteId == "pat1");
        row.PatenteId.ShouldBe("pat1");
    }

    [Test]
    public async Task UserPatente_NavigationFromUser_ContainsPatente()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Patentes.Add(MakePatente("pat1"));
        await db.SaveChangesAsync();

        db.UserPatentes.Add(new UserPatente { UserId = "u1", PatenteId = "pat1" });
        await db.SaveChangesAsync();

        var user = await db.Users
            .Include(u => u.PatentesCreadas)
            .ThenInclude(up => up.Patente)
            .SingleAsync(u => u.Id == "u1");

        user.PatentesCreadas.ShouldHaveSingleItem();
        user.PatentesCreadas.First().Patente.Titulo.ShouldBe("Patente pat1");
    }

    [Test]
    public async Task UserPatente_NavigationFromPatente_ContainsUser()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Patentes.Add(MakePatente("pat1"));
        await db.SaveChangesAsync();

        db.UserPatentes.Add(new UserPatente { UserId = "u1", PatenteId = "pat1" });
        await db.SaveChangesAsync();

        var patente = await db.Patentes
            .Include(p => p.Creadores)
            .ThenInclude(up => up.User)
            .SingleAsync(p => p.Id == "pat1");

        patente.Creadores.ShouldHaveSingleItem();
        patente.Creadores.First().User.Id.ShouldBe("u1");
    }

    [Test]
    public async Task UserPatente_OnePatenteCanHaveMultipleCreadores()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Users.Add(MakeUser("u2"));
        db.Patentes.Add(MakePatente("pat1"));
        await db.SaveChangesAsync();

        db.UserPatentes.AddRange(
            new UserPatente { UserId = "u1", PatenteId = "pat1" },
            new UserPatente { UserId = "u2", PatenteId = "pat1" });
        await db.SaveChangesAsync();

        var creadores = await db.UserPatentes.Where(up => up.PatenteId == "pat1").ToListAsync();
        creadores.Count.ShouldBe(2);
    }

    [Test]
    public async Task UserPatente_OneUserCanCreateMultiplePatentes()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Patentes.Add(MakePatente("pat1"));
        db.Patentes.Add(MakePatente("pat2"));
        await db.SaveChangesAsync();

        db.UserPatentes.AddRange(
            new UserPatente { UserId = "u1", PatenteId = "pat1" },
            new UserPatente { UserId = "u1", PatenteId = "pat2" });
        await db.SaveChangesAsync();

        var patentes = await db.UserPatentes.Where(up => up.UserId == "u1").ToListAsync();
        patentes.Count.ShouldBe(2);
    }

    [Test]
    public async Task UserPatente_DuplicateKey_ThrowsOnSave()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Patentes.Add(MakePatente("pat1"));
        await db.SaveChangesAsync();

        db.UserPatentes.Add(new UserPatente { UserId = "u1", PatenteId = "pat1" });
        Should.Throw<InvalidOperationException>(() =>
            db.UserPatentes.Add(new UserPatente { UserId = "u1", PatenteId = "pat1" }));
    }

    [Test]
    public async Task UserPatente_DeleteUser_CascadesJoinRow()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Patentes.Add(MakePatente("pat1"));
        await db.SaveChangesAsync();

        db.UserPatentes.Add(new UserPatente { UserId = "u1", PatenteId = "pat1" });
        await db.SaveChangesAsync();

        db.Users.Remove((await db.Users.FindAsync("u1"))!);
        await db.SaveChangesAsync();

        (await db.UserPatentes.ToListAsync()).ShouldBeEmpty();
    }

    [Test]
    public async Task UserPatente_DeletePatente_CascadesJoinRow()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Patentes.Add(MakePatente("pat1"));
        await db.SaveChangesAsync();

        db.UserPatentes.Add(new UserPatente { UserId = "u1", PatenteId = "pat1" });
        await db.SaveChangesAsync();

        db.Patentes.Remove((await db.Patentes.FindAsync("pat1"))!);
        await db.SaveChangesAsync();

        (await db.UserPatentes.ToListAsync()).ShouldBeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CROSS-ENTITY — un usuario puede ser creador de los 4 tipos a la vez
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task User_CanBeCreatorOfAllFourEntityTypes_Simultaneously()
    {
        await using var db = CreateDb();
        db.Users.Add(MakeUser("u1"));
        db.Countries.Add(MakeCountry(1));
        db.Institutions.Add(MakeInstitution("i1"));
        db.TipoProductosComercializados.Add(MakeTipo("tp1"));
        db.Registros.Add(MakeRegistro("r1", 1, "i1"));
        db.Normas.Add(MakeNorma("n1", "i1"));
        db.ProductosComercializados.Add(MakeProducto("p1", "tp1", "i1"));
        db.Patentes.Add(MakePatente("pat1"));
        await db.SaveChangesAsync();

        db.UserRegistros.Add(new UserRegistro { UserId = "u1", RegistroId = "r1" });
        db.UserNormas.Add(new UserNorma { UserId = "u1", NormaId = "n1" });
        db.UserProductosComercializados.Add(new UserProductoComercializado
            { UserId = "u1", ProductoComercializadoId = "p1" });
        db.UserPatentes.Add(new UserPatente { UserId = "u1", PatenteId = "pat1" });
        await db.SaveChangesAsync();

        var user = await db.Users
            .Include(u => u.RegistrosCreados)
            .Include(u => u.NormasCreadas)
            .Include(u => u.ProductosComercializadosCreados)
            .Include(u => u.PatentesCreadas)
            .SingleAsync(u => u.Id == "u1");

        user.RegistrosCreados.ShouldHaveSingleItem();
        user.NormasCreadas.ShouldHaveSingleItem();
        user.ProductosComercializadosCreados.ShouldHaveSingleItem();
        user.PatentesCreadas.ShouldHaveSingleItem();
    }
}
