"use client";
import React, { useEffect, useMemo, useRef, useState, useSyncExternalStore } from "react";
import Link from "next/link";
import dynamic from "next/dynamic";
import {
  ArrowDown,
  ArrowRight,
  ArrowUpRight,
  Check,
  ChevronRight,
  ChevronLeft,
  ImageOff,
  GitCompareArrows,
  Heart,
  PackageCheck,
  Plus,
  Recycle,
  Search,
  ShieldCheck,
  ShoppingBag,
  Trash2,
  Truck,
  Wallet,
  Download,
  LogOut,
} from "lucide-react";
import type {
  CartItem,
  LandingDocument,
  PaymentMethod,
  Product,
  Quote,
  Variant,
} from "@/lib/contracts";
import { api, command, useResource, useStore } from "./client";
import {
  Badge,
  Busy,
  Button,
  DataTable,
  Empty,
  ErrorState,
  Field,
  Input,
  Money,
  Select,
  Textarea,
} from "./ui";
import { Cart, AuthPage, Orders, OrderDetail, InvoiceDetail } from "./commerce";
import { Account, Compare, TradeIn, Care, Policy } from "./account-services";
import { PublicLanding, PreviewLanding } from "./landing-renderer";
import { productPhoto, productPhotoStyle } from "./product-photo";
import { categoryLabel, variantLabel } from "./product-labels";
import { ProductSpecifications } from "./product-specifications";
import { AddToBagButton } from "./shopping-feedback";
import feedbackStyles from "./shopping-feedback.module.css";
import { updateQueryUrl, useQueryParams } from "./url-state";
const ProductScene = dynamic(() => import("./product-scene"), {
  ssr: false,
  loading: () => null,
});
type Row = Record<string, unknown>;
export const str = (r: Row | undefined, ...keys: string[]): string => {
  for (const k of keys) {
    const v = r?.[k];
    if (v !== null && v !== undefined) return String(v);
  }
  return "";
};
const rows = (v: unknown): Row[] => (Array.isArray(v) ? v : []);
const labels: Record<string, [string, string]> = {
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
};
export function Status({ value }: { value: string }) {
  const s = useStore();
  return (
    <Badge
      tone={
        ["completed", "success", "paid", "delivered", "issued"].includes(value)
          ? "success"
          : ["failed", "cancelled", "rejected"].includes(value)
            ? "danger"
            : "neutral"
      }
    >
      {labels[value] ? labels[value][s.locale === "vi" ? 0 : 1] : value || "—"}
    </Badge>
  );
}
export function StorefrontPage({ path }: { path: string[] }) {
  const s = useStore();
  const page = path[0] ?? "";
  if (!page) return <PublicLanding />;
  if (page === "preview") return <PreviewLanding />;
  if (page === "products")
    return path[1] ? (
      <ProductDetail key={path[1]} slug={path[1]} />
    ) : (
      <Catalogue />
    );
  if (page === "cart" || page === "checkout")
    return <Cart checkout={page === "checkout"} />;
  if (
    page === "login" ||
    page === "register" ||
    page === "forgot-password" ||
    page === "reset-password" ||
    page === "change-password" ||
    page === "verify-email"
  )
    return <AuthPage key={page} initial={page} />;
  if (page === "account") return <Account />;
  if (page === "orders")
    return path[1] ? <OrderDetail key={path[1]} id={path[1]} /> : <Orders />;
  if (page === "invoices" && path[1])
    return <InvoiceDetail key={path[1]} id={path[1]} />;
  if (page === "wishlist") return <Catalogue wishlist />;
  if (page === "compare") return <Compare />;
  if (page === "trade-in") return <TradeIn />;
  if (page === "care") return <Care />;
  if (page === "policy") return <Policy kind={path[1] ?? "shipping"} />;
  return (
    <div className="page-wrap">
      <Empty title={s.tr("Không tìm thấy trang", "Page not found")}>
        <Link className="button button-primary" href={`/${s.locale}`}>
          {s.tr("Về trang chủ", "Back to home")}
        </Link>
      </Empty>
    </div>
  );
}
function Home() {
  const s = useStore(),
    landing = useResource<LandingDocument>("/api/landing");
  const hero = landing.data?.sections.find((x) => x.type === "hero"),
    duo = landing.data?.sections.find((x) => x.type === "duo");
  const [category, setCategory] = useState("");
  const selected = category
    ? s.products.filter((p) => p.category === category)
    : s.products;
  return (
    <>
      <section className="hero">
        <div className="hero-top">
          <span>BOTZONE COLLECTION / 2026</span>
          <span>
            {s.tr(
              "Công nghệ. Chạm đến cảm hứng.",
              "Technology. Meet inspiration.",
            )}
          </span>
        </div>
        <div className="hero-body">
          <span className="eyebrow">IPHONE 18 PRO MAX · BURGUNDY</span>
          <h1>
            {hero?.title[s.locale] ??
              s
                .tr(
                  "Định nghĩa mới.\nDấu ấn riêng.",
                  "A new perspective.\nEntirely yours.",
                )
                .split("\n")
                .map((x, i) => (
                  <React.Fragment key={x}>
                    {i > 0 && <br />}
                    {x}
                  </React.Fragment>
                ))}
          </h1>
          <p>
            {hero?.subtitle[s.locale] ??
              s.tr(
                "Sức mạnh vượt bậc. Sắc màu đậm cá tính.",
                "Extraordinary power. An unmistakable expression.",
              )}
          </p>
          <div className="actions">
            <Link
              className="button button-primary"
              href={`/${s.locale}/products/${hero?.productSlug || "iphone-18-pro-max"}`}
            >
              {s.tr("Khám phá ngay", "Discover now")}
              <ArrowUpRight size={13} />
            </Link>
            <Link
              className="button button-ghost"
              href={`/${s.locale}/products?category=iphone`}
            >
              {s.tr("Xem tất cả iPhone", "Explore all iPhone")}
              <ChevronRight size={13} />
            </Link>
          </div>
        </div>
        <div className="hero-model">
          <ProductScene
            poster={hero?.poster || "/media/phone-burgundy.svg"}
            sceneId="home-pro"
          />
        </div>
        <div className="hero-bottom">
          <span className="hero-caption">
            <i className="color-dot" />
            {s.tr(
              "Burgundy · Màu sắc mang dấu ấn của bạn",
              "Burgundy · A color that is yours",
            )}
          </span>
          <span className="scroll-label">
            {s.tr("CUỘN ĐỂ KHÁM PHÁ", "SCROLL TO EXPLORE")}
            <ArrowDown size={13} />
          </span>
          <span>01 / 02</span>
        </div>
      </section>
      <section className="duo-section">
        <div className="duo-copy">
          <span className="eyebrow">IPHONE DUO</span>
          <h2>
            {duo?.title[s.locale] ??
              s
                .tr("Mở ra một\nkhả năng mới.", "Unfold a new\npossibility.")
                .split("\n")
                .map((x, i) => (
                  <React.Fragment key={x}>
                    {i > 0 && <br />}
                    {x}
                  </React.Fragment>
                ))}
          </h2>
          <p>
            {duo?.subtitle[s.locale] ??
              s.tr(
                "Khi thiết kế mở rộng giới hạn. Một trải nghiệm mới, vừa vặn trong tay bạn.",
                "When design opens new possibilities. A new experience, right in the palm of your hand.",
              )}
          </p>
          <div className="actions">
            <Link
              className="button button-primary"
              href={`/${s.locale}/products/${duo?.productSlug || "iphone-duo"}`}
            >
              {s.tr("Khám phá iPhone Duo", "Discover iPhone Duo")}
              <ArrowUpRight size={14} />
            </Link>
          </div>
        </div>
        <ProductScene
          duo
          poster={duo?.poster || "/media/duo.svg"}
          sceneId="home-duo"
        />
      </section>
      <section className="page-wrap collection-section">
        <div className="section-heading">
          <div>
            <span className="eyebrow">{s.tr("LỰA CHỌN TỪ BOTZONE", "THE BOTZONE EDIT")}</span>
            <h2 style={{ marginTop: 14 }}>
              {s.tr("Chọn điều bạn yêu.", "Find what moves you.")}
            </h2>
            <p>
              {s.tr(
                "Một hệ sinh thái. Vô vàn cảm hứng.",
                "One ecosystem. Endless inspiration.",
              )}
            </p>
          </div>
          <Link className="text-link" href={`/${s.locale}/products`}>
            {s.tr("Tất cả sản phẩm", "All products")}
            <ArrowUpRight size={15} />
          </Link>
        </div>
        <div className="category-list">
          <button
            className={`category-chip ${!category ? "active" : ""}`}
            onClick={() => setCategory("")}
          >
            {s.tr("Dành cho bạn", "For you")}
          </button>
          {s.categories.map((c) => (
            <button
              className={`category-chip ${category === c.code ? "active" : ""}`}
              key={c.code}
              onClick={() => setCategory(c.code)}
            >
              {s.locale === "vi" ? c.nameVi : c.nameEn}
            </button>
          ))}
        </div>
        {s.catalogueError ? (
          <ErrorState message={s.catalogueError} retry={s.refreshCatalogue} />
        ) : s.catalogueLoading ? (
          <div className="product-grid">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="skeleton product-skeleton" />
            ))}
          </div>
        ) : (
          <div className="product-grid">
            {selected.slice(0, 4).map((p) => (
              <ProductCard product={p} key={p.id} />
            ))}
          </div>
        )}
        <div className="editorial-grid">
          <div className="editorial-card">
            <span className="eyebrow">{s.tr("THU CŨ ĐỔI MỚI BOTZONE", "BOTZONE TRADE IN")}</span>
            <h3>
              {s.tr(
                "Cũ với bạn.\nMới với tương lai.",
                "Your next chapter\nstarts here.",
              )}
            </h3>
            <p>
              {s.tr(
                "Đổi thiết bị đã đồng hành thành số dư cho trải nghiệm tiếp theo.",
                "Give your device a next chapter and get credit for what comes next.",
              )}
            </p>
            <Link className="text-link" href={`/${s.locale}/trade-in`}>
              {s.tr("Tìm hiểu thu cũ", "Explore trade in")}
              <ArrowUpRight size={14} />
            </Link>
            <Recycle aria-hidden />
          </div>
          <div className="editorial-card">
            <span className="eyebrow">BOTZONE CARE</span>
            <h3>
              {s.tr(
                "An tâm hơn.\nDùng lâu hơn.",
                "A little more care.\nA lot more peace.",
              )}
            </h3>
            <p>
              {s.tr(
                "Gia hạn 12 tháng bảo hành lỗi phần cứng sau thời hạn tiêu chuẩn.",
                "12 more months of hardware coverage after your standard warranty.",
              )}
            </p>
            <Link className="text-link" href={`/${s.locale}/care`}>
              {s.tr("Khám phá quyền lợi", "Explore coverage")}
              <ArrowUpRight size={14} />
            </Link>
            <ShieldCheck aria-hidden />
          </div>
        </div>
        <div className="benefits">
          <div className="benefit">
            <PackageCheck />
            <div>
              <h3>{s.tr("Sản phẩm Apple mới", "New Apple products")}</h3>
              <p>
                {s.tr(
                  "Cấu hình và tình trạng kho rõ ràng, sẵn sàng cho lựa chọn của bạn.",
                  "Clear configurations and stock availability, ready for your choice.",
                )}
              </p>
            </div>
          </div>
          <div className="benefit">
            <Truck />
            <div>
              <h3>
                {s.tr("Giao hàng trong Việt Nam", "Delivery across Vietnam")}
              </h3>
              <p>
                {s.tr(
                  "Miễn phí từ 2.000.000 ₫ giá hàng sau ưu đãi.",
                  "Free delivery on eligible products from 2,000,000 ₫ after discounts.",
                )}
              </p>
            </div>
          </div>
          <div className="benefit">
            <ShieldCheck />
            <div>
              <h3>{s.tr("Đồng hành sau mua", "Care beyond checkout")}</h3>
              <p>
                {s.tr(
                  "Trả hàng lỗi hoặc giao sai trong 14 ngày từ khi nhận.",
                  "Return faulty or incorrect items within 14 days of delivery.",
                )}
              </p>
            </div>
          </div>
        </div>
      </section>
    </>
  );
}
function ProductCard({ product: p }: { product: Product }) {
  const s = useStore(),
    variant = p.variants.find((v) => v.active) ?? p.variants[0],
    photo = productPhoto(p);
  return (
    <article className="product-card">
      <button
        className={`button button-icon button-ghost wish-button ${s.wishlist.includes(p.id) ? "selected" : ""}`}
        onClick={() => s.toggleWish(p.id)}
        aria-label={s.tr("Lưu yêu thích", "Save to wishlist") + ` ${p.name}`}
        aria-pressed={s.wishlist.includes(p.id)}
      >
        <Heart
          fill={s.wishlist.includes(p.id) ? "currentColor" : "none"}
          size={18}
        />
      </button>
      <Link className="product-art" href={`/${s.locale}/products/${p.slug}`}>
        {photo.path ? <img src={photo.path} alt={photo.alt} style={productPhotoStyle(photo)} loading="lazy" decoding="async" width={480} height={480} /> : <span className="product-image-empty"><ImageOff aria-hidden />{s.tr("Ảnh đang cập nhật", "Image coming soon")}</span>}
      </Link>
      <div className="product-content">
        <span className="category-label">{categoryLabel(p, s.categories, s.locale)}</span>
        <h3>
          <Link href={`/${s.locale}/products/${p.slug}`}>{p.name}</Link>
        </h3>
        <span className="price">
          {variant
            ? s.money(variant.priceVnd)
            : s.tr("Chưa có giá", "Price unavailable")}
        </span>
        <p className="variant-note">
          {p.variants.length} {s.tr("lựa chọn", p.variants.length === 1 ? "option" : "options")} ·{" "}
          {p.variants.some((v) => v.available > 0)
            ? s.tr("Có sẵn", "Available")
            : s.tr("Hết hàng", "Out of stock")}
        </p>
        <div className="product-bottom">
          <Link
            className="button button-secondary button-sm"
            href={`/${s.locale}/products/${p.slug}`}
          >
            {s.tr("Khám phá", "Discover")}
            <ArrowUpRight size={13} />
          </Link>
          <Button
            variant="ghost"
            size="sm"
            className={s.compare.includes(p.id) ? feedbackStyles.selectedCompare : undefined}
            aria-label={s.tr("So sánh", "Compare")}
            onClick={() => s.toggleCompare(p.id)}
            aria-pressed={s.compare.includes(p.id)}
          >
            {s.compare.includes(p.id) ? <Check aria-hidden /> : <GitCompareArrows aria-hidden />}
            {s.tr("So sánh", "Compare")}
          </Button>
        </div>
      </div>
    </article>
  );
}
const subscribeCatalogueWidth = (notify: () => void) => {
  const query = window.matchMedia("(min-width: 768px)");
  query.addEventListener("change", notify);
  return () => query.removeEventListener("change", notify);
};
const cataloguePageSize = () => window.matchMedia("(min-width: 768px)").matches ? 12 : 6;
const catalogueServerPageSize = () => 6;

