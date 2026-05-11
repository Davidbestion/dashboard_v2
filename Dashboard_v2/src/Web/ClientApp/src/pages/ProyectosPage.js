import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Card, CardBody, CardHeader,
  Table, Button, Spinner, Alert, Badge,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input, InputGroup, InputGroupText,
} from 'reactstrap';
import { useAuth } from '../contexts/AuthContext';
import DataTable from '../components/DataTable';
import FilterableDataTable from '../components/FilterableDataTable';

async function apiFetch(url, options = {}) {
  const response = await fetch(url, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...(options.headers ?? {}) },
    ...options,
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    let errors = data?.errors ?? ['Error desconocido.'];
    // ValidationProblemDetails devuelve errors como { campo: [msg, ...], ... }
    if (!Array.isArray(errors) && typeof errors === 'object' && errors !== null) {
      errors = Object.values(errors).flat();
    }
    throw new Error(errors.join(' '));
  }
  return data;
}

// value = slug de URL; sincronizado con rutas del backend
const TIPOS = [
  { value: 'en-revision',                label: 'En Revisión',                       color: 'warning'   },
  { value: 'empresariales',              label: 'Empresarial (PE)',                   color: 'primary'   },
  { value: 'apoyo-programa',             label: 'Apoyo a Programa (PAP)',             color: 'info'      },
  { value: 'desarrollo-local',           label: 'Desarrollo Local (PDL)',             color: 'success'   },
  { value: 'no-empresariales',           label: 'No Empresarial (PNE)',               color: 'secondary' },
  { value: 'colaboracion-internacional', label: 'Colaboración Internacional (PRCI)', color: 'danger'    },
  { value: 'pnap',                       label: 'PNAP',                               color: 'dark'      },
];

const TIPOS_PAP = [
  { value: 1, label: 'Nacional (N)'    },
  { value: 2, label: 'Sectorial (S)'   },
  { value: 3, label: 'Territorial (T)' },
];

const tipoLabel = (v) => TIPOS.find(t => t.value === v)?.label ?? v;
const tipoColor = (v) => TIPOS.find(t => t.value === v)?.color ?? 'secondary';

const isEjecucion = (tipo) => tipo !== 'en-revision'; // all except EnRevision

const emptyForm = {
  tipo: 'en-revision',
  titulo: '', jefeId: '',
  areaId: '',
  numeroMiembros: 0, cantidadMiembrosUH: 0,
  cantidadEstudiantes: 0, cantidadEstudiantesContratados: 0,
  tributaFormacionDoctoral: false,
  tributaDesarrolloLocal: false,
  clasificacionId: '',
  // EnRevision
  situacion: '', tipoRevision: '',
  // EnEjecucion
  fechaInicio: '', fechaInicioDay: '', fechaCierre: '', fechaCierreDay: '',
  estadoDeEjecucion: '', codigoProyecto: '',
  entidadEjecutoraPrincipal: '', entidadEjecutoraParticipante: '',
  contribucionSectoresEstrategicos: '', contribucionEjesEstrategicos: '',
  // PE
  empresa: '',
  // PAP
  nombrePrograma: '', tipoPAP: 1,
  // PDL
  municipio: '',
  // PNE
  entidadNoEmpresarial: '',
  // PRCI
  fuenteFinanciacion: '', terminosReferencia: '',
  // PNAP
  financiamientoUH: '',
};

