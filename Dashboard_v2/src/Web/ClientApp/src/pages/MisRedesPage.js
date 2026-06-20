import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardBody, CardHeader, Spinner, Alert, Badge, Table } from 'reactstrap';
import { useAuth } from '../contexts/AuthContext';

const TIPOS_RED = [
  { value: 0, label: 'Universitaria' },
  { value: 1, label: 'Nacional' },
  { value: 2, label: 'Internacional' },
];

async function apiFetch(url) {
  const res = await fetch(url, { credentials: 'include' });
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new Error((data?.errors ?? ['Error desconocido.']).join(' '));
  return data;
}

function RedCard({ red, currentUserId }) {
  const isCoordinador = red.coordinadorId === currentUserId;
  const tipoLabel = TIPOS_RED.find(t => t.value === red.tipo)?.label ?? red.tipo;

  return (
    <tr>
      <td>
        <strong>{red.nombre}</strong>
        {isCoordinador && (
          <Badge color="primary" className="ms-2" pill>Coordinador</Badge>
        )}
      </td>
      <td>{tipoLabel}</td>
      <td>{red.countryName ?? '—'}</td>
      <td>{red.cantidadProfesores}</td>
      <td>
        {red.coordinadorNombre
          ? <>{red.coordinadorNombre}<br /><small className="text-muted">{red.coordinadorEmail}</small></>
          : <span className="text-muted">Sin coordinador</span>}
      </td>
      <td>{red.participantes?.length ?? 0}</td>
    </tr>
  );
}

export default function MisRedesPage() {
  const { user } = useAuth();
  const [redes, setRedes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try {
      const data = await apiFetch('/api/Redes/mis-redes');
      setRedes(Array.isArray(data) ? data : []);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const coordinadas = redes.filter(r => r.coordinadorId === user?.id);
  const participando = redes.filter(r => r.coordinadorId !== user?.id);

  if (loading) return <div className="d-flex justify-content-center mt-5"><Spinner color="primary" /></div>;

  return (
    <>
      <h2 className="mb-4">Mis Redes</h2>
      {error && <Alert color="danger">{error}</Alert>}

      <Card className="mb-4">
        <CardHeader>
          <strong>Redes que coordino</strong>
          <small className="text-muted ms-2">({coordinadas.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <Table responsive hover className="mb-0">
            <thead className="table-light">
              <tr>
                <th>Nombre</th>
                <th>Tipo</th>
                <th>País</th>
                <th>Profesores</th>
                <th>Coordinador</th>
                <th>Participantes</th>
              </tr>
            </thead>
            <tbody>
              {coordinadas.length === 0
                ? <tr><td colSpan={6} className="text-center text-muted py-4">No coordinas ninguna red.</td></tr>
                : coordinadas.map(r => <RedCard key={r.id} red={r} currentUserId={user?.id} />)
              }
            </tbody>
          </Table>
        </CardBody>
      </Card>

      <Card>
        <CardHeader>
          <strong>Redes en las que participo</strong>
          <small className="text-muted ms-2">({participando.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <Table responsive hover className="mb-0">
            <thead className="table-light">
              <tr>
                <th>Nombre</th>
                <th>Tipo</th>
                <th>País</th>
                <th>Profesores</th>
                <th>Coordinador</th>
                <th>Participantes</th>
              </tr>
            </thead>
            <tbody>
              {participando.length === 0
                ? <tr><td colSpan={6} className="text-center text-muted py-4">No participas en ninguna red.</td></tr>
                : participando.map(r => <RedCard key={r.id} red={r} currentUserId={user?.id} />)
              }
            </tbody>
          </Table>
        </CardBody>
      </Card>
    </>
  );
}
