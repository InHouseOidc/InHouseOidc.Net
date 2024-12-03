import { ReactNode, useCallback, useEffect, useRef, useState } from "react";

import {
  AuthenticationContext,
  Claim,
  login,
  logout,
} from "./AuthenticationContext";
import { CheckSessionIFrame } from "./CheckSessionIFrame";

interface UserInfo {
  checkSessionUri?: string;
  claims?: Claim[];
  clientId?: string;
  isAuthenticated: boolean;
  sessionExpiry?: Date;
  sessionState?: string;
}

export const AuthenticationProvider = ({
  children,
}: {
  children: ReactNode;
}) => {
  const checkSessionIFrameRef = useRef<CheckSessionIFrame | null>(null);
  const [userInfo, setUserInfo] = useState<UserInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  // Handle session changes
  const logoutWithSessionId = useCallback(() => {
    const sessionId = userInfo?.claims?.find((c) => c.type === "sid")?.value;
    logout(sessionId);
  }, [userInfo]);
  const checkSessionCallback = useCallback(async () => {
    logoutWithSessionId();
  }, [logoutWithSessionId]);
  // Load the user from the BFF API endpoint
  useEffect(() => {
    const getUser = async () => {
      try {
        const response = await fetch("/api/auth/user-info");
        if (response.ok) {
          const userInfo = (await response.json()) as UserInfo;
          setUserInfo(userInfo);
        }
        setIsLoading(false);
      } catch (error) {
        console.error("AuthenticationProvider - load user failed", error);
      }
      setIsLoading(false);
    };
    getUser();
  }, []);
  // Once the user is available, start the session check as required
  useEffect(() => {
    if (
      userInfo &&
      !checkSessionIFrameRef.current &&
      userInfo.checkSessionUri &&
      userInfo.clientId &&
      userInfo.sessionState
    ) {
      const { clientId, checkSessionUri, sessionState } = userInfo;
      const createIFrame = async () => {
        // Setup the iframe to monitor the current session
        checkSessionIFrameRef.current = new CheckSessionIFrame(
          checkSessionCallback,
          clientId,
          checkSessionUri
        );
        await checkSessionIFrameRef.current.setup();
        checkSessionIFrameRef.current.start(sessionState);
      };
      createIFrame();
    }
  }, [checkSessionCallback, userInfo]);
  // Setup the context
  const {
    checkSessionUri,
    claims,
    isAuthenticated,
    sessionExpiry,
    sessionState,
  } = userInfo ?? { isAuthenticated: false };
  return (
    <AuthenticationContext.Provider
      value={{
        checkSessionUri,
        claims,
        isAuthenticated,
        isLoading,
        login,
        logout: logoutWithSessionId,
        sessionExpiry,
        sessionState,
      }}
    >
      {children}
    </AuthenticationContext.Provider>
  );
};
