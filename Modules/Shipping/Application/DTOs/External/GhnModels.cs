using System.Text.Json.Serialization;

namespace Modules.Shipping.Application.DTOs.External;

public sealed record GhnApiResponse<T>(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] T? Data,
    [property: JsonPropertyName("code_message")] string? CodeMessage = null
);

public sealed record GhnCreateOrderRequest(
    [property: JsonPropertyName("payment_type_id")] int PaymentTypeId,
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("required_note")] string RequiredNote,
    [property: JsonPropertyName("from_name")] string? FromName,
    [property: JsonPropertyName("from_phone")] string? FromPhone,
    [property: JsonPropertyName("from_address")] string? FromAddress,
    [property: JsonPropertyName("from_ward_name")] string? FromWardName,
    [property: JsonPropertyName("from_district_name")] string? FromDistrictName,
    [property: JsonPropertyName("from_province_name")] string? FromProvinceName,
    [property: JsonPropertyName("return_phone")] string? ReturnPhone,
    [property: JsonPropertyName("return_address")] string? ReturnAddress,
    [property: JsonPropertyName("return_district_id")] int? ReturnDistrictId,
    [property: JsonPropertyName("return_ward_code")] string? ReturnWardCode,
    [property: JsonPropertyName("client_order_code")] string? ClientOrderCode,
    [property: JsonPropertyName("to_name")] string ToName,
    [property: JsonPropertyName("to_phone")] string ToPhone,
    [property: JsonPropertyName("to_address")] string ToAddress,
    [property: JsonPropertyName("to_ward_code")] string ToWardCode,
    [property: JsonPropertyName("to_district_id")] int ToDistrictId,
    [property: JsonPropertyName("cod_amount")] decimal CodAmount,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("weight")] int Weight,
    [property: JsonPropertyName("length")] int Length,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("pick_station_id")] int? PickStationId,
    [property: JsonPropertyName("deliver_station_id")] int? DeliverStationId,
    [property: JsonPropertyName("insurance_value")] decimal InsuranceValue,
    [property: JsonPropertyName("service_id")] int? ServiceId,
    [property: JsonPropertyName("service_type_id")] int? ServiceTypeId,
    [property: JsonPropertyName("coupon")] string? Coupon,
    [property: JsonPropertyName("pick_shift")] List<int>? PickShift,
    [property: JsonPropertyName("items")] List<GhnCreateOrderItem> Items
);

public sealed record GhnCreateOrderItem(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("price")] int? Price,
    [property: JsonPropertyName("length")] int? Length,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("weight")] int Weight,
    [property: JsonPropertyName("category")] GhnItemCategory? Category = null
);

public sealed record GhnItemCategory(
    [property: JsonPropertyName("level1")] string? Level1,
    [property: JsonPropertyName("level2")] string? Level2,
    [property: JsonPropertyName("level3")] string? Level3
);

public sealed record GhnCreateOrderResponse(
    [property: JsonPropertyName("order_code")] string OrderCode,
    [property: JsonPropertyName("expected_delivery_time")] DateTime? ExpectedDeliveryTime,
    [property: JsonPropertyName("total_fee")] decimal TotalFee,
    [property: JsonPropertyName("sort_code")] string? SortCode,
    [property: JsonPropertyName("trans_type")] string? TransType,
    [property: JsonPropertyName("fee")] GhnFeeBreakdown? Fee
);

public sealed record GhnFeeRequest(
    [property: JsonPropertyName("from_district_id")] int? FromDistrictId,
    [property: JsonPropertyName("from_ward_code")] string? FromWardCode,
    [property: JsonPropertyName("service_id")] int? ServiceId,
    [property: JsonPropertyName("service_type_id")] int? ServiceTypeId,
    [property: JsonPropertyName("to_district_id")] int ToDistrictId,
    [property: JsonPropertyName("to_ward_code")] string ToWardCode,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("length")] int? Length,
    [property: JsonPropertyName("weight")] int Weight,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("insurance_value")] decimal? InsuranceValue,
    [property: JsonPropertyName("cod_failed_amount")] decimal? CodFailedAmount,
    [property: JsonPropertyName("cod_value")] decimal? CodValue,
    [property: JsonPropertyName("coupon")] string? Coupon,
    [property: JsonPropertyName("items")] List<GhnFeeItem>? Items
);

public sealed record GhnFeeItem(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("weight")] int Weight,
    [property: JsonPropertyName("length")] int? Length,
    [property: JsonPropertyName("width")] int? Width
);

