# 0004 — Patrón "search then bind" en LDAP

## Status
Accepted

## Fecha
2026-04-04

## Contexto

Para validar credenciales en LDAP existen dos estrategias principales:

1. **Simple bind con uid**: construir el DN directamente a partir del identificador recibido (ej. `uid=jperez,ou=people,dc=...`) y hacer bind con él y la contraseña. Requiere que el usuario introduzca su `uid` LDAP, no su email.

2. **Search then bind**: un usuario admin se conecta primero para buscar el DN real filtrando por un atributo conocido (ej. `mail`), y luego se hace un segundo bind con ese DN y la contraseña del usuario. Permite que el formulario use email, independientemente del `uid` interno en el directorio.

El formulario de login del sistema usa **email + contraseña** (consistente con el modo local). La opción 1 habría roto esa consistencia o habría requerido que el usuario conociera su `uid` LDAP.

## Decisión

Se implementó el patrón **"search then bind"** en `LdapAuthService.TrySearchThenBind`:

1. El admin se conecta al servidor LDAP (`AdminDn` + `AdminPassword`).
2. Busca el DN del usuario filtrando por `(mail={email})` dentro de `UsersDn`.
3. Hace un segundo bind con ese DN y la contraseña del usuario.
4. Si el bind tiene éxito, extrae los atributos del usuario (`uid`, `sn`, `cn`, `mail`).

El valor del email se escapa antes de insertarlo en el filtro LDAP (`EscapeLdapFilter`) para prevenir **LDAP injection** (RFC 4515: `\`, `*`, `(`, `)`, `\0`).

## Consecuencias

**Positivas:**
- El formulario de login es idéntico para ambos proveedores: siempre email + contraseña.
- La lógica de construcción de DN queda encapsulada; cambios en la estructura del directorio solo afectan la configuración (`UsersDn`).
- Protección contra LDAP injection sin coste adicional.

**Negativas:**
- Requiere una cuenta de admin en el directorio con permisos de búsqueda. Las credenciales deben protegerse en la configuración (variables de entorno o secrets en producción, nunca en el repositorio).
- Dos roundtrips al servidor LDAP por login (bind admin + bind usuario) en lugar de uno.
