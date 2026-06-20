import React, { useState, useEffect } from 'react';
import { Spinner } from 'reactstrap';
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  PieChart, Pie, Cell, Legend,
} from 'recharts';

// ── Colores consistentes ────────────────────────────────────────────────────
const COLORS = ['#4f86c6', '#56b87e', '#e07b39', '#9c65c1', '#d4526e', '#3cb4c5', '#e8c53a'];

// ── StatCard ─────────────────────────────────────────────────────────────────
function StatCard({ icon, label, value, color = 'stat-blue', link, sublabel }) {
  const inner = (
    <div className={`stat-card ${color}`}>
      <div className="stat-card__icon"><i className={`bi ${icon}`} /></div>
      <div className="stat-card__body">
        <span className="stat-card__value">{value != null ? value : '—'}</span>
        <span className="stat-card__label">{label}</span>
        {sublabel && <span className="stat-card__sublabel">{sublabel}</span>}
      </div>
    </div>
  );
  if (link) return <a href={link} style={{ textDecoration: 'none', display: 'block' }}>{inner}</a>;
  return inner;
}

// ── Sección con título ────────────────────────────────────────────────────────
function Section({ title, children }) {
  return (
    <div className="vd-section">
      <h5 className="vd-section__title">{title}</h5>
      {children}
    </div>
  );
}

// ── Gráfico de barras horizontal ──────────────────────────────────────────────
function HBarChart({ data, dataKey = 'cantidad', nameKey = 'label', color = '#4f86c6', height = 220 }) {
  if (!data?.length) return <p className="vd-empty">Sin datos</p>;
  return (
    <ResponsiveContainer width="100%" height={height}>
      <BarChart data={data} layout="vertical" margin={{ left: 8, right: 24, top: 4, bottom: 4 }}>
        <CartesianGrid strokeDasharray="3 3" horizontal={false} />
        <XAxis type="number" allowDecimals={false} tick={{ fontSize: 12 }} />
        <YAxis type="category" dataKey={nameKey} width={130} tick={{ fontSize: 12 }} />
        <Tooltip />
        <Bar dataKey={dataKey} fill={color} radius={[0, 4, 4, 0]} />
      </BarChart>
    </ResponsiveContainer>
  );
}

// ── Gráfico de barras vertical ────────────────────────────────────────────────
function VBarChart({ data, dataKey = 'cantidad', nameKey = 'label', color = '#4f86c6', height = 220 }) {
  if (!data?.length) return <p className="vd-empty">Sin datos</p>;
  return (
    <ResponsiveContainer width="100%" height={height}>
      <BarChart data={data} margin={{ left: 0, right: 8, top: 4, bottom: 4 }}>
        <CartesianGrid strokeDasharray="3 3" vertical={false} />
        <XAxis dataKey={nameKey} tick={{ fontSize: 12 }} />
        <YAxis allowDecimals={false} tick={{ fontSize: 12 }} />
        <Tooltip />
        <Bar dataKey={dataKey} fill={color} radius={[4, 4, 0, 0]} />
      </BarChart>
    </ResponsiveContainer>
  );
}

// ── Gráfico de dona ────────────────────────────────────────────────────────────
function DonutChart({ data, nameKey = 'label', valueKey = 'cantidad', height = 240 }) {
  if (!data?.length) return <p className="vd-empty">Sin datos</p>;
  return (
    <ResponsiveContainer width="100%" height={height}>
      <PieChart>
        <Pie
          data={data}
          dataKey={valueKey}
          nameKey={nameKey}
          cx="50%"
          cy="50%"
          innerRadius="40%"
          outerRadius="65%"
          paddingAngle={2}
          label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
          labelLine={false}
        >
          {data.map((_, i) => (
            <Cell key={i} fill={COLORS[i % COLORS.length]} />
          ))}
        </Pie>
        <Tooltip formatter={(v, n) => [v, n]} />
        <Legend wrapperStyle={{ fontSize: 12 }} />
      </PieChart>
    </ResponsiveContainer>
  );
}

