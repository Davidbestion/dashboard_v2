import React, { useState, useMemo, useEffect } from 'react';
import { Table, Button, Spinner, Pagination, PaginationItem, PaginationLink } from 'reactstrap';

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
}) {
  const [sortKey, setSortKey] = useState(null);
  const [sortDir, setSortDir] = useState('asc');
  const [page, setPage] = useState(1);

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

  const hasActions = actions.length > 0;
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
                  {actions.map(action => {
                    if (action.show && !action.show(item)) return null;

                    if (action.render) {
                      return <React.Fragment key={action.key}>{action.render(item)}</React.Fragment>;
                    }

                    const isDisabled = action.disabled ? action.disabled(item) : false;

                    return (
                      <Button
                        key={action.key}
                        size="sm"
                        color={action.color ?? 'outline-secondary'}
                        className="ms-1"
                        aria-label={action.label}
                        disabled={isDisabled}
                        onClick={() => action.onClick(item)}
                      >
                        {action.icon
                          ? <i className={`bi ${action.icon}`} />
                          : action.label}
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
