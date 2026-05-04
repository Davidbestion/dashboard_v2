import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Nav, NavItem, NavLink,
  TabContent, TabPane,
  Badge, Button,
  Spinner, Alert,
} from 'reactstrap';
import { useAuth } from '../contexts/AuthContext';
import DataTable from '../components/DataTable';
import FilterableDataTable from '../components/FilterableDataTable';

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
  const { user } = useAuth();
  const [publicaciones, setPublicaciones] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [generatingAnexo, setGeneratingAnexo] = useState(false);
  const [activeType, setActiveType] = useState(0);
  const [activeGroup, setActiveGroup] = useState(1);

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
      authors: publicacion.authors ?? [],
      urlDoi: publicacion.urlDoi ?? null,
      proyectoTitulo: publicacion.proyectoTitulo ?? null,
      indexedPublication: publicacion.indexedPublication ?? null,
      journalPublication: publicacion.journalPublication ?? null,
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
   * Solo debe estar disponible visualmente para el superuser.
   */
  async function handleGenerarAnexo() {
    setGeneratingAnexo(true);
    setError('');
    try {
      const response = await fetch('/api/Documents/anexo-publicaciones', { credentials: 'include' });
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
          <Button color="outline-success" onClick={handleGenerarAnexo} disabled={generatingAnexo}>
            {generatingAnexo ? <Spinner size="sm" /> : '⬇ Generar Anexo 2'}
          </Button>
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
                            { key: 'title',                       label: 'Título',       sortable: true },
                            { key: 'publicationData',             label: 'Datos de pub.' },
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
                          emptyMessage={`No hay publicaciones en el Grupo ${group}.`}
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
                        { key: 'title',          label: 'Título',       sortable: true },
                        { key: 'publicationData', label: 'Datos de pub.' },
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
                      emptyMessage="No hay publicaciones de este tipo."
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
