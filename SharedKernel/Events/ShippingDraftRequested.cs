using System.Text.Json.Serialization;

namespace SharedKernel.Events;

public sealed record ShippingDraftRequested(
    [property: JsonPropertyName("orderId")] Guid OrderId,
    [property: JsonPropertyName("orderCode")] string OrderCode,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("toName")] string ToName,
    [property: JsonPropertyName("toPhone")] string ToPhone,
    [property: JsonPropertyName("toAddress")] string ToAddress,
    [property: JsonPropertyName("toDistrictId")] int ToDistrictId,
    [property: JsonPropertyName("toWardCode")] string ToWardCode,
    [property: JsonPropertyName("toWardName")] string? ToWardName,
    [property: JsonPropertyName("toDistrictName")] string? ToDistrictName,
    [property: JsonPropertyName("toProvinceName")] string? ToProvinceName,
    [property: JsonPropertyName("weight")] int Weight,
    [property: JsonPropertyName("length")] int Length,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("serviceId")] int? ServiceId,
    [property: JsonPropertyName("serviceTypeId")] int? ServiceTypeId,
    [property: JsonPropertyName("paymentTypeId")] int PaymentTypeId,
    [property: JsonPropertyName("codAmount")] decimal CodAmount,
    [property: JsonPropertyName("insuranceValue")] decimal InsuranceValue,
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("requiredNote")] string? RequiredNote,
    [property: JsonPropertyName("items")] List<ShippingDraftItem> Items,
    [property: JsonPropertyName("eventType")] string EventType = "ShippingDraftRequested"
);

public sealed record ShippingDraftItem(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("price")] int? Price,
    [property: JsonPropertyName("weight")] int Weight,
    [property: JsonPropertyName("length")] int? Length,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height
);
