import React, { useState, useMemo, useEffect } from 'react';
import { Table, Button, Spinner, Pagination, PaginationItem, PaginationLink, Modal, ModalHeader, ModalBody } from 'reactstrap';

// ─── helpers ──────────────────────────────────────────────────────────────────

/**
 * Accede a una propiedad anidada usando notación de puntos.
 * getValue({ a: { b: 1 } }, 'a.b') === 1
 */
function getValue(item, key) {
  return key.split('.').reduce((obj, k) => obj?.[k], item);
}

function SortIcon({ active, direction }) {
  if (!active) return <i className="bi bi-arrow-down-up text-muted ms-1" style={{ fontSize: '0.7rem', opacity: 0.4 }} />;
  return direction === 'asc'
    ? <i className="bi bi-sort-up ms-1" style={{ fontSize: '0.8rem' }} />
    : <i className="bi bi-sort-down ms-1" style={{ fontSize: '0.8rem' }} />;
}

// ─── Detail modal helpers ─────────────────────────────────────────────────────

const LABEL_ES = {
  name:                  'Nombre',
  nombre:                'Nombre',
  title:                 'Título',
  titulo:                'Título',
  description:           'Descripción',
  descripcion:           'Descripción',
  fecha:                 'Fecha',
  date:                  'Fecha',
  type:                  'Tipo',
  tipo:                  'Tipo',
  country:               'País',
  countryName:           'País',
  area:                  'Área',
  areaNombre:            'Área',
  email:                 'Correo electrónico',
  user:                  'Usuario',
  usuario:               'Usuario',
  userDisplayName:       'Nombre completo',
  // eventos / presentaciones
  eventName:             'Evento',
  eventTypeName:         'Tipo de evento',
  eventType:             'Tipo de evento',
  presentationType:      'Tipo de presentación',
  // publicaciones
  publishedDate:         'Fecha de publicación',
  urlDoi:                'URL / DOI',
  authors:               'Autores',
  publicationData:       'Datos de publicación',
  // premios
  awardName:             'Premio',
  awardTypeName:         'Tipo de premio',
  awardedAt:             'Otorgado el',
  recipient:             'Receptor',
  // proyectos / grupos
  jefe:                  'Jefe',
  correoJefe:            'Correo del jefe',
  numeroMiembros:        'Miembros',
  participantes:         'Participantes',
  // redes
  cantidadProfesores:    'Profesores',
  // registros / patentes / normas
  esNacional:            'Nacional',
  numeroSolicitudConcesion: 'Número solicitud',
  tipoNormaNombre:       'Tipo de norma',
  tipoProductoComercializadoNombre: 'Tipo de producto',
  institutionNombre:     'Institución',
  // usuarios
  userName:              'Nombre de usuario',
  userLastName1:         'Primer apellido',
  userLastName2:         'Segundo apellido',
  isActive:              'Activo',
  isTrained:             'Adiestrado',
  scientificCategory:    'Categoría científica',
  teachingCategory:      'Categoría docente',
  investigationCategory: 'Categoría de investigación',
  universidadNombre:     'Universidad',
};

function camelToLabel(key) {
  if (LABEL_ES[key]) return LABEL_ES[key];
  return key
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, s => s.toUpperCase())
    .trim();
}

function isInternalKey(key) {
  const lower = key.toLowerCase();
  return lower === 'id' || lower.endsWith('id') || lower.endsWith('ids');
}

function objectToDisplay(obj) {
  if (obj == null) return '—';
  // User object
  if (obj.userName != null) {
    return [obj.userName, obj.userLastName1, obj.userLastName2].filter(Boolean).join(' ');
  }
  // Generic: prefer a label field
  const label = obj.nombre ?? obj.name ?? obj.titulo ?? obj.title ?? obj.displayName;
  if (label != null) return String(label);
  // Last resort: show key=value pairs of primitive properties
  const pairs = Object.entries(obj)
    .filter(([, v]) => v != null && typeof v !== 'object')
    .map(([k, v]) => `${camelToLabel(k)}: ${v}`)
    .slice(0, 4);
  return pairs.length ? pairs.join(' · ') : JSON.stringify(obj);
}

function formatValue(val) {
  if (val == null || val === '') return '—';
  if (typeof val === 'boolean') return val ? 'Sí' : 'No';
  if (Array.isArray(val)) {
    if (val.length === 0) return '—';
    if (typeof val[0] === 'object') return val.map(objectToDisplay).join(', ');
    return val.join(', ');
  }
  if (typeof val === 'object') return objectToDisplay(val);
  if (typeof val === 'string' && /^\d{4}-\d{2}-\d{2}T/.test(val)) {
    return new Date(val).toLocaleString('es-CU');
  }
  return String(val);
}

