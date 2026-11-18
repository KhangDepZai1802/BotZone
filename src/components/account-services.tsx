"use client";
import React, { useEffect, useRef, useState } from "react";
import Link from "next/link";
import {
  ArrowRight,
  ArrowUpRight,
  Bell,
  Check,
  ChevronRight,
  Heart,
  LogOut,
  Plus,
  Recycle,
  RefreshCw,
  ShieldCheck,
  Trash2,
  Truck,
  Wallet,
} from "lucide-react";
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
import { AddressDisplay, OrderStatus, RequireAccount } from "./commerce";
import { date, moneyValue, row, rows, str, type Row } from "./records";
import { EvidenceUploader } from "./evidence-uploader";
import { variantLabel } from "./product-labels";
import { accountLabel } from "./account-labels";
import { updateQueryUrl, useQueryParams } from "./url-state";
type AccountData = {
  creditVnd: string;
  points: number;
  tier: string;
  creditEntries: Row[];
  loyaltyEntries: Row[];
  tradeIns: Row[];
  returns: Row[];
  entitlements: Row[];
  claims: Row[];
  addresses: Row[];
  notifications: Row[];
  profile?: Row;
  wishlist?: string[];
};

export { ProductCompare as Compare } from "./product-compare";

export function Account() {
  const s = useStore(),
    params = useQueryParams(),
    data = useResource<AccountData>(s.actor ? "/api/account" : null),
    [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    [name, setName] = useState(""),
    [phone, setPhone] = useState(""),
    [address, setAddress] = useState({
      recipient: "",
      phone: "",
      province: "",
      ward: "",
      addressLine: "",
      isDefault: true,
    });
  const a = data.data;
  useEffect(() => {
    if (s.actor) setName(s.actor.name);
  }, [s.actor]);
  useEffect(() => {
    if (a?.profile) {
      setName(
        str(a.profile, "name", "displayName", "display_name") ||
          s.actor?.name ||
          "",
      );
      setPhone(str(a.profile, "phone"));
    }
  }, [a?.profile]);
  async function run(name: string, input: unknown) {
    setBusy(true);
    setError("");
    try {
      await command(name, input);
      await data.refresh();
      s.notify(s.tr("Đã lưu thay đổi.", "Changes saved."));
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function logout() {
    setBusy(true);
    try {
      await api("/api/auth/logout", {});
      await s.refreshSession();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  const tabs = [
    ["overview", "Tổng quan", "Overview"],
    ["credit", "Số dư & điểm", "Credit & points"],
    ["tradeIns", "Thu cũ", "Trade in"],
    ["returns", "Trả hàng", "Returns"],
    ["warranty", "Bảo hành", "Warranty"],
    ["profile", "Hồ sơ & địa chỉ", "Profile & addresses"],
    ["notifications", "Thông báo", "Notifications"],
  ];
  const requestedTab = params.get("tab");
  const tab = tabs.some(([id]) => id === requestedTab) ? requestedTab! : "overview";
  const setTab = (value: string) => {
    const query = new URLSearchParams(window.location.search);
    if (value === "overview") query.delete("tab"); else query.set("tab", value);
    const href = `/${s.locale}/account${query.size ? `?${query}` : ""}`;
    if (href !== window.location.pathname + window.location.search) updateQueryUrl(href);
  };
  return (
    <RequireAccount>
      <div className="page-wrap">
        <div className="page-heading">
          <div>
            <span className="eyebrow">{s.tr("BOTZONE CỦA BẠN", "YOUR BOTZONE")}</span>
            <h1>
              {s.tr("Xin chào,", "Hello,")} {s.actor?.name.split(" ").pop()}.
            </h1>
            <p>
              {s.tr(
                "Mọi trải nghiệm của bạn, trong cùng một không gian.",
                "All your experiences, in one place.",
              )}
            </p>
          </div>
          <div className="actions">
            <Button variant="outline" onClick={() => void data.refresh()}>
              <RefreshCw />
              {s.tr("Làm mới", "Refresh")}
            </Button>
            <Button
              variant="ghost"
              disabled={busy}
              onClick={() => void logout()}
            >
              <LogOut />
              {s.tr("Đăng xuất", "Sign out")}
            </Button>
          </div>
        </div>
        {error && <ErrorState message={error} />}{" "}
        {data.error ? (
          <ErrorState message={data.error} retry={data.refresh} />
        ) : data.loading ? (
          <Busy />
        ) : (
          a && (
            <>
              <div className="account-kpis">
                <div className="kpi-card">
                  <span>{s.tr("Hạng thành viên", "Membership")}</span>
                  <strong>{accountLabel(a.tier || "Member", s.locale)}</strong>
                  <small>
                    {s.tr(
                      "Theo chi tiêu hàng hợp lệ ròng",
                      "Based on eligible net spending",
                    )}
                  </small>
                </div>
                <div className="kpi-card">
                  <span>{s.tr("Số dư mua hàng", "Store credit")}</span>
                  <strong>
                    <Money value={a.creditVnd} />
                  </strong>
                  <small>
                    {s.tr(
                      "Dùng nhiều lần · Không hết hạn",
                      "Reusable · No expiry",
                    )}
                  </small>
                </div>
                <div className="kpi-card">
                  <span>{s.tr("Điểm của bạn", "Your points")}</span>
                  <strong>{new Intl.NumberFormat(s.locale === "vi" ? "vi-VN" : "en-US").format(a.points)}</strong>
                  <small>{s.tr("1 điểm = 1.000 ₫", "1 point = 1,000 ₫")}</small>
                </div>
              </div>
              <div
                className="tabs"
                role="tablist"
                aria-label={s.tr("Tài khoản", "Account")}
              >
                {tabs.map(([id, vi, en], index) => (
                  <button
                    role="tab"
                    id={`account-tab-${id}`}
                    aria-controls="account-tab-panel"
                    tabIndex={tab === id ? 0 : -1}
                    aria-selected={tab === id}
                    className={tab === id ? "active" : ""}
                    key={id}
                    onClick={() => setTab(id)}
                    onKeyDown={(event) => {
                      const next =
                        event.key === "ArrowRight"
                          ? (index + 1) % tabs.length
                          : event.key === "ArrowLeft"
                            ? (index - 1 + tabs.length) % tabs.length
                            : event.key === "Home"
                              ? 0
                              : event.key === "End"
                                ? tabs.length - 1
                                : -1;
                      if (next < 0) return;
                      event.preventDefault();
                      setTab(tabs[next][0]);
                      document
                        .getElementById(`account-tab-${tabs[next][0]}`)
                        ?.focus();
                    }}
                  >
                    {s.tr(vi, en)}
                  </button>
                ))}
              </div>
              <div
                id="account-tab-panel"
                role="tabpanel"
                aria-labelledby={`account-tab-${tab}`}
                tabIndex={0}
              >
                {tab === "overview" && (
                  <div className="editorial-grid">
                    <div className="panel">
                      <h3>{s.tr("Đơn hàng của bạn", "Your orders")}</h3>
                      <p>
                        {s.tr(
                          "Theo dõi giao hàng, thanh toán và hóa đơn.",
                          "Track deliveries, payments and invoices.",
                        )}
                      </p>
                      <Link
                        className="button button-primary"
                        style={{ marginTop: 23 }}
                        href={`/${s.locale}/orders`}
                      >
                        {s.tr("Xem đơn hàng", "View orders")}
                        <ArrowRight size={16} />
                      </Link>
                    </div>
                    <div className="panel">
                      <h3>
                        {s.tr("Yêu thích & so sánh", "Wishlist & comparison")}
                      </h3>
                      <p>
                        {s.tr(
                          "Những lựa chọn bạn muốn khám phá thêm.",
                          "The products you want to discover next.",
                        )}
                      </p>
                      <div className="actions" style={{ marginTop: 23 }}>
                        <Link
                          className="button button-outline"
                          href={`/${s.locale}/wishlist`}
                        >
                          <Heart size={16} />
                          {s.tr("Yêu thích", "Wishlist")} ({s.wishlist.length})
                        </Link>
                        <Link
                          className="button button-outline"
                          href={`/${s.locale}/compare`}
                        >
                          {s.tr("So sánh", "Compare")} ({s.compare.length})
                        </Link>
                      </div>
                    </div>
                  </div>
                )}
                {tab === "credit" && (
                  <div className="stack">
                    <Ledger
                      title={s.tr(
                        "Lịch sử số dư mua hàng",
                        "Store credit history",
                      )}
                      entries={a.creditEntries}
                      money
                    />
                    <Ledger
                      title={s.tr("Lịch sử điểm", "Points history")}
                      entries={a.loyaltyEntries}
                    />
                    <div className="notice">
                      <Wallet />
                      <span>
                        {s.tr(
                          "Số dư thu cũ là nguồn thanh toán, không đổi thành tiền mặt. Điểm là giảm giá, không phải khoản tiền hoàn.",
                          "Trade-in credit is a payment source and cannot be withdrawn as cash. Points are a discount, not cash refunds.",
                        )}
                      </span>
                    </div>
                  </div>
                )}
                {tab === "tradeIns" && (
                  <TradeInHistory
                    requests={a.tradeIns}
                    onRefresh={data.refresh}
                  />
                )}{" "}
                {tab === "returns" && (
                  <RequestHistory
                    title={s.tr("Yêu cầu trả hàng", "Return requests")}
                    entries={a.returns}
                    empty={s.tr(
                      "Yêu cầu trả hàng được tạo từ chi tiết đơn đã giao.",
                      "Create a return request from a delivered order.",
                    )}
                    type="returns"
                  />
                )}
                {tab === "warranty" && (
                  <WarrantyArea
                    entitlements={a.entitlements}
                    claims={a.claims}
                    onRefresh={data.refresh}
                  />
                )}{" "}
                {tab === "profile" && (
                  <div className="two-column">
                    <form
                      className="panel form-stack"
                      onSubmit={(e) => {
                        e.preventDefault();
                        void run("save_profile", { name, phone });
                      }}
                    >
                      <h3>{s.tr("Hồ sơ của bạn", "Your profile")}</h3>
                      <Field label={s.tr("Họ và tên", "Full name")}>
                        <Input
                          required
                          value={name}
                          onChange={(e) => setName(e.target.value)}
                        />
                      </Field>
                      <Field label="Email">
                        <Input readOnly value={s.actor?.email ?? ""} />
                      </Field>
                      <Field label={s.tr("Điện thoại", "Phone")}>
                        <Input
                          type="tel"
                          value={phone}
                          onChange={(e) => setPhone(e.target.value)}
                        />
                      </Field>
                      <Button disabled={busy} type="submit">
                        {s.tr("Lưu hồ sơ", "Save profile")}
                      </Button>
                      <Link
                        className="text-link"
                        href={`/${s.locale}/change-password`}
                      >
                        {s.tr("Đổi mật khẩu", "Change password")}
                      </Link>
                    </form>
                    <div className="stack">
                      <div className="panel">
                        <h3>{s.tr("Sổ địa chỉ", "Address book")}</h3>
                        {a.addresses.length ? (
                          a.addresses.map((x) => (
                            <div key={str(x, "id")} className="order-card">
                              <AddressDisplay address={x} />
                              {x.isDefault === true || x.is_default === true ? (
                                <Badge>{s.tr("Mặc định", "Default")}</Badge>
                              ) : null}
                            </div>
                          ))
                        ) : (
                          <p>
                            {s.tr("Chưa lưu địa chỉ.", "No saved addresses.")}
                          </p>
                        )}
                      </div>
                      <form
                        className="panel form-stack"
                        onSubmit={(e) => {
                          e.preventDefault();
                          void run("save_address", address);
                        }}
                      >
                        <h3>{s.tr("Thêm địa chỉ", "Add address")}</h3>
                        <div className="form-grid">
                          {(
                            [
                              ["recipient", "Người nhận", "Recipient"],
                              ["phone", "Điện thoại", "Phone"],
                              [
                                "province",
                                "Tỉnh / Thành phố",
                                "Province / City",
                              ],
                              ["ward", "Phường / Xã", "Ward"],
                              [
                                "addressLine",
                                "Số nhà, tên đường",
                                "Street address",
                              ],
                            ] as const
                          ).map(([key, vi, en]) => (
                            <Field key={key} label={s.tr(vi, en)}>
                              <Input
                                required
                                value={address[key]}
                                onChange={(e) =>
                                  setAddress({
                                    ...address,
                                    [key]: e.target.value,
                                  })
                                }
                              />
                            </Field>
                          ))}
                        </div>
                        <label className="checkbox-row">
                          <input
                            type="checkbox"
                            checked={address.isDefault}
                            onChange={(e) =>
                              setAddress({
                                ...address,
                                isDefault: e.target.checked,
                              })
                            }
                          />
                          {s.tr("Đặt làm mặc định", "Set as default")}
                        </label>
                        <Button disabled={busy} type="submit">
                          {s.tr("Lưu địa chỉ", "Save address")}
                        </Button>
                      </form>
                    </div>
                  </div>
                )}
                {tab === "notifications" &&
                  (a.notifications.length ? (
                    <div className="stack">
                      {a.notifications.map((x) => (
                        <div className="panel row" key={str(x, "id")}>
                          <div>
                            <h3>{str(x, "title") || s.tr("Cập nhật từ BotZone", "An update from BotZone")}</h3>
                            <p>{str(x, "message", "body")}</p>
                            <small className="muted">
                              {date(
                                str(x, "createdAt", "created_at"),
                                s.locale,
                              )}
                            </small>
                          </div>
                          {!str(x, "readAt", "read_at") && (
                            <Button
                              size="sm"
                              variant="outline"
                              disabled={busy}
                              onClick={() =>
                                void run("read_notification", {
                                  id: str(x, "id"),
                                })
                              }
                            >
                              {s.tr("Đánh dấu đã đọc", "Mark read")}
                            </Button>
                          )}
                        </div>
                      ))}
                    </div>
                  ) : (
                    <Empty
                      title={s.tr(
                        "Bạn đã xem hết thông báo",
                        "You are all caught up",
                      )}
                    />
                  ))}
              </div>
            </>
          )
        )}
      </div>
    </RequireAccount>
  );
}
function Ledger({
  title,
  entries,
  money = false,
}: {
  title: string;
  entries: Row[];
  money?: boolean;
}) {
  const s = useStore();
  return (
    <section className="panel">
      <h3>{title}</h3>
      {entries.length ? (
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th>{s.tr("Ngày", "Date")}</th>
                <th>{s.tr("Nội dung", "Description")}</th>
                <th>{s.tr("Phát sinh", "Change")}</th>
              </tr>
            </thead>
            <tbody>
              {entries.map((x) => (
                <tr key={str(x, "id")}>
                  <td>{date(str(x, "createdAt", "created_at"), s.locale)}</td>
                  <td>{str(x, "kind") ? accountLabel(str(x, "kind"), s.locale, s.tr("Điều chỉnh tài khoản", "Account adjustment")) : str(x, "description")}</td>
                  <td className="table-money">
                    {money ? (
                      <Money value={moneyValue(x, "deltaVnd", "delta_vnd")} />
                    ) : (
                      new Intl.NumberFormat(s.locale === "vi" ? "vi-VN" : "en-US").format(Number(str(x, "delta") || 0))
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <p>{s.tr("Chưa có phát sinh.", "No transactions yet.")}</p>
      )}
    </section>
  );
}
function RequestHistory({
  title,
  entries,
  empty,
  type,
}: {
  title: string;
  entries: Row[];
  empty: string;
  type: string;
}) {
  const s = useStore();
  return (
    <div className="stack">
      <h3>{title}</h3>
      {entries.length ? (
        entries.map((x) => (
          <div className="panel" key={str(x, "id")}>
            <div className="row">
              <strong>
                {str(x, "code", "requestNo", "request_no", "id").slice(0, 36)}
              </strong>
              <OrderStatus value={str(x, "state", "status")} />
            </div>
            <p className="help-text" style={{ marginTop: 12 }}>
              {date(str(x, "createdAt", "created_at"), s.locale)} ·{" "}
              {accountLabel(str(x, "reason", "description"), s.locale)}
            </p>
            {type === "returns" && (
              <p className="help-text">
                {s.tr(
                  "Kết quả kiểm tra và khoản hoàn được cập nhật sau khi nhân viên xử lý.",
                  "Inspection and refund results are updated after staff review.",
                )}
              </p>
            )}
          </div>
        ))
      ) : (
        <Empty
          title={s.tr("Chưa có hồ sơ", "No requests yet")}
          description={empty}
        />
      )}
    </div>
  );
}

type TradeItem = {
  productId: string;
  variantId: string;
  quantity: number;
  identifier: string;
  condition: {
    condition: string;
    battery: string;
    screen: string;
    components: string;
    notes: string;
  };
};
function freshTradeItem(): TradeItem {
  return {
    productId: "",
    variantId: "",
    quantity: 1,
    identifier: "",
    condition: {
      condition: "working",
      battery: "",
      screen: "",
      components: "",
      notes: "",
    },
  };
}
export function TradeIn() {
  const s = useStore(),
    [items, setItems] = useState<TradeItem[]>([freshTradeItem()]),
    [method, setMethod] = useState("store"),
    [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    [submitted, setSubmitted] = useState(false),
    key = useRef("");
  const update = (i: number, patch: Partial<TradeItem>) => {
    setItems(
      items.map((item, index) => (index === i ? { ...item, ...patch } : item)),
    );
    key.current = "";
  };
  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError("");
    try {
      if (!key.current) key.current = crypto.randomUUID();
      await command(
        "request_trade_in",
        {
          receptionMethod: method,
          items: items.map((x) => ({
            ...x,
            variantId: x.variantId || undefined,
            identifier: x.identifier || undefined,
          })),
        },
        key.current,
      );
      setSubmitted(true);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <div className="page-wrap">
      <section className="service-hero">
        <div>
          <span className="eyebrow">{s.tr("THU CŨ ĐỔI MỚI BOTZONE", "BOTZONE TRADE IN")}</span>
          <h1>
            {s.tr(
              "Một chương mới.\nCho thiết bị cũ.",
              "A new chapter.\nFor your old device.",
            )}
          </h1>
          <p>
            {s.tr(
              "Từ iPhone đến phụ kiện Apple. Trao lại thiết bị đã đồng hành, nhận số dư mua hàng để tiếp tục những trải nghiệm mới.",
              "From iPhone to Apple accessories. Give your device a next chapter and receive store credit for what comes next.",
            )}
          </p>
          <a className="button button-primary" href="#trade-form">
            {s.tr("Bắt đầu thu cũ", "Start your trade in")}
            <ArrowRight size={16} />
          </a>
        </div>
        <Recycle aria-hidden />
      </section>
      <div className="step-grid">
        {[
          [
            "01",
            "Khai tình trạng",
            "Tell us about it",
            "Chọn thiết bị và mô tả tình trạng thực tế.",
            "Choose your device and describe its condition.",
          ],
          [
            "02",
            "Nhận và kiểm định",
            "Receive and inspect",
            "Gửi từ xa hoặc mang đến điểm tiếp nhận mô phỏng.",
            "Send it in or visit the simulated collection point.",
          ],
          [
            "03",
            "Bạn quyết định",
            "You decide",
            "Xem báo giá cuối, chấp nhận hoặc nhận lại thiết bị.",
            "Review the final offer, accept it or get your device back.",
          ],
        ].map(([n, vi, en, dvi, den]) => (
          <div key={n}>
            <span>{n}</span>
            <h3>{s.tr(vi, en)}</h3>
            <p>{s.tr(dvi, den)}</p>
          </div>
        ))}
      </div>
      <div className="notice" style={{ marginBottom: 30 }}>
        <ShieldCheck />
        <span>
          {s.tr(
            "Sao lưu, đăng xuất Apple ID và xóa dữ liệu trước bàn giao. Không cung cấp mật khẩu. BotZone chịu phí gửi đến và gửi trả. Số dư chỉ được cấp sau kiểm định và khi bạn đồng ý báo giá cuối.",
            "Back up, sign out of Apple ID and erase your device before handoff. Never share passwords. BotZone covers inbound and return shipping. Credit is issued only after inspection and your acceptance of the final offer.",
          )}
        </span>
      </div>
      <section id="trade-form">
        {!s.actor ? (
          <Empty title={s.tr("Đăng nhập để bắt đầu", "Sign in to get started")}>
            <Link className="button button-primary" href={`/${s.locale}/login`}>
              {s.tr("Đăng nhập", "Sign in")}
            </Link>
          </Empty>
        ) : submitted ? (
          <div className="panel empty">
            <Check size={36} />
            <h3>
              {s.tr("Đã gửi yêu cầu thu cũ", "Trade-in request submitted")}
            </h3>
            <p>
              {s.tr(
                "Theo dõi tiếp nhận, kiểm định và báo giá trong tài khoản.",
                "Follow reception, inspection and offers in your account.",
              )}
            </p>
            <Link
              className="button button-primary"
              href={`/${s.locale}/account`}
            >
              {s.tr("Về tài khoản", "Go to account")}
            </Link>
          </div>
        ) : (
          <form onSubmit={submit} className="stack">
            <div className="section-heading">
              <h2>{s.tr("Thiết bị của bạn.", "Your devices.")}</h2>
              <Button
                type="button"
                variant="outline"
                onClick={() => {
                  setItems([...items, freshTradeItem()]);
                  key.current = "";
                }}
              >
                <Plus />
                {s.tr("Thêm món", "Add item")}
              </Button>
            </div>
            {items.map((item, i) => {
              const product = s.products.find((p) => p.id === item.productId);
              return (
                <div className="panel form-stack" key={i}>
                  <div className="row">
                    <h3 style={{ margin: 0 }}>
                      {s.tr("Món", "Item")} {i + 1}
                    </h3>
                    {items.length > 1 && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        aria-label={s.tr("Xóa món", "Remove item")}
                        onClick={() => {
                          setItems(items.filter((_, index) => i !== index));
                          key.current = "";
                        }}
                      >
                        <Trash2 />
                      </Button>
                    )}
                  </div>
                  <div className="form-grid">
                    <Field label={s.tr("Sản phẩm Apple", "Apple product")}>
                      <Select
                        required
                        value={item.productId}
                        onChange={(e) =>
                          update(i, {
                            productId: e.target.value,
                            variantId: "",
                          })
                        }
                      >
                        <option value="">
                          {s.tr("Chọn sản phẩm", "Choose a product")}
                        </option>
                        {s.products.map((p) => (
                          <option key={p.id} value={p.id}>
                            {p.name}
                          </option>
                        ))}
                      </Select>
                    </Field>
                    <Field
                      label={s.tr(
                        "Phiên bản (nếu có)",
                        "Configuration (optional)",
                      )}
                    >
                      <Select
                        value={item.variantId}
                        onChange={(e) =>
                          update(i, { variantId: e.target.value })
                        }
                      >
                        <option value="">
                          {s.tr("Chưa xác định", "Not specified")}
                        </option>
                        {product?.variants.map((v) => (
                          <option key={v.id} value={v.id}>
                            {variantLabel(v)}
                          </option>
                        ))}
                      </Select>
                    </Field>
                    <Field label={s.tr("Số lượng", "Quantity")}>
                      <Input
                        type="number"
                        min="1"
                        max="20"
                        required
                        value={item.quantity}
                        onChange={(e) =>
                          update(i, { quantity: Number(e.target.value) })
                        }
                      />
                    </Field>
                    <Field
                      label={s.tr(
                        "IMEI / Serial (nếu có)",
                        "IMEI / Serial (if applicable)",
                      )}
                    >
                      <Input
                        value={item.identifier}
                        onChange={(e) =>
                          update(i, { identifier: e.target.value })
                        }
                      />
                    </Field>
                    <Field
                      label={s.tr(
                        "Tình trạng hoạt động",
                        "Functional condition",
                      )}
                    >
                      <Select
                        value={item.condition.condition}
                        onChange={(e) =>
                          update(i, {
                            condition: {
                              ...item.condition,
                              condition: e.target.value,
                            },
                          })
                        }
                      >
                        <option value="working">
                          {s.tr("Hoạt động bình thường", "Fully working")}
                        </option>
                        <option value="damaged">
                          {s.tr("Có lỗi hoặc hư hỏng", "Faulty or damaged")}
                        </option>
                        <option value="not_working">
                          {s.tr("Không hoạt động", "Not working")}
                        </option>
                      </Select>
                    </Field>
                    {["iphone", "ipad", "mac"].includes(
                      product?.category ?? "",
                    ) && (
                      <>
                        <Field
                          label={s.tr(
                            "Pin / số chu kỳ / tình trạng",
                            "Battery health / cycles / condition",
                          )}
                        >
                          <Input
                            value={item.condition.battery}
                            onChange={(e) =>
                              update(i, {
                                condition: {
                                  ...item.condition,
                                  battery: e.target.value,
                                },
                              })
                            }
                          />
                        </Field>
                        <Field label={s.tr("Màn hình", "Screen condition")}>
                          <Input
                            value={item.condition.screen}
                            onChange={(e) =>
                              update(i, {
                                condition: {
                                  ...item.condition,
                                  screen: e.target.value,
                                },
                              })
                            }
                          />
                        </Field>
                      </>
                    )}
                    {product?.category === "airpods" && (
                      <Field
                        label={s.tr(
                          "Tai trái, tai phải và hộp sạc",
                          "Left earbud, right earbud and charging case",
                        )}
                      >
                        <Input
                          value={item.condition.components}
                          onChange={(e) =>
                            update(i, {
                              condition: {
                                ...item.condition,
                                components: e.target.value,
                              },
                            })
                          }
                        />
                      </Field>
                    )}
                    <Field label={s.tr("Mô tả chi tiết", "Additional details")}>
                      <Textarea
                        value={item.condition.notes}
                        onChange={(e) =>
                          update(i, {
                            condition: {
                              ...item.condition,
                              notes: e.target.value,
                            },
                          })
                        }
                      />
                    </Field>
                  </div>
                  <p className="help-text">
                    {s.tr(
                      "Cần nhân viên định giá. Giá cuối được xác định sau khi nhận và kiểm tra.",
                      "Staff valuation required. The final offer is determined after reception and inspection.",
                    )}
                  </p>
                </div>
              );
            })}
            <div className="panel form-stack">
              <Field label={s.tr("Hình thức tiếp nhận", "Reception method")}>
                <Select
                  value={method}
                  onChange={(e) => {
                    setMethod(e.target.value);
                    key.current = "";
                  }}
                >
                  <option value="store">
                    {s.tr(
                      "Mang đến điểm tiếp nhận mô phỏng",
                      "Bring to simulated collection point",
                    )}
                  </option>
                  <option value="remote">
                    {s.tr(
                      "Gửi từ xa — vận đơn mô phỏng",
                      "Send remotely — simulated shipment",
                    )}
                  </option>
                </Select>
              </Field>
              {error && <ErrorState message={error} />}
              <Button disabled={busy} type="submit">
                {busy
                  ? s.tr("Đang gửi…", "Submitting…")
                  : s.tr("Gửi yêu cầu thu cũ", "Submit trade-in request")}
                <ArrowRight />
              </Button>
            </div>
          </form>
        )}
      </section>
    </div>
  );
}
function TradeInHistory({
  requests,
  onRefresh,
}: {
  requests: Row[];
  onRefresh: () => Promise<void>;
}) {
  const s = useStore(),
    [busy, setBusy] = useState(""),
    [error, setError] = useState(""),
    decisionKeys = useRef<Record<string, string>>({});
  async function decide(quote: Row, item: Row, decision: string) {
    const id = str(item, "id", "quoteItemId", "quote_item_id");
    setBusy(id);
    setError("");
    try {
      const key = `${id}:${decision}`;
      decisionKeys.current[key] ??= crypto.randomUUID();
      await command(
        "decide_trade_in",
        {
          quoteId: str(quote, "id", "quoteId", "quote_id"),
          quoteItemId: id,
          decision,
        },
        decisionKeys.current[key],
      );
      delete decisionKeys.current[key];
      await onRefresh();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy("");
    }
  }
  return (
    <div className="stack">
      <div className="row">
        <h3>{s.tr("Hành trình thu cũ", "Your trade-in journey")}</h3>
        <Link className="button button-outline" href={`/${s.locale}/trade-in`}>
          <Plus size={15} />
          {s.tr("Yêu cầu mới", "New request")}
        </Link>
      </div>
      {error && <ErrorState message={error} />}{" "}
      {requests.length ? (
        requests.map((r) => (
          <div className="panel stack" key={str(r, "id")}>
            <div className="row">
              <strong>{str(r, "code", "requestNo", "request_no", "id")}</strong>
              <OrderStatus value={str(r, "state", "status")} />
            </div>
            <p className="help-text">
              {str(r, "receptionMethod", "reception_method") === "remote"
                ? s.tr(
                    "Gửi từ xa · BotZone chịu phí hai chiều",
                    "Remote · BotZone covers both shipping directions",
                  )
                : s.tr(
                    "Nhận tại điểm mô phỏng",
                    "At the simulated collection point",
                  )}{" "}
              · {date(str(r, "createdAt", "created_at"), s.locale)}
            </p>
            {rows(r.items).map((item) => (
              <div className="row" key={str(item, "id")}>
                <span>
                  {str(
                    item,
                    "productName",
                    "product_name",
                    "internalCode",
                    "internal_code",
                  )}{" "}
                  · {str(item, "quantity", "qty")} ×
                </span>
                <OrderStatus value={str(item, "state")} />
              </div>
            ))}
            {rows(r.quotes).map((quote) => {
              const newest =
                Number(str(quote, "versionNo", "version_no")) ===
                Math.max(
                  ...rows(r.quotes).map((q) =>
                    Number(str(q, "versionNo", "version_no")),
                  ),
                );
              const approved = !!str(quote, "approvedBy", "approved_by");
              const expired =
                new Date(str(quote, "expiresAt", "expires_at")).getTime() <=
                Date.now();
              const canDecide = newest && approved && !expired;
              return (
                <div className="panel" key={str(quote, "id")}>
                  <h3>
                    {s.tr("Báo giá phiên bản", "Offer version")}{" "}
                    {str(quote, "versionNo", "version_no")}
                  </h3>
                  <p className="help-text">
                    {s.tr("Hạn trả lời:", "Respond by:")}{" "}
                    {date(str(quote, "expiresAt", "expires_at"), s.locale)}
                  </p>
                  {rows(quote.items).map((item) => (
                    <div className="order-card" key={str(item, "id")}>
                      <div>
                        <strong>
                          {str(
                            item,
                            "productName",
                            "product_name",
                            "tradeInItemId",
                            "trade_in_item_id",
                          )}
                        </strong>
                        <p>
                          <Money
                            value={moneyValue(
                              item,
                              "offeredVnd",
                              "offered_vnd",
                            )}
                          />
                        </p>
                      </div>
                      {str(item, "decision") === "pending" && canDecide ? (
                        <div className="actions">
                          <Button
                            size="sm"
                            disabled={!!busy}
                            onClick={() => void decide(quote, item, "accepted")}
                          >
                            {s.tr("Chấp nhận", "Accept")}
                          </Button>
                          <Button
                            variant="outline"
                            size="sm"
                            disabled={!!busy}
                            onClick={() => void decide(quote, item, "rejected")}
                          >
                            {s.tr("Từ chối, nhận lại", "Decline & return")}
                          </Button>
                        </div>
                      ) : str(item, "decision") === "pending" ? (
                        <Badge tone="warning">
                          {!newest
                            ? s.tr("Đã có báo giá mới", "Newer offer available")
                            : expired
                              ? s.tr("Báo giá đã hết hạn", "Offer expired")
                              : s.tr(
                                  "Chờ duyệt báo giá",
                                  "Awaiting offer approval",
                                )}
                        </Badge>
                      ) : (
                        <OrderStatus value={str(item, "decision")} />
                      )}
                    </div>
                  ))}
                </div>
              );
            })}
          </div>
        ))
      ) : (
        <Empty
          title={s.tr("Chưa có yêu cầu thu cũ", "No trade-in requests yet")}
        />
      )}
    </div>
  );
}

export function Care() {
  const s = useStore(),
    account = useResource<AccountData>(s.actor ? "/api/account" : null);
  return (
    <div className="page-wrap">
      <section className="service-hero">
        <div>
          <span className="eyebrow">BOTZONE CARE</span>
          <h1>
            {s.tr(
              "Thêm an tâm.\nThêm đồng hành.",
              "More peace of mind.\nMore possibilities.",
            )}
          </h1>
          <p>
            {s.tr(
              "Gia hạn lỗi phần cứng 12 tháng sau bảo hành tiêu chuẩn của từng sản phẩm. Một gói chăm sóc do BotZone quản lý.",
              "12 additional months of hardware coverage after each product’s standard warranty. A service managed by BotZone.",
            )}
          </p>
          <Link
            className="button button-primary"
            href={`/${s.locale}/products`}
          >
            {s.tr("Chọn sản phẩm có Care", "Find a product with Care")}
            <ArrowRight size={16} />
          </Link>
        </div>
        <ShieldCheck aria-hidden />
      </section>
      <div className="step-grid">
        {[
          [
            "01",
            "Gắn với từng thiết bị",
            "One plan, one device",
            "Mua kèm sản phẩm đủ điều kiện tại trang sản phẩm hoặc giỏ hàng.",
            "Add coverage to an eligible product when you buy it.",
          ],
          [
            "02",
            "Sửa chữa trước",
            "Repair first",
            "Tiếp nhận và đánh giá lỗi phần cứng trong thời hạn gia hạn.",
            "Reception and inspection for hardware faults during the extended term.",
          ],
          [
            "03",
            "Giải pháp rõ ràng",
            "Clear resolutions",
            "Không sửa được: đổi cùng SKU. Không thể sửa hoặc đổi: hoàn theo chính sách.",
            "If repair is not possible: same-SKU replacement. If neither is possible: refund under policy.",
          ],
        ].map(([n, vi, en, dvi, den]) => (
          <div key={n}>
            <span>{n}</span>
            <h3>{s.tr(vi, en)}</h3>
            <p>{s.tr(dvi, den)}</p>
          </div>
        ))}
      </div>
      <div className="notice" style={{ marginBottom: 35 }}>
        <ShieldCheck />
        <span>
          {s.tr(
            "BotZone Care là dịch vụ mô phỏng của BotZone, không phải AppleCare. Không bảo vệ rơi vỡ hoặc vào nước. Mỗi thiết bị lưu thời hạn và điều kiện tại lúc mua.",
            "BotZone Care is a simulated BotZone service, not AppleCare. Accidental and liquid damage are excluded. Each device retains the terms and dates agreed at purchase.",
          )}
        </span>
      </div>
      {s.actor ? (
        account.error ? (
          <ErrorState message={account.error} retry={account.refresh} />
        ) : account.loading ? (
          <Busy />
        ) : (
          account.data && (
            <WarrantyArea
              entitlements={account.data.entitlements}
              claims={account.data.claims}
              onRefresh={account.refresh}
            />
          )
        )
      ) : (
        <Link className="button button-outline" href={`/${s.locale}/login`}>
          {s.tr("Đăng nhập xem quyền lợi", "Sign in to view coverage")}
        </Link>
      )}
    </div>
  );
}
function WarrantyArea({
  entitlements,
  claims,
  onRefresh,
}: {
  entitlements: Row[];
  claims: Row[];
  onRefresh: () => Promise<void>;
}) {
  const s = useStore(),
    [id, setId] = useState(""),
    [description, setDescription] = useState(""),
    [damageKind, setDamageKind] = useState("hardware"),
    [evidenceIds, setEvidenceIds] = useState<string[]>([]),
    [uploadingEvidence, setUploadingEvidence] = useState(false),
    [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    key = useRef("");
  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (uploadingEvidence) return;
    setBusy(true);
    setError("");
    try {
      if (!key.current) key.current = crypto.randomUUID();
      await command(
        "request_warranty",
        { entitlementId: id, description, damageKind, evidenceIds },
        key.current,
      );
      setId("");
      setDescription("");
      key.current = "";
      await onRefresh();
      s.notify(s.tr("Đã gửi yêu cầu bảo hành.", "Warranty claim submitted."));
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <div className="stack">
      <h3>{s.tr("Quyền lợi của bạn", "Your coverage")}</h3>
      {entitlements.length ? (
        entitlements.map((x) => (
          <div className="order-card" key={str(x, "id")}>
            <div>
              <h3>
                {str(x, "productName", "product_name", "name") ||
                  str(row(x.snapshot), "name") ||
                  "BotZone Care"}
              </h3>
              <p>{str(x, "serial", "identifier")}</p>
              <p>
                {s.tr("Gia hạn từ", "Extended coverage from")}{" "}
                {date(str(x, "startsOn", "starts_on"), s.locale)}{" "}
                {s.tr("đến", "to")}{" "}
                {date(str(x, "endsOn", "ends_on"), s.locale)}
              </p>
            </div>
            <div className="actions">
              <OrderStatus value={str(x, "state")} />
              <Button
                variant="outline"
                size="sm"
                disabled={busy || uploadingEvidence}
                onClick={() => {
                  setId(str(x, "id"));
                  setEvidenceIds([]);
                  key.current = "";
                }}
              >
                {s.tr("Yêu cầu bảo hành", "Start a claim")}
              </Button>
            </div>
          </div>
        ))
      ) : (
        <Empty
          title={s.tr("Chưa có gói Care", "No Care plans yet")}
          description={s.tr(
            "Gói Care có thể chọn cùng sản phẩm đủ điều kiện.",
            "Care plans can be added to eligible products.",
          )}
        />
      )}{" "}
      {id && (
        <form className="panel form-stack" onSubmit={submit}>
          <h3>{s.tr("Mô tả sự cố", "Tell us what happened")}</h3>
          <Field label={s.tr("Loại sự cố", "Issue type")}>
            <Select
              value={damageKind}
              onChange={(e) => {
                setDamageKind(e.target.value);
                key.current = "";
              }}
            >
              <option value="hardware">
                {s.tr("Lỗi phần cứng", "Hardware fault")}
              </option>
              <option value="accidental">
                {s.tr("Rơi vỡ (ngoài phạm vi)", "Accidental damage (excluded)")}
              </option>
              <option value="liquid">
                {s.tr("Vào nước (ngoài phạm vi)", "Liquid damage (excluded)")}
              </option>
            </Select>
          </Field>
          <Field label={s.tr("Mô tả chi tiết", "Description")}>
            <Textarea
              required
              minLength={10}
              value={description}
              onChange={(e) => {
                setDescription(e.target.value);
                key.current = "";
              }}
            />
          </Field>
          {error && <ErrorState message={error} />}
          <EvidenceUploader
            key={id}
            referenceType="entitlement"
            referenceId={id}
            onBusyChange={setUploadingEvidence}
            onChange={(ids) => {
              setEvidenceIds(ids);
              key.current = "";
            }}
          />
          <div className="actions">
            <Button type="submit" disabled={busy || uploadingEvidence}>
              {s.tr("Gửi yêu cầu", "Submit claim")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              disabled={busy || uploadingEvidence}
              onClick={() => setId("")}
            >
              {s.tr("Hủy", "Cancel")}
            </Button>
          </div>
        </form>
      )}
      <RequestHistory
        title={s.tr("Hồ sơ bảo hành", "Warranty claims")}
        entries={claims}
        empty={s.tr(
          "Chưa phát sinh yêu cầu bảo hành.",
          "No warranty claims yet.",
        )}
        type="warranty"
      />
    </div>
  );
}

export function Policy({ kind }: { kind: string }) {
  const s = useStore();
  const copy: Record<
    string,
    { title: [string, string]; sections: [string, string, string, string][] }
  > = {
    shipping: {
      title: ["Giao hàng & thanh toán", "Delivery & payment"],
      sections: [
        [
          "Phạm vi giao hàng",
          "Delivery area",
          "BotZone giao trong Việt Nam bằng vận đơn mô phỏng. Phí giao 30.000 VND; miễn phí khi giá hàng sau ưu đãi đạt 2.000.000 VND.",
          "BotZone delivers within Vietnam using simulated shipments. Delivery is 30,000 VND, free when eligible product value after discounts reaches 2,000,000 VND.",
        ],
        [
          "Thanh toán minh bạch",
          "Transparent payments",
          "Thanh toán khi nhận hàng (COD), chuyển khoản, MoMo và Visa đều là mô phỏng. Không chuyển tiền thật, không nhập số thẻ hoặc CVV. Giao hàng COD thành công và đối soát thu tiền là hai sự kiện riêng.",
          "COD, bank transfer, MoMo and Visa are simulated. Do not transfer real money or enter card numbers or CVV. Successful delivery and COD collection are separate events.",
        ],
        [
          "Giữ hàng",
          "Reservations",
          "Sau đặt hàng, MoMo/Visa giữ hàng 15 phút, chuyển khoản 30 phút; COD chờ xác nhận tối đa 24 giờ. Giỏ hàng chưa giữ tồn.",
          "After checkout, MoMo/Visa reserve items for 15 minutes, bank transfer for 30 minutes; COD awaits confirmation for up to 24 hours. Adding to bag does not reserve stock.",
        ],
      ],
    },
    returns: {
      title: ["Trả hàng & hoàn tiền", "Returns & refunds"],
      sections: [
        [
          "14 ngày để gửi yêu cầu",
          "14 days to request a return",
          "Chỉ nhận trả hàng lỗi hoặc giao sai trong 14 ngày từ giao thành công. Bạn có thể trả từng món từ chi tiết đơn hàng.",
          "Returns are accepted only for faulty or incorrect items within 14 days of successful delivery. You can return individual items from your order details.",
        ],
        [
          "Nhận và kiểm tra",
          "Receive and inspect",
          "Nhân viên tiếp nhận yêu cầu, kho kiểm tra hàng và chủ cửa hàng phê duyệt khoản hoàn. Hàng đã dùng không tự trở thành hàng mới bán lại. BotZone chịu phí thu hồi yêu cầu hợp lệ.",
          "Staff review the request, the warehouse inspects the items, and the owner approves the refund. Used items do not automatically re-enter new stock. BotZone covers return shipping for eligible requests.",
        ],
        [
          "Hoàn đúng nguồn",
          "Refund to the original source",
          "Hoàn theo giá trị thực trả sau mã ưu đãi và điểm. Phần số dư trở về số dư; phần tiền về kênh thanh toán mô phỏng. Điểm không đổi thành tiền. Hoàn phí giao gốc khi toàn đơn được trả hợp lệ; Care chưa sử dụng được hủy và hoàn theo món.",
          "Refunds use the actual paid value after vouchers and points. Store credit returns to store credit; simulated cash returns to its original channel. Points are not cash. Original shipping is refunded for a full eligible return; unused Care is cancelled and refunded per item.",
        ],
      ],
    },
    privacy: {
      title: ["Quyền riêng tư", "Your privacy"],
      sections: [
        [
          "Dữ liệu local",
          "Local data",
          "Đây là đồ án demo local. Tài khoản, đơn hàng, hồ sơ và ảnh bằng chứng được lưu trong môi trường PostgreSQL và media local của nhóm.",
          "This is a local course demo. Accounts, orders, requests and evidence images are stored in the team’s local PostgreSQL and media environment.",
        ],
        [
          "Quyền truy cập",
          "Access",
          "Bạn chỉ được đọc hồ sơ của mình. Chứng từ, ảnh kiểm định và file xuất được kiểm quyền ở API. Không đưa mật khẩu Apple, mật khẩu thiết bị hoặc dữ liệu thẻ vào biểu mẫu.",
          "You can access your own records. Documents, inspection evidence and exports require API authorization. Do not enter Apple passwords, device passwords or card data in any form.",
        ],
        [
          "Liên hệ",
          "Contact",
          "Liên hệ nhóm vận hành BotZone trong buổi demo để yêu cầu hỗ trợ dữ liệu và tài khoản. Không có email hoặc số điện thoại thương mại thật được dùng trong đồ án.",
          "Contact the BotZone project team during the demo for data and account support. The project does not use real commercial contact details.",
        ],
      ],
    },
  };
  const p = copy[kind] ?? copy.shipping;
  return (
    <article className="page-wrap policy">
      <span className="eyebrow">{s.tr("CHÍNH SÁCH BOTZONE", "BOTZONE POLICY")}</span>
      <h1 style={{ marginTop: 18 }}>{p.title[s.locale === "vi" ? 0 : 1]}</h1>
      {p.sections.map(([vi, en, dvi, den]) => (
        <section key={vi}>
          <h2>{s.tr(vi, en)}</h2>
          <p>{s.tr(dvi, den)}</p>
        </section>
      ))}
      <div className="actions" style={{ marginTop: 35 }}>
        <Link className="button button-outline" href={`/${s.locale}/trade-in`}>
          {s.tr("Thu cũ đổi mới", "Trade in")}
        </Link>
        <Link className="button button-outline" href={`/${s.locale}/care`}>
          BotZone Care
        </Link>
      </div>
    </article>
  );
}
