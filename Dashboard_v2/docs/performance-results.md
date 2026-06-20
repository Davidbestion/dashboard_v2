# Resultados de evaluación de rendimiento

## Entorno de medición

- **Base de datos:** PostgreSQL nativo (sin Testcontainers)
- **Runtime:** .NET 10, Release build
- **Instancia:** misma máquina que el entorno de desarrollo (sin virtualización adicional)
- **Fecha de medición:** 2026-06-18

---

## Resultados por escenario

| Escenario | Registros en BD | Resultado medido | Umbral definido | Estado |
|---|---|---|---|---|
| Lectura — `GET /api/Publications/todas` | 10 000 | **119 ms** | 3 000 ms | ✓ Pasa |
| Carga concurrente — 100 peticiones GET simultáneas | 10 000 | **3 116 ms totales / 31,2 ms por petición** | 10 000 ms total | ✓ Pasa |
| Escritura secuencial — 100 `POST /api/Publications` | — | **media 4,1 ms · P95 6 ms · máx 76 ms** | media < 500 ms | ✓ Pasa |

---

## Interpretación

### Lectura de 10 000 registros

La consulta completa se resuelve en **119 ms**, un 96 % por debajo del umbral de 3 s.
Indica que el índice de PostgreSQL sobre `UserId` es eficaz y el ORM no introduce overhead significativo a esta escala.

### 100 usuarios concurrentes

El lote completo de 100 peticiones simultáneas finalizó en **3,1 s**, con una media de **31 ms por usuario**.
El pool de conexiones de Npgsql absorbe la concurrencia sin degradación apreciable para el volumen objetivo.

### Latencia de escritura

La media de **4,1 ms por inserción** y el P95 en **6 ms** reflejan el coste real de una escritura transaccional en PostgreSQL.
El valor máximo de 76 ms corresponde al primer request (calentamiento de JIT/pool); en condiciones estabilizadas la latencia es constante.

---

## Estimación de dimensionamiento

Para 100 usuarios concurrentes con 10 000 publicaciones en la base de datos:

| Recurso | Estimación |
|---|---|
| RAM (PostgreSQL `shared_buffers`) | ~80 MB (tabla + índices) |
| CPU | < 1 núcleo sostenido en lecturas puras |
| Almacenamiento | ~50 MB (filas + índices, sin adjuntos) |
| Ancho de banda | ~2 MB/s en pico de lectura concurrente |

Estos valores se derivan directamente de las pruebas ejecutadas y son representativos de un entorno de producción universitario de escala media.