function EntityDetailModal({ item, config, isOpen, toggle }) {
  if (!item) return null;

  const title = config?.title
    ? config.title(item)
    : (item.nombre ?? item.titulo ?? item.name ?? item.awardName ?? `#${item.id ?? ''}`);

  const fields = config?.fields
    ? config.fields
    : Object.keys(item)
        .filter(k => !isInternalKey(k))
        .map(k => ({ key: k, label: camelToLabel(k) }));

  return (
    <Modal isOpen={isOpen} toggle={toggle} size="lg" scrollable>
      <ModalHeader toggle={toggle}>{title}</ModalHeader>
      <ModalBody>
        <dl className="row mb-0">
          {fields.map(({ key, label, render }) => {
            const val = getValue(item, key);
            return (
              <React.Fragment key={key}>
                <dt className="col-sm-4 text-muted fw-normal small">{label}</dt>
                <dd className="col-sm-8 mb-2">
                  {render ? render(val, item) : formatValue(val)}
                </dd>
              </React.Fragment>
            );
          })}
        </dl>
      </ModalBody>
    </Modal>
  );
}

// ─── DataTable ────────────────────────────────────────────────────────────────

/**
 * Tabla genérica reutilizable.
 *
 * Props
 * ─────
 * columns      ColumnDef[]   Definición de columnas (ver abajo).
 * data         object[]      Filas a mostrar.
 * keyExtractor fn            (item) => string | number — clave única de cada fila.
 * actions?     ActionDef[]   Botones por fila en la columna final de acciones.
 * emptyMessage? string       Texto cuando no hay filas (default: "No hay datos.").
 * loading?     boolean       Muestra spinner en lugar del contenido.
 * hover?       boolean       Filas resaltadas al pasar el cursor (default: true).
 * responsive?  boolean       Tabla con scroll horizontal en móvil (default: true).
 * className?   string        Clase CSS extra para el elemento <table>.
 * actionsLabel? string       Cabecera de la columna de acciones (default: "Acciones").
 * pageSize?    number        Filas por página (default: 10).
 * detailConfig? object|true  Si se proporciona, añade botón "Ver" por fila que abre un
 *                            modal con todos los campos del ítem. Puede ser `true` para
 *                            auto-generar todo, u objeto con:
 *                              title?: (item) => string
 *                              fields?: { key, label, render? }[]
 *
 * ColumnDef
 * ─────────
 * key              string     Propiedad del item. Soporta notación 'a.b.c'.
 * label            string     Texto de cabecera.
 * sortable?        boolean    Si true, la cabecera es clicable para ordenar.
 * sortValue?       fn         (value, item) => comparable  — valor usado para ordenar en lugar del raw value.
 * render?          fn         (value, item) => ReactNode  — celda personalizada.
 * className?       string     Clase aplicada al <td> y al <th>.
 * headerClassName? string     Clase extra aplicada solo al <th>.
 *
 * ActionDef
 * ─────────
 * key          string     Identificador único de la acción.
 * label        string     Texto del botón y aria-label.
 * icon?        string     Clase Bootstrap Icon, p.ej. 'bi-pencil'. Si se combina
 *                         con label, el label queda solo como aria-label.
 * color?       string     Color Reactstrap (default: 'outline-secondary').
 * onClick      fn         (item) => void
 * show?        fn         (item) => boolean — si se omite, el botón es siempre visible.
 * disabled?    fn         (item) => boolean — si se omite, nunca está deshabilitado.
 * render?      fn         (item) => ReactNode — control total; ignora todos los campos
 *                         anteriores salvo `key` y `show`.
 */
