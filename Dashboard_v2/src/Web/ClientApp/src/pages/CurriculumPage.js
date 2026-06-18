import React, { useState, useEffect } from 'react';
import { Button, Spinner, Alert } from 'reactstrap';
import { useAuth } from '../contexts/AuthContext';

// ─── helpers ──────────────────────────────────────────────────────────────────

async function silentFetch(url) {
  try {
    const r = await fetch(url, { credentials: 'include' });
    if (!r.ok) return null;
    return await r.json();
  } catch { return null; }
}

/**
 * Formatea una fecha parcial almacenada como string.
 * Acepta: "2024", "2024-01", "2024-01-15" o ISO completo.
 */
function formatDate(str) {
  if (!str) return null;
  const parts = String(str).split('T')[0].split('-');
  if (parts.length === 1) return parts[0];
  if (parts.length === 2) {
    const [y, m] = parts;
    const month = new Date(Number(y), Number(m) - 1)
      .toLocaleDateString('es-ES', { month: 'long' });
    return `${month} de ${y}`;
  }
  return new Date(str).toLocaleDateString('es-ES', {
    year: 'numeric', month: 'long', day: 'numeric',
  });
}

/** True si str es una fecha real (año > 1, es decir, no es DateTime.MinValue). */
function isValidDate(str) {
  if (!str) return false;
  const d = new Date(str);
  return !isNaN(d.getTime()) && d.getFullYear() > 1;
}

/**
 * Devuelve el identificador DOI limpio, sin el prefijo https://doi.org/
 * Si no es una URL de doi.org, devuelve la URL tal cual.
 */
