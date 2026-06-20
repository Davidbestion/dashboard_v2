import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardBody, CardHeader, Spinner, Alert, Button } from 'reactstrap';
import FilterableDataTable from '../components/FilterableDataTable';

async function apiFetch(url) {
  const res = await fetch(url, { credentials: 'include' });
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new Error((data?.errors ?? ['Error desconocido.']).join(' '));
  return data;
}

export default function GruposEstudiantilesAreaPage() {
  const [items, setItems]             = useState([]);
  const [loading, setLoading]         = useState(true);
  const [error, setError]             = useState('');
  const [generatingAnexo, setGeneratingAnexo] = useState(false);
  const [anexoError, setAnexoError]   = useState('');

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try { setItems(await apiFetch('/api/GruposEstudiantiles/area')); }
    catch (e) { setError(e.message); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  async function handleGenerarAnexo() {
    setGeneratingAnexo(true); setAnexoError('');
    try {
      const response = await fetch('/api/Documents/anexo-grupos-estudiantiles', { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.error ?? 'Error al generar el anexo.');
      }
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      const now = new Date();
      a.download = `anexo-grupos-estudiantiles_${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}.xlsx`;
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

  return (
    <>
      <h2 className="mb-4">Grupos Estudiantiles del Área</h2>
      {error && <Alert color="danger">{error}</Alert>}
      {anexoError && <Alert color="danger">{anexoError}</Alert>}

      <Card>
        <CardHeader className="d-flex justify-content-between align-items-center">
          <div>
            <strong>Grupos Científicos Estudiantiles</strong>
            <small className="text-muted ms-2">({items.length})</small>
          </div>
          <Button color="success" size="sm" onClick={handleGenerarAnexo} disabled={generatingAnexo}>
            {generatingAnexo ? <Spinner size="sm" /> : '⬇ Generar Anexo 9'}
          </Button>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['nombre', 'areaNombre'], placeholder: 'Buscar grupo...' },
            }}
            columns={[
              { key: 'nombre',     label: 'Nombre',    sortable: true },
              { key: 'areaNombre', label: 'Área' },
              { key: 'lineasDeInvestigacionIds', label: 'Líneas', render: v => v?.length ?? 0 },
            ]}
            data={items}
            keyExtractor={i => i.id}
            emptyMessage="No hay grupos estudiantiles en el área."
            detailConfig
          />
        </CardBody>
      </Card>
    </>
  );
}
