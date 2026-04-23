import React, { useState, useRef } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [step, setStep] = useState('credentials'); // 'credentials' | 'roleSelection' | 'areaSelection' | 'authorLink'
  const [availableRoles, setAvailableRoles] = useState([]);
  const [selectedRole, setSelectedRole] = useState('');
  const [availableAreas, setAvailableAreas] = useState([]);
  const [selectedArea, setSelectedArea] = useState('');

  // Estado del paso de vinculación de autor
  const [authorMatches, setAuthorMatches] = useState(null); // { exactMatches, fuzzyMatches }
  const [selectedAuthorId, setSelectedAuthorId] = useState('none'); // 'none' | authorId
  const [authorLinkLoading, setAuthorLinkLoading] = useState(false);
  const [authorLinkError, setAuthorLinkError] = useState('');

  const justNavigatedBack = useRef(false);

  const { login, refetch } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = location.state?.from?.pathname || '/';

  // Tras login completo: comprueba si hay autores candidatos para vincular
  const checkAuthorMatches = async () => {
    try {
      const res = await fetch('/api/Authors/potential-matches', { credentials: 'include' });
      if (!res.ok) return null;
      return await res.json();
    } catch {
      return null;
    }
  };

  /**
   * Configura el paso de selección de área usando las opciones entregadas por el backend.
   * Centralizar esta transición evita repetir estado derivado en varios handlers del login.
   */
  const moveToAreaSelection = (areas) => {
    setAvailableAreas(areas);
    setSelectedArea(areas[0]?.id ?? '');
    setStep('areaSelection');
  };

  /**
   * Completa la post-autenticación. Primero ofrece la vinculación opcional con un autor existente
   * y, si no hay coincidencias, confirma la sesión actual antes de navegar al destino solicitado.
   */
  const finishLogin = async () => {
    const matches = await checkAuthorMatches();
    if (matches && (matches.exactMatches.length > 0 || matches.fuzzyMatches.length > 0)) {
      setAuthorMatches(matches);
      // Pre-seleccionar la coincidencia exacta si hay una sola
      if (matches.exactMatches.length === 1 && matches.fuzzyMatches.length === 0) {
        setSelectedAuthorId(matches.exactMatches[0].id);
      } else {
        setSelectedAuthorId('none');
      }
      setStep('authorLink');
    } else {
      const refreshedUser = await refetch();

      if (!refreshedUser?.areaId) {
        setError('No fue posible determinar el área del usuario autenticado.');
        return;
      }

      navigate(from, { replace: true });
    }
  };

  const handleCredentialsSubmit = async (e) => {
    e.preventDefault();
    if (justNavigatedBack.current) {
      justNavigatedBack.current = false;
      return;
    }
    setError('');
    setLoading(true);
    try {
      const result = await login(email, password);
      if (result?.requiresRoleSelection) {
        setAvailableRoles(result.availableRoles);
        setSelectedRole(result.availableRoles[0] ?? '');
        setStep('roleSelection');
      } else if (result?.requiresAreaSelection) {
        moveToAreaSelection(result.availableAreas);
      } else {
        await finishLogin();
      }
    } catch (err) {
      setError(err.message || 'Error al iniciar sesión.');
    } finally {
      setLoading(false);
    }
  };

  const handleRoleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const result = await login(email, password, selectedRole);
      if (result?.requiresAreaSelection) {
        moveToAreaSelection(result.availableAreas);
        setLoading(false);
        return;
      }
      await finishLogin();
    } catch (err) {
      setError(err.message || 'Error al iniciar sesión.');
    } finally {
      setLoading(false);
    }
  };

  if (step === 'authorLink') {
    const allCandidates = [
      ...(authorMatches?.exactMatches ?? []),
      ...(authorMatches?.fuzzyMatches ?? []),
    ];
    const handleAuthorLinkSubmit = async (e) => {
      e.preventDefault();
      if (selectedAuthorId !== 'none') {
        setAuthorLinkLoading(true);
        setAuthorLinkError('');
        try {
          const res = await fetch(`/api/Authors/${selectedAuthorId}/link-to-me`, {
            method: 'POST',
            credentials: 'include',
          });
          if (!res.ok) {
            const data = await res.json().catch(() => ({}));
            const errs = Array.isArray(data?.errors) ? data.errors : ['Error al vincular el autor.'];
            setAuthorLinkError(errs.join(' '));
            setAuthorLinkLoading(false);
            return;
          }
        } catch {
          setAuthorLinkError('No se pudo conectar con el servidor.');
          setAuthorLinkLoading(false);
          return;
        }
      }
      navigate(from, { replace: true });
    };

    return (
      <div className="login-screen">
        <div className="login-card">
          <div className="login-card__header">
            <div className="login-card__logo">
              <i className="bi bi-person-lines-fill"></i>
            </div>
            <h1 className="login-card__title">Perfil de autor</h1>
            <p className="login-card__subtitle">
              Se encontró{allCandidates.length === 1 ? '' : 'n'} {allCandidates.length} autor{allCandidates.length === 1 ? '' : 'es'} registrado{allCandidates.length === 1 ? '' : 's'} con un nombre similar al tuyo.
              ¿Eres alguno de ellos?
            </p>
          </div>

          <form onSubmit={handleAuthorLinkSubmit} className="login-card__form">
            {authorLinkError && (
              <div className="login-error">
                <i className="bi bi-exclamation-circle-fill"></i>
                {authorLinkError}
              </div>
            )}

            <div className="form-field">
              <div className="role-list">
                {allCandidates.map((a) => (
                  <label
                    key={a.id}
                    className={`role-option${selectedAuthorId === a.id ? ' role-option--selected' : ''}`}
                  >
                    <input
                      type="radio"
                      name="author"
                      value={a.id}
                      checked={selectedAuthorId === a.id}
                      onChange={() => setSelectedAuthorId(a.id)}
                    />
                    <i className="bi bi-person me-2"></i>
                    {a.name}
                    {(authorMatches?.exactMatches ?? []).some(e => e.id === a.id) && (
                      <span className="badge bg-success ms-2" style={{fontSize:'0.7em'}}>Coincidencia exacta</span>
                    )}
                  </label>
                ))}
                <label
                  className={`role-option${selectedAuthorId === 'none' ? ' role-option--selected' : ''}`}
                >
                  <input
                    type="radio"
                    name="author"
                    value="none"
                    checked={selectedAuthorId === 'none'}
                    onChange={() => setSelectedAuthorId('none')}
                  />
                  <i className="bi bi-x-circle me-2"></i>
                  Ninguno soy yo
                </label>
              </div>
            </div>

            <button type="submit" className="login-btn" disabled={authorLinkLoading}>
              {authorLinkLoading ? (
                <>
                  <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                  Guardando...
                </>
              ) : selectedAuthorId === 'none' ? (
                <><i className="bi bi-arrow-right me-2"></i>Continuar sin vincular</>
              ) : (
                <><i className="bi bi-link-45deg me-2"></i>Vincular y continuar</>
              )}
            </button>
          </form>
        </div>
      </div>
    );
  }

  if (step === 'roleSelection') {
    return (
      <div className="login-screen">
        <div className="login-card">
          <div className="login-card__header">
            <div className="login-card__logo">
              <i className="bi bi-grid-3x3-gap-fill"></i>
            </div>
            <h1 className="login-card__title">Dashboard v2</h1>
            <p className="login-card__subtitle">Selecciona el rol para esta sesión</p>
          </div>

          <form onSubmit={handleRoleSubmit} className="login-card__form">
            {error && (
              <div className="login-error">
                <i className="bi bi-exclamation-circle-fill"></i>
                {error}
              </div>
            )}

            <div className="form-field">
              <label className="form-field__label">Rol activo</label>
              <div className="role-list">
                {availableRoles.map((role) => (
                  <label
                    key={role}
                    className={`role-option${selectedRole === role ? ' role-option--selected' : ''}`}
                  >
                    <input
                      type="radio"
                      name="role"
                      value={role}
                      checked={selectedRole === role}
                      onChange={() => setSelectedRole(role)}
                    />
                    <i className="bi bi-person-badge me-2"></i>
                    {role}
                  </label>
                ))}
              </div>
            </div>

            <button type="submit" className="login-btn" disabled={loading || !selectedRole}>
              {loading ? (
                <>
                  <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                  Iniciando sesión...
                </>
              ) : (
                <>
                  <i className="bi bi-box-arrow-in-right me-2"></i>
                  Continuar
                </>
              )}
            </button>

            <button
              type="button"
              className="btn btn-link w-100 mt-2"
              onClick={() => {
                justNavigatedBack.current = true;
                setStep('credentials');
                setError('');
              }}
            >
              Volver
            </button>
          </form>
        </div>
      </div>
    );
  }

  /**
   * Persiste el área elegida reintentando el login con las mismas credenciales y el rol
   * ya decidido para la sesión actual.
   */
  const handleAreaSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const roleToSend = selectedRole || null;
      await login(email, password, roleToSend, selectedArea);
      await finishLogin();
    } catch (err) {
      setError(err.message || 'Error al seleccionar el área.');
    } finally {
      setLoading(false);
    }
  };

  if (step === 'areaSelection') {
    return (
      <div className="login-screen">
        <div className="login-card">
          <div className="login-card__header">
            <div className="login-card__logo">
              <i className="bi bi-building"></i>
            </div>
            <h1 className="login-card__title">Selecciona tu Área</h1>
            <p className="login-card__subtitle">Elige el área a la que perteneces para continuar</p>
          </div>

          <form onSubmit={handleAreaSubmit} className="login-card__form">
            {error && (
              <div className="login-error">
                <i className="bi bi-exclamation-circle-fill"></i>
                {error}
              </div>
            )}

            <div className="form-field">
              <label className="form-field__label">Área</label>
              <div className="role-list">
                {availableAreas.map((a) => (
                  <label
                    key={a.id}
                    className={`role-option${selectedArea === a.id ? ' role-option--selected' : ''}`}
                  >
                    <input
                      type="radio"
                      name="area"
                      value={a.id}
                      checked={selectedArea === a.id}
                      onChange={() => setSelectedArea(a.id)}
                    />
                    <i className="bi bi-building me-2"></i>
                    {a.nombre}
                  </label>
                ))}
              </div>
            </div>

            <button type="submit" className="login-btn" disabled={loading || !selectedArea}>
              {loading ? (
                <>
                  <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                  Guardando...
                </>
              ) : (
                <>
                  <i className="bi bi-box-arrow-in-right me-2"></i>
                  Continuar
                </>
              )}
            </button>

            <button
              type="button"
              className="btn btn-link w-100 mt-2"
              onClick={() => {
                justNavigatedBack.current = true;
                setStep('credentials');
                setError('');
              }}
            >
              Volver
            </button>
          </form>
        </div>
      </div>
    );
  }

  return (
    <div className="login-screen">
      <div className="login-card">
        <div className="login-card__header">
          <div className="login-card__logo">
            <i className="bi bi-grid-3x3-gap-fill"></i>
          </div>
          <h1 className="login-card__title">Dashboard v2</h1>
          <p className="login-card__subtitle">Inicia sesión para continuar</p>
        </div>

        <form onSubmit={handleCredentialsSubmit} className="login-card__form">
          {error && (
            <div className="login-error">
              <i className="bi bi-exclamation-circle-fill"></i>
              {error}
            </div>
          )}

          <div className="form-field">
            <label className="form-field__label" htmlFor="email">
              Correo electrónico
            </label>
            <div className="form-field__input-wrapper">
              <i className="bi bi-envelope form-field__icon"></i>
              <input
                id="email"
                type="email"
                className="form-field__input"
                placeholder="correo@ejemplo.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoFocus
                autoComplete="email"
              />
            </div>
          </div>

          <div className="form-field">
            <label className="form-field__label" htmlFor="password">
              Contraseña
            </label>
            <div className="form-field__input-wrapper">
              <i className="bi bi-lock form-field__icon"></i>
              <input
                id="password"
                type={showPassword ? 'text' : 'password'}
                className="form-field__input"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="current-password"
              />
              <button
                type="button"
                className="form-field__toggle"
                onClick={() => setShowPassword(!showPassword)}
                tabIndex={-1}
                aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
              >
                <i className={`bi bi-${showPassword ? 'eye-slash' : 'eye'}`}></i>
              </button>
            </div>
          </div>

          <button
            type="submit"
            className="login-btn"
            disabled={loading}
          >
            {loading ? (
              <>
                <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                Iniciando sesión...
              </>
            ) : (
              <>
                <i className="bi bi-box-arrow-in-right me-2"></i>
                Iniciar sesión
              </>
            )}
          </button>
        </form>

        <p className="text-center mt-3 mb-0" style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
          ¿No tienes cuenta?{' '}
          <Link to="/register" style={{ color: 'var(--accent-color)' }}>
            Regístrate
          </Link>
        </p>
      </div>
    </div>
  );
}
