import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Button, Spinner, Alert,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input,
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

export default function ClasificacionesPage() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [nombre, setNombre] = useState('');
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      setItems(await apiFetch('/api/Clasificaciones'));
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  function openCreate() {
    setEditing(null);
    setNombre('');
    setFormError('');
    setModal(true);
  }

  function openEdit(item) {
    setEditing(item);
    setNombre(item.nombre);
    setFormError('');
    setModal(true);
  }

  async function handleSave() {
    setSaving(true);
    setFormError('');
    try {
      if (editing) {
        await apiFetch(`/api/Clasificaciones/${editing.id}`, { method: 'PUT', body: JSON.stringify({ nombre }) });
      } else {
        await apiFetch('/api/Clasificaciones', { method: 'POST', body: JSON.stringify({ nombre }) });
      }
      setModal(false);
      await load();
    } catch (e) {
      setFormError(e.message);
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await apiFetch(`/api/Clasificaciones/${deleteTarget.id}`, { method: 'DELETE' });
      setDeleteTarget(null);
      await load();
    } catch (e) {
      setError(e.message);
    } finally {
      setDeleting(false);
    }
  }

  return (
    <>
      <Card>
        <CardHeader className="d-flex justify-content-between align-items-center">
          <strong>Clasificaciones de Proyectos</strong>
          <Button color="primary" size="sm" onClick={openCreate}>+ Nueva clasificación</Button>
        </CardHeader>
        <CardBody>
          {error && <Alert color="danger">{error}</Alert>}
          {loading ? (
            <div className="text-center py-4"><Spinner /></div>
          ) : (
            <FilterableDataTable
              filterConfig={{ search: { fields: ['nombre'], placeholder: 'Buscar clasificación...' } }}
              columns={[
                { key: 'nombre', label: 'Nombre', sortable: true },
              ]}
              data={items}
              keyExtractor={item => item.id}
              actions={[
                { key: 'edit',   label: 'Editar',   icon: 'bi-pencil', color: 'outline-secondary', onClick: item => openEdit(item) },
                { key: 'delete', label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',    onClick: item => setDeleteTarget(item) },
              ]}
              emptyMessage="No hay clasificaciones registradas."
            />
          )}
        </CardBody>
      </Card>

      <Modal isOpen={modal} toggle={() => setModal(false)}>
        <ModalHeader toggle={() => setModal(false)}>
          {editing ? 'Editar clasificación' : 'Nueva clasificación'}
        </ModalHeader>
        <ModalBody>
          {formError && <Alert color="danger">{formError}</Alert>}
          <Form>
            <FormGroup>
              <Label>Nombre *</Label>
              <Input value={nombre} onChange={e => setNombre(e.target.value)} placeholder="Nombre de la clasificación" />
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

      <Modal isOpen={!!deleteTarget} toggle={() => setDeleteTarget(null)}>
        <ModalHeader toggle={() => setDeleteTarget(null)}>Confirmar eliminación</ModalHeader>
        <ModalBody>
          ¿Eliminar la clasificación <strong>«{deleteTarget?.nombre}»</strong>?
          No se puede eliminar si tiene proyectos asociados.
        </ModalBody>
        <ModalFooter>
          <Button color="danger" onClick={handleDelete} disabled={deleting}>
            {deleting ? <Spinner size="sm" /> : 'Eliminar'}
          </Button>
          <Button color="secondary" onClick={() => setDeleteTarget(null)}>Cancelar</Button>
        </ModalFooter>
      </Modal>
    </>
  );
}
