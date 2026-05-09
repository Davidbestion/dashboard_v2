import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardBody, CardHeader, Spinner, Alert } from 'reactstrap';
import FilterableDataTable from '../components/FilterableDataTable';

async function apiFetch(url) {
  const res = await fetch(url, { credentials: 'include' });
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new Error((data?.errors ?? ['Error desconocido.']).join(' '));
  return data;
}

export default function PatentesAreaPage() {
  const [items, setItems]     = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState('');

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try { setItems(await apiFetch('/api/Patentes')); }
    catch (e) { setError(e.message); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  if (loading) return <div className="d-flex justify-content-center mt-5"><Spinner color="primary" /></div>;

  return (
    <>
      <h2 className="mb-4">Patentes del Área</h2>
      {error && <Alert color="danger">{error}</Alert>}

      <Card>
        <CardHeader>
          <strong>Patentes</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['titulo', 'numeroSolicitudConcesion'], placeholder: 'Buscar patente...' },
              filters: [
                {
                  key: 'esNacional', label: 'Tipo',
                  options: [{ value: 'true', label: 'Nacional' }, { value: 'false', label: 'Internacional' }],
                  match: (item, val) => String(item.esNacional) === val,
                },
              ],
            }}
            columns={[
              { key: 'titulo',                   label: 'Título',   sortable: true },
              { key: 'numeroSolicitudConcesion', label: 'Nº solicitud' },
              { key: 'esNacional',               label: 'Tipo',     render: v => v ? 'Nacional' : 'Internacional' },
              { key: 'creadores',                label: 'Creadores', render: v => (v ?? []).join(', ') },
            ]}
            data={items}
            keyExtractor={i => i.id}
            emptyMessage="No hay patentes."
          />
        </CardBody>
      </Card>
    </>
  );
}
