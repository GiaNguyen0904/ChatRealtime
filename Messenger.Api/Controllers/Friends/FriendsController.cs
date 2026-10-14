using Messenger.Application.Friends.DTOs;
using Messenger.Application.Friends.Interfaces;
using Messenger.Application.Friends.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
namespace Messenger.Api.Controllers.Friends
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FriendsController : ControllerBase
    {
        private readonly IFriendRequestService _friendRequestService;
        private readonly IFriendshipService _friendShipService;

        public FriendsController(IFriendRequestService friendRequestService,
            IFriendshipService friendshipService)
        {
            _friendRequestService = friendRequestService;
            _friendShipService = friendshipService;
        }
        //lay id cua user hien tai tu token
        private int getCurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); // 

        // Friend Request
        [HttpPost("request")]
        public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto requestDto)
        {
            await _friendRequestService.SendRequestAsync(getCurrentUserId(), requestDto);
            return Ok(new { message = "Friend request sent successfully" });
        }

        [HttpGet("request")]
        public async Task<IActionResult> GetRequest([FromQuery] string type = "received")
        {
            var result = await _friendRequestService.GetRequestAsync(getCurrentUserId(), type);
            return Ok(result);
        }

        [HttpPut("request/{id}/respond")]
        public async Task<IActionResult> RespondRequest(int id, [FromBody] RespondFriendRequestDto requestDto)
        {
            await _friendRequestService.RespondRequestAsync(getCurrentUserId(), id, requestDto);
            return Ok(new { message = "Friend request responded successfully" });
        }

        // Friendship
        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            var result = await _friendShipService.GetFriendsAsync(getCurrentUserId());
            return Ok(result);
        }

        [HttpDelete("{friendId}")]
        public async Task<IActionResult> DeleteFriend(int friendId)
        {
            await _friendShipService.DeleteFriendAsync(getCurrentUserId(), friendId);
            return Ok(new { message = " Friend deleted successfully" });
        }

    }
}
