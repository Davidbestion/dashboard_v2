import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Badge, Spinner, Alert,
} from 'reactstrap';
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

const TIPOS = [
  { value: 'en-revision',                label: 'En Revisión',                        color: 'warning'   },
  { value: 'empresariales',              label: 'Empresarial (PE)',                    color: 'primary'   },
  { value: 'apoyo-programa',             label: 'Apoyo a Programa (PAP)',              color: 'info'      },
  { value: 'desarrollo-local',           label: 'Desarrollo Local (PDL)',              color: 'success'   },
  { value: 'no-empresariales',           label: 'No Empresarial (PNE)',                color: 'secondary' },
  { value: 'colaboracion-internacional', label: 'Colaboración Internacional (PRCI)',   color: 'danger'    },
  { value: 'pnap',                       label: 'PNAP',                                color: 'dark'      },
];

const tipoLabel = (v) => TIPOS.find(t => t.value === v)?.label ?? v;
const tipoColor = (v) => TIPOS.find(t => t.value === v)?.color ?? 'secondary';

export default function MisProyectosPage() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await apiFetch('/api/Proyectos/participacion');
      setItems(data);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

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
        <h2 className="mb-0">Mis Proyectos</h2>
      </div>

      {error && <Alert color="danger" toggle={() => setError('')}>{error}</Alert>}

      <Card>
        <CardHeader>
          <strong>Proyectos en los que participas</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['titulo', 'jefe'], placeholder: 'Buscar por título o jefe...' },
              filters: [
                { key: 'tipo', label: 'Tipo',
                  options: TIPOS.map(t => ({ value: t.value, label: t.label })),
                  match: (item, val) => item.tipo === val },
                { key: 'clasificacionNombre', label: 'Clasificación',
                  options: [...new Set(items.map(i => i.clasificacionNombre).filter(Boolean))].sort().map(v => ({ value: v, label: v })) },
              ],
            }}
            columns={[
              { key: 'tipo',              label: 'Tipo',         sortable: true, render: v => <Badge color={tipoColor(v)}>{tipoLabel(v)}</Badge> },
              { key: 'titulo',            label: 'Título',       sortable: true },
              { key: 'jefe',              label: 'Jefe' },
              { key: 'clasificacionNombre', label: 'Clasificación' },
              {
                key: 'participantes',
                label: 'Participantes',
                render: (ps) => ps && ps.length > 0
                  ? <Badge color="secondary" pill>{ps.length} participante{ps.length !== 1 ? 's' : ''}</Badge>
                  : <span className="text-muted small">Sin participantes</span>,
              },
            ]}
            data={items}
            keyExtractor={item => item.id}
            emptyMessage="No participas en ningún proyecto."
            detailConfig
          />
        </CardBody>
      </Card>
    </>
  );
}
