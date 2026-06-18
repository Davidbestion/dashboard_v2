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

const emptyForm = { nombre: '' };

export default function UniversidadesPage() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState(null); // null = create, object = edit
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await apiFetch('/api/Universidades');
      setItems(data);
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
    setForm({ nombre: item.nombre });
    setFormError('');
    setModal(true);
  }

  async function handleSave() {
    setSaving(true);
    setFormError('');
    try {
      if (editing) {
        await apiFetch(`/api/Universidades/${editing.id}`, {
          method: 'PUT',
          body: JSON.stringify({ nombre: form.nombre }),
        });
      } else {
        await apiFetch('/api/Universidades', {
          method: 'POST',
          body: JSON.stringify({ nombre: form.nombre }),
        });
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
    if (!window.confirm('¿Eliminar esta universidad?')) return;
    try {
      await apiFetch(`/api/Universidades/${id}`, { method: 'DELETE' });
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
        <h2 className="mb-0">Universidades</h2>
        <Button color="primary" onClick={openCreate}>+ Nueva universidad</Button>
      </div>

      {error && <Alert color="danger" toggle={() => setError('')}>{error}</Alert>}

      <Card>
        <CardHeader>
          <strong>Universidades registradas</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{ search: { fields: ['nombre'], placeholder: 'Buscar universidad...' } }}
            columns={[
              { key: 'nombre', label: 'Nombre', sortable: true },
            ]}
            data={items}
            keyExtractor={item => item.id}
            actions={[
              { key: 'edit',   label: 'Editar',   icon: 'bi-pencil', color: 'outline-secondary', onClick: item => openEdit(item) },
              { key: 'delete', label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',    onClick: item => handleDelete(item.id) },
            ]}
            emptyMessage="No hay universidades registradas."
            detailConfig
          />
        </CardBody>
      </Card>

      <Modal isOpen={modal} toggle={() => setModal(false)}>
        <ModalHeader toggle={() => setModal(false)}>
          {editing ? 'Editar universidad' : 'Nueva universidad'}
        </ModalHeader>
        <ModalBody>
          {formError && <Alert color="danger">{formError}</Alert>}
          <Form>
            <FormGroup>
              <Label for="nombre">Nombre</Label>
              <Input
                id="nombre"
                value={form.nombre}
                onChange={e => setForm({ nombre: e.target.value })}
                placeholder="Nombre de la universidad"
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
