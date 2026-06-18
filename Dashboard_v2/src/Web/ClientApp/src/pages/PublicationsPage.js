import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Nav, NavItem, NavLink,
  TabContent, TabPane,
  Table, Button, Badge,
  Spinner, Alert,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Form, FormGroup, Label, Input,
  ButtonGroup, ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem,
  UncontrolledPopover, PopoverHeader, PopoverBody,
} from 'reactstrap';
import { useAuth } from '../contexts/AuthContext';
import CoauthorPicker from '../components/CoauthorPicker';
import UserPicker from '../components/UserPicker';
import AuthorResolutionModal from '../components/AuthorResolutionModal';
import FilterableDataTable from '../components/FilterableDataTable';
import CertificateUpload, { CertificateViewButton } from '../components/CertificateUpload';

function SingleSelectPicker({ label, options, value, onChange, onCreate }) {
  const [inputVal, setInputVal] = useState('');
  const [open, setOpen] = useState(false);
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState('');

  const filtered = options.filter(o =>
    o.nombre.toLowerCase().includes(inputVal.toLowerCase())
  );
  const hasExactMatch = options.some(
    o => o.nombre.toLowerCase() === inputVal.trim().toLowerCase()
  );
  const canCreate = onCreate && inputVal.trim().length > 0 && !hasExactMatch;
  const showDropdown = open && (filtered.length > 0 || canCreate);

  function select(nombre) {
    onChange(nombre);
    setInputVal('');
    setOpen(false);
  }

  async function handleCreate() {
    if (!canCreate || creating) return;
    setCreating(true);
    setCreateError('');
    try {
      await onCreate(inputVal.trim());
      onChange(inputVal.trim());
      setInputVal('');
      setOpen(false);
    } catch (e) {
      setCreateError(e.message);
    } finally {
      setCreating(false);
    }
  }

  return (
    <FormGroup>
      <Label>{label}</Label>
      {value && (
        <div className="d-flex align-items-center gap-1 mb-2">
          <Badge color="secondary" className="d-flex align-items-center gap-1 py-1 px-2">
            {value}
            <i className="bi bi-x" style={{ cursor: 'pointer' }} onClick={() => onChange('')} />
          </Badge>
        </div>
      )}
      <div style={{ position: 'relative' }}>
        <Input
          value={inputVal}
          onChange={e => { setInputVal(e.target.value); setOpen(true); }}
          onFocus={() => setOpen(true)}
          onBlur={() => setTimeout(() => setOpen(false), 150)}
          placeholder={value ? 'Cambiar...' : (onCreate ? 'Buscar o escribir para crear...' : 'Buscar...')}
        />
        {showDropdown && (
          <div style={{
            position: 'absolute', top: '100%', left: 0, right: 0, zIndex: 9999,
            background: '#fff', border: '1px solid #dee2e6',
            borderRadius: '0 0 0.25rem 0.25rem', maxHeight: '180px', overflowY: 'auto',
            boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
          }}>
            {filtered.slice(0, 12).map(o => (
              <div key={o.id} className="dropdown-item"
                style={{ cursor: 'pointer', padding: '0.4rem 0.75rem' }}
                onMouseDown={e => { e.preventDefault(); select(o.nombre); }}>
                {o.nombre}
              </div>
            ))}
            {canCreate && (
              <div className="dropdown-item text-primary"
                style={{
                  cursor: creating ? 'default' : 'pointer',
                  padding: '0.4rem 0.75rem', fontStyle: 'italic',
                  borderTop: filtered.length > 0 ? '1px solid #dee2e6' : 'none',
                }}
                onMouseDown={e => { e.preventDefault(); handleCreate(); }}>
                {creating ? <Spinner size="sm" /> : `+ Crear "${inputVal.trim()}"`}
              </div>
            )}
          </div>
        )}
      </div>
      {createError && <small className="text-danger">{createError}</small>}
    </FormGroup>
  );
}

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
      // { errors: string[] }  — nuestro formato
      // { errors: { Field: string[] } } — ProblemDetails con validación
      const errs = Array.isArray(data.errors)
        ? data.errors
        : Object.values(data.errors).flat();
      message = errs.join(' ');
    } else if (data?.title) {
      // ProblemDetails estándar de ASP.NET Core
      message = data.title;
    } else {
      message = `Error ${response.status}`;
    }
    throw new Error(message);
  }
  return data;
}

const EMPTY_FORM = {
  title: '',
  publicationData: '',
  urlDoi: '',
  publishedDate: '',
  publicationType: 0,
  proyectoId: '',
  // Indexadas (tipos 1-4)
  index: '',
  // Revista (tipo 0)
  dataBase: '',
  group: '',
  cuartil: '',
  evidenceFileId: null,
  targetUserId: '',
};

