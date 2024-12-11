import { useCallback, useState } from "react";

import "./App.css";
import { useAuthentication } from "./AuthenticationContext";

function App() {
  const {
    checkSessionUri,
    claims,
    isAuthenticated,
    login,
    logout,
    sessionExpiry,
    sessionState,
  } = useAuthentication();
  const [apiResult, setApiResult] = useState("");
  const [bffResult, setBffResult] = useState("");
  const [providerApiResult, setProviderApiResult] = useState("");
  const callApi = useCallback(
    async (url: string, setResult: (result: string) => void) => {
      try {
        const response = await fetch(url);
        if (response.ok) {
          setResult(await response.text());
        } else {
          setResult(`${response.status} ${response.statusText}`);
        }
      } catch (error) {
        setResult(JSON.stringify(error));
      }
    },
    []
  );
  return (
    <div className="container960">
      <div className="card">
        <div className="card-header d-flex align-items-center justify-content-between">
          <a className="nav-link" href="http://localhost:5105">
            <h3>InHouseOidc.Example.React</h3>
          </a>
          <div style={{ display: "flex", flexDirection: "row", gap: "15px" }}>
            <button onClick={() => login("/")} disabled={isAuthenticated}>
              Login
            </button>
            <button onClick={logout} disabled={!isAuthenticated}>
              Logout
            </button>
          </div>
        </div>
        {isAuthenticated && (
          <div className="card-body">
            <table>
              <thead>
                <tr>
                  <th>Check session URI</th>
                  <th>Session expiry</th>
                  <th>Session state</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>{checkSessionUri ?? ""}</td>
                  <td>{(sessionExpiry ?? "").toString()}</td>
                  <td>{sessionState ?? ""}</td>
                </tr>
              </tbody>
            </table>
            <div className="d-flex mt-2 align-items-center">
              <button onClick={() => callApi("/api/secure-bff", setBffResult)}>
                Call BFF
              </button>
              <span className="ml-2">{bffResult}</span>
            </div>
            <div className="d-flex mt-2 align-items-center">
              <button onClick={() => callApi("/api/secure-api", setApiResult)}>
                Call API
              </button>
              <span className="ml-2">{apiResult}</span>
            </div>
            <div className="d-flex mt-2 align-items-center">
              <button
                onClick={() =>
                  callApi("/api/secure-provider", setProviderApiResult)
                }
              >
                Call Provider API
              </button>
              <span className="ml-2">{providerApiResult}</span>
            </div>
          </div>
        )}
      </div>
      {isAuthenticated && (
        <div className="card mt-2">
          <div className="card-header">
            <h4 className="m-0">Claims</h4>
          </div>
          <div className="card-body">
            <div className="card">
              <table>
                <thead>
                  <tr>
                    <th>Type</th>
                    <th>Value</th>
                  </tr>
                </thead>
                <tbody>
                  {claims?.map((claim, index) => (
                    <tr key={`claim-${index}`}>
                      <td>{claim.type}</td>
                      <td>{claim.value}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default App;
