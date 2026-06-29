import React, { useRef, useState } from 'react';
import { Button, Spinner, Alert, Input } from 'reactstrap';

/**
 * Widget de certificado/evidencia reutilizable.
 *
 * Comportamiento:
 * - Sin certificado + canManage : muestra selector de archivo para subir.
 * - Con certificado + canView   : muestra botón "Ver / Descargar" (URL presignada en nueva pestaña).
 * - Con certificado + canManage : además muestra "Reemplazar" (nuevo upload) y "Quitar" (pone fileId=null).
 *
 * La subida se realiza de inmediato al elegir el archivo (multipart POST /api/FileStorage).
 * Al reemplazar se usa el mismo endpoint POST (sube nuevo archivo, ID diferente).
 * Al quitar solo se actualiza el estado del formulario padre — el archivo en MinIO
 * queda huérfano hasta que se haga una limpieza posterior.
 *
 * Props:
 *   fileId         {number|null}  — ID actual del archivo. null = sin certificado.
 *   onFileIdChange {function}     — Llamado con el nuevo fileId (number) o null al quitar.
 *   canManage      {boolean}      — El usuario puede subir/reemplazar/quitar.
 *   canView        {boolean}      — El usuario puede ver/descargar.
 *   disabled       {boolean}      — Deshabilita todas las acciones (p.ej. mientras el form guarda).
 */
