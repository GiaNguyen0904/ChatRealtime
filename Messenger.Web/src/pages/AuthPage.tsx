import React, { useState } from 'react';
import { isAxiosError } from 'axios';
import { useNavigate } from 'react-router-dom';
import { AuthLayout } from '../layouts/AuthLayout';
import { InputGroup } from '../components/common/InputGroup';
import { useAuthStore } from '../stores/useAuthStore';

export const AuthPage: React.FC = () => {
  const [isLogin, setIsLogin] = useState(true);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fullname, setFullName] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const navigate = useNavigate();
  // Lấy hàm login, register và isLoading trực tiếp từ Zustand
  const { login, register, isLoading } = useAuthStore();

  const handleToggleMode = () => {
    setIsLogin((prev) => !prev);
    setEmail('');
    setPassword('');
    setFullName('');
    setError(null);
  };

const handleSubmit = async (e: React.FormEvent) => {
  e.preventDefault();
  setError(null);

  try {
    if (isLogin) {
      await login({ email, password });
    } else {
      await register({ email, password, fullname });
    }
    // Chuyển hướng sang ChatPage
    navigate('/chat', { replace: true });
  } catch (err: unknown) {
    if (isAxiosError(err)) {
      setError(err.response?.data?.message || 'Có lỗi xảy ra, vui lòng thử lại.');
    } else {
      setError('Có lỗi không xác định xảy ra.');
    }
  }
};

  return (
    <AuthLayout>
      <div className="auth-card-dark">
        {/* Icon User */}
        <div className="user-icon-wrapper">
          <svg className="user-avatar-svg" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z"/>
          </svg>
        </div>

        {error && <div className="error-box-dark">{error}</div>}

        <form onSubmit={handleSubmit}>
          {!isLogin && (
            <InputGroup
              type="text"
              placeholder="Full Name"
              value={fullname}
              onChange={(e) => setFullName(e.target.value)}
              icon="👤"
            />
          )}

          <InputGroup
            type="email"
            placeholder="Email ID"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            icon="👤"
          />

          <InputGroup
            type="password"
            placeholder="Password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            icon="🔒"
          />

          <div className="options-row">
            {isLogin ? (
              <label className="remember-label">
                <input
                  type="checkbox"
                  checked={rememberMe}
                  onChange={(e) => setRememberMe(e.target.checked)}
                />
                Remember me
              </label>
            ) : (
              <div></div>
            )}
            <a href="#" className="forgot-link-dark">Forgot Password?</a>
          </div>

          <button type="submit" disabled={isLoading} className="btn-teal">
            {isLoading ? 'PROCESSING...' : isLogin ? 'LOGIN' : 'REGISTER'}
          </button>
        </form>
      </div>

      <div className="toggle-card-block">
        <button type="button" onClick={handleToggleMode} className="toggle-card-btn">
          {isLogin ? 'REGISTER' : 'LOGIN'}
        </button>
      </div>
    </AuthLayout>
  );
};