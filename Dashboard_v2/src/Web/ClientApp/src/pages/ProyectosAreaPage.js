import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardBody, CardHeader, Spinner, Alert, Button } from 'reactstrap';
import FilterableDataTable from '../components/FilterableDataTable';

const TIPOS = [
  { value: 'en-revision',                label: 'En Revisión'                       },
  { value: 'empresariales',              label: 'Empresarial (PE)'                   },
  { value: 'apoyo-programa',             label: 'Apoyo a Programa (PAP)'             },
  { value: 'desarrollo-local',           label: 'Desarrollo Local (PDL)'             },
  { value: 'no-empresariales',           label: 'No Empresarial (PNE)'               },
  { value: 'colaboracion-internacional', label: 'Colaboración Internacional (PRCI)'  },
  { value: 'pnap',                       label: 'PNAP'                               },
];

const ANEXOS = [
  { reportName: 'anexo-proyectos', label: 'Anexo 4 — Proyectos', ext: 'xlsx' },
];

async function apiFetch(url) {
  const res = await fetch(url, { credentials: 'include' });
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new Error((data?.errors ?? ['Error desconocido.']).join(' '));
  return data;
}

async function downloadReport(reportName, ext) {
  const response = await fetch(`/api/Documents/${reportName}`, { credentials: 'include' });
  if (!response.ok) {
    const data = await response.json().catch(() => null);
    throw new Error(data?.error ?? 'Error al generar el anexo.');
  }
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  const now = new Date();
  a.download = `${reportName}_${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}.${ext}`;
  a.href = url;
  a.click();
  URL.revokeObjectURL(url);
}

export default function ProyectosAreaPage() {
  const [items, setItems]             = useState([]);
  const [loading, setLoading]         = useState(true);
  const [error, setError]             = useState('');
  const [generatingAnexo, setGeneratingAnexo] = useState(null);
  const [anexoError, setAnexoError]   = useState('');

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try { setItems(await apiFetch('/api/Proyectos')); }
    catch (e) { setError(e.message); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  async function handleGenerarAnexo(reportName, ext) {
    setGeneratingAnexo(reportName); setAnexoError('');
    try { await downloadReport(reportName, ext); }
    catch (e) { setAnexoError(e.message); }
    finally { setGeneratingAnexo(null); }
  }

  if (loading) return <div className="d-flex justify-content-center mt-5"><Spinner color="primary" /></div>;

  return (
    <>
      <h2 className="mb-4">Proyectos del Área</h2>
      {error && <Alert color="danger">{error}</Alert>}
      {anexoError && <Alert color="danger">{anexoError}</Alert>}

      <div className="d-flex flex-wrap gap-2 mb-3">
        {ANEXOS.map(({ reportName, label, ext }) => (
          <Button
            key={reportName}
            color="success"
            size="sm"
            onClick={() => handleGenerarAnexo(reportName, ext)}
            disabled={generatingAnexo !== null}
          >
            {generatingAnexo === reportName ? <Spinner size="sm" /> : `⬇ ${label}`}
          </Button>
        ))}
      </div>

      <Card>
        <CardHeader>
          <strong>Proyectos</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['titulo', 'jefe', 'correoJefe'], placeholder: 'Buscar proyecto...' },
              filters: [
                {
                  key: 'tipo',
                  label: 'Tipo',
                  options: TIPOS.map(t => ({ value: t.value, label: t.label })),
                },
              ],
            }}
            columns={[
              { key: 'titulo',      label: 'Título',    sortable: true },
              { key: 'tipo',        label: 'Tipo',      render: v => TIPOS.find(t => t.value === v)?.label ?? v },
              { key: 'jefe',        label: 'Jefe',      render: (v, row) => <>{v}<br /><small className="text-muted">{row.correoJefe}</small></> },
              { key: 'numeroMiembros', label: 'Miembros' },
              { key: 'participantes',  label: 'Participantes', render: v => (v ?? []).map(p => p.nombreCompleto).join(', ') || '—' },
            ]}
            data={items}
            keyExtractor={i => i.id}
            emptyMessage="No hay proyectos en el área."
            detailConfig
          />
        </CardBody>
      </Card>
    </>
  );
}
