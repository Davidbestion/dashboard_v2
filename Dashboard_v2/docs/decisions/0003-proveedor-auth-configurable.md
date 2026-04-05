# 0003 — Proveedor de autenticación configurable (Local/LDAP)

## Status
Accepted

## Fecha
2026-04-04

## Contexto

El sistema de autenticación tiene dos implementaciones con ciclos de vida y propósitos distintos:

- **Local**: provisional, para desarrollo. Gestiona usuarios y contraseñas directamente en PostgreSQL con BCrypt.
- **LDAP**: destino final para producción. Delega la validación de credenciales al directorio de la facultad.

Se necesitaba un mecanismo para alternar entre ambas sin modificar código y sin que las capas superiores (Application, Web) supieran cuál estaba activa.

## Decisión

Se implementó el **patrón Strategy** a través de la interfaz `IIdentityService`:

- Ambas implementaciones (`LocalAuthService`, `LdapAuthService`) implementan `IIdentityService`.
- En `Infrastructure/DependencyInjection.cs`, el contenedor registra **una sola** implementación según el valor de `Auth:Provider` en la configuración:

```csharp
var authProvider = builder.Configuration["Auth:Provider"] ?? "Ldap";
if (authProvider.Equals("Ldap", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddTransient<IIdentityService, LdapAuthService>();
else
    builder.Services.AddTransient<IIdentityService, LocalAuthService>();
```

- `appsettings.json` define `"Provider": "Ldap"` como default de producción.
- `appsettings.Development.json` sobreescribe a `"Provider": "Local"` para desarrollo local sin depender de un servidor LDAP.
- En producción el valor por defecto (`Ldap`) aplica directamente; la implementación local queda inactiva.

## Consecuencias

**Positivas:**
- Los handlers de MediatR y los endpoints no saben qué implementación está activa; solo dependen de `IIdentityService`.
- Cambiar de proveedor es un cambio de configuración, no de código.
- Permite desarrollar y probar ambos flujos en la misma base de código.

**Negativas:**
- Ambas implementaciones deben mantener la misma firma de interfaz. Si en el futuro LDAP necesita operaciones sin equivalente local (o viceversa), habrá que evaluar ampliar la interfaz o segregarla.
