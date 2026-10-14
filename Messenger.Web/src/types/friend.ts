export interface FriendDto {
  friendshipId: number;
  friendId: number;
  fullName: string;
  email: string;
  createdAt: string;
}

export interface FriendRequestDto {
  id: number;
  senderId: number;
  senderFullName: string;
  senderEmail: string;
  receiverId: number;
  receiverFullName: string;
  receiverEmail: string;
  status: number | string; // Pending: 0, Accepted: 1, Rejected: 2
  createdAt: string;
}

export interface UserSearchDto {
  id: number;
  fullName: string;
  email: string;
}