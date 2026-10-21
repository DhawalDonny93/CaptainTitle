import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

// Configure axios defaults for CORS
axios.defaults.withCredentials = true;

// Types
export interface RegisterRequest {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  acceptsTerms: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface GoogleSignInRequest {
  idToken: string;
  acceptsTerms?: boolean;
}

export interface UserProfile {
  language: string;
  theme: string;
  timezone: string;
  extendedProperties: string;
}

export interface AuthResponse {
  success: boolean;
  accessToken: string;
  refreshToken: string;
  isFirstLogin?: boolean;
  requiresSubscription: boolean;
  teamId?: number;
  user: {
    id: string;
    email: string;
    firstName?: string;
    lastName?: string;
    emailVerified: boolean;
    onboardingCompleted: boolean;
    isSubscribed?: boolean;
    profile?: UserProfile;
  };
}

export interface User {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  profilePhotoUrl?: string;
  emailVerified: boolean;
  onboardingCompleted: boolean;
  isSubscribed: boolean;
  provider?: string;
}

class AuthService {
  private accessToken: string | null = null;
  private refreshToken: string | null = null;
  private isRefreshing: boolean = false;
  private refreshSubscribers: Array<(token: string) => void> = [];

  constructor() {
    // Load tokens from localStorage on initialization
    this.accessToken = localStorage.getItem('accessToken');
    this.refreshToken = localStorage.getItem('refreshToken');
  }

  // Email/Password Registration
  async register(data: RegisterRequest): Promise<any> {
    try {
      const response = await axios.post(`${API_BASE_URL}/auth/register`, {
        userName: data.firstName || data.email.split('@')[0],
        email: data.email,
        password: data.password,
      });

      // Check if email verification is required
      if (response.data.requiresEmailVerification) {
        return {
          success: true,
          requiresEmailVerification: true,
          sessionId: response.data.sessionId,
          user: response.data.user
        };
      }

      // If no verification required, store tokens
      if (response.data.success && response.data.accessToken) {
        this.setTokens(response.data.accessToken, response.data.refreshToken);
        return response.data;
      }

      throw new Error('Registration failed');
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Registration failed');
    }
  }

  // Email/Password Login
  async login(data: LoginRequest): Promise<AuthResponse> {
    try {
      const response = await axios.post(`${API_BASE_URL}/auth/login`, data);

      if (response.data.success) {
        // Store tokens
        this.setTokens(response.data.accessToken, response.data.refreshToken);
        return response.data;
      }

      throw new Error('Login failed');
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Invalid email or password');
    }
  }

  // Google Sign In (Client-side flow with idToken)
  async googleSignIn(idToken: string, acceptsTerms: boolean = true): Promise<AuthResponse> {
    try {
      console.log('🔐 AuthService: Calling Google sign-in API');
      console.log('API Endpoint:', `${API_BASE_URL}/auth/google`);
      console.log('Request payload:', {
        idToken: idToken.substring(0, 50) + '...',
        acceptsTerms
      });
      
      const response = await axios.post(`${API_BASE_URL}/auth/google`, {
        idToken,
        acceptsTerms,
      });

      console.log('📥 API Response received:', {
        status: response.status,
        hasAccessToken: !!response.data.accessToken,
        hasRefreshToken: !!response.data.refreshToken,
        hasUser: !!response.data.user
      });

      if (response.data.accessToken) {
        // Store tokens
        this.setTokens(response.data.accessToken, response.data.refreshToken);
        console.log('✅ Tokens stored successfully');
        console.log('User info:', {
          email: response.data.user?.email,
          name: response.data.user?.name,
          onboardingCompleted: response.data.user?.onboardingCompleted
        });
        return response.data;
      }

      console.error('❌ No access token in response');
      throw new Error('Google sign-in failed: No access token received');
    } catch (error: any) {
      console.error('❌ Google sign-in API error:', {
        message: error.message,
        status: error.response?.status,
        statusText: error.response?.statusText,
        data: error.response?.data,
        url: error.config?.url
      });
      
      // Extract meaningful error message
      const errorMessage = error.response?.data?.message 
        || error.response?.data?.error 
        || error.response?.data?.title
        || error.message 
        || 'Google sign-in failed';
      
      throw new Error(errorMessage);
    }
  }

  // Initiate Google OAuth via API backend (Server-side flow)
  initiateGoogleAuthViaAPI() {
    // Get the callback URL from environment or construct it
    const callbackUrl = `${window.location.origin}/auth/google/callback`;
    
    // Redirect to backend OAuth initiation endpoint
    // Backend will handle Google OAuth and redirect back with tokens
    window.location.href = `${API_BASE_URL}/auth/oauth/initiate?provider=google&callbackUrl=${encodeURIComponent(callbackUrl)}`;
  }