export default function ProyectosPage() {
  const { user } = useAuth();
  const isJefeDeProyecto = user?.role === 'Jefe_de_Proyecto';
  const isSuperuser = user?.role === 'Superuser';
  const canCreateProyecto = isJefeDeProyecto || isSuperuser;
  const [items, setItems] = useState([]);
  const [clasificaciones, setClasificaciones] = useState([]);
  const [jefes, setJefes] = useState([]);
  const [areas, setAreas] = useState([]);
  const [tiposEjecucion, setTiposEjecucion] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);
  const [loadingEdit, setLoadingEdit] = useState(false);

  // ─ Publications manager ────────────────────────────────────────────────────────────
  const [pubsModal, setPubsModal] = useState(false);
  const [pubsTarget, setPubsTarget] = useState(null);
  const [pubsList, setPubsList] = useState([]);
  const [pubsLoading, setPubsLoading] = useState(false);
  const [pubsError, setPubsError] = useState('');
  const [availablePubs, setAvailablePubs] = useState([]);
  const [selectedPubToLink, setSelectedPubToLink] = useState('');
  const [linkingPub, setLinkingPub] = useState(false);

  // ─ Patentes manager ───────────────────────────────────────────────────────────────
  const [patentesModal, setPatentesModal] = useState(false);
  const [patentesTarget, setPatentesTarget] = useState(null);
  const [patentesList, setPatentesList] = useState([]);
  const [patentesLoading, setPatentesLoading] = useState(false);
  const [patentesError, setPatentesError] = useState('');
  const [availablePatentes, setAvailablePatentes] = useState([]);
  const [selectedPatenteToLink, setSelectedPatenteToLink] = useState('');
  const [linkingPatente, setLinkingPatente] = useState(false);
  const [patentesSearch, setPatentesSearch] = useState('');

  const [generatingAnexo, setGeneratingAnexo] = useState(false);

  const filteredPatentes = useMemo(() => {
    const q = patentesSearch.trim().toLowerCase();
    if (!q) return patentesList;
    return patentesList.filter(p =>
      String(p.titulo ?? '').toLowerCase().includes(q)
      || String(p.numeroSolicitudConcesion ?? '').toLowerCase().includes(q)
      || String(p.creador ?? '').toLowerCase().includes(q)
      || String((p.creadores ?? []).join(', ')).toLowerCase().includes(q)
    );
  }, [patentesList, patentesSearch]);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [proyData, clasData, jefesData, tiposData, areasData] = await Promise.all([
        apiFetch('/api/Proyectos'),
        apiFetch('/api/Clasificaciones'),
        apiFetch('/api/Users/jefes-de-proyecto'),
        apiFetch('/api/Proyectos/tipos-ejecucion'),
        apiFetch('/api/Areas'),
      ]);
      setItems(proyData);
      setClasificaciones(clasData);
      setJefes(jefesData);
      setTiposEjecucion(tiposData);
      setAreas(areasData);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);
  

  useEffect(() => { load(); }, [load]);

  function openCreate() {
    setEditing(null);
    setForm({
      ...emptyForm,
      clasificacionId: clasificaciones[0]?.id ?? '',
      // For Jefe_de_Proyecto: pre-fill with their own area
      areaId: isJefeDeProyecto ? (user?.areaId ?? '') : '',
      // For Jefe_de_Proyecto the server will override jefeId, but pre-fill for clarity
      jefeId: isJefeDeProyecto ? (user?.id ?? '') : '',
    });
    setFormError('');
    setModal(true);
  }

  async function openEdit(item) {
    setFormError('');
    setLoadingEdit(true);
    try {
      const full = await apiFetch(`/api/Proyectos/${item.tipo}/${item.id}`);
      setEditing(item);
      setForm({
        tipo: item.tipo,
        titulo: full.titulo ?? '',
        jefeId: full.jefeId ?? '',
        areaId: full.areaId ?? '',
        numeroMiembros: full.numeroMiembros ?? 0,
        cantidadMiembrosUH: full.cantidadMiembrosUH ?? 0,
        cantidadEstudiantes: full.cantidadEstudiantes ?? 0,
        cantidadEstudiantesContratados: full.cantidadEstudiantesContratados ?? 0,
        tributaFormacionDoctoral: full.tributaFormacionDoctoral ?? false,
        tributaDesarrolloLocal: full.tributaDesarrolloLocal ?? false,
        clasificacionId: full.clasificacionId ?? '',
        situacion: full.situacion ?? '',
        tipoRevision: full.tipo ?? '',
        fechaInicio: full.fechaInicio ? full.fechaInicio.substring(0, 7) : '',
        fechaInicioDay: full.fechaInicio ? String(parseInt(full.fechaInicio.substring(8, 10))) : '',
        fechaCierre: full.fechaCierre ? full.fechaCierre.substring(0, 7) : '',
        fechaCierreDay: full.fechaCierre ? String(parseInt(full.fechaCierre.substring(8, 10))) : '',
        estadoDeEjecucion: full.estadoDeEjecucion ?? '',
        codigoProyecto: full.codigoProyecto ?? '',
        entidadEjecutoraPrincipal: full.entidadEjecutoraPrincipal ?? '',
        entidadEjecutoraParticipante: full.entidadEjecutoraParticipante ?? '',
        contribucionSectoresEstrategicos: full.contribucionSectoresEstrategicos ?? '',
        contribucionEjesEstrategicos: full.contribucionEjesEstrategicos ?? '',
        empresa: full.empresa ?? '',
        nombrePrograma: full.nombrePrograma ?? '',
        tipoPAP: full.tipoPAP ?? 1,
        municipio: full.municipio ?? '',
        entidadNoEmpresarial: full.entidadNoEmpresarial ?? '',
        fuenteFinanciacion: full.fuenteFinanciacion ?? '',
        terminosReferencia: full.terminosReferencia ?? '',
        financiamientoUH: full.financiamientoUH ?? '',
      });
      setModal(true);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoadingEdit(false);
    }
  }

  const set = (field) => (e) => setForm(f => ({ ...f, [field]: e.target.value }));
  const setNum = (field) => (e) => setForm(f => ({ ...f, [field]: parseInt(e.target.value) || 0 }));
  const setBool = (field) => (e) => setForm(f => ({ ...f, [field]: e.target.checked }));

  function buildBody() {
    const t = form.tipo;
    const base = {
      titulo: form.titulo,
      jefeId: form.jefeId,
      areaId: form.areaId,
      numeroMiembros: form.numeroMiembros,
      cantidadMiembrosUH: form.cantidadMiembrosUH,
      cantidadEstudiantes: form.cantidadEstudiantes,
      cantidadEstudiantesContratados: form.cantidadEstudiantesContratados,
      tributaFormacionDoctoral: form.tributaFormacionDoctoral,
      clasificacionId: form.clasificacionId,
    };
    if (t === 'en-revision') return { ...base, situacion: form.situacion, tipo: form.tipoRevision };
    const ejecucion = {
      ...base,
      fechaInicio: form.fechaInicio ? `${form.fechaInicio}-${String(parseInt(form.fechaInicioDay) || 1).padStart(2, '0')}` : null,
      fechaCierre: form.fechaCierre ? `${form.fechaCierre}-${String(parseInt(form.fechaCierreDay) || 1).padStart(2, '0')}` : null,
      estadoDeEjecucion: form.estadoDeEjecucion,
      codigoProyecto: form.codigoProyecto,
      entidadEjecutoraPrincipal: form.entidadEjecutoraPrincipal,
      entidadEjecutoraParticipante: form.entidadEjecutoraParticipante || null,
      contribucionSectoresEstrategicos: form.contribucionSectoresEstrategicos || null,
      contribucionEjesEstrategicos: form.contribucionEjesEstrategicos || null,
      tributaDesarrolloLocal: form.tributaDesarrolloLocal,
    };
    switch (t) {
      case 'empresariales': return { ...ejecucion, empresa: form.empresa };
      case 'apoyo-programa': return { ...ejecucion, nombrePrograma: form.nombrePrograma, tipoPAP: parseInt(form.tipoPAP) };
      case 'desarrollo-local': return { ...ejecucion, municipio: form.municipio };
      case 'no-empresariales': return { ...ejecucion, entidadNoEmpresarial: form.entidadNoEmpresarial };
      case 'colaboracion-internacional': return { ...ejecucion, fuenteFinanciacion: form.fuenteFinanciacion, terminosReferencia: form.terminosReferencia };
      case 'pnap': return { ...ejecucion, financiamientoUH: form.financiamientoUH };
      default: return base;
    }
  }

  async function handleSave() {
    setSaving(true);
    setFormError('');
    try {
      const slug = form.tipo;
      const body = buildBody();
      if (editing) {
        await apiFetch(`/api/Proyectos/${slug}/${editing.id}`, { method: 'PUT', body: JSON.stringify(body) });
      } else {
        await apiFetch(`/api/Proyectos/${slug}`, { method: 'POST', body: JSON.stringify(body) });
      }
      setModal(false);
      await load();
    } catch (e) {
      setFormError(e.message);
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await apiFetch(`/api/Proyectos/${deleteTarget.id}`, { method: 'DELETE' });
      setDeleteTarget(null);
      await load();
    } catch (e) {
      setError(e.message);
    } finally {
      setDeleting(false);
    }
  }

  async function handleGenerarAnexo() {
    setGeneratingAnexo(true);
    setError('');
    try {
      const response = await fetch('/api/Documents/anexo-proyectos', { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.error ?? 'Error al generar el Anexo 4.');
      }
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      const now = new Date();
      a.download = `Anexo_4_Proyectos_${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}.xlsx`;
      a.href = url;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e) {
      setError(e.message);
    } finally {
      setGeneratingAnexo(false);
    }
  }

  const tipo = form.tipo;

  // ─ Publications manager helpers ────────────────────────────────────────────────

  async function openPubsManager(item) {
    setPubsTarget(item);
    setPubsError('');
    setPubsList([]);
    setAvailablePubs([]);
    setSelectedPubToLink('');
    setPubsLoading(true);
    setPubsModal(true);
    try {
      const [pubs, available] = await Promise.all([
        apiFetch(`/api/Proyectos/${item.id}/publicaciones`),
        apiFetch('/api/Proyectos/publicaciones-disponibles'),
      ]);
      setPubsList(pubs);
      setAvailablePubs(available);
    } catch (e) {
      setPubsError(e.message);
    } finally {
      setPubsLoading(false);
    }
  }

  async function unlinkPub(pubId) {
    setPubsError('');
    try {
      await apiFetch(`/api/Proyectos/${pubsTarget.id}/publicaciones/${pubId}`, { method: 'DELETE' });
      const unlinked = pubsList.find(p => p.id === pubId);
      setPubsList(prev => prev.filter(p => p.id !== pubId));
      if (unlinked) setAvailablePubs(prev => [...prev, unlinked].sort((a, b) => a.title.localeCompare(b.title)));
      load();
    } catch (e) {
      setPubsError(e.message);
    }
  }

  async function linkPub() {
    if (!selectedPubToLink) return;
    setLinkingPub(true);
    setPubsError('');
    try {
      await apiFetch(`/api/Proyectos/${pubsTarget.id}/publicaciones/${selectedPubToLink}`, { method: 'POST' });
      const linked = availablePubs.find(p => p.id === selectedPubToLink);
      setAvailablePubs(prev => prev.filter(p => p.id !== selectedPubToLink));
      if (linked) setPubsList(prev => [...prev, linked].sort((a, b) => a.title.localeCompare(b.title)));
      setSelectedPubToLink('');
      load();
    } catch (e) {
      setPubsError(e.message);
    } finally {
      setLinkingPub(false);
    }
  }

  async function openPatentesManager(item) {
    setPatentesTarget(item);
    setPatentesError('');
    setPatentesList([]);
    setAvailablePatentes([]);
    setSelectedPatenteToLink('');
    setPatentesSearch('');
    setPatentesLoading(true);
    setPatentesModal(true);
    try {
      const [linked, allPatentes] = await Promise.all([
        apiFetch(`/api/Proyectos/${item.id}/patentes`),
        apiFetch('/api/Patentes/mis'),
      ]);
      setPatentesList(linked);
      setAvailablePatentes(
        allPatentes
          .filter(p => !linked.some(lp => lp.patenteId === p.id))
          .sort((a, b) => a.titulo.localeCompare(b.titulo))
      );
    } catch (e) {
      setPatentesError(e.message);
    } finally {
      setPatentesLoading(false);
    }
  }

  async function unlinkPatente(patenteId) {
    setPatentesError('');
    try {
      await apiFetch(`/api/Proyectos/${patentesTarget.id}/patentes/${patenteId}`, { method: 'DELETE' });
      const unlinked = patentesList.find(p => p.patenteId === patenteId);
      setPatentesList(prev => prev.filter(p => p.patenteId !== patenteId));
      if (unlinked) {
        setAvailablePatentes(prev => [
          ...prev,
          { id: unlinked.patenteId, titulo: unlinked.titulo },
        ].sort((a, b) => a.titulo.localeCompare(b.titulo)));
      }
      load();
    } catch (e) {
      setPatentesError(e.message);
    }
  }

  async function linkPatente() {
    if (!selectedPatenteToLink) return;
    setLinkingPatente(true);
    setPatentesError('');
    try {
      await apiFetch(`/api/Proyectos/${patentesTarget.id}/patentes/${selectedPatenteToLink}`, { method: 'POST' });
      setSelectedPatenteToLink('');
      const linked = await apiFetch(`/api/Proyectos/${patentesTarget.id}/patentes`);
      setPatentesList(linked);
      setAvailablePatentes(prev =>
        prev
          .filter(p => p.id !== selectedPatenteToLink)
          .sort((a, b) => a.titulo.localeCompare(b.titulo))
      );
      load();
    } catch (e) {
      setPatentesError(e.message);
    } finally {
      setLinkingPatente(false);
    }
  }

  return (
    <>
      <Card>
        <CardHeader className="d-flex justify-content-between align-items-center">
          <strong>Proyectos</strong>
          <div className="d-flex gap-2 align-items-center">
            <Button color="outline-success" size="sm" onClick={handleGenerarAnexo} disabled={generatingAnexo}>
              {generatingAnexo ? <Spinner size="sm" /> : '⬇ Generar Anexo 4'}
            </Button>
            {canCreateProyecto && (
              <Button color="primary" size="sm" onClick={openCreate}>+ Nuevo proyecto</Button>
            )}
          </div>
        </CardHeader>
        <CardBody>
          {error && <Alert color="danger">{error}</Alert>}
          {loading ? (
            <div className="text-center py-4"><Spinner /></div>
          ) : (
            <FilterableDataTable
              filterConfig={{
                search: { fields: ['titulo', 'jefe'], placeholder: 'Buscar por título o jefe...' },
                filters: [
                  { key: 'tipo', label: 'Tipo',
                    options: TIPOS.map(t => ({ value: String(t.value), label: t.label })),
                    match: (item, val) => String(item.tipo) === val },
                  { key: 'clasificacionNombre', label: 'Clasificación',
                    options: clasificaciones.map(c => ({ value: c.nombre, label: c.nombre })) },
                ],
              }}
              columns={[
                { key: 'tipo',              label: 'Tipo',        sortable: true, render: v => <Badge color={tipoColor(v)}>{tipoLabel(v)}</Badge> },
                { key: 'titulo',            label: 'Título',      sortable: true },
                { key: 'jefe',              label: 'Jefe' },
                { key: 'clasificacionNombre', label: 'Clasificación' },
                {
                  key: 'publicacionesDerivadas',
                  label: 'Publicaciones derivadas',
                  render: (pubs, item) => (
                    <div className="d-flex flex-column gap-1">
                      {(pubs ?? []).length === 0
                        ? <span className="text-muted">—</span>
                        : (pubs ?? []).map((urlDoi, i) => (
                          <div key={i}>
                            <a href={urlDoi.startsWith('http') ? urlDoi : `https://doi.org/${urlDoi}`}
                               target="_blank" rel="noopener noreferrer"
                               title={urlDoi} style={{ fontSize: '0.8rem' }}>
                              {urlDoi.length > 40 ? urlDoi.substring(0, 40) + '…' : urlDoi}
                            </a>
                          </div>
                        ))
                      }
                      <div>
                        <Button color="outline-primary" size="sm"
                                style={{ fontSize: '0.75rem', padding: '0.1rem 0.4rem' }}
                                onClick={() => openPubsManager(item)}>
                          📎 Gestionar
                        </Button>
                      </div>
                    </div>
                  ),
                },
              ]}
              data={items}
              keyExtractor={item => item.id}
              actions={[
                { key: 'patentes', label: 'Patentes', icon: 'bi-lightbulb', color: 'outline-info', onClick: item => openPatentesManager(item) },
                { key: 'edit',   label: 'Editar',   icon: 'bi-pencil', color: 'outline-secondary', onClick: item => openEdit(item),        disabled: () => loadingEdit },
                { key: 'delete', label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',    onClick: item => setDeleteTarget(item) },
              ]}
              emptyMessage="No hay proyectos registrados."
            />
          )}
        </CardBody>
      </Card>

      {/* Create / Edit Modal */}
      <Modal isOpen={modal} toggle={() => setModal(false)} size="lg" scrollable>
        <ModalHeader toggle={() => setModal(false)}>
          {editing ? 'Editar proyecto' : 'Nuevo proyecto'}
        </ModalHeader>
        <ModalBody>
          {formError && <Alert color="danger">{formError}</Alert>}
          <Form>
            {/* Tipo — solo visible al crear */}
            {!editing && (
              <FormGroup>
                <Label>Tipo de proyecto *</Label>
                <Input type="select" value={form.tipo} onChange={set('tipo')}>
                  {TIPOS.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                </Input>
              </FormGroup>
            )}
            {editing && (
              <FormGroup>
                <Label>Tipo</Label>
                <div><Badge color={tipoColor(editing.tipo)}>{tipoLabel(editing.tipo)}</Badge></div>
              </FormGroup>
            )}

            {/* Campos base comunes */}
            <FormGroup>
              <Label>Título *</Label>
              <Input value={form.titulo} onChange={set('titulo')} placeholder="Título del proyecto" />
            </FormGroup>
            <FormGroup>
              <Label>Jefe de proyecto *</Label>
              {isJefeDeProyecto ? (
                // Jefe_de_Proyecto always creates/edits as themselves — no selector needed
                <Input plaintext readOnly value={`${user?.userName ?? ''} (${user?.email ?? ''})`} />
              ) : (
                <Input type="select" value={form.jefeId} onChange={set('jefeId')}>
                  <option value="">-- Seleccionar jefe --</option>
                  {jefes.map(j => (
                    <option key={j.id} value={j.id}>{j.nombreCompleto} ({j.email})</option>
                  ))}
                </Input>
              )}
            </FormGroup>

            <div className="row">
              <div className="col-md-4">
                <FormGroup>
                  <Label>N° miembros</Label>
                  <Input type="number" min={0} value={form.numeroMiembros} onChange={setNum('numeroMiembros')} onFocus={(e) => e.target.select()} />
                </FormGroup>
              </div>
              <div className="col-md-4">
                <FormGroup>
                  <Label>Miembros UH</Label>
                  <Input type="number" min={0} value={form.cantidadMiembrosUH} onChange={setNum('cantidadMiembrosUH')} onFocus={(e) => e.target.select()} />
                </FormGroup>
              </div>
              <div className="col-md-4">
                <FormGroup>
                  <Label>Estudiantes</Label>
                  <Input type="number" min={0} value={form.cantidadEstudiantes} onChange={setNum('cantidadEstudiantes')} onFocus={(e) => e.target.select()} />
                </FormGroup>
              </div>
            </div>
            <div className="row">
              <div className="col-md-6">
                <FormGroup>
                  <Label>Estudiantes contratados</Label>
                  <Input type="number" min={0} value={form.cantidadEstudiantesContratados} onChange={setNum('cantidadEstudiantesContratados')} onFocus={(e) => e.target.select()} />
                </FormGroup>
              </div>
              <div className="col-md-6">
                <FormGroup check className="mt-4 pt-2">
                  <Input type="checkbox" id="tributaFormacion" checked={form.tributaFormacionDoctoral} onChange={setBool('tributaFormacionDoctoral')} />
                  <Label check for="tributaFormacion">Tributa a la formación doctoral</Label>
                </FormGroup>
              </div>
            </div>

            <FormGroup>
              <Label>Clasificación *</Label>
              <Input type="select" value={form.clasificacionId} onChange={set('clasificacionId')}>
                <option value="">-- Seleccionar --</option>
                {clasificaciones.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}
              </Input>
            </FormGroup>

            <FormGroup>
              <Label>Área de pertenencia *</Label>
              {isJefeDeProyecto ? (
                <Input plaintext readOnly value={user?.areaNombre ?? 'Sin área asignada'} />
              ) : (
                <Input type="select" value={form.areaId} onChange={set('areaId')}>
                  <option value="">-- Seleccionar área --</option>
                  {areas.map(a => <option key={a.id} value={a.id}>{a.nombre}</option>)}
                </Input>
              )}
            </FormGroup>

            <hr />

            {/* Campos específicos: En Revisión */}
            {tipo === 'en-revision' && (
              <>
                <FormGroup>
                  <Label>Situación *</Label>
                  <Input value={form.situacion} onChange={set('situacion')} placeholder="Situación actual" />
                </FormGroup>
                <FormGroup>
                  <Label>Tipo (revisión) *</Label>
                  <Input type="select" value={form.tipoRevision} onChange={set('tipoRevision')}>
                    <option value="">-- Seleccionar tipo --</option>
                    {tiposEjecucion.map(t => <option key={t} value={t}>{t}</option>)}
                  </Input>
                </FormGroup>
              </>
            )}

            {/* Campos en ejecución comunes (todos excepto EnRevision) */}
            {isEjecucion(tipo) && (
              <>
                <div className="row">
                  <div className="col-md-6">
                    <FormGroup>
                      <Label>Fecha de inicio *</Label>
                      <Input type="month" value={form.fechaInicio} onChange={set('fechaInicio')} />
                      <div className="d-flex align-items-center gap-2 mt-1">
                        <small className="text-muted text-nowrap">Día (opcional):</small>
                        <Input
                          type="number" min={1} max={31} placeholder="1–31"
                          value={form.fechaInicioDay}
                          onChange={e => setForm(f => ({ ...f, fechaInicioDay: e.target.value }))}
                          style={{ width: '5rem' }}
                        />
                      </div>
                    </FormGroup>
                  </div>
                  <div className="col-md-6">
                    <FormGroup>
                      <Label>Fecha de cierre</Label>
                      <Input type="month" value={form.fechaCierre} onChange={set('fechaCierre')} />
                      <div className="d-flex align-items-center gap-2 mt-1">
                        <small className="text-muted text-nowrap">Día (opcional):</small>
                        <Input
                          type="number" min={1} max={31} placeholder="1–31"
                          value={form.fechaCierreDay}
                          onChange={e => setForm(f => ({ ...f, fechaCierreDay: e.target.value }))}
                          style={{ width: '5rem' }}
                        />
                      </div>
                    </FormGroup>
                  </div>
                </div>
                <div className="row">
                  <div className="col-md-6">
                    <FormGroup>
                      <Label>Estado de ejecución *</Label>
                      <Input value={form.estadoDeEjecucion} onChange={set('estadoDeEjecucion')} placeholder="Ej: En curso, Suspendido..." />
                    </FormGroup>
                  </div>
                  <div className="col-md-6">
                    <FormGroup>
                      <Label>Código del proyecto *</Label>
                      <Input value={form.codigoProyecto} onChange={set('codigoProyecto')} placeholder="Código" />
                    </FormGroup>
                  </div>
                </div>
                <FormGroup>
                  <Label>Entidad ejecutora principal *</Label>
                  <Input value={form.entidadEjecutoraPrincipal} onChange={set('entidadEjecutoraPrincipal')} />
                </FormGroup>
                <FormGroup>
                  <Label>Entidad ejecutora participante</Label>
                  <Input value={form.entidadEjecutoraParticipante} onChange={set('entidadEjecutoraParticipante')} />
                </FormGroup>
                <FormGroup>
                  <Label>Contribución a sectores estratégicos</Label>
                  <Input type="textarea" rows={2} value={form.contribucionSectoresEstrategicos} onChange={set('contribucionSectoresEstrategicos')} />
                </FormGroup>
                <FormGroup>
                  <Label>Contribución a ejes estratégicos</Label>
                  <Input type="textarea" rows={2} value={form.contribucionEjesEstrategicos} onChange={set('contribucionEjesEstrategicos')} />
                </FormGroup>
                {/* TributaDesarrolloLocal: oculto para PDL (siempre true) */}
                {tipo !== 'desarrollo-local' && (
                  <FormGroup check className="mb-2">
                    <Input type="checkbox" id="tributaDesarrolloLocal" checked={form.tributaDesarrolloLocal} onChange={setBool('tributaDesarrolloLocal')} />
                    <Label check for="tributaDesarrolloLocal">Tributa al desarrollo local</Label>
                  </FormGroup>
                )}
              </>
            )}

            {/* PE — Empresarial */}
            {tipo === 'empresariales' && (
              <FormGroup>
                <Label>Empresa *</Label>
                <Input value={form.empresa} onChange={set('empresa')} placeholder="Nombre de la empresa" />
              </FormGroup>
            )}

            {/* PAP */}
            {tipo === 'apoyo-programa' && (
              <>
                <FormGroup>
                  <Label>Nombre del programa *</Label>
                  <Input value={form.nombrePrograma} onChange={set('nombrePrograma')} placeholder="Nombre del programa" />
                </FormGroup>
                <FormGroup>
                  <Label>Tipo PAP *</Label>
                  <Input type="select" value={form.tipoPAP} onChange={set('tipoPAP')}>
                    {TIPOS_PAP.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                  </Input>
                </FormGroup>
              </>
            )}

            {/* PDL */}
            {tipo === 'desarrollo-local' && (
              <>
                <FormGroup>
                  <Label>Municipio *</Label>
                  <Input value={form.municipio} onChange={set('municipio')} placeholder="Municipio" />
                </FormGroup>
                <p className="text-muted small mb-2">
                  <em>Los PDL tributan siempre al desarrollo local.</em>
                </p>
              </>
            )}

            {/* PNE */}
            {tipo === 'no-empresariales' && (
              <FormGroup>
                <Label>Entidad no empresarial *</Label>
                <Input value={form.entidadNoEmpresarial} onChange={set('entidadNoEmpresarial')} placeholder="Entidad" />
              </FormGroup>
            )}

            {/* PRCI */}
            {tipo === 'colaboracion-internacional' && (
              <>
                <FormGroup>
                  <Label>Fuente de financiación *</Label>
                  <Input value={form.fuenteFinanciacion} onChange={set('fuenteFinanciacion')} placeholder="Fuente de financiación" />
                </FormGroup>
                <FormGroup>
                  <Label>Términos de referencia *</Label>
                  <Input type="textarea" rows={3} value={form.terminosReferencia} onChange={set('terminosReferencia')} />
                </FormGroup>
              </>
            )}

            {/* PNAP */}
            {tipo === 'pnap' && (
              <FormGroup>
                <Label>Financiamiento UH *</Label>
                <Input value={form.financiamientoUH} onChange={set('financiamientoUH')} placeholder="Detalles del financiamiento" />
              </FormGroup>
            )}
          </Form>
        </ModalBody>
        <ModalFooter>
          <Button color="primary" onClick={handleSave} disabled={saving}>
            {saving ? <Spinner size="sm" /> : 'Guardar'}
          </Button>
          <Button color="secondary" onClick={() => setModal(false)}>Cancelar</Button>
        </ModalFooter>
      </Modal>

      {/* Delete confirmation */}
      <Modal isOpen={!!deleteTarget} toggle={() => setDeleteTarget(null)}>
        <ModalHeader toggle={() => setDeleteTarget(null)}>Confirmar eliminación</ModalHeader>
        <ModalBody>
          ¿Eliminar el proyecto <strong>«{deleteTarget?.titulo}»</strong>? Esta acción no se puede deshacer.
        </ModalBody>
        <ModalFooter>
          <Button color="danger" onClick={handleDelete} disabled={deleting}>
            {deleting ? <Spinner size="sm" /> : 'Eliminar'}
          </Button>
          <Button color="secondary" onClick={() => setDeleteTarget(null)}>Cancelar</Button>
        </ModalFooter>
      </Modal>

      {/* Publications manager modal */}
      <Modal isOpen={pubsModal} toggle={() => setPubsModal(false)} size="lg" scrollable>
        <ModalHeader toggle={() => setPubsModal(false)}>
          Publicaciones derivadas
        </ModalHeader>
        <ModalBody>
          <p className="text-muted small mb-3">
            Proyecto: <strong>{pubsTarget?.titulo}</strong>
          </p>
          {pubsError && <Alert color="danger">{pubsError}</Alert>}
          {!pubsLoading && availablePubs.length > 0 && (
            <div className="d-flex gap-2 align-items-center mb-3">
              <Input type="select" value={selectedPubToLink}
                     onChange={e => setSelectedPubToLink(e.target.value)}
                     style={{ flex: 1 }}>
                <option value="">— Vincular publicación existente —</option>
                {availablePubs.map(p => (
                  <option key={p.id} value={p.id}>{p.title}</option>
                ))}
              </Input>
              <Button color="success" size="sm" onClick={linkPub}
                      disabled={!selectedPubToLink || linkingPub}>
                {linkingPub ? <Spinner size="sm" /> : 'Vincular'}
              </Button>
            </div>
          )}
          {pubsLoading ? (
            <div className="text-center py-3"><Spinner /></div>
          ) : pubsList.length === 0 ? (
            <p className="text-muted mb-0">Sin publicaciones derivadas aún.</p>
          ) : (
            <Table bordered size="sm" className="mb-0">
              <thead>
                <tr>
                  <th>Título</th>
                  <th>URL / DOI</th>
                  <th style={{ width: 60 }}></th>
                </tr>
              </thead>
              <tbody>
                {pubsList.map(pub => (
                  <tr key={pub.id}>
                    <td>{pub.title}</td>
                    <td style={{ maxWidth: 240 }}>
                      {pub.urlDoi
                        ? <a href={pub.urlDoi.startsWith('http') ? pub.urlDoi : `https://doi.org/${pub.urlDoi}`}
                               target="_blank" rel="noopener noreferrer"
                               className="text-truncate d-block" title={pub.urlDoi}>{pub.urlDoi}</a>
                        : <span className="text-muted">—</span>}
                    </td>
                    <td className="text-center">
                      <Button color="outline-danger" size="sm" title="Desvincular del proyecto"
                              onClick={() => unlinkPub(pub.id)}>✕</Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </Table>
          )}
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={() => setPubsModal(false)}>Cerrar</Button>
        </ModalFooter>
      </Modal>

      <Modal isOpen={patentesModal} toggle={() => setPatentesModal(false)} size="lg" scrollable>
        <ModalHeader toggle={() => setPatentesModal(false)}>
          Patentes vinculadas
        </ModalHeader>
        <ModalBody>
          <p className="text-muted small mb-3">
            Proyecto: <strong>{patentesTarget?.titulo}</strong>
          </p>
          {patentesError && <Alert color="danger">{patentesError}</Alert>}
          {!patentesLoading && availablePatentes.length > 0 && (
            <div className="d-flex gap-2 align-items-center mb-3">
              <Input type="select" value={selectedPatenteToLink}
                     onChange={e => setSelectedPatenteToLink(e.target.value)}
                     style={{ flex: 1 }}>
                <option value="">— Vincular patente propia —</option>
                {availablePatentes.map(p => (
                  <option key={p.id} value={p.id}>{p.titulo}</option>
                ))}
              </Input>
              <Button color="success" size="sm" onClick={linkPatente}
                      disabled={!selectedPatenteToLink || linkingPatente}>
                {linkingPatente ? <Spinner size="sm" /> : 'Vincular'}
              </Button>
            </div>
          )}
          {!patentesLoading && (
            <InputGroup className="mb-3">
              <InputGroupText>
                <i className="bi bi-search" />
              </InputGroupText>
              <Input
                value={patentesSearch}
                onChange={e => setPatentesSearch(e.target.value)}
                placeholder="Buscar por título, número de solicitud o creador"
              />
              {patentesSearch && (
                <Button color="outline-secondary" onClick={() => setPatentesSearch('')}>
                  Limpiar
                </Button>
              )}
            </InputGroup>
          )}
          {patentesLoading ? (
            <div className="text-center py-3"><Spinner /></div>
          ) : filteredPatentes.length === 0 ? (
            <p className="text-muted mb-0">Sin patentes vinculadas aún.</p>
          ) : (
            <DataTable
              data={filteredPatentes}
              keyExtractor={item => item.patenteId}
              pageSize={5}
              columns={[
                { key: 'titulo', label: 'Título', sortable: true },
                { key: 'numeroSolicitudConcesion', label: 'Nro. solicitud', sortable: true },
                {
                  key: 'esNacional',
                  label: 'Tipo',
                  sortable: true,
                  sortValue: v => (v ? 1 : 0),
                  render: v => (v ? 'Nacional' : 'Internacional'),
                },
                { key: 'creador', label: 'Creador', sortable: true, render: v => v || '—' },
                {
                  key: 'creadores',
                  label: 'Creadores',
                  render: v => (Array.isArray(v) && v.length ? v.join(', ') : '—'),
                },
              ]}
              actionsLabel=""
              actions={[
                {
                  key: 'unlink',
                  label: 'Desvincular',
                  icon: 'bi-x-lg',
                  color: 'outline-danger',
                  onClick: item => unlinkPatente(item.patenteId),
                },
              ]}
            />
          )}
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={() => setPatentesModal(false)}>Cerrar</Button>
        </ModalFooter>
      </Modal>
    </>
  );
}
