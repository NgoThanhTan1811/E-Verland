using System;
using System.Linq;
using System.Reflection;
using Modules.Chat.Application.Contracts;
using Modules.Media.Infrastructure.Services;
using Modules.Payment.Application.Commands;
using Modules.Payment.Application.Contracts;
using Modules.Order.Application.Contracts;
using Xunit;
using NSubstitute;
using Microsoft.Extensions.Options;
using Infra.AWS.CloudWatch;
using Infra.AWS.Storage;
using Modules.Media.Infrastructure.Options;
using Modules.Product.Application.Contracts;

namespace Tests.SystemFlowAudit;

public class BugConditionExplorationTests
{
    [Fact]
    public void InitiatePaymentHandler_MustNotInject_CrossModuleServices()
    {
        var ctor = typeof(InitiatePaymentHandler).GetConstructors(BindingFlags.Public | BindingFlags.Instance).First();
        var paramTypes = ctor.GetParameters().Select(p => p.ParameterType).ToList();

        Assert.DoesNotContain(typeof(IProductReservationService), paramTypes);
        Assert.DoesNotContain(typeof(IOrderPaymentSyncService), paramTypes);
    }

    [Fact]
    public void PaymentController_MustNotInject_OrderRepository()
    {
        var paymentControllerType = Type.GetType("Modules.Payment.Api.Controllers.PaymentController, Host");
        Assert.NotNull(paymentControllerType);
        var ctor = paymentControllerType!.GetConstructors(BindingFlags.Public | BindingFlags.Instance).First();
        var paramTypes = ctor.GetParameters().Select(p => p.ParameterType).ToList();

        Assert.DoesNotContain(typeof(IOrderRepository), paramTypes);
    }

    [Fact]
    public void ChatHub_SendMessage_MustPersistMessage_BeforeBroadcast()
    {
        var hubType = Type.GetType("Modules.Chat.Api.Hubs.ChatHub, Host");
        Assert.NotNull(hubType);
        var ctor = hubType!.GetConstructors(BindingFlags.Public | BindingFlags.Instance).First();
        var paramTypes = ctor.GetParameters().Select(p => p.ParameterType).ToList();

        // Expect IMessageRepository to be injected so messages are persisted before broadcasting
        var iMessageRepoType = typeof(IMessageRepository);
        Assert.Contains(iMessageRepoType, paramTypes);
    }

    [Fact]
    public void MediaStorageService_GetPathPrefix_ShouldUseAvatarPrefix_ForImage()
    {
        var storage = Substitute.For<IStorageService>();
        var cloudWatch = Substitute.For<ICloudWatchService>();
        var storageOptions = Options.Create(new StorageOptions { ProductsPrefix = "products", AvatarsPrefix = "avatars", ReviewsPrefix = "reviews" });
        var mediaOptions = Options.Create(new MediaOptions());

        var svc = new MediaStorageService(storage, cloudWatch, storageOptions, mediaOptions);

        // Call private GetPathPrefix via reflection
        var method = typeof(MediaStorageService).GetMethod("GetPathPrefix", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method!.Invoke(svc, new object[] { "image/png" }) as string;

        // Expect avatar prefix for images (this asserts the fixed behavior)
        Assert.Equal("avatars", result);
    }
}
