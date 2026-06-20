import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

/**
 * Protege rutas accesibles por Profesor o Superuser.
 * Si el usuario no está autenticado → /login.
 * Si está autenticado pero no tiene ninguno de esos roles → / (inicio).
 */
export default function ProfesorOrAdminRoute({ children }) {
  const { user, loading } = useAuth();

  if (loading) return null;

  if (!user) return <Navigate to="/login" replace />;

  if (user.role !== 'Profesor' && user.role !== 'Superuser') {
    return <Navigate to="/" replace />;
  }

  return children;
}
