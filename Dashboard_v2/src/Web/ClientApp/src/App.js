import React from 'react';
import { Route, Routes } from 'react-router-dom';
import AppRoutes from './AppRoutes';
import { Layout } from './components/Layout';
import ProtectedRoute from './components/ProtectedRoute';
import AdminRoute from './components/AdminRoute';
import ProfesorRoute from './components/ProfesorRoute';
import ProfesorOrAdminRoute from './components/ProfesorOrAdminRoute';
import JefeOrAdminRoute from './components/JefeOrAdminRoute';
import JefeDeProyectoOrAdminRoute from './components/JefeDeProyectoOrAdminRoute';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import { AuthProvider } from './contexts/AuthContext';
import './custom.css';

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        {/* Public routes — no layout */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        {/* Protected routes — wrapped in dashboard layout */}
        {AppRoutes.map((route, index) => {
          const { element, pageTitle, adminOnly, jefeOrAdminOnly, jefeDeProyectoOrAdminOnly, profesorOnly, profesorOrAdminOnly, ...rest } = route;
          const Guard = adminOnly
            ? AdminRoute
            : jefeOrAdminOnly
              ? JefeOrAdminRoute
              : jefeDeProyectoOrAdminOnly
                ? JefeDeProyectoOrAdminRoute
                : profesorOrAdminOnly
                  ? ProfesorOrAdminRoute
                  : profesorOnly
                    ? ProfesorRoute
                    : ProtectedRoute;
          return (
            <Route
              key={index}
              {...rest}
              element={
                <Guard>
                  <Layout pageTitle={pageTitle}>
                    {element}
                  </Layout>
                </Guard>
              }
            />
          );
        })}
      </Routes>
    </AuthProvider>
  );
}
