import React from 'react';

interface ButtonProps {
  type?: 'button' | 'submit' | 'reset';
  children: React.ReactNode;
  loading?: boolean;
  onClick?: () => void;
}

export const Button: React.FC<ButtonProps> = ({
  type = 'button',
  children,
  loading = false,
  onClick,
}) => {
  return (
    <button
      type={type}
      disabled={loading}
      onClick={onClick}
      className="btn-primary"
    >
      {loading ? 'Processing...' : children}
    </button>
  );
};