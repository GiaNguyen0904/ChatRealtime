import { useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthPage } from './pages/AuthPage';
import { ChatPage } from './pages/ChatPage';
import ContactsPage from './pages/ContactsPage'; // Thêm import ContactsPage
import { ProtectedRoute } from './routes/ProtectedRoute';
import { useAuthStore } from './stores/useAuthStore';

function App() {
  useEffect(() => {
    // Gọi trực tiếp hàm từ Zustand Store để khởi tạo Auth state khi app mount
    useAuthStore.getState().initAuth();
  }, []); // Mảng dependency rỗng tuyệt đối an toàn

  return (
    <BrowserRouter>
      <Routes>
        {/* Public Routes */}
        <Route path="/auth" element={<AuthPage />} />
        <Route path="/login" element={<AuthPage />} />

        {/* Protected Routes (Yêu cầu đăng nhập mới được truy cập) */}
        <Route element={<ProtectedRoute />}>
          <Route path="/chat" element={<ChatPage />} />
          <Route path="/contacts" element={<ContactsPage />} />
        </Route>

        {/* Mặc định chuyển hướng về /chat nếu route không tồn tại */}
        <Route path="*" element={<Navigate to="/chat" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;