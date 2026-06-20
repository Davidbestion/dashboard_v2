# Dashboard_v2

Sistema de gestión de producción científica universitaria, desarrollado con .NET 10 y Clean Architecture.

## Despliegue rápido

El repositorio incluye un `docker-compose.yml` que levanta todos los servicios necesarios:
PostgreSQL, MinIO (almacenamiento de documentos), OpenLDAP (autenticación) y la propia API.

**Requisitos previos:** Docker 24+ y Docker Compose v2.

### 1. Configurar variables de entorno

```bash
cp .env.example .env
```

Edita `.env` y cambia al menos las contraseñas y el secreto JWT:

| Variable | Descripción |
| --- | --- |
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | Credenciales de PostgreSQL |
| `LDAP_ADMIN_PASSWORD` | Contraseña del administrador LDAP |
| `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` | Credenciales de MinIO |
| `API_PORT` | Puerto local donde se expone la API (por defecto `8080`) |
| `JWT_SECRET` | Secreto para firmar tokens JWT (mínimo 64 caracteres; generá uno con `openssl rand -base64 64`) |

### 2. Levantar los servicios

```bash
docker compose up -d
```

Esto inicia en segundo plano:

| Servicio | URL / Puerto | Descripción |
| --- | --- | --- |
| API | `http://localhost:<API_PORT>` | Backend .NET 10 |
| Swagger UI | `http://localhost:<API_PORT>/api` | Documentación interactiva de la API |
| PostgreSQL | `localhost:5432` | Base de datos relacional |
| MinIO API | `http://localhost:9000` | Almacenamiento S3-compatible |
| MinIO Consola | `http://localhost:9001` | Panel web de MinIO |
| OpenLDAP | `localhost:389` | Servidor de directorio |
| phpLDAPadmin | `https://localhost:6443` | Panel web de LDAP |

### 3. Usuarios de prueba (LDAP)

El archivo `ldap/bootstrap.ldif` siembra tres usuarios listos para usar:

| Correo | Contraseña | Rol sugerido |
| --- | --- | --- |
| `superuser@localhost` | `Superuser1!` | Superuser |
| `jperez@matcom.uh.cu` | `Profesor1!` | Profesor |
| `lrodriguez@matcom.uh.cu` | `Vicedecano1!` | Vicedecano_de_investigacion |
| `cmartinez@matcom.uh.cu` | `JefeProyecto1!` | Jefe_de_Proyecto |
| `mgarcia@matcom.uh.cu` | `JefeGrupo1!` | Jefe_de_Grupo_de_investigacion |
| `afortes@matcom.uh.cu` | `JefeMacro1!` | Jefe_de_Macroproyecto |
| `ylopez@matcom.uh.cu` | `JefeRedes1!` | Jefe_de_Redes |

> Los roles dentro de la aplicación se asignan desde el panel de administración
> una vez que el usuario inicia sesión por primera vez.

### 4. Archivos WoS (opcional)

Para habilitar la resolución de bases de datos desde Web of Science, coloca los
archivos Excel exportados de WoS en `src/Web/data/wos/` antes de levantar los
servicios. El volumen queda montado automáticamente en el contenedor de la API.

### 5. Detener los servicios

```bash
docker compose down          # detiene y elimina los contenedores
docker compose down -v       # ídem + elimina los volúmenes de datos
```

---

## Build

Run `dotnet build -tl` to build the solution.

## Run

To run the web application:

```bash
cd .\src\Web\
dotnet watch run
```

Navigate to <https://localhost:5001>. The application will automatically reload if you change any of the source files.

## Code Styles & Formatting

The template includes [EditorConfig](https://editorconfig.org/) support to help maintain consistent coding styles for multiple developers working on the same project across various editors and IDEs. The **.editorconfig** file defines the coding styles applicable to this solution.

## Code Scaffolding

The template includes support to scaffold new commands and queries.

Start in the `.\src\Application\` folder.

Create a new command:

```bash
dotnet new ca-usecase --name CreateTodoList --feature-name TodoLists --usecase-type command --return-type int
```

Create a new query:

```bash
dotnet new ca-usecase -n GetTodos -fn TodoLists -ut query -rt TodosVm
```

If you encounter the error *"No templates or subcommands found matching: 'ca-usecase'."*, install the template and try again:

```bash
dotnet new install Clean.Architecture.Solution.Template::10.0.0-preview
```

## Test

The solution contains unit, integration, functional, and acceptance tests.

To run the unit, integration, and functional tests (excluding acceptance tests):

```bash
dotnet test --filter "FullyQualifiedName!~AcceptanceTests"
```

To run the acceptance tests, first start the application:

```bash
cd .\src\Web\
dotnet run
```

Then, in a new console, run the tests:

```bash
cd .\src\Web\
dotnet test
```

## Help

To learn more about the template go to the [project website](https://github.com/jasontaylordev/CleanArchitecture). Here you can find additional guidance, request new features, report a bug, and discuss the template with other users.
