import { AppShell, Container, Group, Anchor, Title, Button } from '@mantine/core';
import { Link, Outlet, useNavigate } from 'react-router-dom';
import { AppBreadcrumbs } from '../components/AppBreadcrumbs';
import { useAuth } from '../contexts/AuthContext';

export function MainLayout() {
  const { isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    void navigate('/login');
    window.location.reload(); // Clear all caches and reload
  };
  return (
    <AppShell padding="md" header={{ height: 60 }}>
      <AppShell.Header>
        <Container size="lg" h="100%">
          <Group h="100%" justify="space-between">
            <Anchor component={Link} to="/" underline="never" style={{ textDecoration: 'none' }}>
              <Title order={4} style={{ margin: 0, color: 'inherit' }}>TechStack Scanner</Title>
            </Anchor>
            <Group gap="md">
              {isAuthenticated && (
                <Button component={Link} to="/projects" variant="subtle" size="sm">
                  Projects
                </Button>
              )}
              {isAuthenticated && (
                <Button component={Link} to="/admin" variant="subtle" size="sm">
                  Admin area
                </Button>
              )}
              {!isAuthenticated && (
                <Button component={Link} to="/login" variant="subtle" size="sm">
                  Login
                </Button>
              )}
              {isAuthenticated && (
                <Button variant="subtle" color="red" onClick={handleLogout} size="sm">
                  Logout
                </Button>
              )}
            </Group>
          </Group>
        </Container>
      </AppShell.Header>
      <AppShell.Main> 
        <Container size="lg">
          <AppBreadcrumbs />
          <Outlet />
        </Container>
      </AppShell.Main>
    </AppShell>
  );
}
