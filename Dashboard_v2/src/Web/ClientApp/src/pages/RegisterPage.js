import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';

const TEACHING_CATEGORIES = [
  { value: 0, label: 'Sin categoría docente' },
  { value: 1, label: 'Profesor Titular' },
  { value: 2, label: 'Profesor Auxiliar' },
  { value: 3, label: 'Profesor Asistente' },
  { value: 4, label: 'Instructor' },
];

const SCIENTIFIC_CATEGORIES = [
  { value: 0, label: 'Sin categoría científica' },
  { value: 1, label: 'Licenciado' },
  { value: 2, label: 'Master' },
  { value: 3, label: 'Doctor' },
];

const INVESTIGATION_CATEGORIES = [
  { value: 0, label: 'Sin categoría de investigación' },
  { value: 1, label: 'Investigador Titular' },
  { value: 2, label: 'Investigador Auxiliar' },
  { value: 3, label: 'Investigador Agregado' },
  { value: 4, label: 'Investigador Asociado' },
];

export default function RegisterPage() {
  const [form, setForm] = useState({
    userName: '',
    userLastName1: '',
    userLastName2: '',
    email: '',
    password: '',
    confirmPassword: '',
    birthDate: '',
    isTrained: false,
    teachingCategory: 0,
    scientificCategory: 0,
    investigationCategory: 0,
  });
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);

  const navigate = useNavigate();

  const set = (field) => (e) => {
    const value = e.target.type === 'checkbox' ? e.target.checked : e.target.value;
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    if (form.password !== form.confirmPassword) {
      setError('Las contraseñas no coinciden.');
      return;
    }

    setLoading(true);
    try {
      const response = await fetch('/api/Auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userName: form.userName,
          userLastName1: form.userLastName1,
          userLastName2: form.userLastName2 || null,
          email: form.email,
          password: form.password,
          birthDate: form.birthDate,
          isTrained: form.isTrained,
          teachingCategory: Number(form.teachingCategory),
          scientificCategory: Number(form.scientificCategory),
          investigationCategory: Number(form.investigationCategory),
        }),
      });

      const data = await response.json().catch(() => ({}));

      if (!response.ok) {
        const errors = data?.errors ?? ['Error al registrar el usuario.'];
        setError(Array.isArray(errors) ? errors.join(' ') : String(errors));
        return;
      }

      setSuccess(true);
    } catch {
      setError('No se pudo conectar con el servidor.');
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <div className="login-screen">
        <div className="login-card">
          <div className="login-card__header">
            <div className="login-card__logo">
              <i className="bi bi-check-circle-fill text-success"></i>
            </div>
            <h1 className="login-card__title">¡Registro exitoso!</h1>
            <p className="login-card__subtitle">
              Tu cuenta ha sido creada. Un administrador deberá asignarte un rol antes de que puedas iniciar sesión.
            </p>
          </div>
          <button className="login-btn" onClick={() => navigate('/login')}>
            <i className="bi bi-box-arrow-in-right me-2"></i>
            Ir al inicio de sesión
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="login-screen">
      <div className="login-card" style={{ maxWidth: '520px' }}>
        <div className="login-card__header">
          <div className="login-card__logo">
            <i className="bi bi-grid-3x3-gap-fill"></i>
          </div>
          <h1 className="login-card__title">SIGIP</h1>
          <p className="login-card__subtitle">Crea tu cuenta</p>
        </div>

        <form onSubmit={handleSubmit} className="login-card__form">
          {error && (
            <div className="login-error">
              <i className="bi bi-exclamation-circle-fill"></i>
              {error}
            </div>
          )}

          <div className="form-field">
            <label className="form-field__label" htmlFor="userName">Nombre</label>
            <div className="form-field__input-wrapper">
              <i className="bi bi-person form-field__icon"></i>
              <input
                id="userName"
                type="text"
                className="form-field__input"
                placeholder="Nombre(s)"
                value={form.userName}
                onChange={set('userName')}
                required
                autoFocus
                autoComplete="given-name"
              />
            </div>
          </div>

          <div className="form-field">
            <label className="form-field__label" htmlFor="userLastName1">Primer apellido <span style={{color:'red'}}>*</span></label>
            <div className="form-field__input-wrapper">
              <i className="bi bi-person form-field__icon"></i>
              <input
                id="userLastName1"
                type="text"
                className="form-field__input"
                placeholder="Primer apellido"
                value={form.userLastName1}
                onChange={set('userLastName1')}
                required
                autoComplete="family-name"
              />
            </div>
          </div>

          <div className="form-field">
            <label className="form-field__label" htmlFor="userLastName2">Segundo apellido</label>
            <div className="form-field__input-wrapper">
              <i className="bi bi-person form-field__icon"></i>
              <input
                id="userLastName2"
                type="text"
                className="form-field__input"
                placeholder="Segundo apellido (opcional)"
                value={form.userLastName2}
                onChange={set('userLastName2')}
                autoComplete="additional-name"
              />
            </div>
          </div>

          <div className="form-field">
            <label className="form-field__label" htmlFor="email">Correo electrónico</label>
            <div className="form-field__input-wrapper">
              <i className="bi bi-envelope form-field__icon"></i>
              <input
                id="email"
                type="email"
                className="form-field__input"
                placeholder="correo@ejemplo.com"
                value={form.email}
                onChange={set('email')}
                required
                autoComplete="email"
              />
            </div>
          </div>

          <div className="form-field">
            <label className="form-field__label" htmlFor="birthDate">Fecha de nacimiento</label>
            <div className="form-field__input-wrapper">
              <i className="bi bi-calendar form-field__icon"></i>
              <input
                id="birthDate"
                type="date"
                className="form-field__input"
                value={form.birthDate}
                onChange={set('birthDate')}
                required
                max={new Date().toISOString().split('T')[0]}
              />
            </div>
          </div>

          <div className="form-field">
            <label className="d-flex align-items-center gap-2" style={{ cursor: 'pointer' }}>
              <input
                type="checkbox"
                className="form-check-input mt-0"
                checked={form.isTrained}
                onChange={set('isTrained')}
              />
              <span className="form-field__label mb-0">Es adiestrado (sin categorías docente/científica)</span>
            </label>
          </div>

          {!form.isTrained && (
            <>
              <div className="form-field">
                <label className="form-field__label" htmlFor="teachingCategory">Categoría docente</label>
                <select
                  id="teachingCategory"
                  className="form-field__input"
                  value={form.teachingCategory}
                  onChange={set('teachingCategory')}
                >
                  {TEACHING_CATEGORIES.map((c) => (
                    <option key={c.value} value={c.value}>{c.label}</option>
                  ))}
                </select>
              </div>

              <div className="form-field">
                <label className="form-field__label" htmlFor="scientificCategory">Categoría científica</label>
                <select
                  id="scientificCategory"
                  className="form-field__input"
                  value={form.scientificCategory}
                  onChange={set('scientificCategory')}
                >
                  {SCIENTIFIC_CATEGORIES.map((c) => (
                    <option key={c.value} value={c.value}>{c.label}</option>
                  ))}
                </select>
              </div>

              <div className="form-field">
                <label className="form-field__label" htmlFor="investigationCategory">Categoría de investigación</label>
                <select
                  id="investigationCategory"
                  className="form-field__input"
                  value={form.investigationCategory}
                  onChange={set('investigationCategory')}
                >
                  {INVESTIGATION_CATEGORIES.map((c) => (
                    <option key={c.value} value={c.value}>{c.label}</option>
                  ))}
                </select>
              </div>
            </>
          )}

          <div className="form-field">
            <label className="form-field__label" htmlFor="password">Contraseña</label>
            <div className="form-field__input-wrapper">
              <i className="bi bi-lock form-field__icon"></i>
              <input
                id="password"
                type={showPassword ? 'text' : 'password'}
                className="form-field__input"
                placeholder="••••••••"
                value={form.password}
                onChange={set('password')}
                required
                autoComplete="new-password"
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

          <div className="form-field">
            <label className="form-field__label" htmlFor="confirmPassword">Confirmar contraseña</label>
            <div className="form-field__input-wrapper">
              <i className="bi bi-lock-fill form-field__icon"></i>
              <input
                id="confirmPassword"
                type={showPassword ? 'text' : 'password'}
                className="form-field__input"
                placeholder="••••••••"
                value={form.confirmPassword}
                onChange={set('confirmPassword')}
                required
                autoComplete="new-password"
              />
            </div>
          </div>

          <button type="submit" className="login-btn" disabled={loading}>
            {loading ? (
              <>
                <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                Registrando...
              </>
            ) : (
              <>
                <i className="bi bi-person-plus me-2"></i>
                Registrarse
              </>
            )}
          </button>

          <p className="text-center mt-3 mb-0" style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
            ¿Ya tienes cuenta?{' '}
            <Link to="/login" style={{ color: 'var(--accent-color)' }}>
              Inicia sesión
            </Link>
          </p>
        </form>
      </div>
    </div>
  );
}
