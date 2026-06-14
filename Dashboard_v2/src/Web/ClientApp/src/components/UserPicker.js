import React, { useState } from 'react';
import { Input } from 'reactstrap';
import SelectionProfileCard from './SelectionProfileCard';

/**
 * Selector de usuario único con tarjetas interactivas.
 *
 * Props:
 *  - users: UserWithRolesDto[]  — lista completa de usuarios disponibles
 *  - value: string | null       — id del usuario actualmente seleccionado
 *  - onChange: fn(id)           — llamada con el nuevo id (o null si se deselecciona)
 *  - placeholder: string        — texto del filtro (opcional)
 *  - maxHeight: string          — altura máxima del grid con scroll (default "300px")
 */
export default function UserPicker({ users, value, onChange, placeholder, maxHeight = '300px' }) {
  const [filter, setFilter] = useState('');

  const filtered = (users ?? []).filter(u => {
    if (!filter.trim()) return true;
    const q = filter.toLowerCase();
    const fullName = [u.userName, u.userLastName1, u.userLastName2]
      .filter(Boolean).join(' ').toLowerCase();
    return fullName.includes(q) || (u.email ?? '').toLowerCase().includes(q);
  });

  function handleClick(user) {
    onChange(user.id === value ? null : user.id);
  }

  return (
    <>
      <Input
        bsSize="sm"
        placeholder={placeholder ?? 'Filtrar por nombre o correo...'}
        value={filter}
        onChange={e => setFilter(e.target.value)}
        className="mb-2"
      />
      {users.length === 0 ? (
        <p className="text-muted small mb-0">Cargando usuarios…</p>
      ) : filtered.length === 0 ? (
        <p className="text-muted small mb-0">No hay usuarios que coincidan.</p>
      ) : (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))',
            gap: '0.6rem',
            maxHeight,
            overflowY: 'auto',
            paddingRight: '4px',
          }}
        >
          {filtered.map(u => (
            <SelectionProfileCard
              key={u.id}
              person={u}
              variant={u.id === value ? 'selected' : 'neutral'}
              onClick={handleClick}
              clickTitle={u.id === value ? 'Clic para deseleccionar' : 'Clic para seleccionar'}
            />
          ))}
        </div>
      )}
    </>
  );
}
