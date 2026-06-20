# 0008 — Migración CQRS→Service Layer: consolidación CRUD y reubicación de servicios

## Status
Accepted

## Fecha
2026-04-22

## Resumen

Este documento describe la decisión y el trabajo realizado para sustituir el patrón
CQRS (handlers `Command`/`Query` con `MediatR`) por una `Service Layer` explícita que
expone operaciones CRUD a través de interfaces `I*Service` situadas en la capa `Application`.
También documenta las acciones operativas realizadas (centralización de DTOs, refactor
de endpoints, generación de documentos con plantillas `.xlsx`, y la eliminación segura
de handlers legacy con backups).

## Contexto

La solución originalmente usaba el patron CQRS con `MediatR` para todas las operaciones de negocio, incluyendo CRUD. Esto resultó en:
- Un gran número de handlers (`CreateXxxCommandHandler`, `UpdateXxxCommandHandler`, `GetXxxQueryHandler`, etc.), comandos (`CreateXxxCommand`, `UpdateXxxCommand`, `GetXxxQuery`) y DTOs (`XxxDto`) por cada entidad, lo que generaba ruido y duplicación solo para operaciones CRUD sencillas.
- Endpoints que dependían directamente de `MediatR` y enviaban comandos/queries, lo que acoplaba la capa `Web` a la infraestructura de CQRS.

La decisión fue migrar a una arquitectura más tradicional con una `Service Layer` que exponga métodos CRUD y casos de uso agrupados, eliminando la necesidad de handlers CQRS para operaciones básicas. Esto simplifica el código, reduce el número de archivos y hace que los endpoints sean más directos al llamar a servicios explícitos.

## Decisión

Se unificó la arquitectura alrededor de una `Service Layer` en la capa `Application`.
En concreto:

- Se introdujeron o consolidaron servicios de aplicación (`IUserService`, `IRoleService`,
  `IPublicationService`, `IAuthorService`, `IProyectoService`, `IDocumentService`, etc.)
  que exponen operaciones CRUD y casos de uso agrupados.
- Los endpoints en `src/Web/Endpoints` se refactorizaron para depender de estas interfaces
  y no enviar `IRequest` a `MediatR` para operaciones CRUD.
- `MediatR` se mantiene únicamente para la publicación de eventos de dominio y para
  helpers de testing — no se usa ya como flujo principal para CRUD.

## Implementación (qué se hizo)

1. Centralización de contratos y DTOs
   - Se movieron y consolidaron DTOs compartidos en `src/Application` (por ejemplo `UserWithRolesDto`,
     `RoleDto`, etc.) para evitar conflictos de tipo y duplicación.

2. Creación y/o refactor de servicios de aplicación
   - Implementación de `I*Service` en `src/Application` para dominios clave (Users, Roles,
     Publications, Authors, Events, Projects, Documents, etc.).
   - Servicios exponen métodos sin depender de MediatR y encapsulan validación (via
     `IRequestValidationService`), mapeo y llamadas a `IApplicationDbContext`.

3. Refactor de endpoints
   - Todos los endpoints en `src/Web/Endpoints` se actualizaron para inyectar y usar
     los servicios `I*Service` (p. ej. `GetMyPublications(IPublicationService service)`).
   - Esto simplifica el flujo HTTP → Application → Infrastructure.

4. Infraestructura de generación de documentos
   - Añadido `IDocumentService` en `Application` y `IDocumentRenderer`/`DocumentRenderer`
     en `Infrastructure` (implementación basada en ClosedXML.Report) para soportar
     plantillas `.xlsx` y múltiples hojas.

5. Eliminación segura de handlers MediatR obsoletos
   - Para cada handler/query/command reemplazado se siguió un bucle conservador:

```bash
# Respaldo (ruta espejo en removed_handlers_backup)
# Eliminar archivo original
dotnet build -clp:Summary
# Si falla: aplicar corrección mínima o restaurar desde removed_handlers_backup
```

   - Se le hizo backup a cada handler eliminado en `removed_handlers_backup/` para permitir una reversión fácil si se detecta una regresión. Dicha carpeta fue eliminada al finalizar la migración y validación.

6. Registro de dependencias
   - `src/Infrastructure/DependencyInjection.cs` se actualizó para registrar las
     implementaciones de `I*Service`, `IDocumentRenderer` y el proveedor activo de identidad
     (`IIdentityService` → `LdapAuthService` o `LocalAuthService` según `Auth:Provider`).

7. MediatR y eventos de dominio
   - `MediatR` permanece registrado en `Application` únicamente para publicar eventos
     de dominio (ej.: `DispatchDomainEventsInterceptor` utiliza `_mediator.Publish(...)`).

8. Pruebas y build
   - Tras la migración incremental se ejecutó `dotnet build` repetidamente, corrigiendo
     conflictos (usings duplicados, DTOs duplicados) hasta lograr una compilación local
     exitosa. Persisten advertencias de paquetes (`NU1608`) por versiones pre-release
     de Npgsql/EF Core; son no bloqueantes.

## Razonamiento / ventajas

- Menos ruido en el árbol de código: menos archivos `Command`/`Query` por operación ->
  menor coste cognitivo para nuevos contribuyentes.
- Contratos de la API más directos: los endpoints llaman servicios explícitos que
  representan casos de uso claramente nombrados.
- Validación consistente: `IRequestValidationService` permite ejecutar FluentValidation
  desde los servicios sin depender de comportamientos implícitos de pipeline.
- Facilita la refactorización incremental: al centralizar DTOs y servicios, es más sencillo
  continuar migrando otros módulos (p. ej. completar `Proyectos`).

## Trade-offs y riesgos

- Se pierde cierta formalidad y convención que provee la separación `Command`/`Query`.
- Posible duplicación temporal de lógica entre implementaciones de `IIdentityService`
  (p. ej. `LdapAuthService` vs `LocalAuthService`).
- Revisión de pruebas: las pruebas unitarias e integración deben actualizarse para
  consumir la nueva `Service Layer`.

