export interface LoginRequestDTO {
  email: string;
  password: string;
}

export interface RegisterRequestDTO {
  email: string;
  password: string;
  fullname: string;
}

// Khớp chính xác với AuthResponseDto từ C#
export interface AuthResponseDTO {
  accessToken: string;
  expiresAt: string;
  refreshToken: string;
}

export interface RefreshTokenRequestDto {
  refreshToken: string;
}

export interface LogoutRequestDto {
  refreshToken: string;
}

export interface UserDTO {
  id: number;
  email: string;
  fullName: string;
}