function Catalogue({ wishlist = false }: { wishlist?: boolean }) {
  const s = useStore(), params = useQueryParams();
  const pageSize = useSyncExternalStore(subscribeCatalogueWidth, cataloguePageSize, catalogueServerPageSize);
  const resultsStart = useRef<HTMLDivElement>(null);
  const q = params.get("q") ?? "";
  const category = (params.get("category") ?? "").toLowerCase();
  const sort = params.get("sort") ?? "featured";
  const stock = params.get("stock") === "available";
  const result = useMemo(() => {
    let list = s.products.filter((p) =>
      (!wishlist || s.wishlist.includes(p.id)) &&
      (!category || p.category.toLowerCase() === category) &&
      (!q || `${p.name} ${p.modelCode}`.toLocaleLowerCase().includes(q.trim().toLocaleLowerCase())) &&
      (!stock || p.variants.some((v) => v.active && v.available > 0)),
    );
    if (sort === "price-asc" || sort === "price-desc")
      list = [...list].sort((a, b) => {
        const n = BigInt(a.variants[0]?.priceVnd ?? "0") - BigInt(b.variants[0]?.priceVnd ?? "0");
        return (n < 0n ? -1 : n > 0n ? 1 : 0) * (sort === "price-desc" ? -1 : 1);
      });
    return list;
  }, [s.products, s.wishlist, wishlist, category, q, stock, sort]);
  const pages = Math.max(1, Math.ceil(result.length / pageSize));
  const rawPage = params.get("page");
  const requested = rawPage && /^\d+$/.test(rawPage) ? Number(rawPage) : 1;
  const page = Math.min(pages, Math.max(1, Number.isSafeInteger(requested) ? requested : 1));
  const start = (page - 1) * pageSize;
  const shown = result.slice(start, start + pageSize);
  const base = `/${s.locale}/${wishlist ? "wishlist" : "products"}`;
  const pageHref = (number: number) => {
    const query = new URLSearchParams(params);
    if (number > 1) query.set("page", String(number)); else query.delete("page");
    return base + (query.size ? `?${query}` : "");
  };
  // URL is the single source of truth. Next's native History integration keeps
  // controls, browser Back/Forward and rapid updates in sync without refetching.
  const updateFilters = (patch: Record<string, string>, replace = false) => {
    const query = new URLSearchParams(window.location.search);
    for (const [key, value] of Object.entries(patch)) value ? query.set(key, value) : query.delete(key);
    query.delete("page");
    const href = base + (query.size ? `?${query}` : "");
    if (href !== window.location.pathname + window.location.search)
      updateQueryUrl(href, replace);
  };
  useEffect(() => {
    if (s.catalogueLoading || s.catalogueError || !rawPage) return;
    if (rawPage !== String(page) || page === 1) {
      const query = new URLSearchParams(window.location.search);
      if (page > 1) query.set("page", String(page)); else query.delete("page");
      updateQueryUrl(base + (query.size ? `?${query}` : ""), true);
    }
  }, [rawPage, page, base, s.catalogueLoading, s.catalogueError]);
  const goToPage = (event: React.MouseEvent<HTMLAnchorElement>, number: number) => {
    if (event.button || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
    event.preventDefault();
    if (number === page) return;
    updateQueryUrl(pageHref(number));
    requestAnimationFrame(() => {
      resultsStart.current?.focus({ preventScroll: true });
      resultsStart.current?.scrollIntoView({ block: "start", behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "instant" : "smooth" });
    });
  };
  const pageNumbers: (number | string)[] = pages <= 5
    ? Array.from({ length: pages }, (_, i) => i + 1)
    : page <= 3 ? [1, 2, 3, "more", pages]
    : page >= pages - 2 ? [1, "more", pages - 2, pages - 1, pages]
    : [1, "before", page, "after", pages];
  const range = result.length ? `${start + 1}–${Math.min(start + pageSize, result.length)}` : "0";
  return <div className="page-wrap catalogue-page">
    <div className="page-heading">
      <div>
        <span className="eyebrow">{s.tr("BỘ SƯU TẬP BOTZONE", "BOTZONE COLLECTION")}</span>
        <h1>{wishlist ? s.tr("Những điều bạn yêu.", "Your favorites.") : s.tr("Thế giới Apple.\nTheo cách của bạn.", "The world of Apple.\nYour way.")}</h1>
        <p>{s.tr("Khám phá sản phẩm, chọn cấu hình và tìm kết nối phù hợp với bạn.", "Explore products, choose your configuration and find what fits your world.")}</p>
      </div>
    </div>
    <div className="category-list" role="group" aria-label={s.tr("Nhóm sản phẩm", "Product categories")}>
      <button className={`category-chip ${!category ? "active" : ""}`} aria-pressed={!category} onClick={() => updateFilters({ category: "" })}>{s.tr("Tất cả", "All")}</button>
      {s.categories.map((c) => <button key={c.code} className={`category-chip ${category === c.code.toLowerCase() ? "active" : ""}`} aria-pressed={category === c.code.toLowerCase()} onClick={() => updateFilters({ category: c.code.toLowerCase() })}>{s.locale === "vi" ? c.nameVi : c.nameEn}</button>)}
    </div>
    <div className="filters">
      <div className="search-input">
        <Search aria-hidden />
        <Input aria-label={s.tr("Tìm sản phẩm", "Search products")} placeholder={s.tr("Tìm sản phẩm bạn yêu…", "Find your next favorite…")} value={q} onChange={(e) => updateFilters({ q: e.target.value }, true)} />
      </div>
      <div className="filter-selects">
        <Select value={sort} onChange={(e) => updateFilters({ sort: e.target.value === "featured" ? "" : e.target.value })} aria-label={s.tr("Sắp xếp", "Sort")}>
          <option value="featured">{s.tr("Mặc định", "Featured")}</option>
          <option value="price-asc">{s.tr("Giá tăng dần", "Price: low to high")}</option>
          <option value="price-desc">{s.tr("Giá giảm dần", "Price: high to low")}</option>
        </Select>
        <Select value={stock ? "available" : "all"} onChange={(e) => updateFilters({ stock: e.target.value === "available" ? "available" : "" })} aria-label={s.tr("Tình trạng kho", "Availability")}>
          <option value="all">{s.tr("Mọi tình trạng", "All availability")}</option>
          <option value="available">{s.tr("Còn hàng", "In stock")}</option>
        </Select>
      </div>
    </div>
    {s.catalogueError ? <ErrorState message={s.catalogueError} retry={s.refreshCatalogue} /> : s.catalogueLoading ? <Busy text={s.tr("Đang tìm lựa chọn dành cho bạn…", "Finding your next favorite…")} /> : <>
      <div className="catalogue-results" ref={resultsStart} tabIndex={-1} role="region" aria-label={s.tr("Kết quả sản phẩm", "Product results")}>
        <p className="result-count" role="status" aria-live="polite" aria-atomic="true">{s.tr(`Hiển thị ${range} trong ${result.length} sản phẩm`, `Showing ${range} of ${result.length} products`)}</p>
        <span className="page-count">{s.tr(`Trang ${page} / ${pages}`, `Page ${page} / ${pages}`)}</span>
      </div>
      {result.length ? <>
        <div className="product-grid catalogue-grid" key={`${page}:${pageSize}:${category}:${sort}:${stock}`} data-page={page} data-page-size={pageSize}>
          {shown.map((p) => <ProductCard product={p} key={p.id} />)}
        </div>
        {pages > 1 && <nav className="catalogue-pagination" aria-label={s.tr("Phân trang sản phẩm", "Product pagination")}>
          {page > 1 ? <a className="page-control page-direction" href={pageHref(page - 1)} onClick={(e) => goToPage(e, page - 1)} aria-label={s.tr("Trang trước", "Previous page")}><ChevronLeft size={17} aria-hidden /><span>{s.tr("Trước", "Previous")}</span></a> : <span className="page-control page-direction" aria-disabled="true" aria-label={s.tr("Trang trước", "Previous page")}><ChevronLeft size={17} aria-hidden /><span>{s.tr("Trước", "Previous")}</span></span>}
          <div className="page-numbers">{pageNumbers.map((number) => typeof number === "number" ? <a key={number} className="page-control" href={pageHref(number)} onClick={(e) => goToPage(e, number)} aria-current={number === page ? "page" : undefined} aria-label={s.tr(`Đến trang ${number}`, `Go to page ${number}`)}>{number}</a> : <span className="page-gap" key={number} aria-hidden>…</span>)}</div>
          {page < pages ? <a className="page-control page-direction" href={pageHref(page + 1)} onClick={(e) => goToPage(e, page + 1)} aria-label={s.tr("Trang sau", "Next page")}><span>{s.tr("Sau", "Next")}</span><ChevronRight size={17} aria-hidden /></a> : <span className="page-control page-direction" aria-disabled="true" aria-label={s.tr("Trang sau", "Next page")}><span>{s.tr("Sau", "Next")}</span><ChevronRight size={17} aria-hidden /></span>}
        </nav>}
      </> : <Empty title={s.tr("Chưa có sản phẩm phù hợp", "No matching products")} description={s.tr("Thử một từ khóa khác hoặc xóa bộ lọc.", "Try another search or clear your filters.")}>
        <Button onClick={() => updateFilters({ q: "", category: "", stock: "", sort: "" })}>{s.tr("Xóa bộ lọc", "Clear filters")}</Button>
      </Empty>}
    </>}
  </div>;
}
function ProductDetail({ slug }: { slug: string }) {
  const s = useStore(),
    params = useQueryParams(),
    detail = useResource<{ product: Product }>(
      `/api/catalogue/${slug}?locale=${s.locale}`,
    ),
    selected = params.get("variant") ?? "",
    [care, setCare] = useState(false);
  if (detail.loading)
    return <Busy text={s.tr("Đang tải sản phẩm…", "Loading product…")} />;
  if (detail.error)
    return (
      <div className="page-wrap">
        <ErrorState message={detail.error} retry={detail.refresh} />
      </div>
    );
  const p = detail.data?.product;
  if (!p)
    return (
      <Empty title={s.tr("Không tìm thấy sản phẩm", "Product not found")} />
    );
  const v = p.variants.find((x) => x.id === selected) ?? p.variants[0];
  const photo = productPhoto(p, v);
  const compatible = s.products.filter(
    (x) =>
      p.compatibleProducts?.includes(x.id) ||
      p.compatibleProducts?.includes(x.slug),
  );
  return (
    <div className="page-wrap">
      <div className="breadcrumb">
        <Link href={`/${s.locale}/products`}>
          {s.tr("Sản phẩm", "Products")}
        </Link>
        <ChevronRight size={13} />
        <span>{p.name}</span>
      </div>
      <div className="product-detail">
        <div className="detail-gallery">
          {photo.path ? <img key={photo.path} className="detail-photo" src={photo.path} alt={photo.alt} style={productPhotoStyle(photo)} decoding="async" width={720} height={720} /> : <span className="product-image-empty"><ImageOff aria-hidden />{s.tr("Ảnh đang cập nhật", "Image coming soon")}</span>}
        </div>
        <div className="detail-copy">
          <span className="eyebrow">APPLE · {categoryLabel(p, s.categories, s.locale).toLocaleUpperCase()}</span>
          <h1>{p.name}</h1>
          <div className="price">{v ? s.money(v.priceVnd) : "—"}</div>
          {s.currency === "USD" && (
            <small className="muted">
              {s.tr(
                "Giá USD tham khảo. Thanh toán bằng VND.",
                "Indicative USD price. Payments are in VND.",
              )}
            </small>
          )}
          <p className="product-description">{p.description}</p>
          <span className="field">
            {s.tr("Chọn phiên bản của bạn", "Choose your configuration")}
          </span>
          <div className="variant-grid">
            {p.variants.map((option) => (
              <button
                key={option.id}
                className={`variant-option ${v?.id === option.id ? "active" : ""}`}
                onClick={() => {
                  const query = new URLSearchParams(params);
                  query.set("variant", option.id);
                  updateQueryUrl(`/${s.locale}/products/${slug}?${query}`, true);
                }}
                aria-pressed={v?.id === option.id}
              >
                {variantLabel(option)}
                <small>
                  {s.money(option.priceVnd)} ·{" "}
                  {option.available > 0
                    ? s.tr("Còn hàng", "Available")
                    : s.tr("Hết hàng", "Out of stock")}
                </small>
              </button>
            ))}
          </div>
          {v?.careFeeVnd && (
            <label className="care-choice">
              <input
                type="checkbox"
                checked={care}
                onChange={(e) => setCare(e.target.checked)}
              />
              <div>
                <strong>BotZone Care + {s.money(v.careFeeVnd)}</strong>
                <p>
                  {s.tr(
                    `Gia hạn lỗi phần cứng 12 tháng sau ${v.standardWarrantyMonths} tháng bảo hành tiêu chuẩn. Không gồm rơi vỡ, vào nước.`,
                    `12 months of hardware coverage after the ${v.standardWarrantyMonths}-month standard warranty. Accidental and liquid damage excluded.`,
                  )}
                </p>
              </div>
              <ShieldCheck size={22} />
            </label>
          )}
          <div className="stock-label">
            {v?.available > 0
              ? s.tr(
                  `${v.available} sản phẩm sẵn sàng giao`,
                  `In stock · ${v.available} available`,
                )
              : s.tr(
                  "Tạm hết hàng · Không nhận đặt trước",
                  "Out of stock · No pre-orders",
                )}
          </div>
          <div className="actions">
            {v && <AddToBagButton item={{ variantId: v.id, quantity: 1, care: care && !!v.careFeeVnd }} disabled={v.available < 1 || !v.active} />}
            <Button
              variant="outline"
              size="icon"
              aria-label={s.tr("Yêu thích", "Wishlist")}
              aria-pressed={s.wishlist.includes(p.id)}
              onClick={() => s.toggleWish(p.id)}
            >
              <Heart
                fill={s.wishlist.includes(p.id) ? "currentColor" : "none"}
              />
            </Button>
            <Button
              variant="outline"
              className={s.compare.includes(p.id) ? feedbackStyles.selectedCompare : undefined}
              aria-pressed={s.compare.includes(p.id)}
              onClick={() => s.toggleCompare(p.id)}
            >
              <GitCompareArrows />
              {s.compare.includes(p.id) ? s.tr("Đã chọn so sánh", "Selected to compare") : s.tr("So sánh", "Compare")}
            </Button>
          </div>
          <div className="notice" style={{ marginTop: 22 }}>
            <Truck />
            <span>
              {s.tr(
                "Giao trong Việt Nam. Miễn phí từ 2 triệu giá hàng sau ưu đãi; đơn còn lại 30.000 ₫.",
                "Vietnam delivery. Free from 2,000,000 ₫ after discounts; otherwise 30,000 ₫.",
              )}
            </span>
          </div>
        </div>
      </div>
      <ProductSpecifications product={p} />
      {compatible.length > 0 && (
        <section className="specs">
          <div className="section-heading">
            <h2>{s.tr("Kết nối hoàn hảo.", "Better together.")}</h2>
          </div>
          <div className="product-grid">
            {compatible.map((x) => (
              <ProductCard product={x} key={x.id} />
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