  // Handle Google callback from API backend (Server-side flow)
  async handleGoogleCallbackFromAPI(accessToken: string, refreshToken: string | null): Promise<void> {
    try {
      // Store tokens received from API backend
      if (accessToken) {
        this.setTokens(accessToken, refreshToken || '');
        console.log('✅ Tokens stored from API backend callback');
      } else {
        throw new Error('No access token received from API');
      }
    } catch (error: any) {
      console.error('Error handling Google callback from API:', error);
      throw new Error(error.message || 'Failed to process Google authentication');
    }
  }

  // Apple Sign In (using OAuth flow)
  initiateAppleAuth() {
    // Get the callback URL from environment or construct it
    const callbackUrl = `${window.location.origin}/auth/apple/callback`;
    
    // Redirect to backend OAuth initiation endpoint
    window.location.href = `${API_BASE_URL}/auth/oauth/initiate?provider=apple&callbackUrl=${encodeURIComponent(callbackUrl)}`;
  }

  // Handle Apple callback
  async handleAppleCallback(code: string, acceptsTerms: boolean = true): Promise<AuthResponse> {
    try {
      // Exchange code for access token via your backend
      const response = await axios.post(`${API_BASE_URL}/auth/apple`, {
        code,
        acceptsTerms,
      });

      if (response.data.accessToken) {
        this.setTokens(response.data.accessToken, response.data.refreshToken);
        return response.data;
      }

      throw new Error('Apple authentication failed');
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Apple authentication failed');
    }
  }

  // Logout
  async logout(): Promise<void> {
    try {
      if (this.refreshToken) {
        await axios.post(
          `${API_BASE_URL}/auth/logout`,
          { refreshToken: this.refreshToken },
          { headers: this.getAuthHeaders() }
        );
      }
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      this.clearTokens();
    }
  }

  // Refresh Access Token
  async refreshAccessToken(): Promise<string | null> {
    // Prevent concurrent refresh attempts
    if (this.isRefreshing) {
      // Wait for the ongoing refresh to complete
      return new Promise((resolve) => {
        this.refreshSubscribers.push((token: string) => {
          resolve(token);
        });
      });
    }

    this.isRefreshing = true;

    try {
      if (!this.refreshToken) {
        throw new Error('No refresh token available');
      }

      // Use skipAuth flag to prevent interceptor from running
      const response = await axios.post(
        `${API_BASE_URL}/auth/refresh-token`,
        { refreshToken: this.refreshToken },
        { headers: { 'X-Skip-Auth': 'true' } }
      );

      if (response.data.accessToken) {
        this.setTokens(response.data.accessToken, response.data.refreshToken);
        
        // Notify all subscribers
        this.refreshSubscribers.forEach(callback => callback(response.data.accessToken));
        this.refreshSubscribers = [];
        
        return response.data.accessToken;
      }

      return null;
    } catch (error) {
      this.clearTokens();
      this.refreshSubscribers = [];
      return null;
    } finally {
      this.isRefreshing = false;
    }
  }

  // Get Current User
  async getCurrentUser(): Promise<User | null> {
    try {
      const response = await axios.get(`${API_BASE_URL}/user/profile`, {
        headers: this.getAuthHeaders(),
      });

      if (response.data.success) {
        return response.data.user;
      }

      return null;
    } catch (error) {
      console.error('Get current user error:', error);
      return null;
    }
  }

  // Token Management
  private setTokens(accessToken: string, refreshToken: string) {
    this.accessToken = accessToken;
    this.refreshToken = refreshToken;
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
  }