export default function PublicationsPage() {
  const { user } = useAuth();
  const isSuperuser = user?.role === 'Superuser';
  const [publications, setPublications] = useState([]);
  const [types, setTypes] = useState([]);
  const [basesdedatos, setBasesdedatos] = useState([]);
  const [proyectos, setProyectos] = useState([]);
  const [proyectosError, setProyectosError] = useState(false);
  const [allUsers, setAllUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Modal de crear / editar
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState(null); // null = crear, objeto = editar
  const [form, setForm] = useState(EMPTY_FORM);
  const [formError, setFormError] = useState('');
  const [formLoading, setFormLoading] = useState(false);
  // Duplicates modal state
  const [duplicatesModal, setDuplicatesModal] = useState(false);
  const [duplicatesCandidates, setDuplicatesCandidates] = useState([]);
  const [duplicatesError, setDuplicatesError] = useState('');
  const [duplicatesLoading, setDuplicatesLoading] = useState(false);

  // CrossRef search
  const [crossrefModal, setCrossrefModal] = useState(false);
  const [crossrefCandidates, setCrossrefCandidates] = useState([]);
  const [crossrefError, setCrossrefError] = useState('');
  const [crossrefLoading, setCrossrefLoading] = useState(false);
  // Resolver ahora state
  const [resolveLoading, setResolveLoading] = useState(false);
  const [resolveError, setResolveError] = useState('');
  const [resolveSuccess, setResolveSuccess] = useState('');
  const [resolvedIssns, setResolvedIssns] = useState([]);
  // Metadata search dropdown
  const [metaDropdownOpen, setMetaDropdownOpen] = useState(false);
  const [metaSearchLoading, setMetaSearchLoading] = useState(false);

  // Author resolution modal (after CrossRef / OpenAIRE import)
  const [authorResolutionModal, setAuthorResolutionModal] = useState(false);
  const [authorResolutionItems, setAuthorResolutionItems] = useState([]);
  const [authorResolutionLoading, setAuthorResolutionLoading] = useState(false);
  // True when the current user has a linked Author but was NOT found among the CrossRef authors
  const [authorNotInCrossRef, setAuthorNotInCrossRef] = useState(false);
  const [authorWarningConfirmOpen, setAuthorWarningConfirmOpen] = useState(false);

  // Detalles modal para ver una publicación candidata
  const [detailsModal, setDetailsModal] = useState(false);
  const [detailsPublication, setDetailsPublication] = useState(null);
  const [detailsLoading, setDetailsLoading] = useState(false);
  const [detailsError, setDetailsError] = useState('');
  const [detailsReturnToCrossRef, setDetailsReturnToCrossRef] = useState(false);

  // Modal de confirmación de borrado
  const [deleteModal, setDeleteModal] = useState(false);
  const [toDelete, setToDelete] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [deleteError, setDeleteError] = useState('');
  // Pestañas
  const [activeType, setActiveType] = useState(0);
  const [activeGroup, setActiveGroup] = useState(1);
  // Tag-picker de coautores
  const [coauthorTags, setCoauthorTags] = useState([]); // [{id?, name}]
  const loadData = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [pubs, pubTypes, cats, dbs] = await Promise.all([
        apiFetch(isSuperuser ? '/api/Publications/todas' : '/api/Publications'),
        apiFetch('/api/Publications/types'),
        apiFetch('/api/Proyectos/catalogo').catch(() => null),
        apiFetch('/api/Nomencladores/basesdedatos'),
      ]);
      setPublications(pubs);
      setTypes(pubTypes);
      setBasesdedatos(dbs);
      if (cats === null) {
        setProyectosError(true);
        setProyectos([]);
      } else {
        setProyectosError(false);
        setProyectos(cats);
      }
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, [isSuperuser]);

  useEffect(() => { loadData(); }, [loadData]);
  useEffect(() => {
    if (!isSuperuser) return;
    apiFetch('/api/Users').then(setAllUsers).catch(() => {});
  }, [isSuperuser]);

  async function createBaseDeDatos(nombre) {
    const created = await apiFetch('/api/Nomencladores/basesdedatos', {
      method: 'POST', body: JSON.stringify({ nombre }),
    });
    setBasesdedatos(prev => [...prev, created].sort((a, b) => a.nombre.localeCompare(b.nombre)));
    return created;
  }

  // ── helpers de formulario ──────────────────────────────────────────────────

  function openCreate(typeVal, groupVal) {
    setEditing(null);
    setForm({
      ...EMPTY_FORM,
      publicationType: typeVal ?? types[0]?.value ?? 0,
      group: groupVal != null ? String(groupVal) : '',
    });
    setFormError('');
    setResolveError('');
    setResolveSuccess('');
    setResolvedIssns([]);
    setCoauthorTags([]);
    setAuthorNotInCrossRef(false);
    setModal(true);
  }

  /**
   * Convierte un autor de la API al modelo consumido por el picker de tarjetas.
   * Si el autor está vinculado a un usuario, conserva ese snapshot para renderizar la tarjeta rica.
   */
  function mapAuthorToPickerEntry(author) {
    return {
      id: author.id,
      name: author.name,
      type: 'author',
      linkedUser: author.linkedUser ?? null,
    };
  }

  function openEdit(pub) {
    setEditing(pub);
    setForm({
      title: pub.title,
      publicationData: pub.publicationData,
      urlDoi: pub.urlDoi ?? '',
      publishedDate: pub.publishedDate ?? '',
      publicationType: pub.publicationType,
      proyectoId: pub.proyectoId ?? '',
      // Indexadas
      index: pub.indexedPublication?.index ?? '',
      // Revista
      dataBase: pub.journalPublication?.dataBase ?? '',
      group: pub.journalPublication?.group ?? '',
      cuartil: pub.journalPublication?.cuartil ?? '',
      evidenceFileId: pub.evidenceFileId ?? null,
    });
    setFormError('');
    setResolveError('');
    setResolveSuccess('');
    setResolvedIssns([]);
    setAuthorNotInCrossRef(false);
    // Pre-cargar coautores (todos excepto el usuario actual)
    const initialTags = (pub.authors ?? [])
      .filter(a => a.userId !== user?.id)
      .map(mapAuthorToPickerEntry);
    setCoauthorTags(initialTags);
    setModal(true);
  }

  /**
   * Infers the publication group (1–4) from a database name using the same
   * rules as the backend PublicationGroupMapper.
   */
  function inferGroupFromDatabase(db) {
    if (!db) return '';
    const d = db.toLowerCase();
    if (d.includes('scopus') || d.includes('scimago') || d.includes('web of science') ||
        d.includes('scie') || d.includes('ssci') || d.includes('ahci')) return '1';
    if (d.includes('scielo') || d.includes('medline') || d.includes('emerging') ||
        d.includes('chemical') || d.includes('biosis') || d.includes('compendex') ||
        d.includes('cab') || d.includes('inspec')) return '2';
    if (d.includes('doaj') || d.includes('lilacs') || d.includes('redalyc') ||
        d.includes('latindex') || d.includes('ime') || d.includes('icyt') ||
        d.includes('periodica') || d.includes('clase')) return '3';
    return '';
  }

  function handleFormChange(e) {
    const { name, type, value, checked } = e.target;
    if (name === 'dataBase') {
      // Auto-infer group whenever the user changes the database name field.
      const inferred = inferGroupFromDatabase(value);
      setForm(f => ({ ...f, dataBase: value, group: inferred || f.group }));
      return;
    }
    if (name === 'publicationType') {
      // Clear resolve states when switching type to avoid stale journal messages.
      setResolveSuccess('');
      setResolveError('');
      setResolvedIssns([]);
    }
    setForm(f => ({ ...f, [name]: type === 'checkbox' ? checked : value }));
  }

  function canSearchCrossRef() {
    return Boolean(form.title.trim() || form.urlDoi.trim());
  }

  async function handleSubmit() {
    if (!form.title.trim() || form.publicationType === '') {
      setFormError('El título y el tipo de publicación son obligatorios.');
      return;
    }
    if (isSuperuser && !editing && !form.targetUserId) {
      setFormError('Debes seleccionar el autor principal.');
      return;
    }
    if (!form.publishedDate.trim() || !/^\d{4}(-\d{2}(-\d{2})?)?$/.test(form.publishedDate.trim())) {
      setFormError('La fecha de publicación es obligatoria. Use el formato AAAA, AAAA-MM o AAAA-MM-DD.');
      return;
    }
    // If the user was not found among CrossRef authors, ask for confirmation first
    if (authorNotInCrossRef) {
      setAuthorWarningConfirmOpen(true);
      return;
    }
    setFormLoading(true);
    setFormError('');
    try {
      if (editing) {
        // PUT — actualizar: primero comprobar duplicados (excluir la propia publicación)
        try {
          setDuplicatesLoading(true);
          setDuplicatesError('');
          const candidates = await apiFetch(`/api/Publications/duplicates?title=${encodeURIComponent(form.title)}&doi=${encodeURIComponent(form.urlDoi || '')}&url=&excludeId=${encodeURIComponent(editing.id)}`);
          if (candidates && candidates.length > 0) {
            setDuplicatesCandidates(candidates);
            setDuplicatesModal(true);
            return; // wait for user's decision in modal
          }

          // no duplicates -> actualizar inmediatamente
          await performUpdate();
        } finally {
          setDuplicatesLoading(false);
        }
      } else {
        // POST — crear: primero comprobar duplicados
        try {
          setDuplicatesLoading(true);
          setDuplicatesError('');
          const candidates = await apiFetch(`/api/Publications/duplicates?title=${encodeURIComponent(form.title)}&doi=${encodeURIComponent(form.urlDoi || '')}&url=`);
          if (candidates && candidates.length > 0) {
            setDuplicatesCandidates(candidates);
            setDuplicatesModal(true);
            return; // wait for user's decision in modal
          }

          // no duplicates -> crear inmediatamente
          await performCreate();
        } finally {
          setDuplicatesLoading(false);
        }
      }
      setModal(false);
      loadData();
    } catch (e) {
      setFormError(e.message);
    } finally {
      setFormLoading(false);
    }
  }

  async function performCreate() {
    await apiFetch('/api/Publications', {
      method: 'POST',
      body: JSON.stringify({
        title: form.title,
        publicationData: form.publicationData,
        publicationType: parseInt(form.publicationType, 10),
        urlDoi: form.urlDoi || null,
        publishedDate: form.publishedDate || null,
        proyectoId: form.proyectoId || null,
        additionalAuthorIds: coauthorTags.filter(t => t.type === 'author').map(t => t.id),
        additionalAuthorNames: coauthorTags.filter(t => t.type === 'new').map(t => t.name),
        additionalUserIds: coauthorTags.filter(t => t.type === 'user').map(t => t.id),
        // Especialización
        index: parseInt(form.publicationType, 10) !== 0 ? (parseInt(form.index, 10) || null) : null,
        dataBase: parseInt(form.publicationType, 10) === 0 ? form.dataBase || null : null,
        group: parseInt(form.publicationType, 10) === 0 ? parseInt(form.group, 10) || null : null,
        cuartil: parseInt(form.publicationType, 10) === 0 && parseInt(form.group, 10) === 1 ? form.cuartil || null : null,
        evidenceFileId: form.evidenceFileId ?? null,
        ...(isSuperuser ? { targetUserId: form.targetUserId } : {}),
      }),
    });
    setModal(false);
    loadData();
  }

  async function performUpdate() {
    if (!editing) return;
    await apiFetch(`/api/Publications/${editing.id}`, {
      method: 'PUT',
      body: JSON.stringify({
        title: form.title,
        publicationData: form.publicationData,
        publicationType: parseInt(form.publicationType, 10),
        urlDoi: form.urlDoi || null,
        publishedDate: form.publishedDate || null,
        proyectoId: form.proyectoId || null,
        additionalAuthorIds: coauthorTags.filter(t => t.type === 'author').map(t => t.id),
        additionalAuthorNames: coauthorTags.filter(t => t.type === 'new').map(t => t.name),
        additionalUserIds: coauthorTags.filter(t => t.type === 'user').map(t => t.id),
        // Especialización
        index: parseInt(form.publicationType, 10) !== 0 ? (parseInt(form.index, 10) || null) : null,
        dataBase: parseInt(form.publicationType, 10) === 0 ? form.dataBase || null : null,
        group: parseInt(form.publicationType, 10) === 0 ? parseInt(form.group, 10) || null : null,
        cuartil: parseInt(form.publicationType, 10) === 0 && parseInt(form.group, 10) === 1 ? form.cuartil || null : null,
        evidenceFileId: form.evidenceFileId ?? null,
      }),
    });
    setModal(false);
    setEditing(null);
    loadData();
  }

  // ── borrado ────────────────────────────────────────────────────────────────

  function openDelete(pub) {
    setToDelete(pub);
    setDeleteError('');
    setDeleteModal(true);
  }

  async function handleDelete() {
    if (!toDelete) return;
    setDeleteLoading(true);
    setDeleteError('');
    try {
      await apiFetch(`/api/Publications/${toDelete.id}`, { method: 'DELETE' });
      setDeleteModal(false);
    } catch (e) {
      setDeleteError(e.message);
    } finally {
      setDeleteLoading(false);
    }
  }

  // ── render ─────────────────────────────────────────────────────────────────

  const pubsByType = (typeVal) => publications.filter(p => p.publicationType === typeVal);
  const journalByGroup = (g) => publications.filter(p => p.publicationType === 0 && p.journalPublication?.group === g);

  const authorsList = (authors) => authors.map((a, i) => (
    <span key={a.id}>
      {i > 0 && <span className="text-muted me-1">,</span>}
      {a.name}
      {a.userId && (
        <i className="bi bi-person-check ms-1 text-success" title="Usuario registrado"></i>
      )}
    </span>
  ));

  const pubActions = [
    { key: 'edit',   label: 'Editar',   icon: 'bi-pencil', color: 'outline-primary', onClick: (pub) => openEdit(pub) },
    { key: 'delete', label: 'Eliminar', icon: 'bi-trash',  color: 'outline-danger',  onClick: (pub) => openDelete(pub) },
    { key: 'certificate', label: 'Certificado', show: pub => pub.evidenceFileId != null,
      render: pub => <CertificateViewButton fileId={pub.evidenceFileId} /> },
  ];

  function renderMatchLabel(type, score) {
    if (!type) return <span className="text-muted">—</span>;
    const map = {
      doi: 'DOI (coincidencia exacta)',
      url: 'URL (coincidencia)',
      title: 'Título (coincidencia exacta)'
    };
    const label = map[type] ?? type;
    return (
      <div>
        <strong>{label}</strong>
        {typeof score === 'number' && score < 1 && (
          <div className="text-muted" style={{ fontSize: '0.85em' }}>similitud: {(score * 100).toFixed(0)}%</div>
        )}
      </div>
    );
  }

  async function viewDetails(id) {
    setDetailsReturnToCrossRef(false);
    setDetailsError('');
    setDetailsLoading(true);
    try {
      const pub = await apiFetch(`/api/Publications/public/${id}`);
      setDetailsPublication(pub);
      setDetailsModal(true);
    } catch (e) {
      setDetailsError(e.message);
    } finally {
      setDetailsLoading(false);
    }
  }

  async function searchCrossRef() {
    if (!canSearchCrossRef()) {
      setCrossrefError('Escribe al menos un título o un DOI antes de buscar en CrossRef.');
      return;
    }

    setCrossrefError('');
    setMetaSearchLoading(true);
    setCrossrefLoading(true);
    try {
      const res = await apiFetch(`/api/Publications/crossref?doi=${encodeURIComponent(form.urlDoi || '')}&title=${encodeURIComponent(form.title || '')}`);
      setCrossrefCandidates(res || []);
      setCrossrefModal(true);
    } catch (e) {
      setCrossrefError(e.message);
    } finally {
      setCrossrefLoading(false);
      setMetaSearchLoading(false);
    }
  }

  async function searchOpenAire() {
    if (!canSearchCrossRef()) {
      setCrossrefError('Escribe al menos un título o un DOI antes de buscar en OpenAIRE.');
      return;
    }

    setCrossrefError('');
    setMetaSearchLoading(true);
    setCrossrefLoading(true);
    try {
      const res = await apiFetch(`/api/Publications/openaire?doi=${encodeURIComponent(form.urlDoi || '')}&title=${encodeURIComponent(form.title || '')}`);
      setCrossrefCandidates(res || []);
      setCrossrefModal(true);
    } catch (e) {
      setCrossrefError(e.message);
    } finally {
      setCrossrefLoading(false);
      setMetaSearchLoading(false);
    }
  }

  async function resolveDatabaseNow() {
    setResolveError('');
    setResolveSuccess('');
    setResolveLoading(true);
    try {
      // If we already have ISSNs from a previous metadata search, send them
      // directly to avoid a redundant CrossRef call.
      let url;
      if (resolvedIssns.length > 0) {
        url = `/api/Publications/resolve-database?issns=${encodeURIComponent(resolvedIssns.join(','))}`;
      } else {
        if (!canSearchCrossRef()) {
          setResolveError('Escribe al menos un título o un DOI antes de resolver.');
          setResolveLoading(false);
          return;
        }
        url = `/api/Publications/resolve-database?doi=${encodeURIComponent(form.urlDoi || '')}&title=${encodeURIComponent(form.title || '')}`;
      }
      const res = await apiFetch(url);
      if (res) {
        const resolved = !!res.databaseName;
        setForm(f => ({
          ...f,
          dataBase: res.databaseName ?? f.dataBase,
          group: res.group != null ? String(res.group) : (res.databaseName ? inferGroupFromDatabase(res.databaseName) : f.group),
          cuartil: res.cuartil ?? f.cuartil,
        }));
        if (resolved) {
          setResolveSuccess(`Base de datos resuelta: ${res.databaseName} (Grupo ${res.group}${res.cuartil ? ', ' + res.cuartil : ''})`);
        } else {
          const issnText = res.issns?.length > 0
            ? `ISSNs encontrados en CrossRef: ${res.issns.join(', ')}. `
            : '';
          const reason = res.message
            ?? `${issnText}No se pudo determinar la base de datos automáticamente (la revista no está indexada en DOAJ ni en los archivos CSV configurados). Por favor complete los campos manualmente.`;
          setResolveError(reason);
        }
      }
    } catch (e) {
      setResolveError(e.message);
    } finally {
      setResolveLoading(false);
    }
  }

  function importFromCrossRef(candidate) {
    if (!candidate) return;
    setForm(f => ({
      ...f,
      title: candidate.title || f.title,
      urlDoi: candidate.doi ? `https://doi.org/${candidate.doi}` : (candidate.url || f.urlDoi),
      publishedDate: candidate.published || f.publishedDate,
      publicationData: candidate.publicationData || f.publicationData,
      publicationType: candidate.suggestedPublicationType ?? f.publicationType,
    }));
    // Cache ISSNs so resolve-database can skip the CrossRef round-trip.
    setResolvedIssns(candidate.issns ?? []);
    setCrossrefModal(false);

    // Resolve authors: ask the backend if any of the external names are already
    // in the system before adding them to the coauthor picker.
    const externalAuthors = candidate.authors ?? [];
    if (externalAuthors.length > 0) {
      setAuthorResolutionLoading(true);
      apiFetch('/api/Authors/resolve-external', {
        method: 'POST',
        body: JSON.stringify({ names: externalAuthors }),
      })
        .then(items => {
          setAuthorResolutionItems(items ?? []);
          setAuthorResolutionModal(true);
        })
        .catch(() => {
          // On error fall back to adding all as new authors
          const fallbackTags = externalAuthors.map(name => ({
            id: null, name, type: 'new', linkedUser: null,
          }));
          setCoauthorTags(prev => {
            const existing = new Set(prev.map(t => (t.id ?? t.name).toLowerCase()));
            const added = fallbackTags.filter(t => !existing.has(t.name.toLowerCase()));
            return [...prev, ...added];
          });
        })
        .finally(() => setAuthorResolutionLoading(false));
    }
  }

  function buildCrossRefPreview(candidate) {
    const publicationType = candidate?.suggestedPublicationType ?? null;
    const publicationTypeName = publicationType == null
      ? null
      : (types.find(t => t.value === publicationType)?.name ?? null);

    return {
      title: candidate?.title ?? 'Sin título',
      publicationData: candidate?.publicationData ?? '',
      urlDoi: candidate?.doi ? `https://doi.org/${candidate.doi}` : (candidate?.url ?? null),
      authors: (candidate?.authors ?? []).map((name, index) => ({
        id: `crossref-author-${index}`,
        name,
        linkedUser: null,
      })),
      publicationTypeName,
      crossRef: {
        doi: candidate?.doi ?? null,
        url: candidate?.url ?? null,
        containerTitle: candidate?.containerTitle ?? null,
        publisher: candidate?.publisher ?? null,
        published: candidate?.published ?? null,
        type: candidate?.type ?? null,
        volume: candidate?.volume ?? null,
        issue: candidate?.issue ?? null,
        page: candidate?.page ?? null,
        issns: candidate?.issns ?? [],
        isbns: candidate?.isbns ?? [],
      },
    };
  }

  function previewCrossRefCandidate(candidate) {
    setDetailsReturnToCrossRef(true);
    setDetailsError('');
    setDetailsLoading(false);
    setDetailsPublication(buildCrossRefPreview(candidate));
    setCrossrefModal(false);
    setDetailsModal(true);
  }

  function closeDetailsModal() {
    setDetailsModal(false);
    setDetailsError('');
    setDetailsLoading(false);
    setDetailsPublication(null);

    if (detailsReturnToCrossRef) {
      setCrossrefModal(true);
      setDetailsReturnToCrossRef(false);
    }
  }

  function renderPublicationDetails(pub) {
    if (!pub) return null;

    const isCrossRefPreview = Boolean(pub.crossRef);

    return (
      <div>
        <h5>{pub.title}</h5>
        {pub.publicationTypeName && (
          <p><strong>Tipo:</strong> {pub.publicationTypeName}</p>
        )}
        {!isCrossRefPreview && pub.publicationData && (
          <p style={{ whiteSpace: 'pre-line' }}>{pub.publicationData}</p>
        )}
        <p>
          <strong>URL / DOI:</strong>{' '}
          {pub.urlDoi
            ? <a href={pub.urlDoi} target="_blank" rel="noopener noreferrer">{pub.urlDoi}</a>
            : <span className="text-muted">—</span>}
        </p>
        <p><strong>Autores:</strong></p>
        <ul>
          {(pub.authors || []).length > 0
            ? (pub.authors || []).map(a => (
              <li key={a.id}>
                {a.name}
                {a.linkedUser ? ' (usuario del sistema)' : ''}
              </li>
            ))
            : <li className="text-muted">—</li>}
        </ul>
        {pub.journalPublication && (
          <p><strong>Base de datos:</strong> {pub.journalPublication.dataBase} (Grupo {pub.journalPublication.group}) {pub.journalPublication.cuartil && <>- {pub.journalPublication.cuartil}</>}</p>
        )}
        {pub.indexedPublication && (
          <p><strong>Indexación:</strong> {pub.indexedPublication.index ? `Grupo ${pub.indexedPublication.index}` : '—'}</p>
        )}
        {pub.proyectoTitulo && (
          <p><strong>Proyecto vinculado:</strong> {pub.proyectoTitulo}</p>
        )}
        {pub.crossRef && (
          <>
            <p><strong>Fuente CrossRef:</strong> {pub.crossRef.containerTitle ?? <span className="text-muted">—</span>}</p>
            <p><strong>Publisher:</strong> {pub.crossRef.publisher ?? <span className="text-muted">—</span>}</p>
            <p><strong>Fecha publicada:</strong> {pub.crossRef.published ?? <span className="text-muted">—</span>}</p>
            <p><strong>Tipo CrossRef:</strong> {pub.crossRef.type ?? <span className="text-muted">—</span>}</p>
            <p><strong>Volumen:</strong> {pub.crossRef.volume ?? <span className="text-muted">—</span>}</p>
            <p><strong>Número:</strong> {pub.crossRef.issue ?? <span className="text-muted">—</span>}</p>
            <p><strong>Páginas:</strong> {pub.crossRef.page ?? <span className="text-muted">—</span>}</p>
            <p><strong>ISSN:</strong> {pub.crossRef.issns?.length ? pub.crossRef.issns.join(', ') : <span className="text-muted">—</span>}</p>
            <p><strong>ISBN:</strong> {pub.crossRef.isbns?.length ? pub.crossRef.isbns.join(', ') : <span className="text-muted">—</span>}</p>
          </>
        )}
      </div>
    );
  }

  const urlCell = (urlDoi) => urlDoi
    ? <a href={urlDoi} target="_blank" rel="noopener noreferrer"
         className="text-truncate d-block" style={{ maxWidth: 170 }} title={urlDoi}>{urlDoi}</a>
    : <span className="text-muted">—</span>;

  return (
    <>
      <Card className="shadow-sm">
        <CardHeader><strong>Mis publicaciones</strong></CardHeader>
        <CardBody>
          {loading && <div className="text-center py-4"><Spinner /></div>}
          {error && <Alert color="danger">{error}</Alert>}

          {!loading && !error && (
            <>
              {/* ── Pestañas de tipo ── */}
              <Nav tabs>
                {types.map(t => (
                  <NavItem key={t.value}>
                    <NavLink
                      href="#"
                      active={activeType === t.value}
                      onClick={e => { e.preventDefault(); setActiveType(t.value); }}
                    >
                      {t.name}{' '}
                      <Badge color="secondary" pill style={{ fontSize: '0.7em' }}>
                        {pubsByType(t.value).length}
                      </Badge>
                    </NavLink>
                  </NavItem>
                ))}
              </Nav>

              <TabContent activeTab={String(activeType)} className="border border-top-0 rounded-bottom">

                {/* ── Pestaña Journal (tipo 0) ── */}
                <TabPane tabId="0" className="p-3">
                  <div className="d-flex align-items-center mb-3">
                    <Nav tabs className="flex-grow-1">
                      {[1, 2, 3, 4].map(g => (
                        <NavItem key={g}>
                          <NavLink
                            href="#"
                            active={activeGroup === g}
                            onClick={e => { e.preventDefault(); setActiveGroup(g); }}
                          >
                            Grupo {g}{' '}
                            <Badge color="secondary" pill style={{ fontSize: '0.7em' }}>
                              {journalByGroup(g).length}
                            </Badge>
                          </NavLink>
                        </NavItem>
                      ))}
                    </Nav>
                    <Button color="primary" size="sm" className="ms-3 flex-shrink-0"
                      onClick={() => openCreate(0, activeGroup)} disabled={loading}>
                      <i className="bi bi-plus-lg me-1"></i> Nueva publicación
                    </Button>
                  </div>

                  <TabContent activeTab={String(activeGroup)}>
                    {[1, 2, 3, 4].map(g => {
                      const pubs = journalByGroup(g);
                      return (
                        <TabPane tabId={String(g)} key={g}>
                          <FilterableDataTable
                            filterConfig={{ search: { fields: ['title', 'publicationData'], placeholder: 'Buscar publicación...' } }}
                            columns={[
                              { key: 'title', label: 'Título', sortable: true, render: (value, item) => (
                                <>
                                  <span>{value}</span>
                                  {item.publicationData && (
                                    <>
                                      <i id={`pubinfo-${item.id}`} className="bi bi-info-circle ms-1 text-muted" style={{ cursor: 'help', fontSize: '0.85em' }} />
                                      <UncontrolledPopover trigger="hover focus" target={`pubinfo-${item.id}`} placement="right">
                                        <PopoverHeader>Datos de la publicación</PopoverHeader>
                                        <PopoverBody style={{ whiteSpace: 'pre-line', maxWidth: 350 }}>{item.publicationData}</PopoverBody>
                                      </UncontrolledPopover>
                                    </>
                                  )}
                                </>
                              )},
                              { key: 'publishedDate', label: 'Fecha', sortable: true },
                              { key: 'journalPublication.dataBase', label: 'Base de datos' },
                              ...(g === 1 ? [{
                                key: 'journalPublication.cuartil',
                                label: 'Cuartil',
                                render: v => v != null
                                  ? <Badge color="info" pill className="text-dark">{v}</Badge>
                                  : <span className="text-muted">—</span>,
                              }] : []),
                              { key: 'urlDoi',   label: 'URL / DOI', render: v => urlCell(v) },
                              { key: 'authors',  label: 'Autores',   render: v => authorsList(v ?? []) },
                            ]}
                            data={pubs}
                            keyExtractor={pub => pub.id}
                            actions={pubActions}
                            emptyMessage={`No hay publicaciones en el Grupo ${g}.`}
                            detailConfig
                          />
                        </TabPane>
                      );
                    })}
                  </TabContent>
                </TabPane>

                {/* ── Pestañas tipos indexados (1–4) ── */}
                {[1, 2, 3, 4].map(typeVal => {
                  const pubs = pubsByType(typeVal);
                  return (
                    <TabPane tabId={String(typeVal)} key={typeVal} className="p-3">
                      <div className="d-flex justify-content-end mb-2">
                        <Button color="primary" size="sm"
                          onClick={() => openCreate(typeVal, null)} disabled={loading}>
                          <i className="bi bi-plus-lg me-1"></i> Nueva publicación
                        </Button>
                      </div>
                      <FilterableDataTable
                        filterConfig={{ search: { fields: ['title', 'publicationData'], placeholder: 'Buscar publicación...' } }}
                        columns={[
                          { key: 'title', label: 'Título', sortable: true, render: (value, item) => (
                            <>
                              <span>{value}</span>
                              {item.publicationData && (
                                <>
                                  <i id={`pubinfo-${item.id}`} className="bi bi-info-circle ms-1 text-muted" style={{ cursor: 'help', fontSize: '0.85em' }} />
                                  <UncontrolledPopover trigger="hover focus" target={`pubinfo-${item.id}`} placement="right">
                                    <PopoverHeader>Datos de la publicación</PopoverHeader>
                                    <PopoverBody style={{ whiteSpace: 'pre-line', maxWidth: 350 }}>{item.publicationData}</PopoverBody>
                                  </UncontrolledPopover>
                                </>
                              )}
                            </>
                          )},
                          { key: 'publishedDate', label: 'Fecha', sortable: true },
                          { key: 'indexedPublication.index', label: 'Indexación',  render: v => v ? `Grupo ${v}` : <span className="text-muted">—</span> },
                          { key: 'urlDoi',  label: 'URL / DOI', render: v => urlCell(v) },
                          { key: 'authors', label: 'Autores',   render: v => authorsList(v ?? []) },
                        ]}
                        data={pubs}
                        keyExtractor={pub => pub.id}
                        actions={pubActions}
                        emptyMessage="No hay publicaciones de este tipo."
                        detailConfig
                      />
                    </TabPane>
                  );
                })}

              </TabContent>
            </>
          )}
        </CardBody>
      </Card>

      {/* ── Modal crear / editar ── */}
      <Modal isOpen={modal} toggle={() => setModal(false)} size="lg">
        <ModalHeader toggle={() => setModal(false)}>
          {editing ? 'Editar publicación' : 'Nueva publicación'}
        </ModalHeader>
        <ModalBody>
          {formError && <Alert color="danger">{formError}</Alert>}
          <Form>
            {/* ══ Sección 1: datos principales ══════════════════════════ */}
            {isSuperuser && !editing && (
              <FormGroup>
                <Label>Autor principal (usuario) <span className="text-danger">*</span></Label>
                <small className="d-block text-muted mb-1">
                  El usuario cuyo perfil de autor quedará vinculado como autor principal.
                </small>
                <UserPicker
                  users={allUsers}
                  value={form.targetUserId}
                  onChange={id => setForm(f => ({ ...f, targetUserId: id }))}
                />
              </FormGroup>
            )}
            <FormGroup>
              <Label for="title">Título <span className="text-danger">*</span></Label>
              <Input
                id="title"
                name="title"
                value={form.title}
                onChange={handleFormChange}
                placeholder="Título de la publicación"
              />
            </FormGroup>
            <div className="row g-3 mb-3">
              <div className="col-md-6">
                <Label for="publicationType">
                  Tipo <span className="text-danger">*</span>
                </Label>
                <Input
                  type="select"
                  id="publicationType"
                  name="publicationType"
                  value={form.publicationType}
                  onChange={handleFormChange}
                >
                  {types.map(t => (
                    <option key={t.value} value={t.value}>{t.name}</option>
                  ))}
                </Input>
              </div>
              <div className="col-md-6">
                <Label for="publishedDate">Fecha de publicación <span className="text-danger">*</span></Label>
                <Input
                  id="publishedDate"
                  name="publishedDate"
                  value={form.publishedDate}
                  onChange={handleFormChange}
                  placeholder="AAAA, AAAA-MM o AAAA-MM-DD"
                  required
                />
                <small className="text-muted">Año, año-mes o fecha completa.</small>
              </div>
            </div>
            <FormGroup>
              <Label for="urlDoi">URL / DOI</Label>
              <Input
                id="urlDoi"
                name="urlDoi"
                value={form.urlDoi}
                onChange={handleFormChange}
                placeholder="https://doi.org/10.xxxx/... o URL de la publicación"
              />
            </FormGroup>

            {/* ── Acciones de búsqueda (siempre juntas) ── */}
            <div className="d-flex gap-2 flex-wrap align-items-start mb-1">
              <ButtonGroup size="sm">
                <Button
                  type="button"
                  color="outline-secondary"
                  onClick={searchCrossRef}
                  disabled={metaSearchLoading}
                >
                  {metaSearchLoading ? <Spinner size="sm" className="me-1" /> : null}
                  Buscar metadatos
                </Button>
                <ButtonDropdown
                  isOpen={metaDropdownOpen}
                  toggle={() => setMetaDropdownOpen(o => !o)}
                >
                  <DropdownToggle
                    caret
                    color="outline-secondary"
                    size="sm"
                    disabled={metaSearchLoading}
                  />
                  <DropdownMenu>
                    <DropdownItem header>Fuente de metadatos</DropdownItem>
                    <DropdownItem onClick={searchCrossRef} disabled={metaSearchLoading}>
                      CrossRef
                      <small className="d-block text-muted">Título, autores, fecha, ISSN/ISBN</small>
                    </DropdownItem>
                    <DropdownItem onClick={searchOpenAire} disabled={metaSearchLoading}>
                      OpenAIRE
                      <small className="d-block text-muted">Incluye SciELO, repositorios LA</small>
                    </DropdownItem>
                  </DropdownMenu>
                </ButtonDropdown>
              </ButtonGroup>

              {parseInt(form.publicationType, 10) === 0 && (
                <Button
                  type="button"
                  color="outline-primary"
                  size="sm"
                  onClick={resolveDatabaseNow}
                  disabled={resolveLoading}
                >
                  {resolveLoading ? <Spinner size="sm" className="me-1" /> : null}
                  Resolver base de datos
                </Button>
              )}
            </div>
            <div className="text-muted small mb-1">
              <strong>Buscar metadatos</strong>: rellena título, autores, tipo y fecha desde CrossRef u OpenAIRE.
              {parseInt(form.publicationType, 10) === 0 && (
                <>{' '}<strong>Resolver base de datos</strong>: determina base de datos (Scopus, SciELO, DOAJ…), grupo y cuartil automáticamente.</>
              )}
            </div>
            {crossrefError && <div className="text-danger small mb-2">{crossrefError}</div>}
            {resolveSuccess && <div className="text-success small mb-2">✓ {resolveSuccess}</div>}
            {resolveError && <Alert color="warning" className="small mb-2 py-2">{resolveError}</Alert>}

            <FormGroup>
              <Label for="publicationData">Datos de la publicación</Label>
              <Input
                type="textarea"
                id="publicationData"
                name="publicationData"
                rows={4}
                value={form.publicationData}
                onChange={handleFormChange}
                placeholder="Editorial, revista, datos o cualquier información adicional relevante sobre la publicación"
              />
            </FormGroup>
            <FormGroup>
              <Label>Coautores adicionales</Label>
              <CoauthorPicker
                value={coauthorTags}
                onChange={setCoauthorTags}
                placeholder="Buscar coautor o escribir nombre libre..."
                helpText="Selecciona una tarjeta del resultado o escribe un nombre nuevo y presiona Enter. Si el autor ya está vinculado a un usuario del sistema, verás su tarjeta institucional completa."
              />
            </FormGroup>

            {/* ══ Sección 2: clasificación ═══════════════════════════════ */}
            <hr className="my-3" />
            {proyectosError ? (
              <FormGroup>
                <Label>Proyecto derivado <small className="text-muted">(opcional)</small></Label>
                <Input disabled value="" />
                <small className="text-danger">No se pudieron cargar los proyectos.</small>
              </FormGroup>
            ) : (
              <FormGroup>
                <Label for="proyectoId">Proyecto derivado <small className="text-muted">(opcional)</small></Label>
                <Input
                  type="select"
                  id="proyectoId"
                  name="proyectoId"
                  value={form.proyectoId}
                  onChange={handleFormChange}
                >
                  <option value="">— Sin proyecto asociado —</option>
                  {proyectos.map(p => (
                    <option key={p.id} value={p.id}>{p.titulo}</option>
                  ))}
                </Input>
                {proyectos.length === 0 && (
                  <small className="text-muted">No hay proyectos disponibles.</small>
                )}
              </FormGroup>
            )}

            {parseInt(form.publicationType, 10) === 0 ? (
              <>
                <SingleSelectPicker
                  label={<>Base de datos <span className="text-danger">*</span></>}
                  options={basesdedatos}
                  value={form.dataBase}
                  onChange={nombre => {
                    const inferred = inferGroupFromDatabase(nombre);
                    setForm(f => ({ ...f, dataBase: nombre, group: inferred || f.group }));
                  }}
                  onCreate={createBaseDeDatos}
                />
                <div className="row g-3 mb-3">
                  <div className="col-md-6">
                    <Label for="group">Grupo (1–4) <span className="text-danger">*</span></Label>
                    <Input
                      type="select"
                      id="group"
                      name="group"
                      value={form.group}
                      onChange={handleFormChange}
                    >
                      <option value="">Selecciona grupo...</option>
                      {[1, 2, 3, 4].map(g => (
                        <option key={g} value={g}>Grupo {g}</option>
                      ))}
                    </Input>
                  </div>
                  {parseInt(form.group, 10) === 1 && (
                    <div className="col-md-6">
                      <Label for="cuartil">Cuartil Scimago <span className="text-danger">*</span></Label>
                      <Input
                        type="select"
                        id="cuartil"
                        name="cuartil"
                        value={form.cuartil}
                        onChange={handleFormChange}
                      >
                        <option value="">Selecciona cuartil...</option>
                        {[1, 2, 3, 4].map(q => (
                          <option key={q} value={`Q${q}`}>Q{q}</option>
                        ))}
                      </Input>
                    </div>
                  )}
                </div>
              </>
            ) : [1, 2, 3].includes(parseInt(form.publicationType, 10)) ? (
              <FormGroup>
                <Label for="index">Indexación <span className="text-danger">*</span></Label>
                <Input
                  type="select"
                  id="index"
                  name="index"
                  value={form.index}
                  onChange={handleFormChange}
                >
                  <option value="">— Selecciona el grupo —</option>
                  <option value="1">Grupo 1 — BkCI (Web de la Ciencia)</option>
                  <option value="2">Grupo 2 — Editoriales de prestigio internacional (SciELO Libros, SPI, PSM)</option>
                  <option value="3">Grupo 3 — Editoriales nacionales u otras</option>
                </Input>
              </FormGroup>
            ) : null}

            <hr className="my-3" />
            <FormGroup>
              <Label>Certificado / Evidencia</Label>
              <CertificateUpload
                fileId={form.evidenceFileId}
                onFileIdChange={id => setForm(f => ({ ...f, evidenceFileId: id }))}
                canManage
                canView
                disabled={formLoading}
              />
            </FormGroup>
          </Form>
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" outline onClick={() => setModal(false)} disabled={formLoading}>
            Cancelar
          </Button>
          <Button color="primary" onClick={handleSubmit} disabled={formLoading}>
            {formLoading
              ? <><Spinner size="sm" className="me-1" /> Guardando…</>
              : editing ? 'Guardar cambios' : 'Crear publicación'
            }
          </Button>
        </ModalFooter>
      </Modal>

      {/* ── Modal confirmar borrado ── */}
      <Modal isOpen={deleteModal} toggle={() => setDeleteModal(false)} size="sm">
        <ModalHeader toggle={() => setDeleteModal(false)}>Eliminar publicación</ModalHeader>
        <ModalBody>
          {deleteError && <Alert color="danger">{deleteError}</Alert>}
          <p>
            ¿Seguro que quieres eliminar <strong>"{toDelete?.title}"</strong>?
            Esta acción no se puede deshacer.
          </p>
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" outline onClick={() => setDeleteModal(false)} disabled={deleteLoading}>
            Cancelar
          </Button>
          <Button color="danger" onClick={handleDelete} disabled={deleteLoading}>
            {deleteLoading
              ? <><Spinner size="sm" className="me-1" /> Eliminando…</>
              : 'Eliminar'
            }
          </Button>
        </ModalFooter>
      </Modal>

      {/* ── Modal candidatos duplicados ── */}
      <Modal isOpen={duplicatesModal} toggle={() => setDuplicatesModal(false)} size="lg">
        <ModalHeader toggle={() => setDuplicatesModal(false)}>Publicaciones similares encontradas</ModalHeader>
        <ModalBody>
          {duplicatesError && <Alert color="danger">{duplicatesError}</Alert>}
          {duplicatesLoading && <div className="text-center py-3"><Spinner /></div>}
          {!duplicatesLoading && duplicatesCandidates.length === 0 && (
            <p className="text-muted">No se encontraron coincidencias.</p>
          )}
          {!duplicatesLoading && duplicatesCandidates.length > 0 && (
            <Table hover responsive size="sm">
              <thead>
                <tr>
                  <th>Título</th>
                  <th>URL / DOI</th>
                  <th>Coincidencia</th>
                  <th style={{ width: 220 }}></th>
                </tr>
              </thead>
              <tbody>
                {duplicatesCandidates.map(c => (
                  <tr key={c.id}>
                    <td style={{ maxWidth: 420 }}>{c.title}</td>
                    <td style={{ maxWidth: 220 }}>{c.urlDoi ?? '—'}</td>
                    <td>{renderMatchLabel(c.matchType, c.score)}</td>
                    <td className="text-end" style={{ whiteSpace: 'nowrap' }}>
                      <Button type="button" color="primary" size="sm" className="me-2"
                        onClick={async () => {
                          try {
                            await apiFetch(`/api/Publications/${c.id}/coauthors`, { method: 'POST' });
                            setDuplicatesModal(false);
                            setModal(false);
                            loadData();
                          } catch (e) {
                            setDuplicatesError(e.message);
                          }
                        }}>
                        Sí — añadirme como coautor
                      </Button>
                      <Button type="button" color="outline-secondary" size="sm" onClick={() => viewDetails(c.id)}>Ver</Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </Table>
          )}
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" outline onClick={() => { setDuplicatesModal(false); }} disabled={duplicatesLoading}>
            Volver
          </Button>
          <Button type="button" color="success" onClick={async () => { try { if (editing) await performUpdate(); else await performCreate(); setDuplicatesModal(false); } catch (e) { setDuplicatesError(e.message); } }} disabled={duplicatesLoading}>
            {editing ? 'Actualizar publicación' : 'Crear nueva publicación'}
          </Button>
        </ModalFooter>
      </Modal>

      {/* ── Modal resultados CrossRef ── */}
      <Modal isOpen={crossrefModal} toggle={() => setCrossrefModal(false)} size="lg">
        <ModalHeader toggle={() => setCrossrefModal(false)}>Resultados CrossRef</ModalHeader>
        <ModalBody>
          {crossrefError && <Alert color="danger">{crossrefError}</Alert>}
          {crossrefLoading && <div className="text-center py-3"><Spinner /></div>}
          {!crossrefLoading && crossrefCandidates.length === 0 && (
            <p className="text-muted">No se encontraron resultados en CrossRef.</p>
          )}
          {!crossrefLoading && crossrefCandidates.length > 0 && (
            <Table hover responsive size="sm">
              <thead>
                <tr>
                  <th>Título</th>
                  <th>DOI / URL</th>
                  <th>Fuente</th>
                  <th style={{ width: 220 }}></th>
                </tr>
              </thead>
              <tbody>
                {crossrefCandidates.map((c, idx) => (
                  <tr key={c.doi ?? c.url ?? idx}>
                    <td style={{ maxWidth: 420 }}>{c.title}</td>
                    <td style={{ maxWidth: 220 }}>{c.doi ? `https://doi.org/${c.doi}` : (c.url ?? '—')}</td>
                    <td>{c.containerTitle ?? '—'}</td>
                    <td className="text-end">
                      <Button type="button" color="primary" size="sm" className="me-2" onClick={() => importFromCrossRef(c)} disabled={authorResolutionLoading}>
                        {authorResolutionLoading ? <><Spinner size="sm" className="me-1" />Resolviendo…</> : 'Rellenar formulario'}
                      </Button>
                      <Button type="button" color="outline-secondary" size="sm" onClick={() => previewCrossRefCandidate(c)}>
                        Previsualizar
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </Table>
          )}
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" outline onClick={() => setCrossrefModal(false)}>Cerrar</Button>
        </ModalFooter>
      </Modal>

      {/* ── Modal ver detalles de publicación ── */}
      <Modal isOpen={detailsModal} toggle={closeDetailsModal} size="lg">
        <ModalHeader toggle={closeDetailsModal}>Detalles de la publicación</ModalHeader>
        <ModalBody>
          {detailsError && <Alert color="danger">{detailsError}</Alert>}
          {detailsLoading && <div className="text-center py-3"><Spinner /></div>}
          {detailsPublication && renderPublicationDetails(detailsPublication)}
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={closeDetailsModal}>Cerrar</Button>
        </ModalFooter>
      </Modal>

      {/* ── Modal revisión de autores externos (CrossRef / OpenAIRE) ── */}
      {authorResolutionModal && (
        <AuthorResolutionModal
          isOpen={authorResolutionModal}
          items={authorResolutionItems}
          currentUserId={user?.id}
          currentUserHasLinkedAuthor={user?.hasLinkedAuthor ?? false}
          onConfirm={(resolvedTags) => {
            setAuthorResolutionModal(false);
            // Detect if the current user appeared among the resolved authors.
            // If the user has a linked Author but was not identified, flag it so
            // handleSubmit can request explicit confirmation before saving.
            const userIdentified = resolvedTags.some(
              t => t.type === 'author' && t.linkedUser?.id === user?.id
            );
            setAuthorNotInCrossRef(!userIdentified && (user?.hasLinkedAuthor ?? false));
            setCoauthorTags(prev => {
              const existing = new Set(
                prev.map(t => t.id ? `id:${t.id}` : `name:${t.name.trim().toLowerCase()}`)
              );
              const toAdd = resolvedTags.filter(t => {
                const key = t.id ? `id:${t.id}` : `name:${t.name.trim().toLowerCase()}`;
                return !existing.has(key);
              });
              return [...prev, ...toAdd];
            });
          }}
          onCancel={() => setAuthorResolutionModal(false)}
        />
      )}

      {/* ── Confirmación: usuario no aparece como autor en CrossRef ── */}
      <Modal isOpen={authorWarningConfirmOpen} toggle={() => setAuthorWarningConfirmOpen(false)} size="sm" centered>
        <ModalHeader toggle={() => setAuthorWarningConfirmOpen(false)}>
          Confirmar registro de autoría
        </ModalHeader>
        <ModalBody style={{ fontSize: '0.92rem' }}>
          <p>
            Su nombre <strong>no figura oficialmente como autor</strong> de esta publicación
            según los metadatos recuperados. Si continúa, usted quedará registrado como autor
            en el sistema de todas formas. Esto puede generar inconsistencias en cuanto a la autoría de esta publicación
            y ocasionar problemas para con otros usuarios y servicios dentro del sistema. Por favor, 
            verifique cuidadosamente antes de continuar. Si aun así desea seguir, tenga en cuenta 
            que esta autoría quedará registrada únicamente en este sistema y no se reflejará en los metadatos 
            oficiales de la publicación ni en servicios externos como ORCID.
          </p>
          <p className="mb-0">¿Deseas continuar?</p>
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" outline onClick={() => setAuthorWarningConfirmOpen(false)}>
            Cancelar
          </Button>
          <Button
            color="primary"
            onClick={() => {
              setAuthorWarningConfirmOpen(false);
              setAuthorNotInCrossRef(false); // bypass the gate on re-entry
              handleSubmit();
            }}
          >
            Continuar de todas formas
          </Button>
        </ModalFooter>
      </Modal>
    </>
  );
}
