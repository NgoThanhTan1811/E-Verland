using System.Text.Json;
using Infra.AWS.SQS;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modules.Shipping.Application.Commands;
using Modules.Shipping.Application.DTOs.Request;
using SharedKernel.Events;

namespace Modules.Shipping.Infrastructure.Consumers;

public sealed class ShippingRequestConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISQSService _sqsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ShippingRequestConsumer> _logger;
    private readonly int _pollIntervalMs = 5000;
    private readonly int _maxMessages = 10;

    public ShippingRequestConsumer(
        ISQSService sqsService,
        IConfiguration configuration,
        ILogger<ShippingRequestConsumer> logger,
        IServiceScopeFactory scopeFactory)
    {
        _sqsService = sqsService;
        _configuration = configuration;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = _configuration["AWS:SQS:ShippingDraftQueueUrl"]
            ?? _configuration["SQS:ShippingDraftQueueUrl"]
            ?? Environment.GetEnvironmentVariable("AWS_SQS_SHIPPING_DRAFT_QUEUE_URL");

        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            _logger.LogWarning("ShippingDraftQueueUrl not configured, ShippingRequestConsumer will not run");
            return;
        }

        _logger.LogInformation("ShippingRequestConsumer started, listening to queue {QueueUrl}", queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await _sqsService.ReceiveMessagesAsync<JsonDocument>(queueUrl, _maxMessages, stoppingToken);

                if (messages == null || messages.Count == 0)
                {
                    await Task.Delay(_pollIntervalMs, stoppingToken);
                    continue;
                }

                foreach (var msg in messages)
                {
                    if (msg.Body is null)
                    {
                        await _sqsService.DeleteMessageAsync(queueUrl, msg.ReceiptHandle, stoppingToken);
                        continue;
                    }

                    // 1. Tạo scope cho MỖI message để đảm bảo các scoped service (Repo, DbContext) hoạt động đúng
                    using var scope = _scopeFactory.CreateScope();

                    // 2. Resolve Mediator từ scope này
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    using var document = msg.Body;
                    try
                    {
                        var root = document.RootElement;
                        var eventType = TryGetString(root, "eventType", "EventType", "type", "Type");

                        await HandleEventAsync(mediator, eventType, root, stoppingToken);

                        await _sqsService.DeleteMessageAsync(queueUrl, msg.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing order event message {MessageId}", msg.MessageId);
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling ShippingDraft queue");
                await Task.Delay(_pollIntervalMs, stoppingToken);
            }
        }
    }

    private async Task HandleEventAsync(IMediator mediator, string? eventType, JsonElement root, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            _logger.LogWarning("Shipping request missing eventType");
            return;
        }

        var raw = root.GetRawText();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        switch (eventType)
        {
            case "ShippingDraftRequested":
                {
                    var evt = JsonSerializer.Deserialize<ShippingDraftRequested>(raw, options);
                    if (evt is null)
                    {
                        _logger.LogWarning("Invalid ShippingDraftRequested payload");
                        return;
                    }

                    var items = evt.Items.Select(i => new ShippingItemRequestDto(
                        i.Name,
                        i.Code,
                        i.Quantity,
                        i.Price,
                        i.Weight,
                        i.Length,
                        i.Width,
                        i.Height)).ToList();

                    var request = new CreateShippingDraftRequestDto(
                        evt.OrderId,
                        evt.UserId,
                        evt.OrderCode,
                        new ShippingAddressRequestDto(
                            evt.ToName,
                            evt.ToPhone,
                            evt.ToAddress,
                            evt.ToDistrictId,
                            evt.ToWardCode,
                            evt.ToWardName,
                            evt.ToDistrictName,
                            evt.ToProvinceName),
                        new ShippingDimensionsRequestDto(
                            evt.Weight,
                            evt.Length,
                            evt.Width,
                            evt.Height),
                        items,
                        evt.ServiceId,
                        evt.ServiceTypeId,
                        evt.PaymentTypeId,
                        evt.CodAmount,
                        evt.InsuranceValue,
                        evt.Note,
                        evt.RequiredNote
                    );

                    await mediator.Send(new CreateShippingDraftCommand(request), ct);
                    break;
                }
            case "ShippingActivationRequested":
                {
                    var evt = JsonSerializer.Deserialize<ShippingActivationRequested>(raw, options);
                    if (evt is null)
                    {
                        _logger.LogWarning("Invalid ShippingActivationRequested payload");
                        return;
                    }

                    await mediator.Send(new ActivateShippingOrderCommand(evt.OrderId), ct);
                    break;
                }
            case "ShippingCancelRequested":
                {
                    var evt = JsonSerializer.Deserialize<ShippingCancelRequested>(raw, options);
                    if (evt is null)
                    {
                        _logger.LogWarning("Invalid ShippingCancelRequested payload");
                        return;
                    }

                    await mediator.Send(new CancelShippingOrderCommand(evt.OrderId), ct);
                    break;
                }
            default:
                _logger.LogInformation("Ignoring unsupported shipping event type {EventType}", eventType);
                break;
        }
    }

    private static string? TryGetString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString();
                }

                if (prop.ValueKind == JsonValueKind.Number)
                {
                    return prop.GetRawText();
                }
            }
        }

        return null;
    }
}
