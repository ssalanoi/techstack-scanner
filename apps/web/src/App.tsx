import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { MainLayout } from './layouts/MainLayout';
import { AdminLayout } from './layouts/AdminLayout';
import Dashboard from './pages/Dashboard';
import Projects from './pages/Projects';
import ProjectDetails from './pages/ProjectDetails';
import Login from './pages/Login';
import Admin from './pages/Admin';

const router = createBrowserRouter([
  {
    path: '/',
    element: <MainLayout />,
    children: [
      { index: true, element: <Dashboard /> },
      { path: 'login', element: <Login /> },
    ],
  },
  {
    element: <ProtectedRoute />, // guard protected routes
    children: [
      {
        path: '/',
        element: <MainLayout />,
        children: [
          { path: 'projects', element: <Projects /> },
          { path: 'projects/:id', element: <ProjectDetails /> },
        ],
      },
      {
        path: '/admin',
        element: <AdminLayout />,
        children: [{ index: true, element: <Admin /> }],
      },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
]);

function App() {
  return (
    <AuthProvider>
      <RouterProvider router={router} />
    </AuthProvider>
  );
}

export default App;
