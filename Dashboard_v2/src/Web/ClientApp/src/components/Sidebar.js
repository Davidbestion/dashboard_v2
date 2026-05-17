import React from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const baseGroups = [
  {
    heading: 'Principal',
    items: [
      { to: '/', icon: 'bi-house-door', label: 'Inicio', end: true },
    ],
  },
];

const profesorGroups = [
  {
    heading: 'Actividad académica',
    items: [
      { to: '/publications', icon: 'bi-journal-text', label: 'Mis publicaciones' },
      { to: '/awards', icon: 'bi-trophy', label: 'Mis premios' },
      { to: '/events', icon: 'bi-mic', label: 'Mis eventos' },
      { to: '/mis-patentes',  icon: 'bi-lightbulb', label: 'Mis Patentes' },
      { to: '/mis-registros', icon: 'bi-clipboard-check', label: 'Mis Registros' },
      { to: '/mis-normas',    icon: 'bi-file-earmark-text', label: 'Mis Normas' },
      { to: '/mis-productos', icon: 'bi-box-seam', label: 'Mis Productos' },
    ],
  },
];

const investigacionGroups = [
  {
    heading: 'Investigación',
    items: [
      { to: '/mis-grupos', icon: 'bi-people-fill', label: 'Mis Grupos de Investigación' },
    ],
  },
];

const coordinadorGroups = [
  {
    heading: 'Coordinación de Redes',
    items: [
      { to: '/mis-redes-publicaciones', icon: 'bi-journal-text', label: 'Publicaciones de Redes' },
    ],
  },
];

const jefeGroups = [
  {
    heading: 'Investigación',
    items: [
      { to: '/grupos-investigacion', icon: 'bi-people-fill', label: 'Grupos de Investigación' },
    ],
  },
];

const jefeRedesGroups = [
  {
    heading: 'Gestión de Redes',
    items: [
      { to: '/redes', icon: 'bi-globe', label: 'Redes' },
      { to: '/mis-redes-publicaciones', icon: 'bi-journal-text', label: 'Publicaciones de Redes' },
    ],
  },
];

const jefeDeProyectoGroups = [
  {
    heading: 'Gestión de Proyectos',
    items: [
      { to: '/proyectos', icon: 'bi-kanban', label: 'Mis Proyectos' },
      { to: '/publicaciones', icon: 'bi-journal-text', label: 'Publicaciones' },
    ],
  },
];

const vicedecanoGroups = [
  {
    heading: 'Actividad del Área',
    items: [
      { to: '/publicaciones-area', icon: 'bi-journal-text', label: 'Publicaciones del Área' },
      { to: '/patentes-area',       icon: 'bi-lightbulb',         label: 'Patentes del Área' },
      { to: '/registros-area',      icon: 'bi-clipboard-check',   label: 'Registros del Área' },
      { to: '/normas-area',         icon: 'bi-file-earmark-text', label: 'Normas del Área' },
      { to: '/productos-area',      icon: 'bi-box-seam',          label: 'Productos del Área' },
      { to: '/events', icon: 'bi-mic', label: 'Eventos' },
    ],
  },
];

const adminGroups = [
  {
    heading: 'Administración',
    items: [
      { to: '/users', icon: 'bi-people', label: 'Usuarios' },
      { to: '/roles', icon: 'bi-shield-lock', label: 'Roles y permisos' },
    ],
  },
  {
    heading: 'Estructura académica',
    items: [
      { to: '/universidades', icon: 'bi-building', label: 'Universidades' },
      { to: '/areas', icon: 'bi-diagram-3', label: 'Áreas' },
      { to: '/grupos-estudiantiles', icon: 'bi-people', label: 'Grupos Estudiantiles' },
      { to: '/grupos-investigacion', icon: 'bi-people-fill', label: 'Grupos de Investigación' },
      { to: '/areas-conocimiento', icon: 'bi-book', label: 'Áreas del Conocimiento' },
      { to: '/lineas-investigacion', icon: 'bi-lightbulb', label: 'Líneas de Investigación' },
    ],
  },
  {
    heading: 'Gestión de Proyectos',
    items: [
      { to: '/awards', icon: 'bi-trophy', label: 'Premios' },
      { to: '/events', icon: 'bi-mic', label: 'Eventos y presentaciones' },
      { to: '/proyectos', icon: 'bi-kanban', label: 'Proyectos' },
      { to: '/publicaciones', icon: 'bi-journal-text', label: 'Publicaciones' },
      { to: '/clasificaciones', icon: 'bi-tags', label: 'Clasificaciones' },
          { to: '/registros', icon: 'bi-file-earmark-text', label: 'Registros' },
          { to: '/normas', icon: 'bi-book', label: 'Normas' },
          { to: '/patentes', icon: 'bi-lightbulb', label: 'Patentes' },
          { to: '/productos-comercializados', icon: 'bi-box-seam', label: 'Productos comercializados' },
              { to: '/redes', icon: 'bi-globe', label: 'Redes' },
    ],
  },
  {
    heading: 'Sistema',
    items: [
      { to: '/settings', icon: 'bi-gear', label: 'Configuración' },
    ],
  },
];

export default function Sidebar({ collapsed, onToggle }) {
  const { user } = useAuth();

  const groups = [
    ...baseGroups,
    ...(user?.role === 'Profesor' ? [...profesorGroups, ...investigacionGroups, ...coordinadorGroups] : []),
    ...(user?.role === 'Jefe_de_Grupo_de_investigacion' ? jefeGroups : []),
    ...(user?.role === 'Jefe_de_Redes' ? jefeRedesGroups : []),
    ...(user?.role === 'Jefe_de_Proyecto' ? jefeDeProyectoGroups : []),
    ...(user?.role === 'Vicedecano_de_investigacion' ? vicedecanoGroups : []),
    ...(user?.role === 'Superuser' ? adminGroups : []),
  ];

  return (
    <aside className={`sidebar${collapsed ? ' sidebar--collapsed' : ''}`}>
      <div className="sidebar__brand">
        {!collapsed && (
          <span className="sidebar__brand-name">Dashboard v2</span>
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
          <div key={group.heading} className="sidebar__group">
            {!collapsed && (
              <span className="sidebar__group-heading">{group.heading}</span>
            )}
            {group.items.map((item) => (
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
        ))}
      </nav>
    </aside>
  );
}
