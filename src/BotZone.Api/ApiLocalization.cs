using System.Text.RegularExpressions;

namespace BotZone.Api;

public static class ApiLocalization
{
    // The browser explicitly sends its route locale; native WinForms retains Vietnamese by default.
    public static string Locale(HttpContext context)
    {
        var language = context.Request.Headers.AcceptLanguage.ToString().Split(',')[0].Split(';')[0].Trim();
        return language.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : "vi";
    }
    private static readonly Dictionary<string, string> English = new(StringComparer.Ordinal)
    {
        ["Vui lòng đăng nhập."] = "Please sign in to continue.",
        ["Email hoặc mật khẩu không đúng, hoặc tài khoản đang khóa."] = "The email or password is incorrect, or this account is temporarily locked.",
        ["Tài khoản cần đặt lại mật khẩu qua email."] = "Please reset your password using the email link before signing in.",
        ["Tài khoản nhân viên đã bị khóa."] = "This staff account has been disabled.",
        ["Client không hợp lệ."] = "Invalid client type.",
        ["Lựa chọn ghi nhớ đăng nhập không hợp lệ."] = "Invalid remember-me option.",
        ["Ứng dụng quản trị dành cho nhân viên."] = "The administration app is available to staff only.",
        ["Đăng ký không nhận vai trò."] = "Public registration cannot assign a role.",
        ["Tên phải có 2–100 ký tự."] = "Your name must contain 2–100 characters.",
        ["Email không hợp lệ."] = "Enter a valid email address.",
        ["Số điện thoại phải có 9–15 chữ số."] = "Enter a phone number containing 9–15 digits.",
        ["Địa chỉ chưa đầy đủ."] = "Complete the recipient, phone number and delivery address.",
        ["Chỉ giao tại Việt Nam."] = "Delivery is available within Vietnam only.",
        ["Token không hợp lệ."] = "This link is invalid or has expired. Request a new email link.",
        ["Email đã được xác minh."] = "This email address has already been verified.",
        ["Phiên đã hết hạn."] = "Your session has expired. Please sign in again.",
        ["Phiên đã bị thu hồi."] = "Your session has been revoked. Please sign in again.",
        ["Quyền tài khoản đã thay đổi. Vui lòng đăng nhập lại."] = "Your account permissions have changed. Please sign in again.",
        ["Không tìm thấy sản phẩm."] = "This product could not be found.",
        ["Không tìm thấy dữ liệu trong phạm vi được phép."] = "The requested record is unavailable or outside your access.",
        ["Phiên yêu cầu đã thay đổi. Hãy tải lại và thử lại."] = "Your secure session has changed. Reload the page and try again.",
        ["Dữ liệu gửi lên không đúng định dạng hoặc vượt giới hạn."] = "The submitted information has an invalid format or exceeds the allowed limit.",
        ["Dữ liệu hoặc trạng thái không còn phù hợp. Hãy tải lại trước khi thử lại."] = "This information has changed. Reload before trying again.",
        ["Không thể hoàn tất yêu cầu. Xem log API local để kiểm tra."] = "We could not complete your request. Please try again shortly.",
        ["Hãy xác minh email trước khi đặt hàng."] = "Verify your email address before placing an order.",
        ["Báo giá đã đổi. Vui lòng xem lại."] = "Your order quote has changed. Review the updated total.",
        ["Phương thức không hợp lệ."] = "Choose a valid payment method.",
        ["Thiếu tệp upload."] = "Choose a file to upload.",
        ["Mật khẩu không đúng."] = "The current password is incorrect.",
        ["Mật khẩu phải có ít nhất 6 ký tự."] = "Use a password with at least 6 characters.",
        ["Email đã được sử dụng."] = "This email address is already registered. Sign in or reset your password.",
        ["Liên kết không hợp lệ hoặc đã hết hạn."] = "This link is invalid or has expired. Request a new email link.",
        ["Thông tin tài khoản không hợp lệ. Vui lòng kiểm tra lại."] = "Check your account information and try again.",
        ["Giỏ cần 1–30 dòng."] = "Your bag must contain 1–30 different items.",
        ["Giỏ quá lớn."] = "Your bag exceeds the allowed size. Remove some items and try again.",
        ["Biến thể trùng trong giỏ."] = "The same configuration appears more than once in your bag.",
        ["Số lượng cần từ 1 đến 100."] = "Choose a quantity between 1 and 100.",
        ["Số lượng không hợp lệ."] = "Choose a valid quantity.",
        ["SKU không còn được bán."] = "This configuration is no longer available for sale.",
        ["SKU chưa đủ điều kiện Care."] = "BotZone Care is unavailable for this configuration.",
        ["Không đủ tồn khả dụng."] = "The selected quantity is no longer in stock.",
        ["Điểm không âm."] = "Reward points cannot be negative.",
        ["Điểm không đủ."] = "You do not have enough available reward points.",
        ["Số dư không đủ."] = "Your balance is insufficient for this amount.",
        ["Số dư khả dụng không đủ."] = "Your available balance is insufficient for this amount.",
        ["Mã khuyến mãi không hiệu lực."] = "This promotion code is invalid or has expired.",
        ["Giỏ chưa đạt điều kiện hoặc sai phạm vi khuyến mãi."] = "Your bag does not meet this promotion's requirements.",
        ["Chương trình đã hết lượt sử dụng."] = "This promotion has reached its redemption limit.",
        ["Mã quá dài."] = "This code is too long.",
        ["Đơn không nhận thanh toán."] = "This order is not accepting payments.",
        ["Đơn không thể thanh toán."] = "Payment is unavailable for this order's current status.",
        ["Đơn đã thu đủ."] = "This order has already been paid in full.",
        ["Không có tiền demo cần thu."] = "There is no outstanding demo payment for this order.",
        ["Công cụ thanh toán demo đã tắt."] = "Demo payments are currently disabled.",
        ["Lần thanh toán đã xử lý hoặc số tiền không khớp."] = "This payment was already processed, or the amount does not match.",
        ["Đã bàn giao; cần hồ sơ thu hồi."] = "This order has been dispatched. Submit a return request instead.",
        ["Chỉ yêu cầu trả hàng đã giao thành công."] = "Return requests are available for delivered orders only.",
        ["Hãy xác minh email trước khi yêu cầu trả hàng."] = "Verify your email address before requesting a return.",
        ["Chỉ trả hàng do lỗi hoặc giao sai."] = "Returns are available for faulty or incorrectly delivered items.",
        ["Đã hết thời hạn yêu cầu trả hàng theo đơn."] = "The return-request period for this order has ended.",
        ["Ghi chú cần 5–2.000 ký tự."] = "Enter a note containing 5–2,000 characters.",
        ["Giao sai cần mô tả mã/món thực nhận."] = "Describe the item or model you actually received.",
        ["Mã món thực nhận quá dài."] = "The received item's model code is too long.",
        ["Chọn 1–30 dòng hàng."] = "Choose between 1 and 30 items.",
        ["Chọn 1–100 đơn vị khác nhau."] = "Choose between 1 and 100 distinct units.",
        ["Dòng hàng không được lặp."] = "The same item cannot be selected twice.",
        ["Không có đơn vị hợp lệ để trả."] = "There are no eligible units to return.",
        ["Số lượng trả không hợp lệ."] = "Choose a valid return quantity.",
        ["Số lượng đã hoặc đang được trả."] = "Some of these units have already been returned or are being returned.",
        ["Một đơn vị đã có yêu cầu trả hoặc điều chỉnh."] = "One of these units already has a return request or adjustment.",
        ["Đơn vị hàng chưa giao hoặc không thuộc dòng đã chọn."] = "This unit has not been delivered or does not belong to the selected item.",
        ["Mã thiết bị chỉ gồm chữ/số, tối đa 100 ký tự."] = "Use up to 100 letters and numbers for the device identifier.",
        ["Tối đa sáu ảnh bằng chứng."] = "Attach up to six evidence images.",
        ["Ảnh bằng chứng cần danh sách."] = "Provide evidence images as a list.",
        ["Không được dùng ảnh riêng của khách khác."] = "You cannot use another customer's private images.",
        ["Ảnh bằng chứng không thuộc hồ sơ nguồn."] = "An evidence image does not belong to this request.",
        ["Mã ảnh không hợp lệ."] = "An image reference is invalid.",
        ["Ảnh tối đa 5 MB."] = "Each image must be no larger than 5 MB.",
        ["Ảnh tối đa 20 megapixel."] = "Each image must be no larger than 20 megapixels.",
        ["Cấu trúc ảnh không hợp lệ."] = "This image file is invalid or damaged.",
        ["Dùng ảnh WebP tĩnh."] = "Choose a static WebP image.",
        ["Chỉ nhận nội dung JPEG, PNG, WebP hoặc GLB hợp lệ; không nhận SVG/HTML."] = "Choose a valid JPEG, PNG or WebP image. SVG and HTML files are not accepted.",
        ["Bằng chứng phải là ảnh; GLB chỉ dùng cho catalogue."] = "Evidence attachments must be images.",
        ["Xác minh email trước khi gửi bằng chứng."] = "Verify your email address before uploading evidence.",
        ["Đăng nhập để đọc bằng chứng riêng tư."] = "Sign in to view private evidence.",
        ["Hồ sơ không thuộc tài khoản của bạn."] = "This request does not belong to your account.",
        ["Bằng chứng cần hồ sơ liên quan."] = "Evidence must be linked to a request.",
        ["Bằng chứng chưa liên kết hồ sơ hợp lệ."] = "This evidence is not linked to a valid request.",
        ["Tệp media không còn trong thư viện."] = "This media file is no longer available."
    };
    private static readonly Dictionary<string, string> Codes = new(StringComparer.Ordinal)
    {
        ["UNAUTHENTICATED"] = "Please sign in to continue.", ["FORBIDDEN"] = "Your account cannot perform this action.",
        ["NOT_FOUND"] = "The requested record could not be found.", ["VALIDATION_ERROR"] = "Check the information entered and try again.",
        ["OUT_OF_STOCK"] = "The selected quantity is no longer available. Update your bag and try again.",
        ["INSUFFICIENT_STOCK"] = "The selected quantity is no longer available. Update your bag and try again.",
        ["PROMOTION_INVALID"] = "This promotion is not available for your order.",
        ["QUOTE_CHANGED"] = "Your order quote has changed. Review the updated total.",
        ["STATE_CONFLICT"] = "This record has changed. Refresh the page before continuing.",
        ["IDEMPOTENCY_CONFLICT"] = "This request has already been used for a different operation. Refresh before trying again.",
        ["EMAIL_IN_USE"] = "This email address is already registered. Sign in or reset your password.",
        ["TOKEN_INVALID"] = "This link is invalid or has expired. Request a new email link.",
        ["TOKEN_USED"] = "This link has already been used.",
        ["PASSWORD_POLICY"] = "Use a password with at least 6 characters."
    };
    public static string Message(HttpContext context, string code, string message)
    {
        if (Locale(context) != "en") return message;
        if (English.TryGetValue(message, out var translated)) return translated;
        if (!Regex.IsMatch(message, @"[\u00c0-\u1eff]")) return message;
        if (message.StartsWith("Thiếu hoặc sai trường ", StringComparison.Ordinal)) return "A required field is missing or invalid.";
        return Codes.GetValueOrDefault(code, "This action could not be completed. Review the information and try again.");
    }
}
