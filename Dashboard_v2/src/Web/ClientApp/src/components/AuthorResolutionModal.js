import React, { useState } from 'react';
import {
  Modal, ModalHeader, ModalBody, ModalFooter,
  Button, Badge, Alert,
} from 'reactstrap';
import SelectionProfileCard from './SelectionProfileCard';

/**
 * Modal de revisión de autores externos.
 *
 * Recibe una lista de items provenientes de la resolución backend:
 *   [ { externalName: "García, Juan", match: { id, name, lastName, firstName, linkedUser? } | null } ]
 *
 * Para cada item:
 *  - Si hay coincidencia (match != null): muestra el nombre externo + la tarjeta del autor en
 *    el sistema + (si está vinculado) la tarjeta del usuario. El usuario puede aceptar la
 *    coincidencia o rechazarla (el nombre se tratará como nuevo autor externo).
 *  - Si no hay coincidencia (match == null): solo muestra el nombre externo; se añadirá como
 *    autor nuevo automáticamente, o el usuario puede indicar que ese nombre le corresponde a él
 *    mediante el botón "Soy yo".
 *
 * Al confirmar, llama a `onConfirm(coauthorTags)` con la lista de entradas listas para el
 * CoauthorPicker:
 *   - Aceptadas: { id, name, type: 'author', linkedUser? }
 *   - Rechazadas / sin coincidencia: { id: null, name: externalName, type: 'new' }
 *   - "Soy yo": excluidas (el usuario registrante siempre se añade en el backend)
 *
 * @param {string|null} currentUserId  ID del usuario autenticado actualmente.
 */
export default function AuthorResolutionModal({ isOpen, items, onConfirm, onCancel, currentUserId, currentUserHasLinkedAuthor }) {
  // decisions[externalName] = 'accept' | 'reject' | 'none' | 'self'
  const [decisions, setDecisions] = useState(() => {
    const init = {};
    (items ?? []).forEach(item => {
      init[item.externalName] = item.match ? 'accept' : 'none';
    });
    return init;
  });

  function toggle(externalName, value) {
    setDecisions(prev => ({ ...prev, [externalName]: value }));
  }

  function handleConfirm() {
    const tags = (items ?? []).flatMap(item => {
      const decision = decisions[item.externalName];
      // "Soy yo" → omit entirely; the backend always adds the current user as author
      if (decision === 'self') return [];
      if (item.match && decision === 'accept') {
        return [{
          id: item.match.id,
          name: item.match.name,
          type: 'author',
          linkedUser: item.match.linkedUser ?? null,
        }];
      }
      // rejected or no match → new author entry
      return [{ id: null, name: item.externalName, type: 'new', linkedUser: null }];
    });
    onConfirm(tags);
  }

  const matchCount = (items ?? []).filter(i => i.match).length;

  // ¿Está el usuario actual ya identificado entre los autores resueltos?
  const userAlreadyIdentified = currentUserId && (items ?? []).some(
    item => item.match?.linkedUser?.id === currentUserId
  );

  return (
    <Modal isOpen={isOpen} toggle={onCancel} size="lg" scrollable>
      <ModalHeader toggle={onCancel}>
        Revisión de autores externos
      </ModalHeader>

      <ModalBody>
        {/* Aviso: el usuario no aparece entre los autores importados */}
        {!userAlreadyIdentified && currentUserId && currentUserHasLinkedAuthor && (
          <Alert color="warning" className="mb-3" style={{ fontSize: '0.88rem' }}>
            <i className="bi bi-exclamation-triangle-fill me-2" />
            <strong>Tu nombre no aparece entre los autores reconocidos por CrossRef.</strong>
            {' '}Serás añadido igualmente como autor al guardar la publicación.
          </Alert>
        )}
        {!userAlreadyIdentified && currentUserId && !currentUserHasLinkedAuthor && (
          <Alert color="warning" className="mb-3" style={{ fontSize: '0.88rem' }}>
            <i className="bi bi-exclamation-triangle-fill me-2" />
            <strong>¿Eres uno de los autores de esta publicación?</strong>
            {' '}Tu nombre no fue reconocido automáticamente en la lista. Revisa si alguno
            de los nombres de abajo corresponde a ti (puede estar abreviado o sin tildes):
            si tiene coincidencia en el sistema, <strong>acéptala</strong>; si no tiene
            coincidencia, usa el botón <strong>"Soy yo"</strong>. En cualquier caso serás
            añadido como autor al guardar.
          </Alert>
        )}

        <p className="text-muted mb-3" style={{ fontSize: '0.9rem' }}>
          Se encontraron <strong>{matchCount}</strong> coincidencia{matchCount !== 1 ? 's' : ''} en el
          sistema para los autores importados. Revisa cada una y decide si la coincidencia detectada
          corresponde realmente al mismo autor o no.
        </p>

        <div className="d-flex flex-column gap-4">
          {(items ?? []).map(item => (
            <AuthorResolutionRow
              key={item.externalName}
              item={item}
              decision={decisions[item.externalName] ?? 'none'}
              onToggle={(val) => toggle(item.externalName, val)}
              currentUserId={currentUserId}
              currentUserHasLinkedAuthor={currentUserHasLinkedAuthor}
            />
          ))}
        </div>
      </ModalBody>

      <ModalFooter>
        <Button color="secondary" outline onClick={onCancel}>Cancelar</Button>
        <Button color="primary" onClick={handleConfirm}>
          Confirmar y añadir autores
        </Button>
      </ModalFooter>
    </Modal>
  );
}

