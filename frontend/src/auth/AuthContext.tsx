import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api, getToken, setToken, setUnauthorizedHandler } from '../api/client';
import type { LoginResponse, Role, User } from '../api/types';

interface AuthState {
  user: User | null;
  loading: boolean;
  login: (email: string, password: string, role?: Role) => Promise<User>;
  logout: () => void;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  const logout = useCallback(() => {
    setToken(null);
    setUser(null);
  }, []);

  useEffect(() => {
    setUnauthorizedHandler(logout);
    if (!getToken()) {
      setLoading(false);
      return;
    }
    api
      .get<User>('/api/auth/me')
      .then(setUser)
      .catch(() => setToken(null))
      .finally(() => setLoading(false));
  }, [logout]);

  const login = useCallback(async (email: string, password: string, role?: Role) => {
    const res = await api.post<LoginResponse>('/api/auth/login', { email, password, role });
    setToken(res.accessToken);
    setUser(res.user);
    return res.user;
  }, []);

  const value = useMemo(() => ({ user, loading, login, logout }), [user, loading, login, logout]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('AuthProvider eksik');
  return ctx;
}
