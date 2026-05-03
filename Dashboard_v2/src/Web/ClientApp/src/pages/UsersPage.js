import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Table, Badge, Button,
  Spinner, Alert,
  Input, FormGroup, Label,
  Modal, ModalHeader, ModalBody, ModalFooter,
} from 'reactstrap';

async function apiFetch(url, options = {}) {
  const response = await fetch(url, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...(options.headers ?? {}) },
    ...options,
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    const errors = data?.errors ?? ['Error desconocido.'];
    throw new Error(Array.isArray(errors) ? errors.join(' ') : String(errors));
  }
  return data;
}

export default function UsersPage() {
  const [users, setUsers] = useState([]);
  const [roles, setRoles] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Modal para asignar rol
  const [modal, setModal] = useState(false);
  const [selectedUser, setSelectedUser] = useState(null);
  const [selectedRole, setSelectedRole] = useState('');
  const [actionLoading, setActionLoading] = useState(false);
  const [actionError, setActionError] = useState('');

  const loadData = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [usersData, rolesData] = await Promise.all([
        apiFetch('/api/Users'),
        apiFetch('/api/Roles'),
      ]);
      setUsers(usersData);
      setRoles(rolesData);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Disponibles = roles que el usuario aún no tiene asignados
  function availableRoles(user) {
    return roles.filter(r => !user.roles.includes(r.name));
  }

  function openAssignModal(user) {
    const avail = availableRoles(user);
    setSelectedUser(user);
    setSelectedRole(avail.length > 0 ? avail[0].name : '');
    setActionError('');
    setModal(true);
  }

  async function handleAssignRole() {
    if (!selectedUser || !selectedRole) return;
    setActionLoading(true);
    setActionError('');
    try {
      await apiFetch(`/api/Users/${selectedUser.id}/roles`, {
        method: 'POST',
        body: JSON.stringify({ roleName: selectedRole }),
      });
      setModal(false);
      await loadData();
    } catch (e) {
      setActionError(e.message);
    } finally {
      setActionLoading(false);
    }
  }

  async function handleRemoveRole(userId, roleName) {
    try {
      await apiFetch(`/api/Users/${userId}/roles/${encodeURIComponent(roleName)}`, {
        method: 'DELETE',
      });
      await loadData();
    } catch (e) {
      setError(e.message);
    }
  }

  if (loading) {
    return (
      <div className="d-flex justify-content-center mt-5">
        <Spinner color="primary" />
      </div>
    );
  }

  return (
    <>
      <h2 className="mb-4">Gestión de Usuarios</h2>

      {error && (
        <Alert color="danger" toggle={() => setError('')}>
          {error}
        </Alert>
      )}

      <Card>
        <CardHeader>
          <strong>Usuarios registrados</strong>
          <small className="text-muted ms-2">({users.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <Table responsive hover className="mb-0">
            <thead className="table-light">
              <tr>
                <th>Usuario</th>
                <th>Email</th>
                <th>Estado</th>
                <th>Roles asignados</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {users.length === 0 && (
                <tr>
                  <td colSpan={5} className="text-center text-muted py-4">
                    No hay usuarios registrados.
                  </td>
                </tr>
              )}
              {users.map(user => (
                <tr key={user.id}>
                  <td className="align-middle fw-semibold">{user.userName}</td>
                  <td className="align-middle">{user.email}</td>
                  <td className="align-middle">
                    <Badge color={user.isActive ? 'success' : 'secondary'} pill>
                      {user.isActive ? 'Activo' : 'Inactivo'}
                    </Badge>
                  </td>
                  <td className="align-middle">
                    {user.roles.length === 0 ? (
                      <span className="text-muted small">Sin roles</span>
                    ) : (
                      user.roles.map(role => (
                        <Badge
                          key={role}
                          color="primary"
                          className="me-1"
                          style={{ cursor: 'default' }}
                        >
                          {role.replace(/_/g, ' ')}
                          <button
                            type="button"
                            className="btn-close btn-close-white ms-1"
                            style={{ fontSize: '0.55rem', verticalAlign: 'middle' }}
                            aria-label={`Quitar rol ${role.replace(/_/g, ' ')}`}
                            onClick={() => handleRemoveRole(user.id, role)}
                          />
                        </Badge>
                      ))
                    )}
                  </td>
                  <td className="align-middle">
                    <Button
                      size="sm"
                      color="outline-primary"
                      disabled={availableRoles(user).length === 0}
                      onClick={() => openAssignModal(user)}
                    >
                      + Asignar rol
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
        </CardBody>
      </Card>

      {/* Modal asignar rol */}
      <Modal isOpen={modal} toggle={() => setModal(false)}>
        <ModalHeader toggle={() => setModal(false)}>
          Asignar rol — {selectedUser?.userName}
        </ModalHeader>
        <ModalBody>
          {actionError && <Alert color="danger">{actionError}</Alert>}
          <FormGroup>
            <Label for="roleSelect">Rol a asignar</Label>
            <Input
              id="roleSelect"
              type="select"
              value={selectedRole}
              onChange={e => setSelectedRole(e.target.value)}
            >
              {selectedUser && availableRoles(selectedUser).map(r => (
                <option key={r.name} value={r.name}>{r.name.replace(/_/g, ' ')}</option>
              ))}
            </Input>
          </FormGroup>
        </ModalBody>
        <ModalFooter>
          <Button color="primary" onClick={handleAssignRole} disabled={actionLoading || !selectedRole}>
            {actionLoading ? <Spinner size="sm" /> : 'Asignar'}
          </Button>
          <Button color="secondary" outline onClick={() => setModal(false)}>
            Cancelar
          </Button>
        </ModalFooter>
      </Modal>
    </>
  );
}
