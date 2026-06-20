# Dashboard de Producción Científica — MATCOM / UH

Sistema web para la gestión y visualización de la producción científica de la Facultad de Matemática y Computación de la Universidad de La Habana.

Permite registrar, consultar y exportar publicaciones, proyectos, eventos, premios, patentes, redes de investigación y otros resultados académicos, con autenticación integrada contra el directorio LDAP institucional.

## Estructura del repositorio

```text
dashboard_v2/
├── Dashboard_v2/   # Aplicación web principal (.NET 10 + React)
└── TemplateGen/    # CLI para generar plantillas Excel de importación
```

## Dashboard_v2

Aplicación web construida con .NET 10 (Clean Architecture) y React.

**Stack:** ASP.NET Core · Entity Framework Core · PostgreSQL · MinIO · OpenLDAP · JWT

Consulta [`Dashboard_v2/README.md`](Dashboard_v2/README.md) para instrucciones de despliegue, configuración de variables de entorno y usuarios de prueba.

## TemplateGen

Herramienta de línea de comandos que genera los archivos Excel correspondientes a los anexos del informe de producción científica institucional.

### Uso

```bash
cd TemplateGen
dotnet run                      # menú interactivo
dotnet run -- all               # genera todas las plantillas
dotnet run -- publicaciones     # genera una plantilla específica
```

### Plantillas disponibles

| Clave | Anexo |
| --- | --- |
| `publicaciones` | Anexo 2 — Publicaciones |
| `eventos` | Anexo 3 — Eventos y actividades científicas |
| `proyectos` | Anexo 4 — Proyectos de Investigación |
| `premios` | Anexo 5 — Premios |
| `redes-nac-inter` | Anexo 6 — Redes Nacionales e Internacionales |
| `redes-universitaria` | Anexo 6 — Plantilla Red Universitaria (una por red) |
| `registros` | Anexo 7 — Patentes, Registros, Normas y Productos |
| `grupos-estudiantiles` | Anexo 9 — Grupos Científicos Estudiantiles |
| `grupos` | Anexo 10 — Grupos de Investigación |

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) 24+ con Docker Compose v2 (para despliegue completo)
- [Node.js](https://nodejs.org/) 20+ (solo para desarrollo frontend)

## Licencia

MIT — © 2025 Facultad de Matemática y Computación, Universidad de La Habana.
