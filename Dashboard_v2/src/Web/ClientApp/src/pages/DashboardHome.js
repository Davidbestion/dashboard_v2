import React, { useState, useEffect } from 'react';
import { Spinner } from 'reactstrap';
import { useAuth } from '../contexts/AuthContext';

// ─── helpers ──────────────────────────────────────────────────────────────────

const ROLE_LABELS = {
  'Profesor':                       'Profesor / Investigador',
  'Vicedecano_de_investigacion':    'Vicedecano de Investigación',
  'Jefe_de_Proyecto':               'Jefe de Proyecto',
  'Jefe_de_Grupo_de_investigacion': 'Jefe de Grupo de Investigación',
  'Jefe_de_Macroproyecto':          'Jefe de Macroproyecto',
  'Jefe_de_Redes':                  'Jefe de Redes',
  'Superuser':                      'Administrador del Sistema',
};

async function silentFetch(url) {
  try {
    const r = await fetch(url, { credentials: 'include' });
    if (!r.ok) return null;
    return await r.json();
  } catch { return null; }
}

// ─── Shared building blocks ───────────────────────────────────────────────────

function StatCard({ icon, label, value, color = 'stat-blue', link, sublabel }) {
  const inner = (
    <div className={`stat-card ${color}`}>
      <div className="stat-card__icon"><i className={`bi ${icon}`} /></div>
      <div className="stat-card__body">
        <span className="stat-card__value">{value != null ? value : '—'}</span>
        <span className="stat-card__label">{label}</span>
        {sublabel && <span className="stat-card__sublabel">{sublabel}</span>}
      </div>
    </div>
  );
  if (link) return <a href={link} style={{ textDecoration: 'none', display: 'block' }}>{inner}</a>;
  return inner;
}

function LoadingHome() {
  return (
    <div className="d-flex justify-content-center align-items-center py-5">
      <Spinner color="primary" />
    </div>
  );
}

// ─── Profesor / Vicedecano de Investigación ────────────────────────────────────
function ProfesorHome() {
  const [data, setData] = useState(null);

  useEffect(() => {
    async function load() {
      const [pubs, pres, events, awards] = await Promise.all([
        silentFetch('/api/Publications'),
        silentFetch('/api/Presentations'),
        silentFetch('/api/Events'),
        silentFetch('/api/Awards'),
      ]);
      const revistas  = pubs?.filter(p => p.publicationType === 0).length ?? 0;
      const indexadas = pubs?.filter(p => p.publicationType !== 0).length ?? 0;
      setData({
        publications:  pubs?.length    ?? 0,
        revistas,
        indexadas,
        presentations: pres?.length    ?? 0,
        events:        events?.length  ?? 0,
        awards:        awards?.length  ?? 0,
      });
    }
    load();
  }, []);

  if (!data) return <LoadingHome />;

  return (
    <>
      <div className="stats-grid">
        <StatCard
          icon="bi-file-earmark-text-fill"
          label="Publicaciones"
          value={data.publications}
          color="stat-blue"
          link="/publications"
          sublabel={`${data.revistas} revistas · ${data.indexadas} indexadas`}
        />
        <StatCard
          icon="bi-mic-fill"
          label="Ponencias"
          value={data.presentations}
          color="stat-green"
          link="/events"
        />
        <StatCard
          icon="bi-calendar-event-fill"
          label="Eventos"
          value={data.events}
          color="stat-orange"
          link="/events"
        />
        <StatCard
          icon="bi-trophy-fill"
          label="Premios"
          value={data.awards}
          color="stat-purple"
          link="/awards"
        />
        <StatCard
          icon="bi-file-person-fill"
          label="Mi Currículum"
          value="PDF"
          color="stat-teal"
          link="/curriculum"
          sublabel="Exportar currículum científico"
        />
      </div>
    </>
  );
}

