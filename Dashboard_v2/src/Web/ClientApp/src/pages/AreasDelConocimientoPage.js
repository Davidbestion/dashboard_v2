import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Button, Spinner, Alert, Badge,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input,
} from 'reactstrap';
import Select from 'react-select';
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

const emptyForm = { nombre: '', descripcion: '', lineasDeInvestigacionIds: [] };

export default function AreasDelConocimientoPage() {
  const [items, setItems] = useState([]);
  const [lineasDeInvestigacion, setLineasDeInvestigacion] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [areasData, lineasData] = await Promise.all([
        apiFetch('/api/AreasDelConocimiento'),
        apiFetch('/api/LineasDeInvestigacion'),
      ]);
      setItems(areasData);
      setLineasDeInvestigacion(lineasData);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setFormError('');
    setModal(true);
  }

  function openEdit(item) {
    setEditing(item);
    setForm({ nombre: item.nombre, descripcion: item.descripcion ?? '', lineasDeInvestigacionIds: item.lineasDeInvestigacionIds ?? [] });
    setFormError('');
    setModal(true);
  }

  async function handleSave() {
    setSaving(true);
    setFormError('');
    const body = { nombre: form.nombre, descripcion: form.descripcion || null, lineasDeInvestigacionIds: form.lineasDeInvestigacionIds };
    try {
      if (editing) {
        await apiFetch(`/api/AreasDelConocimiento/${editing.id}`, { method: 'PUT', body: JSON.stringify(body) });
      } else {
        await apiFetch('/api/AreasDelConocimiento', { method: 'POST', body: JSON.stringify(body) });
      }
      setModal(false);
      await load();
    } catch (e) {
      setFormError(e.message);
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id) {
    if (!window.confirm('¿Eliminar esta área del conocimiento? Se eliminarán también sus líneas de investigación.')) return;
    try {
      await apiFetch(`/api/AreasDelConocimiento/${id}`, { method: 'DELETE' });
      await load();
    } catch (e) {
      setError(e.message);
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
        <h2 className="mb-0">Áreas del Conocimiento</h2>
        <Button color="primary" onClick={openCreate}>+ Nueva área del conocimiento</Button>
      </div>

      {error && <Alert color="danger" toggle={() => setError('')}>{error}</Alert>}

      <Card>
        <CardHeader>
          <strong>Áreas del conocimiento registradas</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['nombre', 'descripcion'], placeholder: 'Buscar área del conocimiento...' },
              filters: [
                { key: 'lineasDeInvestigacionIds', label: 'Línea de investigación',
                  options: lineasDeInvestigacion.map(l => ({ value: String(l.id), label: l.nombre })),
                  match: (item, val) => (item.lineasDeInvestigacionIds ?? []).map(String).includes(val) },
              ],
            }}
            columns={[
              { key: 'nombre',      label: 'Nombre',      sortable: true, className: 'fw-semibold' },
              { key: 'descripcion', label: 'Descripción', render: v => <span className="text-muted small">{v ?? '—'}</span> },
              {
                key: 'lineasDeInvestigacionIds',
                label: 'Líneas de investigación',
                render: (ids, item) => ids && ids.length > 0
                  ? lineasDeInvestigacion
                      .filter(l => ids.includes(l.id))
                      .map(l => <Badge color="info" pill className="me-1" key={l.id}>{l.nombre}</Badge>)
                  : <span className="text-muted small">Sin líneas</span>,
              },
            ]}
            data={items}
            keyExtractor={item => item.id}
            actions={[
              { key: 'edit',   label: 'Editar',   icon: 'bi-pencil', color: 'outline-secondary', onClick: item => openEdit(item) },
              { key: 'delete', label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',    onClick: item => handleDelete(item.id) },
            ]}
            emptyMessage="No hay áreas del conocimiento registradas."
            detailConfig
          />
        </CardBody>
      </Card>

      <Modal isOpen={modal} toggle={() => setModal(false)}>
        <ModalHeader toggle={() => setModal(false)}>
          {editing ? 'Editar área del conocimiento' : 'Nueva área del conocimiento'}
        </ModalHeader>
        <ModalBody>
          {formError && <Alert color="danger">{formError}</Alert>}
          <Form>
            <FormGroup>
              <Label for="nombre">Nombre *</Label>
              <Input
                id="nombre"
                value={form.nombre}
                onChange={e => setForm(f => ({ ...f, nombre: e.target.value }))}
                placeholder="Nombre del área del conocimiento"
              />
            </FormGroup>
            <FormGroup>
              <Label for="descripcion">Descripción</Label>
              <Input
                id="descripcion"
                type="textarea"
                rows={3}
                value={form.descripcion}
                onChange={e => setForm(f => ({ ...f, descripcion: e.target.value }))}
                placeholder="Descripción opcional"
              />
            </FormGroup>
            <FormGroup>
              <Label>Líneas de investigación que posee</Label>
              <Select
                isMulti
                options={lineasDeInvestigacion.map(l => ({ value: l.id, label: l.nombre }))}
                value={form.lineasDeInvestigacionIds.map(id => {
                  const l = lineasDeInvestigacion.find(x => x.id === id);
                  return l ? { value: l.id, label: l.nombre } : null;
                }).filter(Boolean)}
                onChange={sel => setForm(f => ({ ...f, lineasDeInvestigacionIds: sel.map(s => s.value) }))}
                placeholder="Buscar línea..."
                noOptionsMessage={() => 'Sin resultados'}
                menuPortalTarget={document.body}
                styles={{ menuPortal: base => ({ ...base, zIndex: 9999 }) }}
              />
            </FormGroup>
          </Form>
        </ModalBody>
        <ModalFooter>
          <Button color="primary" onClick={handleSave} disabled={saving}>
            {saving ? <Spinner size="sm" /> : 'Guardar'}
          </Button>
          <Button color="secondary" onClick={() => setModal(false)}>Cancelar</Button>
        </ModalFooter>
      </Modal>
    </>
  );
}
