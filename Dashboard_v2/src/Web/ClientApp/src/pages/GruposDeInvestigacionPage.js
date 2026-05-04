import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Button, Spinner, Alert, Badge,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input,
} from 'reactstrap';
import Select from 'react-select';
import UserCard from '../components/UserCard';
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

export default function GruposDeInvestigacionPage() {
  const [items, setItems] = useState([]);
  const [areas, setAreas] = useState([]);
  const [lineasDeInvestigacion, setLineasDeInvestigacion] = useState([]);
  const [usuarios, setUsuarios] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  const [membrosModal, setMembrosModal] = useState(false);
  const [membrosGrupo, setMembrosGrupo] = useState(null);
  const [membrosSelectedIds, setMembrosSelectedIds] = useState(new Set());
  const [membrosOriginalIds, setMembrosOriginalIds] = useState(new Set());
  const [savingMiembros, setSavingMiembros] = useState(false);
  const [membrosError, setMembrosError] = useState('');
  const [membrosFilter, setMembrosFilter] = useState('');

  const [generatingAnexo, setGeneratingAnexo] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [gruposData, areasData, lineasData, usuariosData] = await Promise.all([
        apiFetch('/api/GruposDeInvestigacion'),
        apiFetch('/api/Areas'),
        apiFetch('/api/LineasDeInvestigacion'),
        apiFetch('/api/Users'),
      ]);
      setItems(gruposData);
      setAreas(areasData);
      setLineasDeInvestigacion(lineasData);
      setUsuarios(usuariosData);
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
        await apiFetch(`/api/GruposDeInvestigacion/${editing.id}`, { method: 'PUT', body: JSON.stringify(body) });
      } else {
        await apiFetch('/api/GruposDeInvestigacion', { method: 'POST', body: JSON.stringify(body) });
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
    if (!window.confirm('¿Eliminar este grupo de investigación?')) return;
    try {
      await apiFetch(`/api/GruposDeInvestigacion/${id}`, { method: 'DELETE' });
      await load();
    } catch (e) {
      setError(e.message);
    }
  }

  function openMembros(item) {
    setMembrosGrupo(item);
    const initial = new Set(item.usuariosIds ?? []);
    setMembrosSelectedIds(initial);
    setMembrosOriginalIds(new Set(initial));
    setMembrosError('');
    setMembrosFilter('');
    setMembrosModal(true);
  }

  async function handleSaveMiembros() {
    setSavingMiembros(true);
    setMembrosError('');
    try {
      await apiFetch(`/api/GruposDeInvestigacion/${membrosGrupo.id}/miembros`, {
        method: 'PUT',
        body: JSON.stringify({ usuariosIds: [...membrosSelectedIds] }),
      });
      setMembrosModal(false);
      await load();
    } catch (e) {
      setMembrosError(e.message);
    } finally {
      setSavingMiembros(false);
    }
  }

  function toggleMiembro(userId) {
    setMembrosSelectedIds(prev => {
      const next = new Set(prev);
      if (next.has(userId)) next.delete(userId);
      else next.add(userId);
      return next;
    });
  }

  async function handleGenerarAnexo() {
    setGeneratingAnexo(true);
    setError('');
    try {
      const response = await fetch('/api/Documents/anexo-grupos', { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.error ?? 'Error al generar el Anexo 10.');
      }
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      const now = new Date();
      a.download = `Anexo_10_Grupos_Investigacion_${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}.xlsx`;
      a.href = url;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e) {
      setError(e.message);
    } finally {
      setGeneratingAnexo(false);
    }
  }

  /**
   * Permite localizar fichas por datos personales y también por adscripción institucional.
   * Esto hace más útil el modal ahora que las tarjetas muestran área y universidad.
   */
  const filteredUsuarios = membrosFilter.trim()
    ? usuarios.filter(u =>
        `${u.userName} ${u.userLastName1 ?? ''} ${u.userLastName2 ?? ''} ${u.email} ${u.areaNombre ?? ''} ${u.universidadNombre ?? ''}`
          .toLowerCase()
          .includes(membrosFilter.toLowerCase())
      )
    : usuarios;

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
        <h2 className="mb-0">Grupos de Investigación</h2>
        <div className="d-flex gap-2">
          <Button color="outline-success" onClick={handleGenerarAnexo} disabled={generatingAnexo}>
            {generatingAnexo ? <Spinner size="sm" /> : '⬇ Generar Anexo 10'}
          </Button>
          <Button color="primary" onClick={openCreate} disabled={areas.length === 0}>
            + Nuevo grupo
          </Button>
        </div>
      </div>

      {areas.length === 0 && (
        <Alert color="warning">
          Debes crear al menos un <strong>Área</strong> antes de poder añadir grupos de investigación.
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
                  options: [...new Set(items.map(i => i.areaNombre).filter(Boolean))].sort().map(v => ({ value: v, label: v })) },
              ],
            }}
            columns={[
              { key: 'nombre',   label: 'Nombre', sortable: true, className: 'fw-semibold' },
              { key: 'areaNombre', label: 'Área', render: v => <Badge color="secondary" pill>{v}</Badge> },
              {
                key: 'usuariosIds',
                label: 'Miembros',
                sortable: true,
                sortValue: ids => (ids ?? []).length,
                render: ids => ids && ids.length > 0
                  ? <Badge color="primary" pill>{ids.length} miembro{ids.length !== 1 ? 's' : ''}</Badge>
                  : <span className="text-muted small">Sin miembros</span>,
              },
            ]}
            data={items}
            keyExtractor={item => item.id}
            actions={[
              { key: 'members', label: 'Miembros', color: 'outline-info',      onClick: item => openMembros(item) },
              { key: 'edit',    label: 'Editar',   icon: 'bi-pencil', color: 'outline-secondary', onClick: item => openEdit(item) },
              { key: 'delete',  label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',    onClick: item => handleDelete(item.id) },
            ]}
            emptyMessage="No hay grupos de investigación registrados."
          />
        </CardBody>
      </Card>

      <Modal isOpen={modal} toggle={() => setModal(false)}>
        <ModalHeader toggle={() => setModal(false)}>
          {editing ? 'Editar grupo de investigación' : 'Nuevo grupo de investigación'}
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

      {/* Modal: Gestionar miembros — fichas de usuario */}
      <Modal isOpen={membrosModal} toggle={() => setMembrosModal(false)} size="xl" scrollable>
        <ModalHeader toggle={() => setMembrosModal(false)}>
          Miembros de «{membrosGrupo?.nombre}»
          {membrosSelectedIds.size > 0 && (
            <Badge color="primary" pill className="ms-2">{membrosSelectedIds.size} seleccionado{membrosSelectedIds.size !== 1 ? 's' : ''}</Badge>
          )}
        </ModalHeader>
        <ModalBody>
          {membrosError && <Alert color="danger">{membrosError}</Alert>}
          <p className="text-muted small mb-3">
            Haz clic en una ficha para agregar o quitar miembros.
            <span style={{ color: '#198754', fontWeight: 600 }}> Verde</span> = ya es miembro,
            <span style={{ color: '#0d6efd', fontWeight: 600 }}> Azul</span> = recién agregado,
            <span style={{ color: '#dc3545', fontWeight: 600 }}> Rojo</span> = se eliminará,
            <span style={{ color: '#d97706', fontWeight: 600 }}> ★</span> = creador del grupo.
          </p>
          <Input
            placeholder="Filtrar por nombre, apellido o correo..."
            value={membrosFilter}
            onChange={e => setMembrosFilter(e.target.value)}
            className="mb-3"
          />
          {filteredUsuarios.length === 0
            ? <p className="text-muted text-center py-3">No hay usuarios que coincidan.</p>
            : (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(230px, 1fr))', gap: '0.75rem' }}>
                {filteredUsuarios.map(u => (
                  <UserCard
                    key={u.id}
                    user={u}
                    isSelected={membrosSelectedIds.has(u.id)}
                    isOriginal={membrosOriginalIds.has(u.id)}
                    isCreator={u.id === membrosGrupo?.creadorId}
                    onClick={toggleMiembro}
                  />
                ))}
              </div>
            )
          }
        </ModalBody>
        <ModalFooter>
          <Button color="primary" onClick={handleSaveMiembros} disabled={savingMiembros}>
            {savingMiembros ? <Spinner size="sm" /> : 'Guardar miembros'}
          </Button>
          <Button color="secondary" onClick={() => setMembrosModal(false)}>Cancelar</Button>
        </ModalFooter>
      </Modal>
    </>
  );
}
