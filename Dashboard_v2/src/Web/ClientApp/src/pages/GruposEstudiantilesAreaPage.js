import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Button, Spinner, Alert,
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

const emptyForm = { nombre: '', lineasDeInvestigacionIds: [] };

export default function GruposEstudiantilesAreaPage() {
  const [items, setItems] = useState([]);
  const [lineas, setLineas] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [generatingAnexo, setGeneratingAnexo] = useState(false);
  const [anexoError, setAnexoError] = useState('');

  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try {
      const [gruposData, lineasData] = await Promise.all([
        apiFetch('/api/GruposEstudiantiles/area'),
        apiFetch('/api/LineasDeInvestigacion'),
      ]);
      setItems(gruposData);
      setLineas(lineasData);
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
    setForm({ nombre: item.nombre, lineasDeInvestigacionIds: item.lineasDeInvestigacionIds ?? [] });
    setFormError('');
    setModal(true);
  }

  async function handleDelete(item) {
    if (!window.confirm(`¿Eliminar el grupo "${item.nombre}"?`)) return;
    try {
      await apiFetch(`/api/GruposEstudiantiles/${item.id}`, { method: 'DELETE' });
      await load();
    } catch (e) {
      setError(e.message);
    }
  }

  async function handleSave() {
    setSaving(true); setFormError('');
    // areaId is auto-assigned by the backend for Vicedecano; send empty string as placeholder.
    const body = { nombre: form.nombre, areaId: '', lineasDeInvestigacionIds: form.lineasDeInvestigacionIds };
    try {
      if (editing) {
        await apiFetch(`/api/GruposEstudiantiles/${editing.id}`, { method: 'PUT', body: JSON.stringify(body) });
      } else {
        await apiFetch('/api/GruposEstudiantiles', { method: 'POST', body: JSON.stringify(body) });
      }
      setModal(false);
      await load();
    } catch (e) {
      setFormError(e.message);
    } finally {
      setSaving(false);
    }
  }

  async function handleGenerarAnexo() {
    setGeneratingAnexo(true); setAnexoError('');
    try {
      const response = await fetch('/api/Documents/anexo-grupos-estudiantiles', { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.error ?? 'Error al generar el anexo.');
      }
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      const now = new Date();
      a.download = `Anexo_9_Grupos_Cientificos_Estudiantiles_${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}.xlsx`;
      a.href = url;
      a.click();
      URL.revokeObjectURL(url);
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
        <h2 className="mb-0">Grupos Estudiantiles del Área</h2>
        <div className="d-flex gap-2">
          <Button color="outline-success" onClick={handleGenerarAnexo} disabled={generatingAnexo}>
            {generatingAnexo ? <Spinner size="sm" /> : '⬇ Generar Anexo 9'}
          </Button>
          <Button color="primary" onClick={openCreate}>
            + Nuevo grupo
          </Button>
        </div>
      </div>

      {error && <Alert color="danger" toggle={() => setError('')}>{error}</Alert>}
      {anexoError && <Alert color="danger" toggle={() => setAnexoError('')}>{anexoError}</Alert>}

      <Card>
        <CardHeader>
          <strong>Grupos Científicos Estudiantiles</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['nombre', 'areaNombre'], placeholder: 'Buscar grupo...' },
            }}
            columns={[
              { key: 'nombre',     label: 'Nombre',    sortable: true, className: 'fw-semibold' },
              { key: 'areaNombre', label: 'Área' },
              { key: 'lineasDeInvestigacionIds', label: 'Líneas', render: v => v?.length ?? 0 },
            ]}
            data={items}
            keyExtractor={i => i.id}
            actions={[
              { key: 'edit',   label: 'Editar',   icon: 'bi-pencil', color: 'outline-secondary', onClick: item => openEdit(item) },
              { key: 'delete', label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',    onClick: item => handleDelete(item) },
            ]}
            emptyMessage="No hay grupos estudiantiles en el área."
            detailConfig
          />
        </CardBody>
      </Card>

      <Modal isOpen={modal} toggle={() => setModal(false)}>
        <ModalHeader toggle={() => setModal(false)}>
          {editing ? 'Editar grupo estudiantil' : 'Nuevo grupo estudiantil'}
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
                placeholder="Nombre del grupo"
              />
            </FormGroup>
            <FormGroup>
              <Label>Líneas de investigación que estudia</Label>
              <Select
                isMulti
                options={lineas.map(l => ({ value: l.id, label: l.nombre }))}
                value={form.lineasDeInvestigacionIds.map(id => {
                  const l = lineas.find(x => x.id === id);
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
