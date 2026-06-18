import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Button, Spinner, Alert,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input,
} from 'reactstrap';
import FilterableDataTable from '../components/FilterableDataTable';
import CoauthorPicker from '../components/CoauthorPicker';
import { useAuth } from '../contexts/AuthContext';

const EMPTY_FORM = { titulo: '', numeroSolicitudConcesion: '', esNacional: true };

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
  const { user } = useAuth();
  const [items, setItems]   = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError]   = useState('');

  // CRUD modal
  const [modal, setModal]     = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm]       = useState(EMPTY_FORM);
  const [saving, setSaving]   = useState(false);
  const [formError, setFormError] = useState('');
  const [coauthorTags, setCoauthorTags] = useState([]);

  // Delete confirmation
  const [deleteModal, setDeleteModal]   = useState(false);
  const [toDelete, setToDelete]         = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [deleteError, setDeleteError]   = useState('');

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

  // --- CRUD handlers ---
  function openCreate() {
    setEditing(null); setForm(EMPTY_FORM); setFormError(''); setCoauthorTags([]); setModal(true);
  }
  function mapCreatorToPickerEntry(creator) {
    return {
      id: creator.id,
      name: creator.name,
      type: 'author',
      linkedUser: creator.userId ? { id: creator.userId } : null,
    };
  }
  function openEdit(item) {
    setEditing(item);
    setForm({ titulo: item.titulo, numeroSolicitudConcesion: item.numeroSolicitudConcesion, esNacional: item.esNacional });
    const initialTags = (item.creadoresDetalle ?? [])
      .filter(c => c.userId !== user?.id)
      .map(mapCreatorToPickerEntry);
    setCoauthorTags(initialTags);
    setFormError(''); setModal(true);
  }
  async function handleSave(e) {
    if (e) e.preventDefault();
    setSaving(true); setFormError('');
    const body = {
      titulo: form.titulo,
      numeroSolicitudConcesion: form.numeroSolicitudConcesion,
      esNacional: form.esNacional,
      additionalAuthorIds: coauthorTags.filter(t => t.type === 'author').map(t => t.id),
      additionalAuthorNames: coauthorTags.filter(t => t.type === 'new').map(t => t.name),
      additionalUserIds: coauthorTags.filter(t => t.type === 'user').map(t => t.id),
    };
    try {
      if (editing) await apiFetch(`/api/Patentes/${editing.id}`, { method: 'PUT', body: JSON.stringify(body) });
      else         await apiFetch('/api/Patentes',               { method: 'POST', body: JSON.stringify(body) });
      setModal(false); await load();
    } catch (e) { setFormError(e.message); } finally { setSaving(false); }
  }

  function openDelete(item) { setToDelete(item); setDeleteError(''); setDeleteModal(true); }
  async function confirmDelete() {
    setDeleteLoading(true); setDeleteError('');
    try {
      await apiFetch(`/api/Patentes/${toDelete.id}`, { method: 'DELETE' });
      setDeleteModal(false); await load();
    } catch (e) { setDeleteError(e.message); } finally { setDeleteLoading(false); }
  }

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
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="mb-0">Mis Patentes</h2>
        <Button color="primary" onClick={openCreate}><i className="bi bi-plus-lg me-1" />Nueva patente</Button>
      </div>
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
              { key: 'titulo',                   label: 'Título',     sortable: true },
              { key: 'numeroSolicitudConcesion', label: 'Nº solicitud' },
              { key: 'esNacional',               label: 'Tipo',       render: v => v ? 'Nacional' : 'Internacional' },
              { key: 'creadores',                label: 'Creadores',  render: v => (v ?? []).join(', ') },
            ]}
            data={items}
            keyExtractor={i => i.id}
            actions={[
              { key: 'proyectos', label: 'Proyectos', icon: 'bi-kanban',    color: 'outline-info',    onClick: i => openProyectos(i) },
              { key: 'edit',      label: 'Editar',    icon: 'bi-pencil',    color: 'outline-secondary', onClick: i => openEdit(i) },
              { key: 'delete',    label: 'Eliminar',  icon: 'bi-trash',     color: 'outline-danger',  onClick: i => openDelete(i) },
            ]}
            emptyMessage="No tienes patentes registradas."
            detailConfig
          />
        </CardBody>
      </Card>

      {/* CRUD modal */}
      <Modal isOpen={modal} toggle={() => setModal(false)}>
        <ModalHeader toggle={() => setModal(false)}>
          {editing ? 'Editar patente' : 'Nueva patente'}
        </ModalHeader>
        <Form onSubmit={handleSave}>
          <ModalBody>
            {formError && <Alert color="danger">{formError}</Alert>}
            <FormGroup>
              <Label>Título *</Label>
              <Input required value={form.titulo} onChange={e => setForm(f => ({ ...f, titulo: e.target.value }))} />
            </FormGroup>
            <FormGroup>
              <Label>Nº solicitud/concesión *</Label>
              <Input required value={form.numeroSolicitudConcesion} onChange={e => setForm(f => ({ ...f, numeroSolicitudConcesion: e.target.value }))} />
            </FormGroup>
            <FormGroup>
              <Label>Tipo</Label>
              <Input type="select" value={String(form.esNacional)} onChange={e => setForm(f => ({ ...f, esNacional: e.target.value === 'true' }))}>
                <option value="true">Nacional</option>
                <option value="false">Internacional</option>
              </Input>
            </FormGroup>
            <FormGroup>
              <Label>Creadores</Label>
              <CoauthorPicker value={coauthorTags} onChange={setCoauthorTags} />
            </FormGroup>
          </ModalBody>
          <ModalFooter>
            <Button color="secondary" type="button" onClick={() => setModal(false)}>Cancelar</Button>
            <Button color="primary" type="submit" disabled={saving}>{saving ? <Spinner size="sm" /> : 'Guardar'}</Button>
          </ModalFooter>
        </Form>
      </Modal>

      {/* Delete confirmation modal */}
      <Modal isOpen={deleteModal} toggle={() => setDeleteModal(false)}>
        <ModalHeader toggle={() => setDeleteModal(false)}>Confirmar eliminación</ModalHeader>
        <ModalBody>
          {deleteError && <Alert color="danger">{deleteError}</Alert>}
          <p>¿Eliminar la patente <strong>{toDelete?.titulo}</strong>? Esta acción no se puede deshacer.</p>
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={() => setDeleteModal(false)}>Cancelar</Button>
          <Button color="danger" onClick={confirmDelete} disabled={deleteLoading}>{deleteLoading ? <Spinner size="sm" /> : 'Eliminar'}</Button>
        </ModalFooter>
      </Modal>

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
