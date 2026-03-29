import DashboardHome from "./pages/DashboardHome";
import { Counter } from "./components/Counter";
import UsersPage from "./pages/UsersPage";
import PublicationsPage from "./pages/PublicationsPage";

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
];

export default AppRoutes;
