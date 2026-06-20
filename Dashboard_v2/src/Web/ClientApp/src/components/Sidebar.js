import React, { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

// ── Inicio ────────────────────────────────────────────────────────────────────
const baseGroups = [
  {
    heading: 'Principal',
    items: [
      { to: '/', icon: 'bi-house-door', label: 'Inicio', end: true },
    ],
  },
];

// ── Profesor ──────────────────────────────────────────────────────────────────
const profesorGroups = [
  {
    heading: 'Publicaciones',
    items: [
      { to: '/publications', icon: 'bi-journal-text', label: 'Mis publicaciones' },
    ],
  },
  {
    heading: 'Eventos',
    items: [
      { to: '/events', icon: 'bi-mic', label: 'Mis eventos' },
    ],
  },
  {
    heading: 'Proyectos',
    items: [
      { to: '/mis-proyectos', icon: 'bi-kanban', label: 'Mis proyectos' },
    ],
  },
  {
    heading: 'Premios',
    items: [
      { to: '/awards', icon: 'bi-trophy', label: 'Mis premios' },
    ],
  },
  {
    heading: 'Redes',
    items: [
      { to: '/mis-redes',               icon: 'bi-globe',        label: 'Mis redes' },
      { to: '/mis-redes-publicaciones', icon: 'bi-journal-text', label: 'Publicaciones de mis redes' },
    ],
  },
  {
    heading: 'Registros',
    items: [
      { to: '/mis-patentes',  icon: 'bi-lightbulb',         label: 'Mis patentes' },
      { to: '/mis-registros', icon: 'bi-clipboard-check',   label: 'Mis registros' },
      { to: '/mis-normas',    icon: 'bi-file-earmark-text', label: 'Mis normas' },
      { to: '/mis-productos', icon: 'bi-box-seam',          label: 'Mis productos' },
    ],
  },
  {
    heading: 'Grupos de Investigación',
    items: [
      { to: '/mis-grupos', icon: 'bi-people-fill', label: 'Mis grupos' },
    ],
  },
];

// ── Jefe de Grupo de Investigación ────────────────────────────────────────────
const jefeGroups = [
  {
    heading: 'Grupos de Investigación',
    items: [
      { to: '/grupos-investigacion', icon: 'bi-people-fill', label: 'Grupos de investigación' },
    ],
  },
];

// ── Jefe de Redes ─────────────────────────────────────────────────────────────
const jefeRedesGroups = [
  {
    heading: 'Redes',
    items: [
      { to: '/redes',                   icon: 'bi-globe',        label: 'Redes' },
      { to: '/mis-redes-publicaciones', icon: 'bi-journal-text', label: 'Publicaciones de redes' },
    ],
  },
];

// ── Jefe de Proyecto ──────────────────────────────────────────────────────────
const jefeDeProyectoGroups = [
  {
    heading: 'Proyectos',
    items: [
      { to: '/proyectos', icon: 'bi-kanban', label: 'Proyectos' },
    ],
  },
  {
    heading: 'Publicaciones',
    items: [
      { to: '/publicaciones', icon: 'bi-journal-text', label: 'Publicaciones' },
    ],
  },
];

// ── Vicedecano de investigación ───────────────────────────────────────────────
const vicedecanoGroups = [
  {
    heading: 'Publicaciones',
    items: [
      { to: '/publicaciones-area', icon: 'bi-journal-text', label: 'Publicaciones del área' },
    ],
  },
  {
    heading: 'Eventos',
    items: [
      { to: '/events', icon: 'bi-mic', label: 'Eventos del área' },
    ],
  },
  {
    heading: 'Proyectos',
    items: [
      { to: '/proyectos-area', icon: 'bi-kanban', label: 'Proyectos del área' },
    ],
  },
  {
    heading: 'Premios',
    items: [
      { to: '/premios-area', icon: 'bi-trophy', label: 'Premios del área' },
    ],
  },
  {
    heading: 'Redes',
    items: [
      { to: '/redes-area', icon: 'bi-globe', label: 'Redes del área' },
    ],
  },
  {
    heading: 'Registros',
    items: [
      { to: '/patentes-area',  icon: 'bi-lightbulb',         label: 'Patentes del área' },
      { to: '/registros-area', icon: 'bi-clipboard-check',   label: 'Registros del área' },
      { to: '/normas-area',    icon: 'bi-file-earmark-text', label: 'Normas del área' },
      { to: '/productos-area', icon: 'bi-box-seam',          label: 'Productos del área' },
    ],
  },
  {
    heading: 'Grupos de Investigación',
    items: [
      { to: '/grupos-investigacion-area', icon: 'bi-people-fill', label: 'Grupos de inv. del área' },
    ],
  },
  {
    heading: 'Grupos Estudiantiles',
    items: [
      { to: '/grupos-estudiantiles-area', icon: 'bi-people', label: 'Grupos estud. del área' },
    ],
  },
];

// ── Superuser ─────────────────────────────────────────────────────────────────
const adminGroups = [
  {
    heading: 'Publicaciones',
    items: [
      { to: '/publicaciones', icon: 'bi-journal-text', label: 'Publicaciones' },
    ],
  },
  {
    heading: 'Eventos',
    items: [
      { to: '/events', icon: 'bi-mic', label: 'Eventos y presentaciones' },
    ],
  },
  {
    heading: 'Proyectos',
    items: [
      { to: '/proyectos',       icon: 'bi-kanban', label: 'Proyectos' },
      { to: '/clasificaciones', icon: 'bi-tags',   label: 'Clasificaciones' },
    ],
  },
  {
    heading: 'Premios',
    items: [
      { to: '/awards', icon: 'bi-trophy', label: 'Premios' },
    ],
  },
  {
    heading: 'Redes',
    items: [
      { to: '/redes', icon: 'bi-globe', label: 'Redes' },
    ],
  },
  {
    heading: 'Registros',
    items: [
      { to: '/patentes',                  icon: 'bi-lightbulb',         label: 'Patentes' },
      { to: '/registros',                 icon: 'bi-clipboard-check',   label: 'Registros' },
      { to: '/normas',                    icon: 'bi-book',              label: 'Normas' },
      { to: '/productos-comercializados', icon: 'bi-box-seam',          label: 'Productos comercializados' },
    ],
  },
  {
    heading: 'Grupos de Investigación',
    items: [
      { to: '/grupos-investigacion', icon: 'bi-people-fill', label: 'Grupos de investigación' },
    ],
  },
  {
    heading: 'Grupos Estudiantiles',
    items: [
      { to: '/grupos-estudiantiles', icon: 'bi-people', label: 'Grupos estudiantiles' },
    ],
  },
  {
    heading: 'Administración',
    items: [
      { to: '/users',               icon: 'bi-people',    label: 'Usuarios' },
      { to: '/universidades',       icon: 'bi-building',  label: 'Universidades' },
      { to: '/areas',               icon: 'bi-diagram-3', label: 'Áreas' },
      { to: '/areas-conocimiento',  icon: 'bi-book',      label: 'Áreas del conocimiento' },
      { to: '/lineas-investigacion', icon: 'bi-lightbulb', label: 'Líneas de investigación' },
    ],
  },
];

function SidebarGroup({ heading, items, collapsed }) {
  const key = `sidebar_open_${heading}`;
  const [open, setOpen] = useState(() => {
    const saved = localStorage.getItem(key);
    return saved === null ? false : saved === 'true';
  });

  const toggle = () => setOpen(prev => {
    const next = !prev;
    localStorage.setItem(key, next);
    return next;
  });

  return (
    <div className="sidebar__group">
      {!collapsed && (
        <button
          className="sidebar__group-btn"
          onClick={toggle}
          aria-expanded={open}
        >
          <span className="sidebar__group-heading-text">{heading}</span>
          <i className="bi bi-chevron-down sidebar__group-chevron"></i>
        </button>
      )}
      <div className={`sidebar__group-items${(!collapsed && !open) ? ' sidebar__group-items--hidden' : ''}`}>
        {items.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            className={({ isActive }) =>
              `sidebar__link${isActive ? ' sidebar__link--active' : ''}`
            }
            title={collapsed ? item.label : undefined}
          >
            <i className={`bi ${item.icon} sidebar__link-icon`}></i>
            {!collapsed && (
              <span className="sidebar__link-label">{item.label}</span>
            )}
          </NavLink>
        ))}
      </div>
    </div>
  );
}

