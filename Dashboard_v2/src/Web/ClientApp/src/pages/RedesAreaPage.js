import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardBody, CardHeader, Spinner, Alert, Button } from 'reactstrap';
import FilterableDataTable from '../components/FilterableDataTable';

const TIPOS_RED = [
  { value: 0, label: 'Universitaria' },
  { value: 1, label: 'Nacional' },
  { value: 2, label: 'Internacional' },
];

const ANEXOS = [
  { reportName: 'anexo-redes-universitarias', label: 'Anexo Redes Universitarias', fileLabel: 'Anexo_Redes_Universitarias' },
  { reportName: 'anexo-redes-nac-inter',      label: 'Anexo Redes Nac./Inter.',    fileLabel: 'Anexo_Redes_Nac_Inter'       },
];

async function apiFetch(url) {
  const res = await fetch(url, { credentials: 'include' });
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new Error((data?.errors ?? ['Error desconocido.']).join(' '));
  return data;
}

export default function RedesAreaPage() {
  const [items, setItems]             = useState([]);
  const [loading, setLoading]         = useState(true);
  const [error, setError]             = useState('');
  const [generatingAnexo, setGeneratingAnexo] = useState(null);
  const [anexoError, setAnexoError]   = useState('');

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try { setItems(await apiFetch('/api/Redes')); }
    catch (e) { setError(e.message); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  async function handleGenerarAnexo(reportName, fileLabel) {
    setGeneratingAnexo(reportName); setAnexoError('');
    try {
      const response = await fetch(`/api/Documents/${reportName}`, { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.error ?? 'Error al generar el anexo.');
      }
      const blob = await response.blob();
      const isZip = (response.headers.get('Content-Type') ?? '').includes('zip');
      const ext = isZip ? 'zip' : 'xlsx';
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      const now = new Date();
      a.download = `${fileLabel}_${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}.${ext}`;
      a.href = url;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e) {
      setAnexoError(e.message);
    } finally {
      setGeneratingAnexo(null);
    }
  }

  if (loading) return <div className="d-flex justify-content-center mt-5"><Spinner color="primary" /></div>;

  return (
    <>
      <h2 className="mb-4">Redes del Área</h2>
      {error && <Alert color="danger">{error}</Alert>}
      {anexoError && <Alert color="danger">{anexoError}</Alert>}

      <div className="d-flex flex-wrap gap-2 mb-3">
        {ANEXOS.map(({ reportName, label, fileLabel }) => (
          <Button
            key={reportName}
            color="success"
            size="sm"
            onClick={() => handleGenerarAnexo(reportName, fileLabel)}
            disabled={generatingAnexo !== null}
          >
            {generatingAnexo === reportName ? <Spinner size="sm" /> : `⬇ ${label}`}
          </Button>
        ))}
      </div>

      <Card>
        <CardHeader>
          <strong>Redes</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['nombre', 'countryName'], placeholder: 'Buscar red...' },
              filters: [
                {
                  key: 'tipo',
                  label: 'Tipo',
                  options: TIPOS_RED.map(t => ({ value: t.value, label: t.label })),
                  match: (item, val) => item.tipo === Number(val),
                },
              ],
            }}
            columns={[
              { key: 'nombre',             label: 'Nombre',   sortable: true },
              { key: 'tipo',               label: 'Tipo',     render: v => TIPOS_RED.find(t => t.value === v)?.label ?? v },
              { key: 'countryName',        label: 'País' },
              { key: 'cantidadProfesores', label: 'Profesores' },
            ]}
            data={items}
            keyExtractor={i => i.id}
            emptyMessage="No hay redes en el área."
            detailConfig
          />
        </CardBody>
      </Card>
    </>
  );
}
