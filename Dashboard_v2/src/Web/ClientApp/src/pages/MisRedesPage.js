import React, { useState, useEffect, useCallback } from 'react';
import {
  Alert, Badge, Button, Card, CardBody, CardHeader,
  Modal, ModalHeader, ModalBody, ModalFooter, Spinner,
} from 'reactstrap';
import { useAuth } from '../contexts/AuthContext';
import FilterableDataTable from '../components/FilterableDataTable';

const TIPOS_RED = [
  { value: 0, label: 'Universitaria' },
  { value: 1, label: 'Nacional' },
  { value: 2, label: 'Internacional' },
];

const TIPO_OPTIONS = TIPOS_RED.map(t => ({ value: String(t.value), label: t.label }));

// Explicit detail modal config for RedConCoordinadorDto
const RED_DETAIL_CONFIG = {
  fields: [
    { key: 'nombre',           label: 'Nombre' },
    { key: 'tipo',             label: 'Tipo',        render: v => TIPOS_RED.find(t => t.value === v)?.label ?? v },
    { key: 'countryName',      label: 'País',        render: v => v ?? '—' },
    { key: 'cantidadProfesores', label: 'Profesores' },
    { key: 'coordinadorNombre',  label: 'Coordinador', render: v => v ?? <span className="text-muted">Sin coordinador</span> },
    { key: 'coordinadorEmail',   label: 'Correo del coordinador', render: v => v ?? '—' },
    {
      key: 'participantes',
      label: 'Participantes',
      render: v => v?.length
        ? v.map(p => p.authorNombre).join(', ')
        : <span className="text-muted">Sin participantes</span>,
    },
  ],
};

async function apiFetch(url, options) {
  const res = await fetch(url, { credentials: 'include', ...options });
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new Error((data?.errors ?? ['Error desconocido.']).join(' '));
  return data;
}

// Columns shared by both tables
const COLUMNS = [
  { key: 'nombre', label: 'Nombre', sortable: true },
  {
    key: 'tipo',
    label: 'Tipo',
    render: v => TIPOS_RED.find(t => t.value === v)?.label ?? v,
  },
  { key: 'countryName', label: 'País', render: v => v ?? '—' },
  { key: 'cantidadProfesores', label: 'Profesores' },
  {
    key: 'coordinadorNombre',
    label: 'Coordinador',
    render: (v, row) => v
      ? <>{v}<br /><small className="text-muted">{row.coordinadorEmail}</small></>
      : <span className="text-muted">Sin coordinador</span>,
  },
  {
    key: 'participantes',
    label: 'Participantes',
    render: v => v?.length ?? 0,
  },
];

const FILTER_CONFIG = {
  search: { fields: ['nombre', 'countryName'], placeholder: 'Buscar red...' },
  filters: [
    {
      key: 'tipo',
      label: 'Tipo',
      options: TIPO_OPTIONS,
      match: (item, val) => item.tipo === Number(val),
    },
  ],
};

