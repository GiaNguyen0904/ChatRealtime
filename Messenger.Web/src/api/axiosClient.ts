import axios from "axios";
import type { AuthResponseDTO } from "../types/auth";
import { useAuthStore } from "../stores/useAuthStore";
import { signalRService } from "../services/signalRService";

const axiosClient = axios.create({
  baseURL: "https://localhost:7093/api",
  headers: {
    "Content-Type": "application/json",
  },
});

axiosClient.interceptors.request.use(
  (config) => {
    const accessToken = localStorage.getItem("accessToken");
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  },
  (error: unknown) => Promise.reject(error)
);

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (err: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token!);
    }
  });
  failedQueue = [];
};

axiosClient.interceptors.response.use(
  (response) => response,
  async (error: unknown) => {
    if (axios.isAxiosError(error)) {
      const originalRequest = error.config;
      if (!originalRequest) return Promise.reject(error);

      const customConfig = originalRequest as typeof originalRequest & { _retry?: boolean };

      if (error.response?.status === 401 && !customConfig._retry) {
        if (isRefreshing) {
          return new Promise<string>((resolve, reject) => {
            failedQueue.push({ resolve, reject });
          })
            .then((token) => {
              customConfig.headers.Authorization = `Bearer ${token}`;
              return axiosClient(customConfig);
            })
            .catch((err: unknown) => Promise.reject(err));
        }

        customConfig._retry = true;
        isRefreshing = true;

        const refreshToken = localStorage.getItem("refreshToken");

        if (!refreshToken) {
          isRefreshing = false;
          forceLogout();
          return Promise.reject(error);
        }

        try {
          const response = await axios.post<AuthResponseDTO>(
          "https://localhost:7093/api/auth/refresh-token",
          { refreshToken }
        );

          const { accessToken: newAccessToken, refreshToken: newRefreshToken } = response.data;

          localStorage.setItem("accessToken", newAccessToken);
          localStorage.setItem("refreshToken", newRefreshToken);

          // Tự động kết nối lại SignalR với access token mới nếu đang bị gián đoạn
          signalRService.startConnection().catch(() => {});

          customConfig.headers.Authorization = `Bearer ${newAccessToken}`;

          processQueue(null, newAccessToken);
          isRefreshing = false;

          return axiosClient(customConfig);
        } catch (refreshError: unknown) {
          processQueue(refreshError, null);
          isRefreshing = false;
          forceLogout();
          return Promise.reject(refreshError);
        }
      }
    }

    return Promise.reject(error);
  }
);

const forceLogout = () => {
  // Ngắt kết nối SignalR và dọn dẹp state
  signalRService.stopConnection();
  useAuthStore.getState().logout();
  window.location.href = "/login";
};

export default axiosClient;