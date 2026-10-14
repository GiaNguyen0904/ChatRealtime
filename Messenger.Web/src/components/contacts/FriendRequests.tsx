import { useEffect, useState } from 'react';
import { useFriendStore } from '../../stores/useFriendStore';

export default function FriendRequests() {
  const [tab, setTab] = useState<'received' | 'sent'>('received');
  const { requests, fetchRequests, respondRequest, isLoading } = useFriendStore();

  useEffect(() => {
    fetchRequests(tab);
  }, [tab, fetchRequests]);

  return (
    <div className="w-full">
      <div className="flex gap-4 mb-6 border-b border-gray-200 pb-2">
        <button
          onClick={() => setTab('received')}
          className={`pb-2 font-medium ${tab === 'received' ? 'border-b-2 border-blue-600 text-blue-600' : 'text-gray-500'}`}
        >
          Lời mời đã nhận
        </button>
        <button
          onClick={() => setTab('sent')}
          className={`pb-2 font-medium ${tab === 'sent' ? 'border-b-2 border-blue-600 text-blue-600' : 'text-gray-500'}`}
        >
          Lời mời đã gửi
        </button>
      </div>

      {isLoading ? (
        <p className="text-gray-500 text-xs">Đang tải...</p>
      ) : requests.length === 0 ? (
        <p className="text-gray-500 text-xs">Không có lời mời nào.</p>
      ) : (
        <div className="space-y-3">
          {requests.map((req) => (
            <div key={req.id} className="bg-white p-4 rounded-xl border border-gray-100 shadow-sm flex items-center justify-between">
              <div>
                <h4 className="font-bold text-gray-800 text-sm">
                  {tab === 'received' ? req.senderFullName : req.receiverFullName}
                </h4>
                <p className="text-xs text-gray-500">
                  {tab === 'received' ? req.senderEmail : req.receiverEmail}
                </p>
              </div>

              {tab === 'received' && (
                <div className="flex gap-2">
                  <button
                    onClick={() => respondRequest(req.id, 1)}
                    className="bg-blue-600 text-white px-4 py-2 rounded-lg text-xs font-medium hover:bg-blue-700 transition"
                  >
                    Chấp nhận
                  </button>
                  <button
                    onClick={() => respondRequest(req.id, 2)}
                    className="bg-gray-100 text-gray-600 px-4 py-2 rounded-lg text-xs font-medium hover:bg-gray-200 transition"
                  >
                    Từ chối
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}