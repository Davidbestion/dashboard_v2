# 0001 — Identity custom sin ASP.NET Identity

## Status
Accepted

## Fecha
2026-04-04

## Contexto

Al diseñar el modelo de base de datos del proyecto se definió una entidad `User` con campos específicos del dominio académico: `UserLastName1`, `UserLastName2`, `BirthDate`, `TeachingCategory`, `ScientificCategory`, `InvestigationCategory`, etc. Este modelo no encaja con el esquema de tablas que genera ASP.NET Identity (`AspNetUsers`, `AspNetRoles`, `AspNetUserClaims`, …), que está pensado para un modelo de usuario genérico y habría requerido extenderlo o convivir con tablas que no se necesitan.

Adicionalmente, el sistema de autenticación para producción está planificado que sea **LDAP exclusivamente** (el directorio de la facultad). ASP.NET Identity habría introducido una capa de complejidad innecesaria para un caso de uso que no es el objetivo final.

## Decisión

Se implementó un sistema de identity **completamente custom**:

- Entidad `User` propia, definida en la capa `Domain`, sin herencia de `IdentityUser`.
- Tabla `UserRoles` propia con una clave foránea a `User` y una columna enum `Role`.
- Interfaz `IIdentityService` definida en `Application`, con implementaciones intercambiables en `Infrastructure`.
- Contraseñas hasheadas con **BCrypt** en la implementación local.
- Sin middlewares ni pipelines de ASP.NET Identity; la autenticación se resuelve vía JWT validado en cada request.

**Este sistema local es temporal y provisional.** Está pensado únicamente para facilitar el desarrollo sin depender de un servidor LDAP activo. No está diseñado para producción.

## Consecuencias

**Positivas:**
- El modelo de `User` refleja exactamente el dominio, sin campos sobrantes ni tablas auxiliares.
- La interfaz `IIdentityService` permite cambiar la implementación (Local ↔ LDAP) sin tocar nada fuera de `Infrastructure`.
- El código es más simple al no cargar con la abstracción de Identity.

**Negativas / deuda técnica:**
- No se dispone de funcionalidades out-of-the-box de Identity (reset de contraseña, confirmación de email, lockout, etc.). Si se necesitaran en el futuro, habría que implementarlas manualmente.
- La implementación local (`LocalAuthService`) **no debe usarse en producción**. Para producción se usará únicamente LDAP.
