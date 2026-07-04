export async function apiFetch(url, options = {}) {
  const response = await fetch(url, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...(options.headers ?? {}) },
    ...options,
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    if (response.status >= 500) {
      throw new Error('Error interno del servidor. Por favor intente de nuevo o contacte al administrador.');
    }
    let errors = data?.errors;
    if (!errors) {
      errors = data?.title ? [data.title] : ['Ocurrió un error inesperado.'];
    } else if (!Array.isArray(errors) && typeof errors === 'object') {
      errors = Object.values(errors).flat();
    }
    throw new Error(errors.join('\n'));
  }
  return data;
}
