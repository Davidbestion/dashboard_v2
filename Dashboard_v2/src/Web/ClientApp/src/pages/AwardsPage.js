import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Table, Button, Badge,
  Spinner, Alert,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input,
} from 'reactstrap';
import { useAuth } from '../contexts/AuthContext';

async function apiFetch(url, options = {}) {
  const response = await fetch(url, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...(options.headers ?? {}) },
    ...options,
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    let message;
    if (data?.errors) {
      const errs = Array.isArray(data.errors)
        ? data.errors
        : Object.values(data.errors).flat();
      message = errs.join(' ');
    } else if (data?.title) {
      message = data.title;
    } else {
      message = `Error ${response.status}`;
    }
    throw new Error(message);
  }
  return data;
}

const AWARD_TYPES = [
  { value: 0, label: 'Premio Academia de Ciencias' },
  { value: 1, label: 'Premio MES' },
  { value: 2, label: 'Premio CITMA Innovación Tecnológica' },
  { value: 3, label: 'Premio CITMA Jóvenes Investigadores' },
  { value: 4, label: 'Premio Forum Ciencia y Técnica' },
  { value: 5, label: 'Premio Investigación UH' },
  { value: 6, label: 'Otros premios' },
  { value: 7, label: 'Premio Internacional' },
];

function awardTypeLabel(value) {
  return AWARD_TYPES.find(t => t.value === value)?.label ?? 'Desconocido';
}

const EMPTY_FORM = {
  awardName: '',
  awardType: '0',
  year: new Date().getFullYear().toString(),
  awardedAt: new Date().toISOString().slice(0, 10),
};

