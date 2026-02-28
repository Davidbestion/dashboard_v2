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

**Recursos**: 
- [Template Original](https://github.com/jasontaylordev/CleanArchitecture)
- [Documentación Clean Architecture](https://jasontaylor.dev)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)