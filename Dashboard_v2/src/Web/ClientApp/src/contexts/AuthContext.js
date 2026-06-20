import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';

/**
 * Autenticación basada en cookies HttpOnly.
 * El token JWT vive en una cookie HttpOnly+Secure+SameSite=Strict configurada por el servidor.
 * JavaScript nunca puede leer ese token, lo que elimina el vector de ataque XSS.
 * El navegador envía la cookie automáticamente con credentials: 'include'.
 */
const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  /**
   * Refresca el usuario autenticado a partir de la cookie HttpOnly actual.
   * Además de actualizar el estado global, devuelve el payload recibido para evitar
   * decisiones basadas en un estado React todavía no sincronizado en el mismo tick.
   */
  const fetchCurrentUser = useCallback(async () => {
    let currentUser = null;

    try {
      // credentials: 'include' asegura que el browser envíe la cookie HttpOnly al servidor
      const response = await fetch('/api/Auth/me', {
        credentials: 'include',
      });
      if (response.ok) {
        currentUser = await response.json();
        setUser(currentUser);
      } else {
        setUser(null);
      }
    } catch {
      setUser(null);
    } finally {
      setLoading(false);
    }

    return currentUser;
  }, []);

  useEffect(() => {
    fetchCurrentUser();
  }, [fetchCurrentUser]);

  const login = async (email, password, selectedRole = null, selectedAreaId = null) => {
    const body = { email, password };
    if (selectedRole) body.selectedRole = selectedRole;
    if (selectedAreaId) body.selectedAreaId = selectedAreaId;

    const response = await fetch('/api/Auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      const data = await response.json().catch(() => ({}));
      const errors = data?.errors ?? ['Credenciales inválidas.'];
      throw new Error(Array.isArray(errors) ? errors.join(' ') : errors);
    }

    const data = await response.json();

    // El servidor puede pedir que el usuario elija un rol o un área antes de completar el login.
    // Devolvemos el objeto para que la UI muestre el selector correspondiente.
    if (data.requiresRoleSelection || data.requiresAreaSelection) {
      return data;
    }

    // Login completo: el servidor ya colocó la cookie.
    await fetchCurrentUser();
    return null;
  };

  const logout = async () => {
    // Le pedimos al servidor que limpie la cookie
    await fetch('/api/Auth/logout', {
      method: 'POST',
      credentials: 'include',
    }).catch(() => {});
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, loading, login, logout, refetch: fetchCurrentUser }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
