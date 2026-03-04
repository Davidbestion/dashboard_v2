import React, { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export default function TopBar({ pageTitle }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);

  const initials = user?.userName
    ? user.userName.slice(0, 2).toUpperCase()
    : user?.email?.slice(0, 2).toUpperCase() ?? '??';

  const displayName = user?.userName ?? user?.email ?? 'Usuario';

  const handleLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  // Close dropdown on outside click
  useEffect(() => {
    const handler = (e) => {
      if (menuRef.current && !menuRef.current.contains(e.target)) {
        setMenuOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  return (
    <header className="topbar">
      <div className="topbar__left">
        {pageTitle && <h1 className="topbar__title">{pageTitle}</h1>}
      </div>

      <div className="topbar__right">
        {/* Notification bell */}
        <button className="topbar__icon-btn" aria-label="Notificaciones">
          <i className="bi bi-bell"></i>
          <span className="topbar__badge">0</span>
        </button>

        {/* User menu */}
        <div className="topbar__user" ref={menuRef}>
          <button
            className="topbar__user-btn"
            onClick={() => setMenuOpen(!menuOpen)}
            aria-expanded={menuOpen}
            aria-haspopup="true"
          >
            <span className="topbar__avatar">{initials}</span>
            <span className="topbar__user-name">{displayName}</span>
            <i className={`bi bi-chevron-${menuOpen ? 'up' : 'down'} topbar__chevron`}></i>
          </button>

          {menuOpen && (
            <div className="topbar__dropdown">
              <div className="topbar__dropdown-header">
                <span className="topbar__dropdown-name">{displayName}</span>
                <span className="topbar__dropdown-email">{user?.email}</span>
              </div>
              <div className="topbar__dropdown-divider"></div>
              <a href="/Identity/Account/Manage" className="topbar__dropdown-item">
                <i className="bi bi-person-circle"></i>
                Mi perfil
              </a>
              <button onClick={handleLogout} className="topbar__dropdown-item topbar__dropdown-item--danger">
                <i className="bi bi-box-arrow-right"></i>
                Cerrar sesión
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
