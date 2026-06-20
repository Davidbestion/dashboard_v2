import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

/**
 * Protege rutas accesibles por Vicedecano_de_investigacion, Profesor o Superuser.
 * Si el usuario no está autenticado → /login.
 * Si está autenticado pero no tiene ninguno de esos roles → / (inicio).
 */
export default function VicedecanoOrProfesorOrAdminRoute({ children }) {
  const { user, loading } = useAuth();

  if (loading) return null;

  if (!user) return <Navigate to="/login" replace />;

  const allowed = ['Vicedecano_de_investigacion', 'Profesor', 'Superuser'];
  if (!allowed.includes(user.role)) {
    return <Navigate to="/" replace />;
  }

  return children;
}
