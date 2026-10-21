import { createContext, useContext, useState, useEffect, ReactNode, useMemo, useCallback } from 'react';
import { authService, User } from '../services/authService';

interface AuthContextType {
  user: User | null;
  teamId: number | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, firstName: string, acceptsTerms: boolean) => Promise<void>;
  googleSignIn: (idToken: string, acceptsTerms: boolean) => Promise<void>;
  logout: () => Promise<void>;
  isAuthenticated: boolean;
  setUser: (user: User | null, teamId?: number | null) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUserState] = useState<User | null>(null);
  const [teamId, setTeamIdState] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Check if user is already logged in
    const initAuth = async () => {
      // List of pages where we should not validate authentication
      const publicPages = ['/', '/pricing', '/login', '/register', '/forgot-password', '/verify-email'];
      const isPublicPage = publicPages.some(page => window.location.pathname === page || window.location.pathname.startsWith(page + '/'));

      if (isPublicPage) {
        // Don't validate tokens on public pages - just check if we have a valid token
        if (!authService.isAuthenticated()) {
          setUserState(null);
          setTeamIdState(null);
        }
        setLoading(false);
        return;
      }

      // For protected pages, validate the user is authenticated
      if (authService.isAuthenticated()) {
        try {
          const currentUser = await authService.getCurrentUser();
          const storedTeamId = localStorage.getItem('teamId');
          setUserState(currentUser);
          if (storedTeamId) {
            setTeamIdState(parseInt(storedTeamId, 10));
          }
        } catch (error) {
          console.error('Failed to get current user:', error);
          authService.logout();
        }
      }
      setLoading(false);
    };

    initAuth();
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const response = await authService.login({ email, password });
    setUserState({ ...response.user, isSubscribed: response.user.isSubscribed || false });
    if (response.teamId) {
      setTeamIdState(response.teamId);
      localStorage.setItem('teamId', response.teamId.toString());
    }
  }, []);

  const register = useCallback(async (email: string, password: string, firstName: string, acceptsTerms: boolean) => {
    const response = await authService.register({
      email,
      password,
      firstName,
      acceptsTerms,
    });
    setUserState({ ...response.user, isSubscribed: response.user.isSubscribed || false });
    if (response.teamId) {
      setTeamIdState(response.teamId);
      localStorage.setItem('teamId', response.teamId.toString());
    }
  }, []);

  const googleSignIn = useCallback(async (idToken: string, acceptsTerms: boolean) => {
    const response = await authService.googleSignIn(idToken, acceptsTerms);
    setUserState({ ...response.user, isSubscribed: response.user.isSubscribed || false });
    if (response.teamId) {
      setTeamIdState(response.teamId);
      localStorage.setItem('teamId', response.teamId.toString());
    }
  }, []);

  const logout = useCallback(async () => {
    await authService.logout();
    setUserState(null);
    setTeamIdState(null);
    localStorage.removeItem('teamId');
    // Clear loading state to prevent redirect loops
    setLoading(false);
  }, []);

  const setUserCallback = useCallback((user: User | null, newTeamId?: number | null) => {
    setUserState(user);
    if (newTeamId !== undefined) {
      setTeamIdState(newTeamId);
      if (newTeamId) {
        localStorage.setItem('teamId', newTeamId.toString());
      } else {
        localStorage.removeItem('teamId');
      }
    }
  }, []);

  const value = useMemo(() => ({
    user,
    teamId,
    loading,
    login,
    register,
    googleSignIn,
    logout,
    isAuthenticated: !!user,
    setUser: setUserCallback,
  }), [user, teamId, loading, login, register, googleSignIn, logout, setUserCallback]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
