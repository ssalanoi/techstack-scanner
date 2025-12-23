import type React from 'react';
import { useState } from 'react';
import { useNavigate, useLocation, type Location } from 'react-router-dom';
import { Alert, Button, Paper, PasswordInput, Stack, Text, TextInput, Title } from '@mantine/core';
import { login, getErrorMessage } from '../services/api';
import { useAuth } from '../contexts/AuthContext';

export default function Login() {
  const { setToken } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const searchParams = new URLSearchParams(location.search);
  const fromQuery = searchParams.get('from');
  const from = (location.state as { from?: Location })?.from?.pathname || fromQuery || '/admin';

  const [email, setEmail] = useState('admin@techstack.local');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [emailError, setEmailError] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);

  const validate = () => {
    let valid = true;
    const trimmedEmail = email.trim();
    const trimmedPassword = password.trim();

    if (!trimmedEmail) {
      setEmailError('Email is required');
      valid = false;
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmedEmail)) {
      setEmailError('Enter a valid email');
      valid = false;
    } else {
      setEmailError(null);
    }

    if (!trimmedPassword) {
      setPasswordError('Password is required');
      valid = false;
    } else {
      setPasswordError(null);
    }

    return valid;
  };

  const handleLogin = async (e?: React.FormEvent) => {
    e?.preventDefault();
    setError(null);
    if (!validate()) return;
    setLoading(true);
    try {
      const res = await login(email.trim(), password);
      setToken(res.token);
      void navigate(from, { replace: true });
    } catch (err: unknown) {
      setError(getErrorMessage(err) || 'Login failed. Check credentials and API availability.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Paper withBorder p="lg" radius="md" maw={420} mx="auto" mt="lg" component="form" onSubmit={(e) => void handleLogin(e)}>
      <Stack>
        <div>
          <Title order={3}>Admin Login</Title>
          <Text c="dimmed" size="sm">
            Use ADMIN_EMAIL and ADMIN_PASSWORD configured on the API.
          </Text>
        </div>
        <TextInput
          label="Email"
          value={email}
          onChange={(e) => setEmail(e.currentTarget.value)}
          type="email"
          required
          error={emailError}
          autoComplete="username"
        />
        <PasswordInput
          label="Password"
          value={password}
          onChange={(e) => setPassword(e.currentTarget.value)}
          required
          error={passwordError}
          autoComplete="current-password"
        />
        <Button type="submit" loading={loading} disabled={loading}>
          Login
        </Button>
        {error && <Alert color="red">{error}</Alert>}
      </Stack>
    </Paper>
  );
}
