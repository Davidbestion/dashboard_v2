import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardBody, CardHeader, Spinner, Alert, Badge, Button } from 'reactstrap';
import FilterableDataTable from '../components/FilterableDataTable';

async function apiFetch(url) {
  const res = await fetch(url, { credentials: 'include' });
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new Error((data?.errors ?? ['Error desconocido.']).join(' '));
  return data;
}

export default function RegistrosAreaPage() {
  const [items, setItems]             = useState([]);
  const [loading, setLoading]         = useState(true);
  const [error, setError]             = useState('');
  const [generatingAnexo, setGeneratingAnexo] = useState(false);
  const [anexoError, setAnexoError]   = useState('');

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try { setItems(await apiFetch('/api/Registros')); }
    catch (e) { setError(e.message); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  async function handleGenerarAnexo() {
    setGeneratingAnexo(true); setAnexoError('');
    try {
      const response = await fetch('/api/Documents/anexo-registros', { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.error ?? 'Error al generar el anexo.');
      }
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      const now = new Date();
      a.download = `anexo-registros_${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}.xlsx`;
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

  const countries     = [...new Set(items.map(i => i.countryName).filter(Boolean))].sort();
  const institutions  = [...new Set(items.map(i => i.institutionNombre).filter(Boolean))].sort();

  return (
    <>
      <h2 className="mb-4">Registros del Área</h2>
      {error && <Alert color="danger">{error}</Alert>}
      {anexoError && <Alert color="danger">{anexoError}</Alert>}

      <Card>
        <CardHeader className="d-flex justify-content-between align-items-center">
          <div>
            <strong>Registros</strong>
            <small className="text-muted ms-2">({items.length})</small>
          </div>
          <Button color="success" size="sm" onClick={handleGenerarAnexo} disabled={generatingAnexo}>
            {generatingAnexo ? <Spinner size="sm" /> : '⬇ Generar Anexo 7'}
          </Button>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['titulo', 'numeroCertificado'], placeholder: 'Buscar registro...' },
              filters: [
                { key: 'countryName',       label: 'País',        options: countries.map(v => ({ value: v, label: v })) },
                { key: 'institutionNombre', label: 'Institución', options: institutions.map(v => ({ value: v, label: v })) },
              ],
            }}
            columns={[
              { key: 'titulo',            label: 'Título',     sortable: true },
              { key: 'numeroCertificado', label: 'Nº certif.' },
              { key: 'countryName',       label: 'País', render: v => <Badge color="secondary" pill>{v}</Badge> },
              { key: 'institutionNombre', label: 'Institución' },
              { key: 'creadores',         label: 'Creadores', render: v => (v ?? []).join(', ') },
            ]}
            data={items}
            keyExtractor={i => i.id}
            emptyMessage="No hay registros."
            detailConfig
          />
        </CardBody>
      </Card>
    </>
  );
}