// ── Row component ────────────────────────────────────────────────────────────

function AuthorResolutionRow({ item, decision, onToggle, currentUserId, currentUserHasLinkedAuthor }) {
  const hasMatch = Boolean(item.match);
  // This row already links to the current user (auto-accepted)
  const isCurrentUser = currentUserId && item.match?.linkedUser?.id === currentUserId;

  return (
    <div
      style={{
        border: `1px solid ${decision === 'self' ? '#ffc107' : '#dee2e6'}`,
        borderRadius: 8,
        padding: '1rem',
        backgroundColor: decision === 'self' ? '#fffbf0' : (hasMatch ? '#f8f9fa' : '#fff'),
      }}
    >
      {/* External name label */}
      <div className="d-flex align-items-center gap-2 mb-2 flex-wrap">
        <Badge color="secondary" pill style={{ fontSize: '0.75rem' }}>Importado</Badge>
        <span style={{ fontFamily: 'monospace', fontWeight: 600, fontSize: '0.95rem' }}>
          {item.externalName}
        </span>
        {decision === 'self' && (
          <Badge color="warning" text="dark" pill style={{ fontSize: '0.72rem' }}>
            <i className="bi bi-person-fill me-1" />
            Eres tú — no se añadirá como coautor separado
          </Badge>
        )}
        {isCurrentUser && decision !== 'self' && (
          <Badge color="info" pill style={{ fontSize: '0.72rem' }}>
            <i className="bi bi-person-check me-1" />
            Identificado como tú
          </Badge>
        )}
      </div>

      {!hasMatch && decision !== 'self' && (
        <p className="text-muted mb-2" style={{ fontSize: '0.85rem' }}>
          No se encontró ningún autor con este nombre en el sistema.
          Se registrará como autor nuevo.
        </p>
      )}

      {/* "Soy yo" button — only for UNMATCHED items and only when the user has NO linked
          Author yet. If the user already has an Author entity, the backend will use it
          automatically, so the button is redundant. */}
      {!hasMatch && !isCurrentUser && currentUserId && !currentUserHasLinkedAuthor && (
        <div className="mb-2">
          {decision === 'self' ? (
            <Button
              size="sm"
              color="warning"
              outline
              onClick={() => onToggle('none')}
            >
              <i className="bi bi-arrow-counterclockwise me-1" />
              Deshacer "Soy yo"
            </Button>
          ) : (
            <Button
              size="sm"
              color="outline-warning"
              onClick={() => onToggle('self')}
              title="Indica que este nombre te corresponde a ti. No se añadirá como coautor separado."
            >
              <i className="bi bi-person-fill me-1" />
              Soy yo
            </Button>
          )}
        </div>
      )}

      {hasMatch && decision !== 'self' && (
        <>
          <p className="mb-2" style={{ fontSize: '0.85rem' }}>
            <i className="bi bi-search me-1 text-primary" />
            Posible coincidencia encontrada en el sistema:
          </p>

          <div
            style={{
              display: 'grid',
              gridTemplateColumns: item.match.linkedUser
                ? 'repeat(auto-fill, minmax(220px, 1fr))'
                : '1fr',
              gap: '0.75rem',
              maxWidth: item.match.linkedUser ? '100%' : 340,
              marginBottom: '0.75rem',
            }}
          >
            {/* Author card (without linked user) */}
            <div>
              <p className="text-muted mb-1" style={{ fontSize: '0.75rem', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                Autor en el sistema
              </p>
              <AuthorCard match={item.match} />
            </div>

            {/* Linked user card */}
            {item.match.linkedUser && (
              <div>
                <p className="text-muted mb-1" style={{ fontSize: '0.75rem', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                  Usuario vinculado
                </p>
                <SelectionProfileCard
                  person={{ ...item.match.linkedUser, type: 'user', linkedUser: null }}
                  variant="neutral"
                  onClick={null}
                />
              </div>
            )}
          </div>

          {/* Accept / Reject buttons */}
          <div className="d-flex gap-2 flex-wrap">
            <Button
              size="sm"
              color={decision === 'accept' ? 'success' : 'outline-success'}
              onClick={() => onToggle('accept')}
            >
              <i className="bi bi-check-circle me-1" />
              {decision === 'accept' ? 'Coincidencia aceptada' : 'Aceptar coincidencia'}
            </Button>
            <Button
              size="sm"
              color={decision === 'reject' ? 'danger' : 'outline-danger'}
              onClick={() => onToggle('reject')}
            >
              <i className="bi bi-x-circle me-1" />
              {decision === 'reject' ? 'Rechazada — se añade como nuevo' : 'Rechazar (añadir como nuevo)'}
            </Button>
          </div>
        </>
      )}
    </div>
  );
}

// ── Simple author card (no SelectionProfileCard dependency needed) ─────────────

function AuthorCard({ match }) {
  return (
    <div
      style={{
        border: '1px solid #0d6efd',
        borderRadius: 6,
        padding: '0.6rem 0.8rem',
        backgroundColor: '#e8f0fe',
        fontSize: '0.9rem',
      }}
    >
      <div style={{ fontWeight: 600 }}>{match.lastName}</div>
      {match.firstName && (
        <div className="text-muted" style={{ fontSize: '0.82rem' }}>{match.firstName}</div>
      )}
      {match.linkedUser && (
        <Badge color="success" className="mt-1" style={{ fontSize: '0.7rem' }}>
          <i className="bi bi-person-check me-1" />
          Usuario registrado
        </Badge>
      )}
    </div>
  );
}
