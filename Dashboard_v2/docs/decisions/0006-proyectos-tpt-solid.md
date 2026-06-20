# 0006 — Diseño de Proyectos: TPT + Commands y Queries por tipo (SOLID)

## Status
Superseded by ADR 0008

## Nota

Este ADR describe una etapa intermedia del módulo `Proyectos`. La decisión vigente
para la capa de aplicación está documentada en [0008](0008-proyectos-auth-servicios-crud.md),
que mantiene TPT en persistencia pero reemplaza `Commands/Queries` por una capa de
servicios CRUD coherente con el resto del sistema.

## Fecha
2026-04-10

## Contexto

El sistema gestiona 7 tipos de proyectos con jerarquía de especialización:
- `Proyecto` (base abstracta)
  - `ProyectoEnRevision`
  - `ProyectoEnEjecucion` (abstracta)
    - `ProyectoEmpresarial` (PE)
    - `ProyectoApoyoPrograma` (PAP)
    - `ProyectoDesarrolloLocal` (PDL)
    - `ProyectoNoEmpresarial` (PNE)
    - `ProyectoColabInternacional` (PRCI)
    - `ProyectoPNAP` (PNAP)

### Decisión 1 — Table Per Type (TPT) en vez de Table Per Hierarchy (TPH)

La primera implementación usó **TPH** (discriminator column en la tabla base). Con TPH todas las columnas de todos los subtipos conviven en una sola tabla, lo que genera:
- Muchas columnas `NULL` por fila (cada fila solo rellena las columnas de su tipo).
- Sin posibilidad de `NOT NULL` en columnas propias de subtipos.
- Viola ACID a nivel de esquema relacional.

**TPT** crea una tabla por cada tipo de la jerarquía unida por FK:
- `Proyectos` — campos base comunes
- `ProyectosEnEjecucion` — campos intermedios de ejecución
- `ProyectosEmpresariales`, `ProyectosApoyoPrograma`, ... — campos específicos por tipo

Resultados: 9 tablas, cero NULLs estructurales, integridad referencial garantizada.

### Decisión 2 — Commands y Queries por tipo (OCP)

La implementación inicial tenía un único `CreateProyectoCommand` y `UpdateProyectoCommand` con un `switch (tipo)` que:
1. **Viola Open/Closed Principle**: añadir un nuevo tipo de proyecto requiere modificar el handler existente.
2. **Viola Single Responsibility**: el handler conoce la lógica de construcción de 7 tipos distintos.

Se refactorizó a **un Command y un Handler por tipo**:
- `CreateProyectoEnRevisionCommand`, `CreateProyectoEmpresarialCommand`, ... (7 Create)
- `UpdateProyectoEnRevisionCommand`, `UpdateProyectoEmpresarialCommand`, ... (7 Update)
- `DeleteProyectoCommand` — permanece compartido (lógica idéntica independiente del tipo)
- 7 Queries `GetProyectoXxxQuery` (GetById por tipo para edición)
- `GetProyectosQuery` — devuelve `ProyectoResumenDto` con 7 consultas `OfType<T>()` separadas

### Decisión 3 — DTOs tipados sin nullables

Se eliminó el DTO monolítico `ProyectoDto` con todos los campos nullable. Se adoptó una jerarquía de records:
- `ProyectoResumenDto` — solo campos de display para el listado general
- `ProyectoBaseDto` (abstract record) — campos comunes a todos
- `ProyectoEnEjecucionBaseDto : ProyectoBaseDto` (abstract) — campos de ejecución
- 7 DTOs concretos sin campos nullable estructurales

### Decisión 4 — Endpoints por tipo bajo `/api/Proyectos/{slug}`

| Método | Ruta | Acción |
|--------|------|--------|
| GET | `/api/Proyectos` | Listado resumen (todos los tipos) |
| DELETE | `/api/Proyectos/{id}` | Eliminar (compartido) |
| GET/POST/PUT | `/api/Proyectos/en-revision/{id?}` | CRUD EnRevision |
| GET/POST/PUT | `/api/Proyectos/empresariales/{id?}` | CRUD PE |
| GET/POST/PUT | `/api/Proyectos/apoyo-programa/{id?}` | CRUD PAP |
| GET/POST/PUT | `/api/Proyectos/desarrollo-local/{id?}` | CRUD PDL |
| GET/POST/PUT | `/api/Proyectos/no-empresariales/{id?}` | CRUD PNE |
| GET/POST/PUT | `/api/Proyectos/colaboracion-internacional/{id?}` | CRUD PRCI |
| GET/POST/PUT | `/api/Proyectos/pnap/{id?}` | CRUD PNAP |

## Decisión

Se combina **TPT** para el esquema relacional con **CQRS per-type** para la capa de aplicación. Añadir un futuro 8.° tipo de proyecto requiere:
1. Nueva entidad de dominio heredando de `Proyecto` o `ProyectoEnEjecucion`.
2. Nueva configuración `IEntityTypeConfiguration<T>` y migración.
3. Nuevos `CreateXxx`/`UpdateXxx`/`GetXxx` (copiar patrón existente).
4. Nuevos endpoints y entrada de frontend.

**No se modifica código existente.** Se cumple OCP.

## Consecuencias

**Positivas:**
- Esquema relacional normalizado: 9 tablas, sin NULLs estructurales, FK con integridad referencial.
- Cada handler tiene una sola responsabilidad; extensible sin modificar código existente.
- DTOs tipados eliminan nullability innecesaria y hacen explícito el contrato por tipo.
- Consultas `OfType<T>()` separadas en `GetProyectosQuery` evitan el LEFT JOIN masivo que TPT generaría con una consulta polimórfica sobre el `DbSet` base.

**Negativas / deuda técnica:**
- Más archivos: 7 Create + 7 Update + 7 GetById = 21 archivos de comandos/queries. La navegación del proyecto es más verbosa.
- TPT genera joins en cada consulta por tipo; para volúmenes muy grandes podría considerarse añadir índices compuestos en las FK de las tablas de subtipos o incluso migrar a JSONB para los campos específicos si el rendimiento lo requiere.
- El frontend debe conocer el slug por tipo para llamar al endpoint correcto; hay un mapa `TIPO_SLUG` que deberá actualizarse al añadir nuevos tipos.
