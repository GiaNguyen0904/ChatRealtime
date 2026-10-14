import React, { useState, useEffect } from 'react';
import { useFriendStore } from '../../stores/useFriendStore';

export const SearchUsers: React.FC = () => {
  const [keyword, setKeyword] = useState('');
  const { 
    searchResults, 
    searchUsers, 
    sendRequest, 
    friends, 
    fetchFriends, 
    sentRequestIds, 
    fetchRequests, 
    isLoading,
    clearSearch
  } = useFriendStore();

  useEffect(() => {
    fetchFriends();
    fetchRequests('sent');
    return () =>{
        clearSearch();
    };
  }, [fetchFriends, fetchRequests, clearSearch]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    searchUsers(keyword);
  };

  return (
    <div className="w-full">
      {/* Thanh tìm kiếm giữ nguyên khung giới hạn và căn giữa */}
      <div className="max-w-3xl mx-auto">
        <form onSubmit={handleSearch} className="flex gap-2 mb-8">
          <input
            type="text"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
            placeholder="Nhập tên hoặc email người dùng..."
            className="flex-1 border border-gray-300 px-4 py-3 rounded-xl focus:outline-none focus:border-blue-600 text-xs bg-white text-gray-800 shadow-sm"
          />
          <button type="submit" className="bg-blue-600 text-white px-6 py-3 rounded-xl font-medium text-xs hover:bg-blue-700 transition shadow-sm">
            Tìm kiếm
          </button>
        </form>
      </div>

      {/* Danh sách kết quả tìm kiếm trải rộng full width ra sát ngoài */}
      {isLoading ? (
        <p className="text-gray-500 text-xs">Đang tìm...</p>
      ) : (
        <div className="w-full space-y-3">
          {searchResults.map((user) => {
            const isFriend = friends.some((f) => f.friendId === user.id);
            const isRequested = sentRequestIds.includes(user.id);

            return (
              <div key={user.id} className="bg-white p-4 rounded-xl border border-gray-100 shadow-sm flex items-center justify-between w-full">
                <div>
                  <h4 className="font-bold text-gray-800 text-sm">{user.fullName}</h4>
                  <p className="text-xs text-gray-500">{user.email}</p>
                </div>

                {isFriend ? (
                  <button
                    disabled
                    className="bg-gray-100 text-gray-500 px-4 py-2 rounded-lg text-xs font-semibold cursor-not-allowed border border-gray-200"
                  >
                    ✓ Bạn bè
                  </button>
                ) : isRequested ? (
                  <button
                    disabled
                    className="bg-gray-100 text-gray-400 px-4 py-2 rounded-lg text-xs font-medium cursor-not-allowed border border-gray-200"
                  >
                    Đã gửi lời mời
                  </button>
                ) : (
                  <button
                    onClick={() => sendRequest(user.id)}
                    className="bg-blue-50 text-blue-600 px-4 py-2 rounded-lg text-xs font-medium hover:bg-blue-100 transition"
                  >
                    + Kết bạn
                  </button>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};