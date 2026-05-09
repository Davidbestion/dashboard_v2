import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Button, Spinner, Alert,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input,
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

export default function MisPatentesPage() {
  const [items, setItems]   = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError]   = useState('');

  // Modal proyectos
  const [proyModal, setProyModal]               = useState(false);
  const [selPatente, setSelPatente]             = useState(null);
  const [proyList, setProyList]                 = useState([]);
  const [allProyectos, setAllProyectos]         = useState([]);
  const [selProyectoId, setSelProyectoId]       = useState('');
  const [proyLoading, setProyLoading]           = useState(false);
  const [proyError, setProyError]               = useState('');

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try { setItems(await apiFetch('/api/Patentes/mis')); }
    catch (e) { setError(e.message); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openProyectos = async (patente) => {
    setSelPatente(patente);
    setProyError(''); setSelProyectoId('');
    setProyLoading(true); setProyModal(true);
    try {
      const [linked, all] = await Promise.all([
        apiFetch(`/api/Patentes/${patente.id}/proyectos`),
        apiFetch('/api/Proyectos'),
      ]);
      setProyList(linked);
      setAllProyectos(all);
    } catch (e) { setProyError(e.message); }
    finally { setProyLoading(false); }
  };

  const handleLink = async () => {
    if (!selProyectoId) return;
    try {
      await apiFetch(`/api/Patentes/${selPatente.id}/proyectos/${selProyectoId}`, { method: 'POST' });
      setProyList(await apiFetch(`/api/Patentes/${selPatente.id}/proyectos`));
      setSelProyectoId('');
    } catch (e) { setProyError(e.message); }
  };

  const handleUnlink = async (proyectoId) => {
    try {
      await apiFetch(`/api/Patentes/${selPatente.id}/proyectos/${proyectoId}`, { method: 'DELETE' });
      setProyList(prev => prev.filter(p => p.proyectoId !== proyectoId));
    } catch (e) { setProyError(e.message); }
  };

  const linkedIds      = new Set(proyList.map(p => p.proyectoId));
  const availableProys = allProyectos.filter(p => !linkedIds.has(p.id));

  if (loading) return <div className="d-flex justify-content-center mt-5"><Spinner color="primary" /></div>;

  return (
    <>
      <h2 className="mb-4">Mis Patentes</h2>
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
              { key: 'titulo',                   label: 'Título',          sortable: true },
              { key: 'numeroSolicitudConcesion', label: 'Nº solicitud' },
              { key: 'esNacional',               label: 'Tipo',    render: v => v ? 'Nacional' : 'Internacional' },
              { key: 'creadores',                label: 'Creadores', render: v => (v ?? []).join(', ') },
            ]}
            data={items}
            keyExtractor={i => i.id}
            actions={[
              {
                key: 'proyectos', label: 'Proyectos', icon: 'bi-kanban', color: 'outline-info',
                onClick: i => openProyectos(i),
              },
            ]}
            emptyMessage="No tienes patentes registradas."
          />
        </CardBody>
      </Card>

      {/* Modal proyectos de la patente */}
      <Modal isOpen={proyModal} toggle={() => setProyModal(false)} size="lg">
        <ModalHeader toggle={() => setProyModal(false)}>
          Proyectos — {selPatente?.titulo}
        </ModalHeader>
        <ModalBody>
          {proyError && <Alert color="danger">{proyError}</Alert>}
          {proyLoading ? <div className="text-center"><Spinner /></div> : (
            <>
              <h6>Proyectos vinculados</h6>
              {proyList.length === 0
                ? <p className="text-muted">Ninguno.</p>
                : (
                  <ul className="list-group mb-3">
                    {proyList.map(p => (
                      <li key={p.proyectoId} className="list-group-item d-flex justify-content-between align-items-center">
                        {p.proyectoTitulo}
                        <Button size="sm" color="outline-danger" onClick={() => handleUnlink(p.proyectoId)}>
                          <i className="bi bi-x-lg" />
                        </Button>
                      </li>
                    ))}
                  </ul>
                )
              }

              <h6>Vincular proyecto</h6>
              <Form className="d-flex gap-2">
                <FormGroup className="flex-grow-1 mb-0">
                  <Input
                    type="select"
                    value={selProyectoId}
                    onChange={e => setSelProyectoId(e.target.value)}
                  >
                    <option value="">— Seleccionar proyecto —</option>
                    {availableProys.map(p => (
                      <option key={p.id} value={p.id}>{p.titulo}</option>
                    ))}
                  </Input>
                </FormGroup>
                <Button color="primary" onClick={handleLink} disabled={!selProyectoId}>
                  Vincular
                </Button>
              </Form>
            </>
          )}
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={() => setProyModal(false)}>Cerrar</Button>
        </ModalFooter>
      </Modal>
    </>
  );
}
