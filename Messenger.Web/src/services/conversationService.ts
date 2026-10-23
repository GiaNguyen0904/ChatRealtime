import axiosClient from '../api/axiosClient';

// --- DTO Interfaces ---
export interface ConversationDto {
  id: number;
  type: 'PRIVATE' | 'GROUP';
  title: string | null;
  updatedAt: string;
  unreadCount: number;
  otherUserId?: number;
}

export interface SeenByDto {
  userId: number;
  fullName: string;
}

export interface MessageDto {
  id: number;
  senderId: number;
  content: string;
  messageType: string;
  createdAt: string;
  seenBy: SeenByDto[];
}

export interface SendMessageDto {
  content: string;
  messageType: string;
}

export interface CreateGroupDto {
  title: string;
  memberIds: number[];
}

export interface MarkReadDto {
  lastReadMessageId: number;
}
export interface InviteMembersDto { memberIds: number[]; }
export interface ConversationMemberDto {
  userId: number;
  fullName: string;
  role: string;
  nickname? : string;
}

export interface ChangeNicknameDto {
  targetUserId: number;
  newNickname: string | null;
}

export interface ChangeGroupTitleDto { 
  newTitle: string; 
}

// --- Service Methods ---
export const conversationService = {
  getConversations: () => 
    axiosClient.get<ConversationDto[]>('/Conversations'),

  openPrivateChat: (friendId: number) => 
    axiosClient.post<{ conversationId: number }>('/Conversations/private', friendId),

  createGroupChat: (data: CreateGroupDto) => 
    axiosClient.post<{ conversationId: number }>('/Conversations/group', data),

  getMessages: (conversationId: number, page: number = 1, pageSize: number = 20) => 
    axiosClient.get<MessageDto[]>(`/Conversations/${conversationId}/messages?page=${page}&pageSize=${pageSize}`),

  sendMessage: (conversationId: number, data: SendMessageDto) => 
    axiosClient.post<MessageDto>(`/Conversations/${conversationId}/messages`, data),

  markAsRead: (conversationId: number, data: MarkReadDto) => 
    axiosClient.post(`/Conversations/${conversationId}/read`, data),
  leaveGroup: (conversationId : number) =>
    axiosClient.post(`/Conversations/${conversationId}/leave`),

  transferOwner: ( conversationId: number, newOwnerId: number ) => 
    axiosClient.put( `/Conversations/${conversationId}/transfer-owner`, newOwnerId ),
  
  inviteMembers: ( conversationId: number, data: InviteMembersDto ) => 
    axiosClient.post( `/Conversations/${conversationId}/members`, data ),
  getMembers: (conversationId: number) => 
    axiosClient.get<ConversationMemberDto[]>(`/Conversations/${conversationId}/members`),
  changeNickname: (conversationId : number , data : ChangeNicknameDto) =>
    axiosClient.put(`/Conversations/${conversationId}/nickname`, data),
  changeGroupTitle: (conversationId: number, data: ChangeGroupTitleDto) => 
    axiosClient.put(`/Conversations/${conversationId}/title`, data),

};