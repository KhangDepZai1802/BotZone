"use client";
import React, { useEffect, useRef, useState } from "react";
import Link from "next/link";
import dynamic from "next/dynamic";
import { useSearchParams } from "next/navigation";
import {
  ArrowDown,
  ArrowRight,
  ArrowUpRight,
  ChevronRight,
  PackageCheck,
  Recycle,
  ShieldCheck,
  Truck,
} from "lucide-react";
import type { LandingDocument, LandingSection, Product } from "@/lib/contracts";
import { api, useResource, useStore } from "./client";
import { Busy, Button, Empty, ErrorState, Select } from "./ui";
import { productPhoto, productPhotoStyle } from "./product-photo";
const ProductScene = dynamic(() => import("./product-scene"), {
  ssr: false,
  loading: () => null,
});
export function PublicLanding() {
  const s = useStore();
  const data = useResource<LandingDocument>("/api/landing");
  return data.loading ? (
    <div className="landing-loading">
      <div className="skeleton heading-skeleton" />
      <div
        className="skeleton"
        style={{ height: 420, width: "70%", margin: "30px auto" }}
      />
    </div>
  ) : data.error ? (
    <div className="page-wrap">
      <ErrorState message={data.error} retry={data.refresh} />
      <Link
        href={`/${s.locale}/products`}
        className="button button-outline"
        style={{ marginTop: 25 }}
      >
        Khám phá sản phẩm / Explore products
      </Link>
    </div>
  ) : data.data?.sections.length ? (
    <LandingRenderer document={data.data} />
  ) : (
    <div className="page-wrap">
      <Empty
        title={s.tr(
          "Bộ sưu tập đang được chuẩn bị",
          "Our next collection is being prepared",
        )}
      >
        <Link className="button button-primary" href={`/${s.locale}/products`}>
          {s.tr("Khám phá sản phẩm", "Explore products")}
        </Link>
      </Empty>
    </div>
  );
}
export function LandingRenderer({ document }: { document: LandingDocument }) {
  return (
    <>
      {document.sections.map((section, index) =>
        section.type === "hero" || section.type === "duo" ? (
          <HeroSection key={section.id} section={section} index={index} />
        ) : section.type === "collection" ? (
          <CollectionSection section={section} key={section.id} />
        ) : (
          <BenefitsSection section={section} key={section.id} />
        ),
      )}
    </>
  );
}
function HeroSection({
  section,
  index,
}: {
  section: LandingSection;
  index: number;
}) {
  const s = useStore(),
    duo = section.type === "duo",
    product = s.products.find((p) => p.slug === section.productSlug);
  const canBuy =
    product?.variants.some((v) => v.active && v.available > 0) ?? false;
  const title = section.title[s.locale];
  const scene = (
    <div className="landing-scene-shell">
      <img className="landing-scene-placeholder" src={section.poster} alt="" aria-hidden
        width={duo ? 1600 : 1024} height={duo ? 1200 : 1280}
        loading={index === 0 ? "eager" : "lazy"} fetchPriority={index === 0 ? "high" : "auto"} decoding="async" />
      <ProductScene
        duo={duo}
        poster={section.poster}
        sceneId={`landing-${section.id}`}
        config={section}
      />
    </div>
  );
  if (duo)
    return (
      <section className="duo-section landing-feature" data-product-hero aria-labelledby={`${section.id}-heading`}>
        <div className="duo-copy">
          <span className="eyebrow">IPHONE DUO</span>
          <h2 id={`${section.id}-heading`}>{title}</h2>
          <p>{section.subtitle[s.locale]}</p>
          <div className="actions">
            <Link
              className="button button-primary"
              href={`/${s.locale}/products/${section.productSlug}`}
            >
              {s.tr("Khám phá iPhone Duo", "Discover iPhone Duo")}
              <ArrowUpRight size={14} />
            </Link>
            {product && !canBuy && (
              <small>{s.tr("Tạm hết hàng", "Currently out of stock")}</small>
            )}
          </div>
        </div>
        <div className="duo-model">{scene}</div>
      </section>
    );
  return (
    <section className="hero landing-feature" data-product-hero aria-labelledby={`${section.id}-heading`}>
      <div className="hero-top">
        <span>{s.tr("BỘ SƯU TẬP BOTZONE / 2026", "BOTZONE COLLECTION / 2026")}</span>
        <span>
          {s.tr(
            "Công nghệ. Chạm đến cảm hứng.",
            "Technology. Meet inspiration.",
          )}
        </span>
      </div>
      <div className="hero-stage">
      <div className="hero-body">
        <span className="eyebrow">IPHONE 18 PRO MAX · BURGUNDY</span>
        <h1 id={`${section.id}-heading`}>{title}</h1>
        <p>{section.subtitle[s.locale]}</p>
        <div className="actions">
          <Link
            className="button button-primary"
            href={`/${s.locale}/products/${section.productSlug}`}
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
      <div className="hero-model">{scene}</div>
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
        <span>{String(index + 1).padStart(2, "0")}</span>
      </div>
    </section>
  );
}
function CollectionSection({ section }: { section: LandingSection }) {
  const s = useStore(),
    [category, setCategory] = useState(""),
    selected = category
      ? s.products.filter((p) => p.category === category)
      : s.products;
  return (
    <section className="page-wrap collection-section">
      <div className="section-heading">
        <div>
          <span className="eyebrow">{s.tr("TUYỂN CHỌN BOTZONE", "THE BOTZONE EDIT")}</span>
          <h2 style={{ marginTop: 14 }}>{section.title[s.locale]}</h2>
          <p>{section.subtitle[s.locale]}</p>
        </div>
        <Link className="text-link" href={`/${s.locale}/products`}>
          {s.tr("Tất cả sản phẩm", "All products")}
          <ArrowUpRight size={15} />
        </Link>
      </div>
      <div className="category-list" role="group" aria-label={s.tr("Bộ sưu tập sản phẩm", "Product collections")}>
        <button
          className={`category-chip ${!category ? "active" : ""}`}
          aria-pressed={!category}
          onClick={() => setCategory("")}
        >
          {s.tr("Dành cho bạn", "For you")}
        </button>
        {s.categories.map((c) => (
          <button
            key={c.code}
            className={`category-chip ${category === c.code ? "active" : ""}`}
            aria-pressed={category === c.code}
            onClick={() => setCategory(c.code)}
          >
            {s.locale === "vi" ? c.nameVi : c.nameEn}
          </button>
        ))}
      </div>
      {s.catalogueError ? (
        <ErrorState message={s.catalogueError} retry={s.refreshCatalogue} />
      ) : s.catalogueLoading ? (
        <Busy />
      ) : selected.length ? (
        <div className="product-grid">
          {selected.slice(0, 4).map((p) => (
            <LandingProductCard product={p} key={p.id} />
          ))}
        </div>
      ) : (
        <Empty
          title={s.tr(
            "Chưa có sản phẩm trong nhóm này",
            "No products in this collection yet",
          )}
        />
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
    </section>
  );
}
function LandingProductCard({ product: p }: { product: Product }) {
  const s = useStore();
  const photo = productPhoto(p);
  return (
    <article className="product-card">
      <Link className="product-art" href={`/${s.locale}/products/${p.slug}`}>
        <img src={photo.path} alt={photo.alt} style={productPhotoStyle(photo)} loading="lazy" decoding="async" width={480} height={480} />
      </Link>
      <div className="product-content">
        <span className="category-label">{p.category}</span>
        <h3>
          <Link href={`/${s.locale}/products/${p.slug}`}>{p.name}</Link>
        </h3>
        <span className="price">{s.money(p.variants[0]?.priceVnd ?? "0")}</span>
        <div className="product-bottom">
          <Link
            className="button button-secondary button-sm"
            href={`/${s.locale}/products/${p.slug}`}
          >
            {s.tr("Khám phá", "Discover")}
            <ArrowUpRight size={13} />
          </Link>
          <span className="variant-note">
            {p.variants.length} {s.tr("lựa chọn", "options")}
          </span>
        </div>
      </div>
    </article>
  );
}
function BenefitsSection({ section }: { section: LandingSection }) {
  const s = useStore();
  return (
    <section className="page-wrap" style={{ paddingTop: 0 }}>
      <h2 className="sr-only">{section.title[s.locale]}</h2>
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
  );
}
export function PreviewLanding() {
  const s = useStore(),
    params = useSearchParams(),
    [document, setDocument] = useState<LandingDocument | null>(null),
    [error, setError] = useState(""),
    [width, setWidth] = useState(
      params.get("viewport") === "mobile" ? "390px" : "100%",
    ),
    [loading, setLoading] = useState(true),
    redeeming = useRef(false);
  useEffect(() => {
    if (redeeming.current) return;
    redeeming.current = true;
    (async () => {
      try {
        const code = params.get("code");
        if (code) {
          await api("/api/landing/preview/redeem", { code });
          const url = new URL(window.location.href);
          url.searchParams.delete("code");
          window.history.replaceState({}, "", url);
        }
        setDocument(await api<LandingDocument>("/api/landing/preview"));
      } catch (e) {
        setError((e as Error).message);
      } finally {
        setLoading(false);
      }
    })();
  }, [params]);
  if (params.get("frame") === "1")
    return loading ? (
      <Busy />
    ) : error ? (
      <ErrorState message={error} />
    ) : document ? (
      <LandingRenderer document={document} />
    ) : null;
  return (
    <>
      <div className="preview-toolbar">
        <strong>{s.tr("Xem trước bản nháp", "Draft preview")}</strong>
        <span className="help-text">
          {s.tr(
            "Chưa xuất bản · Phiên xem có thời hạn",
            "Unpublished · Limited preview session",
          )}
        </span>
        <Select
          aria-label={s.tr("Kích thước xem trước", "Preview width")}
          value={width}
          onChange={(e) => setWidth(e.target.value)}
        >
          <option value="100%">{s.tr("Máy tính", "Laptop")}</option>
          <option value="390px">{s.tr("Điện thoại · 390px", "Mobile · 390px")}</option>
          <option value="360px">{s.tr("Điện thoại · 360px", "Mobile · 360px")}</option>
          <option value="768px">{s.tr("Máy tính bảng · 768px", "Tablet · 768px")}</option>
        </Select>
      </div>
      {loading ? (
        <Busy />
      ) : error ? (
        <div className="page-wrap">
          <ErrorState message={error} />
        </div>
      ) : (
        document && (
          <iframe
            className="preview-viewport"
            title={s.tr("Xem trước landing", "Landing preview")}
            style={{ width, maxWidth: "100%" }}
            src={`/${s.locale}/preview?frame=1`}
          />
        )
      )}
    </>
  );
}