public sealed record GhnFeeResponse(
    [property: JsonPropertyName("total")] decimal Total,
    [property: JsonPropertyName("service_fee")] decimal ServiceFee,
    [property: JsonPropertyName("insurance_fee")] decimal InsuranceFee,
    [property: JsonPropertyName("pick_station_fee")] decimal PickStationFee,
    [property: JsonPropertyName("coupon_value")] decimal CouponValue,
    [property: JsonPropertyName("r2s_fee")] decimal R2SFee,
    [property: JsonPropertyName("document_return")] decimal DocumentReturn,
    [property: JsonPropertyName("double_check")] decimal DoubleCheck,
    [property: JsonPropertyName("cod_fee")] decimal CodFee,
    [property: JsonPropertyName("pick_remote_areas_fee")] decimal PickRemoteAreasFee,
    [property: JsonPropertyName("deliver_remote_areas_fee")] decimal DeliverRemoteAreasFee,
    [property: JsonPropertyName("cod_failed_fee")] decimal CodFailedFee
);

public sealed record GhnServiceRequest(
    [property: JsonPropertyName("shop_id")] int ShopId,
    [property: JsonPropertyName("from_district")] int FromDistrict,
    [property: JsonPropertyName("to_district")] int ToDistrict
);

public sealed record GhnServiceResponse(
    [property: JsonPropertyName("service_id")] int ServiceId,
    [property: JsonPropertyName("short_name")] string? ShortName,
    [property: JsonPropertyName("service_type_id")] int ServiceTypeId
);

public sealed record GhnCancelRequest(
    [property: JsonPropertyName("order_codes")] List<string> OrderCodes
);

public sealed record GhnCancelResult(
    [property: JsonPropertyName("order_code")] string OrderCode,
    [property: JsonPropertyName("result")] bool Result,
    [property: JsonPropertyName("message")] string? Message
);

public sealed record GhnOrderInfoResponse(
    [property: JsonPropertyName("order_code")] string OrderCode,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("service_id")] int? ServiceId,
    [property: JsonPropertyName("service_type_id")] int? ServiceTypeId,
    [property: JsonPropertyName("leadtime")] DateTime? LeadTime
);

public sealed record GhnFeeBreakdown(
    [property: JsonPropertyName("coupon")] decimal Coupon,
    [property: JsonPropertyName("insurance")] decimal Insurance,
    [property: JsonPropertyName("main_service")] decimal MainService,
    [property: JsonPropertyName("r2s")] decimal R2S,
    [property: JsonPropertyName("return")] decimal Return,
    [property: JsonPropertyName("station_do")] decimal StationDo,
    [property: JsonPropertyName("station_pu")] decimal StationPu
);

public sealed record GhnWebhookPayload(
    [property: JsonPropertyName("OrderCode")] string OrderCode,
    [property: JsonPropertyName("Status")] string Status,
    [property: JsonPropertyName("Type")] string Type,
    [property: JsonPropertyName("Time")] DateTime? Time,
    [property: JsonPropertyName("Reason")] string? Reason,
    [property: JsonPropertyName("ReasonCode")] string? ReasonCode,
    [property: JsonPropertyName("Description")] string? Description,
    [property: JsonPropertyName("PaymentType")] int? PaymentType,
    [property: JsonPropertyName("CODAmount")] decimal? CodAmount,
    [property: JsonPropertyName("TotalFee")] decimal? TotalFee,
    [property: JsonPropertyName("Weight")] int? Weight,
    [property: JsonPropertyName("Length")] int? Length,
    [property: JsonPropertyName("Width")] int? Width,
    [property: JsonPropertyName("Height")] int? Height,
    [property: JsonPropertyName("Fee")] GhnWebhookFee? Fee
);

public sealed record GhnWebhookFee(
    [property: JsonPropertyName("CODFailedFee")] decimal? CodFailedFee,
    [property: JsonPropertyName("CODFee")] decimal? CodFee,
    [property: JsonPropertyName("Coupon")] decimal? Coupon,
    [property: JsonPropertyName("DeliverRemoteAreasFee")] decimal? DeliverRemoteAreasFee,
    [property: JsonPropertyName("DocumentReturn")] decimal? DocumentReturn,
    [property: JsonPropertyName("DoubleCheck")] decimal? DoubleCheck,
    [property: JsonPropertyName("Insurance")] decimal? Insurance,
    [property: JsonPropertyName("MainService")] decimal? MainService,
    [property: JsonPropertyName("PickRemoteAreasFee")] decimal? PickRemoteAreasFee,
    [property: JsonPropertyName("R2S")] decimal? R2S,
    [property: JsonPropertyName("Return")] decimal? Return,
    [property: JsonPropertyName("StationDO")] decimal? StationDo,
    [property: JsonPropertyName("StationPU")] decimal? StationPu,
    [property: JsonPropertyName("Total")] decimal? Total
);
