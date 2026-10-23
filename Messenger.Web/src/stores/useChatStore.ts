import { create } from 'zustand';
import { conversationService, type ConversationDto, type MessageDto, type ConversationMemberDto } from '../services/conversationService';

interface ChatState {
  conversations: ConversationDto[];
  messages: MessageDto[];
  members: ConversationMemberDto[];
  isLoadingConversations: boolean;
  isLoadingMessages: boolean;
  activeConversationId: number | null;
  typingUsers: Record<number, Record<number, boolean>>; // conversationId -> { [userId]: boolean }
  onlineUsers: number[]; // danh sách userId đang online realtime

  setOnlineUsers: (users: number[]) => void;
  setUserOnline: (userId: number) => void;
  setUserOffline: (userId: number) => void;
  fetchConversations: () => Promise<void>;
  fetchMessages: (conversationId: number) => Promise<MessageDto[]>;
  fetchMembers: (conversationId: number) => Promise<void>;
  sendMessage: (conversationId: number, content: string, type?: string) => Promise<void>;
  markAsRead: (conversationId: number, messageId: number) => Promise<void>;
  leaveGroup: (conversationId: number) => Promise<void>;
  transferOwner: (conversationId: number, newOwnerId: number) => Promise<void>;
  inviteMembers: (conversationId: number, memberIds: number[]) => Promise<void>;
  changeNickname: (conversationId: number, targetUserId: number, newNickname: string | null) => Promise<void>;
  changeGroupTitle: (conversationId: number, newTitle: string) => Promise<void>;
  setActiveConversationId: (id: number | null) => void;
  receiveNewMessage: (conversationId: number, message: MessageDto) => void;
  setUserTyping: (conversationId: number, userId: number, isTyping: boolean) => void;
}

const typingTimeouts: Record<string, ReturnType<typeof setTimeout>> = {};

export const useChatStore = create<ChatState>((set, get) => ({
  conversations: [],
  members: [],
  messages: [],
  isLoadingConversations: false,
  isLoadingMessages: false,
  activeConversationId: null,
  typingUsers: {},
  onlineUsers: [],

  setOnlineUsers: (users) => set({ onlineUsers: users }),

  setUserOnline: (userId) =>
    set((state) => {
      if (state.onlineUsers.includes(userId)) return state;
      return { onlineUsers: [...state.onlineUsers, userId] };
    }),

  setUserOffline: (userId) =>
    set((state) => ({
      onlineUsers: state.onlineUsers.filter((id) => id !== userId),
    })),

  fetchConversations: async () => {
    set({ isLoadingConversations: true });
    try {
      const res = await conversationService.getConversations();
      set({ conversations: res.data });
    } catch (error) {
      console.error('Failed to fetch conversations:', error);
    } finally {
      set({ isLoadingConversations: false });
    }
  },

  fetchMessages: async (conversationId) => {
    set({ isLoadingMessages: true, messages: [] });

    try {
      const res = await conversationService.getMessages(conversationId);

      console.log('================================');
      console.log('CONVERSATION ID:', conversationId);
      console.log('MESSAGES FROM API:', res.data.length);
      console.log('MESSAGES:', res.data);
      console.log('================================');

      set({ messages: res.data });

      return res.data;
    } catch (error) {
      console.error('Failed to fetch messages:', error);
      return [];
    } finally {
      set({ isLoadingMessages: false });
    }
  },
  fetchMembers: async (conversationId) => {
    try {
      const res = await conversationService.getMembers(conversationId);
      set({ members: res.data });
    } catch (error) {
      console.error('Failed to fetch members:', error);
      set({ members: [] });
    }
  },

  sendMessage: async (conversationId, content, type = 'TEXT') => {
    try {
      await conversationService.sendMessage(
        conversationId,
        { content, messageType: type }
      );

      await get().fetchConversations();
    } catch (error) {
      console.error('Failed to send message:', error);
    }
  },

  markAsRead: async (conversationId, messageId) => {
    try {
      await conversationService.markAsRead(conversationId, { lastReadMessageId: messageId });
      await get().fetchConversations();
    } catch (error) {
      console.error('Failed to mark as read:', error);
    }
  },

  leaveGroup: async (conversationId) => {
    try {
      await conversationService.leaveGroup(conversationId);
      await get().fetchConversations(); // Reload danh sách chat để ẩn group vừa rời
    } catch (error) {
      console.error('Failed to leave group:', error);
      alert('Không thể rời nhóm. Vui lòng chuyển quyền Trưởng nhóm trước!');
    }
  },

  transferOwner: async (conversationId, newOwnerId) => {
    try {
      await conversationService.transferOwner(conversationId, newOwnerId);
      alert('Chuyển quyền thành công!');
    } catch (error) {
      console.error('Failed to transfer owner:', error);
    }
  },
  inviteMembers: async (conversationId, memberIds) => {
    try {
      await conversationService.inviteMembers(conversationId, { memberIds });
      await get().fetchMembers(conversationId); // Lấy lại danh sách member mới nhất
    } catch (error) {
      console.error('Failed to invite members:', error);
      alert('Có lỗi xảy ra hoặc bạn không có quyền thêm thành viên!');
    }
  },

  changeNickname: async (conversationId, targetUserId, newNickname) => {
    try {
      await conversationService.changeNickname(conversationId, { targetUserId, newNickname });
      await get().fetchMembers(conversationId); // Gọi lại API để load biệt danh mới
      await get().fetchConversations();
    } catch (error) {
      console.error('Failed to change nickname:', error);
      alert('Có lỗi xảy ra khi đổi biệt danh!');
    }
  },

  changeGroupTitle: async (conversationId: number, newTitle: string) => {
    try {
      await conversationService.changeGroupTitle(conversationId, { newTitle });
      await get().fetchConversations(); // Load lại sidebar để thấy tên mới
    } catch (error) {
      console.error('Failed to change group title', error);
    }
  },


  setActiveConversationId: (id) => set({ activeConversationId: id }),

  receiveNewMessage: (conversationId, message) => {
    const state = get();

    if (message.senderId) {
      get().setUserTyping(conversationId, Number(message.senderId), false);
    }

    const isMatchingChat =
      Number(state.activeConversationId) === Number(conversationId);

    if (isMatchingChat) {
      const isExist = state.messages.some(
        (m) => Number(m.id) === Number(message.id)
      );

      if (!isExist) {
        set((state) => ({
          messages: [...state.messages, message]
        }));

        conversationService
          .markAsRead(conversationId, {
            lastReadMessageId: message.id
          })
          .then(() => get().fetchConversations());
      }
    } else {
      state.fetchConversations();
    }
  },

  setUserTyping: (conversationId, userId, isTyping) => {
    const key = `${conversationId}_${userId}`;
    if (typingTimeouts[key]) {
      clearTimeout(typingTimeouts[key]);
      delete typingTimeouts[key];
    }

    set((state) => {
      const currentConvTyping = { ...(state.typingUsers[conversationId] || {}) };
      if (isTyping) {
        currentConvTyping[userId] = true;
      } else {
        delete currentConvTyping[userId];
      }
      return {
        typingUsers: {
          ...state.typingUsers,
          [conversationId]: currentConvTyping,
        },
      };
    });

    if (isTyping) {
      typingTimeouts[key] = setTimeout(() => {
        get().setUserTyping(conversationId, userId, false);
      }, 4000);
    }
  },
}));

