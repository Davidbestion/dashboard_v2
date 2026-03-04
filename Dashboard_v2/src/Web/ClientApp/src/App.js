import React from 'react';
import { Route, Routes } from 'react-router-dom';
import AppRoutes from './AppRoutes';
import { Layout } from './components/Layout';
import ProtectedRoute from './components/ProtectedRoute';
import LoginPage from './pages/LoginPage';
import { AuthProvider } from './contexts/AuthContext';
import './custom.css';

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        {/* Public route — no layout */}
        <Route path="/login" element={<LoginPage />} />

        {/* Protected routes — wrapped in dashboard layout */}
        {AppRoutes.map((route, index) => {
          const { element, pageTitle, ...rest } = route;
          return (
            <Route
              key={index}
              {...rest}
              element={
                <ProtectedRoute>
                  <Layout pageTitle={pageTitle}>
                    {element}
                  </Layout>
                </ProtectedRoute>
              }
            />
          );
        })}
      </Routes>
    </AuthProvider>
  );
}
