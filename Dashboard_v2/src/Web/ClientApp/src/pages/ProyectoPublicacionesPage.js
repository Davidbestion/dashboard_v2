import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Spinner, Alert, Badge, Table,
  FormGroup, Label, Input,
} from 'reactstrap';
import { CertificateViewButton } from '../components/CertificateUpload';

const PUB_TIPOS = [
  'Artículo en Revista Científica',
  'Libro',
  'Monografía',
  'Capítulo',
  'Artículo de Divulgación',
];

async function apiFetch(url) {
  const res = await fetch(url, { credentials: 'include' });
  if (!res.ok) throw new Error(`Error ${res.status}`);
  return res.json();
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

export default function ProyectoPublicacionesPage() {
  const [publications, setPublications] = useState([]);
  const [loading, setLoading]           = useState(true);
  const [error, setError]               = useState('');
  const [selectedId, setSelectedId]     = useState(null);

  const loadData = useCallback(async () => {
    setLoading(true); setError('');
    try {
      const data = await apiFetch('/api/Publications/proyecto');
      setPublications(data ?? []);
      if (data?.length > 0) {
        setSelectedId(prev => prev ?? data[0].proyectoId);
      }
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadData(); }, [loadData]);

  // Derive unique projects from publications
  const proyectos = React.useMemo(() => {
    const seen = new Map();
    for (const p of publications) {
      if (p.proyectoId && !seen.has(p.proyectoId)) {
        seen.set(p.proyectoId, p.proyectoTitulo ?? p.proyectoId);
      }
    }
    return [...seen.entries()].map(([id, titulo]) => ({ id, titulo }));
  }, [publications]);

  const selectedPubs = publications.filter(p => p.proyectoId === selectedId);
  const selectedTitulo = proyectos.find(p => p.id === selectedId)?.titulo ?? '';

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
        <h2>Publicaciones de mis proyectos</h2>
        <p className="text-muted mb-0">
          Seleccione un proyecto para ver las publicaciones derivadas del mismo.
        </p>
      </div>

      {error && <Alert color="danger">{error}</Alert>}

      {proyectos.length === 0 && !error && (
        <Alert color="info">
          No hay publicaciones vinculadas a sus proyectos. Puede vincular publicaciones
          desde la página <a href="/mis-proyectos">Mis Proyectos</a>.
        </Alert>
      )}

      {proyectos.length > 0 && (
        <>
          <FormGroup className="mb-4" style={{ maxWidth: 480 }}>
            <Label className="fw-semibold">
              <i className="bi bi-folder me-1" />Proyecto
            </Label>
            <Input
              type="select"
              value={selectedId ?? ''}
              onChange={e => setSelectedId(e.target.value)}
            >
              {proyectos.map(p => (
                <option key={p.id} value={p.id}>{p.titulo}</option>
              ))}
            </Input>
          </FormGroup>

          <Card className="shadow-sm">
            <CardHeader className="py-3">
              <div className="d-flex align-items-center gap-2 flex-wrap">
                <i className="bi bi-folder-fill text-primary fs-5" />
                <strong className="fs-5">{selectedTitulo}</strong>
                <Badge color="primary" pill>{selectedPubs.length} publicación{selectedPubs.length !== 1 ? 'es' : ''}</Badge>
              </div>
            </CardHeader>

            <CardBody>
              {selectedPubs.length === 0 ? (
                <p className="text-muted small fst-italic">
                  No hay publicaciones registradas para este proyecto.
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
        </>
      )}
    </>
  );
}
