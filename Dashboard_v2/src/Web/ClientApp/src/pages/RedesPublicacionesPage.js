import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Button, Spinner, Alert, Badge, Table,
  Modal, ModalHeader, ModalBody, ModalFooter,
  FormGroup, Label, Input,
} from 'reactstrap';
import { CertificateViewButton } from '../components/CertificateUpload';
import { useAuth } from '../contexts/AuthContext';

const PUB_TIPOS = ['Diario', 'Libro', 'Monografía', 'Capítulo', 'Artículo de Divulgación'];
const TIPO_RED_LABELS = ['Universitaria', 'Nacional', 'Internacional'];
const TIPO_RED_COLORS = ['primary', 'success', 'warning'];

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

export default function RedesPublicacionesPage() {
  const { user } = useAuth();
  const isJefe = user?.role === 'Jefe_de_Redes' || user?.role === 'Superuser';

  const [redes, setRedes] = useState([]);
  const [publications, setPublications] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [selectedRedId, setSelectedRedId] = useState(null);

  // Modal asignar coordinador
  const [coordModal, setCoordModal] = useState(false);
  const [coordUserId, setCoordUserId] = useState('');
  const [coordUserSearch, setCoordUserSearch] = useState('');
  const [coordUserResults, setCoordUserResults] = useState([]);
  const [coordUserSearching, setCoordUserSearching] = useState(false);
  const [coordLoading, setCoordLoading] = useState(false);
  const [coordError, setCoordError] = useState('');
  const [coordSuccess, setCoordSuccess] = useState('');

  const loadData = useCallback(async () => {
    setLoading(true); setError('');
    try {
      const [redsData, pubsData] = await Promise.all([
        apiFetch('/api/Redes/mis-redes'),
        apiFetch('/api/Publications/redes'),
      ]);
      setRedes(redsData ?? []);
      setPublications(pubsData ?? []);
      setSelectedRedId(prev => prev ?? (redsData?.[0]?.id ?? null));
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadData(); }, [loadData]);

  function pubsForRed(redId) {
    return publications.filter(p => p.redId === redId);
  }

  function openCoordModal() {
    setCoordUserId('');
    setCoordUserSearch('');
    setCoordUserResults([]);
    setCoordError('');
    setCoordSuccess('');
    setCoordModal(true);
  }

  async function searchCoordUsers() {
    if (!coordUserSearch.trim()) return;
    setCoordUserSearching(true);
    try {
      const data = await apiFetch(`/api/Users/search?q=${encodeURIComponent(coordUserSearch.trim())}`);
      setCoordUserResults(data ?? []);
    } catch {
      setCoordUserResults([]);
    } finally {
      setCoordUserSearching(false);
    }
  }

  async function handleAsignarCoordinador() {
    if (!coordUserId) { setCoordError('Seleccione un usuario coordinador.'); return; }
    setCoordLoading(true); setCoordError(''); setCoordSuccess('');
    try {
      await apiFetch(`/api/Redes/${selectedRedId}/coordinador`, {
        method: 'PUT',
        body: JSON.stringify({ coordinadorId: coordUserId }),
      });
      setCoordSuccess('Coordinador asignado correctamente.');
      await loadData();
    } catch (e) {
      setCoordError(e.message);
    } finally {
      setCoordLoading(false);
    }
  }

  function authorsList(authors) {
    return (authors ?? []).map((a, i) => (
      <span key={a.id}>
        {i > 0 && <span className="text-muted me-1">,</span>}
        {a.name}
        {a.userId && <i className="bi bi-person-check ms-1 text-success" title="Usuario registrado" />}
      </span>
    ));
  }

  const selectedRed = redes.find(r => r.id === selectedRedId) ?? null;
  const selectedPubs = selectedRed ? pubsForRed(selectedRed.id) : [];
  const tipoColor = TIPO_RED_COLORS[selectedRed?.tipo] ?? 'secondary';

  if (loading) {
    return (
      <div className="d-flex justify-content-center mt-5">
        <Spinner color="primary" />
      </div>
    );
  }

  return (
    <>
      <div className="mb-3">
        <h2>
          {isJefe ? 'Publicaciones por Red' : 'Publicaciones de mi Red coordinada'}
        </h2>
        <p className="text-muted mb-0">
          {isJefe
            ? 'Seleccione una red para ver su información, coordinador y publicaciones.'
            : 'Vea la información y publicaciones de la red que coordina.'}
        </p>
      </div>

      {error && <Alert color="danger">{error}</Alert>}

      {redes.length === 0 && !error && (
        <Alert color="info">
          {isJefe ? 'No hay redes registradas.' : 'No coordina ninguna red actualmente.'}
        </Alert>
      )}

      {redes.length > 0 && (
        <FormGroup className="mb-4" style={{ maxWidth: 420 }}>
          <Label className="fw-semibold">
            <i className="bi bi-globe me-1" />Red
          </Label>
          <Input
            type="select"
            value={selectedRedId ?? ''}
            onChange={e => setSelectedRedId(e.target.value)}
          >
            {redes.map(r => (
              <option key={r.id} value={r.id}>{r.nombre}</option>
            ))}
          </Input>
        </FormGroup>
      )}

      {selectedRed && (
        <Card className="shadow-sm">
          <CardHeader className="d-flex justify-content-between align-items-start gap-2 flex-wrap py-3">
            <div>
              <strong className="fs-5 me-2">{selectedRed.nombre}</strong>
              <Badge color={tipoColor} className="me-2">
                {TIPO_RED_LABELS[selectedRed.tipo] ?? `Tipo ${selectedRed.tipo}`}
              </Badge>
              {selectedRed.countryName && (
                <span className="text-muted small me-3">
                  <i className="bi bi-globe me-1" />{selectedRed.countryName}
                </span>
              )}
              <span className="text-muted small">
                <i className="bi bi-people me-1" />{selectedRed.cantidadProfesores} profesores
              </span>
            </div>
            {isJefe && (
              <Button size="sm" color="outline-secondary" onClick={openCoordModal}>
                <i className="bi bi-person-gear me-1" />Asignar coordinador
              </Button>
            )}
          </CardHeader>

          <CardBody>
            {/* Coordinador */}
            <div className="mb-4">
              <p className="text-muted small fw-semibold mb-1">
                <i className="bi bi-person-badge me-1" />Coordinador
              </p>
              {selectedRed.coordinadorNombre
                ? (
                  <div className="border rounded px-3 py-2 bg-light small" style={{ maxWidth: 320 }}>
                    <div className="fw-semibold text-primary">
                      <i className="bi bi-person me-1" />{selectedRed.coordinadorNombre}
                    </div>
                    {selectedRed.coordinadorEmail && (
                      <div className="text-muted mt-1">
                        <i className="bi bi-envelope me-1" />
                        <a href={`mailto:${selectedRed.coordinadorEmail}`} className="text-muted">
                          {selectedRed.coordinadorEmail}
                        </a>
                      </div>
                    )}
                  </div>
                )
                : <span className="text-muted small fst-italic">Sin coordinador asignado.</span>
              }
            </div>

            {/* Publicaciones */}
            <div className="mb-2">
              <strong>
                Publicaciones
                <span className="text-muted fw-normal ms-2 small">({selectedPubs.length})</span>
              </strong>
            </div>

            {selectedPubs.length === 0 ? (
              <p className="text-muted small fst-italic">
                No hay publicaciones registradas para esta red.
              </p>
            ) : (
              <Table responsive size="sm" className="mb-0 align-middle">
                <thead className="table-light">
                  <tr>
                    <th>Título</th>
                    <th>Tipo</th>
                    <th className="text-nowrap">Fecha</th>
                    <th>Autores</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {selectedPubs.map(pub => (
                    <tr key={pub.id}>
                      <td>{pub.title}</td>
                      <td>
                        <Badge color="secondary" pill>
                          {PUB_TIPOS[pub.publicationType] ?? `Tipo ${pub.publicationType}`}
                        </Badge>
                      </td>
                      <td className="text-nowrap text-muted small">{pub.publishedDate}</td>
                      <td className="small">{authorsList(pub.authors)}</td>
                      <td className="text-nowrap">
                        {pub.evidenceFileId && (
                          <CertificateViewButton fileId={pub.evidenceFileId} />
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            )}
          </CardBody>
        </Card>
      )}

      {/* Modal asignar coordinador */}
      {isJefe && (
        <Modal isOpen={coordModal} toggle={() => setCoordModal(false)}>
          <ModalHeader toggle={() => setCoordModal(false)}>
            Asignar coordinador — {selectedRed?.nombre}
          </ModalHeader>
          <ModalBody>
            {coordError && <Alert color="danger">{coordError}</Alert>}
            {coordSuccess && <Alert color="success">{coordSuccess}</Alert>}

            <FormGroup>
              <Label>Buscar usuario coordinador *</Label>
              <div className="d-flex gap-2">
                <Input
                  placeholder="Nombre o email del usuario"
                  value={coordUserSearch}
                  onChange={e => setCoordUserSearch(e.target.value)}
                  onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); searchCoordUsers(); } }}
                />
                <Button
                  color="outline-secondary"
                  onClick={searchCoordUsers}
                  disabled={coordUserSearching}
                >
                  {coordUserSearching ? <Spinner size="sm" /> : <i className="bi bi-search" />}
                </Button>
              </div>
              {coordUserResults.length > 0 && (
                <div className="list-group mt-2">
                  {coordUserResults.map(u => (
                    <button
                      type="button"
                      key={u.id}
                      className={`list-group-item list-group-item-action py-2 ${coordUserId === u.id ? 'active' : ''}`}
                      onClick={() => { setCoordUserId(u.id); setCoordUserSearch(u.fullName); setCoordUserResults([]); }}
                    >
                      <div className="fw-semibold">{u.fullName}</div>
                      <small className="text-muted">{u.email}</small>
                    </button>
                  ))}
                </div>
              )}
            </FormGroup>
          </ModalBody>
          <ModalFooter>
            <Button color="primary" onClick={handleAsignarCoordinador} disabled={coordLoading}>
              {coordLoading ? <Spinner size="sm" /> : 'Asignar'}
            </Button>
            <Button color="secondary" onClick={() => setCoordModal(false)}>Cerrar</Button>
          </ModalFooter>
        </Modal>
      )}
    </>
  );
}
