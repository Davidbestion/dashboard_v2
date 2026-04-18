import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Table, Badge,
  Spinner, Alert,
} from 'reactstrap';

// Etiquetas correspondientes al enum PublicationType del backend (índice = valor entero)
const PUB_TIPOS = ['Diario', 'Libro', 'Monografía', 'Capítulo', 'Artículo de Divulgación'];

async function apiFetch(url) {
  const response = await fetch(url, { credentials: 'include' });
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    throw new Error(data?.title ?? `Error ${response.status}`);
  }
  return data;
}

export default function PublicacionesConsultaPage() {
  const [publicaciones, setPublicaciones] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await apiFetch('/api/Publications/todas');
      setPublicaciones(data);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  return (
    <Card>
      <CardHeader><strong>Publicaciones</strong></CardHeader>
      <CardBody>
        {error && <Alert color="danger">{error}</Alert>}
        {loading ? (
          <div className="text-center py-4"><Spinner /></div>
        ) : (
          <Table bordered hover responsive size="sm">
            <thead>
              <tr>
                <th>Título</th>
                <th>Tipo</th>
                <th>URL / DOI</th>
                <th>Proyecto vinculado</th>
              </tr>
            </thead>
            <tbody>
              {publicaciones.length === 0 && (
                <tr>
                  <td colSpan={4} className="text-center text-muted">No hay publicaciones registradas.</td>
                </tr>
              )}
              {publicaciones.map(p => (
                <tr key={p.id}>
                  <td>{p.titulo}</td>
                  <td>
                    <Badge color="secondary">{PUB_TIPOS[p.tipo] ?? p.tipo}</Badge>
                  </td>
                  <td style={{ maxWidth: 280 }}>
                    {p.urlDoi
                      ? (
                        <a
                          href={p.urlDoi.startsWith('http') ? p.urlDoi : `https://doi.org/${p.urlDoi}`}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-truncate d-block"
                          title={p.urlDoi}
                        >
                          {p.urlDoi}
                        </a>
                      )
                      : <span className="text-muted">—</span>}
                  </td>
                  <td>{p.proyectoTitulo ?? <span className="text-muted">—</span>}</td>
                </tr>
              ))}
            </tbody>
          </Table>
        )}
      </CardBody>
    </Card>
  );
}
