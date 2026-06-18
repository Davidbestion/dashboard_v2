import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader, Spinner, Alert, Button,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input, InputGroup,
} from 'reactstrap';
import FilterableDataTable from '../components/FilterableDataTable';
import CoauthorPicker from '../components/CoauthorPicker';
import { useAuth } from '../contexts/AuthContext';

const EMPTY_FORM = { titulo: '', tipoProductoComercializadoId: '', institutionId: '' };

async function apiFetch(url, options = {}) {
  const res = await fetch(url, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...(options.headers ?? {}) },
    ...options,
  });
  const data = await res.json().catch(() => null);
  if (!res.ok) {
    const errors = data?.errors ?? ['Error desconocido.'];
    throw new Error(Array.isArray(errors) ? errors.join(' ') : String(errors));
  }
  return data;
}

function NuevoTipoProductoInline({ onCreated }) {
  const [open, setOpen] = useState(false);
  const [value, setValue] = useState('');
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState('');

  async function handleCreate() {
    const nombre = value.trim();
    if (!nombre) return;
    setLoading(true); setErr('');
    try {
      const created = await apiFetch('/api/Nomencladores/tiposproducto', { method: 'POST', body: JSON.stringify({ nombre }) });
      onCreated(created);
      setValue(''); setOpen(false);
    } catch (e) { setErr(e.message); } finally { setLoading(false); }
  }

  if (!open) return (
    <Button type="button" color="secondary" outline size="sm" onClick={() => setOpen(true)}>
      <i className="bi bi-plus" /> Nuevo tipo
    </Button>
  );
  return (
    <div className="w-100 mt-1">
      {err && <Alert color="danger" className="py-1 px-2 small mb-1">{err}</Alert>}
      <InputGroup size="sm">
        <Input placeholder="Nombre del tipo..." value={value} autoFocus
          onChange={e => setValue(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); handleCreate(); } }} />
        <Button type="button" color="primary" onClick={handleCreate} disabled={loading}>
          {loading ? <Spinner size="sm" /> : 'Crear'}
        </Button>
        <Button type="button" color="secondary" outline onClick={() => { setOpen(false); setErr(''); }}>✕</Button>
      </InputGroup>
    </div>
  );
}

