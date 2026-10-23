import * as signalR from "@microsoft/signalr";
import { useChatStore } from "../stores/useChatStore";
import { type MessageDto } from "../services/conversationService";

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private retryTimeout: ReturnType<typeof setTimeout> | null = null;
  private isStoppedManually: boolean = false;

  private scheduleRetry = (delayMs = 3000) => {
    if (this.isStoppedManually) return;
    if (this.retryTimeout) return;

    this.retryTimeout = setTimeout(() => {
      this.retryTimeout = null;
      if (!this.isStoppedManually) {
        console.log(`[SignalR] Retrying connection after ${delayMs}ms...`);
        this.startConnection().catch(() => {});
      }
    }, delayMs);
  };

  public startConnection = async () => {
    this.isStoppedManually = false;

    // Hủy bỏ retry timer nếu đang chờ
    if (this.retryTimeout) {
      clearTimeout(this.retryTimeout);
      this.retryTimeout = null;
    }

    // Đã có connection đang hoạt động
    if (this.connection) {
      const state = this.connection.state;

      if (
        state === signalR.HubConnectionState.Connected ||
        state === signalR.HubConnectionState.Connecting ||
        state === signalR.HubConnectionState.Reconnecting
      ) {
        return;
      }
    }

    // Đã có một startConnection() khác đang chạy
    if (this.startPromise) {
      return this.startPromise;
    }

    const token = localStorage.getItem("accessToken");

    if (!token) {
      console.warn("[SignalR] No access token. Connection skipped.");
      return;
    }

    const HUB_URL = "https://localhost:7093/hubs/chat";

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => localStorage.getItem("accessToken") || "",
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount < 3) return 2000;
          if (retryContext.previousRetryCount < 6) return 5000;
          return 10000;
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connection.on(
      "ReceiveNewMessage",
      (conversationId: number, message: MessageDto) => {
        useChatStore
          .getState()
          .receiveNewMessage(conversationId, message);
      }
    );

    connection.on(
      "UserTyping",
      (conversationId: number, userId: number, isTyping: boolean) => {
        useChatStore
          .getState()
          .setUserTyping(conversationId, userId, isTyping);
      }
    );

    connection.on("GetOnlineUsers", (users: number[]) => {
      useChatStore.getState().setOnlineUsers(users);
    });

    connection.on("UserIsOnline", (userId: number) => {
      useChatStore.getState().setUserOnline(userId);
    });

    connection.on("UserIsOffline", (userId: number) => {
      useChatStore.getState().setUserOffline(userId);
    });

    // Xử lý khi kết nối bị đóng hoàn toàn
    connection.onclose((error) => {
      console.warn("[SignalR] Connection closed:", error);
      if (this.connection === connection) {
        this.connection = null;
      }
      if (!this.isStoppedManually) {
        this.scheduleRetry(3000);
      }
    });

    this.connection = connection;

    this.startPromise = connection
      .start()
      .then(() => {
        console.log("[SignalR] Connected successfully.");
      })
      .catch((err) => {
        console.error("[SignalR] Connection start failed:", err);
        if (this.connection === connection) {
          this.connection = null;
        }
        // Tự động thử lại nếu không phải do người dùng cố tình disconnect
        if (!this.isStoppedManually) {
          this.scheduleRetry(3000);
        }
        throw err;
      })
      .finally(() => {
        this.startPromise = null;
      });

    return this.startPromise;
  };

  public sendTyping = async (conversationId: number, isTyping: boolean) => {
    if (
      this.connection &&
      this.connection.state === signalR.HubConnectionState.Connected
    ) {
      try {
        await this.connection.invoke("SendTyping", conversationId, isTyping);
      } catch (err) {
        console.error("SignalR SendTyping Error: ", err);
      }
    }
  };

  public stopConnection = async () => {
    this.isStoppedManually = true;

    if (this.retryTimeout) {
      clearTimeout(this.retryTimeout);
      this.retryTimeout = null;
    }

    const connection = this.connection;
    this.connection = null;

    if (!connection) {
      return;
    }

    try {
      await connection.stop();
      console.log("[SignalR] Disconnected successfully.");
    } catch (err) {
      console.error("SignalR Disconnect Error: ", err);
    }
  };
}

export const signalRService = new SignalRService();