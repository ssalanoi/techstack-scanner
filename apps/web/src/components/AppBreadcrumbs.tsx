import { Breadcrumbs, Anchor } from '@mantine/core';
import { Link, useLocation } from 'react-router-dom';

export function AppBreadcrumbs() {
  const location = useLocation();
  const pathnames = location.pathname.split('/').filter((x) => x);

  // Map route segments to readable names
  const getName = (segment: string, idx: number) => {
    if (segment === 'projects') return 'Projects';
    if (segment === 'admin') return 'Admin';
    if (segment === 'login') return 'Login';
    if (segment.match(/^\d+$/)) return `Project #${segment}`;
    if (segment === 'dashboard') return 'Dashboard';
    return segment.charAt(0).toUpperCase() + segment.slice(1);
  };

  const items = [
    <Anchor component={Link} to="/" key="home">
      Dashboard
    </Anchor>,
    ...pathnames.map((value, idx) => {
      const to = '/' + pathnames.slice(0, idx + 1).join('/');
      return idx === pathnames.length - 1 ? (
        <span key={to}>{getName(value, idx)}</span>
      ) : (
        <Anchor component={Link} to={to} key={to}>
          {getName(value, idx)}
        </Anchor>
      );
    }),
  ];

  return <Breadcrumbs mb="md">{items}</Breadcrumbs>;
}
