import { createContext, useContext } from "react";

export interface Claim {
  type: string;
  value: string;
}
export interface Authentication {
  checkSessionUri?: string;
  claims?: Claim[];
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (returnUrl: string) => void;
  logout: () => void;
  sessionExpiry?: Date;
  sessionState?: string;
}

export const login = (returnUrl: string) => {
  // Login using the BFF API endpoint
  const params = new URLSearchParams({ returnUrl });
  window.location.href = `/api/auth/login?${params}`;
};
export const logout = (sessionId: string | undefined) => {
  // Logout using the BFF API endpoint
  const params = sessionId ? `?${new URLSearchParams({ sessionId })}` : "";
  window.location.href = `/api/auth/logout${params}`;
};

export const AuthenticationContext = createContext<Authentication>({
  isAuthenticated: false,
  isLoading: true,
  login,
  logout: () => logout(undefined),
});

export const useAuthentication = () => useContext(AuthenticationContext);
