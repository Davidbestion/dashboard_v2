import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Badge, Button,
  Spinner, Alert,
} from 'reactstrap';
import DataTable from '../components/DataTable';
import FilterableDataTable from '../components/FilterableDataTable';

async function apiFetch(url, options = {}) {
  const response = await fetch(url, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...(options.headers ?? {}) },
    ...options,
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    const errors = data?.errors ?? ['Error desconocido.'];
    throw new Error(Array.isArray(errors) ? errors.join(' ') : String(errors));
  }
  return data;
}

export default function MisGruposDeInvestigacionPage() {
  const [items, setItems] = useState([]);
  const [lineasDeInvestigacion, setLineasDeInvestigacion] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [generatingAnexo, setGeneratingAnexo] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [gruposData, lineasData] = await Promise.all([
        apiFetch('/api/GruposDeInvestigacion/mine'),
        apiFetch('/api/LineasDeInvestigacion'),
      ]);
      setItems(gruposData);
      setLineasDeInvestigacion(lineasData);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  async function handleGenerarAnexo() {
    setGeneratingAnexo(true);
    setError('');
    try {
      const response = await fetch('/api/Documents/anexo-grupos', { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.error ?? 'Error al generar el Anexo 10.');
      }
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      const now = new Date();
      a.download = `Anexo_10_Grupos_Investigacion_${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}.xlsx`;
      a.href = url;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e) {
      setError(e.message);
    } finally {
      setGeneratingAnexo(false);
    }
  }

  if (loading) {
    return (
      <div className="d-flex justify-content-center mt-5">
        <Spinner color="primary" />
      </div>
    );
  }

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="mb-0">Mis Grupos de Investigación</h2>
        <Button color="outline-success" onClick={handleGenerarAnexo} disabled={generatingAnexo}>
          {generatingAnexo ? <Spinner size="sm" /> : '⬇ Generar Anexo 10'}
        </Button>
      </div>

      {error && <Alert color="danger" toggle={() => setError('')}>{error}</Alert>}

      <Card>
        <CardHeader>
          <strong>Grupos a los que perteneces</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['nombre'], placeholder: 'Buscar grupo...' },
              filters: [
                { key: 'areaNombre', label: 'Área',
                  options: [...new Set(items.map(i => i.areaNombre).filter(Boolean))].sort().map(v => ({ value: v, label: v })) },
              ],
            }}
            columns={[
              { key: 'nombre',   label: 'Nombre', sortable: true, className: 'fw-semibold' },
              { key: 'areaNombre', label: 'Área', render: v => <Badge color="secondary" pill>{v}</Badge> },
              {
                key: 'lineasDeInvestigacionIds',
                label: 'Líneas de investigación',
                render: (ids, item) => ids && ids.length > 0
                  ? lineasDeInvestigacion
                      .filter(l => ids.includes(l.id))
                      .map(l => <Badge key={l.id} color="info" pill className="me-1">{l.nombre}</Badge>)
                  : <span className="text-muted small">Sin líneas asignadas</span>,
              },
            ]}
            data={items}
            keyExtractor={item => item.id}
            emptyMessage="No perteneces a ningún grupo de investigación."
            detailConfig
          />
        </CardBody>
      </Card>
    </>
  );
}
