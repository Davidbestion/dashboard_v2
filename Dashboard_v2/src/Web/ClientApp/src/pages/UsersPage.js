import React, { useState, useEffect, useCallback } from 'react';
import {
  Card, CardBody, CardHeader,
  Badge, Button,
  Spinner, Alert,
  Input, FormGroup, Label,
  Modal, ModalHeader, ModalBody, ModalFooter,
  Row, Col,
} from 'reactstrap';
import FilterableDataTable from '../components/FilterableDataTable';

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

const EMPTY_CREATE_FORM = {
  userName: '',
  userLastName1: '',
  userLastName2: '',
  email: '',
  roleName: '',
  areaId: '',
};

export default function UsersPage() {
  const [users, setUsers] = useState([]);
  const [roles, setRoles] = useState([]);
  const [areas, setAreas] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Modal asignar rol
  const [assignModal, setAssignModal] = useState(false);
  const [selectedUser, setSelectedUser] = useState(null);
  const [selectedRole, setSelectedRole] = useState('');
  const [actionLoading, setActionLoading] = useState(false);
  const [actionError, setActionError] = useState('');

  // Modal crear usuario
  const [createModal, setCreateModal] = useState(false);
  const [createForm, setCreateForm] = useState(EMPTY_CREATE_FORM);
  const [createLoading, setCreateLoading] = useState(false);
  const [createError, setCreateError] = useState('');

  const loadData = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [usersData, rolesData, areasData] = await Promise.all([
        apiFetch('/api/Users'),
        apiFetch('/api/Roles'),
        apiFetch('/api/Areas'),
      ]);
      setUsers(usersData);
      setRoles(rolesData);
      setAreas(areasData ?? []);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadData(); }, [loadData]);

  const pendingUsers = users.filter(u => u.roles.length === 0);

  function availableRoles(user) {
    return roles.filter(r => !user.roles.includes(r.name));
  }

  function openAssignModal(user) {
    const avail = availableRoles(user);
    setSelectedUser(user);
    setSelectedRole(avail.length > 0 ? avail[0].name : '');
    setActionError('');
    setAssignModal(true);
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
      setAssignModal(false);
      await loadData();
    } catch (e) {
      setActionError(e.message);
    } finally {
      setActionLoading(false);
    }
  }

  async function handleRemoveRole(userId, roleName) {
    try {
      await apiFetch(`/api/Users/${userId}/roles/${encodeURIComponent(roleName)}`, { method: 'DELETE' });
      await loadData();
    } catch (e) {
      setError(e.message);
    }
  }

  async function handleToggleActive(user) {
    try {
      await apiFetch(`/api/Users/${user.id}/active`, {
        method: 'PUT',
        body: JSON.stringify({ active: !user.isActive }),
      });
      await loadData();
    } catch (e) {
      setError(e.message);
    }
  }

  function openCreateModal() {
    setCreateForm({ ...EMPTY_CREATE_FORM, roleName: roles[0]?.name ?? '' });
    setCreateError('');
    setCreateModal(true);
  }

  async function handleCreateUser() {
    if (!createForm.userName.trim() || !createForm.userLastName1.trim() || !createForm.email.trim() || !createForm.roleName) {
      setCreateError('Nombre de usuario, primer apellido, email y rol son obligatorios.');
      return;
    }
    setCreateLoading(true);
    setCreateError('');
    try {
      await apiFetch('/api/Users', {
        method: 'POST',
        body: JSON.stringify({
          userName:      createForm.userName.trim(),
          userLastName1: createForm.userLastName1.trim(),
          userLastName2: createForm.userLastName2.trim() || null,
          email:         createForm.email.trim(),
          roleName:      createForm.roleName,
          areaId:        createForm.areaId || null,
        }),
      });
      setCreateModal(false);
      await loadData();
    } catch (e) {
      setCreateError(e.message);
    } finally {
      setCreateLoading(false);
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
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="mb-0">Gestión de Usuarios</h2>
        <Button color="primary" onClick={openCreateModal}>
          <i className="bi bi-person-plus me-1" /> Crear usuario
        </Button>
      </div>

      {error && (
        <Alert color="danger" toggle={() => setError('')}>{error}</Alert>
      )}

      {pendingUsers.length > 0 && (
        <Alert color="warning" className="d-flex align-items-start gap-2">
          <i className="bi bi-exclamation-triangle-fill mt-1" />
          <div>
            <strong>{pendingUsers.length} usuario{pendingUsers.length > 1 ? 's' : ''} pendiente{pendingUsers.length > 1 ? 's' : ''} de configuración.</strong>
            {' '}Intentaron iniciar sesión pero aún no tienen rol asignado:{' '}
            {pendingUsers.map((u, i) => (
              <span key={u.id}>
                {i > 0 && ', '}
                <strong>{u.userName}</strong> ({u.email})
              </span>
            ))}.
            {' '}Asígnales un rol desde la tabla.
          </div>
        </Alert>
      )}

      <Card>
        <CardHeader>
          <strong>Usuarios registrados</strong>
          <small className="text-muted ms-2">({users.length})</small>
        </CardHeader>
        <CardBody className="p-0">
          <FilterableDataTable
            filterConfig={{
              search: { fields: ['userName', 'email'], placeholder: 'Buscar usuario...' },
              filters: [
                { key: 'isActive', label: 'Estado',
                  options: [{ value: 'true', label: 'Activo' }, { value: 'false', label: 'Inactivo' }],
                  match: (item, val) => String(item.isActive) === val },
                { key: 'role', label: 'Rol',
                  options: roles.map(r => ({ value: r.name, label: r.name.replace(/_/g, ' ') })),
                  match: (item, val) => (item.roles ?? []).includes(val) },
              ],
            }}
            columns={[
              { key: 'userName', label: 'Usuario', sortable: true, className: 'fw-semibold',
                render: (v, u) => (
                  <>
                    {v}
                    {u.roles.length === 0 && (
                      <Badge color="warning" pill className="ms-2" style={{ fontSize: '0.65em' }}>
                        Sin rol
                      </Badge>
                    )}
                  </>
                ),
              },
              { key: 'email', label: 'Email', sortable: true },
              {
                key: 'isActive',
                label: 'Estado',
                render: v => <Badge color={v ? 'success' : 'secondary'} pill>{v ? 'Activo' : 'Inactivo'}</Badge>,
              },
              {
                key: 'roles',
                label: 'Roles asignados',
                render: (roleList, u) => roleList.length === 0
                  ? <span className="text-muted small">Sin roles</span>
                  : roleList.map(role => (
                    <Badge key={role} color="primary" className="me-1" style={{ cursor: 'default' }}>
                      {role.replace(/_/g, ' ')}
                      <button
                        type="button"
                        className="btn-close btn-close-white ms-1"
                        style={{ fontSize: '0.55rem', verticalAlign: 'middle' }}
                        aria-label={`Quitar rol ${role.replace(/_/g, ' ')}`}
                        onClick={() => handleRemoveRole(u.id, role)}
                      />
                    </Badge>
                  )),
              },
            ]}
            data={users}
            keyExtractor={u => u.id}
            actions={[
              {
                key: 'assign',
                label: '+ Asignar rol',
                color: 'outline-primary',
                disabled: u => availableRoles(u).length === 0,
                onClick: u => openAssignModal(u),
              },
              {
                key: 'toggle-active',
                color: u => u.isActive ? 'outline-secondary' : 'outline-success',
                label: u => u.isActive ? 'Desactivar' : 'Activar',
                onClick: u => handleToggleActive(u),
              },
            ]}
            emptyMessage="No hay usuarios registrados."
            detailConfig
          />
        </CardBody>
      </Card>

      {/* Modal asignar rol */}
      <Modal isOpen={assignModal} toggle={() => setAssignModal(false)}>
        <ModalHeader toggle={() => setAssignModal(false)}>
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
          <Button color="secondary" outline onClick={() => setAssignModal(false)}>Cancelar</Button>
        </ModalFooter>
      </Modal>

      {/* Modal crear usuario */}
      <Modal isOpen={createModal} toggle={() => setCreateModal(false)}>
        <ModalHeader toggle={() => setCreateModal(false)}>Crear usuario</ModalHeader>
        <ModalBody>
          {createError && <Alert color="danger">{createError}</Alert>}
          <p className="text-muted small mb-3">
            <i className="bi bi-info-circle me-1" />
            El email debe coincidir exactamente con el registrado en LDAP para que el usuario pueda iniciar sesión.
          </p>
          <Row>
            <Col md={6}>
              <FormGroup>
                <Label>Nombre de usuario <span className="text-danger">*</span></Label>
                <Input
                  value={createForm.userName}
                  onChange={e => setCreateForm(f => ({ ...f, userName: e.target.value }))}
                  placeholder="ej. jperez"
                />
              </FormGroup>
            </Col>
            <Col md={6}>
              <FormGroup>
                <Label>Email <span className="text-danger">*</span></Label>
                <Input
                  type="email"
                  value={createForm.email}
                  onChange={e => setCreateForm(f => ({ ...f, email: e.target.value }))}
                  placeholder="ej. jperez@matcom.uh.cu"
                />
              </FormGroup>
            </Col>
          </Row>
          <Row>
            <Col md={6}>
              <FormGroup>
                <Label>Primer apellido <span className="text-danger">*</span></Label>
                <Input
                  value={createForm.userLastName1}
                  onChange={e => setCreateForm(f => ({ ...f, userLastName1: e.target.value }))}
                  placeholder="ej. Pérez"
                />
              </FormGroup>
            </Col>
            <Col md={6}>
              <FormGroup>
                <Label>Segundo apellido</Label>
                <Input
                  value={createForm.userLastName2}
                  onChange={e => setCreateForm(f => ({ ...f, userLastName2: e.target.value }))}
                  placeholder="ej. García"
                />
              </FormGroup>
            </Col>
          </Row>
          <Row>
            <Col md={6}>
              <FormGroup>
                <Label>Rol <span className="text-danger">*</span></Label>
                <Input
                  type="select"
                  value={createForm.roleName}
                  onChange={e => setCreateForm(f => ({ ...f, roleName: e.target.value }))}
                >
                  {roles.map(r => (
                    <option key={r.name} value={r.name}>{r.name.replace(/_/g, ' ')}</option>
                  ))}
                </Input>
              </FormGroup>
            </Col>
            <Col md={6}>
              <FormGroup>
                <Label>Área <span className="text-muted small">(opcional)</span></Label>
                <Input
                  type="select"
                  value={createForm.areaId}
                  onChange={e => setCreateForm(f => ({ ...f, areaId: e.target.value }))}
                >
                  <option value="">— Sin área asignada —</option>
                  {areas.map(a => (
                    <option key={a.id} value={a.id}>{a.nombre}</option>
                  ))}
                </Input>
              </FormGroup>
            </Col>
          </Row>
        </ModalBody>
        <ModalFooter>
          <Button color="primary" onClick={handleCreateUser} disabled={createLoading}>
            {createLoading ? <Spinner size="sm" /> : 'Crear usuario'}
          </Button>
          <Button color="secondary" outline onClick={() => setCreateModal(false)}>Cancelar</Button>
        </ModalFooter>
      </Modal>
    </>
  );
}
