import React from 'react';

interface InputGroupProps {
  type: string;
  placeholder: string;
  value: string;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  icon: React.ReactNode;
  required?: boolean;
}

export const InputGroup: React.FC<InputGroupProps> = ({
  type,
  placeholder,
  value,
  onChange,
  icon,
  required = true,
}) => {
  return (
    <div className="custom-input-group">
      <div className="custom-input-icon">{icon}</div>
      <input
        type={type}
        placeholder={placeholder}
        value={value}
        onChange={onChange}
        required={required}
        className="custom-input-field"
      />
    </div>
  );
};