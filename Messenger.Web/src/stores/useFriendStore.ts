import { create } from 'zustand';
import { friendService } from '../services/friendService';
import type { FriendDto, FriendRequestDto, UserSearchDto } from '../types/friend';

interface FriendState {
  friends: FriendDto[];
  requests: FriendRequestDto[];
  searchResults: UserSearchDto[];
  sentRequestIds: number[]; // Lưu ID những người đã gửi lời mời
  isLoading: boolean;
  fetchFriends: () => Promise<void>;
  fetchRequests: (type: 'sent' | 'received') => Promise<void>;
  deleteFriend: (friendId: number) => Promise<void>;
  searchUsers: (keyword: string) => Promise<void>;
  sendRequest: (receiverId: number) => Promise<void>;
  respondRequest: (requestId: number, status: number) => Promise<void>;
  clearSearch: () => void;
}

export const useFriendStore = create<FriendState>((set, get) => ({
  friends: [],
  requests: [],
  searchResults: [],
  sentRequestIds: [],
  isLoading: false,

  fetchFriends: async () => {
    try {
      const res = await friendService.getFriends();
      set({ friends: res.data });
    } catch {
      // Handle error
    }
  },

  fetchRequests: async (type) => {
    try {
      const res = await friendService.getRequests(type);
      if (type === 'sent') {
        // Đồng thời cập nhật danh sách ID đã gửi từ server
        const sentIds = res.data.map((r: FriendRequestDto) => r.receiverId);
        set({ requests: res.data, sentRequestIds: sentIds });
      } else {
        set({ requests: res.data });
      }
    } catch {
      // Handle error
    }
  },

  deleteFriend: async (friendId) => {
    try {
      await friendService.deleteFriend(friendId);
      await get().fetchFriends();
    } catch {
      // Handle error
    }
  },

  searchUsers: async (keyword) => {
    if (!keyword.trim()) {
      set({ searchResults: [] });
      return;
    }
    set({ isLoading: true });
    try {
      const res = await friendService.searchUsers(keyword);
      set({ searchResults: res.data, isLoading: false });
    } catch {
      set({ isLoading: false });
    }
  },

  sendRequest: async (receiverId) => {
    try {
      await friendService.sendRequest(receiverId);
      // Cập nhật ngay lập tức mảng sentRequestIds để đổi trạng thái nút thành "Đã gửi"
      set((state) => ({
        sentRequestIds: [...state.sentRequestIds, receiverId]
      }));
    } catch {
      // Handle error (ví dụ: đã tồn tại request hoặc conflict)
    }
  },

  respondRequest: async (requestId, status) => {
    try {
      await friendService.respondRequest(requestId, status);
      await get().fetchRequests('received');
      await get().fetchFriends(); // Nếu accept thì cập nhật lại danh sách bạn bè luôn
    } catch {
      // Handle error
    }
  },

  clearSearch: () => set({ searchResults: [] })
}));