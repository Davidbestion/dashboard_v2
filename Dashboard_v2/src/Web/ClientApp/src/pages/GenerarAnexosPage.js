import React, { useState } from 'react';
import {
  Card, CardBody, CardHeader, CardFooter,
  Button, Spinner, Alert,
  Form, FormGroup, Label, Input, FormText,
  Badge, Row, Col,
} from 'reactstrap';
import { useAuth } from '../contexts/AuthContext';

// ── Registro de anexos disponibles ────────────────────────────────────────────
//
// Cada entrada describe un anexo generatable. Para añadir uno nuevo basta con
// agregar una entrada aquí; el formulario y la descarga se generan solos.
//
// Campos:
//   id         — clave única, usada también para el estado
//   numero     — número de anexo (para el título y el nombre del archivo)
//   titulo     — nombre descriptivo
//   endpoint   — ruta del backend que devuelve el .xlsx
//   filters    — lista de filtros que se muestran en el formulario
//     type: 'month-range' → dos inputs type="month" (desde/hasta)
//     type: 'year-range'  → dos inputs type="number" (año desde/hasta)
//   roles      — roles autorizados a generar este anexo
//
const ANEXOS = [
  {
    id: 'publicaciones',
    numero: 2,
    titulo: 'Publicaciones',
    endpoint: '/api/Documents/anexo-publicaciones',
    descripcion: 'Incluye artículos en revistas (por grupo y cuartil), libros, monografías, capítulos y artículos de divulgación.',
    filters: [
      {
        type: 'month-range',
        label: 'Rango de publicación',
        fromKey: 'from',
        toKey: 'to',
        hint: 'Filtra por la fecha de publicación. Deja en blanco para incluir todas.',
      },
    ],
    roles: ['Superuser', 'Vicedecano_de_investigacion'],
  },
];

// ── Componente de filtros ──────────────────────────────────────────────────────

function MonthRangeFilter({ filter, values, onChange }) {
  return (
    <Row>
      <Col md={6}>
        <FormGroup>
          <Label className="fw-semibold">{filter.label} — Desde</Label>
          <Input
            type="month"
            value={values[filter.fromKey] ?? ''}
            onChange={e => onChange(filter.fromKey, e.target.value)}
          />
        </FormGroup>
      </Col>
      <Col md={6}>
        <FormGroup>
          <Label className="fw-semibold">Hasta</Label>
          <Input
            type="month"
            value={values[filter.toKey] ?? ''}
            onChange={e => onChange(filter.toKey, e.target.value)}
          />
        </FormGroup>
      </Col>
      {filter.hint && <Col xs={12}><FormText className="text-muted">{filter.hint}</FormText></Col>}
    </Row>
  );
}

// ── Tarjeta de un anexo individual ────────────────────────────────────────────

function AnexoCard({ anexo, userRole }) {
  const [filterValues, setFilterValues] = useState({});
  const [loading, setLoading] = useState(false);
  const [error, setError]   = useState('');
  const [success, setSuccess] = useState(false);

  if (!anexo.roles.includes(userRole)) return null;

  function handleFilterChange(key, value) {
    setFilterValues(prev => ({ ...prev, [key]: value }));
    setSuccess(false);
    setError('');
  }

  async function handleGenerar() {
    setLoading(true);
    setError('');
    setSuccess(false);
    try {
      const params = new URLSearchParams();
      for (const [k, v] of Object.entries(filterValues)) {
        if (v) params.set(k, v);
      }
      const qs = params.toString() ? `?${params.toString()}` : '';
      const response = await fetch(`${anexo.endpoint}${qs}`, { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.error ?? `Error al generar el Anexo ${anexo.numero}.`);
      }
      const blob = await response.blob();
      const url  = URL.createObjectURL(blob);
      const a    = document.createElement('a');
      const now  = new Date();
      a.download = `Anexo_${anexo.numero}_${anexo.titulo}_${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}.xlsx`;
      a.href = url;
      a.click();
      URL.revokeObjectURL(url);
      setSuccess(true);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <Card className="shadow-sm mb-4">
      <CardHeader className="d-flex align-items-center gap-2">
        <Badge color="primary" className="fs-6 px-2 py-1">Anexo {anexo.numero}</Badge>
        <strong className="fs-5">{anexo.titulo}</strong>
      </CardHeader>
      <CardBody>
        {anexo.descripcion && (
          <p className="text-muted mb-3" style={{ fontSize: '0.9em' }}>
            <i className="bi bi-info-circle me-1" />
            {anexo.descripcion}
          </p>
        )}
        <Form onSubmit={e => { e.preventDefault(); handleGenerar(); }}>
          {anexo.filters.map((filter, i) => (
            filter.type === 'month-range'
              ? <MonthRangeFilter key={i} filter={filter} values={filterValues} onChange={handleFilterChange} />
              : null
          ))}
          {error   && <Alert color="danger"  className="py-2">{error}</Alert>}
          {success && <Alert color="success" className="py-2"><i className="bi bi-check-circle me-1" />Archivo descargado correctamente.</Alert>}
        </Form>
      </CardBody>
      <CardFooter className="d-flex justify-content-end">
        <Button color="success" onClick={handleGenerar} disabled={loading}>
          {loading
            ? <><Spinner size="sm" className="me-2" />Generando…</>
            : <><i className="bi bi-file-earmark-excel me-2" />Generar y descargar Anexo {anexo.numero}</>
          }
        </Button>
      </CardFooter>
    </Card>
  );
}

// ── Página principal ──────────────────────────────────────────────────────────

export default function GenerarAnexosPage() {
  const { user } = useAuth();
  const available = ANEXOS.filter(a => a.roles.includes(user?.role));

  return (
    <>
      <p className="text-muted mb-4">
        Selecciona los filtros deseados y descarga el archivo Excel correspondiente a cada anexo.
        Los campos de fecha son opcionales; si se dejan en blanco se incluyen todos los registros.
      </p>

      {available.length === 0 && (
        <Alert color="warning">No tienes permisos para generar ningún anexo.</Alert>
      )}

      {available.map(anexo => (
        <AnexoCard key={anexo.id} anexo={anexo} userRole={user?.role} />
      ))}
    </>
  );
}
