# 0005 — Auto-provisioning de usuarios LDAP en PostgreSQL

## Status
Accepted

## Fecha
2026-04-04

## Contexto

En modo LDAP, el directorio es la fuente de verdad para la autenticación, pero el sistema necesita datos adicionales del usuario en PostgreSQL: roles, perfil académico, relaciones con publicaciones y eventos, etc. 

Al momento del primer login de un usuario LDAP, ese usuario no existe en la base de datos local. Hay dos opciones:

1. **Pre-provisioning manual**: un administrador crea el usuario en PostgreSQL antes de que pueda hacer login.
2. **Auto-provisioning**: el sistema crea el usuario automáticamente al primer login exitoso con LDAP.

La opción 1 crea fricción operativa innecesaria. El directorio LDAP ya es la fuente de verdad; duplicar esa gestión manualmente es trabajo redundante.

## Decisión

Se implementó auto-provisioning en `LdapAuthService.ProvisionUserAsync`:

- Tras un bind exitoso, se busca al usuario en PostgreSQL por email.
- Si no existe, se crea con los datos disponibles en LDAP (`uid` → `UserName`, `sn` → `UserLastName1`) y valores por defecto para los campos del perfil académico que LDAP no conoce.
- La contraseña **no se almacena** (`PasswordHash = null`); las credenciales viven solo en el directorio.
- El usuario se crea con `IsActive = true` pero **sin roles asignados**.

Los roles deben asignarse posteriormente por un Superuser. Hasta entonces, el usuario puede autenticarse en LDAP pero no puede hacer login en la aplicación (el sistema rechaza usuarios sin roles).

## Consecuencias

**Positivas:**
- Un usuario dado de alta en el directorio LDAP puede acceder a la aplicación sin intervención adicional del administrador del sistema, una vez que un Superuser le asigne un rol.
- Los datos académicos se completan progresivamente; no bloquean el alta inicial.

**Negativas:**
- El perfil del usuario recién creado estará incompleto (sin categorías, con fecha de nacimiento placeholder `1970-01-01`). Debe completarse manualmente.
- Si el atributo `sn` no está definido en LDAP, se usará el fragmento del email antes del `@` como apellido, lo que puede generar datos sucios.