// ─── Vicedecano de Investigación ──────────────────────────────────────────────
function VicedecanoHome() {
  const [data, setData] = useState(null);

  useEffect(() => {
    async function load() {
      const [pubs, pres, events, awards, proyectos, grupos] = await Promise.all([
        silentFetch('/api/Publications/area'),
        silentFetch('/api/Presentations/area'),
        silentFetch('/api/Events'),
        silentFetch('/api/Awards/area'),
        silentFetch('/api/Proyectos'),
        silentFetch('/api/GruposDeInvestigacion/area'),
      ]);
      setData({
        publications:  pubs?.length      ?? 0,
        presentations: pres?.length      ?? 0,
        events:        events?.length    ?? 0,
        awards:        awards?.flatMap(a => a.grantings ?? []).flatMap(g => g.recipients ?? []).length ?? 0,
        proyectos:     proyectos?.length ?? 0,
        grupos:        grupos?.length    ?? 0,
      });
    }
    load();
  }, []);

  if (!data) return <LoadingHome />;

  return (
    <>
      <div className="stats-grid">
        <StatCard
          icon="bi-file-earmark-text-fill"
          label="Publicaciones del Área"
          value={data.publications}
          color="stat-blue"
          link="/publicaciones-area"
        />
        <StatCard
          icon="bi-mic-fill"
          label="Ponencias del Área"
          value={data.presentations}
          color="stat-green"
          link="/events"
        />
        <StatCard
          icon="bi-calendar-event-fill"
          label="Eventos del Área"
          value={data.events}
          color="stat-orange"
          link="/events"
        />
        <StatCard
          icon="bi-trophy-fill"
          label="Premios del Área"
          value={data.awards}
          color="stat-purple"
          link="/premios-area"
        />
        <StatCard
          icon="bi-kanban-fill"
          label="Proyectos del Área"
          value={data.proyectos}
          color="stat-teal"
          link="/proyectos-area"
        />
        <StatCard
          icon="bi-people-fill"
          label="Grupos de Investigación"
          value={data.grupos}
          color="stat-blue"
          link="/grupos-investigacion-area"
        />
      </div>
    </>
  );
}

// ─── Jefe de Proyecto / Jefe de Macroproyecto ─────────────────────────────────
function JefeProyectoHome() {
  const [data, setData] = useState(null);

  useEffect(() => {
    async function load() {
      const proyectos = await silentFetch('/api/Proyectos');
      const totalProyectos = proyectos?.length ?? 0;
      const enEjecucion    = proyectos?.filter(p => p.tipo !== 'en-revision').length ?? 0;
      const enRevision     = proyectos?.filter(p => p.tipo === 'en-revision').length ?? 0;
      const totalMiembros  = proyectos?.reduce((s, p) => s + (p.numeroMiembros ?? 0), 0) ?? 0;
      setData({ totalProyectos, enEjecucion, enRevision, totalMiembros });
    }
    load();
  }, []);

  if (!data) return <LoadingHome />;

  return (
    <>
      <div className="stats-grid">
        <StatCard
          icon="bi-folder-fill"
          label="Mis proyectos"
          value={data.totalProyectos}
          color="stat-blue"
          link="/proyectos"
        />
        <StatCard
          icon="bi-play-circle-fill"
          label="En ejecución"
          value={data.enEjecucion}
          color="stat-green"
          link="/proyectos"
        />
        <StatCard
          icon="bi-hourglass-split"
          label="En revisión"
          value={data.enRevision}
          color="stat-orange"
          link="/proyectos"
        />
        <StatCard
          icon="bi-people-fill"
          label="Investigadores"
          value={data.totalMiembros}
          color="stat-purple"
          link="/proyectos"
          sublabel="Total en todos mis proyectos"
        />
      </div>
    </>
  );
}

// ─── Jefe de Grupo de Investigación ───────────────────────────────────────────
function JefeGrupoHome() {
  const [data, setData] = useState(null);

  useEffect(() => {
    async function load() {
      const grupos = await silentFetch('/api/GruposDeInvestigacion/mine');
      const totalMiembros = grupos?.reduce(
        (s, g) => s + (g.membersCount ?? g.members?.length ?? 0), 0
      ) ?? 0;
      setData({ totalGrupos: grupos?.length ?? 0, totalMiembros });
    }
    load();
  }, []);

  if (!data) return <LoadingHome />;

  return (
    <>
      <div className="stats-grid">
        <StatCard
          icon="bi-diagram-3-fill"
          label="Grupos liderados"
          value={data.totalGrupos}
          color="stat-blue"
          link="/grupos-investigacion"
        />
        <StatCard
          icon="bi-person-badge-fill"
          label="Miembros totales"
          value={data.totalMiembros}
          color="stat-green"
          link="/grupos-investigacion"
          sublabel="En todos tus grupos"
        />
      </div>
    </>
  );
}

