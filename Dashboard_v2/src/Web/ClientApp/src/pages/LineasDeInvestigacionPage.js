import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Button, Spinner, Alert, Badge,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input,
} from 'reactstrap';
import Select from 'react-select';
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

const emptyForm = { nombre: '', descripcion: '', areasDelConocimientoIds: [] };

export default function LineasDeInvestigacionPage() {
  const [items, setItems] = useState([]);
  const [areasDelConocimiento, setAreasDelConocimiento] = useState([]);
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
      const [lineasData, areasData] = await Promise.all([
        apiFetch('/api/LineasDeInvestigacion'),
        apiFetch('/api/AreasDelConocimiento'),
      ]);
      setItems(lineasData);
      setAreasDelConocimiento(areasData);
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
    setForm({ nombre: item.nombre, descripcion: item.descripcion ?? '', areasDelConocimientoIds: item.areasDelConocimientoIds ?? [] });
    setFormError('');
    setModal(true);
  }

  async function handleSave() {
    setSaving(true);
    setFormError('');
    const body = {
      nombre: form.nombre,
      descripcion: form.descripcion || null,
      areasDelConocimientoIds: form.areasDelConocimientoIds,
    };
    try {
      if (editing) {
        await apiFetch(`/api/LineasDeInvestigacion/${editing.id}`, { method: 'PUT', body: JSON.stringify(body) });
      } else {
        await apiFetch('/api/LineasDeInvestigacion', { method: 'POST', body: JSON.stringify(body) });
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
    if (!window.confirm('¿Eliminar esta línea de investigación?')) return;
    try {
      await apiFetch(`/api/LineasDeInvestigacion/${id}`, { method: 'DELETE' });
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
        <h2 className="mb-0">Líneas de Investigación</h2>
        <Button color="primary" onClick={openCreate}>
          + Nueva línea
        </Button>
      </div>

      {error && <Alert color="danger" toggle={() => setError('')}>{error}</Alert>}

      <Card>
        <CardHeader>
          <strong>Líneas de investigación registradas</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['nombre', 'descripcion'], placeholder: 'Buscar línea...' },
              filters: [
                { key: 'areasDelConocimientoIds', label: 'Área del conocimiento',
                  options: areasDelConocimiento.map(a => ({ value: String(a.id), label: a.nombre })),
                  match: (item, val) => (item.areasDelConocimientoIds ?? []).map(String).includes(val) },
              ],
            }}
            columns={[
              { key: 'nombre',      label: 'Nombre',      sortable: true, className: 'fw-semibold' },
              { key: 'descripcion', label: 'Descripción', render: v => <span className="text-muted small">{v ?? '—'}</span> },
              {
                key: 'areasDelConocimientoIds',
                label: 'Áreas del conocimiento',
                render: ids => ids && ids.length > 0
                  ? areasDelConocimiento
                      .filter(a => ids.includes(a.id))
                      .map(a => <Badge color="secondary" pill className="me-1" key={a.id}>{a.nombre}</Badge>)
                  : <span className="text-muted small">Sin área asignada</span>,
              },
            ]}
            data={items}
            keyExtractor={item => item.id}
            actions={[
              { key: 'edit',   label: 'Editar',   icon: 'bi-pencil', color: 'outline-secondary', onClick: item => openEdit(item) },
              { key: 'delete', label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',    onClick: item => handleDelete(item.id) },
            ]}
            emptyMessage="No hay líneas de investigación registradas."
          />
        </CardBody>
      </Card>

      <Modal isOpen={modal} toggle={() => setModal(false)}>
        <ModalHeader toggle={() => setModal(false)}>
          {editing ? 'Editar línea de investigación' : 'Nueva línea de investigación'}
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
                placeholder="Nombre de la línea de investigación"
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
              <Label>Áreas del conocimiento</Label>
              <Select
                isMulti
                options={areasDelConocimiento.map(a => ({ value: a.id, label: a.nombre }))}
                value={form.areasDelConocimientoIds.map(id => {
                  const a = areasDelConocimiento.find(x => x.id === id);
                  return a ? { value: a.id, label: a.nombre } : null;
                }).filter(Boolean)}
                onChange={sel => setForm(f => ({ ...f, areasDelConocimientoIds: sel.map(s => s.value) }))}
                placeholder="Buscar área..."
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
