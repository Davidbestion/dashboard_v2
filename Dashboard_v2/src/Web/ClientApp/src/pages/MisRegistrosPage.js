import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader, Spinner, Alert, Badge, Button,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input, InputGroup,
} from 'reactstrap';
import FilterableDataTable from '../components/FilterableDataTable';
import CoauthorPicker from '../components/CoauthorPicker';
import { useAuth } from '../contexts/AuthContext';

const EMPTY_FORM = { titulo: '', numeroCertificado: '', esInformatico: false, countryId: '', institutionId: '' };

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

export default function MisRegistrosPage() {
  const { user } = useAuth();
  const [items, setItems]         = useState([]);
  const [countries, setCountries] = useState([]);
  const [institutions, setInstitutions] = useState([]);
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
      const [regs, insts, cts] = await Promise.all([
        apiFetch('/api/Registros/mis'),
        apiFetch('/api/Institutions'),
        apiFetch('/api/Events/countries'),
      ]);
      setItems(regs); setInstitutions(insts); setCountries(cts);
    } catch (e) { setError(e.message); } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  function openCreate() {
    setEditing(null);
    setForm({ ...EMPTY_FORM, countryId: countries.length > 0 ? String(countries[0].id) : '' });
    setFormError(''); setShowNewInst(false); setCoauthorTags([]); setModal(true);
  }
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
    setForm({ titulo: item.titulo, numeroCertificado: item.numeroCertificado, esInformatico: item.esInformatico, countryId: String(item.countryId ?? ''), institutionId: item.institutionId ?? '' });
    const initialTags = (item.creadoresDetalle ?? [])
      .filter(c => c.userId !== user?.id)
      .map(mapCreatorToPickerEntry);
    setCoauthorTags(initialTags);
    setFormError(''); setShowNewInst(false); setModal(true);
  }
  async function handleSave(e) {
    if (e) e.preventDefault();
    setSaving(true); setFormError('');
    const body = {
      titulo: form.titulo,
      numeroCertificado: form.numeroCertificado,
      esInformatico: form.esInformatico,
      countryId: parseInt(form.countryId, 10),
      institutionId: form.institutionId,
      additionalAuthorIds: coauthorTags.filter(t => t.type === 'author').map(t => t.id),
      additionalAuthorNames: coauthorTags.filter(t => t.type === 'new').map(t => t.name),
      additionalUserIds: coauthorTags.filter(t => t.type === 'user').map(t => t.id),
    };
    try {
      if (editing) await apiFetch(`/api/Registros/${editing.id}`, { method: 'PUT', body: JSON.stringify(body) });
      else         await apiFetch('/api/Registros',               { method: 'POST', body: JSON.stringify(body) });
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
    try { await apiFetch(`/api/Registros/${toDelete.id}`, { method: 'DELETE' }); setDeleteModal(false); await load(); }
    catch (e) { setDeleteError(e.message); } finally { setDeleteLoading(false); }
  }

  if (loading) return <div className="d-flex justify-content-center mt-5"><Spinner color="primary" /></div>;

  const countryOpts      = [...new Set(items.map(i => i.countryName).filter(Boolean))].sort();
  const institutionOpts  = [...new Set(items.map(i => i.institutionNombre).filter(Boolean))].sort();

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="mb-0">Mis Registros</h2>
        <Button color="primary" onClick={openCreate}><i className="bi bi-plus-lg me-1" />Nuevo registro</Button>
      </div>
      {error && <Alert color="danger">{error}</Alert>}

      <Card>
        <CardHeader>
          <strong>Registros</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['titulo', 'numeroCertificado'], placeholder: 'Buscar registro...' },
              filters: [
                { key: 'countryName',       label: 'País',        options: countryOpts.map(v => ({ value: v, label: v })) },
                { key: 'institutionNombre', label: 'Institución', options: institutionOpts.map(v => ({ value: v, label: v })) },
              ],
            }}
            columns={[
              { key: 'titulo',            label: 'Título',      sortable: true },
              { key: 'numeroCertificado', label: 'Nº certif.' },
              { key: 'countryName',       label: 'País',        render: v => <Badge color="secondary" pill>{v}</Badge> },
              { key: 'institutionNombre', label: 'Institución' },
              { key: 'creadores',         label: 'Creadores',   render: v => (v ?? []).join(', ') },
            ]}
            data={items}
            keyExtractor={i => i.id}
            actions={[
              { key: 'edit',   label: 'Editar',   icon: 'bi-pencil', color: 'outline-secondary', onClick: i => openEdit(i) },
              { key: 'delete', label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',    onClick: i => openDelete(i) },
            ]}
            emptyMessage="No tienes registros registrados."
            detailConfig
          />
        </CardBody>
      </Card>

      {/* CRUD modal */}
      <Modal isOpen={modal} toggle={() => setModal(false)}>
        <ModalHeader toggle={() => setModal(false)}>{editing ? 'Editar registro' : 'Nuevo registro'}</ModalHeader>
        <Form onSubmit={handleSave}>
          <ModalBody>
            {formError && <Alert color="danger">{formError}</Alert>}
            <FormGroup>
              <Label>Título *</Label>
              <Input required value={form.titulo} onChange={e => setForm(f => ({ ...f, titulo: e.target.value }))} />
            </FormGroup>
            <FormGroup>
              <Label>Nº certificado *</Label>
              <Input required value={form.numeroCertificado} onChange={e => setForm(f => ({ ...f, numeroCertificado: e.target.value }))} />
            </FormGroup>
            <FormGroup check className="mb-3">
              <Input type="checkbox" id="esInformatico" checked={form.esInformatico} onChange={e => setForm(f => ({ ...f, esInformatico: e.target.checked }))} />
              <Label check for="esInformatico">Es informático</Label>
            </FormGroup>
            <FormGroup>
              <Label>País *</Label>
              <Input type="select" required value={form.countryId} onChange={e => setForm(f => ({ ...f, countryId: e.target.value }))}>
                <option value="">— Seleccionar —</option>
                {countries.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </Input>
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
          <p>¿Eliminar el registro <strong>{toDelete?.titulo}</strong>? Esta acción no se puede deshacer.</p>
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={() => setDeleteModal(false)}>Cancelar</Button>
          <Button color="danger" onClick={confirmDelete} disabled={deleteLoading}>{deleteLoading ? <Spinner size="sm" /> : 'Eliminar'}</Button>
        </ModalFooter>
      </Modal>
    </>
  );
}
