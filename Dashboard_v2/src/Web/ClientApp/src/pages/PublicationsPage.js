import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Table, Button, Badge,
  Spinner, Alert,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input,
} from 'reactstrap';

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
      // { errors: string[] }  — nuestro formato
      // { errors: { Field: string[] } } — ProblemDetails con validación
      const errs = Array.isArray(data.errors)
        ? data.errors
        : Object.values(data.errors).flat();
      message = errs.join(' ');
    } else if (data?.title) {
      // ProblemDetails estándar de ASP.NET Core
      message = data.title;
    } else {
      message = `Error ${response.status}`;
    }
    throw new Error(message);
  }
  return data;
}

const EMPTY_FORM = {
  title: '',
  publicationData: '',
  urlDoi: '',
  publicationTypeId: '',
  additionalAuthorNames: '',
};

export default function PublicationsPage() {
  const [publications, setPublications] = useState([]);
  const [types, setTypes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Modal de crear / editar
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState(null); // null = crear, objeto = editar
  const [form, setForm] = useState(EMPTY_FORM);
  const [formError, setFormError] = useState('');
  const [formLoading, setFormLoading] = useState(false);

  // Modal de confirmación de borrado
  const [deleteModal, setDeleteModal] = useState(false);
  const [toDelete, setToDelete] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [deleteError, setDeleteError] = useState('');
  // Creación inline de nuevo tipo
  const [newTypeName, setNewTypeName] = useState('');
  const [showNewType, setShowNewType] = useState(false);
  const [newTypeLoading, setNewTypeLoading] = useState(false);
  const [newTypeError, setNewTypeError] = useState('');
  const loadData = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [pubs, pubTypes] = await Promise.all([
        apiFetch('/api/Publications'),
        apiFetch('/api/Publications/types'),
      ]);
      setPublications(pubs);
      setTypes(pubTypes);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadData(); }, [loadData]);

  // ── helpers de formulario ──────────────────────────────────────────────────

  function openCreate() {
    setEditing(null);
    setForm({ ...EMPTY_FORM, publicationTypeId: types[0]?.id ?? '' });
    setFormError('');
    setShowNewType(false);
    setNewTypeName('');
    setNewTypeError('');
    setModal(true);
  }

  function openEdit(pub) {
    setEditing(pub);
    setForm({
      title: pub.title,
      publicationData: pub.publicationData,
      urlDoi: pub.urlDoi ?? '',
      publicationTypeId: pub.publicationType.id,
      additionalAuthorNames: '',
    });
    setFormError('');
    setShowNewType(false);
    setNewTypeName('');
    setNewTypeError('');
    setModal(true);
  }

  function handleFormChange(e) {
    setForm(f => ({ ...f, [e.target.name]: e.target.value }));
  }

  async function handleSubmit() {
    if (!form.title.trim() || !form.publicationTypeId) {
      setFormError('El título y el tipo de publicación son obligatorios.');
      return;
    }
    setFormLoading(true);
    setFormError('');
    try {
      if (editing) {
        // PUT — actualizar
        await apiFetch(`/api/Publications/${editing.id}`, {
          method: 'PUT',
          body: JSON.stringify({
            title: form.title,
            publicationData: form.publicationData,
            publicationTypeId: form.publicationTypeId,
            urlDoi: form.urlDoi || null,
          }),
        });
      } else {
        // POST — crear
        const additionalAuthors = form.additionalAuthorNames
          .split(',')
          .map(s => s.trim())
          .filter(Boolean);
        await apiFetch('/api/Publications', {
          method: 'POST',
          body: JSON.stringify({
            title: form.title,
            publicationData: form.publicationData,
            publicationTypeId: form.publicationTypeId,
            urlDoi: form.urlDoi || null,
            additionalAuthorNames: additionalAuthors,
          }),
        });
      }
      setModal(false);
      loadData();
    } catch (e) {
      setFormError(e.message);
    } finally {
      setFormLoading(false);
    }
  }

  // ── creación de tipo inline ────────────────────────────────────────────────

  async function handleCreateType() {
    if (!newTypeName.trim()) return;
    setNewTypeLoading(true);
    setNewTypeError('');
    try {
      const created = await apiFetch('/api/Publications/types', {
        method: 'POST',
        body: JSON.stringify({ name: newTypeName.trim() }),
      });
      setTypes(prev => [...prev, created].sort((a, b) => a.name.localeCompare(b.name)));
      setForm(f => ({ ...f, publicationTypeId: created.id }));
      setShowNewType(false);
      setNewTypeName('');
    } catch (e) {
      setNewTypeError(e.message);
    } finally {
      setNewTypeLoading(false);
    }
  }

  // ── borrado ────────────────────────────────────────────────────────────────

  function openDelete(pub) {
    setToDelete(pub);
    setDeleteError('');
    setDeleteModal(true);
  }

  async function handleDelete() {
    if (!toDelete) return;
    setDeleteLoading(true);
    setDeleteError('');
    try {
      await apiFetch(`/api/Publications/${toDelete.id}`, { method: 'DELETE' });
      setDeleteModal(false);
      loadData();
    } catch (e) {
      setDeleteError(e.message);
    } finally {
      setDeleteLoading(false);
    }
  }

  // ── render ─────────────────────────────────────────────────────────────────

  return (
    <>
      <Card className="shadow-sm">
        <CardHeader className="d-flex justify-content-between align-items-center">
          <strong>Mis publicaciones</strong>
          <Button color="primary" size="sm" onClick={openCreate} disabled={loading}>
            <i className="bi bi-plus-lg me-1"></i> Nueva publicación
          </Button>
        </CardHeader>
        <CardBody>
          {loading && <div className="text-center py-4"><Spinner /></div>}
          {error && <Alert color="danger">{error}</Alert>}

          {!loading && !error && publications.length === 0 && (
            <p className="text-muted text-center py-3">
              Aún no tienes publicaciones registradas.
            </p>
          )}

          {!loading && publications.length > 0 && (
            <Table hover responsive size="sm">
              <thead>
                <tr>
                  <th>Título</th>
                  <th>Tipo</th>
                  <th>URL / DOI</th>
                  <th>Autores</th>
                  <th style={{ width: 130 }}></th>
                </tr>
              </thead>
              <tbody>
                {publications.map(pub => (
                  <tr key={pub.id}>
                    <td>{pub.title}</td>
                    <td>
                      <Badge color="secondary" pill>
                        {pub.publicationType.name}
                      </Badge>
                    </td>
                    <td style={{ maxWidth: 200 }}>
                      {pub.urlDoi
                        ? <a href={pub.urlDoi} target="_blank" rel="noopener noreferrer"
                             className="text-truncate d-block" style={{ maxWidth: 190 }}
                             title={pub.urlDoi}>{pub.urlDoi}</a>
                        : <span className="text-muted">—</span>}
                    </td>
                    <td>
                      {pub.authors.map(a => (
                        <span key={a.id} className="me-2">
                          {a.name}
                          {a.userId && (
                            <i className="bi bi-person-check ms-1 text-success"
                               title="Usuario registrado"></i>
                          )}
                        </span>
                      ))}
                    </td>
                    <td className="text-end">
                      <Button
                        color="outline-primary"
                        size="sm"
                        className="me-1"
                        onClick={() => openEdit(pub)}
                      >
                        <i className="bi bi-pencil"></i>
                      </Button>
                      <Button
                        color="outline-danger"
                        size="sm"
                        onClick={() => openDelete(pub)}
                      >
                        <i className="bi bi-trash"></i>
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </Table>
          )}
        </CardBody>
      </Card>

      {/* ── Modal crear / editar ── */}
      <Modal isOpen={modal} toggle={() => setModal(false)} size="lg">
        <ModalHeader toggle={() => setModal(false)}>
          {editing ? 'Editar publicación' : 'Nueva publicación'}
        </ModalHeader>
        <ModalBody>
          {formError && <Alert color="danger">{formError}</Alert>}
          <Form>
            <FormGroup>
              <Label for="title">Título <span className="text-danger">*</span></Label>
              <Input
                id="title"
                name="title"
                value={form.title}
                onChange={handleFormChange}
                placeholder="Título de la publicación"
              />
            </FormGroup>
            <FormGroup>
              <div className="d-flex justify-content-between align-items-center mb-1">
                <Label for="publicationTypeId" className="mb-0">
                  Tipo <span className="text-danger">*</span>
                </Label>
                {!showNewType && (
                  <button type="button" className="btn btn-link btn-sm p-0"
                    onClick={() => { setShowNewType(true); setNewTypeError(''); }}>
                    <i className="bi bi-plus-circle me-1"></i>Nuevo tipo
                  </button>
                )}
              </div>
              {showNewType ? (
                <>
                  <div className="input-group input-group-sm">
                    <Input
                      value={newTypeName}
                      onChange={e => setNewTypeName(e.target.value)}
                      placeholder="Nombre del nuevo tipo"
                      onKeyDown={e => e.key === 'Enter' && handleCreateType()}
                      disabled={newTypeLoading}
                      autoFocus
                    />
                    <Button color="primary" size="sm" onClick={handleCreateType}
                      disabled={newTypeLoading || !newTypeName.trim()}>
                      {newTypeLoading ? <Spinner size="sm" /> : 'Crear'}
                    </Button>
                    <Button color="secondary" outline size="sm"
                      onClick={() => { setShowNewType(false); setNewTypeName(''); setNewTypeError(''); }}
                      disabled={newTypeLoading}>
                      Cancelar
                    </Button>
                  </div>
                  {newTypeError && <small className="text-danger">{newTypeError}</small>}
                </>
              ) : (
                <Input
                  type="select"
                  id="publicationTypeId"
                  name="publicationTypeId"
                  value={form.publicationTypeId}
                  onChange={handleFormChange}
                >
                  {types.map(t => (
                    <option key={t.id} value={t.id}>{t.name}</option>
                  ))}
                </Input>
              )}
            </FormGroup>
            <FormGroup>
              <Label for="urlDoi">URL / DOI</Label>
              <Input
                id="urlDoi"
                name="urlDoi"
                value={form.urlDoi}
                onChange={handleFormChange}
                placeholder="https://doi.org/10.xxxx/... o URL de la publicación"
              />
            </FormGroup>
            <FormGroup>
              <Label for="publicationData">Datos / Resumen</Label>
              <Input
                type="textarea"
                id="publicationData"
                name="publicationData"
                rows={4}
                value={form.publicationData}
                onChange={handleFormChange}
                placeholder="Resumen o cualquier información adicional relevante"
              />
            </FormGroup>
            {/* Coautores solo al crear */}
            {!editing && (
              <FormGroup>
                <Label for="additionalAuthorNames">
                  Coautores adicionales
                  <small className="text-muted ms-2">(nombres separados por coma)</small>
                </Label>
                <Input
                  id="additionalAuthorNames"
                  name="additionalAuthorNames"
                  value={form.additionalAuthorNames}
                  onChange={handleFormChange}
                  placeholder="Ej: Juan Pérez, María González"
                />
              </FormGroup>
            )}
          </Form>
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" outline onClick={() => setModal(false)} disabled={formLoading}>
            Cancelar
          </Button>
          <Button color="primary" onClick={handleSubmit} disabled={formLoading}>
            {formLoading
              ? <><Spinner size="sm" className="me-1" /> Guardando…</>
              : editing ? 'Guardar cambios' : 'Crear publicación'
            }
          </Button>
        </ModalFooter>
      </Modal>

      {/* ── Modal confirmar borrado ── */}
      <Modal isOpen={deleteModal} toggle={() => setDeleteModal(false)} size="sm">
        <ModalHeader toggle={() => setDeleteModal(false)}>Eliminar publicación</ModalHeader>
        <ModalBody>
          {deleteError && <Alert color="danger">{deleteError}</Alert>}
          <p>
            ¿Seguro que quieres eliminar <strong>"{toDelete?.title}"</strong>?
            Esta acción no se puede deshacer.
          </p>
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" outline onClick={() => setDeleteModal(false)} disabled={deleteLoading}>
            Cancelar
          </Button>
          <Button color="danger" onClick={handleDelete} disabled={deleteLoading}>
            {deleteLoading
              ? <><Spinner size="sm" className="me-1" /> Eliminando…</>
              : 'Eliminar'
            }
          </Button>
        </ModalFooter>
      </Modal>
    </>
  );
}
