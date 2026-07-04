import React, { useState, useEffect, useCallback } from 'react';
import {
  Alert, Badge, Button, Card, CardBody, CardHeader,
  Modal, ModalHeader, ModalBody, ModalFooter, Spinner,
} from 'reactstrap';
import FilterableDataTable from '../components/FilterableDataTable';
import { apiFetch } from '../utils/apiFetch';

const TIPOS = [
  { value: 'en-revision',                label: 'En Revisión',                      color: 'warning'   },
  { value: 'empresariales',              label: 'Empresarial (PE)',                  color: 'primary'   },
  { value: 'apoyo-programa',             label: 'Apoyo a Programa (PAP)',            color: 'info'      },
  { value: 'desarrollo-local',           label: 'Desarrollo Local (PDL)',            color: 'success'   },
  { value: 'no-empresariales',           label: 'No Empresarial (PNE)',              color: 'secondary' },
  { value: 'colaboracion-internacional', label: 'Colaboración Internacional (PRCI)', color: 'danger'    },
  { value: 'pnap',                       label: 'PNAP',                              color: 'dark'      },
];

const tipoLabel = (v) => TIPOS.find(t => t.value === v)?.label ?? v;
const tipoColor = (v) => TIPOS.find(t => t.value === v)?.color ?? 'secondary';

const COLUMNS = [
  { key: 'tipo',              label: 'Tipo',          sortable: true, render: v => <Badge color={tipoColor(v)}>{tipoLabel(v)}</Badge> },
  { key: 'titulo',            label: 'Título',        sortable: true },
  { key: 'jefe',              label: 'Jefe' },
  { key: 'clasificacionNombre', label: 'Clasificación' },
  {
    key: 'participantes',
    label: 'Participantes',
    render: ps => ps?.length > 0
      ? <Badge color="secondary" pill>{ps.length} participante{ps.length !== 1 ? 's' : ''}</Badge>
      : <span className="text-muted small">Sin participantes</span>,
  },
];

const FILTER_CONFIG = {
  search: { fields: ['titulo', 'jefe'], placeholder: 'Buscar por título o jefe...' },
  filters: [
    {
      key: 'tipo',
      label: 'Tipo',
      options: TIPOS.map(t => ({ value: t.value, label: t.label })),
      match: (item, val) => item.tipo === val,
    },
  ],
};

export default function MisProyectosPage() {
  const [misProyectos, setMisProyectos] = useState([]);
  const [todosProyectos, setTodosProyectos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [actionError, setActionError] = useState('');
  const [joinModalOpen, setJoinModalOpen] = useState(false);
  const [joining, setJoining] = useState(false);
  const [leaving, setLeaving] = useState(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await apiFetch('/api/Proyectos/participacion');
      setMisProyectos(data);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  const loadTodos = useCallback(async () => {
    try {
      const data = await apiFetch('/api/Proyectos/todos');
      setTodosProyectos(Array.isArray(data) ? data : []);
    } catch {
      setTodosProyectos([]);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openJoinModal = () => {
    loadTodos();
    setActionError('');
    setJoinModalOpen(true);
  };

  const handleJoin = async (proyecto) => {
    setJoining(true);
    setActionError('');
    try {
      await apiFetch(`/api/Proyectos/${proyecto.id}/participacion`, { method: 'POST' });
      setJoinModalOpen(false);
      await load();
    } catch (e) {
      setActionError(e.message);
    } finally {
      setJoining(false);
    }
  };

  const handleLeave = async (proyecto) => {
    if (!window.confirm(`¿Seguro que deseas salir del proyecto "${proyecto.titulo}"?`)) return;
    setLeaving(proyecto.id);
    setActionError('');
    try {
      await apiFetch(`/api/Proyectos/${proyecto.id}/participacion`, { method: 'DELETE' });
      await load();
    } catch (e) {
      setActionError(e.message);
    } finally {
      setLeaving(null);
    }
  };

  const misIds = new Set(misProyectos.map(p => p.id));
  const disponibles = todosProyectos.filter(p => !misIds.has(p.id));

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
        <Button color="primary" onClick={openJoinModal}>
          <i className="bi bi-plus-circle me-1" /> Unirse a un Proyecto
        </Button>
      </div>

      {error && <Alert color="danger" toggle={() => setError('')}>{error}</Alert>}
      {actionError && <Alert color="danger" toggle={() => setActionError('')}>{actionError}</Alert>}

      <Card>
        <CardHeader className="d-flex align-items-center gap-2">
          <strong>Proyectos en los que participas</strong>
          <Badge color="secondary" pill>{misProyectos.length}</Badge>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={FILTER_CONFIG}
            columns={COLUMNS}
            data={misProyectos}
            keyExtractor={item => item.id}
            actions={[
              {
                key: 'leave',
                label: 'Salir',
                icon: 'bi-box-arrow-right',
                color: 'outline-danger',
                onClick: handleLeave,
                disabled: item => leaving === item.id,
              },
            ]}
            emptyMessage="No participas en ningún proyecto."
            detailConfig
          />
        </CardBody>
      </Card>

      {/* Modal: unirse a un proyecto */}
      <Modal isOpen={joinModalOpen} toggle={() => setJoinModalOpen(false)} size="xl">
        <ModalHeader toggle={() => setJoinModalOpen(false)}>
          Unirse a un Proyecto
        </ModalHeader>
        <ModalBody>
          {actionError && <Alert color="danger">{actionError}</Alert>}
          <FilterableDataTable
            filterConfig={FILTER_CONFIG}
            columns={COLUMNS}
            data={disponibles}
            keyExtractor={p => p.id}
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
            emptyMessage="No hay proyectos disponibles para unirse."
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