export default function CertificateUpload({
  fileId,
  onFileIdChange,
  onUploadingChange,  // (isUploading: boolean) => void — optional, fired when upload starts/ends
  onUploadError,      // (message: string) => void   — optional, fired on failure (message) or success ('')
  canManage = false,
  canView = false,
  disabled = false,
}) {
  const fileInputRef = useRef(null);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState('');
  const [viewLoading, setViewLoading] = useState(false);

  const hasFile = fileId != null;

  // ── Upload ────────────────────────────────────────────────────────────────

  async function handleFileChange(e) {
    const file = e.target.files?.[0];
    if (!file) return;

    // Reset input so the same file can be re-selected after an error
    e.target.value = '';

    setUploading(true);
    setUploadError('');
    onUploadingChange?.(true);
    onUploadError?.('');
    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await fetch('/api/FileStorage', {
        method: 'POST',
        credentials: 'include',
        body: formData,
      });

      const data = await response.json().catch(() => null);
      if (!response.ok) {
        if (response.status === 503) {
          throw new Error('El servicio de archivos no está disponible en este momento. Inténtelo de nuevo más tarde.');
        }
        const msg = data?.errors
          ? (Array.isArray(data.errors) ? data.errors : Object.values(data.errors).flat()).join(' ')
          : (data?.title ?? `Error ${response.status}`);
        throw new Error(msg);
      }

      onFileIdChange(data.id);
    } catch (err) {
      setUploadError(err.message);
      onUploadError?.(err.message);
    } finally {
      setUploading(false);
      onUploadingChange?.(false);
    }
  }

  // ── View / Download ───────────────────────────────────────────────────────

  async function handleView() {
    if (!fileId) return;
    setViewLoading(true);
    setUploadError('');
    try {
      const response = await fetch(`/api/FileStorage/${fileId}/url`, {
        credentials: 'include',
      });
      if (!response.ok) {
        if (response.status === 503) {
          throw new Error('El servicio de archivos no está disponible en este momento. Inténtelo de nuevo más tarde.');
        }
        const data = await response.json().catch(() => null);
        throw new Error(data?.title ?? `Error ${response.status}`);
      }
      const url = await response.json();
      window.open(url, '_blank', 'noopener,noreferrer');
    } catch (err) {
      setUploadError(err.message);
    } finally {
      setViewLoading(false);
    }
  }

  // ── Remove (only clears local state) ─────────────────────────────────────

  function handleRemove() {
    setUploadError('');
    onUploadError?.('');
    onFileIdChange(null);
  }

  // ── Render ────────────────────────────────────────────────────────────────
  // Render logic:
  // - No file + canManage → show upload button
  // - No file + !canManage → show "no file" message
  // - Has file → show view/download controls
  // - Has file + canManage → also show replace/remove controls

  const inputDisabled = disabled || uploading;

  return (
    <div className="certificate-upload">
      {uploadError && (
        <Alert color="danger" className="py-1 px-2 small mb-2" toggle={() => setUploadError('')}>
          {uploadError}
        </Alert>
      )}

      {!hasFile && canManage && (
        <div className="d-flex align-items-center gap-2">
          <Button
            type="button"
            color="outline-secondary"
            size="sm"
            disabled={inputDisabled}
            onClick={() => fileInputRef.current?.click()}
          >
            {uploading ? <Spinner size="sm" className="me-1" /> : <i className="bi bi-paperclip me-1" />}
            {uploading ? 'Subiendo…' : 'Adjuntar certificado'}
          </Button>
          <Input
            innerRef={fileInputRef}
            type="file"
            className="d-none"
            accept=".pdf,.doc,.docx,.odt,.txt,.rtf"
            onChange={handleFileChange}
          />
          <small className="text-muted">Formatos: PDF, Word, ODT, TXT, RTF</small>
        </div>
      )}

      {!hasFile && !canManage && (
        <span className="text-muted small"><i className="bi bi-slash-circle me-1" />Sin certificado</span>
      )}

      {hasFile && (
        <div className="d-flex align-items-center flex-wrap gap-2">
          <i className="bi bi-file-earmark-check text-success" />
          <span className="text-success small fw-semibold">Certificado adjunto</span>

          {canView && (
            <Button
              type="button"
              color="outline-primary"
              size="sm"
              disabled={disabled || viewLoading}
              onClick={handleView}
            >
              {viewLoading
                ? <Spinner size="sm" />
                : <><i className="bi bi-eye me-1" />Ver / Descargar</>}
            </Button>
          )}

          {canManage && (
            <>
              <Button
                type="button"
                color="outline-secondary"
                size="sm"
                disabled={inputDisabled}
                onClick={() => fileInputRef.current?.click()}
              >
                {uploading ? <Spinner size="sm" className="me-1" /> : <i className="bi bi-arrow-repeat me-1" />}
                {uploading ? 'Subiendo…' : 'Reemplazar'}
              </Button>
              <Input
                innerRef={fileInputRef}
                type="file"
                className="d-none"
                accept=".pdf,.doc,.docx,.odt,.txt,.rtf"
                onChange={handleFileChange}
              />

              <Button
                type="button"
                color="outline-danger"
                size="sm"
                disabled={inputDisabled}
                onClick={handleRemove}
              >
                <i className="bi bi-x-lg me-1" />Quitar
              </Button>
            </>
          )}
        </div>
      )}
    </div>
  );
}

/**
 * Botón compacto de descarga/vista para usar en filas de tabla.
 * Solo muestra algo cuando fileId != null.
 */
export function CertificateViewButton({ fileId, disabled = false }) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  if (!fileId) return null;

  async function handleClick() {
    setLoading(true);
    setError('');
    try {
      const response = await fetch(`/api/FileStorage/${fileId}/url`, {
        credentials: 'include',
      });
      if (!response.ok) {
        if (response.status === 503) {
          throw new Error('El servicio de archivos no está disponible en este momento. Inténtelo de nuevo más tarde.');
        }
        const data = await response.json().catch(() => null);
        throw new Error(data?.title ?? `Error ${response.status}`);
      }
      const url = await response.json();
      window.open(url, '_blank', 'noopener,noreferrer');
    } catch (err) {
      setError(err.message);
      setTimeout(() => setError(''), 4000);
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <Button
        type="button"
        color="outline-info"
        size="sm"
        disabled={disabled || loading}
        onClick={handleClick}
        title="Ver / Descargar certificado"
      >
        {loading ? <Spinner size="sm" /> : <i className="bi bi-file-earmark-check" />}
      </Button>
      {error && <span className="text-danger small ms-1">{error}</span>}
    </>
  );
}
