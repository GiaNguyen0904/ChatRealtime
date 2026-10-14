import React from 'react';

interface AuthCardProps {
  title: string;
  children: React.ReactNode;
}

export const AuthCard: React.FC<AuthCardProps> = ({ title, children }) => {
  return (
    <div className="auth-card">
      <div className="avatar-wrapper">
        <svg className="avatar-icon" viewBox="0 0 24 24" fill="currentColor">
          <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z" />
        </svg>
      </div>
      <h2 className="auth-title">{title}</h2>
      {children}
    </div>
  );
};