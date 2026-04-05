# 0002 — JWT almacenado en cookie HttpOnly

## Status
Accepted

## Fecha
2026-04-04

## Contexto

Una vez generado el JWT tras el login, hay que decidir dónde almacenarlo en el cliente para enviarlo en cada request subsiguiente. Las dos opciones más comunes son:

1. **`localStorage` / `sessionStorage`**: el frontend lo guarda en JS y lo envía manualmente en el header `Authorization: Bearer <token>`.
2. **Cookie HttpOnly**: el servidor la establece con `Set-Cookie`; el browser la envía automáticamente y el JS no puede leerla.

La opción 1 es vulnerable a **XSS**: cualquier script inyectado puede leer el token de `localStorage` y exfiltrarlo. La opción 2 elimina ese vector porque `HttpOnly` impide el acceso desde JavaScript.

## Decisión

El JWT se almacena en una **cookie HttpOnly** llamada `access_token`, establecida por el endpoint `POST /api/Auth/login` con las siguientes opciones:

```csharp
new CookieOptions
{
    HttpOnly = true,
    Secure   = httpContext.Request.IsHttps,   // Solo HTTPS en producción
    SameSite = SameSiteMode.Strict,           // Protección CSRF
    Expires  = DateTimeOffset.UtcNow.AddHours(8)
}
```

- `HttpOnly = true`: JavaScript no puede leer ni robar el token.
- `Secure = true` en HTTPS: la cookie no viaja en texto plano.
- `SameSite = Strict`: el browser no envía la cookie en requests originados desde otros sitios, mitigando CSRF.

El logout elimina la cookie en el servidor con `Response.Cookies.Delete("access_token")`.

## Consecuencias

**Positivas:**
- Elimina el riesgo de robo de token por XSS.
- `SameSite=Strict` mitiga CSRF sin necesidad de tokens CSRF adicionales para el caso de uso actual (SPA en el mismo origen).
- El frontend no necesita lógica para adjuntar el token manualmente en cada request.

**Negativas:**
- Clientes no-browser (CLI, mobile nativo, otras APIs) no manejan cookies automáticamente; necesitarían lógica adicional para trabajar con este esquema.
- En desarrollo sin HTTPS, `Secure` se desactiva automáticamente (`IsHttps = false`), lo que es aceptable para desarrollo local.
