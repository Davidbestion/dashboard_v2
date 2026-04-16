import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Table, Button, Spinner, Alert, Badge,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input,
} from 'reactstrap';
import { useAuth } from '../contexts/AuthContext';

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
  const [items, setItems] = useState([]);
  const [clasificaciones, setClasificaciones] = useState([]);
  const [jefes, setJefes] = useState([]);
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

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [proyData, clasData, jefesData] = await Promise.all([
        apiFetch('/api/Proyectos'),
        apiFetch('/api/Clasificaciones'),
        apiFetch('/api/Users/jefes-de-proyecto'),
      ]);
      setItems(proyData);
      setClasificaciones(clasData);
      setJefes(jefesData);
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

  const tipo = form.tipo;

  return (
    <>
      <Card>
        <CardHeader className="d-flex justify-content-between align-items-center">
          <strong>Proyectos</strong>
          <Button color="primary" size="sm" onClick={openCreate}>+ Nuevo proyecto</Button>
        </CardHeader>
        <CardBody>
          {error && <Alert color="danger">{error}</Alert>}
          {loading ? (
            <div className="text-center py-4"><Spinner /></div>
          ) : (
            <Table bordered hover responsive size="sm">
              <thead>
                <tr>
                  <th>Tipo</th>
                  <th>Título</th>
                  <th>Jefe</th>
                  <th>Clasificación</th>
                  <th style={{ width: 100 }}>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {items.length === 0 && (
                  <tr><td colSpan={5} className="text-center text-muted">No hay proyectos registrados.</td></tr>
                )}
                {items.map(item => (
                  <tr key={item.id}>
                    <td><Badge color={tipoColor(item.tipo)}>{tipoLabel(item.tipo)}</Badge></td>
                    <td>{item.titulo}</td>
                    <td>{item.jefe}</td>
                    <td>{item.clasificacionNombre}</td>
                    <td>
                      <Button color="outline-secondary" size="sm" className="me-1" onClick={() => openEdit(item)} disabled={loadingEdit}>✏️</Button>
                      <Button color="outline-danger" size="sm" onClick={() => setDeleteTarget(item)}>🗑️</Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </Table>
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
                  <Input value={form.tipoRevision} onChange={set('tipoRevision')} placeholder="Tipo de proyecto en revisión" />
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
    </>
  );
}
