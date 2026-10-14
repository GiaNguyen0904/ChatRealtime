import { useState } from 'react';
import { useFriendStore } from '../stores/useFriendStore';

export default function SearchUsers() {
  const [keyword, setKeyword] = useState('');
  const { searchResults, searchUsers, sendRequest, isLoading } = useFriendStore();

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    searchUsers(keyword);
  };

  return (
    <div className="max-w-3xl mx-auto">
      <form onSubmit={handleSearch} className="flex gap-2 mb-8">
        <input
          type="text"
          value={keyword}
          onChange={(e) => setKeyword(e.target.value)}
          placeholder="Nhập tên hoặc email người dùng..."
          className="flex-1 border border-gray-300 px-4 py-3 rounded-xl focus:outline-none focus:border-blue-600"
        />
        <button type="submit" className="bg-blue-600 text-white px-6 py-3 rounded-xl font-medium hover:bg-blue-700">
          Tìm kiếm
        </button>
      </form>

      {isLoading ? (
        <p className="text-gray-500">Đang tìm...</p>
      ) : (
        <div className="space-y-4">
          {searchResults.map((user) => (
            <div key={user.id} className="bg-white p-4 rounded-xl border border-gray-100 shadow-sm flex items-center justify-between">
              <div>
                <h4 className="font-bold text-gray-800">{user.fullName}</h4>
                <p className="text-sm text-gray-500">{user.email}</p>
              </div>
              <button
                onClick={() => sendRequest(user.id)}
                className="bg-blue-50 text-blue-600 px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-100"
              >
                + Kết bạn
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}