// ─── Jefe de Redes ─────────────────────────────────────────────────────────────
function JefeRedesHome() {
  const [data, setData] = useState(null);

  useEffect(() => {
    async function load() {
      const redes = await silentFetch('/api/Redes');
      const totalEventos = redes?.reduce(
        (s, r) => s + (r.eventsCount ?? r.events?.length ?? 0), 0
      ) ?? 0;
      setData({ totalRedes: redes?.length ?? 0, totalEventos });
    }
    load();
  }, []);

  if (!data) return <LoadingHome />;

  return (
    <>
      <div className="stats-grid">
        <StatCard
          icon="bi-share-fill"
          label="Redes gestionadas"
          value={data.totalRedes}
          color="stat-blue"
          link="/redes"
        />
        <StatCard
          icon="bi-calendar-event-fill"
          label="Eventos en redes"
          value={data.totalEventos}
          color="stat-green"
          link="/redes"
          sublabel="En todas las redes"
        />
      </div>
    </>
  );
}

// ─── Superuser / Administrador ────────────────────────────────────────────────
function AdminHome() {
  const [data, setData] = useState(null);

  useEffect(() => {
    async function load() {
      const [users, grupos, proyectos, pubs] = await Promise.all([
        silentFetch('/api/Users'),
        silentFetch('/api/GruposDeInvestigacion'),
        silentFetch('/api/Proyectos'),
        silentFetch('/api/Publications/todas'),
      ]);
      const activeUsers = users?.filter(u => u.isActive).length ?? 0;
      setData({
        totalUsers:         users?.length     ?? 0,
        activeUsers,
        totalGrupos:        grupos?.length    ?? 0,
        totalProyectos:     proyectos?.length ?? 0,
        totalPublicaciones: pubs?.length      ?? 0,
      });
    }
    load();
  }, []);

  if (!data) return <LoadingHome />;

  return (
    <>
      <div className="stats-grid">
        <StatCard
          icon="bi-people-fill"
          label="Usuarios"
          value={data.totalUsers}
          color="stat-blue"
          link="/users"
          sublabel={`${data.activeUsers} activos`}
        />
        <StatCard
          icon="bi-diagram-3-fill"
          label="Grupos de investigación"
          value={data.totalGrupos}
          color="stat-green"
          link="/grupos-investigacion"
        />
        <StatCard
          icon="bi-folder-fill"
          label="Proyectos"
          value={data.totalProyectos}
          color="stat-orange"
          link="/proyectos"
        />
        <StatCard
          icon="bi-file-earmark-text-fill"
          label="Publicaciones"
          value={data.totalPublicaciones}
          color="stat-purple"
          link="/publicaciones"
        />
      </div>
    </>
  );
}

// ─── Main ─────────────────────────────────────────────────────────────────────

export default function DashboardHome() {
  const { user } = useAuth();

  const hour = new Date().getHours();
  const greeting = hour < 12 ? 'Buenos días' : hour < 19 ? 'Buenas tardes' : 'Buenas noches';
  const displayName = user?.userName ?? user?.email ?? 'usuario';
  const role = user?.role;
  const roleLabel = ROLE_LABELS[role] ?? role ?? '';

  function renderContent() {
    if (role === 'Superuser')                      return <AdminHome />;
    if (role === 'Profesor')                        return <ProfesorHome />;
    if (role === 'Vicedecano_de_investigacion')    return <VicedecanoHome />;
    if (role === 'Jefe_de_Proyecto'
      || role === 'Jefe_de_Macroproyecto')         return <JefeProyectoHome />;
    if (role === 'Jefe_de_Grupo_de_investigacion') return <JefeGrupoHome />;
    if (role === 'Jefe_de_Redes')                  return <JefeRedesHome />;
    return null;
  }

  return (
    <div className="dashboard-home">
      <div className="dashboard-home__welcome">
        <div>
          <h2 className="dashboard-home__greeting">
            {greeting}, <span>{displayName}</span> 👋
          </h2>
          <p className="dashboard-home__subtext">
            {user?.AreaNombre
              ? <><i className="bi bi-geo-alt-fill me-1" style={{ opacity: 0.6 }} />{user.AreaNombre} · </>
              : null}
            {roleLabel && <span className="home-role-badge">{roleLabel}</span>}
          </p>
        </div>
      </div>

      {renderContent()}
    </div>
  );
}
