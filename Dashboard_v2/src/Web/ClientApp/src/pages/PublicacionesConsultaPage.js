import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Nav, NavItem, NavLink,
  TabContent, TabPane,
  Badge, Button,
  Spinner, Alert,
  Input, Label,
  UncontrolledPopover, PopoverHeader, PopoverBody,
  Dropdown, DropdownToggle, DropdownMenu,
} from 'reactstrap';
import { useAuth } from '../contexts/AuthContext';
import FilterableDataTable from '../components/FilterableDataTable';
import { CertificateViewButton } from '../components/CertificateUpload';

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

export default function PublicacionesConsultaPage({ apiUrl = '/api/Publications/todas' }) {
  const { user } = useAuth();
  const [publicaciones, setPublicaciones] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [generatingAnexo, setGeneratingAnexo] = useState(false);
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [activeType, setActiveType] = useState(0);
  const [activeGroup, setActiveGroup] = useState(1);
  const [anexoDropOpen, setAnexoDropOpen] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await apiFetch(apiUrl);
      setPublicaciones(data);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, [apiUrl]);

  useEffect(() => { load(); }, [load]);

  /**
   * Normaliza la forma de una publicación para tolerar tanto el DTO detallado nuevo
   * como el DTO resumido anterior mientras el backend local se recompila o reinicia.
   */
  function normalizePublication(publicacion) {
    return {
      id: publicacion.id,
      title: publicacion.title ?? publicacion.titulo ?? '',
      publicationType: publicacion.publicationType ?? publicacion.tipo,
      publicationData: publicacion.publicationData ?? '',
      publishedDate: publicacion.publishedDate ?? '',
      authors: publicacion.authors ?? [],
      urlDoi: publicacion.urlDoi ?? null,
      proyectoTitulo: publicacion.proyectoTitulo ?? null,
      indexedPublication: publicacion.indexedPublication ?? null,
      journalPublication: publicacion.journalPublication ?? null,
      evidenceFileId: publicacion.evidenceFileId ?? null,
    };
  }

  const publicationsNormalized = publicaciones.map(normalizePublication);
  const pubsByType = (typeVal) => publicationsNormalized.filter(p => p.publicationType === typeVal);
  const journalByGroup = (group) => publicationsNormalized
    .filter(p => p.publicationType === 0 && p.journalPublication?.group === group);

  /**
   * Renderiza la lista de autores tal como se muestra en la vista del profesor,
   * pero sin acciones de edición porque esta pantalla es solo de consulta global.
   */
  function authorsList(authors) {
    if (!authors?.length) {
      return <span className="text-muted">—</span>;
    }

    return authors.map((author, index) => (
      <span key={author.id}>
        {index > 0 && <span className="text-muted me-1">,</span>}
        {author.name}
        {author.userId && (
          <i className="bi bi-person-check ms-1 text-success" title="Usuario registrado"></i>
        )}
      </span>
    ));
  }

  /**
   * Renderiza la URL o DOI de la publicación con truncado visual.
   */
  function urlCell(urlDoi) {
    return urlDoi
      ? (
        <a
          href={urlDoi.startsWith('http') ? urlDoi : `https://doi.org/${urlDoi}`}
          target="_blank"
          rel="noopener noreferrer"
          className="text-truncate d-block"
          style={{ maxWidth: 170 }}
          title={urlDoi}
        >
          {urlDoi}
        </a>
      )
      : <span className="text-muted">—</span>;
  }

  /**
   * Dispara la generación y descarga del anexo de publicaciones.
   * Acepta opcionalmente fechas límite (from/to) que se pasan como query params.
   */
  async function handleGenerarAnexo({ from = '', to = '' } = {}) {
    setGeneratingAnexo(true);
    setError('');
    try {
      const params = new URLSearchParams();
      if (from) params.set('from', from);
      if (to)   params.set('to', to);
      const qs = params.toString() ? `?${params.toString()}` : '';
      const response = await fetch(`/api/Documents/anexo-publicaciones${qs}`, { credentials: 'include' });
      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.error ?? 'Error al generar el Anexo 2.');
      }

      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      const now = new Date();
      a.download = `Anexo_2_Publicaciones_${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}.xlsx`;
      a.href = url;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e) {
      setError(e.message);
    } finally {
      setGeneratingAnexo(false);
    }
  }

  return (
    <Card className="shadow-sm">
      <CardHeader className="d-flex justify-content-between align-items-center gap-2">
        <div>
          <strong>Publicaciones</strong>
          <small className="text-muted ms-2">({publicationsNormalized.length})</small>
        </div>
        {user?.role === 'Superuser' && (
          <Button color="outline-success" onClick={() => handleGenerarAnexo()} disabled={generatingAnexo}>
            {generatingAnexo ? <Spinner size="sm" /> : '⬇ Generar Anexo 2'}
          </Button>
        )}
        {user?.role === 'Vicedecano_de_investigacion' && (
          <Dropdown isOpen={anexoDropOpen} toggle={() => setAnexoDropOpen(o => !o)}>
            <DropdownToggle color="outline-success" size="sm" caret disabled={generatingAnexo}>
              {generatingAnexo ? <Spinner size="sm" /> : '⬇ Generar Anexo 2'}
            </DropdownToggle>
            <DropdownMenu end style={{ minWidth: 280, padding: '0.75rem' }}>
              <p className="text-muted mb-2" style={{ fontSize: '0.82em' }}>
                <i className="bi bi-info-circle me-1" />
                Filtra las publicaciones incluidas en el anexo por rango de meses. Deja en blanco para incluir todas.
              </p>
              <div className="mb-2">
                <Label className="mb-1 fw-semibold" style={{ fontSize: '0.85em' }}>Desde (mes)</Label>
                <Input
                  type="month"
                  bsSize="sm"
                  value={dateFrom}
                  onChange={e => setDateFrom(e.target.value)}
                />
              </div>
              <div className="mb-3">
                <Label className="mb-1 fw-semibold" style={{ fontSize: '0.85em' }}>Hasta (mes)</Label>
                <Input
                  type="month"
                  bsSize="sm"
                  value={dateTo}
                  onChange={e => setDateTo(e.target.value)}
                />
              </div>
              <Button
                color="success"
                size="sm"
                className="w-100"
                onClick={() => { setAnexoDropOpen(false); handleGenerarAnexo({ from: dateFrom, to: dateTo }); }}
                disabled={generatingAnexo}
              >
                ⬇ Descargar Anexo 2
              </Button>
            </DropdownMenu>
          </Dropdown>
        )}
      </CardHeader>
      <CardBody>
        {loading && <div className="text-center py-4"><Spinner /></div>}
        {error && <Alert color="danger">{error}</Alert>}

        {!loading && !error && (
          <>
            <Nav tabs>
              {PUB_TIPOS.map((name, typeValue) => (
                <NavItem key={typeValue}>
                  <NavLink
                    href="#"
                    active={activeType === typeValue}
                    onClick={e => { e.preventDefault(); setActiveType(typeValue); }}
                  >
                    {name}{' '}
                    <Badge color="secondary" pill style={{ fontSize: '0.7em' }}>
                      {pubsByType(typeValue).length}
                    </Badge>
                  </NavLink>
                </NavItem>
              ))}
            </Nav>

            <TabContent activeTab={String(activeType)} className="border border-top-0 rounded-bottom">
              <TabPane tabId="0" className="p-3">
                <Nav tabs className="mb-3">
                  {[1, 2, 3, 4].map(group => (
                    <NavItem key={group}>
                      <NavLink
                        href="#"
                        active={activeGroup === group}
                        onClick={e => { e.preventDefault(); setActiveGroup(group); }}
                      >
                        Grupo {group}{' '}
                        <Badge color="secondary" pill style={{ fontSize: '0.7em' }}>
                          {journalByGroup(group).length}
                        </Badge>
                      </NavLink>
                    </NavItem>
                  ))}
                </Nav>

                <TabContent activeTab={String(activeGroup)}>
                  {[1, 2, 3, 4].map(group => {
                    const pubs = journalByGroup(group);
                    return (
                      <TabPane tabId={String(group)} key={group}>
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
                            ...(group === 1 ? [{
                              key: 'journalPublication.cuartil',
                              label: 'Cuartil',
                              render: v => v != null
                                ? <Badge color="info" pill className="text-dark">{v}</Badge>
                                : <span className="text-muted">—</span>,
                            }] : []),
                            { key: 'urlDoi',        label: 'URL / DOI',          render: v => urlCell(v) },
                            { key: 'authors',       label: 'Autores',            render: v => authorsList(v ?? []) },
                            { key: 'proyectoTitulo', label: 'Proyecto vinculado', render: v => v ?? <span className="text-muted">—</span> },
                          ]}
                          data={pubs}
                          keyExtractor={pub => pub.id}
                          actions={[
                            { key: 'certificate', label: 'Certificado', show: pub => pub.evidenceFileId != null,
                              render: pub => <CertificateViewButton fileId={pub.evidenceFileId} /> },
                          ]}
                          emptyMessage={`No hay publicaciones en el Grupo ${group}.`}
                          detailConfig
                        />
                      </TabPane>
                    );
                  })}
                </TabContent>
              </TabPane>

              {[1, 2, 3, 4].map(typeVal => {
                const pubs = pubsByType(typeVal);
                return (
                  <TabPane tabId={String(typeVal)} key={typeVal} className="p-3">
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
                        {
                          key: 'indexedPublication.index',
                          label: 'Indexación',
                          render: v => v ? `Grupo ${v}` : <span className="text-muted">—</span>,
                        },
                        { key: 'urlDoi',         label: 'URL / DOI',          render: v => urlCell(v) },
                        { key: 'authors',        label: 'Autores',            render: v => authorsList(v ?? []) },
                        { key: 'proyectoTitulo', label: 'Proyecto vinculado', render: v => v ?? <span className="text-muted">—</span> },
                      ]}
                      data={pubs}
                      keyExtractor={pub => pub.id}
                      actions={[
                        { key: 'certificate', label: 'Certificado', show: pub => pub.evidenceFileId != null,
                          render: pub => <CertificateViewButton fileId={pub.evidenceFileId} /> },
                      ]}
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
  );
}
