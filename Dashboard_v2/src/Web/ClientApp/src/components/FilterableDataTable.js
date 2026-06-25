import React, { useState, useMemo } from 'react';
import { Input } from 'reactstrap';
import DataTable from './DataTable';

// ─── helper ───────────────────────────────────────────────────────────────────

function getVal(item, key) {
  return key.split('.').reduce((o, k) => o?.[k], item);
}

// ─── FilterableDataTable ──────────────────────────────────────────────────────

/**
 * Tabla genérica con buscador de texto libre y filtros desplegables.
 * Envuelve DataTable y acepta todos sus props, más `filterConfig`.
 *
 * Props adicionales
 * ─────────────────
 * filterConfig   FilterConfig   Configuración de búsqueda y filtros.
 *                               Si se omite, se comporta igual que DataTable.
 *
 * FilterConfig
 * ────────────
 * search?    SearchDef     Buscador de texto libre.
 * filters?   FilterDef[]   Filtros desplegables (pueden combinarse con search).
 *
 * SearchDef
 * ─────────
 * fields        string[]   Campos dot-notation sobre los que buscar.
 * placeholder?  string     Texto de ayuda del input (default: "Buscar...").
 *
 * FilterDef
 * ─────────
 * key       string                  Identificador único. También se usa como campo
 *                                   dot-notation del ítem cuando no hay `match`.
 * label     string                  Etiqueta del desplegable.
 * options   { value, label }[]      Opciones del select. Se generan desde estado
 *                                   del componente padre o se derivan de los datos.
 * match?    (item, val) => boolean  Función personalizada de comparación.
 *                                   Si se omite, compara String(item[key]) === val.
 *                                   Útil para arrays, booleans y campos numéricos.
 *
 * Ejemplos de uso
 * ───────────────
 * // Solo búsqueda de texto:
 * <FilterableDataTable
 *   filterConfig={{ search: { fields: ['nombre', 'descripcion'] } }}
 *   columns={...} data={...} keyExtractor={...} actions={...}
 * />
 *
 * // Con filtros desplegables derivados de estado del padre:
 * <FilterableDataTable
 *   filterConfig={{
 *     search: { fields: ['titulo'], placeholder: 'Buscar norma...' },
 *     filters: [
 *       { key: 'institutionNombre', label: 'Institución',
 *         options: institutions.map(i => ({ value: i.nombre, label: i.nombre })) },
 *     ],
 *   }}
 *   ...
 * />
 *
 * // Con filtro sobre campo array o booleano (match personalizado):
 * <FilterableDataTable
 *   filterConfig={{
 *     filters: [
 *       { key: 'areasIds', label: 'Área',
 *         options: areas.map(a => ({ value: String(a.id), label: a.nombre })),
 *         match: (item, val) => (item.areasIds ?? []).map(String).includes(val) },
 *       { key: 'esNacional', label: 'Tipo',
 *         options: [{ value: 'true', label: 'Nacional' }, { value: 'false', label: 'Internacional' }],
 *         match: (item, val) => String(item.esNacional) === val },
 *     ],
 *   }}
 *   ...
 * />
 */
export default function FilterableDataTable({ filterConfig, data = [], ...tableProps }) {
  const { search, filters = [] } = filterConfig ?? {};

  const [text, setText] = useState('');
  const [active, setActive] = useState({});

  const isFiltered = text !== '' || Object.values(active).some(v => v !== '' && v != null);

  // ── Lógica de filtrado ────────────────────────────────────────────────────
  // Filtering logic intentionally omits filterConfig from dependencies —
  // config is set once and doesn't change; only data, search text, and active filters trigger re-filtering.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const filtered = useMemo(() => {
    let result = data;

    // Búsqueda de texto libre (case-insensitive, dot-notation)
    if (search?.fields?.length && text.trim()) {
      const q = text.trim().toLowerCase();
      result = result.filter(item =>
        search.fields.some(field =>
          String(getVal(item, field) ?? '').toLowerCase().includes(q)
        )
      );
    }

    // Filtros desplegables
    for (const fd of filters) {
      const sel = active[fd.key] ?? '';
      if (!sel) continue;
      result = result.filter(item =>
        fd.match
          ? fd.match(item, sel)
          : String(getVal(item, fd.key) ?? '') === sel
      );
    }

    return result;
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data, text, active]);

  function clearAll() {
    setText('');
    setActive({});
  }

  // Sin configuración → tabla plana sin overhead
  if (!search && filters.length === 0) {
    return <DataTable {...tableProps} data={data} />;
  }

  return (
    <div>
      {/* ── Barra de búsqueda y filtros ── */}
      <div className="px-3 pt-3 pb-2 border-bottom bg-light">
        <div className="d-flex flex-wrap gap-2 align-items-end">

          {/* Buscador de texto */}
          {search && (
            <div style={{ flex: '2 1 200px', minWidth: 180 }}>
              <div className="small text-muted mb-1">Buscar</div>
              <div className="input-group input-group-sm">
                <Input
                  bsSize="sm"
                  placeholder={search.placeholder ?? 'Buscar...'}
                  value={text}
                  onChange={e => setText(e.target.value)}
                />
                {text && (
                  <button
                    className="btn btn-outline-secondary"
                    type="button"
                    aria-label="Limpiar búsqueda"
                    onClick={() => setText('')}
                  >
                    <i className="bi bi-x" />
                  </button>
                )}
              </div>
            </div>
          )}

          {/* Filtros desplegables */}
          {filters.map(f => (
            <div key={f.key} style={{ flex: '1 1 150px', minWidth: 130 }}>
              <div className="small text-muted mb-1">{f.label}</div>
              <Input
                type="select"
                bsSize="sm"
                value={active[f.key] ?? ''}
                onChange={e => setActive(a => ({ ...a, [f.key]: e.target.value }))}
              >
                <option value="">— Todos —</option>
                {f.options.map(o => (
                  <option key={o.value} value={o.value}>{o.label}</option>
                ))}
              </Input>
            </div>
          ))}

          {/* Botón limpiar todo */}
          {isFiltered && (
            <div style={{ flex: '0 0 auto', alignSelf: 'flex-end' }}>
              <button
                className="btn btn-sm btn-outline-secondary"
                type="button"
                onClick={clearAll}
              >
                <i className="bi bi-x-circle me-1" />
                Limpiar
              </button>
            </div>
          )}
        </div>

        {/* Contador de resultados */}
        {isFiltered && (
          <small className="text-muted d-block mt-1">
            {filtered.length} de {data.length} resultado{data.length !== 1 ? 's' : ''}
          </small>
        )}
      </div>

      <DataTable {...tableProps} data={filtered} />
    </div>
  );
}
