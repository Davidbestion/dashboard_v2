import React from 'react';
import { useAuth } from '../contexts/AuthContext';

const stats = [
  { icon: 'bi-people-fill', label: 'Usuarios', value: '—', color: 'stat-blue' },
  { icon: 'bi-shield-check', label: 'Roles activos', value: '—', color: 'stat-green' },
  { icon: 'bi-activity', label: 'Actividad hoy', value: '—', color: 'stat-orange' },
  { icon: 'bi-bell', label: 'Notificaciones', value: '—', color: 'stat-purple' },
];

const quickLinks = [
  { icon: 'bi-people', label: 'Gestionar usuarios', to: '/users', desc: 'Crear, editar y desactivar cuentas' },
  { icon: 'bi-shield-lock', label: 'Roles y permisos', to: '/roles', desc: 'Configurar accesos del sistema' },
  { icon: 'bi-gear', label: 'Configuración', to: '/settings', desc: 'Parámetros generales' },
];

export default function DashboardHome() {
  const { user } = useAuth();

  const hour = new Date().getHours();
  const greeting = hour < 12 ? 'Buenos días' : hour < 19 ? 'Buenas tardes' : 'Buenas noches';
  const displayName = user?.userName ?? user?.email ?? 'usuario';

  return (
    <div className="dashboard-home">
      <div className="dashboard-home__welcome">
        <div>
          <h2 className="dashboard-home__greeting">
            {greeting}, <span>{displayName}</span> 👋
          </h2>
          <p className="dashboard-home__subtext">
            Aquí tienes un resumen de la actividad del sistema.
          </p>
        </div>
      </div>

      {/* Stats row */}
      <div className="stats-grid">
        {stats.map((s) => (
          <div key={s.label} className={`stat-card ${s.color}`}>
            <div className="stat-card__icon">
              <i className={`bi ${s.icon}`}></i>
            </div>
            <div className="stat-card__body">
              <span className="stat-card__value">{s.value}</span>
              <span className="stat-card__label">{s.label}</span>
            </div>
          </div>
        ))}
      </div>

      {/* Quick access */}
      <div className="section-header">
        <h3 className="section-header__title">Acceso rápido</h3>
      </div>
      <div className="quick-grid">
        {quickLinks.map((q) => (
          <a key={q.label} href={q.to} className="quick-card">
            <div className="quick-card__icon">
              <i className={`bi ${q.icon}`}></i>
            </div>
            <div className="quick-card__body">
              <span className="quick-card__label">{q.label}</span>
              <span className="quick-card__desc">{q.desc}</span>
            </div>
            <i className="bi bi-chevron-right quick-card__arrow"></i>
          </a>
        ))}
      </div>
    </div>
  );
}
