import DashboardHome from "./pages/DashboardHome";
import { Counter } from "./components/Counter";
import UsersPage from "./pages/UsersPage";
import PublicationsPage from "./pages/PublicationsPage";
import CurriculumPage from "./pages/CurriculumPage";
import AwardsPage from "./pages/AwardsPage";
import EventsPage from "./pages/EventsPage";
import UniversidadesPage from "./pages/UniversidadesPage";
import AreasPage from "./pages/AreasPage";
import GruposDeInvestigacionPage from "./pages/GruposDeInvestigacionPage";import MisGruposDeInvestigacionPage from './pages/MisGruposDeInvestigacionPage';import AreasDelConocimientoPage from "./pages/AreasDelConocimientoPage";
import GruposEstudiantilesPage from "./pages/GruposEstudiantilesPage";
import RegistrosPage from "./pages/RegistrosPage";
import NormasPage from "./pages/NormasPage";
import PatentesPage from "./pages/PatentesPage";
import MisPatentesPage from "./pages/MisPatentesPage";
import MisRegistrosPage from "./pages/MisRegistrosPage";
import MisNormasPage from "./pages/MisNormasPage";
import MisProductosPage from "./pages/MisProductosPage";
import PatentesAreaPage from "./pages/PatentesAreaPage";
import RegistrosAreaPage from "./pages/RegistrosAreaPage";
import NormasAreaPage from "./pages/NormasAreaPage";
import ProductosAreaPage from "./pages/ProductosAreaPage";
import ProductosComercializadosPage from "./pages/ProductosComercializadosPage";
import RedesPage from "./pages/RedesPage";
import LineasDeInvestigacionPage from "./pages/LineasDeInvestigacionPage";
import ProyectosPage from "./pages/ProyectosPage";
import ClasificacionesPage from "./pages/ClasificacionesPage";
import PublicacionesConsultaPage from "./pages/PublicacionesConsultaPage";

const AppRoutes = [
  {
    index: true,
    element: <DashboardHome />,
    pageTitle: 'Inicio',
  },
  {
    path: '/counter',
    element: <Counter />,
    pageTitle: 'Contador',
  },
  {
    path: '/users',
    element: <UsersPage />,
    pageTitle: 'Usuarios',
    adminOnly: true,
  },
  {
    path: '/publications',
    element: <PublicationsPage />,
    pageTitle: 'Mis publicaciones',
    profesorOnly: true,
  },
  {
    path: '/curriculum',
    element: <CurriculumPage />,
    pageTitle: 'Mi Currículum',
    profesorOrAdminOnly: true,
  },
  {
    path: '/awards',
    element: <AwardsPage />,
    pageTitle: 'Premios',
    profesorOrAdminOnly: true,
  },
  {
    path: '/events',
    element: <EventsPage />,
    pageTitle: 'Eventos y presentaciones',
    vicedecanoOrProfesorOrAdminOnly: true,
  },
  {
    path: '/universidades',
    element: <UniversidadesPage />,
    pageTitle: 'Universidades',
    adminOnly: true,
  },
  {
    path: '/areas',
    element: <AreasPage />,
    pageTitle: 'Áreas',
    adminOnly: true,
  },
  {
    path: '/grupos-investigacion',
    element: <GruposDeInvestigacionPage />,
    pageTitle: 'Grupos de Investigación',
    jefeOrAdminOnly: true,
  },
  {
    path: '/grupos-estudiantiles',
    element: <GruposEstudiantilesPage />,
    pageTitle: 'Grupos Científicos Estudiantiles',
    adminOnly: true,
  },
  {
    path: '/mis-grupos',
    element: <MisGruposDeInvestigacionPage />,
    pageTitle: 'Mis Grupos de Investigación',
  },
  {
    path: '/areas-conocimiento',
    element: <AreasDelConocimientoPage />,
    pageTitle: 'Áreas del Conocimiento',
    adminOnly: true,
  },
  {
    path: '/lineas-investigacion',
    element: <LineasDeInvestigacionPage />,
    pageTitle: 'Líneas de Investigación',
    adminOnly: true,
  },
  {
    path: '/proyectos',
    element: <ProyectosPage />,
    pageTitle: 'Proyectos',
    jefeDeProyectoOrAdminOnly: true,
  },
  {
    path: '/publicaciones',
    element: <PublicacionesConsultaPage />,
    pageTitle: 'Todas las publicaciones',
    jefeDeProyectoOrAdminOnly: true,
  },
  {
    path: '/publicaciones-area',
    element: <PublicacionesConsultaPage apiUrl="/api/Publications/area" />,
    pageTitle: 'Publicaciones del Área',
    vicedecanoOnly: true,
  },
  // Profesor — Mis producciones
  {
    path: '/mis-patentes',
    element: <MisPatentesPage />,
    pageTitle: 'Mis Patentes',
    profesorOnly: true,
  },
  {
    path: '/mis-registros',
    element: <MisRegistrosPage />,
    pageTitle: 'Mis Registros',
    profesorOnly: true,
  },
  {
    path: '/mis-normas',
    element: <MisNormasPage />,
    pageTitle: 'Mis Normas',
    profesorOnly: true,
  },
  {
    path: '/mis-productos',
    element: <MisProductosPage />,
    pageTitle: 'Mis Productos Comercializados',
    profesorOnly: true,
  },
  // Vicedecano — producciones del área
  {
    path: '/patentes-area',
    element: <PatentesAreaPage />,
    pageTitle: 'Patentes del Área',
    vicedecanoOnly: true,
  },
  {
    path: '/registros-area',
    element: <RegistrosAreaPage />,
    pageTitle: 'Registros del Área',
    vicedecanoOnly: true,
  },
  {
    path: '/normas-area',
    element: <NormasAreaPage />,
    pageTitle: 'Normas del Área',
    vicedecanoOnly: true,
  },
  {
    path: '/productos-area',
    element: <ProductosAreaPage />,
    pageTitle: 'Productos del Área',
    vicedecanoOnly: true,
  },
  {
    path: '/clasificaciones',
    element: <ClasificacionesPage />,
    pageTitle: 'Clasificaciones de Proyectos',
    adminOnly: true,
  },
  {
    path: '/registros',
    element: <RegistrosPage />,
    pageTitle: 'Registros',
    adminOnly: true,
  },
  {
    path: '/normas',
    element: <NormasPage />,
    pageTitle: 'Normas',
    adminOnly: true,
  },
  {
    path: '/patentes',
    element: <PatentesPage />,
    pageTitle: 'Patentes',
    adminOnly: true,
  },
  {
    path: '/productos-comercializados',
    element: <ProductosComercializadosPage />,
    pageTitle: 'Productos comercializados',
    adminOnly: true,
  },
  {
    path: '/redes',
    element: <RedesPage />,
    pageTitle: 'Redes',
    jefeRedesOrAdminOnly: true,
  },
];

export default AppRoutes;
