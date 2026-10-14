import { create } from "zustand";
import { AuthService } from "../services/authService";
import type { UserDTO, LoginRequestDTO, RegisterRequestDTO } from "../types/auth";
import { signalRService } from "../services/signalRService";

interface AuthState {
  user: UserDTO | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  
  // Actions
  login: (data: LoginRequestDTO) => Promise<void>;
  register: (data: RegisterRequestDTO) => Promise<void>;
  logout: () => Promise<void>;
  initAuth: () => void;
}

const getUserIdFromToken = (token: string): number => {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));
    
    const payload = JSON.parse(jsonPayload);
    const idStr = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || payload['sub'] || payload['id'];
    return Number(idStr) || 0;
  } catch {
    return 0;
  }
};

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: !!localStorage.getItem("accessToken"),
  isLoading: true,

  // Khôi phục trạng thái khi F5 lại trang
  initAuth: () => {
    const savedUser = localStorage.getItem("user");
    const accessToken = localStorage.getItem("accessToken");

    if (savedUser && accessToken) {
      try {
        set({ user: JSON.parse(savedUser), isAuthenticated: true, isLoading: false });
        return;
      } catch {
        localStorage.removeItem("user");
      }
    }
    set({ user: null, isAuthenticated: false, isLoading: false });
  },

  

  login: async (data: LoginRequestDTO) => {
    set({ isLoading: true });
    try {
      const res = await AuthService.login(data);
      
      localStorage.setItem("accessToken", res.accessToken);
      localStorage.setItem("refreshToken", res.refreshToken);

      // Lấy ID thật từ token thay vì để id: 0
      const realUserId = getUserIdFromToken(res.accessToken);

      const tempUser: UserDTO = { id: realUserId, email: data.email, fullName: data.email };
      localStorage.setItem("user", JSON.stringify(tempUser));

      set({ user: tempUser, isAuthenticated: true });

      // Tự động kết nối SignalR sau khi đăng nhập thành công
      signalRService.startConnection().catch(() => {});
    } finally {
      set({ isLoading: false });
    }
  },

  register: async (data: RegisterRequestDTO) => {
    set({ isLoading: true });
    try {
      const res = await AuthService.register(data);
      
      localStorage.setItem("accessToken", res.accessToken);
      localStorage.setItem("refreshToken", res.refreshToken);

      // Lấy ID thật từ token thay vì để id: 0
      const realUserId = getUserIdFromToken(res.accessToken);

      const tempUser: UserDTO = { id: realUserId, email: data.email, fullName: data.fullname };
      localStorage.setItem("user", JSON.stringify(tempUser));

      set({ user: tempUser, isAuthenticated: true });

      // Tự động kết nối SignalR sau khi đăng ký thành công
      signalRService.startConnection().catch(() => {});
    } finally {
      set({ isLoading: false });
    }
  },

  logout: async () => {
    set({ isLoading: true });
    const refreshToken = localStorage.getItem("refreshToken");

    // Dừng kết nối SignalR trước khi xóa token
    signalRService.stopConnection();

    try {
      if (refreshToken) {
        await AuthService.logout({ refreshToken });
      }
    } catch (error: unknown) {
      console.error("Lỗi khi gọi Logout API ở Backend:", error);
    } finally {
      localStorage.removeItem("accessToken");
      localStorage.removeItem("refreshToken");
      localStorage.removeItem("user");
      set({ user: null, isAuthenticated: false, isLoading: false });
    }
  },
}));