export default function Sidebar({ collapsed, onToggle }) {
  const { user } = useAuth();

  const groups = [
    ...baseGroups,
    ...(user?.role === 'Profesor'                       ? profesorGroups       : []),
    ...(user?.role === 'Jefe_de_Grupo_de_investigacion' ? jefeGroups           : []),
    ...(user?.role === 'Jefe_de_Redes'                  ? jefeRedesGroups      : []),
    ...(user?.role === 'Jefe_de_Proyecto'               ? jefeDeProyectoGroups : []),
    ...(user?.role === 'Vicedecano_de_investigacion'    ? vicedecanoGroups     : []),
    ...(user?.role === 'Superuser'                      ? adminGroups          : []),
  ];

  return (
    <aside className={`sidebar${collapsed ? ' sidebar--collapsed' : ''}`}>
      <div className="sidebar__brand">
        {!collapsed && (
          <span className="sidebar__brand-name">SIGIP</span>
        )}
        <button
          className="sidebar__toggle"
          onClick={onToggle}
          aria-label={collapsed ? 'Expandir menú' : 'Colapsar menú'}
        >
          <i className={`bi bi-${collapsed ? 'chevron-bar-right' : 'chevron-bar-left'}`}></i>
        </button>
      </div>

      <nav className="sidebar__nav">
        {groups.map((group) => (
          <SidebarGroup
            key={group.heading}
            heading={group.heading}
            items={group.items}
            collapsed={collapsed}
          />
        ))}
      </nav>
    </aside>
  );
}
