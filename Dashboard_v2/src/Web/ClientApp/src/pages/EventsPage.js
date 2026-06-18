import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Nav, NavItem, NavLink as RNavLink,
  TabContent, TabPane,
  Button, Badge,
  Spinner, Alert,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input, InputGroup,
} from 'reactstrap';
import { useAuth } from '../contexts/AuthContext';
import FilterableDataTable from '../components/FilterableDataTable';
import CertificateUpload, { CertificateViewButton } from '../components/CertificateUpload';
import UserPicker from '../components/UserPicker';

async function apiFetch(url, options = {}) {
  const response = await fetch(url, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...(options.headers ?? {}) },
    ...options,
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    const errs = data?.errors
      ? (Array.isArray(data.errors) ? data.errors : Object.values(data.errors).flat())
      : [data?.title ?? `Error ${response.status}`];
    throw new Error(errs.join(' '));
  }
  return data;
}

// ─── Shared helpers ────────────────────────────────────────────────────────────

function isoToDDMM(v) {
  const m = v && v.match(/^(\d{4})-(\d{2})-(\d{2})/);
  return m ? `${m[3]}/${m[2]}/${m[1]}` : '';
}
function ddmmToISO(v) {
  const m = v && v.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
  if (!m) return '';
  return `${m[3]}-${m[2].padStart(2, '0')}-${m[1].padStart(2, '0')}`;
}

const EMPTY_PRES = { name: '', eventId: '', fecha: '', targetUserId: '' };
const EMPTY_EVENT = { name: '', countryId: '', eventType: '', institutions: [], redId: '', organizadorIds: [], evidenceFileId: null };

const EVENT_TYPE_LABELS = { 0: 'Internacional', 1: 'Nacional', 2: 'De área', 3: 'Local' };