  private clearTokens() {
    this.accessToken = null;
    this.refreshToken = null;
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  getRefreshToken(): string | null {
    return this.refreshToken;
  }

  isAuthenticated(): boolean {
    return !!this.accessToken;
  }

  // Validate token expiration
  isTokenValid(): boolean {
    if (!this.accessToken) {
      console.log('❌ No access token found');
      return false;
    }
    
    try {
      // Decode JWT token (without verification, just to check expiry)
      const parts = this.accessToken.split('.');
      if (parts.length !== 3) {
        console.error('❌ Invalid token format');
        return false;
      }
      
      const payload = JSON.parse(atob(parts[1]));
      
      if (!payload.exp) {
        console.error('❌ Token missing exp claim');
        return false;
      }
      
      const expiryTime = payload.exp * 1000; // Convert to milliseconds
      const now = Date.now();
      const timeUntilExpiry = expiryTime - now;
      
      console.log('🔍 Token validation:', {
        expiresAt: new Date(expiryTime).toISOString(),
        now: new Date(now).toISOString(),
        timeUntilExpiry: `${Math.floor(timeUntilExpiry / 1000)}s`,
        isValid: timeUntilExpiry > 5000
      });
      
      // Token is valid if it hasn't expired (with 5 second buffer instead of 60)
      return timeUntilExpiry > 5000;
    } catch (error) {
      console.error('❌ Token validation error:', error);
      return false;
    }
  }

  // Ensure valid token before making requests
  async ensureValidToken(): Promise<boolean> {
    if (!this.accessToken) {
      return false;
    }

    if (this.isTokenValid()) {
      return true;
    }

    // Token expired, try to refresh
    const newToken = await this.refreshAccessToken();
    return !!newToken;
  }

  private getAuthHeaders() {
    return {
      Authorization: `Bearer ${this.accessToken}`,
    };
  }

  // Email Verification
  async verifyEmailOtp(data: { email: string; otp: string; sessionId: string }): Promise<AuthResponse> {
    try {
      const response = await axios.post(
        `${API_BASE_URL}/auth/email-login/verify?isRegistration=true`,
        data
      );

      if (response.data.success && response.data.result) {
        // Store tokens
        this.setTokens(response.data.result.accessToken, response.data.result.refreshToken);
        return response.data.result;
      }

      throw new Error('Email verification failed');
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Invalid verification code');
    }
  }

  // Resend Email OTP
  async resendEmailOtp(data: { email: string; sessionId: string }): Promise<void> {
    try {
      await axios.post(`${API_BASE_URL}/auth/email-login/resend`, data);
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Failed to resend verification code');
    }
  }

  // Request Password Reset
  async requestPasswordReset(email: string): Promise<void> {
    try {
      await axios.post(`${API_BASE_URL}/auth/request-password-reset`, { email });
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Failed to send reset email');
    }
  }

  // Reset Password
  async resetPassword(data: { token: string; newPassword: string }): Promise<void> {
    try {
      const response = await axios.post(`${API_BASE_URL}/auth/reset-password`, {
        token: data.token,
        newPassword: data.newPassword,
      });

      if (!response.data.message) {
        throw new Error('Password reset failed');
      }
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Failed to reset password');
    }
  }

  // Axios interceptors for authentication and token refresh
  setupInterceptors() {
    // Request interceptor - Add token to all requests
    axios.interceptors.request.use(
      async (config) => {
        // Skip auth for refresh token requests and public endpoints
        const skipAuth = config.headers?.['X-Skip-Auth'] === 'true';
        const publicEndpoints = ['/auth/login', '/auth/register', '/auth/google', '/auth/apple', '/auth/github', '/auth/refresh-token'];
        const isPublicEndpoint = publicEndpoints.some(endpoint => config.url?.includes(endpoint));
        
        if (!skipAuth && !isPublicEndpoint && this.accessToken) {
          // Simply add the token - let response interceptor handle refresh if needed
          config.headers.Authorization = `Bearer ${this.accessToken}`;
        }
        
        // Remove the skip auth header so it doesn't get sent to server
        if (skipAuth) {
          delete config.headers['X-Skip-Auth'];
        }
        
        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // Response interceptor - Handle 401 errors
    axios.interceptors.response.use(
      (response) => response,
      async (error) => {
        const originalRequest = error.config;

        // Don't retry refresh token requests or public endpoints
        const isRefreshRequest = originalRequest.url?.includes('/auth/refresh-token');
        const publicEndpoints = ['/auth/login', '/auth/register', '/auth/google', '/auth/apple', '/auth/github'];
        const isPublicEndpoint = publicEndpoints.some(endpoint => originalRequest.url?.includes(endpoint));

        // List of public pages that should not redirect to login
        const publicPages = ['/', '/pricing', '/login', '/register', '/forgot-password', '/verify-email', '/onboarding', '/terms', '/privacy', '/features', '/about'];
        const isPublicPage = publicPages.some(page => window.location.pathname === page || window.location.pathname.startsWith(page + '/'));

        // If error is 401 and we haven't retried yet
        if (error.response?.status === 401 && !originalRequest._retry && !isRefreshRequest && !isPublicEndpoint) {
          originalRequest._retry = true;

          const newAccessToken = await this.refreshAccessToken();

          if (newAccessToken) {
            originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
            return axios(originalRequest);
          } else {
            // Refresh failed, clear tokens and redirect to login only if on protected page
            this.clearTokens();
            if (!isPublicPage) {
              window.location.href = '/login?reason=session_expired';
            }
          }
        }

        // If refresh token request fails with 401, clear tokens and redirect only if on protected page
        if (error.response?.status === 401 && isRefreshRequest) {
          this.clearTokens();
          if (!isPublicPage) {
            window.location.href = '/login?reason=session_expired';
          }
        }

        // If error is 403, user doesn't have permission
        if (error.response?.status === 403) {
          console.error('Access forbidden:', error.response.data);
        }

        return Promise.reject(error);
      }
    );
  }
}

export const authService = new AuthService();

// Setup interceptors on app initialization
authService.setupInterceptors();
