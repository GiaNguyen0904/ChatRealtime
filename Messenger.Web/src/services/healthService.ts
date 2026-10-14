import axiosClient from "../api/axiosClient";

export interface HealthResponse {
  status: string;
  message: string;
}

export const getHealth = async (): Promise<HealthResponse> => {
  const response = await axiosClient.get<HealthResponse>("/health");

  return response.data;
};