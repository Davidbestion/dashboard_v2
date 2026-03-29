import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

/**
 * Protege rutas que requieren el rol Profesor.
 * Si el usuario no está autenticado → /login.
 * Si está autenticado pero no tiene el rol → / (inicio).
 */
export default function ProfesorRoute({ children }) {
  const { user, loading } = useAuth();

  if (loading) return null;

  if (!user) return <Navigate to="/login" replace />;

  if (user.role !== 'Profesor') return <Navigate to="/" replace />;

  return children;
}