export default function DataTable({
  columns = [],
  data = [],
  keyExtractor,
  actions = [],
  emptyMessage = 'No hay datos.',
  loading = false,
  hover = true,
  responsive = true,
  className = '',
  actionsLabel = 'Acciones',
  pageSize = 10,
  detailConfig,
}) {
  const [sortKey, setSortKey] = useState(null);
  const [sortDir, setSortDir] = useState('asc');
  const [page, setPage] = useState(1);
  const [detailItem, setDetailItem] = useState(null);

  // Vuelve a la primera página cuando cambian los datos o el orden
  useEffect(() => { setPage(1); }, [data, sortKey]);

  function handleSort(key) {
    if (sortKey === key) {
      setSortDir(d => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortKey(key);
      setSortDir('asc');
    }
  }

  /**
   * Sorts displayed data by the active sort column.
   * Uses the column's sortValue() callback if defined (for custom sort logic like case-insensitive strings).
   * Falls back to direct value comparison.
   */
  const sortedData = useMemo(() => {
    if (!sortKey) return data;
    const col = columns.find(c => c.key === sortKey);
    return [...data].sort((a, b) => {
      const rawA = getValue(a, sortKey);
      const rawB = getValue(b, sortKey);
      const va = col?.sortValue ? (col.sortValue(rawA, a) ?? '') : (rawA ?? '');
      const vb = col?.sortValue ? (col.sortValue(rawB, b) ?? '') : (rawB ?? '');
      const cmp =
        typeof va === 'number' && typeof vb === 'number'
          ? va - vb
          : String(va).localeCompare(String(vb), 'es', { sensitivity: 'base' });
      return sortDir === 'asc' ? cmp : -cmp;
    });
  }, [data, sortKey, sortDir, columns]);

  const totalPages = Math.max(1, Math.ceil(sortedData.length / pageSize));

  const pageNumbers = useMemo(() => {
    const delta = 2;
    const start = Math.max(1, page - delta);
    const end = Math.min(totalPages, page + delta);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }, [page, totalPages]);

  const pagedData = useMemo(() => {
    const from = (page - 1) * pageSize;
    return sortedData.slice(from, from + pageSize);
  }, [sortedData, page, pageSize]);

  const hasDetail = Boolean(detailConfig);
  const detailCfg = detailConfig === true ? {} : detailConfig;
  const hasActions = actions.length > 0 || hasDetail;
  const colSpan = columns.length + (hasActions ? 1 : 0);

  if (loading) {
    return (
      <div className="d-flex justify-content-center py-4">
        <Spinner color="primary" />
      </div>
    );
  }

  return (
    <>
    <Table responsive={responsive} hover={hover} className={`mb-0 ${className}`}>
      <thead className="table-light">
        <tr>
          {columns.map(col => (
            <th
              key={col.key}
              className={[
                col.className ?? '',
                col.headerClassName ?? '',
                col.sortable ? 'user-select-none' : '',
              ].join(' ').trim()}
              style={col.sortable ? { cursor: 'pointer', whiteSpace: 'nowrap' } : undefined}
              onClick={col.sortable ? () => handleSort(col.key) : undefined}
            >
              {col.label}
              {col.sortable && (
                <SortIcon active={sortKey === col.key} direction={sortDir} />
              )}
            </th>
          ))}
          {hasActions && (
            <th className="text-end" style={{ whiteSpace: 'nowrap' }}>
              {actionsLabel}
            </th>
          )}
        </tr>
      </thead>
      <tbody>
        {sortedData.length === 0 && (
          <tr>
            <td colSpan={colSpan} className="text-center text-muted py-4">
              {emptyMessage}
            </td>
          </tr>
        )}
        {pagedData.map(item => {
          const rowKey = keyExtractor ? keyExtractor(item) : item.id;
          return (
            <tr key={rowKey}>
              {columns.map(col => {
                const value = getValue(item, col.key);
                return (
                  <td key={col.key} className={`align-middle ${col.className ?? ''}`}>
                    {col.render ? col.render(value, item) : (value ?? '—')}
                  </td>
                );
              })}
              {hasActions && (
                <td className="align-middle text-end" style={{ whiteSpace: 'nowrap' }}>
                  {hasDetail && (
                    <Button
                      size="sm"
                      color="outline-info"
                      className="ms-1"
                      aria-label="Ver detalles"
                      onClick={() => setDetailItem(item)}
                    >
                      <i className="bi bi-eye" />
                    </Button>
                  )}
                  {actions.map(action => {
                    if (action.show && !action.show(item)) return null;

                    if (action.render) {
                      return <React.Fragment key={action.key}>{action.render(item)}</React.Fragment>;
                    }

                    const isDisabled = action.disabled ? action.disabled(item) : false;
                    const actionColor = typeof action.color === 'function' ? action.color(item) : (action.color ?? 'outline-secondary');
                    const actionLabel = typeof action.label === 'function' ? action.label(item) : action.label;

                    return (
                      <Button
                        key={action.key}
                        size="sm"
                        color={actionColor}
                        className="ms-1"
                        aria-label={actionLabel}
                        disabled={isDisabled}
                        onClick={() => action.onClick(item)}
                      >
                        {action.icon
                          ? <i className={`bi ${action.icon}`} />
                          : actionLabel}
                      </Button>
                    );
                  })}
                </td>
              )}
            </tr>
          );
        })}
      </tbody>
    </Table>
    <EntityDetailModal
      item={detailItem}
      config={detailCfg}
      isOpen={Boolean(detailItem)}
      toggle={() => setDetailItem(null)}
    />
    <div className="d-flex align-items-center justify-content-between flex-wrap gap-2 px-3 py-2 border-top bg-light">
      <small className="text-muted">
        {sortedData.length === 0
          ? 'Sin resultados'
          : `Mostrando ${(page - 1) * pageSize + 1}–${Math.min(page * pageSize, sortedData.length)} de ${sortedData.length}`}
      </small>
      <Pagination size="sm" className="mb-0">
        <PaginationItem disabled={page === 1}>
          <PaginationLink first onClick={() => setPage(1)} />
        </PaginationItem>
        <PaginationItem disabled={page === 1}>
          <PaginationLink previous onClick={() => setPage(p => p - 1)} />
        </PaginationItem>
        {pageNumbers.map(n => (
          <PaginationItem key={n} active={n === page}>
            <PaginationLink onClick={() => setPage(n)}>{n}</PaginationLink>
          </PaginationItem>
        ))}
        <PaginationItem disabled={page === totalPages}>
          <PaginationLink next onClick={() => setPage(p => p + 1)} />
        </PaginationItem>
        <PaginationItem disabled={page === totalPages}>
          <PaginationLink last onClick={() => setPage(totalPages)} />
        </PaginationItem>
      </Pagination>
    </div>
    </>
  );
}
