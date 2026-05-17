import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Button, Spinner, Alert,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input,
  Table,
} from 'reactstrap';
import DataTable from '../components/DataTable';
import FilterableDataTable from '../components/FilterableDataTable';
import UserCard from '../components/UserCard';

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

const TIPOS_RED = [
  { value: 0, label: 'Universitaria' },
  { value: 1, label: 'Nacional' },
  { value: 2, label: 'Internacional' },
];

const emptyForm = { nombre: '', countryId: '', cantidadProfesores: 0, tipo: 0 };

export default function RedesPage() {
  const [items, setItems] = useState([]);
  const [countries, setCountries] = useState([]);
  const [areas, setAreas] = useState([]);
  const [misRedes, setMisRedes] = useState([]);
  const [allUsers, setAllUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  // Crear país inline
  const [newCountryName, setNewCountryName] = useState('');
  const [newCountryOpen, setNewCountryOpen] = useState(false);
  const [newCountrySaving, setNewCountrySaving] = useState(false);
  const [newCountryError, setNewCountryError] = useState('');

  // Coordinador de red universitaria
  const [coordAreaId, setCoordAreaId] = useState('');
  const [coordUserId, setCoordUserId] = useState('');
  const [coordUserSearch, setCoordUserSearch] = useState('');
  const [eventsModal, setEventsModal] = useState(false);
  const [assigningRed, setAssigningRed] = useState(null);
  const [availableEvents, setAvailableEvents] = useState([]);
  const [eventsLoading, setEventsLoading] = useState(false);
  const [selectedEventIds, setSelectedEventIds] = useState(new Set());
  const [assignError, setAssignError] = useState('');
  const [assignSaving, setAssignSaving] = useState(false);

  // Generación de anexos
  const [generatingAnexo, setGeneratingAnexo] = useState(null); // null | 'universitarias' | 'nac-inter'
  const [anexoError, setAnexoError] = useState('');

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try {
      const [reds, cs, areasList, misRedesList, usersList] = await Promise.all([
        apiFetch('/api/Redes').catch(() => []),
        apiFetch('/api/Events/countries').catch(() => []),
        apiFetch('/api/Areas').catch(() => []),
        apiFetch('/api/Redes/mis-redes').catch(() => []),
        apiFetch('/api/Users').catch(() => []),
      ]);
      setItems(reds);
      setCountries(cs);
      setAreas(areasList);
      setMisRedes(misRedesList);
      setAllUsers(Array.isArray(usersList) ? usersList : []);
    } catch (e) { setError(e.message); } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  function resetCoord() {
    setCoordAreaId(''); setCoordUserId(''); setCoordUserSearch('');
  }

  function openCreate() {
    setEditing(null); setForm(emptyForm); setFormError('');
    setNewCountryOpen(false); setNewCountryName('');
    resetCoord();
    setModal(true);
  }

  function openEdit(it) {
    setEditing(it);
    setForm({ nombre: it.nombre ?? it.Nombre, countryId: (it.countryId ?? it.CountryId ?? '')?.toString?.() ?? '', cantidadProfesores: it.cantidadProfesores ?? it.CantidadProfesores, tipo: it.tipo ?? it.Tipo ?? 0 });
    setFormError(''); setNewCountryOpen(false); setNewCountryName('');
    // Pre-rellenar coordinador si es universitaria
    const redId = it.id ?? it.Id;
    const redInfo = misRedes.find(r => r.id === redId);
    const firstCoord = redInfo?.coordinadores?.[0];
    if (firstCoord) {
      setCoordAreaId(firstCoord.areaId ?? '');
      setCoordUserId(firstCoord.coordinadorId ?? '');
      setCoordUserSearch('');
    } else {
      resetCoord();
    }
    setModal(true);
  }

  function openAssign(it) {
    setAssigningRed(it);
    setAssignError('');
    setEventsModal(true);
    loadEventsForRed(it);
  }

  async function loadEventsForRed(red) {
    setEventsLoading(true);
    try {
      const list = await apiFetch(`/api/Redes/${red.id ?? red.Id}/events`);
      setAvailableEvents(list);
      const selected = new Set(list.filter(x => x.assigned ?? x.Assigned).map(x => x.id ?? x.Id));
      setSelectedEventIds(selected);
    } catch (e) {
      setAssignError(e.message);
    } finally {
      setEventsLoading(false);
    }
  }

  function toggleSelectEvent(evId) {
    setSelectedEventIds(s => {
      const copy = new Set(Array.from(s));
      if (copy.has(evId)) copy.delete(evId); else copy.add(evId);
      return copy;
    });
  }

  async function handleAssignSave(e) {
    if (e) e.preventDefault();
    setAssignSaving(true); setAssignError('');
    try {
      const ids = Array.from(selectedEventIds);
      await apiFetch(`/api/Redes/${assigningRed.id ?? assigningRed.Id}/events`, { method: 'POST', body: JSON.stringify({ eventIds: ids }) });
      setEventsModal(false);
      await load();
    } catch (err) {
      setAssignError(err.message);
    } finally { setAssignSaving(false); }
  }

  async function handleGenerarAnexo(tipo) {
    const reportName = tipo === 'universitarias'
      ? 'anexo-redes-universitarias'
      : 'anexo-redes-nac-inter';
    const label = tipo === 'universitarias'
      ? 'Anexo_Redes_Universitarias'
      : 'Anexo_Redes_Nac_Inter';
    setGeneratingAnexo(tipo); setAnexoError('');
    try {
      const response = await fetch(`/api/Documents/${reportName}`, { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.error ?? 'Error al generar el anexo.');
      }
      const blob = await response.blob();
      const isZip = (response.headers.get('Content-Type') ?? '').includes('zip');
      const ext = isZip ? 'zip' : 'xlsx';
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      const now = new Date();
      a.download = `${label}_${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}.${ext}`;
      a.href = url;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e) {
      setAnexoError(e.message);
    } finally {
      setGeneratingAnexo(null);
    }
  }

  async function handleSave(e) {
    if (e) e.preventDefault(); setSaving(true); setFormError('');
    const body = { nombre: form.nombre, countryId: form.countryId ? parseInt(form.countryId, 10) : null, cantidadProfesores: parseInt(form.cantidadProfesores, 10), tipo: parseInt(form.tipo, 10) };
    try {
      let redId;
      if (editing) {
        await apiFetch(`/api/Redes/${editing.id}`, { method: 'PUT', body: JSON.stringify(body) });
        redId = editing.id;
      } else {
        const created = await apiFetch('/api/Redes', { method: 'POST', body: JSON.stringify(body) });
        redId = created?.id ?? created?.Id;
      }
      // Asignar coordinador si es universitaria y hay usuario seleccionado
      if (parseInt(form.tipo, 10) === 0 && coordUserId) {
        const coordUser = allUsers.find(u => u.id === coordUserId);
        const effectiveAreaId = coordAreaId || coordUser?.areaId;
        if (!effectiveAreaId) {
          setFormError('El coordinador seleccionado no tiene área asignada. Selecciona un área en el formulario.');
          setSaving(false);
          return;
        }
        await apiFetch(`/api/Redes/${redId}/coordinadores/${effectiveAreaId}`, {
          method: 'PUT',
          body: JSON.stringify({ coordinadorId: coordUserId }),
        });
      }
      setModal(false); await load();
    } catch (e) { setFormError(e.message); } finally { setSaving(false); }
  }

  async function handleDelete(id) { if (!window.confirm('¿Eliminar esta red?')) return; try { await apiFetch(`/api/Redes/${id}`, { method: 'DELETE' }); await load(); } catch (e) { setError(e.message); } }

  async function handleCreateCountry(e) {
    if (e) e.preventDefault();
    if (!newCountryName.trim()) return;
    setNewCountrySaving(true); setNewCountryError('');
    try {
      const created = await apiFetch('/api/Events/countries', {
        method: 'POST',
        body: JSON.stringify({ name: newCountryName.trim() }),
      });
      setCountries(cs => [...cs, created]);
      setForm(f => ({ ...f, countryId: String(created.id ?? created.Id) }));
      setNewCountryName('');
      setNewCountryOpen(false);
    } catch (err) {
      setNewCountryError(err.message);
    } finally {
      setNewCountrySaving(false);
    }
  }

  const selectedCoordUser = coordUserId ? allUsers.find(u => u.id === coordUserId) : null;
  const filteredCoordUsers = !coordUserId && coordUserSearch.trim()
    ? allUsers.filter(u =>
        `${u.userName} ${u.userLastName1 ?? ''} ${u.userLastName2 ?? ''} ${u.email} ${u.areaNombre ?? ''} ${u.universidadNombre ?? ''}`
          .toLowerCase()
          .includes(coordUserSearch.toLowerCase())
      )
    : [];

  if (loading) return <div className="d-flex justify-content-center mt-5"><Spinner color="primary" /></div>;

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="mb-0">Redes</h2>
        <div className="d-flex gap-2">
          <Button
            color="outline-success"
            size="sm"
            onClick={() => handleGenerarAnexo('universitarias')}
            disabled={generatingAnexo !== null}
            title="Descargar Anexo Redes Universitarias"
          >
            {generatingAnexo === 'universitarias' ? <Spinner size="sm" /> : <><i className="bi bi-file-earmark-excel me-1" />Universitarias</>}
          </Button>
          <Button
            color="outline-success"
            size="sm"
            onClick={() => handleGenerarAnexo('nac-inter')}
            disabled={generatingAnexo !== null}
            title="Descargar Anexo Redes Nacionales e Internacionales"
          >
            {generatingAnexo === 'nac-inter' ? <Spinner size="sm" /> : <><i className="bi bi-file-earmark-excel me-1" />Nac. / Inter.</>}
          </Button>
          <Button color="primary" onClick={openCreate}>+ Nueva red</Button>
        </div>
      </div>

      {anexoError && <Alert color="danger" toggle={() => setAnexoError('')}>{anexoError}</Alert>}

      {error && <Alert color="danger">{error}</Alert>}

      <Card>
        <CardHeader><strong>Redes</strong> <small className="text-muted ms-2">({items.length})</small></CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['nombre'], placeholder: 'Buscar red...' },
              filters: [
                { key: 'countryName', label: 'País',
                  options: countries.map(c => ({ value: c.name ?? c.Name, label: c.name ?? c.Name })) },
              ],
            }}
            columns={[
              { key: 'nombre',              label: 'Nombre', sortable: true, render: v => v },
              { key: 'countryName',         label: 'País' },
              { key: 'cantidadProfesores',  label: 'Cantidad profesores' },
              { key: 'tipo',                label: 'Tipo', render: v => TIPOS_RED.find(t => t.value === v)?.label ?? v },
            ]}
            data={items}
            keyExtractor={i => i.id}
            actions={[
              { key: 'edit',   label: 'Editar',          color: 'outline-secondary', onClick: i => openEdit(i) },
              { key: 'assign', label: 'Asignar eventos', color: 'outline-secondary', onClick: i => openAssign(i) },
              { key: 'delete', label: 'Eliminar',        color: 'outline-danger',    onClick: i => handleDelete(i.id) },
            ]}
            emptyMessage="No hay redes."
          />
        </CardBody>
      </Card>

      <Modal isOpen={modal} toggle={() => { setModal(false); setNewCountryOpen(false); setNewCountryName(''); }}>
        <Form onSubmit={handleSave}>
          <ModalHeader toggle={() => { setModal(false); setNewCountryOpen(false); setNewCountryName(''); }}>{editing ? 'Editar red' : 'Nueva red'}</ModalHeader>
          <ModalBody>
            {formError && <Alert color="danger">{formError}</Alert>}
            <FormGroup>
              <Label>Nombre *</Label>
              <Input value={form.nombre} onChange={e => setForm(f => ({ ...f, nombre: e.target.value }))} />
            </FormGroup>
            <FormGroup>
              <Label for="redTipo">Tipo *</Label>
              <Input type="select" id="redTipo" value={form.tipo} required
                onChange={e => {
                  const tipo = e.target.value;
                  setForm(f => {
                    const cuba = countries.find(c => (c.name ?? c.Name ?? '').toLowerCase() === 'cuba');
                    const countryId = (String(tipo) === '0' || String(tipo) === '1') && cuba
                      ? String(cuba.id ?? cuba.Id)
                      : f.countryId;
                    return { ...f, tipo, countryId };
                  });
                }}>
                {TIPOS_RED.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
              </Input>
            </FormGroup>
            <FormGroup>
              <Label for="redCountry">País *</Label>
              <Input type="select" id="redCountry" value={form.countryId ?? ''} required
                onChange={e => setForm(f => ({ ...f, countryId: e.target.value }))}>
                <option value="">— Selecciona un país —</option>
                {countries.map(c => <option key={c.id ?? c.Id} value={c.id ?? c.Id}>{c.name ?? c.Name}</option>)}
              </Input>
              {!newCountryOpen ? (
                <button type="button" className="btn btn-link btn-sm p-0 mt-1"
                  onClick={() => { setNewCountryOpen(true); setNewCountryError(''); }}>
                  <i className="bi bi-plus-circle me-1" />¿No está el país? Crear nuevo
                </button>
              ) : (
                <div className="mt-2 p-2 border rounded bg-light">
                  <small className="fw-semibold d-block mb-1">Nuevo país</small>
                  {newCountryError && <Alert color="danger" className="py-1 small">{newCountryError}</Alert>}
                  <div className="d-flex gap-2">
                    <Input
                      bsSize="sm"
                      placeholder="Nombre del país"
                      value={newCountryName}
                      onChange={e => setNewCountryName(e.target.value)}
                      onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); handleCreateCountry(); } }}
                      autoFocus
                    />
                    <Button size="sm" color="primary" onClick={handleCreateCountry} disabled={newCountrySaving || !newCountryName.trim()}>
                      {newCountrySaving ? <Spinner size="sm" /> : 'Crear'}
                    </Button>
                    <Button size="sm" color="secondary" outline onClick={() => { setNewCountryOpen(false); setNewCountryName(''); }}>
                      Cancelar
                    </Button>
                  </div>
                </div>
              )}
            </FormGroup>
            <FormGroup>
              <Label>Cantidad de profesores</Label>
              <Input type="number" value={form.cantidadProfesores} onChange={e => setForm(f => ({ ...f, cantidadProfesores: e.target.value }))} />
            </FormGroup>
            {String(form.tipo) === '0' && (
              <>
                <FormGroup>
                  <Label>Área coordinadora <span className="text-muted small">(se usa la del coordinador si no se selecciona)</span></Label>
                  <Input type="select" value={coordAreaId} onChange={e => setCoordAreaId(e.target.value)}>
                    <option value="">— Usar área del coordinador —</option>
                    {areas.map(a => <option key={a.id} value={a.id}>{a.nombre}</option>)}
                  </Input>
                </FormGroup>
                <FormGroup>
                  <Label>Coordinador</Label>
                  {selectedCoordUser ? (
                    <div>
                      <p className="text-muted small mb-1">Coordinador seleccionado — haz clic en la tarjeta para cambiarlo:</p>
                      <UserCard
                        user={selectedCoordUser}
                        isSelected
                        isOriginal={false}
                        onClick={() => { setCoordUserId(''); setCoordUserSearch(''); }}
                        clickTitle="Clic para cambiar coordinador"
                      />
                      {!coordAreaId && !selectedCoordUser.areaId && (
                        <Alert color="warning" className="mt-2 mb-0 py-2">
                          Este usuario no tiene área asignada en su perfil. Selecciona un área coordinadora en el campo de arriba.
                        </Alert>
                      )}
                    </div>
                  ) : (
                    <>
                      <Input
                        placeholder="Filtrar por nombre, apellido o correo..."
                        value={coordUserSearch}
                        onChange={e => setCoordUserSearch(e.target.value)}
                        className="mb-2"
                      />
                      {coordUserSearch.trim() && filteredCoordUsers.length === 0 && (
                        <p className="text-muted small text-center mb-0">Sin coincidencias.</p>
                      )}
                      {filteredCoordUsers.length > 0 && (
                        <div style={{ maxHeight: 240, overflowY: 'auto' }}>
                          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(190px, 1fr))', gap: '0.5rem' }}>
                            {filteredCoordUsers.map(u => (
                              <UserCard
                                key={u.id}
                                user={u}
                                isSelected={false}
                                isOriginal={false}
                                onClick={() => {
                                  setCoordUserId(u.id);
                                  if (!coordAreaId && u.areaId) setCoordAreaId(u.areaId);
                                }}
                                clickTitle="Clic para seleccionar como coordinador"
                              />
                            ))}
                          </div>
                        </div>
                      )}
                    </>
                  )}
                </FormGroup>
              </>
            )}
          </ModalBody>
          <ModalFooter>
            <Button color="secondary" outline onClick={() => openAssign(editing)} disabled={!editing || saving} className="me-auto">Asignar eventos</Button>
            <Button color="primary" type="submit" disabled={saving}>{saving ? <Spinner size="sm" /> : 'Guardar'}</Button>
            <Button color="secondary" onClick={() => { setModal(false); setNewCountryOpen(false); setNewCountryName(''); }}>Cancelar</Button>
          </ModalFooter>
        </Form>
      </Modal>

      <Modal isOpen={eventsModal} toggle={() => setEventsModal(false)} size="lg">
        <Form onSubmit={handleAssignSave}>
          <ModalHeader toggle={() => setEventsModal(false)}>{assigningRed ? `Asignar eventos — ${assigningRed.nombre ?? assigningRed.Nombre}` : 'Asignar eventos'}</ModalHeader>
          <ModalBody>
            {assignError && <Alert color="danger">{assignError}</Alert>}
            {eventsLoading ? (
              <div className="d-flex justify-content-center"><Spinner /></div>
            ) : (
              <Table responsive hover className="mb-0">
                <thead className="table-light"><tr><th style={{width: '40px'}}></th><th>Nombre</th></tr></thead>
                <tbody>
                  {availableEvents.length === 0 && <tr><td colSpan={2} className="text-center text-muted py-4">No hay eventos.</td></tr>}
                  {availableEvents.map(ev => {
                    const id = ev.id ?? ev.Id;
                    const name = ev.name ?? ev.Name;
                    const assigned = ev.assigned ?? ev.Assigned;
                    return (
                      <tr key={id}>
                        <td>
                          <Input type="checkbox" checked={selectedEventIds.has(id)} onChange={() => toggleSelectEvent(id)} />
                        </td>
                        <td>{name}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </Table>
            )}
          </ModalBody>
          <ModalFooter>
            <Button color="primary" type="submit" disabled={assignSaving}>{assignSaving ? <Spinner size="sm" /> : 'Guardar asignaciones'}</Button>
            <Button color="secondary" onClick={() => setEventsModal(false)}>Cancelar</Button>
          </ModalFooter>
        </Form>
      </Modal>
    </>
  );
}
