import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardBody, CardHeader, Spinner, Alert, Badge, Button } from 'reactstrap';
import FilterableDataTable from '../components/FilterableDataTable';

async function apiFetch(url) {
  const res = await fetch(url, { credentials: 'include' });
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new Error((data?.errors ?? ['Error desconocido.']).join(' '));
  return data;
}

export default function PremiosAreaPage() {
  const [items, setItems]             = useState([]);
  const [loading, setLoading]         = useState(true);
  const [error, setError]             = useState('');
  const [generatingAnexo, setGeneratingAnexo] = useState(false);
  const [anexoError, setAnexoError]   = useState('');

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try { setItems(await apiFetch('/api/Awards/area')); }
    catch (e) { setError(e.message); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  async function handleGenerarAnexo() {
    setGeneratingAnexo(true); setAnexoError('');
    try {
      const response = await fetch('/api/Documents/anexo-premios', { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.error ?? 'Error al generar el anexo.');
      }
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      const now = new Date();
      a.download = `anexo-premios_${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}.xlsx`;
      a.href = url;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e) {
      setAnexoError(e.message);
    } finally {
      setGeneratingAnexo(false);
    }
  }

  if (loading) return <div className="d-flex justify-content-center mt-5"><Spinner color="primary" /></div>;

  // Flatten award groups into rows for the table
  const rows = items.flatMap(award =>
    award.grantings.flatMap(g =>
      g.recipients.map(r => ({
        id: r.id,
        awardName: award.awardName,
        awardTypeName: award.awardTypeName,
        awardedAt: g.awardedAt,
        recipient: r.userDisplayName,
      }))
    )
  );

  const types = [...new Set(rows.map(r => r.awardTypeName).filter(Boolean))].sort();

  return (
    <>
      <h2 className="mb-4">Premios del Área</h2>
      {error && <Alert color="danger">{error}</Alert>}
      {anexoError && <Alert color="danger">{anexoError}</Alert>}

      <Card>
        <CardHeader className="d-flex justify-content-between align-items-center">
          <div>
            <strong>Premios</strong>
            <small className="text-muted ms-2">({rows.length})</small>
          </div>
          <Button color="success" size="sm" onClick={handleGenerarAnexo} disabled={generatingAnexo}>
            {generatingAnexo ? <Spinner size="sm" /> : '⬇ Generar Anexo 5'}
          </Button>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['awardName', 'recipient'], placeholder: 'Buscar premio o persona...' },
              filters: [
                { key: 'awardTypeName', label: 'Tipo', options: types.map(v => ({ value: v, label: v })) },
              ],
            }}
            columns={[
              { key: 'awardName',     label: 'Premio',    sortable: true },
              { key: 'awardTypeName', label: 'Tipo',      render: v => <Badge color="secondary" pill>{v}</Badge> },
              { key: 'recipient',     label: 'Receptor' },
              { key: 'awardedAt',     label: 'Fecha',     render: v => v ? new Date(v).toLocaleDateString('es-CU') : '—' },
            ]}
            data={rows}
            keyExtractor={r => r.id}
            emptyMessage="No hay premios registrados en el área."
            detailConfig
          />
        </CardBody>
      </Card>
    </>
  );
}
