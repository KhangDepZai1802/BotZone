"use client";
import { ArrowUpRight, ChevronDown, FileCheck2 } from "lucide-react";
import type { Product } from "@/lib/contracts";
import { useStore } from "./client";
import styles from "./product-specifications.module.css";

export function SpecificationSources({ product }: { product: Product }) {
  const s = useStore(), specs = product.technicalSpecs;
  const sources = specs?.sources ?? (product.sourceUrl ? [{ title: "Apple", url: product.sourceUrl }] : []);
  const date = specs?.verifiedAt ? new Date(specs.verifiedAt) : null;
  return <div className={styles.sources}>
    <span><FileCheck2 size={15} aria-hidden />{s.tr("Nguồn thông số", "Specification sources")}</span>
    {sources.map(source => <a key={source.url} href={source.url} target="_blank" rel="noreferrer">{source.title}<ArrowUpRight size={12} aria-hidden /></a>)}
    {date && !Number.isNaN(date.getTime()) && <small>{s.tr("Đối chiếu", "Checked")} {new Intl.DateTimeFormat(s.locale === "vi" ? "vi-VN" : "en-US", { dateStyle: "medium", timeZone: "UTC" }).format(date)}</small>}
  </div>;
}

export function ProductSpecifications({ product }: { product: Product }) {
  const s = useStore(), specs = product.technicalSpecs;
  const highlights = specs?.groups.flatMap(group => group.items).filter(item => specs.highlightIds.includes(item.id) && item.value).slice(0, 4) ?? [];
  return <section className={styles.section} aria-labelledby="technical-specifications-title">
    <div className={styles.heading}><span className="eyebrow">{s.tr("CHI TIẾT TẠO KHÁC BIỆT", "THE DETAILS THAT MATTER")}</span><h2 id="technical-specifications-title">{s.tr("Hiểu rõ lựa chọn của bạn.", "Know your next favorite.")}</h2><p>{s.tr("Thông số theo từng nhóm, để bạn tìm đúng điều mình quan tâm.", "Explore the specifications that matter to you, one category at a time.")}</p></div>
    {highlights.length > 0 && <dl className={styles.highlights}>{highlights.map(item => <div key={item.id}><dt>{item.label}</dt><dd>{item.value}</dd></div>)}</dl>}
    {specs?.groups.length ? <div className={styles.groups}>{specs.groups.map(group => <details key={group.id} className={styles.group} open>
      <summary>{group.label}<ChevronDown size={19} aria-hidden /></summary>
      <dl>{group.items.map(item => <div key={item.id}><dt>{item.label}</dt><dd>{item.value || "—"}</dd></div>)}</dl>
    </details>)}</div> : <p className={styles.unavailable}>{s.tr("Thông số chi tiết đã xác minh đang được cập nhật cho sản phẩm này.", "Verified detailed specifications are being prepared for this product.")}</p>}
    <SpecificationSources product={product} />
  </section>;
}
