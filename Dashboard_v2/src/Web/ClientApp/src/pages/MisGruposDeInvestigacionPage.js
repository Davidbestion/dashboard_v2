import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Table, Spinner, Alert, Badge,
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

export default function MisGruposDeInvestigacionPage() {
  const [items, setItems] = useState([]);
  const [lineasDeInvestigacion, setLineasDeInvestigacion] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [gruposData, lineasData] = await Promise.all([
        apiFetch('/api/GruposDeInvestigacion/mine'),
        apiFetch('/api/LineasDeInvestigacion'),
      ]);
      setItems(gruposData);
      setLineasDeInvestigacion(lineasData);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  if (loading) {
    return (
      <div className="d-flex justify-content-center mt-5">
        <Spinner color="primary" />
      </div>
    );
  }

  return (
    <>
      <h2 className="mb-4">Mis Grupos de Investigación</h2>

      {error && <Alert color="danger" toggle={() => setError('')}>{error}</Alert>}

      <Card>
        <CardHeader>
          <strong>Grupos a los que perteneces</strong>
          <small className="text-muted ms-2">({items.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <Table responsive hover className="mb-0">
            <thead className="table-light">
              <tr>
                <th>Nombre</th>
                <th>Área</th>
                <th>Líneas de investigación</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 && (
                <tr>
                  <td colSpan={3} className="text-center text-muted py-4">
                    No perteneces a ningún grupo de investigación.
                  </td>
                </tr>
              )}
              {items.map(item => (
                <tr key={item.id}>
                  <td className="align-middle fw-semibold">{item.nombre}</td>
                  <td className="align-middle">
                    <Badge color="secondary" pill>{item.areaNombre}</Badge>
                  </td>
                  <td className="align-middle">
                    {item.lineasDeInvestigacionIds && item.lineasDeInvestigacionIds.length > 0
                      ? lineasDeInvestigacion
                          .filter(l => item.lineasDeInvestigacionIds.includes(l.id))
                          .map(l => <Badge color="info" pill className="me-1" key={l.id}>{l.nombre}</Badge>)
                      : <span className="text-muted small">Sin líneas asignadas</span>
                    }
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
        </CardBody>
      </Card>
    </>
  );
}
