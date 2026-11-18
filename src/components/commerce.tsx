"use client";
import { productPhoto, productPhotoStyle } from "./product-photo";
import commerceStyles from "./commerce.module.css";
import React, { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  ArrowRight,
  Check,
  Download,
  Minus,
  Plus,
  RefreshCw,
  ShieldCheck,
  ShoppingBag,
  Trash2,
  Truck,
} from "lucide-react";
import type { Address, Locale, PaymentMethod, Quote } from "@/lib/contracts";
import { api, command, useResource, useStore } from "./client";
import {
  Badge,
  Busy,
  Button,
  Empty,
  ErrorState,
  Field,
  Input,
  Money,
  Select,
  Textarea,
} from "./ui";
import { date, moneyValue, row, rows, str, type Row } from "./records";
import { EvidenceUploader } from "./evidence-uploader";

function paymentLabel(value: string, locale: Locale) {
  const labels: Record<string, [string, string]> = {
    cod: ["Thanh toán khi nhận hàng", "Cash on delivery"],
    banking: ["Chuyển khoản", "Bank transfer"], momo: ["MoMo", "MoMo"], visa: ["Visa", "Visa"],
    cash_demo: ["Khoản thu mô phỏng", "Demo payment"], store_credit: ["Số dư cửa hàng", "Store credit"],
  };
  return labels[value]?.[locale === "vi" ? 0 : 1] ?? value;
}
function lineTypeLabel(value: string, locale: Locale) {
  const labels: Record<string, [string, string]> = {
    product: ["Sản phẩm", "Product"], warranty: ["Gói bảo hành", "Warranty plan"], shipping: ["Phí giao hàng", "Shipping"],
  };
  return labels[value]?.[locale === "vi" ? 0 : 1] ?? value;
}

export function RequireAccount({ children }: { children: React.ReactNode }) {
  const s = useStore();
  if (s.sessionLoading)
    return (
      <Busy
        text={s.tr("Đang kiểm tra phiên đăng nhập…", "Checking your session…")}
      />
    );
  if (!s.actor)
    return (
      <div className="page-wrap">
        <Empty
          title={s.tr("Chào mừng đến BotZone", "Welcome to BotZone")}
          description={s.tr(
            "Đăng nhập để tiếp tục mua sắm và theo dõi những kết nối của bạn.",
            "Sign in to continue shopping and manage your orders.",
          )}
        >
          <Link className="button button-primary" href={`/${s.locale}/login`}>
            {s.tr("Đăng nhập", "Sign in")}
            <ArrowRight />
          </Link>
        </Empty>
      </div>
    );
  return <>{children}</>;
}
export function OrderStatus({ value }: { value: string }) {
  const s = useStore();
  const map: Record<string, [string, string]> = {
    pending: ["Chờ xử lý", "Pending"],
    placed: ["Đã đặt hàng", "Placed"],
    confirmed: ["Đã xác nhận", "Confirmed"],
    shipped: ["Đã bàn giao", "Shipped"],
    delivered: ["Đã giao", "Delivered"],
    completed: ["Hoàn tất", "Completed"],
    cancelled: ["Đã hủy", "Cancelled"],
    expired: ["Hết hạn", "Expired"],
    paid: ["Đã thanh toán", "Paid"],
    unpaid: ["Chưa thu đủ", "Unpaid"],
    success: ["Thành công", "Success"],
    failed: ["Thất bại", "Failed"],
    reserved: ["Đang giữ hàng", "Reserved"],
    returned: ["Đã thu hồi", "Returned"],
    in_transit: ["Đang giao", "In transit"],
    issued: ["Đã phát hành", "Issued"],
    draft: ["Bản nháp", "Draft"],
    inspected: ["Đã kiểm định", "Inspected"],
    quoted: ["Có báo giá", "Quoted"],
    accepted: ["Đã chấp nhận", "Accepted"],
    rejected: ["Đã từ chối", "Rejected"],
    received: ["Đã tiếp nhận", "Received"],
    submitted: ["Đã gửi yêu cầu", "Submitted"],
    active: ["Có hiệu lực", "Active"],
    partially_paid: ["Đã thu một phần", "Partially paid"],
    reconciliation: ["Chờ đối soát", "Reconciliation"],
    requested: ["Đã gửi yêu cầu", "Requested"],
    approved: ["Đã duyệt", "Approved"],
    credited: ["Đã lập điều chỉnh", "Credit note issued"],
    refunded: ["Đã hoàn tiền", "Refunded"],
    contact_pending: ["Chờ liên hệ", "Awaiting contact"],
    exception: ["Cần xử lý ngoại lệ", "Exception needs review"],
    awaiting_payment: ["Chờ thanh toán", "Awaiting payment"],
    used: ["Đã sử dụng", "Used"],
    building: ["Đang tạo", "Preparing"],
    handed_off: ["Đã bàn giao vận chuyển", "Handed to carrier"],
    closed: ["Đã đóng", "Closed"],
    created: ["Đã tạo", "Created"],
    awaiting_inspection: ["Chờ kiểm định", "Awaiting inspection"],
    awaiting_owner_approval: ["Chờ chủ cửa hàng duyệt", "Awaiting owner approval"],
    finalized: ["Đã hoàn tất", "Finalized"],
    stored: ["Đã nhập kho", "Stored"],
    repairing: ["Đang sửa chữa", "Under repair"],
    repaired: ["Đã sửa chữa", "Repaired"],
    replace: ["Đã duyệt thay thế", "Replacement approved"],
    replaced: ["Đã thay thế", "Replaced"],
    refund: ["Đã duyệt hoàn tiền", "Refund approved"],
    consumed: ["Đã sử dụng", "Consumed"],
    released: ["Đã giải phóng", "Released"],
    part_received: ["Đã nhận một phần", "Partially received"],
    available: ["Có sẵn", "Available"],
    quarantine: ["Chờ xử lý tại kho", "Quarantined"],
    service: ["Đang bảo hành", "In service"],
    inactive: ["Không hoạt động", "Inactive"],
    processing: ["Đang xử lý", "Processing"],
    ready: ["Sẵn sàng", "Ready"],
  };
  return (
    <Badge
      tone={
        [
          "completed",
          "success",
          "paid",
          "delivered",
          "issued",
          "active",
        ].includes(value)
          ? "success"
          : ["failed", "cancelled", "rejected"].includes(value)
            ? "danger"
            : "neutral"
      }
    >
      {map[value] ? map[value][s.locale === "vi" ? 0 : 1] : value || "—"}
    </Badge>
  );
}

