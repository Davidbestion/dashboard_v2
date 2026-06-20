import React from 'react';
import { Badge } from 'reactstrap';

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

const VARIANT_STYLES = {
  neutral: {
    borderColor: '#dee2e6',
    hoverBorderColor: '#adb5bd',
    backgroundColor: '#fff',
  },
  selected: {
    borderColor: '#0d6efd',
    hoverBorderColor: '#0a58ca',
    backgroundColor: '#e8f0fe',
    statusLabel: 'Seleccionado',
    statusColor: '#0d6efd',
  },
  existing: {
    borderColor: '#198754',
    hoverBorderColor: '#146c43',
    backgroundColor: '#d1e7dd',
    statusLabel: 'Miembro',
    statusColor: '#198754',
  },
  added: {
    borderColor: '#0d6efd',
    hoverBorderColor: '#0a58ca',
    backgroundColor: '#e8f0fe',
    statusLabel: '+ Nuevo',
    statusColor: '#0d6efd',
  },
  removed: {
    borderColor: '#dc3545',
    hoverBorderColor: '#b02a37',
    backgroundColor: '#f8d7da',
    statusLabel: 'Eliminar',
    statusColor: '#dc3545',
  },
};

/**
 * Extrae el snapshot de usuario asociado a la tarjeta.
 * Si el registro representa directamente a un usuario, retorna ese mismo objeto.
 * Si representa a un autor vinculado, usa la información enriquecida de `linkedUser`.
 */
function resolveLinkedUser(person) {
  if (!person) return null;
  if (person.linkedUser) return person.linkedUser;
  if (person.userName && person.userLastName1) return person;
  return null;
}

/**
 * Calcula el nombre visible de la tarjeta.
 * Cuando existe un usuario asociado se prioriza el nombre completo institucional;
 * en caso contrario se usa el nombre libre del autor.
 */
function resolveDisplayName(person, linkedUser) {
  if (linkedUser) {
    return [linkedUser.userName, linkedUser.userLastName1, linkedUser.userLastName2]
      .filter(Boolean)
      .join(' ');
  }

  return person.name ?? 'Registro sin nombre';
}

/**
 * Tarjeta reutilizable para seleccionar usuarios y autores.
 * Renderiza una vista rica de usuario cuando existe una cuenta vinculada y
 * cae a una tarjeta genérica de autor cuando el registro es puramente bibliográfico.
 */
export default function SelectionProfileCard({
  person,
  variant = 'neutral',
  isCreator = false,
  disabled = false,
  onClick,
  clickTitle,
}) {
  const linkedUser = resolveLinkedUser(person);
  const displayName = resolveDisplayName(person, linkedUser);
  const style = VARIANT_STYLES[variant] ?? VARIANT_STYLES.neutral;

  const scientificCategory = linkedUser ? SCIENTIFIC_CATEGORY[linkedUser.scientificCategory] : null;
  const teachingCategory = linkedUser ? TEACHING_CATEGORY[linkedUser.teachingCategory] : null;
  const investigationCategory = linkedUser ? INVESTIGATION_CATEGORY[linkedUser.investigationCategory] : null;

  const borderColor = isCreator ? '#d97706' : style.borderColor;
  const hoverBorderColor = isCreator ? '#b45309' : style.hoverBorderColor;
  const backgroundColor = isCreator && (variant === 'selected' || variant === 'existing' || variant === 'added')
    ? '#fffbeb'
    : style.backgroundColor;

  const cardStyle = {
    border: `2px solid ${borderColor}`,
    borderRadius: 10,
    padding: '12px 14px',
    backgroundColor,
    cursor: onClick && !disabled ? 'pointer' : 'default',
    userSelect: 'none',
    transition: 'border-color 0.15s, background-color 0.15s',
    position: 'relative',
    minWidth: 0,
  };

  return (
    <div
      style={cardStyle}
      onClick={!disabled && onClick ? () => onClick(person) : undefined}
      onMouseEnter={onClick && !disabled
        ? (event) => { event.currentTarget.style.borderColor = hoverBorderColor; }
        : undefined}
      onMouseLeave={onClick && !disabled
        ? (event) => { event.currentTarget.style.borderColor = borderColor; }
        : undefined}
      title={clickTitle}
    >
      <div
        style={{
          position: 'absolute',
          top: 6,
          right: 8,
          display: 'flex',
          gap: 6,
          alignItems: 'center',
          flexWrap: 'wrap',
          justifyContent: 'flex-end',
          maxWidth: '58%',
        }}
      >
        {style.statusLabel && (
          <span style={{ fontSize: '0.65rem', color: style.statusColor, fontWeight: 700 }}>
            {style.statusLabel}
          </span>
        )}
        {isCreator && (
          <span style={{ fontSize: '0.65rem', color: '#d97706', fontWeight: 700 }}>
            ★ Creador
          </span>
        )}
        {!linkedUser && (
          <span style={{ fontSize: '0.65rem', color: '#6c757d', fontWeight: 700 }}>
            Autor
          </span>
        )}
      </div>

      <div className="fw-semibold text-truncate mb-1" style={{ fontSize: '0.95rem', paddingRight: 72 }}>
        {displayName}
      </div>

      {linkedUser ? (
        <>
          <div className="text-muted text-truncate mb-2" style={{ fontSize: '0.75rem' }}>
            <i className="bi bi-envelope me-1" />
            {linkedUser.email}
          </div>
          <div className="mb-2" style={{ fontSize: '0.73rem' }}>
            <div className="text-truncate text-muted">
              <i className="bi bi-diagram-3 me-1" />
              {linkedUser.areaNombre ?? 'Sin área asignada'}
            </div>
            <div className="text-truncate text-muted">
              <i className="bi bi-building me-1" />
              {linkedUser.universidadNombre ?? 'Sin universidad asociada'}
            </div>
          </div>
          <div className="d-flex flex-wrap gap-1">
            {linkedUser.isTrained && (
              <Badge color="secondary" pill style={{ fontSize: '0.65rem' }}>Adiestrado</Badge>
            )}
            {scientificCategory && (
              <Badge color="success" pill style={{ fontSize: '0.65rem' }}>{scientificCategory}</Badge>
            )}
            {teachingCategory && (
              <Badge color="info" pill style={{ fontSize: '0.65rem', color: '#000' }}>{teachingCategory}</Badge>
            )}
            {investigationCategory && (
              <Badge color="primary" pill style={{ fontSize: '0.65rem' }}>{investigationCategory}</Badge>
            )}
            {!linkedUser.isTrained && !scientificCategory && !teachingCategory && !investigationCategory && (
              <span style={{ fontSize: '0.65rem', color: '#6c757d', fontStyle: 'italic' }}>
                Sin categorías asignadas
              </span>
            )}
          </div>
        </>
      ) : (
        <>
          <div className="text-muted mb-2" style={{ fontSize: '0.78rem' }}>
            <i className="bi bi-person-badge me-1" />
            Autor sin cuenta vinculada
          </div>
          <div className="d-flex flex-wrap gap-1">
            <Badge color="secondary" pill style={{ fontSize: '0.65rem' }}>
              Registro bibliográfico
            </Badge>
          </div>
        </>
      )}
    </div>
  );
}
