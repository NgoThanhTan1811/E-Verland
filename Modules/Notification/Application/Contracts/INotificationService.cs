using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.Notification.Application.Contracts
{
    public interface INotificationService
    {
        void RegisterUserConnection(Guid userId, StreamWriter writer);

        void UnregisterUserConnection(Guid userId);

        Task SendToUserAsync(Guid userId, Domain.Notification notification);

        Task BroadcastToUsersAsync(IEnumerable<Guid> userIds, Domain.Notification notification);

        bool IsUserConnected(Guid userId);
        IEnumerable<Guid> GetConnectedUsers();
    }

}