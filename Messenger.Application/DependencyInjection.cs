using Messenger.Application.Auth.Interfaces;
using Messenger.Application.Auth.Services;
using Messenger.Application.Users.Services;
using Messenger.Application.Users.Interfaces;
using Messenger.Application.Friends.Interfaces;
using Messenger.Application.Friends.Services;
using Messenger.Application.Conversations.Interfaces;
using Messenger.Application.Conversations.Services;
using Microsoft.Extensions.DependencyInjection;


namespace Messenger.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>(); // dang ky dich vu AuthService voi IAuthService
        services.AddScoped<IUserService, UserService>();// dang ky User Service voi IUserService
        services.AddScoped<IFriendshipService, FriendshipService>();
        services.AddScoped<IFriendRequestService, FriendRequestService>();
        services.AddScoped<IConversationService, ConversationService>();

        return services;
    }
}