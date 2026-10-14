import axiosClient from '../api/axiosClient';
import { type FriendDto , type FriendRequestDto,type UserSearchDto } from '../types/friend';

export const friendService = {
  getFriends: () => 
    axiosClient.get<FriendDto[]>('/friends'),

  deleteFriend: (friendId: number) => 
    axiosClient.delete<{ message: string }>(`/friends/${friendId}`),

  getRequests: (type: 'sent' | 'received' = 'received') => 
    axiosClient.get<FriendRequestDto[]>(`/friends/request?type=${type}`),

  sendRequest: (receiverId: number) => 
    axiosClient.post<{ message: string }>('/friends/request', { receiverId }),

  respondRequest: (id: number, status: number) => 
    axiosClient.put<{ message: string }>(`/friends/request/${id}/respond`, { status }),

  searchUsers: (keyword: string) => 
    axiosClient.get<UserSearchDto[]>(`/users/search?keyword=${keyword}`)
};