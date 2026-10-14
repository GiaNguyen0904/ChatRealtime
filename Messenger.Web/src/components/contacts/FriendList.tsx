import { useEffect } from 'react';
import { useFriendStore } from '../../stores/useFriendStore';

export default function FriendList() {
  const { friends, fetchFriends, deleteFriend, isLoading } = useFriendStore();

  useEffect(() => {
    fetchFriends();
  }, [fetchFriends]);

  if (isLoading) return <div className="text-gray-500">Đang tải danh sách...</div>;

  return (
    <div className="max-w-5xl mx-auto">
      <h2 className="text-2xl font-bold text-gray-800 mb-6">Bạn bè ({friends.length})</h2>
      {friends.length === 0 ? (
        <p className="text-gray-500">Chưa có bạn bè nào.</p>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {friends.map((friend) => (
            <div key={friend.friendshipId} className="bg-white p-6 rounded-2xl border border-gray-100 shadow-sm flex flex-col items-center text-center">
              <div className="w-16 h-16 bg-blue-100 text-blue-600 rounded-full flex items-center justify-center text-xl font-bold mb-4">
                {friend.fullName ? friend.fullName.charAt(0).toUpperCase() : 'U'}
              </div>
              <h3 className="font-bold text-gray-800 text-lg">{friend.fullName}</h3>
              <p className="text-gray-500 text-sm mb-6">{friend.email}</p>
              
              <div className="flex gap-2 w-full">
                <button className="flex-1 bg-blue-600 hover:bg-blue-700 text-white py-2 rounded-lg text-sm font-medium transition">
                  💬 Nhắn tin
                </button>
                <button 
                  onClick={() => deleteFriend(friend.friendId)}
                  className="flex-1 bg-red-50 hover:bg-red-100 text-red-600 py-2 rounded-lg text-sm font-medium transition"
                >
                  Xóa bạn
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}