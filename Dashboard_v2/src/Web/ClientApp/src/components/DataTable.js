import React, { useState, useMemo } from 'react';
import { Table, Button, Spinner } from 'reactstrap';

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
 *
 * ColumnDef
 * ─────────
 * key              string     Propiedad del item. Soporta notación 'a.b.c'.
 * label            string     Texto de cabecera.
 * sortable?        boolean    Si true, la cabecera es clicable para ordenar.
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
}) {
  const [sortKey, setSortKey] = useState(null);
  const [sortDir, setSortDir] = useState('asc');

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
    return [...data].sort((a, b) => {
      const va = getValue(a, sortKey) ?? '';
      const vb = getValue(b, sortKey) ?? '';
      const cmp =
        typeof va === 'number' && typeof vb === 'number'
          ? va - vb
          : String(va).localeCompare(String(vb), 'es', { sensitivity: 'base' });
      return sortDir === 'asc' ? cmp : -cmp;
    });
  }, [data, sortKey, sortDir]);

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
        {sortedData.map(item => {
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
  );
}
