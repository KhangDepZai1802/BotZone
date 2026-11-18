namespace BotZone.Infrastructure.Services;

public static class CustomerNotificationPresentation
{
    // Translate system-authored templates only. Customer/staff notes and historical records remain intact.
    public static List<Dictionary<string, object?>> Localize(List<Dictionary<string, object?>> notifications, string locale)
    {
        if (locale != "en") return notifications;
        foreach (var notification in notifications)
        {
            var message = notification.S("message");
            if (message.StartsWith("Đã nhận đơn ", StringComparison.Ordinal))
                notification["message"] = "Order received: " + message["Đã nhận đơn ".Length..];
            else if (message == "Đã nhận yêu cầu trả hàng; chờ hướng dẫn gửi trả.")
                notification["message"] = "Your return request has been received. We will send return instructions.";
            else if (message == "Hồ sơ ngoài phạm vi: gói không bảo vệ rơi vỡ/vào nước.")
                notification["message"] = "This claim is outside your coverage: accidental and liquid damage are excluded.";
            else if (message == "Đã tiếp nhận yêu cầu. Giá cuối cần kiểm định và khách chấp nhận; BotZone chịu phí gửi/nhận.")
                notification["message"] = "Your trade-in request has been received. The final offer requires inspection and your acceptance; BotZone covers both shipping directions.";
            else if (message == "Có báo giá phiên bản mới; cần chấp nhận lại từng món sau khi được duyệt.")
                notification["message"] = "A new trade-in offer is available. Review and accept each item again after approval.";
            else if (message == "Giá trị thu cũ đã vào số dư mua hàng; không quy đổi tiền mặt.")
                notification["message"] = "Your trade-in value has been added as store credit. It cannot be exchanged for cash.";
            else if (message == "Đã hoàn đúng nguồn thanh toán mô phỏng; điểm không được đổi thành tiền.")
                notification["message"] = "Your refund has been returned to its original demo payment source. Reward points cannot be converted to cash.";
            else if (message.StartsWith("Không đủ điều kiện hoàn; nhân viên sẽ liên hệ trả lại hàng. ", StringComparison.Ordinal))
                notification["message"] = "This item is ineligible for a refund. Our team will arrange its return. "
                    + message["Không đủ điều kiện hoàn; nhân viên sẽ liên hệ trả lại hàng. ".Length..];
            else if (message.StartsWith("Hàng được gửi trả: ", StringComparison.Ordinal))
                notification["message"] = "Your item has been sent back: " + message["Hàng được gửi trả: ".Length..];
        }
        return notifications;
    }
}