export default function MisProductosPage() {
  const { user } = useAuth();
  const [items, setItems]         = useState([]);
  const [institutions, setInstitutions] = useState([]);
  const [tipos, setTipos]         = useState([]);
  const [loading, setLoading]     = useState(true);
  const [error, setError]         = useState('');

  const [modal, setModal]         = useState(false);
  const [editing, setEditing]     = useState(null);
  const [form, setForm]           = useState(EMPTY_FORM);
  const [saving, setSaving]       = useState(false);
  const [formError, setFormError] = useState('');
  const [coauthorTags, setCoauthorTags] = useState([]);

  const [showNewInst, setShowNewInst]       = useState(false);
  const [newInstInput, setNewInstInput]     = useState('');
  const [newInstLoading, setNewInstLoading] = useState(false);
  const [newInstError, setNewInstError]     = useState('');

  const [deleteModal, setDeleteModal]     = useState(false);
  const [toDelete, setToDelete]           = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [deleteError, setDeleteError]     = useState('');

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try {
      const [ps, insts, ts] = await Promise.all([
        apiFetch('/api/ProductosComercializados/mis'),
        apiFetch('/api/Institutions'),
        apiFetch('/api/Nomencladores/tiposproducto'),
      ]);
      setItems(ps); setInstitutions(insts); setTipos(ts);
    } catch (e) { setError(e.message); } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  function openCreate() { setEditing(null); setForm(EMPTY_FORM); setFormError(''); setShowNewInst(false); setCoauthorTags([]); setModal(true); }
  function mapCreatorToPickerEntry(creator) {
    return {
      id: creator.id,
      name: creator.name,
      type: 'author',
      linkedUser: null,
    };
  }
  function openEdit(item) {
    setEditing(item);
    setForm({ titulo: item.titulo, tipoProductoComercializadoId: item.tipoProductoComercializadoId, institutionId: item.institutionId });
    const initialTags = (item.creadoresDetalle ?? [])
      .filter(c => c.userId !== user?.id)
      .map(mapCreatorToPickerEntry);
    setCoauthorTags(initialTags);
    setFormError('');
    setShowNewInst(false);
    setModal(true);
  }
  async function handleSave(e) {
    if (e) e.preventDefault();
    setSaving(true); setFormError('');
    const body = {
      ...form,
      additionalAuthorIds: coauthorTags.filter(t => t.type === 'author').map(t => t.id),
      additionalAuthorNames: coauthorTags.filter(t => t.type === 'new').map(t => t.name),
      additionalUserIds: coauthorTags.filter(t => t.type === 'user').map(t => t.id),
    };
    try {
      if (editing) await apiFetch(`/api/ProductosComercializados/${editing.id}`, { method: 'PUT', body: JSON.stringify(body) });
      else         await apiFetch('/api/ProductosComercializados',               { method: 'POST', body: JSON.stringify(body) });
      setModal(false); await load();
    } catch (e) { setFormError(e.message); } finally { setSaving(false); }
  }
  async function handleCreateInstitution() {
    const name = newInstInput.trim();
    if (!name) return;
    setNewInstLoading(true); setNewInstError('');
    try {
      const created = await apiFetch('/api/Institutions', { method: 'POST', body: JSON.stringify({ nombre: name }) });
      setInstitutions(prev => [...prev, created].sort((a, b) => a.nombre.localeCompare(b.nombre)));
      setForm(f => ({ ...f, institutionId: created.id }));
      setNewInstInput(''); setShowNewInst(false);
    } catch (e) { setNewInstError(e.message); } finally { setNewInstLoading(false); }
  }
  function openDelete(item) { setToDelete(item); setDeleteError(''); setDeleteModal(true); }
  async function confirmDelete() {
    setDeleteLoading(true); setDeleteError('');
    try { await apiFetch(`/api/ProductosComercializados/${toDelete.id}`, { method: 'DELETE' }); setDeleteModal(false); await load(); }
    catch (e) { setDeleteError(e.message); } finally { setDeleteLoading(false); }
  }

  if (loading) return <div className="d-flex justify-content-center mt-5"><Spinner color="primary" /></div>;

  const institutionOpts = [...new Set(items.map(i => i.institutionNombre).filter(Boolean))].sort();

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="mb-0">Mis Productos Comercializados</h2>
        <Button color="primary" onClick={openCreate}><i className="bi bi-plus-lg me-1" />Nuevo producto</Button>
      </div>
      {error && <Alert color="danger">{error}</Alert>}

      <Card>
        <CardHeader>
          <strong>Productos Comercializados</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['titulo'], placeholder: 'Buscar producto...' },
              filters: [
                { key: 'tipoProductoComercializadoNombre', label: 'Tipo',        options: tipos.map(t => ({ value: t.nombre, label: t.nombre })) },
                { key: 'institutionNombre',                label: 'Institución', options: institutionOpts.map(v => ({ value: v, label: v })) },
              ],
            }}
            columns={[
              { key: 'titulo',                           label: 'Título',      sortable: true },
              { key: 'tipoProductoComercializadoNombre', label: 'Tipo' },
              { key: 'institutionNombre',                label: 'Institución' },
              { key: 'creadores',                        label: 'Creadores',   render: v => (v ?? []).join(', ') },
            ]}
            data={items}
            keyExtractor={i => i.id}
            actions={[
              { key: 'edit',   label: 'Editar',   icon: 'bi-pencil', color: 'outline-secondary', onClick: i => openEdit(i) },
              { key: 'delete', label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',    onClick: i => openDelete(i) },
            ]}
            emptyMessage="No tienes productos comercializados registrados."
            detailConfig
          />
        </CardBody>
      </Card>

      {/* CRUD modal */}
      <Modal isOpen={modal} toggle={() => setModal(false)}>
        <ModalHeader toggle={() => setModal(false)}>{editing ? 'Editar producto' : 'Nuevo producto'}</ModalHeader>
        <Form onSubmit={handleSave}>
          <ModalBody>
            {formError && <Alert color="danger">{formError}</Alert>}
            <FormGroup>
              <Label>Título *</Label>
              <Input required value={form.titulo} onChange={e => setForm(f => ({ ...f, titulo: e.target.value }))} />
            </FormGroup>
            <FormGroup>
              <Label>Tipo de producto *</Label>
              <Input type="select" required value={form.tipoProductoComercializadoId} onChange={e => setForm(f => ({ ...f, tipoProductoComercializadoId: e.target.value }))}>
                <option value="">— Seleccionar —</option>
                {tipos.map(t => <option key={t.id} value={t.id}>{t.nombre}</option>)}
              </Input>
              <div className="mt-1">
                <NuevoTipoProductoInline
                  onCreated={t => {
                    setTipos(prev => [...prev, t].sort((a, b) => a.nombre.localeCompare(b.nombre)));
                    setForm(f => ({ ...f, tipoProductoComercializadoId: t.id }));
                  }}
                />
              </div>
            </FormGroup>
            <FormGroup>
              <Label>Institución *</Label>
              {showNewInst ? (
                <>
                  <InputGroup>
                    <Input placeholder="Nombre nueva institución" value={newInstInput} onChange={e => setNewInstInput(e.target.value)} />
                    <Button color="success" onClick={handleCreateInstitution} disabled={newInstLoading}>{newInstLoading ? <Spinner size="sm" /> : 'Crear'}</Button>
                    <Button color="secondary" onClick={() => setShowNewInst(false)}>Cancelar</Button>
                  </InputGroup>
                  {newInstError && <div className="text-danger small mt-1">{newInstError}</div>}
                </>
              ) : (
                <>
                  <Input type="select" required value={form.institutionId} onChange={e => setForm(f => ({ ...f, institutionId: e.target.value }))}>
                    <option value="">— Seleccionar —</option>
                    {institutions.map(i => <option key={i.id} value={i.id}>{i.nombre}</option>)}
                  </Input>
                  <Button color="link" className="p-0 mt-1 small" onClick={() => setShowNewInst(true)}>+ Nueva institución</Button>
                </>
              )}
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

      {/* Delete confirmation */}
      <Modal isOpen={deleteModal} toggle={() => setDeleteModal(false)}>
        <ModalHeader toggle={() => setDeleteModal(false)}>Confirmar eliminación</ModalHeader>
        <ModalBody>
          {deleteError && <Alert color="danger">{deleteError}</Alert>}
          <p>¿Eliminar el producto <strong>{toDelete?.titulo}</strong>? Esta acción no se puede deshacer.</p>
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={() => setDeleteModal(false)}>Cancelar</Button>
          <Button color="danger" onClick={confirmDelete} disabled={deleteLoading}>{deleteLoading ? <Spinner size="sm" /> : 'Eliminar'}</Button>
        </ModalFooter>
      </Modal>
    </>
  );
}