export default function EventsPage() {
  const { user } = useAuth();
  const isSuperuser = user?.role === 'Superuser';
  const isVicedecano = user?.role === 'Vicedecano_de_investigacion';
  const isManager = isSuperuser || isVicedecano;  // puede ver todos los eventos
  const [activeTab, setActiveTab] = useState(isVicedecano ? 'events' : 'presentations');
  const [generatingAnexo, setGeneratingAnexo] = useState(false);
  const [anexoError, setAnexoError] = useState('');

  // Lookups
  const [countries, setCountries] = useState([]);
  const [eventTypes, setEventTypes] = useState([]);
  const [allEvents, setAllEvents] = useState([]);  // para el select del form de presentaciones
  const [allInstitutions, setAllInstitutions] = useState([]);
  const [allUsers, setAllUsers] = useState([]);
  const [reds, setReds] = useState([]);

  // PRESENTATIONS state
  const [presentations, setPresentations] = useState([]);
  const [presLoading, setPresLoading] = useState(true);
  const [presError, setPresError] = useState('');
  const [presModal, setPresModal] = useState(false);
  const [presEditing, setPresEditing] = useState(null);
  const [presForm, setPresForm] = useState(EMPTY_PRES);
  const [presFormError, setPresFormError] = useState('');
  const [presFormLoading, setPresFormLoading] = useState(false);
  const [presDeleteModal, setPresDeleteModal] = useState(false);
  const [presToDelete, setPresToDelete] = useState(null);
  const [presDeleteLoading, setPresDeleteLoading] = useState(false);
  const [presDeleteError, setPresDeleteError] = useState('');

  // EVENTS state
  const [events, setEvents] = useState([]);
  const [evLoading, setEvLoading] = useState(true);
  const [evError, setEvError] = useState('');
  const [evModal, setEvModal] = useState(false);
  const [evEditing, setEvEditing] = useState(null);
  const [evForm, setEvForm] = useState(EMPTY_EVENT);
  const [evFormError, setEvFormError] = useState('');
  const [evFormLoading, setEvFormLoading] = useState(false);
  const [evDeleteModal, setEvDeleteModal] = useState(false);
  const [evToDelete, setEvToDelete] = useState(null);
  const [evDeleteLoading, setEvDeleteLoading] = useState(false);
  const [evDeleteError, setEvDeleteError] = useState('');
  // Institution input
  const [selectedInstId, setSelectedInstId] = useState('');
  // Organizador input
  const [selectedOrganizadorId, setSelectedOrganizadorId] = useState('');
  // Inline new-institution
  const [showNewInstitution, setShowNewInstitution] = useState(false);
  const [newInstitutionInput, setNewInstitutionInput] = useState('');
  const [newInstitutionLoading, setNewInstitutionLoading] = useState(false);
  const [newInstitutionError, setNewInstitutionError] = useState('');
  // Inline new-country
  const [newCountryInput, setNewCountryInput] = useState('');
  const [showNewCountry, setShowNewCountry] = useState(false);
  const [newCountryLoading, setNewCountryLoading] = useState(false);
  const [newCountryError, setNewCountryError] = useState('');

  // ── Load data ──────────────────────────────────────────────────────────────

  const loadLookups = useCallback(async () => {
    const [c, et, ae] = await Promise.all([
      apiFetch('/api/Events/countries').catch(() => []),
      apiFetch('/api/Events/types').catch(() => []),
      apiFetch('/api/Events/all').catch(() => []),
    ]);
    setCountries(c);
    setEventTypes(et);
    setAllEvents(ae);
    // Institutions for event form dropdown
    try { setAllInstitutions(await apiFetch('/api/Institutions')); } catch { setAllInstitutions([]); }
    // Users for organizador picker
    try { setAllUsers(await apiFetch('/api/Users')); } catch { setAllUsers([]); }
    // Reds for event selection (optional)
    try { setReds(await apiFetch('/api/Redes')); } catch { setReds([]); }
  }, []);

  const loadPresentations = useCallback(async () => {
    setPresLoading(true);
    setPresError('');
    try {
      const url = isSuperuser
        ? '/api/Presentations/all'
        : isVicedecano
          ? '/api/Presentations/area'
          : '/api/Presentations';
      setPresentations(await apiFetch(url));
    } catch (e) {
      setPresError(e.message);
    } finally {
      setPresLoading(false);
    }
  }, [isSuperuser, isVicedecano]);

  const loadEvents = useCallback(async () => {
    setEvLoading(true);
    setEvError('');
    try {
      setEvents(await apiFetch(isManager ? '/api/Events/all' : '/api/Events'));
    } catch (e) {
      setEvError(e.message);
    } finally {
      setEvLoading(false);
    }
  }, [isManager]);

  useEffect(() => {
    loadLookups();
    loadPresentations();
    loadEvents();
  }, [loadLookups, loadPresentations, loadEvents]);

  async function handleGenerateAnexo() {
    setGeneratingAnexo(true);
    setAnexoError('');
    try {
      const response = await fetch('/api/Documents/anexo-eventos', { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => ({}));
        const message = data?.error ?? data?.title ?? 'No se pudo generar el anexo.';
        throw new Error(message);
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'anexo-eventos.xlsx';
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

  // ── Presentations CRUD ─────────────────────────────────────────────────────

  function openCreatePres() {
    setPresEditing(null);
    const d = new Date();
    const todayDDMM = `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`;
    setPresForm({ ...EMPTY_PRES, fecha: todayDDMM });
    setPresFormError('');
    setPresModal(true);
  }

  function openEditPres(pres) {
    setPresEditing(pres);
    setPresForm({
      name: pres.name,
      eventId: pres.eventId.toString(),
      fecha: isoToDDMM(pres.fecha ? pres.fecha.substring(0, 10) : ''),
    });
    setPresFormError('');
    setPresModal(true);
  }

  async function handlePresSubmit(e) {
    e.preventDefault();
    if (isSuperuser && !presEditing && !presForm.targetUserId) {
      setPresFormError('Debes seleccionar el usuario presentador.');
      return;
    }
    setPresFormLoading(true);
    setPresFormError('');

    const body = {
      name: presForm.name.trim(),
      eventId: parseInt(presForm.eventId, 10),
      fecha: ddmmToISO(presForm.fecha) || new Date().toISOString().substring(0, 10),
      ...(isSuperuser && !presEditing ? { targetUserId: presForm.targetUserId } : {}),
    };

    try {
      if (presEditing) {
        await apiFetch(`/api/Presentations/${presEditing.id}`, { method: 'PUT', body: JSON.stringify(body) });
      } else {
        await apiFetch('/api/Presentations', { method: 'POST', body: JSON.stringify(body) });
      }
      setPresModal(false);
      loadPresentations();
      loadEvents();
    } catch (e) {
      setPresFormError(e.message);
    } finally {
      setPresFormLoading(false);
    }
  }

  async function confirmDeletePres() {
    if (!presToDelete) return;
    setPresDeleteLoading(true);
    setPresDeleteError('');
    try {
      await apiFetch(`/api/Presentations/${presToDelete.id}`, { method: 'DELETE' });
      setPresDeleteModal(false);
      setPresToDelete(null);
      loadPresentations();
      loadEvents();
    } catch (e) {
      setPresDeleteError(e.message);
    } finally {
      setPresDeleteLoading(false);
    }
  }

  // ── Events CRUD ────────────────────────────────────────────────────────────

  function resetNewCountry() {
    setShowNewCountry(false);
    setNewCountryInput('');
    setNewCountryError('');
  }

  function openCreateEv() {
    setEvEditing(null);
    setEvForm(EMPTY_EVENT);
    setSelectedInstId('');
    setSelectedOrganizadorId('');
    setEvFormError('');
    resetNewCountry();
    setShowNewInstitution(false);
    setNewInstitutionInput('');
    setNewInstitutionError('');
    setEvModal(true);
  }

  function openEditEv(ev) {
    setEvEditing(ev);
    setEvForm({
      name: ev.name,
      countryId: ev.countryId?.toString(),
      eventType: String(ev.eventTypeId ?? ev.eventType ?? ''),
      institutions: [...ev.institutions],
      redId: ev.redId ?? ev.RedId ?? '',
      organizadorIds: [...(ev.organizadorIds ?? [])],
      evidenceFileId: ev.evidenceFileId ?? null,
    });
    setSelectedInstId('');
    setSelectedOrganizadorId('');
    setEvFormError('');
    resetNewCountry();
    setShowNewInstitution(false);
    setNewInstitutionInput('');
    setNewInstitutionError('');
    setEvModal(true);
  }

  async function handleCreateCountry() {
    const name = newCountryInput.trim();
    if (!name) return;
    setNewCountryLoading(true);
    setNewCountryError('');
    try {
      const created = await apiFetch('/api/Events/countries', {
        method: 'POST',
        body: JSON.stringify({ name }),
      });
      setCountries(prev => [...prev, created].sort((a, b) => a.name.localeCompare(b.name)));
      setEvForm(f => ({ ...f, countryId: created.id.toString() }));
      resetNewCountry();
    } catch (e) {
      setNewCountryError(e.message);
    } finally {
      setNewCountryLoading(false);
    }
  }

  async function handleCreateInstitution() {
    const name = newInstitutionInput.trim();
    if (!name) return;
    setNewInstitutionLoading(true);
    setNewInstitutionError('');
    try {
      const created = await apiFetch('/api/Institutions', { method: 'POST', body: JSON.stringify({ nombre: name }) });
      setAllInstitutions(prev => [...prev, created].sort((a, b) => a.nombre.localeCompare(b.nombre)));
      setEvForm(f => ({ ...f, institutions: [...f.institutions, created.nombre] }));
      setNewInstitutionInput('');
      setShowNewInstitution(false);
    } catch (e) {
      setNewInstitutionError(e.message);
    } finally {
      setNewInstitutionLoading(false);
    }
  }

  function addSelectedInstitution() {
    if (!selectedInstId) return;
    const inst = allInstitutions.find(i => i.id === selectedInstId);
    if (!inst) return;
    if (evForm.institutions.includes(inst.nombre)) return;
    setEvForm(f => ({ ...f, institutions: [...f.institutions, inst.nombre] }));
    setSelectedInstId('');
  }

  function removeInstitution(idx) {
    setEvForm(f => ({ ...f, institutions: f.institutions.filter((_, i) => i !== idx) }));
  }

  function addSelectedOrganizador() {
    if (!selectedOrganizadorId) return;
    if (evForm.organizadorIds.includes(selectedOrganizadorId)) return;
    setEvForm(f => ({ ...f, organizadorIds: [...f.organizadorIds, selectedOrganizadorId] }));
    setSelectedOrganizadorId('');
  }

  function removeOrganizador(id) {
    setEvForm(f => ({ ...f, organizadorIds: f.organizadorIds.filter(x => x !== id) }));
  }

  async function handleEvSubmit(e) {
    e.preventDefault();
    setEvFormLoading(true);
    setEvFormError('');

    const body = {
      name: evForm.name.trim(),
      countryId: parseInt(evForm.countryId, 10),
      eventType: parseInt(evForm.eventType, 10),
      institutions: evForm.institutions,
      redId: evForm.redId && evForm.redId.length > 0 ? evForm.redId : null,
      organizadorIds: evForm.organizadorIds,
      evidenceFileId: evForm.evidenceFileId ?? null,
    };

    try {
      if (evEditing) {
        await apiFetch(`/api/Events/${evEditing.id}`, { method: 'PUT', body: JSON.stringify(body) });
      } else {
        await apiFetch('/api/Events', { method: 'POST', body: JSON.stringify(body) });
      }
      setEvModal(false);
      loadEvents();
      loadLookups(); // refresca allEvents para el selector de presentaciones
    } catch (e) {
      setEvFormError(e.message);
    } finally {
      setEvFormLoading(false);
    }
  }

  async function confirmDeleteEv() {
    if (!evToDelete) return;
    setEvDeleteLoading(true);
    setEvDeleteError('');
    try {
      await apiFetch(`/api/Events/${evToDelete.id}`, { method: 'DELETE' });
      setEvDeleteModal(false);
      setEvToDelete(null);
      loadEvents();
      loadLookups(); // refresca allEvents
    } catch (e) {
      setEvDeleteError(e.message);
    } finally {
      setEvDeleteLoading(false);
    }
  }

  // ── Render ────────────────────────────────────────────────────────────────

  return (
    <>
      <Card>
        <CardHeader>
          <Nav tabs className="card-header-tabs">
            <NavItem>
              <RNavLink
                active={activeTab === 'presentations'}
                onClick={() => setActiveTab('presentations')}
                style={{ cursor: 'pointer' }}
              >
                <i className="bi bi-mic me-1" />
                {isSuperuser
                  ? 'Presentaciones registradas'
                  : isVicedecano
                    ? 'Presentaciones del Área'
                    : 'Mis presentaciones'}
              </RNavLink>
            </NavItem>
            <NavItem>
              <RNavLink
                active={activeTab === 'events'}
                onClick={() => setActiveTab('events')}
                style={{ cursor: 'pointer' }}
              >
                <i className="bi bi-calendar-event me-1" />
                {isManager ? 'Eventos registrados' : 'Mis eventos'}
              </RNavLink>
            </NavItem>
          </Nav>
        </CardHeader>

        <CardBody>
          {(isSuperuser || isVicedecano) && (
            <div className="d-flex justify-content-end mb-3">
              <Button color="success" size="sm" onClick={handleGenerateAnexo} disabled={generatingAnexo}>
                {generatingAnexo ? <Spinner size="sm" /> : '⬇ Generar Anexo 3'}
              </Button>
            </div>
          )}
          {anexoError && <Alert color="danger">{anexoError}</Alert>}

          <TabContent activeTab={activeTab}>

            {/* ── PRESENTATIONS TAB ── */}
            <TabPane tabId="presentations">
              {!isVicedecano && (
                <div className="d-flex justify-content-end mb-3">
                  <Button color="primary" size="sm" onClick={openCreatePres}>
                    <i className="bi bi-plus-lg me-1" />
                    Nueva presentación
                  </Button>
                </div>
              )}

              {presLoading && <div className="text-center py-4"><Spinner color="primary" /></div>}
              {!presLoading && presError && <Alert color="danger">{presError}</Alert>}
              {!presLoading && !presError && (
                <FilterableDataTable
                  filterConfig={{
                    search: { fields: ['name', 'eventName'], placeholder: 'Buscar presentación...' },
                  }}
                  loading={presLoading}
                  columns={[
                    { key: 'name',      label: 'Nombre', sortable: true },
                    { key: 'eventName', label: 'Evento', sortable: true },
                    {
                      key: 'fecha',
                      label: 'Fecha',
                      sortable: true,
                      render: v => {
                        if (!v) return '—';
                        const [y, m, d] = String(v).substring(0, 10).split('-');
                        return `${d}/${m}/${y}`;
                      },
                    },
                    {
                      key: 'user',
                      label: 'Presentador',
                      render: u => u ? `${u.userName ?? ''} ${u.userLastName1 ?? ''}`.trim() : '—',
                    },
                  ]}
                  data={presentations}
                  keyExtractor={p => p.id}
                  actions={isVicedecano ? [] : [
                    { key: 'edit',   label: 'Editar',   icon: 'bi-pencil', color: 'outline-secondary', onClick: p => openEditPres(p) },
                    { key: 'delete', label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',    onClick: p => { setPresToDelete(p); setPresDeleteError(''); setPresDeleteModal(true); } },
                  ]}
                  emptyMessage={
                    isSuperuser ? 'No hay presentaciones registradas.'
                    : isVicedecano ? 'No hay presentaciones registradas en el área.'
                    : 'No tienes presentaciones registradas.'
                  }
                  detailConfig
                />
              )}
            </TabPane>

            {/* ── EVENTS TAB ── */}
            <TabPane tabId="events">
              {!isVicedecano && (
                <div className="d-flex justify-content-end mb-3">
                  <Button color="primary" size="sm" onClick={openCreateEv}>
                    <i className="bi bi-plus-lg me-1" />
                    Nuevo evento
                  </Button>
                </div>
              )}

              {evLoading && <div className="text-center py-4"><Spinner color="primary" /></div>}
              {!evLoading && evError && <Alert color="danger">{evError}</Alert>}
              {!evLoading && !evError && (
                <FilterableDataTable
                  filterConfig={{
                    search: { fields: ['name', 'countryName'], placeholder: 'Buscar evento...' },
                    filters: [
                      { key: 'eventTypeId', label: 'Tipo',
                        options: Object.entries(EVENT_TYPE_LABELS).map(([k, v]) => ({ value: k, label: v })),
                        match: (item, val) => String(item.eventTypeId) === val },
                    ],
                  }}
                  loading={evLoading}
                  columns={[
                    { key: 'name',        label: 'Nombre', sortable: true },
                    { key: 'countryName', label: 'País',  sortable: true },
                    {
                      key: 'eventTypeId',
                      label: 'Tipo',
                      render: (v, ev) => <Badge color="info" pill>{EVENT_TYPE_LABELS[v] ?? ev.eventTypeName ?? ev.eventType}</Badge>,
                    },
                    {
                      key: 'institutions',
                      label: 'Instituciones',
                      render: insts => (insts || []).map((inst, i) => (
                        <Badge key={i} color="secondary" pill className="me-1">{inst}</Badge>
                      )),
                    },
                    { key: 'redName',          label: 'Red' },
                    { key: 'presentationCount', label: 'Presentaciones' },
                  ]}
                  data={events}
                  keyExtractor={ev => ev.id}
                  actions={[
                    { key: 'edit',   label: 'Editar',   icon: 'bi-pencil', color: 'outline-secondary', show: () => !isVicedecano, onClick: ev => openEditEv(ev) },
                    { key: 'delete', label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',    show: () => !isVicedecano, onClick: ev => { setEvToDelete(ev); setEvDeleteError(''); setEvDeleteModal(true); } },
                    { key: 'certificate', label: 'Certificado', show: ev => ev.evidenceFileId != null,
                      render: ev => <CertificateViewButton fileId={ev.evidenceFileId} /> },
                  ]}
                  emptyMessage={isManager ? 'No hay eventos registrados.' : 'No participas en ningún evento aún.'}
                  detailConfig
                />
              )}
            </TabPane>
          </TabContent>
        </CardBody>
      </Card>

      {/* ── PRESENTATION MODAL ── */}
      <Modal isOpen={presModal} toggle={() => setPresModal(false)} size="lg">
        <Form onSubmit={handlePresSubmit}>
          <ModalHeader toggle={() => setPresModal(false)}>
            {presEditing ? 'Editar presentación' : 'Nueva presentación'}
          </ModalHeader>
          <ModalBody>
            {presFormError && <Alert color="danger">{presFormError}</Alert>}

            {isSuperuser && !presEditing && (
              <FormGroup>
                <Label>Usuario presentador <span className="text-danger">*</span></Label>
                <UserPicker
                  users={allUsers}
                  value={presForm.targetUserId}
                  onChange={id => setPresForm(f => ({ ...f, targetUserId: id }))}
                />
              </FormGroup>
            )}

            <FormGroup>
              <Label for="presName">Nombre *</Label>
              <Input id="presName" value={presForm.name} required
                onChange={e => setPresForm(f => ({ ...f, name: e.target.value }))}
                placeholder="Título de la presentación" />
            </FormGroup>

            <FormGroup>
              <Label for="presEvent">Evento *</Label>
              <Input type="select" id="presEvent" value={presForm.eventId} required
                onChange={e => setPresForm(f => ({ ...f, eventId: e.target.value }))}>
                <option value="">— Selecciona un evento —</option>
                {allEvents.map(ev => (
                  <option key={ev.id} value={ev.id}>{ev.name}</option>
                ))}
              </Input>
              <small className="text-muted">
                Si el evento no existe, créalo primero en la pestaña "Mis eventos".
              </small>
            </FormGroup>

            <FormGroup>
              <Label for="presFecha">Fecha *</Label>
              <Input type="text" id="presFecha" value={presForm.fecha}
                onChange={e => setPresForm(f => ({ ...f, fecha: e.target.value }))}
                placeholder="DD/MM/AAAA" maxLength={10} />
            </FormGroup>
          </ModalBody>
          <ModalFooter>
            <Button color="secondary" onClick={() => setPresModal(false)} disabled={presFormLoading}>Cancelar</Button>
            <Button color="primary" type="submit" disabled={presFormLoading}>
              {presFormLoading ? <Spinner size="sm" /> : (presEditing ? 'Guardar cambios' : 'Crear')}
            </Button>
          </ModalFooter>
        </Form>
      </Modal>

      {/* ── PRESENTATION DELETE MODAL ── */}
      <Modal isOpen={presDeleteModal} toggle={() => setPresDeleteModal(false)}>
        <ModalHeader toggle={() => setPresDeleteModal(false)}>Eliminar presentación</ModalHeader>
        <ModalBody>
          {presDeleteError && <Alert color="danger">{presDeleteError}</Alert>}
          <p>¿Eliminar <strong>{presToDelete?.name}</strong>?</p>
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={() => setPresDeleteModal(false)} disabled={presDeleteLoading}>Cancelar</Button>
          <Button color="danger" onClick={confirmDeletePres} disabled={presDeleteLoading}>
            {presDeleteLoading ? <Spinner size="sm" /> : 'Eliminar'}
          </Button>
        </ModalFooter>
      </Modal>

      {/* ── EVENT MODAL ── */}
      <Modal isOpen={evModal} toggle={() => setEvModal(false)} size="lg">
        <Form onSubmit={handleEvSubmit}>
          <ModalHeader toggle={() => setEvModal(false)}>
            {evEditing ? 'Editar evento' : 'Nuevo evento'}
          </ModalHeader>
          <ModalBody>
            {evFormError && <Alert color="danger">{evFormError}</Alert>}

            <FormGroup>
              <Label for="evName">Nombre *</Label>
              <Input id="evName" value={evForm.name} required
                onChange={e => setEvForm(f => ({ ...f, name: e.target.value }))}
                placeholder="Nombre del evento científico" />
            </FormGroup>

            <FormGroup>
              <Label for="evCountry">País *</Label>
              <div className="d-flex gap-2 align-items-center">
                <Input type="select" id="evCountry" value={evForm.countryId} required
                  onChange={e => setEvForm(f => ({ ...f, countryId: e.target.value }))}>
                  <option value="">— Selecciona un país —</option>
                  {countries.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                </Input>
                <Button type="button" color="secondary" outline size="sm" style={{ whiteSpace: 'nowrap' }}
                  onClick={() => { setShowNewCountry(v => !v); setNewCountryError(''); }}>
                  <i className="bi bi-plus" /> Nuevo
                </Button>
              </div>
              {showNewCountry && (
                <div className="mt-2">
                  {newCountryError && <Alert color="danger" className="py-1 px-2 small mb-1">{newCountryError}</Alert>}
                  <InputGroup size="sm">
                    <Input
                      placeholder="Nombre del país..."
                      value={newCountryInput}
                      onChange={e => setNewCountryInput(e.target.value)}
                      onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); handleCreateCountry(); } }}
                      autoFocus
                    />
                    <Button type="button" color="primary" onClick={handleCreateCountry} disabled={newCountryLoading}>
                      {newCountryLoading ? <Spinner size="sm" /> : 'Crear'}
                    </Button>
                    <Button type="button" color="secondary" outline onClick={resetNewCountry}>✕</Button>
                  </InputGroup>
                </div>
              )}
            </FormGroup>

            <FormGroup>
              <Label for="evType">Tipo de evento *</Label>
              <Input type="select" id="evType" value={evForm.eventType} required
                onChange={e => setEvForm(f => ({ ...f, eventType: e.target.value }))}>
                <option value="">-- seleccionar --</option>
                {eventTypes.map(et => <option key={et.id} value={et.id}>{EVENT_TYPE_LABELS[et.id] ?? et.name}</option>)}
              </Input>
            </FormGroup>

            <FormGroup>
              <Label>Instituciones organizadoras</Label>
              <div className="d-flex flex-wrap gap-1 mb-2">
                {evForm.institutions.map((inst, i) => (
                  <Badge key={i} color="secondary" className="d-flex align-items-center gap-1 py-1 px-2">
                    {inst}
                    <i className="bi bi-x" style={{ cursor: 'pointer' }} onClick={() => removeInstitution(i)} />
                  </Badge>
                ))}
              </div>
              <div className="d-flex gap-2 align-items-center">
                <Input type="select" id="evInstitution" value={selectedInstId}
                  onChange={e => setSelectedInstId(e.target.value)}>
                  <option value="">— Selecciona una institución —</option>
                  {allInstitutions
                    .filter(i => !evForm.institutions.includes(i.nombre))
                    .map(i => <option key={i.id} value={i.id}>{i.nombre}</option>)}
                </Input>
                <Button type="button" color="secondary" outline size="sm" style={{ whiteSpace: 'nowrap' }}
                  onClick={addSelectedInstitution} disabled={!selectedInstId}>
                  <i className="bi bi-plus" /> Agregar
                </Button>
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

            <FormGroup>
              <Label>Organizadores</Label>
              <div className="d-flex flex-wrap gap-1 mb-2">
                {evForm.organizadorIds.map(uid => {
                  const u = allUsers.find(x => x.id === uid);
                  return (
                    <Badge key={uid} color="primary" className="d-flex align-items-center gap-1 py-1 px-2">
                      {u ? `${u.nombreCompleto} (${u.email})` : uid}
                      <i className="bi bi-x" style={{ cursor: 'pointer' }} onClick={() => removeOrganizador(uid)} />
                    </Badge>
                  );
                })}
              </div>
              <div className="d-flex gap-2 align-items-center">
                <Input type="select" value={selectedOrganizadorId}
                  onChange={e => setSelectedOrganizadorId(e.target.value)}>
                  <option value="">— Selecciona un organizador —</option>
                  {allUsers
                    .filter(u => !evForm.organizadorIds.includes(u.id))
                    .map(u => <option key={u.id} value={u.id}>{u.nombreCompleto} ({u.email})</option>)}
                </Input>
                <Button type="button" color="secondary" outline size="sm" style={{ whiteSpace: 'nowrap' }}
                  onClick={addSelectedOrganizador} disabled={!selectedOrganizadorId}>
                  <i className="bi bi-plus" /> Agregar
                </Button>
              </div>
            </FormGroup>

            <FormGroup>
              <Label for="evRed">Red coordinadora (opcional)</Label>
              <Input type="select" id="evRed" value={evForm.redId ?? ''}
                onChange={e => setEvForm(f => ({ ...f, redId: e.target.value }))}>
                <option value="">— Ninguna —</option>
                {reds.map(r => (
                  <option key={r.id ?? r.Id} value={r.id ?? r.Id}>{r.nombre ?? r.Nombre}</option>
                ))}
              </Input>
              <small className="text-muted">Selecciona una red coordinadora si aplica.</small>
            </FormGroup>

            <FormGroup>
              <Label>Certificado / Evidencia</Label>
              <CertificateUpload
                fileId={evForm.evidenceFileId}
                onFileIdChange={id => setEvForm(f => ({ ...f, evidenceFileId: id }))}
                canManage
                canView
                disabled={evFormLoading}
              />
            </FormGroup>
          </ModalBody>
          <ModalFooter>
            <Button color="secondary" onClick={() => setEvModal(false)} disabled={evFormLoading}>Cancelar</Button>
            <Button color="primary" type="submit" disabled={evFormLoading}>
              {evFormLoading ? <Spinner size="sm" /> : (evEditing ? 'Guardar cambios' : 'Crear')}
            </Button>
          </ModalFooter>
        </Form>
      </Modal>

      {/* ── EVENT DELETE MODAL ── */}
      <Modal isOpen={evDeleteModal} toggle={() => setEvDeleteModal(false)}>
        <ModalHeader toggle={() => setEvDeleteModal(false)}>Eliminar evento</ModalHeader>
        <ModalBody>
          {evDeleteError && <Alert color="danger">{evDeleteError}</Alert>}
          <p>¿Eliminar <strong>{evToDelete?.name}</strong>?</p>
          <p className="text-muted small">
            Solo es posible si el evento no tiene presentaciones registradas.
          </p>
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={() => setEvDeleteModal(false)} disabled={evDeleteLoading}>Cancelar</Button>
          <Button color="danger" onClick={confirmDeleteEv} disabled={evDeleteLoading}>
            {evDeleteLoading ? <Spinner size="sm" /> : 'Eliminar'}
          </Button>
        </ModalFooter>
      </Modal>
    </>
  );
}