export default function MisRedesPage() {
  const { user } = useAuth();
  const [redes, setRedes] = useState([]);
  const [todasRedes, setTodasRedes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [actionError, setActionError] = useState('');
  const [joinModalOpen, setJoinModalOpen] = useState(false);
  const [joining, setJoining] = useState(false);

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try {
      const data = await apiFetch('/api/Redes/mis-redes');
      setRedes(Array.isArray(data) ? data : []);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  const loadTodasRedes = useCallback(async () => {
    try {
      const data = await apiFetch('/api/Redes');
      setTodasRedes(Array.isArray(data) ? data : []);
    } catch {
      setTodasRedes([]);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openJoinModal = () => {
    loadTodasRedes();
    setActionError('');
    setJoinModalOpen(true);
  };

  const handleJoin = async (red) => {
    setJoining(true); setActionError('');
    try {
      await apiFetch(`/api/Redes/${red.id}/participaciones/mine`, { method: 'POST' });
      setJoinModalOpen(false);
      await load();
    } catch (e) {
      setActionError(e.message);
    } finally {
      setJoining(false);
    }
  };

  const handleLeave = async (red) => {
    if (!window.confirm(`¿Seguro que deseas salir de la red "${red.nombre}"?`)) return;
    setActionError('');
    try {
      await apiFetch(`/api/Redes/${red.id}/participaciones/mine`, { method: 'DELETE' });
      await load();
    } catch (e) {
      setActionError(e.message);
    }
  };

  const coordinadas = redes.filter(r => r.coordinadorId === user?.id);
  const participando = redes.filter(r => r.coordinadorId !== user?.id);

  // Networks not yet joined (for the join modal)
  const miRedIds = new Set(redes.map(r => r.id));
  const disponibles = todasRedes.filter(r => !miRedIds.has(r.id));

  if (loading) return <div className="d-flex justify-content-center mt-5"><Spinner color="primary" /></div>;

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="mb-0">Mis Redes</h2>
        <Button color="primary" onClick={openJoinModal}>
          <i className="bi bi-plus-circle me-1" /> Unirse a una Red
        </Button>
      </div>

      {error && <Alert color="danger">{error}</Alert>}
      {actionError && <Alert color="danger" toggle={() => setActionError('')}>{actionError}</Alert>}

      <Card className="mb-4">
        <CardHeader className="d-flex align-items-center gap-2">
          <strong>Redes que coordino</strong>
          <Badge color="secondary" pill>{coordinadas.length}</Badge>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={FILTER_CONFIG}
            columns={[
              ...COLUMNS,
              {
                key: 'id',
                label: '',
                render: () => <Badge color="primary" pill>Coordinador</Badge>,
              },
            ]}
            data={coordinadas}
            keyExtractor={r => r.id}
            emptyMessage="No coordinas ninguna red."
            detailConfig={RED_DETAIL_CONFIG}
          />
        </CardBody>
      </Card>

      <Card>
        <CardHeader className="d-flex align-items-center gap-2">
          <strong>Redes en las que participo</strong>
          <Badge color="secondary" pill>{participando.length}</Badge>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={FILTER_CONFIG}
            columns={COLUMNS}
            data={participando}
            keyExtractor={r => r.id}
            actions={[
              {
                key: 'leave',
                label: 'Salir',
                icon: 'bi-box-arrow-right',
                color: 'outline-danger',
                onClick: handleLeave,
              },
            ]}
            emptyMessage="No participas en ninguna red."
            detailConfig={RED_DETAIL_CONFIG}
          />
        </CardBody>
      </Card>

      {/* Modal: join a network */}
      <Modal isOpen={joinModalOpen} toggle={() => setJoinModalOpen(false)} size="lg">
        <ModalHeader toggle={() => setJoinModalOpen(false)}>
          Unirse a una Red
        </ModalHeader>
        <ModalBody>
          {actionError && <Alert color="danger">{actionError}</Alert>}
          <FilterableDataTable
            filterConfig={FILTER_CONFIG}
            columns={[
              { key: 'nombre', label: 'Nombre', sortable: true },
              {
                key: 'tipo',
                label: 'Tipo',
                render: v => TIPOS_RED.find(t => t.value === v)?.label ?? v,
              },
              { key: 'countryName', label: 'País', render: v => v ?? '—' },
              { key: 'cantidadProfesores', label: 'Profesores' },
            ]}
            data={disponibles}
            keyExtractor={r => r.id}
            actions={[
              {
                key: 'join',
                label: 'Unirse',
                icon: 'bi-person-plus',
                color: 'outline-primary',
                onClick: handleJoin,
                disabled: () => joining,
              },
            ]}
            emptyMessage="No hay redes disponibles para unirse."
          />
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" outline onClick={() => setJoinModalOpen(false)}>
            Cerrar
          </Button>
        </ModalFooter>
      </Modal>
    </>
  );
}