export function Cart({ checkout = false }: { checkout?: boolean }) {
  const s = useStore(),
    router = useRouter(),
    [quote, setQuote] = useState<Quote | null>(null),
    [points, setPoints] = useState("0"),
    [credit, setCredit] = useState("0"),
    [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    [method, setMethod] = useState<PaymentMethod>("cod"),
    [address, setAddress] = useState<Address>({
      recipient: "",
      phone: "",
      province: "",
      ward: "",
      addressLine: "",
      country: "VN",
    }),
    [review, setReview] = useState(false);
  useEffect(() => { setError(""); }, [s.locale]);
  const requestKey = useRef(""),
    payload = JSON.stringify({
      items: s.cart,
      promotionCode: s.promotionCode,
      points,
      credit,
    });
  useEffect(() => {
    setQuote(null);
    setReview(false);
    requestKey.current = "";
  }, [payload]);
  useEffect(() => {
    requestKey.current = "";
    setReview(false);
  }, [address, method]);
  const find = (id: string) => {
    for (const p of s.products) {
      const v = p.variants.find((x) => x.id === id);
      if (v) return { p, v };
    }
    return null;
  };
  async function price() {
    setBusy(true);
    setError("");
    try {
      const data = await api<Quote>("/api/quote", {
        items: s.cart,
        promotionCode: s.promotionCode,
        points: Number(points),
        creditVnd: credit,
      });
      setQuote(data);
      if (s.actor)
        await command("save_cart", {
          items: s.cart,
          promotionCode: s.promotionCode,
        });
      return data;
    } catch (e) {
      setError((e as Error).message);
      return null;
    } finally {
      setBusy(false);
    }
  }
  async function place() {
    if (!quote || !s.actor) return;
    setBusy(true);
    setError("");
    try {
      if (!requestKey.current) requestKey.current = crypto.randomUUID();
      const result = await command<{ orderId: string; orderNo: string }>(
        "place_order",
        {
          items: s.cart,
          promotionCode: s.promotionCode,
          points: quote.pointsUsed,
          creditVnd: quote.creditVnd,
          quoteHash: quote.quoteHash,
          address,
          method,
        },
        requestKey.current,
      );
      s.setCart([]);
      s.setPromotionCode("");
      router.push(`/${s.locale}/orders/${result.orderId}`);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  if (s.catalogueLoading)
    return <Busy text={s.tr("Đang tải giỏ hàng…", "Loading your bag…")} />;
  return (
    <div className="page-wrap">
      <div className="page-heading">
        <div>
          <span className="eyebrow">
            {checkout ? s.tr("THANH TOÁN", "CHECKOUT") : s.tr("LỰA CHỌN CỦA BẠN", "YOUR SELECTION")}
          </span>
          <h1>
            {checkout
              ? s.tr("Sẵn sàng kết nối.", "Ready to connect.")
              : s.tr("Giỏ hàng của bạn.", "Your shopping bag.")}
          </h1>
          <p>
            {s.tr(
              "Mọi khoản thanh toán được mô phỏng và ghi nhận bằng VND.",
              "All payments are simulated and recorded in VND.",
            )}
          </p>
        </div>
        <Link className="text-link" href={`/${s.locale}/products`}>
          {s.tr("Tiếp tục khám phá", "Continue exploring")}
          <ArrowRight size={15} />
        </Link>
      </div>
      {!s.cart.length ? (
        <Empty
          title={s.tr("Giỏ hàng đang trống", "Your bag is empty")}
          description={s.tr(
            "Một lựa chọn tuyệt vời đang đợi bạn.",
            "Your next favorite is waiting.",
          )}
        >
          <Link
            className="button button-primary"
            href={`/${s.locale}/products`}
          >
            {s.tr("Khám phá sản phẩm", "Explore products")}
          </Link>
        </Empty>
      ) : (
        <div className="two-column">
          <div className="stack">
            <section>
              {s.cart.map((item, index) => {
                const info = find(item.variantId);
                const photo = info ? productPhoto(info.p, info.v) : undefined;
                return (
                  <div
                    className="cart-line"
                    key={`${item.variantId}:${item.care}`}
                  >
                    <div className="cart-image">
                    {photo?.path ? <img
                      src={photo.path}
                      alt={photo.alt}
                      style={productPhotoStyle(photo)}
                      loading="lazy" decoding="async" width={100} height={100}
                    /> : null}
                    </div>
                    <div>
                      <h3>
                        {info?.p.name ??
                          s.tr(
                            "Sản phẩm không còn hiển thị",
                            "Product no longer available",
                          )}
                      </h3>
                      <p>
                        {info
                          ? Object.values(info.v.optionLabels ?? info.v.options).join(" · ")
                          : item.variantId}
                      </p>
                      {item.care && <p>+ BotZone Care</p>}
                      <div className="quantity">
                        <button
                          aria-label={s.tr(
                            "Giảm số lượng",
                            "Decrease quantity",
                          )}
                          disabled={item.quantity <= 1 || busy}
                          onClick={() =>
                            s.setCart(
                              s.cart.map((x, i) =>
                                i === index
                                  ? { ...x, quantity: x.quantity - 1 }
                                  : x,
                              ),
                            )
                          }
                        >
                          <Minus size={12} />
                        </button>
                        <span>{item.quantity}</span>
                        <button
                          aria-label={s.tr(
                            "Tăng số lượng",
                            "Increase quantity",
                          )}
                          disabled={
                            busy || item.quantity >= (info?.v.available ?? 0)
                          }
                          onClick={() =>
                            s.setCart(
                              s.cart.map((x, i) =>
                                i === index
                                  ? { ...x, quantity: x.quantity + 1 }
                                  : x,
                              ),
                            )
                          }
                        >
                          <Plus size={12} />
                        </button>
                      </div>
                      <Button
                        size="icon"
                        variant="ghost"
                        disabled={busy}
                        aria-label={s.tr("Xóa sản phẩm", "Remove item")}
                        onClick={() =>
                          s.setCart(s.cart.filter((_, i) => i !== index))
                        }
                      >
                        <Trash2 size={14} />
                      </Button>
                    </div>
                    <div className="cart-price">
                      {info
                        ? s.money(
                            (
                              BigInt(info.v.priceVnd) * BigInt(item.quantity)
                            ).toString(),
                          )
                        : "—"}
                    </div>
                  </div>
                );
              })}
            </section>
            <div className="panel form-stack">
              <h3>{s.tr("Ưu đãi dành cho bạn", "Make it yours")}</h3>
              <div className="form-grid">
                <Field label={s.tr("Mã ưu đãi", "Promotion code")}>
                  <Input
                    value={s.promotionCode}
                    onChange={(e) =>
                      s.setPromotionCode(e.target.value.toUpperCase())
                    }
                    placeholder="BOTZONE"
                  />
                </Field>
                <Field
                  label={s.tr("Điểm muốn sử dụng", "Points to redeem")}
                  hint={s.tr(
                    "1 điểm = 1.000 ₫. Tối đa 5% hàng sau voucher.",
                    "1 point = 1,000 ₫. Up to 5% of products after voucher.",
                  )}
                >
                  <Input
                    type="number"
                    min="0"
                    step="1"
                    value={points}
                    onChange={(e) => setPoints(e.target.value)}
                  />
                </Field>
                <Field
                  label={s.tr(
                    "Số dư mua hàng sử dụng (VND)",
                    "Store credit to use (VND)",
                  )}
                >
                  <Input
                    inputMode="numeric"
                    pattern="[0-9]+"
                    value={credit}
                    onChange={(e) =>
                      setCredit(e.target.value.replace(/\D/g, ""))
                    }
                  />
                </Field>
              </div>
              <div className="actions">
                <Button
                  variant="outline"
                  disabled={busy}
                  onClick={() => void price()}
                >
                  {busy
                    ? s.tr("Đang tính…", "Calculating…")
                    : s.tr("Áp dụng và tính lại", "Apply and recalculate")}
                </Button>
                {quote && s.promotionCode && (
                  <Badge tone="success">
                    {s.tr("Đã xác nhận ưu đãi", "Promotion confirmed")}
                  </Badge>
                )}
              </div>
            </div>
            {checkout &&
              (!s.actor ? (
                <div className="notice">
                  <ShieldCheck />
                  <div>
                    {s.tr(
                      "Đăng nhập để đặt hàng.",
                      "Sign in to place your order.",
                    )}
                    <Link
                      className="text-link"
                      style={{ display: "block", marginTop: 10 }}
                      href={`/${s.locale}/login?next=checkout`}
                    >
                      {s.tr("Đăng nhập / Đăng ký", "Sign in / Create account")}
                    </Link>
                  </div>
                </div>
              ) : (
                <form
                  id="checkout-form"
                  className="panel form-stack"
                  onSubmit={async (e) => {
                    e.preventDefault();
                    const result = await price();
                    if (result) setReview(true);
                  }}
                >
                  <h3>{s.tr("Thông tin giao hàng", "Delivery details")}</h3>
                  <div className="form-grid">
                    {(
                      [
                        ["recipient", "Người nhận", "Recipient"],
                        ["phone", "Số điện thoại", "Phone number"],
                        ["province", "Tỉnh / Thành phố", "Province / City"],
                        ["ward", "Phường / Xã", "Ward"],
                        ["addressLine", "Số nhà, tên đường", "Street address"],
                      ] as const
                    ).map(([key, vi, en]) => (
                      <Field key={key} label={s.tr(vi, en)}>
                        <Input
                          required
                          type={key === "phone" ? "tel" : "text"}
                          autoComplete={
                            key === "recipient"
                              ? "name"
                              : key === "phone"
                                ? "tel"
                                : key === "addressLine"
                                  ? "street-address"
                                  : "off"
                          }
                          value={address[key]}
                          onChange={(e) => {
                            setAddress({ ...address, [key]: e.target.value });
                            setReview(false);
                          }}
                        />
                      </Field>
                    ))}
                    <Field label={s.tr("Quốc gia", "Country")}>
                      <Input value={s.tr("Việt Nam", "Vietnam")} readOnly />
                    </Field>
                  </div>
                  <Field
                    label={s.tr(
                      "Phương thức thanh toán demo",
                      "Demo payment method",
                    )}
                  >
                    <Select
                      value={method}
                      onChange={(e) => {
                        setMethod(e.target.value as PaymentMethod);
                        setReview(false);
                      }}
                    >
                      <option value="cod">
                        {s.tr("COD — thu khi giao", "COD — pay on delivery")}
                      </option>
                      <option value="banking">
                        {s.tr(
                          "Chuyển khoản mô phỏng",
                          "Simulated bank transfer",
                        )}
                      </option>
                      <option value="momo">MoMo · {s.tr("Mô phỏng", "Demo")}</option>
                      <option value="visa">Visa · {s.tr("Mô phỏng", "Demo")}</option>
                    </Select>
                  </Field>
                  <div className="notice">
                    <ShieldCheck />
                    <span>
                      {s.tr(
                        "Không nhập số thẻ, CVV hoặc chuyển tiền thật. Tài khoản thanh toán trong đồ án chỉ là mô phỏng.",
                        "Do not enter real card details or transfer money. All payments in this course project are simulated.",
                      )}
                    </span>
                  </div>
                  <Button type="submit" disabled={busy}>
                    {s.tr("Rà soát đơn hàng", "Review order")}
                    <ArrowRight />
                  </Button>
                </form>
              ))}
            {error && <ErrorState message={error} />}
          </div>
          <aside className="panel summary-panel">
            <h3>{s.tr("Tóm tắt lựa chọn", "Your selection")}</h3>
            {quote ? (
              <>
                <div className="summary-line">
                  <span>{s.tr("Trước giảm", "Before discounts")}</span>
                  <Money value={quote.subtotalVnd} />
                </div>
                <div className="summary-line">
                  <span>{s.tr("Ưu đãi & điểm", "Discounts & points")}</span>
                  <span>
                    −<Money value={quote.discountVnd} />
                  </span>
                </div>
                <div className="summary-line">
                  <span>{s.tr("Phí giao hàng", "Delivery")}</span>
                  <Money value={quote.shippingVnd} />
                </div>
                <div className="summary-line summary-total">
                  <span>{s.tr("Tổng đơn", "Order total")}</span>
                  <Money value={quote.totalVnd} />
                </div>
                <div className="summary-line">
                  <span>{s.tr("Số dư mua hàng", "Store credit")}</span>
                  <Money value={quote.creditVnd} />
                </div>
                <div className="summary-line">
                  <strong>{s.tr("Thanh toán demo", "Demo payment")}</strong>
                  <strong>
                    <Money value={quote.cashDueVnd} />
                  </strong>
                </div>
                {quote.pointsUsed !== Number(points) && (
                  <p className="help-text">
                    {s.tr(
                      `Số điểm đủ điều kiện: ${quote.pointsUsed}.`,
                      `Eligible points: ${quote.pointsUsed}.`,
                    )}
                  </p>
                )}
              </>
            ) : (
              <p className="help-text">
                {s.tr(
                  "Nhấn tính lại để nhận tổng tiền, ưu đãi và tồn kho được xác nhận từ hệ thống.",
                  "Recalculate to confirm prices, discounts and availability.",
                )}
              </p>
            )}
            {checkout ? (
              review && s.actor ? (
                <>
                  <div className="notice success" style={{ marginTop: 20 }}>
                    <Check />
                    <span>
                      {s.tr(
                        "Kiểm tra thông tin bên dưới trước khi xác nhận.",
                        "Please review the details before confirming.",
                      )}
                    </span>
                  </div>
                  <p className="help-text" style={{ marginTop: 15 }}>
                    {address.recipient} · {address.phone}
                    <br />
                    {address.addressLine}, {address.ward}, {address.province}
                    <br />
                    {paymentLabel(method, s.locale)} · VND
                  </p>
                  <Button
                    disabled={busy || !quote || !s.actor.verified}
                    onClick={() => void place()}
                  >
                    {busy
                      ? s.tr("Đang đặt hàng…", "Placing order…")
                      : s.tr("Xác nhận đặt hàng", "Confirm order")}
                  </Button>
                  {!s.actor.verified && (
                    <p className="help-text">
                      {s.tr(
                        "Xác minh email trước khi đặt hàng.",
                        "Verify your email before placing an order.",
                      )}
                    </p>
                  )}
                </>
              ) : (
                <p className="help-text" style={{ marginTop: 20 }}>
                  {s.tr(
                    "Điền thông tin giao hàng rồi nhấn Rà soát đơn hàng.",
                    "Complete delivery details, then review your order.",
                  )}
                </p>
              )
            ) : (
              <Link
                className="button button-primary"
                href={`/${s.locale}/checkout`}
              >
                {s.tr("Tiến hành thanh toán", "Proceed to checkout")}
                <ArrowRight />
              </Link>
            )}
            <p className="help-text" style={{ marginTop: 18 }}>
              {s.tr(
                "Giá và tồn được kiểm tra lại khi xác nhận. Giỏ hàng chưa giữ sản phẩm.",
                "Prices and stock are rechecked on confirmation. Your bag does not reserve items.",
              )}
            </p>
          </aside>
        </div>
      )}
    </div>
  );
}

export { AuthPage } from "./auth-page";

export function Orders() {
  const s = useStore(),
    data = useResource<{ orders: Row[] }>(s.actor ? "/api/orders" : null);
  return (
    <RequireAccount>
      <div className="page-wrap">
        <div className="page-heading">
          <div>
            <span className="eyebrow">{s.tr("ĐƠN HÀNG CỦA BẠN", "YOUR ORDERS")}</span>
            <h1>{s.tr("Những lựa chọn của bạn.", "Your next chapters.")}</h1>
          </div>
          <Button variant="outline" onClick={() => void data.refresh()}>
            <RefreshCw />
            {s.tr("Làm mới", "Refresh")}
          </Button>
        </div>
        {data.error ? (
          <ErrorState message={data.error} retry={data.refresh} />
        ) : data.loading ? (
          <Busy />
        ) : data.data?.orders.length ? (
          data.data.orders.map((o) => (
            <Link
              className="order-card"
              key={str(o, "id")}
              href={`/${s.locale}/orders/${str(o, "id")}`}
            >
              <div>
                <h3>{str(o, "orderNo", "order_no")}</h3>
                <p>{date(str(o, "createdAt", "created_at"), s.locale)}</p>
              </div>
              <div className="actions">
                <OrderStatus value={str(o, "orderStatus", "order_status")} />
                <strong>
                  <Money value={moneyValue(o, "totalVnd", "total_vnd")} />
                </strong>
                <ArrowRight size={17} />
              </div>
            </Link>
          ))
        ) : (
          <Empty title={s.tr("Bạn chưa có đơn hàng", "No orders yet")}>
            <Link
              className="button button-primary"
              href={`/${s.locale}/products`}
            >
              {s.tr("Khám phá sản phẩm", "Explore products")}
            </Link>
          </Empty>
        )}
      </div>
    </RequireAccount>
  );
}

type OrderData = {
  order: Row;
  items: Row[];
  units: Row[];
  attempts: Row[];
  receipts: Row[];
  shipments: Row[];
  invoice: Row | null;
  events?: Row[];
};
export function OrderDetail({ id }: { id: string }) {
  const s = useStore(),
    data = useResource<OrderData>(s.actor ? `/api/orders/${id}` : null),
    [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    [attempt, setAttempt] = useState<Row | null>(null),
    [returning, setReturning] = useState(false),
    [reason, setReason] = useState("defective"),
    [unitIds, setUnitIds] = useState<string[]>([]),
    [evidenceIds, setEvidenceIds] = useState<string[]>([]),
    [uploadingEvidence, setUploadingEvidence] = useState(false),
    [actual, setActual] = useState("");
  useEffect(() => { setError(""); }, [s.locale, id]);
  const paymentKey = useRef(""),
    paymentStartKey = useRef(""),
    cancelKey = useRef(""),
    returnKey = useRef("");
  const d = data.data,
    o = d?.order,
    method = str(o, "paymentMethod", "payment_method"),
    status = str(o, "orderStatus", "order_status");
  async function act(name: string, input: unknown, key?: string) {
    setBusy(true);
    setError("");
    try {
      const result = await command<Row>(name, input, key);
      await data.refresh();
      return result;
    } catch (e) {
      setError((e as Error).message);
      return null;
    } finally {
      setBusy(false);
    }
  }
  async function beginPayment() {
    if (!paymentStartKey.current) paymentStartKey.current = crypto.randomUUID();
    const result = await act(
      "start_payment",
      { orderId: id },
      paymentStartKey.current,
    );
    if (result) {
      setAttempt(row(result.attempt ?? result));
      paymentKey.current = "";
      paymentStartKey.current = "";
    }
  }
  async function recordPayment(outcome: string) {
    if (!attempt) return;
    if (!paymentKey.current) paymentKey.current = crypto.randomUUID();
    const result = await act(
      "record_payment",
      {
        attemptId: str(attempt, "attemptId", "id"),
        outcome,
        amountVnd: moneyValue(
          attempt,
          "requestedAmountVnd",
          "requested_amount_vnd",
          "amountVnd",
        ),
      },
      paymentKey.current,
    );
    if (result) {
      setAttempt(null);
      paymentKey.current = "";
    }
  }
  async function requestReturn(e: React.FormEvent) {
    e.preventDefault();
    if (uploadingEvidence) return;
    const grouped = new Map<string, string[]>();
    for (const u of d?.units ?? []) {
      if (unitIds.includes(str(u, "id"))) {
        const key = str(u, "orderItemId", "order_item_id");
        grouped.set(key, [...(grouped.get(key) ?? []), str(u, "id")]);
      }
    }
    if (!grouped.size) {
      setError(
        s.tr("Chọn ít nhất một đơn vị hàng.", "Select at least one item."),
      );
      return;
    }
    if (!returnKey.current) returnKey.current = crypto.randomUUID();
    const result = await act(
      "request_return",
      {
        orderId: id,
        reason,
        evidenceIds,
        items: [...grouped].map(([orderItemId, ids]) => ({
          orderItemId,
          unitIds: ids,
          actualReceivedIdentifier:
            reason === "wrong_item" ? actual : undefined,
        })),
      },
      returnKey.current,
    );
    if (result) {
      setReturning(false);
      s.notify(s.tr("Đã gửi yêu cầu trả hàng.", "Return request submitted."));
    }
  }
  return (
    <RequireAccount>
      <div className={`page-wrap ${commerceStyles.detailPage}`}>
        <div className="breadcrumb">
          <Link href={`/${s.locale}/orders`}>{s.tr("Đơn hàng", "Orders")}</Link>
          <span>/</span>
          <span>{str(o, "orderNo", "order_no") || id}</span>
        </div>
        {data.loading ? (
          <Busy />
        ) : data.error ? (
          <ErrorState message={data.error} retry={data.refresh} />
        ) : (
          d && (
            <>
              <div className="page-heading">
                <div>
                  <span className="eyebrow">{s.tr("ĐƠN HÀNG CỦA BẠN", "YOUR ORDER")}</span>
                  <h1 style={{ fontSize: 38 }}>
                    {str(o, "orderNo", "order_no")}
                  </h1>
                  <p>{date(str(o, "createdAt", "created_at"), s.locale)}</p>
                </div>
                <div className="actions">
                  <OrderStatus value={status} />
                  <Button variant="outline" onClick={() => void data.refresh()}>
                    <RefreshCw />
                    {s.tr("Làm mới", "Refresh")}
                  </Button>
                </div>
              </div>
              <div className={`two-column ${commerceStyles.detailColumns}`}>
                <div className="stack">
                  <div className="panel">
                    <h3>{s.tr("Trong đơn hàng của bạn", "In your order")}</h3>
                    <LineItems items={d.items} />
                  </div>
                  <div className="panel">
                    <h3>{s.tr("Hành trình giao hàng", "Delivery journey")}</h3>
                    {d.shipments.length ? (
                      <div className="timeline">
                        {d.shipments.map((x) => (
                          <div className="timeline-item" key={str(x, "id")}>
                            <strong>
                              {str(x, "trackingNo", "tracking_no")}
                            </strong>
                            <div>
                              <OrderStatus value={str(x, "status")} />
                            </div>
                            <p>
                              {date(
                                str(
                                  x,
                                  "updatedAt",
                                  "updated_at",
                                  "createdAt",
                                  "created_at",
                                ),
                                s.locale,
                              )}
                            </p>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <p>
                        {s.tr(
                          "Đơn đang được chuẩn bị. Vận đơn sẽ xuất hiện sau khi bàn giao.",
                          "Your order is being prepared. Tracking appears after handoff.",
                        )}
                      </p>
                    )}
                  </div>
                  {d.invoice && (
                    <div className="panel row">
                      <div>
                        <h3 style={{ marginBottom: 8 }}>
                          {s.tr("Hóa đơn của bạn", "Your invoice")}
                        </h3>
                        <p>{str(d.invoice, "invoiceNo", "invoice_no")}</p>
                      </div>
                      <Link
                        className="button button-outline"
                        href={`/${s.locale}/invoices/${str(d.invoice, "id")}`}
                      >
                        {s.tr("Xem hóa đơn", "View invoice")}
                        <ArrowRight size={16} />
                      </Link>
                    </div>
                  )}
                  {returning && (
                    <form className="panel form-stack" onSubmit={requestReturn}>
                      <h3>{s.tr("Yêu cầu trả hàng", "Request a return")}</h3>
                      <p className="help-text">
                        {s.tr(
                          "Trong 14 ngày từ khi giao thành công, chỉ hàng lỗi hoặc giao sai. Hoàn theo giá trị và nguồn thanh toán đã phân bổ.",
                          "Within 14 days of delivery, for faulty or incorrect items. Refunds follow the original amounts and payment sources.",
                        )}
                      </p>
                      <Field label={s.tr("Lý do", "Reason")}>
                        <Select
                          value={reason}
                          onChange={(e) => {
                            setReason(e.target.value);
                            returnKey.current = "";
                          }}
                        >
                          <option value="defective">
                            {s.tr("Hàng lỗi", "Faulty item")}
                          </option>
                          <option value="wrong_item">
                            {s.tr("Giao sai hàng", "Incorrect item")}
                          </option>
                        </Select>
                      </Field>
                      {d.units
                        .filter((u) =>
                          d.items.some(
                            (i) =>
                              str(i, "id") ===
                                str(u, "orderItemId", "order_item_id") &&
                              str(i, "lineType", "line_type") === "product",
                          ),
                        )
                        .map((u) => (
                          <label className="checkbox-row" key={str(u, "id")}>
                            <input
                              type="checkbox"
                              checked={unitIds.includes(str(u, "id"))}
                              onChange={(e) => {
                                setUnitIds(
                                  e.target.checked
                                    ? [...unitIds, str(u, "id")]
                                    : unitIds.filter((x) => x !== str(u, "id")),
                                );
                                returnKey.current = "";
                              }}
                            />
                            {str(
                              row(
                                d.items.find(
                                  (item) =>
                                    str(item, "id") ===
                                    str(u, "orderItemId", "order_item_id"),
                                )?.snapshot,
                              ),
                              "name",
                              "productName",
                              "sku",
                            ) ||
                              str(u, "name", "sku") ||
                              s.tr("Sản phẩm", "Product")}{" "}
                            · #{str(u, "unitOrdinal", "unit_ordinal")} ·{" "}
                            <Money
                              value={moneyValue(
                                u,
                                "netAmountVnd",
                                "net_amount_vnd",
                              )}
                            />
                          </label>
                        ))}
                      {reason === "wrong_item" && (
                        <Field
                          label={s.tr(
                            "Mã / mô tả món thực nhận",
                            "Identifier / description of the item received",
                          )}
                        >
                          <Input
                            value={actual}
                            onChange={(e) => {
                              setActual(e.target.value);
                              returnKey.current = "";
                            }}
                          />
                        </Field>
                      )}
                      <EvidenceUploader
                        referenceType="order"
                        referenceId={id}
                        onBusyChange={setUploadingEvidence}
                        onChange={(ids) => {
                          setEvidenceIds(ids);
                          returnKey.current = "";
                        }}
                      />
                      <Button
                        type="submit"
                        disabled={busy || uploadingEvidence}
                      >
                        {s.tr("Gửi yêu cầu", "Submit request")}
                      </Button>
                    </form>
                  )}
                </div>
                <aside className="stack">
                  <div className="panel summary-panel">
                    <h3>{s.tr("Thanh toán", "Payment")}</h3>
                    <div className="summary-line">
                      <span>{s.tr("Phương thức", "Method")}</span>
                      <strong>{paymentLabel(method, s.locale)} · {s.tr("MÔ PHỎNG", "DEMO")}</strong>
                    </div>
                    <div className="summary-line summary-total">
                      <span>{s.tr("Tổng đơn", "Order total")}</span>
                      <Money value={moneyValue(o, "totalVnd", "total_vnd")} />
                    </div>
                    {d.receipts.length ? (
                      d.receipts.map((x) => (
                        <div className="summary-line" key={str(x, "id")}>
                          <span>
                            {str(x, "tenderKind", "tender_kind") ===
                            "store_credit"
                              ? s.tr("Đã dùng số dư", "Store credit used")
                              : s.tr("Đã thu demo", "Demo payment received")}
                          </span>
                          <Money
                            value={moneyValue(x, "amountVnd", "amount_vnd")}
                          />
                        </div>
                      ))
                    ) : (
                      <p className="help-text">
                        {s.tr(
                          "Chưa ghi nhận khoản thu. Đã giao không đồng nghĩa đã thu COD.",
                          "No payment recorded. Delivery does not mean COD has been collected.",
                        )}
                      </p>
                    )}
                    {["momo", "visa"].includes(method) &&
                      !["cancelled", "completed", "expired"].includes(
                        status,
                      ) && (
                        <Button
                          disabled={busy}
                          onClick={() => void beginPayment()}
                        >
                          {s.tr(
                            "Mở thanh toán mô phỏng",
                            "Open payment simulator",
                          )}
                        </Button>
                      )}
                    {method === "banking" && (
                      <div className="notice" style={{ marginTop: 18 }}>
                        <ShieldCheck />
                        <span>
                          {s.tr("Mã tham chiếu:", "Reference:")}{" "}
                          {str(o, "orderNo", "order_no")}.{" "}
                          {s.tr(
                            "Nhân viên sẽ xác nhận giao dịch demo. Không chuyển tiền vào tài khoản thật.",
                            "Staff will confirm the simulated transfer. Do not send real money.",
                          )}
                        </span>
                      </div>
                    )}
                    {attempt && (
                      <div className="form-stack" style={{ marginTop: 20 }}>
                        <h3>
                          {s.tr(
                            "Chọn kết quả demo",
                            "Choose a simulated outcome",
                          )}
                        </h3>
                        {(
                          [
                            "success",
                            "failed",
                            "cancelled",
                            "pending",
                            "expired",
                          ] as const
                        ).map((outcome, i) => (
                          <Button
                            key={outcome}
                            variant={
                              outcome === "success" ? "default" : "outline"
                            }
                            disabled={busy}
                            onClick={() => void recordPayment(outcome)}
                          >
                            {s.tr(
                              [
                                "Thành công",
                                "Thất bại",
                                "Hủy",
                                "Chờ xử lý",
                                "Hết hạn",
                              ][i],
                              [
                                "Success",
                                "Failed",
                                "Cancelled",
                                "Pending",
                                "Expired",
                              ][i],
                            )}
                          </Button>
                        ))}
                      </div>
                    )}
                  </div>
                  <div className="panel">
                    <h3>{s.tr("Địa chỉ giao hàng", "Delivery address")}</h3>
                    <AddressDisplay
                      address={row(o?.addressSnapshot ?? o?.address_snapshot)}
                    />
                  </div>
                  <div className="actions">
                    {[
                      "placed",
                      "pending",
                      "confirmed",
                      "awaiting_payment",
                    ].includes(status) && (
                      <Button
                        variant="outline"
                        disabled={busy}
                        onClick={() => {
                          if (
                            confirm(
                              s.tr("Hủy đơn hàng này?", "Cancel this order?"),
                            )
                          ) {
                            if (!cancelKey.current)
                              cancelKey.current = crypto.randomUUID();
                            void act(
                              "cancel_order",
                              { orderId: id },
                              cancelKey.current,
                            );
                          }
                        }}
                      >
                        {s.tr("Hủy đơn hàng", "Cancel order")}
                      </Button>
                    )}
                    {["delivered", "completed"].includes(status) && (
                      <Button
                        variant="outline"
                        onClick={() => setReturning(!returning)}
                      >
                        {s.tr("Yêu cầu trả hàng", "Request return")}
                      </Button>
                    )}
                  </div>
                </aside>
              </div>
              {error && (
                <div style={{ marginTop: 20 }}>
                  <ErrorState message={error} />
                </div>
              )}
            </>
          )
        )}
      </div>
    </RequireAccount>
  );
}
export function AddressDisplay({ address: a }: { address: Row }) {
  return (
    <p className="help-text">
      {str(a, "recipient")} · {str(a, "phone")}
      <br />
      {str(a, "addressLine", "address_line")}, {str(a, "ward")},{" "}
      {str(a, "province")}
    </p>
  );
}
export function LineItems({ items }: { items: Row[] }) {
  const s = useStore();
  return (
    <div className={`table-scroll ${commerceStyles.lineItems}`} role="region" aria-label={s.tr("Chi tiết sản phẩm và dịch vụ", "Product and service line items")} tabIndex={0}>
      <table>
        <thead>
          <tr>
            <th>{s.tr("Sản phẩm / dịch vụ", "Product / service")}</th>
            <th>{s.tr("SL", "Qty")}</th>
            <th>{s.tr("Đơn giá", "Unit price")}</th>
            <th>{s.tr("Giảm", "Discount")}</th>
            <th>{s.tr("Thành tiền", "Amount")}</th>
          </tr>
        </thead>
        <tbody>
          {items.map((i, index) => {
            const snapshot = row(i.snapshot);
            const kind = str(i, "lineType", "line_type");
            const savedName = str(i, "nameSnapshot", "name_snapshot", "name") ||
              str(snapshot, s.locale === "vi" ? "nameVi" : "nameEn", "name", "nameVi");
            // System service labels follow the interface locale; immutable product snapshots remain verbatim.
            const displayName = kind === "shipping" ? s.tr("Giao hàng Việt Nam", "Delivery in Vietnam") :
              kind === "warranty" && !savedName ? "BotZone Care" : savedName || lineTypeLabel(kind, s.locale);
            return (
              <tr key={str(i, "id") || index} data-line-kind={kind}>
                <td>
                  <strong>
                    {displayName}
                  </strong>
                  <br />
                  <span className="muted">
                    {kind === "shipping" ? s.tr("Dịch vụ vận chuyển", "Delivery service") :
                      str(i, "skuSnapshot", "sku_snapshot", "sku") || str(snapshot, "sku")}
                  </span>
                </td>
                <td>{str(i, "quantity")}</td>
                <td className="table-money">
                  <Money
                    value={moneyValue(i, "unitPriceVnd", "unit_price_vnd")}
                  />
                </td>
                <td className="table-money">
                  <Money
                    value={(
                      BigInt(
                        moneyValue(
                          i,
                          "voucherDiscountVnd",
                          "voucher_discount_vnd",
                        ),
                      ) +
                      BigInt(
                        moneyValue(
                          i,
                          "pointsDiscountVnd",
                          "points_discount_vnd",
                        ),
                      )
                    ).toString()}
                  />
                </td>
                <td className="table-money">
                  <Money
                    value={moneyValue(i, "netAmountVnd", "net_amount_vnd")}
                  />
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

export function InvoiceDetail({ id }: { id: string }) {
  const s = useStore(),
    data = useResource<{
      invoice: Row;
      items: Row[];
      receipts: Row[];
      creditNotes: Row[];
      refunds: Row[];
    }>(s.actor ? `/api/invoices/${id}` : null),
    [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    [exported, setExported] = useState<{
      links: Record<string, string>;
      readAt: string;
    } | null>(null);
  useEffect(() => { setError(""); setExported(null); }, [s.locale, id]);
  async function createExport() {
    setBusy(true);
    setError("");
    try {
      setExported(
        await api("/api/invoices/exports", {
          invoiceId: id,
          kind: "detail",
          formats: ["xlsx", "pdf"],
          locale: s.locale,
        }),
      );
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  const inv = data.data?.invoice;
  return (
    <RequireAccount>
      <div className="page-wrap">
        <div className="page-heading">
          <div>
            <span className="eyebrow">
              BOTZONE · {s.tr("CHỨNG TỪ DEMO", "DEMO DOCUMENT")}
            </span>
            <h1 className="invoice-title">
              {str(inv, "invoiceNo", "invoice_no") ||
                s.tr("Hóa đơn", "Invoice")}
            </h1>
            <p>
              {s.tr(
                "Chứng từ bán hàng mô phỏng, không phải hóa đơn điện tử pháp lý.",
                "Simulated sales document, not a legal electronic invoice.",
              )}
            </p>
          </div>
          <Button disabled={busy || !inv} onClick={() => void createExport()}>
            <Download />
            {s.tr("Tạo XLSX & PDF", "Create XLSX & PDF")}
          </Button>
        </div>
        {data.error ? (
          <ErrorState message={data.error} retry={data.refresh} />
        ) : data.loading ? (
          <Busy />
        ) : (
          data.data && (
            <div className="panel stack">
              <div className="invoice-head">
                <div>
                  <h3>{s.tr("Khách hàng", "Customer")}</h3>
                  <p>
                    {str(
                      row(inv?.customerSnapshot ?? inv?.customer_snapshot),
                      "name",
                      "displayName",
                      "email",
                    )}
                  </p>
                </div>
                <div>
                  <h3>{s.tr("Phát hành", "Issued")}</h3>
                  <p>{date(str(inv, "issuedAt", "issued_at"), s.locale)}</p>
                  <p>
                    {s.tr("Nhân viên phụ trách:", "Responsible staff:")}{" "}
                    {str(
                      row(inv?.staffSnapshot ?? inv?.staff_snapshot),
                      "name",
                      "staffCode",
                    )}
                  </p>
                </div>
              </div>
              <LineItems items={data.data.items} />
              <div style={{ alignSelf: "flex-end", minWidth: 260 }}>
                <div className="summary-line">
                  <span>{s.tr("Trước giảm", "Subtotal")}</span>
                  <Money
                    value={moneyValue(inv, "subtotalVnd", "subtotal_vnd")}
                  />
                </div>
                <div className="summary-line">
                  <span>{s.tr("Giảm giá", "Discount")}</span>
                  <Money
                    value={moneyValue(
                      inv,
                      "discountTotalVnd",
                      "discount_total_vnd",
                    )}
                  />
                </div>
                <div className="summary-line summary-total">
                  <span>
                    {s.tr("Tổng hóa đơn gốc", "Original invoice total")}
                  </span>
                  <Money
                    value={moneyValue(inv, "grandTotalVnd", "grand_total_vnd")}
                  />
                </div>
              </div>
              <div>
                <h3>{s.tr("Khoản thu", "Payment receipts")}</h3>
                {data.data.receipts.length ? (
                  data.data.receipts.map((x) => (
                    <div className="summary-line" key={str(x, "id")}>
                      <span>
                        {paymentLabel(str(x, "tenderKind", "tender_kind"), s.locale)} ·{" "}
                        {date(str(x, "receivedAt", "received_at"), s.locale)}
                      </span>
                      <Money value={moneyValue(x, "amountVnd", "amount_vnd")} />
                    </div>
                  ))
                ) : (
                  <p>
                    {s.tr("Chưa ghi nhận khoản thu.", "No receipts recorded.")}
                  </p>
                )}
              </div>
              <div>
                <h3>
                  {s.tr("Điều chỉnh giảm & hoàn tiền", "Adjustments & refunds")}
                </h3>
                {[...data.data.creditNotes, ...data.data.refunds].length ? (
                  [...data.data.creditNotes, ...data.data.refunds].map((x) => (
                    <div className="summary-line" key={str(x, "id")}>
                      <span>
                        {str(x, "creditNoteNo", "credit_note_no", "id")} ·{" "}
                        <OrderStatus value={str(x, "status")} />
                      </span>
                      <Money
                        value={moneyValue(
                          x,
                          "totalVnd",
                          "total_vnd",
                          "amountVnd",
                          "amount_vnd",
                        )}
                      />
                    </div>
                  ))
                ) : (
                  <p>
                    {s.tr(
                      "Chưa có điều chỉnh hoặc khoản hoàn.",
                      "No adjustments or refunds.",
                    )}
                  </p>
                )}
              </div>
            </div>
          )
        )}
        {error && <ErrorState message={error} />}{" "}
        {exported && (
          <div className="notice success" style={{ marginTop: 20 }}>
            <Check />
            <div>
              <p>
                {s.tr(
                  "Hai tệp cùng thời điểm dữ liệu:",
                  "Both files use the same dataset timestamp:",
                )}{" "}
                {date(exported.readAt, s.locale)}
              </p>
              <div className="actions" style={{ marginTop: 10 }}>
                {Object.entries(exported.links).map(([format, url]) => (
                  <a
                    key={format}
                    className="button button-outline button-sm"
                    href={url}
                    download
                  >
                    <Download size={14} />
                    {format.toUpperCase()}
                  </a>
                ))}
              </div>
            </div>
          </div>
        )}
      </div>
    </RequireAccount>
  );
}