function formatDoiText(url) {
  if (!url) return url;
  return url.replace(/^https?:\/\/(dx\.)?doi\.org\//i, '');
}

// ─── Building block ───────────────────────────────────────────────────────────

function CvSection({ title, icon, children, empty, emptyMsg = 'Sin registros.' }) {
  return (
    <section className="cv-section">
      <h3 className="cv-section__title">
        <i className={`bi ${icon}`} />
        {title}
      </h3>
      <div className="cv-section__body">
        {empty ? <p className="cv-empty-msg">{emptyMsg}</p> : children}
      </div>
    </section>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function CurriculumPage() {
  const { user } = useAuth();
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState('');
  const [cv, setCv]           = useState(null);

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError('');
      try {
        const [pubs, pres, events, grupos, rawAwards, lineas, areas, proyectos] = await Promise.all([
          silentFetch('/api/Publications'),
          silentFetch('/api/Presentations'),
          silentFetch('/api/Events'),
          silentFetch('/api/GruposDeInvestigacion/mine'),
          silentFetch('/api/Awards'),
          silentFetch('/api/LineasDeInvestigacion'),
          silentFetch('/api/Areas'),
          silentFetch('/api/Proyectos/participacion'),
        ]);

        // /api/Awards returns AwardWithGrantingsDto[]
        // Flatten to one row per (award + granting date) for the CV
        const awards = (rawAwards ?? []).flatMap(a =>
          a.grantings?.length > 0
            ? a.grantings.map(g => ({
                awardName:    a.awardName,
                awardTypeName: a.awardTypeName,
                awardedAt:    g.awardedAt,
              }))
            : [{ awardName: a.awardName, awardTypeName: a.awardTypeName, awardedAt: null }]
        );

        // Find the user's area from the full list
        const areaInfo = user?.areaId
          ? (areas ?? []).find(ar => String(ar.id) === String(user.areaId)) ?? null
          : null;

        setCv({
          pubs:      pubs      ?? [],
          pres:      pres      ?? [],
          events:    events    ?? [],
          grupos:    grupos    ?? [],
          awards,
          lineas:    lineas    ?? [],
          areaInfo,
          proyectos: proyectos ?? [],
        });
      } catch (e) {
        setError(e.message);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, []);

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center py-5">
        <Spinner color="primary" />
      </div>
    );
  }
  if (error) return <Alert color="danger">{error}</Alert>;

  const { pubs, pres, events, grupos, awards, lineas, areaInfo, proyectos } = cv;

  // Líneas index for name lookup  (API returns objects with id + nombre)
  const lineasById = Object.fromEntries(
    lineas.map(l => [String(l.id), l.nombre ?? l.name ?? String(l.id)])
  );

  // Group presentations by eventId
  const presByEvent = {};
  pres.forEach(p => {
    const key = String(p.eventId);
    if (!presByEvent[key]) presByEvent[key] = [];
    presByEvent[key].push(p);
  });

  // Split publications by specialization
  const revistas  = pubs.filter(p => p.journalPublication  != null);
  const indexadas = pubs.filter(p => p.indexedPublication  != null);
  const otras     = pubs.filter(p => p.journalPublication  == null && p.indexedPublication == null);

  // Sort awards by date descending (use awardedAt; skip DateTime.MinValue)
  const sortedAwards = [...awards].sort((a, b) => {
    const ta = isValidDate(a.awardedAt) ? new Date(a.awardedAt).getTime() : 0;
    const tb = isValidDate(b.awardedAt) ? new Date(b.awardedAt).getTime() : 0;
    return tb - ta;
  });

  const today = new Date().toLocaleDateString('es-ES', {
    year: 'numeric', month: 'long', day: 'numeric',
  });

  return (
    <div className="curriculum-wrapper">

      {/* ── Toolbar (hidden when printing) ─────────────────────────── */}
      <div className="curriculum-toolbar print-hide">
        <h4 className="curriculum-toolbar__title">
          <i className="bi bi-file-person me-2" />
          Currículum Científico
        </h4>
        <Button color="primary" onClick={() => {
          const prev = document.title;
          document.title = `Currículum — ${user?.userName ?? 'usuario'}`;
          window.print();
          document.title = prev;
        }}>
          <i className="bi bi-download me-2" />
          Exportar PDF
        </Button>
      </div>

      {/* ── CV Document ─────────────────────────────────────────────── */}
      <div className="cv-doc">

        {/* Header */}
        <header className="cv-doc__header">
          <div className="cv-doc__avatar">
            <i className="bi bi-person-circle" />
          </div>
          <div className="cv-doc__meta">
            <h1 className="cv-doc__name">{user?.userName ?? '—'}</h1>
            <p className="cv-doc__line">
              <i className="bi bi-envelope me-1" />
              {user?.email ?? '—'}
            </p>
            {user?.areaNombre && (
              <p className="cv-doc__line">
                <i className="bi bi-building me-1" />
                {user.areaNombre}
              </p>
            )}
            {areaInfo?.universidadNombre && (
              <p className="cv-doc__line">
                <i className="bi bi-bank me-1" />
                {areaInfo.universidadNombre}
              </p>
            )}
            <p className="cv-doc__generated">Generado el {today}</p>
          </div>
        </header>

        {/* ── Artículos en Revistas ──────────────────────────────── */}
        <CvSection
          title="Artículos en Revistas Científicas"
          icon="bi-journal-text"
          empty={revistas.length === 0}
          emptyMsg="No se han registrado artículos en revistas."
        >
          <ol className="cv-list">
            {revistas.map((p, i) => (
              <li key={p.id ?? i} className="cv-list__item">
                <span className="cv-item__title">{p.title}</span>
                {p.publicationData && (
                  <span className="cv-item__venue">
                    . <em>{p.publicationData}</em>
                  </span>
                )}
                {p.journalPublication?.dataBase && (
                  <span className="cv-item__meta">
                    {' '}[{p.journalPublication.dataBase}]
                  </span>
                )}
                {p.journalPublication?.group != null && (
                  <span className="cv-item__meta">
                    {' '}Grupo {p.journalPublication.group}
                  </span>
                )}
                {p.journalPublication?.cuartil && (
                  <span className="cv-item__badge">
                    {' '}{p.journalPublication.cuartil}
                  </span>
                )}
                {p.publishedDate && (
                  <span className="cv-item__date">
                    {' '}({formatDate(p.publishedDate)})
                  </span>
                )}
                {p.urlDoi && (
                  <>
                    {' '}·{' '}
                    <a
                      className="cv-item__doi"
                      href={p.urlDoi}
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      {formatDoiText(p.urlDoi)}
                    </a>
                  </>
                )}
                {p.authors?.length > 0 && (
                  <div className="cv-item__authors">
                    {p.authors.map(a => a.name).join(', ')}
                  </div>
                )}
              </li>
            ))}
          </ol>
        </CvSection>

        {/* ── Publicaciones Indexadas ──────────────────────────────── */}
        <CvSection
          title="Publicaciones Indexadas"
          icon="bi-file-earmark-check"
          empty={indexadas.length === 0}
          emptyMsg="No se han registrado publicaciones indexadas."
        >
          <ol className="cv-list">
            {indexadas.map((p, i) => (
              <li key={p.id ?? i} className="cv-list__item">
                <span className="cv-item__title">{p.title}</span>
                {p.publicationData && (
                  <span className="cv-item__venue">
                    . <em>{p.publicationData}</em>
                  </span>
                )}
                {p.indexedPublication?.index && (
                  <span className="cv-item__meta">
                    {' '}[{p.indexedPublication.index}]
                  </span>
                )}
                {p.publishedDate && (
                  <span className="cv-item__date">
                    {' '}({formatDate(p.publishedDate)})
                  </span>
                )}
                {p.urlDoi && (
                  <>
                    {' '}·{' '}
                    <a
                      className="cv-item__doi"
                      href={p.urlDoi}
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      {formatDoiText(p.urlDoi)}
                    </a>
                  </>
                )}
                {p.authors?.length > 0 && (
                  <div className="cv-item__authors">
                    {p.authors.map(a => a.name).join(', ')}
                  </div>
                )}
              </li>
            ))}
          </ol>
        </CvSection>

        {/* ── Otras publicaciones (sin especialización) ───────────── */}
        {otras.length > 0 && (
          <CvSection title="Otras Publicaciones" icon="bi-file-earmark-text">
            <ol className="cv-list">
              {otras.map((p, i) => (
                <li key={p.id ?? i} className="cv-list__item">
                  <span className="cv-item__title">{p.title}</span>
                  {p.publicationData && (
                    <span className="cv-item__venue">
                      . <em>{p.publicationData}</em>
                    </span>
                  )}
                  {p.publishedDate && (
                    <span className="cv-item__date">
                      {' '}({formatDate(p.publishedDate)})
                    </span>
                  )}
                  {p.urlDoi && (
                    <>
                      {' '}·{' '}
                      <a
                        className="cv-item__doi"
                        href={p.urlDoi}
                        target="_blank"
                        rel="noopener noreferrer"
                      >
                        {formatDoiText(p.urlDoi)}
                      </a>
                    </>
                  )}
                </li>
              ))}
            </ol>
          </CvSection>
        )}

        {/* ── Eventos y Ponencias ──────────────────────────────────── */}
        <CvSection
          title="Eventos y Ponencias"
          icon="bi-mic"
          empty={events.length === 0 && pres.length === 0}
          emptyMsg="No se han registrado eventos ni ponencias."
        >
          <div className="cv-events">
            {events.map((ev, i) => {
              const evPres = presByEvent[String(ev.id)] ?? [];
              return (
                <div key={ev.id ?? i} className="cv-event">
                  <div className="cv-event__name">
                    {ev.name}
                    {(ev.eventTypeName || ev.countryName) && (
                      <span className="cv-event__meta">
                        {' '}—{' '}
                        {[ev.eventTypeName, ev.countryName]
                          .filter(Boolean)
                          .join(', ')}
                      </span>
                    )}
                  </div>
                  {evPres.length > 0 ? (
                    <ul className="cv-event__pres">
                      {evPres.map((pr, j) => (
                        <li key={pr.id ?? j}>
                          <span className="cv-item__title">{pr.name}</span>
                          {pr.authors?.length > 0 && (
                            <span className="cv-item__authors-inline">
                              {' '}— {pr.authors.join(', ')}
                            </span>
                          )}
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <p className="cv-event__no-pres">
                      Sin ponencias registradas en este evento.
                    </p>
                  )}
                </div>
              );
            })}

            {/* Ponencias sin evento correspondiente en la lista */}
            {pres
              .filter(p => !events.some(e => e.id === p.eventId))
              .map((pr, i) => (
                <div key={pr.id ?? i} className="cv-event">
                  <div className="cv-event__name">
                    {pr.eventName ?? 'Evento sin detalle'}
                  </div>
                  <ul className="cv-event__pres">
                    <li>
                      <span className="cv-item__title">{pr.name}</span>
                      {pr.authors?.length > 0 && (
                        <span className="cv-item__authors-inline">
                          {' '}— {pr.authors.join(', ')}
                        </span>
                      )}
                    </li>
                  </ul>
                </div>
              ))}
          </div>
        </CvSection>

        {/* ── Grupos de Investigación ──────────────────────────────── */}
        <CvSection
          title="Grupos de Investigación"
          icon="bi-diagram-3"
          empty={grupos.length === 0}
          emptyMsg="No perteneces a ningún grupo de investigación."
        >
          <ul className="cv-list">
            {grupos.map((g, i) => {
              const lineasNombres = (g.lineasDeInvestigacionIds ?? [])
                .map(id => lineasById[String(id)])
                .filter(Boolean);
              return (
                <li key={g.id ?? i} className="cv-list__item">
                  <span className="cv-item__title">{g.nombre}</span>
                  {g.areaNombre && (
                    <span className="cv-item__meta"> — {g.areaNombre}</span>
                  )}
                  {lineasNombres.length > 0 && (
                    <div className="cv-item__sub">
                      Líneas de investigación: {lineasNombres.join(' · ')}
                    </div>
                  )}
                </li>
              );
            })}
          </ul>
        </CvSection>

        {/* ── Proyectos de Investigación ───────────────────────────── */}
        <CvSection
          title="Proyectos de Investigación"
          icon="bi-kanban"
          empty={proyectos.length === 0}
          emptyMsg="No se han registrado participaciones en proyectos."
        >
          <ul className="cv-list">
            {proyectos.map((p, i) => (
              <li key={p.id ?? i} className="cv-list__item">
                <span className="cv-item__title">{p.titulo}</span>
                {p.clasificacionNombre && (
                  <span className="cv-item__meta"> — {p.clasificacionNombre}</span>
                )}
                {p.codigoProyecto && (
                  <span className="cv-item__meta"> ({p.codigoProyecto})</span>
                )}
                {p.jefe && (
                  <div className="cv-item__sub">Jefe: {p.jefe}</div>
                )}
              </li>
            ))}
          </ul>
        </CvSection>

        {/* ── Premios y Reconocimientos ────────────────────────────── */}
        <CvSection
          title="Premios y Reconocimientos"
          icon="bi-trophy"
          empty={sortedAwards.length === 0}
          emptyMsg="No se han registrado premios."
        >
          <ul className="cv-list">
            {sortedAwards.map((a, i) => (
              <li key={a.id ?? i} className="cv-list__item">
                <span className="cv-item__title">{a.awardName}</span>
                <span className="cv-item__meta"> — {a.awardTypeName}</span>
                {isValidDate(a.awardedAt) && (
                  <span className="cv-item__date"> — {formatDate(a.awardedAt)}</span>
                )}
              </li>
            ))}
          </ul>
        </CvSection>

      </div>
    </div>
  );
}
