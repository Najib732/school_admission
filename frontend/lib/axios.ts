import axios from "axios";

declare global {
  interface Window {
    __API_URL__?: string;
  }
}

// The backend address comes from the API_URL environment variable. The root layout reads it on
// the server for every request and puts it on window.__API_URL__, so changing API_URL and
// restarting the container is enough; no rebuild needed.
const api = axios.create({
  headers: { "Content-Type": "application/json" },
  timeout: 15000,
});

api.interceptors.request.use((config) => {
  if (typeof window !== "undefined" && window.__API_URL__) {
    config.baseURL = window.__API_URL__.replace(/\/+$/, "");
  }
  return config;
});

// The student is identified by the X-Student-Id header (no login).
export function studentHeader(studentId: string) {
  return { headers: { "X-Student-Id": studentId } };
}

export default api;