// ── Tabla compacta ─────────────────────────────────────────────────────────────
function MiniTable({ rows, col1 = 'Tipo', col2 = 'Cantidad' }) {
  if (!rows?.length) return <p className="vd-empty">Sin datos</p>;
  return (
    <table className="vd-mini-table">
      <thead>
        <tr><th>{col1}</th><th>{col2}</th></tr>
      </thead>
      <tbody>
        {rows.map((r, i) => (
          <tr key={i}>
            <td>{r.label}</td>
            <td>{r.cantidad}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

// ── Componente principal ───────────────────────────────────────────────────────
export default function VicedecanoDashboard() {
  const [data, setData] = useState(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    fetch('/api/Dashboard/vicedecano', { credentials: 'include' })
      .then(r => r.ok ? r.json() : Promise.reject())
      .then(setData)
      .catch(() => setError(true));
  }, []);

  if (error) return <p className="text-danger">No se pudo cargar el dashboard del área.</p>;
  if (!data) return (
    <div className="d-flex justify-content-center py-5"><Spinner color="primary" /></div>
  );

  return (
    <div className="vd-dashboard">

      {/* ── Totales ────────────────────────────────────────────────────── */}
      <div className="stats-grid">
        <StatCard icon="bi-trophy-fill"            label="Premios"        value={data.totalPremios}       color="stat-purple" link="/premios-area" />
        <StatCard icon="bi-file-earmark-text-fill" label="Publicaciones"  value={data.totalPublicaciones} color="stat-blue"   link="/publicaciones-area" />
        <StatCard icon="bi-mic-fill"               label="Ponencias"      value={data.totalPonencias}     color="stat-green"  link="/events" />
        <StatCard icon="bi-calendar-event-fill"    label="Eventos"        value={data.totalEventos}       color="stat-orange" link="/events" />
        <StatCard icon="bi-folder-fill"            label="Proyectos"      value={data.totalProyectos}     color="stat-teal"   link="/proyectos-area" />
        <StatCard icon="bi-share-fill"             label="Redes"          value={data.totalRedes}         color="stat-blue"   link="/redes-area" />
        <StatCard icon="bi-people-fill"            label="Grupos Inv."    value={data.totalGrupos}        color="stat-green"  link="/grupos-investigacion-area" />
        <StatCard icon="bi-lightbulb-fill"         label="Patentes"       value={data.totalPatentes}      color="stat-purple" link="/patentes-area" />
        <StatCard icon="bi-shield-check-fill"      label="Registros"      value={data.totalRegistros}     color="stat-teal"   link="/registros-area" />
        <StatCard icon="bi-file-ruled-fill"        label="Normas"         value={data.totalNormas}        color="stat-orange" link="/normas-area" />
        <StatCard icon="bi-box-seam-fill"          label="Nuevos Prod."   value={data.totalProductos}     color="stat-blue"   link="/productos-area" />
      </div>

      {/* ── Fila 1: Premios + Publicaciones por grupo ─────────────────── */}
      <div className="vd-grid-2">
        <Section title="Premios por tipo de premio">
          <HBarChart data={data.premiosPorTipo} color="#9c65c1" />
        </Section>

        <Section title="Publicaciones por grupo de indexación">
          <VBarChart data={data.publicacionesPorGrupo} color="#4f86c6" />
        </Section>
      </div>

      {/* ── Fila 2: Publicaciones por año + Proyectos por estado ───────── */}
      <div className="vd-grid-2">
        <Section title="Publicaciones por año">
          <VBarChart data={data.publicacionesPorAno} color="#56b87e" />
        </Section>

        <Section title="Proyectos por estado de ejecución">
          <DonutChart data={data.proyectosPorEstado} />
        </Section>
      </div>

      {/* ── Fila 3: Redes por tipo + Eventos por tipo ─────────────────── */}
      <div className="vd-grid-2">
        <Section title="Redes científicas por tipo">
          <DonutChart data={data.redesPorTipo} />
        </Section>

        <Section title="Eventos por tipo">
          <HBarChart data={data.eventosPorTipo} color="#e07b39" />
        </Section>
      </div>

      {/* ── Fila 4: Tablas de propiedad intelectual ───────────────────── */}
      <div className="vd-grid-3">
        <Section title="Patentes por origen">
          <MiniTable rows={data.patentesPorOrigen} col1="Origen" />
          <p className="vd-note">Total: {data.totalPatentes}</p>
        </Section>

        <Section title="Registros de software">
          <p className="vd-stat-big">{data.totalRegistros}</p>
        </Section>

        <Section title="Normas técnicas">
          <p className="vd-stat-big">{data.totalNormas}</p>
        </Section>
      </div>

    </div>
  );
}
