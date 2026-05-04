import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Button, Spinner, Alert, Badge,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input, FormText,
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

const emptyForm = { nombre: '', descripcion: '', universidadId: '', areasDelConocimientoIds: [] };

export default function AreasPage() {
  const [items, setItems] = useState([]);
  const [universidades, setUniversidades] = useState([]);
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
      const [areasData, univData, areasConocimientoData] = await Promise.all([
        apiFetch('/api/Areas'),
        apiFetch('/api/Universidades'),
        apiFetch('/api/AreasDelConocimiento'),
      ]);
      setItems(areasData);
      setUniversidades(univData);
      setAreasDelConocimiento(areasConocimientoData);
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
    setForm({
      nombre: item.nombre,
      descripcion: item.descripcion ?? '',
      universidadId: item.universidadId ?? '',
      areasDelConocimientoIds: item.areasDelConocimientoIds ?? [],
    });
    setFormError('');
    setModal(true);
  }

  async function handleSave() {
    setSaving(true);
    setFormError('');
    const body = {
      nombre: form.nombre,
      descripcion: form.descripcion || null,
      universidadId: form.universidadId || null,
      areasDelConocimientoIds: form.areasDelConocimientoIds,
    };
    try {
      if (editing) {
        await apiFetch(`/api/Areas/${editing.id}`, { method: 'PUT', body: JSON.stringify(body) });
      } else {
        await apiFetch('/api/Areas', { method: 'POST', body: JSON.stringify(body) });
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
    if (!window.confirm('¿Eliminar esta área? Se eliminarán también sus grupos de investigación.')) return;
    try {
      await apiFetch(`/api/Areas/${id}`, { method: 'DELETE' });
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
        <h2 className="mb-0">Áreas</h2>
        <Button color="primary" onClick={openCreate}>+ Nueva área</Button>
      </div>

      {error && <Alert color="danger" toggle={() => setError('')}>{error}</Alert>}

      <Card>
        <CardHeader>
          <strong>Áreas registradas</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['nombre', 'descripcion'], placeholder: 'Buscar área...' },
              filters: [
                { key: 'universidadNombre', label: 'Universidad',
                  options: universidades.map(u => ({ value: u.nombre, label: u.nombre })) },
              ],
            }}
            columns={[
              { key: 'nombre',           label: 'Nombre',      sortable: true, className: 'fw-semibold' },
              { key: 'descripcion',      label: 'Descripción', render: v => <span className="text-muted small">{v ?? '—'}</span> },
              { key: 'universidadNombre',label: 'Universidad',  render: v => v
                  ? <Badge color="info" pill>{v}</Badge>
                  : <span className="text-muted small">Sin universidad</span> },
            ]}
            data={items}
            keyExtractor={item => item.id}
            actions={[
              { key: 'edit',   label: 'Editar',   icon: 'bi-pencil', color: 'outline-secondary', onClick: item => openEdit(item) },
              { key: 'delete', label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',    onClick: item => handleDelete(item.id) },
            ]}
            emptyMessage="No hay áreas registradas."
          />
        </CardBody>
      </Card>

      <Modal isOpen={modal} toggle={() => setModal(false)}>
        <ModalHeader toggle={() => setModal(false)}>
          {editing ? 'Editar área' : 'Nueva área'}
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
                placeholder="Nombre del área"
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
              <Label for="universidadId">Universidad</Label>
              <Input
                id="universidadId"
                type="select"
                value={form.universidadId}
                onChange={e => setForm(f => ({ ...f, universidadId: e.target.value }))}
              >
                <option value="">— Sin universidad —</option>
                {universidades.map(u => (
                  <option key={u.id} value={u.id}>{u.nombre}</option>
                ))}
              </Input>
            </FormGroup>
            <FormGroup>
              <Label>Áreas del conocimiento que investiga</Label>
              {areasDelConocimiento.length === 0
                ? <FormText color="muted">No hay áreas del conocimiento registradas.</FormText>
                : <div className="border rounded p-2" style={{ maxHeight: '160px', overflowY: 'auto' }}>
                    {areasDelConocimiento.map(ac => (
                      <FormGroup check key={ac.id} className="mb-1">
                        <Input
                          type="checkbox"
                          id={`ac-${ac.id}`}
                          checked={form.areasDelConocimientoIds.includes(ac.id)}
                          onChange={e => {
                            const ids = e.target.checked
                              ? [...form.areasDelConocimientoIds, ac.id]
                              : form.areasDelConocimientoIds.filter(id => id !== ac.id);
                            setForm(f => ({ ...f, areasDelConocimientoIds: ids }));
                          }}
                        />
                        <Label check for={`ac-${ac.id}`}>{ac.nombre}</Label>
                      </FormGroup>
                    ))}
                  </div>
              }
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
