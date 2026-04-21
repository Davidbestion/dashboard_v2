import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  Card, CardBody, CardHeader,
  Nav, NavItem, NavLink as RNavLink,
  TabContent, TabPane,
  Table, Button, Badge,
  Spinner, Alert,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input, InputGroup,
} from 'reactstrap';

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

const EMPTY_PRES = { name: '', eventId: '' };
const EMPTY_EVENT = { name: '', countryId: '', eventType: '', institutions: [] };

const EVENT_TYPE_LABELS = { 0: 'Internacional', 1: 'Nacional', 2: 'De área', 3: 'Local' };

export default function EventsPage() {
  const [activeTab, setActiveTab] = useState('presentations');

  // Lookups
  const [countries, setCountries] = useState([]);
  const [eventTypes, setEventTypes] = useState([]);
  const [allEvents, setAllEvents] = useState([]);  // para el select del form de presentaciones

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

  // Coauthor tag-picker
  const [coauthorTags, setCoauthorTags] = useState([]);
  const [coauthorInput, setCoauthorInput] = useState('');
  const [coauthorSuggestions, setCoauthorSuggestions] = useState([]);
  const [suggestionsOpen, setSuggestionsOpen] = useState(false);
  const coauthorDebounce = useRef(null);

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
  const [instInput, setInstInput] = useState('');
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
  }, []);

  const loadPresentations = useCallback(async () => {
    setPresLoading(true);
    setPresError('');
    try {
      setPresentations(await apiFetch('/api/Presentations'));
    } catch (e) {
      setPresError(e.message);
    } finally {
      setPresLoading(false);
    }
  }, []);

  const loadEvents = useCallback(async () => {
    setEvLoading(true);
    setEvError('');
    try {
      setEvents(await apiFetch('/api/Events'));
    } catch (e) {
      setEvError(e.message);
    } finally {
      setEvLoading(false);
    }
  }, []);

  useEffect(() => {
    loadLookups();
    loadPresentations();
    loadEvents();
  }, [loadLookups, loadPresentations, loadEvents]);

  // ── Coauthor tag-picker ────────────────────────────────────────────────────

  function handleCoauthorInput(e) {
    const val = e.target.value;
    setCoauthorInput(val);
    clearTimeout(coauthorDebounce.current);
    if (val.trim().length < 2) { setCoauthorSuggestions([]); setSuggestionsOpen(false); return; }
    coauthorDebounce.current = setTimeout(async () => {
      try {
        const data = await apiFetch(`/api/Authors/search-coauthors?q=${encodeURIComponent(val.trim())}`);
        setCoauthorSuggestions(data);
        setSuggestionsOpen(data.length > 0);
      } catch { setSuggestionsOpen(false); }
    }, 300);
  }

  function addCoauthorFromSuggestion(s) {
    setCoauthorTags(prev => prev.some(t => t.id === s.id && t.type === s.type) ? prev : [...prev, s]);
    setCoauthorInput('');
    setSuggestionsOpen(false);
  }

  function addCoauthorFreeText() {
    const val = coauthorInput.trim();
    if (!val) return;
    setCoauthorTags(prev => [...prev, { id: null, name: val, type: 'new' }]);
    setCoauthorInput('');
    setSuggestionsOpen(false);
  }

  function removeCoauthor(idx) {
    setCoauthorTags(prev => prev.filter((_, i) => i !== idx));
  }

  function tagColor(type) {
    if (type === 'author') return 'primary';
    if (type === 'user') return 'success';
    return 'secondary';
  }

  // ── Presentations CRUD ─────────────────────────────────────────────────────

  function openCreatePres() {
    setPresEditing(null);
    setPresForm(EMPTY_PRES);
    setCoauthorTags([]);
    setCoauthorInput('');
    setPresFormError('');
    setPresModal(true);
  }

  function openEditPres(pres) {
    setPresEditing(pres);
    setPresForm({ name: pres.name, eventId: pres.eventId.toString() });
    setCoauthorTags(
      pres.authors.map(name => ({ id: null, name, type: 'new' }))
    );
    setCoauthorInput('');
    setPresFormError('');
    setPresModal(true);
  }

  async function handlePresSubmit(e) {
    e.preventDefault();
    setPresFormLoading(true);
    setPresFormError('');

    const coauthorIds = coauthorTags.filter(t => t.type === 'author').map(t => t.id);
    const coauthorNames = coauthorTags
      .filter(t => t.type === 'new' || t.type === 'user')
      .map(t => t.name);

    const body = {
      name: presForm.name.trim(),
      eventId: parseInt(presForm.eventId, 10),
      coauthorIds,
      coauthorNames,
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
    setInstInput('');
    setEvFormError('');
    resetNewCountry();
    setEvModal(true);
  }

  function openEditEv(ev) {
    setEvEditing(ev);
    setEvForm({
      name: ev.name,
      countryId: ev.countryId?.toString(),
      eventType: String(ev.eventTypeId ?? ev.eventType ?? ''),
      institutions: [...ev.institutions],
    });
    setInstInput('');
    setEvFormError('');
    resetNewCountry();
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

  function addInstitution() {
    const val = instInput.trim();
    if (!val) return;
    setEvForm(f => ({ ...f, institutions: [...f.institutions, val] }));
    setInstInput('');
  }

  function removeInstitution(idx) {
    setEvForm(f => ({ ...f, institutions: f.institutions.filter((_, i) => i !== idx) }));
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
                Mis presentaciones
              </RNavLink>
            </NavItem>
            <NavItem>
              <RNavLink
                active={activeTab === 'events'}
                onClick={() => setActiveTab('events')}
                style={{ cursor: 'pointer' }}
              >
                <i className="bi bi-calendar-event me-1" />
                Mis eventos
              </RNavLink>
            </NavItem>
          </Nav>
        </CardHeader>

        <CardBody>
          <TabContent activeTab={activeTab}>

            {/* ── PRESENTATIONS TAB ── */}
            <TabPane tabId="presentations">
              <div className="d-flex justify-content-end mb-3">
                <Button color="primary" size="sm" onClick={openCreatePres}>
                  <i className="bi bi-plus-lg me-1" />
                  Nueva presentación
                </Button>
              </div>

              {presLoading && <div className="text-center py-4"><Spinner color="primary" /></div>}
              {!presLoading && presError && <Alert color="danger">{presError}</Alert>}
              {!presLoading && !presError && presentations.length === 0 && (
                <p className="text-muted text-center py-3">No tienes presentaciones registradas.</p>
              )}
              {!presLoading && !presError && presentations.length > 0 && (
                <Table responsive hover>
                  <thead>
                    <tr>
                      <th>Nombre</th>
                      <th>Evento</th>
                      <th>Autores</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {presentations.map(p => (
                      <tr key={p.id}>
                        <td>{p.name}</td>
                        <td>{p.eventName}</td>
                        <td>
                          {p.authors.map((a, i) => (
                            <Badge key={i} color="secondary" pill className="me-1">{a}</Badge>
                          ))}
                        </td>
                        <td className="text-end">
                          <Button color="outline-secondary" size="sm" className="me-2"
                            onClick={() => openEditPres(p)}>
                            <i className="bi bi-pencil" />
                          </Button>
                          <Button color="outline-danger" size="sm"
                            onClick={() => { setPresToDelete(p); setPresDeleteError(''); setPresDeleteModal(true); }}>
                            <i className="bi bi-trash" />
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </Table>
              )}
            </TabPane>

            {/* ── EVENTS TAB ── */}
            <TabPane tabId="events">
              <div className="d-flex justify-content-end mb-3">
                <Button color="primary" size="sm" onClick={openCreateEv}>
                  <i className="bi bi-plus-lg me-1" />
                  Nuevo evento
                </Button>
              </div>

              {evLoading && <div className="text-center py-4"><Spinner color="primary" /></div>}
              {!evLoading && evError && <Alert color="danger">{evError}</Alert>}
              {!evLoading && !evError && events.length === 0 && (
                <p className="text-muted text-center py-3">No participas en ningún evento aún.</p>
              )}
              {!evLoading && !evError && events.length > 0 && (
                <Table responsive hover>
                  <thead>
                    <tr>
                      <th>Nombre</th>
                      <th>País</th>
                      <th>Tipo</th>
                      <th>Instituciones</th>
                      <th>Presentaciones</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {events.map(ev => (
                      <tr key={ev.id}>
                        <td>{ev.name}</td>
                        <td>{ev.countryName}</td>
                        <td><Badge color="info" pill>{EVENT_TYPE_LABELS[ev.eventTypeId] ?? ev.eventTypeName ?? ev.eventType}</Badge></td>
                        <td>
                          {ev.institutions.map((inst, i) => (
                            <Badge key={i} color="secondary" pill className="me-1">{inst}</Badge>
                          ))}
                        </td>
                        <td className="text-center">{ev.presentationCount}</td>
                        <td className="text-end">
                          <Button color="outline-secondary" size="sm" className="me-2"
                            onClick={() => openEditEv(ev)}>
                            <i className="bi bi-pencil" />
                          </Button>
                          <Button color="outline-danger" size="sm"
                            onClick={() => { setEvToDelete(ev); setEvDeleteError(''); setEvDeleteModal(true); }}>
                            <i className="bi bi-trash" />
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </Table>
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
              <Label>Coautores</Label>
              <div className="d-flex flex-wrap gap-1 mb-2">
                {coauthorTags.map((t, i) => (
                  <Badge key={i} color={tagColor(t.type)} className="d-flex align-items-center gap-1 py-1 px-2">
                    {t.type === 'author' && <i className="bi bi-person-check" />}
                    {t.type === 'user' && <i className="bi bi-person" />}
                    {t.type === 'new' && <i className="bi bi-person-plus" />}
                    {t.name}
                    <i className="bi bi-x" style={{ cursor: 'pointer' }} onClick={() => removeCoauthor(i)} />
                  </Badge>
                ))}
              </div>
              <div className="position-relative">
                <InputGroup>
                  <Input
                    placeholder="Buscar autor o escribir nombre nuevo..."
                    value={coauthorInput}
                    onChange={handleCoauthorInput}
                    onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); addCoauthorFreeText(); } }}
                  />
                  <Button type="button" color="secondary" outline onClick={addCoauthorFreeText}>
                    <i className="bi bi-plus" />
                  </Button>
                </InputGroup>
                {suggestionsOpen && (
                  <div className="position-absolute w-100 border rounded bg-white shadow-sm"
                    style={{ zIndex: 1050, top: '100%' }}>
                    {coauthorSuggestions.map(s => (
                      <div key={`${s.type}-${s.id}`}
                        className="px-3 py-2 hover-bg-light"
                        style={{ cursor: 'pointer' }}
                        onMouseDown={() => addCoauthorFromSuggestion(s)}>
                        <i className={`bi ${s.type === 'author' ? 'bi-person-check text-primary' : 'bi-person text-success'} me-2`} />
                        {s.name}
                      </div>
                    ))}
                  </div>
                )}
              </div>
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
              <InputGroup>
                <Input
                  placeholder="Nombre de institución..."
                  value={instInput}
                  onChange={e => setInstInput(e.target.value)}
                  onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); addInstitution(); } }}
                />
                <Button type="button" color="secondary" outline onClick={addInstitution}>
                  <i className="bi bi-plus" />
                </Button>
              </InputGroup>
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
