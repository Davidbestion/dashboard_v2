import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

/**
 * Protege rutas accesibles por Superuser O Jefe_de_Redes.
 * Si no está autenticado → /login.
 * Si está autenticado pero no tiene ninguno de esos roles → / (inicio).
 */
export default function JefeRedesOrAdminRoute({ children }) {
  const { user, loading } = useAuth();

  if (loading) return null;

  if (!user) return <Navigate to="/login" replace />;

  if (user.role !== 'Superuser' && user.role !== 'Jefe_de_Redes') {
    return <Navigate to="/" replace />;
  }

  return children;
}