export default function AwardsPage() {
  const { user } = useAuth();
  const isSuperuser = user?.role === 'Superuser';
  const [awards, setAwards] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [generatingAnexo, setGeneratingAnexo] = useState(false);
  const [anexoError, setAnexoError] = useState('');

  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState(EMPTY_FORM);
  const [formError, setFormError] = useState('');
  const [formLoading, setFormLoading] = useState(false);

  const [deleteModal, setDeleteModal] = useState(false);
  const [toDelete, setToDelete] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [deleteError, setDeleteError] = useState('');

  const loadAwards = useCallback(async () => {
    if (isSuperuser) {
      setAwards([]);
      setLoading(false);
      setError('');
      return;
    }

    setLoading(true);
    setError('');
    try {
      const data = await apiFetch('/api/Awards');
      setAwards(data);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, [isSuperuser]);

  useEffect(() => { loadAwards(); }, [loadAwards]);

  async function handleGenerateAnexo() {
    setGeneratingAnexo(true);
    setAnexoError('');
    try {
      const response = await fetch('/api/Documents/anexo-premios', { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => ({}));
        const message = data?.error ?? data?.title ?? 'No se pudo generar el anexo.';
        throw new Error(message);
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'anexo-premios.xlsx';
      document.body.appendChild(a);
      a.click();
      a.remove();
      window.URL.revokeObjectURL(url);
    } catch (e) {
      setAnexoError(e.message);
    } finally {
      setGeneratingAnexo(false);
    }
  }

  function openCreate() {
    setEditing(null);
    setForm(EMPTY_FORM);
    setFormError('');
    setModal(true);
  }

  function openEdit(award) {
    setEditing(award);
    setForm({
      awardName: award.awardName,
      awardType: String(award.awardTypeId ?? award.awardType ?? '0'),
      year: award.year?.toString() ?? '',
      awardedAt: (award.awardedAt ?? '').slice(0, 10),
    });
    setFormError('');
    setModal(true);
  }

  function closeModal() {
    setModal(false);
    setEditing(null);
  }

  function handleChange(e) {
    const { name, value } = e.target;
    setForm(f => ({ ...f, [name]: value }));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setFormLoading(true);
    setFormError('');
    const body = {
      awardName: form.awardName.trim(),
      awardType: parseInt(form.awardType, 10),
      year: parseInt(form.year, 10),
      awardedAt: new Date(form.awardedAt).toISOString(),
    };
    try {
      if (editing) {
        await apiFetch(`/api/Awards/${editing.id}`, {
          method: 'PUT',
          body: JSON.stringify(body),
        });
      } else {
        await apiFetch('/api/Awards', {
          method: 'POST',
          body: JSON.stringify(body),
        });
      }
      closeModal();
      loadAwards();
    } catch (e) {
      setFormError(e.message);
    } finally {
      setFormLoading(false);
    }
  }

  function openDelete(award) {
    setToDelete(award);
    setDeleteError('');
    setDeleteModal(true);
  }

  async function confirmDelete() {
    if (!toDelete) return;
    setDeleteLoading(true);
    setDeleteError('');
    try {
      await apiFetch(`/api/Awards/${toDelete.id}`, { method: 'DELETE' });
      setDeleteModal(false);
      setToDelete(null);
      loadAwards();
    } catch (e) {
      setDeleteError(e.message);
    } finally {
      setDeleteLoading(false);
    }
  }

  return (
    <>
      <Card>
        <CardHeader className="d-flex justify-content-between align-items-center">
          <span className="fw-semibold">{isSuperuser ? 'Premios' : 'Mis premios'}</span>
          {isSuperuser ? (
            <Button color="success" size="sm" onClick={handleGenerateAnexo} disabled={generatingAnexo}>
              {generatingAnexo ? <Spinner size="sm" /> : '⬇ Generar Anexo 5'}
            </Button>
          ) : (
            <Button color="primary" size="sm" onClick={openCreate}>
              <i className="bi bi-plus-lg me-1" />
              Nuevo premio
            </Button>
          )}
        </CardHeader>

        <CardBody>
          {anexoError && <Alert color="danger">{anexoError}</Alert>}

          {isSuperuser && (
            <Alert color="info">
              El anexo de premios se genera desde esta vista. El listado editable de premios sigue disponible solo para usuarios con rol Profesor.
            </Alert>
          )}

          {loading && (
            <div className="text-center py-4">
              <Spinner color="primary" />
            </div>
          )}

          {!loading && error && <Alert color="danger">{error}</Alert>}

          {!isSuperuser && !loading && !error && awards.length === 0 && (
            <p className="text-muted text-center py-3">No tienes premios registrados.</p>
          )}

          {!isSuperuser && !loading && !error && awards.length > 0 && (
            <Table responsive hover>
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th>Tipo</th>
                  <th>Año</th>
                  <th>Fecha de otorgamiento</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {awards.map(a => (
                  <tr key={a.id}>
                    <td>{a.awardName}</td>
                    <td>
                      <Badge color="info" pill>{a.awardTypeId != null ? awardTypeLabel(a.awardTypeId) : (a.awardTypeName ?? 'Desconocido')}</Badge>
                    </td>
                    <td>{a.year}</td>
                    <td>{new Date(a.awardedAt).toLocaleDateString('es-CU')}</td>
                    <td className="text-end">
                      <Button
                        color="outline-secondary"
                        size="sm"
                        className="me-2"
                        onClick={() => openEdit(a)}
                      >
                        <i className="bi bi-pencil" />
                      </Button>
                      <Button
                        color="outline-danger"
                        size="sm"
                        onClick={() => openDelete(a)}
                      >
                        <i className="bi bi-trash" />
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </Table>
          )}
        </CardBody>
      </Card>

      {/* Modal crear / editar */}
      <Modal isOpen={modal} toggle={closeModal}>
        <Form onSubmit={handleSubmit}>
          <ModalHeader toggle={closeModal}>
            {editing ? 'Editar premio' : 'Registrar nuevo premio'}
          </ModalHeader>

          <ModalBody>
            {formError && <Alert color="danger">{formError}</Alert>}

            <FormGroup>
              <Label for="awardName">Nombre del premio *</Label>
              <Input
                id="awardName"
                name="awardName"
                value={form.awardName}
                onChange={handleChange}
                required
                placeholder="Nombre oficial del premio"
              />
            </FormGroup>

            <FormGroup>
              <Label for="awardType">Tipo de premio *</Label>
              <Input
                type="select"
                id="awardType"
                name="awardType"
                value={form.awardType}
                onChange={handleChange}
              >
                {AWARD_TYPES.map(t => (
                  <option key={t.value} value={t.value}>{t.label}</option>
                ))}
              </Input>
            </FormGroup>

            <FormGroup>
              <Label for="year">Año *</Label>
              <Input
                type="number"
                id="year"
                name="year"
                value={form.year}
                onChange={handleChange}
                min="1900"
                max={new Date().getFullYear() + 1}
                required
              />
            </FormGroup>

            <FormGroup>
              <Label for="awardedAt">Fecha de otorgamiento *</Label>
              <Input
                type="date"
                id="awardedAt"
                name="awardedAt"
                value={form.awardedAt}
                onChange={handleChange}
                required
              />
            </FormGroup>
          </ModalBody>

          <ModalFooter>
            <Button color="secondary" onClick={closeModal} disabled={formLoading}>
              Cancelar
            </Button>
            <Button color="primary" type="submit" disabled={formLoading}>
              {formLoading ? <Spinner size="sm" /> : (editing ? 'Guardar cambios' : 'Registrar')}
            </Button>
          </ModalFooter>
        </Form>
      </Modal>

      {/* Modal confirmar borrado */}
      <Modal isOpen={deleteModal} toggle={() => setDeleteModal(false)}>
        <ModalHeader toggle={() => setDeleteModal(false)}>Eliminar premio</ModalHeader>
        <ModalBody>
          {deleteError && <Alert color="danger">{deleteError}</Alert>}
          <p>
            ¿Seguro que deseas eliminar <strong>{toDelete?.awardName}</strong>?
            Esta acción no se puede deshacer.
          </p>
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={() => setDeleteModal(false)} disabled={deleteLoading}>
            Cancelar
          </Button>
          <Button color="danger" onClick={confirmDelete} disabled={deleteLoading}>
            {deleteLoading ? <Spinner size="sm" /> : 'Eliminar'}
          </Button>
        </ModalFooter>
      </Modal>
    </>
  );
}
