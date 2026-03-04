import React, { useState } from 'react';
import Sidebar from './Sidebar';
import TopBar from './TopBar';

export function Layout({ children, pageTitle }) {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  return (
    <div className={`app-shell${sidebarCollapsed ? ' app-shell--collapsed' : ''}`}>
      <Sidebar
        collapsed={sidebarCollapsed}
        onToggle={() => setSidebarCollapsed(!sidebarCollapsed)}
      />
      <div className="app-shell__main">
        <TopBar pageTitle={pageTitle} />
        <main className="app-shell__content">
          {children}
        </main>
      </div>
    </div>
  );
}
