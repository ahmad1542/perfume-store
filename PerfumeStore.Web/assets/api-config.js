(function () {
  const DEFAULT_LOCAL_API = "https://localhost:7209/api";

  function getApiBaseUrl() {
    // 1) allow manual override from window
    if (window.__API_BASE_URL__) {
      return window.__API_BASE_URL__.replace(/\/$/, "");
    }

    // 2) if frontend and backend are on same host but different path via reverse proxy
    // example: https://mydomain.com/api
    if (window.location.protocol !== "file:" && window.location.port !== "5500") {
      return `${window.location.origin}/api`;
    }

    // 3) fallback for local development
    return DEFAULT_LOCAL_API;
  }

  window.APP_CONFIG = {
    API_BASE_URL: getApiBaseUrl()
  };
})();
