namespace BotZone.Admin.WinForms;

internal static class WorkflowGuide
{
    internal static string For(string module,string state)
    {
        var flow=module switch
        {
            "catalogue"=>"Mẫu sản phẩm → Phiên bản bán (màu, dung lượng) → Giá và mở bán",
            "purchases"=>"Lập phiếu → Chủ cửa hàng duyệt → Kho nhận hàng",
            "returns"=>"Tiếp nhận → Kho kiểm tra → Chủ cửa hàng duyệt → Hoàn tiền / trả lại hàng",
            "trade_ins"=>"Nhận máy → Kiểm định → Báo giá → Khách đồng ý → Cấp số dư mua hàng",
            "warranty"=>"Tiếp nhận → Kiểm định → Sửa chữa hoặc duyệt đổi / hoàn → Trả máy",
            "stock_adjustments"=>"Kho đề nghị → Chủ cửa hàng duyệt → Cập nhật tồn",
            "refunds"=>"Chứng từ được duyệt → Hoàn tiền → Kiểm tra kết quả",
            _=>""
        };
        return flow.Length==0?"":"Quy trình: "+flow+(state.Length==0?"":"  ·  Hiện tại: "+ModuleView.ValueLabel("state",state));
    }
}
