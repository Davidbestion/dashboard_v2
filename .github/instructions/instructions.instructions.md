---
description: Clean Architecture Dashboard v2 - Arquitectura y Convenciones
applyTo: '**/*.cs' # Se aplica a todos los archivos C# del proyecto
---

# Dashboard v2 - Clean Architecture Project

**Basado en**: [Clean Architecture Template de Jason Taylor](https://github.com/jasontaylordev/CleanArchitecture)

⚠️ **NOTA IMPORTANTE**: Los features `TodoItems`, `TodoLists` y `WeatherForecasts` son **ejemplos de la template** y fueron eliminados. NO uses estos como referencia para nuevas funcionalidades.

## Stack Tecnológico

### Backend
- **.NET 10**: Framework principal (C#)
- **Entity Framework Core**: ORM para PostgreSQL
- **PostgreSQL**: Base de datos relacional
- **MediatR**: Implementación de CQRS
- **FluentValidation**: Validación de comandos/queries
- **NSwag**: Generación de clientes API TypeScript para React

### Frontend
- **React**: Framework de UI (ubicado en `src/Web/ClientApp/`)
- **TypeScript**: Lenguaje para el cliente
- **NSwag Client**: Cliente API generado automáticamente desde el backend

### Testing
- **xUnit**: Framework de testing
- **Testcontainers**: Testing con contenedores PostgreSQL reales
- **FluentAssertions**: Assertions para tests

## Arquitectura del Proyecto

Este proyecto sigue **Clean Architecture** con las siguientes capas:

### 1. **Domain** (`src/Domain/`)
- **Propósito**: Núcleo de la lógica de negocio, completamente independiente
- **Contenido**: Entidades, Value Objects, Enums, Events, Exceptions
- **Reglas**:
  - NO debe tener dependencias de otras capas
  - NO debe referenciar ningún paquete de infraestructura
  - Solo contiene lógica de dominio pura
  - Las entidades deben heredar de clases base del dominio

### 2. **Application** (`src/Application/`)
- **Propósito**: Casos de uso y lógica de aplicación
- **Contenido**: Commands, Queries, DTOs, Interfaces, Validators
- **Reglas**:
  - Solo depende de Domain
  - Usa MediatR para CQRS (Commands y Queries)
  - Los handlers implementan `IRequestHandler<TRequest, TResponse>`
  - Validadores con FluentValidation
  - Define interfaces de repositorios/servicios (implementados en Infrastructure)

### 3. **Infrastructure** (`src/Infrastructure/`)
- **Propósito**: Implementaciones técnicas (BD, servicios externos, etc.)
- **Contenido**: DbContext, Repositories, Identity, External Services, Migrations
- **Reglas**:
  - Implementa las interfaces definidas en Application
  - Gestión de base de datos con Entity Framework Core
  - Configuraciones de entidades en `Data/Configurations/`
  - Migraciones de BD en `Data/Migrations/`
  - Inyección de dependencias en `DependencyInjection.cs`

### 4. **Web** (`src/Web/`)
- **Propósito**: Capa de presentación (API y UI)
- **Contenido**: Controllers, Endpoints, Pages, ClientApp (frontend)
- **Reglas**:
  - Endpoints usan Minimal APIs o Controllers
  - NO debe contener lógica de negocio
  - Usa MediatR para invocar Commands/Queries
  - Validación en la capa de Application, no aquí

## Convenciones de Código

### Estructura de Features
Organizar por feature vertical (no por tipo):
```
Application/
  [NombreFeature]/           # Reemplazar con el nombre real de tu feature
    Commands/
      Create[Entity]/
        Create[Entity]Command.cs
        Create[Entity]CommandHandler.cs
        Create[Entity]CommandValidator.cs
    Queries/
      Get[Entity]/
        Get[Entity]Query.cs
        Get[Entity]QueryHandler.cs
```

### Patrones a Seguir

1. **CQRS con MediatR**
   - Commands: modifican estado, no retornan datos
   - Queries: solo lectura, retornan datos

2. **Repository Pattern**
   - Interfaces en Application
   - Implementaciones en Infrastructure

3. **Dependency Injection**
   - Cada capa tiene su `DependencyInjection.cs`
   - Registros de servicios específicos de la capa

4. **Validación**
   - FluentValidation para reglas de validación
   - Validators en la misma carpeta que el Command/Query

5. **Testing**
   - Unit Tests: Application y Domain
   - Integration Tests: Infrastructure
   - Functional Tests: Application con BD real (Testcontainers)
   - Acceptance Tests: Web (end-to-end)

## Gestión de Base de Datos con Migraciones

### Flujo de Trabajo con Migraciones

Las migraciones de EF Core son la forma estándar de gestionar cambios al esquema de la base de datos. **Siempre usa migraciones, nunca modifiques la BD manualmente.**

#### Crear una migración (después de agregar/modificar entidades):
```bash
dotnet ef migrations add NombreDeLaMigracion --project src/Infrastructure --startup-project src/Web
```

#### Aplicar migraciones a la base de datos local:
```bash
dotnet ef database update --project src/Infrastructure --startup-project src/Web
```

#### Ver historial de migraciones:
```bash
dotnet ef migrations list --project src/Infrastructure --startup-project src/Web
```

#### Revertir a una migración anterior:
```bash
dotnet ef database update MigracionAnterior --project src/Infrastructure --startup-project src/Web
```

#### Generar script SQL (para producción):
```bash
dotnet ef migrations script --project src/Infrastructure --startup-project src/Web --output migration.sql
```

#### Eliminar la última migración (si no se aplicó):
```bash
dotnet ef migrations remove --project src/Infrastructure --startup-project src/Web
```

### Dónde se guardan las migraciones

- **Ubicación**: `src/Infrastructure/Data/Migrations/`
- **Versionadas en Git**: ✅ Sí, siempre commiteadas al repositorio
- **Formato**: Código C# generado automáticamente

### Flujo de trabajo típico:

1. **Creas/modificas una entidad** en `Domain/Entities/`
2. **Opcionalmente configuras** en `Infrastructure/Data/Configurations/`
3. **Generas una migración**: `dotnet ef migrations add NombreMigracion`
4. **Revisas el código generado** (es C# legible y editable)
5. **Aplicas la migración** localmente: `dotnet ef database update`
6. **Pruebas** que todo funciona correctamente
7. **Commiteas la migración** al repositorio con tu código
8. **En otros ambientes** (otros devs, CI/CD, producción) las migraciones se aplican automáticamente o mediante scripts

### Estrategias por ambiente:

- **Desarrollo Local**: `MigrateAsync()` en el inicializador (automático al iniciar)
- **Staging/QA**: Scripts SQL o pipelines de CI/CD
- **Producción**: Scripts SQL revisados + aplicación manual/controlada

## Flujo de Trabajo Típico

**Agregar una nueva funcionalidad:**

1. **Domain**: Crear/modificar entidades si es necesario
2. **Infrastructure**: 
   - Crear configuración EF Core si es necesario (`Data/Configurations/`)
   - Generar migración: `dotnet ef migrations add NombreFeature`
   - Aplicar migración: `dotnet ef database update`
3. **Application**: 
   - Crear Command o Query
   - Crear Handler
   - Crear Validator
   - Crear DTO de respuesta
4. **Web**: Crear endpoint que llame al Command/Query vía MediatR
5. **Tests**: Crear tests en cada capa según corresponda

## Reglas Importantes

⚠️ **NO hacer:**
- No poner lógica de negocio en Web o Infrastructure
- No referenciar Infrastructure desde Application
- No crear dependencias circulares entre capas
- No bypass de la capa de Application (Web no debe acceder directamente a repositorios)
- **NO modificar la base de datos manualmente fuera de migraciones**
- **NO usar EnsureCreated() en producción** (solo para tests rápidos)

✅ **SIEMPRE hacer:**
- Respetar las dependencias entre capas (Domain ← Application ← Infrastructure/Web)
- Usar MediatR para separar concerns
- Validar en Application con FluentValidation
- Escribir tests para cada capa
- Seguir la estructura de carpetas por feature
- **Usar migraciones para todos los cambios de esquema de BD**
- **Versionar las migraciones en Git**
- **Revisar el código de migración generado antes de aplicarlo**
- **Cumplir los principios SOLID en toda implementación** (ver sección abajo)

## Principios SOLID — Obligatorios en toda implementación

Todo código nuevo o modificado debe cumplir los 5 principios. Ante cualquier duda, analiza el código frente a cada punto antes de hacer un commit.

### S — Single Responsibility Principle
Cada clase/servicio tiene **una sola razón para cambiar**.
- Los handlers de comando solo orquestan: delegan validación compleja, lógica de dominio y operaciones reutilizables a servicios específicos.
- No duplicar lógica entre handlers. Si la misma operación aparece en dos lugares, extráela a un servicio con interfaz en `Application/Common/Interfaces/`.
- Ejemplo aplicado: `IAuthorResolutionService` centraliza "find-or-create author"; `IPublicationSpecializationService` centraliza la lógica de especialización, evitando duplicación en `CreatePublicationCommandHandler` y `UpdatePublicationCommandHandler`.

### O — Open/Closed Principle
El código está **abierto para extensión, cerrado para modificación**.
- Evitar condicionales `if (tipo == X) { ... } else if (tipo == Y) { ... }` dispersos por handlers. Concentrarlos en un servicio específico de modo que extender un nuevo tipo solo requiera modificar ese servicio.
- Usar interfaces + inyección de dependencias para permitir comportamiento intercambiable sin tocar el código que lo consume.
- Ejemplo aplicado: `IPublicationSpecializationService` encapsula toda la lógica condicional por `PublicationType`; agregar un nuevo tipo de especialización solo requiere modificar su implementación.

### L — Liskov Substitution Principle
Las implementaciones son **sustituibles por su interfaz** sin alterar el comportamiento del sistema.
- No añadir comportamiento adicional en implementaciones que rompa el contrato declarado en la interfaz.
- `ApplicationDbContext` debe satisfacer completamente `IApplicationDbContext`.

### I — Interface Segregation Principle
Las interfaces son **específicas al cliente que las usa**, no monolíticas.
- Preferir varias interfaces pequeñas y cohesivas sobre una grande.
- `IApplicationDbContext` es una excepción aceptada (patrón EF Core + CQRS), pero los servicios de dominio deben tener interfaces granulares.
- No inyectar `IApplicationDbContext` completo en código que solo necesita uno o dos `DbSet`.

### D — Dependency Inversion Principle
Los módulos de alto nivel dependen de **abstracciones**, no de implementaciones concretas.
- Toda dependencia en Application debe ser una interfaz definida en `Application/Common/Interfaces/`.
- Infrastructure implementa las interfaces; nunca al revés.
- Web solo depende de `ISender` (MediatR) y los contratos de Application.

## Comandos Útiles

### Backend
```bash
dotnet restore                                    # Restaurar dependencias
dotnet build                                      # Compilar solución
dotnet test                                       # Ejecutar todos los tests
dotnet run --project src/Web                      # Ejecutar backend + frontend
```

### Migraciones de Base de Datos (PostgreSQL)
```bash
# Crear migración
dotnet ef migrations add <NombreMigracion> --project src/Infrastructure --startup-project src/Web

# Aplicar migraciones
dotnet ef database update --project src/Infrastructure --startup-project src/Web

# Ver migraciones
dotnet ef migrations list --project src/Infrastructure --startup-project src/Web

# Generar script SQL
dotnet ef migrations script --project src/Infrastructure --startup-project src/Web --output migration.sql
```

### Frontend (dentro de src/Web/ClientApp/)
```bash
npm install                                       # Instalar dependencias
npm start                                         # Ejecutar solo React en dev
npm run build                                     # Build de producción
```

## Integración Backend-Frontend

- El backend genera automáticamente clientes TypeScript con **NSwag** (config en `src/Web/config.nswag`)
- React consume la API mediante estos clientes generados
- En desarrollo: React Proxy redirige requests al backend .NET
- En producción: .NET sirve los assets estáticos de React

---

**Al implementar cualquier funcionalidad, siempre verifica que respete esta arquitectura y estructura.**

## Architecture Decision Records (ADRs) — Obligatorio

Después de implementar cada sección, feature o decisión técnica relevante del proyecto, **debes crear un ADR** en `Dashboard_v2/docs/decisions/`.

### Cuándo crear un ADR

Crea un ADR cuando tomes una decisión que afecte:
- La arquitectura de una capa o módulo nuevo (e.g., cómo modelar una jerarquía de entidades).
- El esquema de base de datos de una entidad relevante (estrategia de herencia, relaciones N:N, etc.).
- Una tecnología, patrón o enfoque elegido entre varias alternativas (e.g., TPH vs. TPT, CQRS por tipo vs. monolítico).
- Una convención de seguridad o autenticación.

### Formato obligatorio (seguir el de los ADRs existentes)

```markdown
# 000N — Título conciso

## Status
Accepted | Proposed | Deprecated

## Fecha
YYYY-MM-DD

## Contexto
Describe el problema, las opciones consideradas y por qué era necesario tomar una decisión.

## Decisión
Explica qué se decidió y por qué. Sé específico sobre implementación si es relevante.

## Consecuencias

**Positivas:**
- ...

**Negativas / deuda técnica:**
- ...
```

### Reglas

- El número de ADR es secuencial (`0001`, `0002`, ...). Consulta el `README.md` del directorio para el siguiente número.
- **Actualiza el índice** `Dashboard_v2/docs/decisions/README.md` cada vez que añadas un ADR.
- Los ADRs existentes NO se modifican si son `Accepted`; crea uno nuevo que los deprecate si cambias la decisión.

**Recursos**: 
- [Template Original](https://github.com/jasontaylordev/CleanArchitecture)
- [Documentación Clean Architecture](https://jasontaylor.dev)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)