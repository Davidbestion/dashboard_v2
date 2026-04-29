import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Table, Button, Spinner, Alert,
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
    const errors = data?.errors ?? ['Error desconocido.'];
    throw new Error(Array.isArray(errors) ? errors.join(' ') : String(errors));
  }
  return data;
}

const emptyForm = { nombre: '', countryId: '', cantidadProfesores: 0 };

export default function RedesPage() {
  const [items, setItems] = useState([]);
  const [countries, setCountries] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');
  const [eventsModal, setEventsModal] = useState(false);
  const [assigningRed, setAssigningRed] = useState(null);
  const [availableEvents, setAvailableEvents] = useState([]);
  const [eventsLoading, setEventsLoading] = useState(false);
  const [selectedEventIds, setSelectedEventIds] = useState(new Set());
  const [assignError, setAssignError] = useState('');
  const [assignSaving, setAssignSaving] = useState(false);

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try {
      const [reds, cs] = await Promise.all([
        apiFetch('/api/Redes').catch(() => []),
        apiFetch('/api/Events/countries').catch(() => []),
      ]);
      setItems(reds);
      setCountries(cs);
    } catch (e) { setError(e.message); } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  function openCreate() { setEditing(null); setForm(emptyForm); setFormError(''); setModal(true); }
  function openEdit(it) { setEditing(it); setForm({ nombre: it.nombre ?? it.Nombre, countryId: (it.countryId ?? it.CountryId ?? '')?.toString?.() ?? '', cantidadProfesores: it.cantidadProfesores ?? it.CantidadProfesores }); setFormError(''); setModal(true); }

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

  async function handleSave(e) {
    if (e) e.preventDefault(); setSaving(true); setFormError('');
    const body = { nombre: form.nombre, countryId: form.countryId ? parseInt(form.countryId, 10) : null, cantidadProfesores: parseInt(form.cantidadProfesores, 10) };
    try {
      if (editing) await apiFetch(`/api/Redes/${editing.id}`, { method: 'PUT', body: JSON.stringify(body) });
      else await apiFetch('/api/Redes', { method: 'POST', body: JSON.stringify(body) });
      setModal(false); await load();
    } catch (e) { setFormError(e.message); } finally { setSaving(false); }
  }

  async function handleDelete(id) { if (!window.confirm('¿Eliminar esta red?')) return; try { await apiFetch(`/api/Redes/${id}`, { method: 'DELETE' }); await load(); } catch (e) { setError(e.message); } }

  if (loading) return <div className="d-flex justify-content-center mt-5"><Spinner color="primary" /></div>;

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="mb-0">Redes</h2>
        <Button color="primary" onClick={openCreate}>+ Nueva red</Button>
      </div>

      {error && <Alert color="danger">{error}</Alert>}

      <Card>
        <CardHeader><strong>Redes</strong> <small className="text-muted ms-2">({items.length})</small></CardHeader>
        <CardBody className="p-0">
          <Table responsive hover className="mb-0">
            <thead className="table-light"><tr><th>Nombre</th><th>País</th><th>Cantidad profesores</th><th className="text-end">Acciones</th></tr></thead>
            <tbody>
              {items.length === 0 && <tr><td colSpan={4} className="text-center text-muted py-4">No hay redes.</td></tr>}
              {items.map(i => (
                <tr key={i.id}><td>{i.nombre ?? i.Nombre}</td><td>{i.countryName ?? i.CountryName ?? ''}</td><td>{i.cantidadProfesores ?? i.CantidadProfesores}</td>
                  <td className="text-end">
                    <Button size="sm" color="outline-secondary" className="me-2" onClick={() => openEdit(i)}>Editar</Button>
                    <Button size="sm" color="outline-secondary" className="me-2" onClick={() => openAssign(i)}>Asignar eventos</Button>
                    <Button size="sm" color="outline-danger" onClick={() => handleDelete(i.id)}>Eliminar</Button>
                  </td></tr>
              ))}
            </tbody>
          </Table>
        </CardBody>
      </Card>

      <Modal isOpen={modal} toggle={() => setModal(false)}>
        <Form onSubmit={handleSave}>
          <ModalHeader toggle={() => setModal(false)}>{editing ? 'Editar red' : 'Nueva red'}</ModalHeader>
          <ModalBody>
            {formError && <Alert color="danger">{formError}</Alert>}
            <FormGroup>
              <Label>Nombre *</Label>
              <Input value={form.nombre} onChange={e => setForm(f => ({ ...f, nombre: e.target.value }))} />
            </FormGroup>
            <FormGroup>
              <Label for="redCountry">País *</Label>
              <Input type="select" id="redCountry" value={form.countryId ?? ''} required
                onChange={e => setForm(f => ({ ...f, countryId: e.target.value }))}>
                <option value="">— Selecciona un país —</option>
                {countries.map(c => <option key={c.id ?? c.Id} value={c.id ?? c.Id}>{c.name ?? c.Name}</option>)}
              </Input>
            </FormGroup>
            <FormGroup>
              <Label>Cantidad de profesores</Label>
              <Input type="number" value={form.cantidadProfesores} onChange={e => setForm(f => ({ ...f, cantidadProfesores: e.target.value }))} />
            </FormGroup>
          </ModalBody>
          <ModalFooter>
            <Button color="secondary" outline onClick={() => openAssign(editing)} disabled={!editing || saving} className="me-auto">Asignar eventos</Button>
            <Button color="primary" type="submit" disabled={saving}>{saving ? <Spinner size="sm" /> : 'Guardar'}</Button>
            <Button color="secondary" onClick={() => setModal(false)}>Cancelar</Button>
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
