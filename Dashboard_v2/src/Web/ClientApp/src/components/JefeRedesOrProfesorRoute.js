import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

/**
 * Protege rutas accesibles por Superuser, Jefe_de_Redes O Profesor.
 * Si no está autenticado → /login.
 * Si está autenticado pero no tiene ninguno de esos roles → / (inicio).
 */
export default function JefeRedesOrProfesorRoute({ children }) {
  const { user, loading } = useAuth();

  if (loading) return null;

  if (!user) return <Navigate to="/login" replace />;

  if (
    user.role !== 'Superuser' &&
    user.role !== 'Jefe_de_Redes' &&
    user.role !== 'Profesor'
  ) {
    return <Navigate to="/" replace />;
  }

  return children;
}
