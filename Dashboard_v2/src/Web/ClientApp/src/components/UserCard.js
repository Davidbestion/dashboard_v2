import React from 'react';
import { Badge } from 'reactstrap';

// ── Mapeo de enums (deben coincidir con los valores del backend) ─────────────
const SCIENTIFIC_CATEGORY = {
  0: null,
  1: 'Licenciado',
  2: 'Master',
  3: 'Doctor',
};

const TEACHING_CATEGORY = {
  0: null,
  1: 'Profesor Titular',
  2: 'Profesor Auxiliar',
  3: 'Profesor Asistente',
  4: 'Instructor',
};

const INVESTIGATION_CATEGORY = {
  0: null,
  1: 'Investigador Titular',
  2: 'Investigador Auxiliar',
  3: 'Investigador Agregado',
  4: 'Investigador Asociado',
};

/**
 * Ficha de usuario reutilizable.
 *
 * Props:
 *  - user: UserWithRolesDto (id, userName, userLastName1, userLastName2?,
 *          email, scientificCategory, teachingCategory, investigationCategory, isTrained)
 *  - isSelected: bool  — actualmente seleccionado (en el Set de trabajo)
 *  - isOriginal: bool  — estaba en la BD al abrir el modal
 *  - isCreator: bool   — borde dorado, indica que es el creador del grupo
 *  - onClick: fn       — si se provee, la ficha es interactiva (clickable)
 *  - disabled: bool    — deshabilita la interacción
 *
 * Estados visuales (borde / fondo):
 *  isOriginal &&  isSelected  → verde   — miembro existente (sin cambios)
 * !isOriginal &&  isSelected  → azul    — recién agregado esta sesión
 *  isOriginal && !isSelected  → rojo    — se va a eliminar
 * !isOriginal && !isSelected  → gris    — no es miembro
 *  isCreator sobreescribe el borde con ámbar, independientemente del estado.
 */
export default function UserCard({ user, isSelected, isOriginal, isCreator, onClick, disabled }) {
  const fullName = [user.userName, user.userLastName1, user.userLastName2]
    .filter(Boolean)
    .join(' ');

  const sciLabel = SCIENTIFIC_CATEGORY[user.scientificCategory];
  const teachLabel = TEACHING_CATEGORY[user.teachingCategory];
  const invLabel = INVESTIGATION_CATEGORY[user.investigationCategory];

  // ── Colores según estado ─────────────────────────────────────────────────
  // El creador siempre tiene borde ámbar (sobre cualquier otro estado).
  // Para el resto: verde=existente, azul=nuevo, rojo=eliminado, gris=sin cambio.
  const borderColor = isCreator
    ? '#d97706'
    : isOriginal && isSelected
      ? '#198754'   // verde — miembro existente, sin cambios
      : !isOriginal && isSelected
        ? '#0d6efd' // azul  — recién agregado
        : isOriginal && !isSelected
          ? '#dc3545' // rojo  — será eliminado
          : '#dee2e6'; // gris  — no miembro

  const hoverBorderColor = isCreator
    ? '#b45309'
    : isOriginal && isSelected
      ? '#146c43'
      : !isOriginal && isSelected
        ? '#0a58ca'
        : isOriginal && !isSelected
          ? '#b02a37'
          : '#adb5bd';

  const bgColor = isCreator
    ? (isSelected || isOriginal ? '#fffbeb' : '#fff')
    : isOriginal && isSelected
      ? '#d1e7dd'
      : !isOriginal && isSelected
        ? '#e8f0fe'
        : isOriginal && !isSelected
          ? '#f8d7da'
          : '#fff';

  const cardStyle = {
    border: `2px solid ${borderColor}`,
    borderRadius: 8,
    padding: '10px 12px',
    backgroundColor: bgColor,
    cursor: onClick && !disabled ? 'pointer' : 'default',
    userSelect: 'none',
    transition: 'border-color 0.15s, background-color 0.15s',
    position: 'relative',
    minWidth: 0,
  };

  return (
    <div
      style={cardStyle}
      onClick={!disabled && onClick ? () => onClick(user.id) : undefined}
      onMouseEnter={onClick && !disabled
        ? e => { e.currentTarget.style.borderColor = hoverBorderColor; }
        : undefined}
      onMouseLeave={onClick && !disabled
        ? e => { e.currentTarget.style.borderColor = borderColor; }
        : undefined}
      title={onClick && !disabled
        ? isOriginal && isSelected ? 'Clic para quitar del grupo'
          : !isOriginal && isSelected ? 'Clic para deshacer agregar'
          : isOriginal && !isSelected ? 'Clic para mantener en el grupo'
          : 'Clic para agregar al grupo'
        : undefined}
    >
      {/* Indicadores de estado */}
      <div style={{ position: 'absolute', top: 4, right: 6, display: 'flex', gap: 4, alignItems: 'center' }}>
        {isOriginal && isSelected && (
          <span style={{ fontSize: '0.65rem', color: '#198754', fontWeight: 600 }}>✓ Miembro</span>
        )}
        {!isOriginal && isSelected && (
          <span style={{ fontSize: '0.65rem', color: '#0d6efd', fontWeight: 600 }}>+ Nuevo</span>
        )}
        {isOriginal && !isSelected && (
          <span style={{ fontSize: '0.65rem', color: '#dc3545', fontWeight: 600 }}>✕ Eliminar</span>
        )}
        {isCreator && (
          <span style={{ fontSize: '0.65rem', color: '#d97706', fontWeight: 600 }}>★ Creador</span>
        )}
      </div>

      {/* Nombre completo */}
      <div className="fw-semibold text-truncate mb-1" style={{ fontSize: '0.9rem', paddingRight: 60 }}>
        {fullName}
      </div>

      {/* Correo */}
      <div className="text-muted text-truncate mb-2" style={{ fontSize: '0.75rem' }}>
        <i className="bi bi-envelope me-1" />
        {user.email}
      </div>

      {/* Badges de categorías */}
      <div className="d-flex flex-wrap gap-1">
        {user.isTrained && (
          <Badge color="secondary" pill style={{ fontSize: '0.65rem' }}>Adiestrado</Badge>
        )}
        {sciLabel && (
          <Badge color="success" pill style={{ fontSize: '0.65rem' }}>{sciLabel}</Badge>
        )}
        {teachLabel && (
          <Badge color="info" pill style={{ fontSize: '0.65rem', color: '#000' }}>{teachLabel}</Badge>
        )}
        {invLabel && (
          <Badge color="primary" pill style={{ fontSize: '0.65rem' }}>{invLabel}</Badge>
        )}
        {!user.isTrained && !sciLabel && !teachLabel && !invLabel && (
          <span style={{ fontSize: '0.65rem', color: '#aaa', fontStyle: 'italic' }}>Sin categorías asignadas</span>
        )}
      </div>
    </div>
  );
}
