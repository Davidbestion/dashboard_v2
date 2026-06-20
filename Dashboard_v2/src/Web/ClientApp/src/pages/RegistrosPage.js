import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Button, Spinner, Alert, Badge,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input, InputGroup,
} from 'reactstrap';
import { useAuth } from '../contexts/AuthContext';
import FilterableDataTable from '../components/FilterableDataTable';
import CertificateUpload, { CertificateViewButton } from '../components/CertificateUpload';

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

const emptyForm = { titulo: '', numeroCertificado: '', esInformatico: false, countryId: '', institutionId: '', evidenceFileId: null };

export default function RegistrosPage() {
  const [items, setItems] = useState([]);
  const [institutions, setInstitutions] = useState([]);
  const [countries, setCountries] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const { user } = useAuth();
  const canGenerateAnexo = user?.role === 'Superuser' || user?.role === 'Jefe_de_Grupo_de_investigacion';
  const [generatingAnexo, setGeneratingAnexo] = useState(false);
  const [anexoError, setAnexoError] = useState('');

  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');
  // Inline create institution
  const [showNewInstitution, setShowNewInstitution] = useState(false);
  const [newInstitutionInput, setNewInstitutionInput] = useState('');
  const [newInstitutionLoading, setNewInstitutionLoading] = useState(false);
  const [newInstitutionError, setNewInstitutionError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [regs, insts, cts] = await Promise.all([
        apiFetch('/api/Registros'),
        apiFetch('/api/Institutions'),
        apiFetch('/api/Events/countries'),
      ]);
      setItems(regs);
      setInstitutions(insts);
      setCountries(cts);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  function openCreate() {
    setEditing(null);
    setForm({ ...emptyForm, countryId: countries.length > 0 ? String(countries[0].id) : '' });
    setFormError('');
    setModal(true);
  }

  function openEdit(item) {
    setEditing(item);
    setForm({
      titulo: item.titulo,
      numeroCertificado: item.numeroCertificado,
      esInformatico: item.esInformatico,
      countryId: String(item.countryId ?? ''),
      institutionId: item.institutionId ?? '',
      evidenceFileId: item.evidenceFileId ?? null,
    });
    setFormError('');
    setModal(true);
  }

  async function handleSave(e) {
    if (e) e.preventDefault();
    setSaving(true);
    setFormError('');
    const body = {
      titulo: form.titulo,
      numeroCertificado: form.numeroCertificado,
      esInformatico: form.esInformatico,
      countryId: parseInt(form.countryId, 10),
      institutionId: form.institutionId,
      evidenceFileId: form.evidenceFileId ?? null,
    };

    try {
      if (editing) {
        await apiFetch(`/api/Registros/${editing.id}`, { method: 'PUT', body: JSON.stringify(body) });
      } else {
        await apiFetch('/api/Registros', { method: 'POST', body: JSON.stringify(body) });
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
    if (!window.confirm('¿Eliminar este registro?')) return;
    try {
      await apiFetch(`/api/Registros/${id}`, { method: 'DELETE' });
      await load();
    } catch (e) {
      setError(e.message);
    }
  }

  async function handleCreateInstitution() {
    const name = newInstitutionInput.trim();
    if (!name) return;
    setNewInstitutionLoading(true);
    setNewInstitutionError('');
    try {
      const created = await apiFetch('/api/Institutions', { method: 'POST', body: JSON.stringify({ nombre: name }) });
      setInstitutions(prev => [...prev, created].sort((a, b) => a.nombre.localeCompare(b.nombre)));
      setForm(f => ({ ...f, institutionId: created.id }));
      setNewInstitutionInput('');
      setShowNewInstitution(false);
    } catch (e) {
      setNewInstitutionError(e.message);
    } finally {
      setNewInstitutionLoading(false);
    }
  }

  async function handleGenerateAnexo() {
    setGeneratingAnexo(true);
    setAnexoError('');
    try {
      const response = await fetch('/api/Documents/anexo-registros', { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => ({}));
        const message = data?.error ?? data?.title ?? 'No se pudo generar el anexo.';
        throw new Error(message);
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'anexo-registros.xlsx';
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


  if (loading) return <div className="d-flex justify-content-center mt-5"><Spinner color="primary" /></div>;

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="mb-0">Registros</h2>
        <div>
          {canGenerateAnexo && (
            <Button color="success" onClick={handleGenerateAnexo} disabled={generatingAnexo} className="me-2">
              {generatingAnexo ? <Spinner size="sm" /> : '⬇ Generar Anexo 7'}
            </Button>
          )}
          <Button color="primary" onClick={openCreate}>+ Nuevo registro</Button>
        </div>
      </div>

      {error && <Alert color="danger">{error}</Alert>}
      {anexoError && <Alert color="danger">{anexoError}</Alert>}

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
                { key: 'countryName', label: 'País',
                  options: countries.map(c => ({ value: c.name, label: c.name })) },
                { key: 'institutionNombre', label: 'Institución',
                  options: institutions.map(i => ({ value: i.nombre, label: i.nombre })) },
              ],
            }}
            columns={[
              { key: 'titulo',             label: 'Título', sortable: true },
              { key: 'numeroCertificado',  label: 'Número' },
              { key: 'countryName',        label: 'País',   render: v => <Badge color="secondary" pill>{v}</Badge> },
              { key: 'institutionNombre',  label: 'Institución' },
            ]}
            data={items}
            keyExtractor={it => it.id}
            actions={[
              { key: 'edit',   label: 'Editar',   icon: 'bi-pencil', color: 'outline-secondary', onClick: it => openEdit(it) },
              { key: 'delete', label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',    onClick: it => handleDelete(it.id) },
              { key: 'certificate', label: 'Certificado', show: it => it.evidenceFileId != null,
                render: it => <CertificateViewButton fileId={it.evidenceFileId} /> },
            ]}
            emptyMessage="No hay registros."
            detailConfig
          />
        </CardBody>
      </Card>

      <Modal isOpen={modal} toggle={() => setModal(false)}>
        <Form onSubmit={handleSave}>
          <ModalHeader toggle={() => setModal(false)}>{editing ? 'Editar registro' : 'Nuevo registro'}</ModalHeader>
          <ModalBody>
            {formError && <Alert color="danger">{formError}</Alert>}
            <FormGroup>
              <Label for="titulo">Título *</Label>
              <Input id="titulo" value={form.titulo} onChange={e => setForm(f => ({ ...f, titulo: e.target.value }))} />
            </FormGroup>
            <FormGroup>
              <Label for="numero">Número certificado *</Label>
              <Input id="numero" value={form.numeroCertificado} onChange={e => setForm(f => ({ ...f, numeroCertificado: e.target.value }))} />
            </FormGroup>
            <FormGroup>
              <Label for="country">País *</Label>
              <Input type="select" id="country" value={form.countryId} onChange={e => setForm(f => ({ ...f, countryId: e.target.value }))}>
                <option value="">— Selecciona —</option>
                {countries.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </Input>
            </FormGroup>
            <FormGroup>
              <Label for="institution">Institución *</Label>
              <Input type="select" id="institution" value={form.institutionId} onChange={e => setForm(f => ({ ...f, institutionId: e.target.value }))}>
                <option value="">— Selecciona —</option>
                {institutions.map(i => <option key={i.id} value={i.id}>{i.nombre}</option>)}
              </Input>
              <div className="d-flex gap-2 align-items-center mt-2">
                <Button type="button" color="secondary" outline size="sm" style={{ whiteSpace: 'nowrap' }}
                  onClick={() => { setShowNewInstitution(v => !v); setNewInstitutionError(''); }}>
                  <i className="bi bi-plus" /> Nuevo
                </Button>
              </div>
              {showNewInstitution && (
                <div className="mt-2">
                  {newInstitutionError && <Alert color="danger" className="py-1 px-2 small mb-1">{newInstitutionError}</Alert>}
                  <InputGroup size="sm">
                    <Input
                      placeholder="Nombre de la institución..."
                      value={newInstitutionInput}
                      onChange={e => setNewInstitutionInput(e.target.value)}
                      onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); handleCreateInstitution(); } }}
                      autoFocus
                    />
                    <Button type="button" color="primary" onClick={handleCreateInstitution} disabled={newInstitutionLoading}>
                      {newInstitutionLoading ? <Spinner size="sm" /> : 'Crear'}
                    </Button>
                    <Button type="button" color="secondary" outline onClick={() => setShowNewInstitution(false)}>✕</Button>
                  </InputGroup>
                </div>
              )}
            </FormGroup>
            <FormGroup check>
              <Label check>
                <Input type="checkbox" checked={form.esInformatico} onChange={e => setForm(f => ({ ...f, esInformatico: e.target.checked }))} />{' '}
                Es informático
              </Label>
            </FormGroup>
            <FormGroup>
              <Label>Certificado / Evidencia</Label>
              <CertificateUpload
                fileId={form.evidenceFileId}
                onFileIdChange={id => setForm(f => ({ ...f, evidenceFileId: id }))}
                canManage
                canView
                disabled={saving}
              />
            </FormGroup>
          </ModalBody>
          <ModalFooter>
            <Button color="primary" type="submit" disabled={saving}>{saving ? <Spinner size="sm" /> : 'Guardar'}</Button>
            <Button color="secondary" onClick={() => setModal(false)}>Cancelar</Button>
          </ModalFooter>
        </Form>
      </Modal>
    </>
  );
}
