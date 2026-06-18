import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardBody, CardHeader, Spinner, Alert } from 'reactstrap';
import FilterableDataTable from '../components/FilterableDataTable';

async function apiFetch(url) {
  const res = await fetch(url, { credentials: 'include' });
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new Error((data?.errors ?? ['Error desconocido.']).join(' '));
  return data;
}

export default function ProductosAreaPage() {
  const [items, setItems]     = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState('');

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try { setItems(await apiFetch('/api/ProductosComercializados')); }
    catch (e) { setError(e.message); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  if (loading) return <div className="d-flex justify-content-center mt-5"><Spinner color="primary" /></div>;

  const tipos        = [...new Set(items.map(i => i.tipoProductoComercializadoNombre).filter(Boolean))].sort();
  const institutions = [...new Set(items.map(i => i.institutionNombre).filter(Boolean))].sort();

  return (
    <>
      <h2 className="mb-4">Productos Comercializados del Área</h2>
      {error && <Alert color="danger">{error}</Alert>}

      <Card>
        <CardHeader>
          <strong>Productos Comercializados</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['titulo'], placeholder: 'Buscar producto...' },
              filters: [
                { key: 'tipoProductoComercializadoNombre', label: 'Tipo',        options: tipos.map(v => ({ value: v, label: v })) },
                { key: 'institutionNombre',                label: 'Institución', options: institutions.map(v => ({ value: v, label: v })) },
              ],
            }}
            columns={[
              { key: 'titulo',                          label: 'Título',      sortable: true },
              { key: 'tipoProductoComercializadoNombre', label: 'Tipo' },
              { key: 'institutionNombre',               label: 'Institución' },
              { key: 'creadores',                       label: 'Creadores', render: v => (v ?? []).join(', ') },
            ]}
            data={items}
            keyExtractor={i => i.id}
            emptyMessage="No hay productos comercializados."
            detailConfig
          />
        </CardBody>
      </Card>
    </>
  );
}
