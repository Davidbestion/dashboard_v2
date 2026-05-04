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

const emptyForm = { nombre: '', areaId: '', lineasDeInvestigacionIds: [] };

export default function GruposEstudiantilesPage() {
  const [items, setItems] = useState([]);
  const [areas, setAreas] = useState([]);
  const [lineasDeInvestigacion, setLineasDeInvestigacion] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [generatingAnexo, setGeneratingAnexo] = useState(false);

  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [gruposData, areasData, lineasData] = await Promise.all([
        apiFetch('/api/GruposEstudiantiles'),
        apiFetch('/api/Areas'),
        apiFetch('/api/LineasDeInvestigacion'),
      ]);
      setItems(gruposData);
      setAreas(areasData);
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
    setForm({ nombre: '', areaId: areas.length > 0 ? areas[0].id : '', lineasDeInvestigacionIds: [] });
    setFormError('');
    setModal(true);
  }

  function openEdit(item) {
    setEditing(item);
    setForm({ nombre: item.nombre, areaId: item.areaId, lineasDeInvestigacionIds: item.lineasDeInvestigacionIds ?? [] });
    setFormError('');
    setModal(true);
  }

  async function handleSave() {
    setSaving(true);
    setFormError('');
    const body = { nombre: form.nombre, areaId: form.areaId, lineasDeInvestigacionIds: form.lineasDeInvestigacionIds };
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

  async function handleDelete(id) {
    if (!window.confirm('¿Eliminar este grupo estudiantil?')) return;
    try {
      await apiFetch(`/api/GruposEstudiantiles/${id}`, { method: 'DELETE' });
      await load();
    } catch (e) {
      setError(e.message);
    }
  }

  /**
   * Solicita al backend la generación del anexo de grupos estudiantiles
   * y dispara la descarga del archivo Excel resultante en el navegador.
   */
  async function handleGenerarAnexo() {
    setGeneratingAnexo(true);
    setError('');
    try {
      const response = await fetch('/api/Documents/anexo-grupos-estudiantiles', { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.error ?? 'Error al generar el Anexo 9.');
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
      setError(e.message);
    } finally {
      setGeneratingAnexo(false);
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
        <h2 className="mb-0">Grupos Científicos Estudiantiles</h2>
        <div className="d-flex gap-2">
          <Button color="outline-success" onClick={handleGenerarAnexo} disabled={generatingAnexo}>
            {generatingAnexo ? <Spinner size="sm" /> : '⬇ Generar Anexo 9'}
          </Button>
          <Button color="primary" onClick={openCreate} disabled={areas.length === 0}>
            + Nuevo grupo
          </Button>
        </div>
      </div>

      {areas.length === 0 && (
        <Alert color="warning">
          Debes crear al menos un <strong>Área</strong> antes de poder añadir grupos estudiantiles.
        </Alert>
      )}

      {error && <Alert color="danger" toggle={() => setError('')}>{error}</Alert>}

      <Card>
        <CardHeader>
          <strong>Grupos registrados</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['nombre'], placeholder: 'Buscar grupo...' },
              filters: [
                { key: 'areaNombre', label: 'Área',
                  options: areas.map(a => ({ value: a.nombre, label: a.nombre })) },
              ],
            }}
            columns={[
              { key: 'nombre',   label: 'Nombre', sortable: true, className: 'fw-semibold' },
              { key: 'areaNombre', label: 'Área', render: v => <Badge color="secondary" pill>{v}</Badge> },
            ]}
            data={items}
            keyExtractor={item => item.id}
            actions={[
              { key: 'edit',   label: 'Editar',   icon: 'bi-pencil', color: 'outline-secondary', onClick: item => openEdit(item) },
              { key: 'delete', label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',    onClick: item => handleDelete(item.id) },
            ]}
            emptyMessage="No hay grupos estudiantiles registrados."
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
              <Label for="areaId">Área *</Label>
              <Input
                id="areaId"
                type="select"
                value={form.areaId}
                onChange={e => setForm(f => ({ ...f, areaId: e.target.value }))}
              >
                <option value="">— Selecciona un área —</option>
                {areas.map(a => (
                  <option key={a.id} value={a.id}>{a.nombre}</option>
                ))}
              </Input>
            </FormGroup>
            <FormGroup>
              <Label>Líneas de investigación que estudia</Label>
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
