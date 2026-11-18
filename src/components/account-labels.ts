import type { Locale } from "@/lib/contracts";

const labels: Record<string, [string, string]> = {
  member: ["Thành viên", "Member"], silver: ["Bạc", "Silver"], gold: ["Vàng", "Gold"], platinum: ["Bạch kim", "Platinum"],
  earn: ["Tích điểm mua hàng", "Purchase points earned"], redeem: ["Sử dụng điểm", "Points redeemed"],
  restore: ["Hoàn lại điểm", "Points restored"], reverse: ["Điều chỉnh điểm đã tích", "Earned points adjustment"],
  trade_in: ["Nhận số dư thu cũ", "Trade-in credit"], spend: ["Sử dụng số dư mua hàng", "Store credit spent"], refund: ["Hoàn lại số dư", "Store credit refund"],
  adjustment: ["Điều chỉnh số dư", "Balance adjustment"], opening: ["Số dư ban đầu", "Opening balance"],
  defective: ["Sản phẩm bị lỗi", "Defective product"], wrong_item: ["Giao sai sản phẩm", "Incorrect item delivered"],
};
export function accountLabel(value: string, locale: Locale, fallback = value) {
  return labels[value.toLowerCase()]?.[locale === "vi" ? 0 : 1] ?? fallback;
}
