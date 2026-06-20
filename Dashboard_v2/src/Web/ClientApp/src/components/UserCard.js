import React from 'react';
import SelectionProfileCard from './SelectionProfileCard';

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
  const variant = isOriginal && isSelected
    ? 'existing'
    : !isOriginal && isSelected
      ? 'added'
      : isOriginal && !isSelected
        ? 'removed'
        : 'neutral';

  return (
    <SelectionProfileCard
      person={user}
      variant={variant}
      isCreator={isCreator}
      disabled={disabled}
      onClick={onClick ? (selectedUser) => onClick(selectedUser.id) : undefined}
      clickTitle={onClick && !disabled
        ? isOriginal && isSelected ? 'Clic para quitar del grupo'
          : !isOriginal && isSelected ? 'Clic para deshacer agregar'
          : isOriginal && !isSelected ? 'Clic para mantener en el grupo'
          : 'Clic para agregar al grupo'
        : undefined}
    />
  );
}
