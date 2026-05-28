using Infra.AWS.CloudWatch;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Modules.Notification.Application.Commands;
using Modules.Notification.Application.DTOs.Request;
using Modules.Notification.Application.DTOs.Response;
using Modules.Notification.Application.Queries;
using Modules.Notification.Application.Contracts;

namespace Modules.Notification.Api.Controllers;

[ApiController]
[EnableRateLimiting("notification")]
[Route("api/[controller]")]
[Authorize]
public class NotificationController(
    IMediator mediator,
    INotificationService notificationService,
    ICloudWatchService cloudWatch,
    ILogger<NotificationController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly INotificationService _notificationService = notificationService;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;
    private readonly ILogger<NotificationController> _logger = logger;

    #region SSE Endpoints

    /// <summary>
    /// Subscribe to push notifications via Server-Sent Events (SSE)
    /// This endpoint keeps an open connection and streams notifications to the client in real-time
    /// </summary>
    [Authorize(Policy = "SellerOrCustomer")]
    [HttpGet("subscribe")]
    [AllowAnonymous]
    public async Task Subscribe(Guid userId, CancellationToken cancellationToken)
    {
        // Configure response for SSE
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var writer = new StreamWriter(Response.Body)
        {
            AutoFlush = true
        };

        // Register user connection
        _notificationService.RegisterUserConnection(userId, writer);
        await _cloudWatch.PutMetricAsync("notification.sse.connected", 1, "Count");

        try
        {
            // Keep connection alive
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(30000, cancellationToken); // Keep-alive ping every 30 seconds
                await writer.WriteLineAsync($": keep-alive ping at {DateTime.UtcNow:O}");
                await writer.FlushAsync();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE connection cancelled for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE subscription for user {UserId}", userId);
        }
        finally
        {
            _notificationService.UnregisterUserConnection(userId);
            await writer.FlushAsync();
            writer.Dispose();
        }
    }

    #endregion

    #region Send Notification Endpoints

    /// <summary>
    /// Send a notification to a single user
    /// </summary>
    [HttpPost("send")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendNotification(
        [FromBody] SendNotificationRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new SendNotificationCommand(request);
            var notificationId = await _mediator.Send(command, cancellationToken);
            return Ok(new { notificationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Broadcast notification to multiple users
    /// </summary>
    [HttpPost("broadcast")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BroadcastNotification(
        [FromBody] BroadcastNotificationRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new BroadcastNotificationCommand(request);
            var notificationIds = await _mediator.Send(command, cancellationToken);
            return Ok(new { notificationIds });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting notification");
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Retrieve Notifications Endpoints

    /// <summary>
    /// Get all unread notifications for the current user
    /// </summary>
    [Authorize(Policy = "SellerOrCustomer")]
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetUnreadNotificationsQuery(userId);
            var notifications = await _mediator.Send(query, cancellationToken);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread notifications");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get notifications for user with pagination
    /// </summary>
    [Authorize(Policy = "SellerOrCustomer")]
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserNotifications(
        Guid userId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetUserNotificationsQuery(userId, take);
            var notifications = await _mediator.Send(query, cancellationToken);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user notifications");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    [Authorize(Policy = "SellerOrCustomer")]
    [HttpPost("{notificationId}/mark-as-read")]
    public async Task<IActionResult> MarkAsRead(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new MarkNotificationAsReadQuery(notificationId);
            var notification = await _mediator.Send(query, cancellationToken);
            return Ok(notification);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Connection Status

    /// <summary>
    /// Check if a user has an active SSE connection
    /// </summary>
    [HttpGet("status/{userId}")]
    [Authorize(Policy = "SellerOrCustomer")]
    public IActionResult CheckConnectionStatus(Guid userId)
    {
        var isConnected = _notificationService.IsUserConnected(userId);
        return Ok(new { userId, isConnected });
    }

    /// <summary>
    /// Get all connected users (admin only)
    /// </summary>
    [HttpGet("connected-users")]
    [Authorize(Policy = "AdminPolicy")]
    public IActionResult GetConnectedUsers()
    {
        var connectedUsers = _notificationService.GetConnectedUsers().ToList();
        return Ok(new { count = connectedUsers.Count, users = connectedUsers });
    }

    #endregion
}
