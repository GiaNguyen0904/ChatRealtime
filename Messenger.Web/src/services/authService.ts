import axiosClient from "../api/axiosClient";
import {type LoginRequestDTO, type RegisterRequestDTO, type AuthResponseDTO, type LogoutRequestDto } from "../types/auth";

export const AuthService = {
    login : async (data: LoginRequestDTO) : Promise<AuthResponseDTO> => {
        const response = await axiosClient.post<AuthResponseDTO>('/auth/login', data);
        return response.data;
    },
    register : async (data: RegisterRequestDTO) : Promise<AuthResponseDTO> => {
        const response = await axiosClient.post<AuthResponseDTO>('/auth/register', data);
        return response.data;
    },
    logout : async (data : LogoutRequestDto) : Promise<void> =>{
        await axiosClient.post('/auth/logout',data);
    }
};

