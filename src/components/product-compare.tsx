"use client";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { ArrowLeftRight, ArrowRight, Check, ChevronDown, ChevronsUpDown, Info, Plus, Trash2, X } from "lucide-react";
import type { Product } from "@/lib/contracts";
import { useStore } from "./client";
import { Busy, Button, Empty, ErrorState } from "./ui";
import { productPhoto, productPhotoStyle } from "./product-photo";
import { categoryLabel, startingPrice, variantLabel } from "./product-labels";
import { SpecificationSources } from "./product-specifications";
import { useCompareMessage } from "./shopping-feedback";
import { compareSpecification, storageFromVariant } from "./specification-comparison";
import styles from "./product-compare.module.css";

type FieldComparison = ReturnType<typeof compareSpecification>;
type CompareItem = { id: string; label: string; values: string[]; different: boolean; comparison: FieldComparison };

export function ProductCompare() {
  const s = useStore(), [onlyDifferences, setOnlyDifferences] = useState(false), message = useCompareMessage();
  const products = s.compare.map(id => s.products.find(p => p.id === id)).filter((p): p is Product => !!p);
  useEffect(() => { if (products.length < 2) setOnlyDifferences(false); }, [products.length]);
  const groups = useMemo(() => {
    const map = new Map<string, { id: string; label: string; items: Map<string, CompareItem> }>();
    for (const product of products) for (const group of product.technicalSpecs?.groups ?? []) {
      if (!map.has(group.id)) map.set(group.id, { id: group.id, label: group.label, items: new Map() });
      for (const item of group.items) {
        const values = products.map(p => p.technicalSpecs?.groups.find(g => g.id === group.id)?.items.find(i => i.id === item.id)?.value?.trim() || "—");
        map.get(group.id)!.items.set(item.id, { id: item.id, label: item.label, values,
          different: new Set(values.map(value => value.replace(/\s+/g, " ").toLocaleLowerCase())).size > 1,
          comparison: compareSpecification(item.id, values, s.locale) });
      }
    }
    return [...map.values()].map(group => ({ ...group, items: [...group.items.values()] }));
  }, [products.map(p => p.id + JSON.stringify(p.technicalSpecs)).join("|"), s.locale]);
  const visibleGroups = groups.map(group => ({ ...group, items: group.items.filter(item => !onlyDifferences || item.different) })).filter(group => group.items.length);
  const storageValues = products.map(product => storageFromVariant(product).join(" / ") || "—");
  const storageComparison = compareSpecification("storage", storageValues, s.locale);
  const storageDiffers = new Set(storageValues).size > 1;
  const differences = groups.reduce((n, group) => n + group.items.filter(item => item.different).length, 0) + Number(storageDiffers);
  const category = products[0]?.category;
  const addHref = `/${s.locale}/products${category ? `?category=${category}` : ""}`;
  return <div className={`page-wrap ${styles.page}`}>
    <div className={styles.heading}><span className="eyebrow">{s.tr("ĐẶT CẠNH NHAU. NHÌN RÕ HƠN.", "SIDE BY SIDE. CLEARER CHOICES.")}</span><h1>{s.tr("Lựa chọn nào dành cho bạn?", "Find the one for you.")}</h1><p>{s.tr("Từ khác biệt nhỏ đến điều bạn cần nhất. So sánh tối đa ba sản phẩm cùng nhóm.", "From the smallest differences to what matters most. Compare up to three products in one category.")}</p></div>
    {s.catalogueLoading ? <Busy /> : s.catalogueError ? <ErrorState message={s.catalogueError} retry={s.refreshCatalogue} /> : !products.length ? <div className={styles.empty}><ArrowLeftRight size={34} aria-hidden /><Empty title={s.tr("Bắt đầu với những lựa chọn bạn thích.", "Start with your favorites.")} description={s.tr("Nhấn “So sánh” trên sản phẩm. Các lựa chọn sẽ xuất hiện cùng nhau tại đây.", "Select Compare on any product. Your choices will come together here.")}><Link className="button button-primary" href={addHref}>{s.tr("Chọn sản phẩm", "Choose products")}<ArrowRight size={16} /></Link></Empty>{message && <p role="status">{message}</p>}</div> : <>
      <div className={styles.toolbar}>
        <div><span className={styles.count}>{products.length}/3</span><strong>{categoryLabel(products[0], s.categories, s.locale)}</strong><span className={styles.differenceCount}>{s.tr(`${differences} thông số khác nhau`, `${differences} specification differences`)}</span></div>
        <div>{products.length < 3 && <Link className="button button-outline button-sm" href={addHref}><Plus size={15} />{s.tr("Thêm sản phẩm", "Add product")}</Link>}<Button size="sm" variant="ghost" onClick={s.clearCompare}><Trash2 size={15} />{s.tr("Xóa tất cả", "Clear all")}</Button></div>
      </div>
      <div className={styles.controls}><button className={styles.switch} type="button" role="switch" aria-checked={onlyDifferences} onClick={() => setOnlyDifferences(!onlyDifferences)} disabled={products.length < 2}><span className={styles.track}><span /></span>{s.tr("Chỉ xem khác biệt", "Show differences only")}</button><div className={styles.legend}><span>{s.tr("Nền nhạt: có khác biệt", "Soft shading: different")}</span><span className={styles.advantageKey}>{s.tr("In đậm: ưu điểm theo từng thông số", "Bold: an advantage in this specification")}</span></div></div>
      <p className={message || products.length === 1 ? styles.feedback : styles.srOnly} role="status" aria-live="polite" aria-atomic="true">{message || (products.length === 1 ? s.tr("Thêm một sản phẩm nữa để bắt đầu đối chiếu.", "Add another product to start comparing.") : s.tr("Các lựa chọn của bạn đã sẵn sàng.", "Your selections are ready."))}</p>
      <p className={styles.scrollHint}><ChevronsUpDown size={16} aria-hidden />{s.tr("Cuộn trong bảng để xem toàn bộ sản phẩm và thông số.", "Scroll within the table to see all products and specifications.")}</p>
      <div className={styles.tableRegion} tabIndex={0} role="region" aria-label={s.tr("Bảng so sánh sản phẩm", "Product comparison table")}>
        <table className={`compare-table ${styles.table}`} style={{ "--products": products.length } as React.CSSProperties}>
          <caption className={styles.srOnly}>{s.tr("So sánh thông số đã xác minh. Dấu gạch ngang nghĩa là chưa có thông tin. Giá trị in đậm có ưu điểm được giải thích ở từng thông số, không phải xếp hạng tổng thể sản phẩm.", "Verified specification comparison. A dash means information is unavailable. Bold values have an explained advantage for that specification, not an overall product ranking.")}</caption>
          <thead><tr><th scope="col" className={styles.featureHeading}><span>{s.tr("Thông số", "Specifications")}</span><small>{s.tr("Lựa chọn của bạn", "Your selection")}</small></th>{products.map(product => {
            const photo = productPhoto(product), price = startingPrice(product);
            return <th scope="col" key={product.id} className={styles.productHeader}>
              <button type="button" className={styles.remove} aria-label={s.tr(`Bỏ ${product.name} khỏi so sánh`, `Remove ${product.name} from comparison`)} onClick={() => s.toggleCompare(product.id)}><X size={16} /></button>
              <div className={styles.productIdentity}>
                <span className={styles.productImage}><img src={photo.path} alt="" width={76} height={88} style={productPhotoStyle(photo)} /></span>
                <div className={styles.productInfo}><h2>{product.name}</h2><p className={styles.price}>{s.tr("Từ", "From")} {price ? s.money(price) : "—"}</p><Link className={styles.productCta} href={`/${s.locale}/products/${product.slug}`}>{s.tr("Xem sản phẩm", "View product")}<ArrowRight size={13} aria-hidden /></Link></div>
              </div>
            </th>;
          })}</tr></thead>
          <tbody>
            {(!onlyDifferences || storageDiffers) && <><tr className={styles.groupRow}><th colSpan={products.length + 1}>{s.tr("Lựa chọn tại BotZone", "Your BotZone options")}</th></tr><tr className={storageDiffers ? styles.different : undefined} data-different={storageDiffers} data-spec-id="variant-storage"><th scope="row"><RowLabel label={s.tr("Dung lượng đang bán", "Storage offered")} different={storageDiffers} note={storageComparison.note} /></th>{storageValues.map((value, index) => <ComparisonValue key={products[index].id} value={value} comparison={storageComparison.cells[index]} />)}</tr></>}
            {!onlyDifferences && <><tr><th scope="row">{s.tr("Cấu hình", "Configurations")}</th>{products.map(product => <td key={product.id}>{product.variants.map(variant => <p key={variant.id}>{variantLabel(variant)}</p>)}</td>)}</tr><tr><th scope="row">{s.tr("Tình trạng", "Availability")}</th>{products.map(product => <td key={product.id}>{product.variants.some(v => v.active && v.available > 0) ? <span className={styles.available}><Check size={13} />{s.tr("Có cấu hình còn hàng", "Available configurations")}</span> : s.tr("Tạm hết hàng", "Currently out of stock")}</td>)}</tr></>}
            {visibleGroups.map(group => <CompareGroup key={group.id} group={group} products={products} />)}
            {!visibleGroups.length && (!onlyDifferences || !storageDiffers) && <tr><td colSpan={products.length + 1} className={styles.noRows}>{onlyDifferences ? s.tr("Không có thông số khác nhau trong dữ liệu hiện có.", "No differences in the specifications currently available.") : s.tr("Thông số chi tiết đã xác minh đang được cập nhật.", "Verified detailed specifications are being prepared.")}</td></tr>}
          </tbody>
        </table>
      </div>
      <div className={styles.sourceGrid}>{products.map(product => <div key={product.id}><strong>{product.name}</strong><SpecificationSources product={product} /></div>)}</div>
    </>}
  </div>;
}

function CompareGroup({ group, products }: { group: { label: string; items: CompareItem[] }; products: Product[] }) {
  return <><tr className={styles.groupRow}><th colSpan={products.length + 1}>{group.label}</th></tr>{group.items.map(item => <tr key={item.id} className={item.different ? styles.different : undefined} data-different={item.different} data-spec-id={item.id}>
    <th scope="row"><RowLabel label={item.label} different={item.different} note={item.comparison.note} /></th>
    {item.values.map((value, index) => <ComparisonValue key={products[index].id} value={value} comparison={item.comparison.cells[index]} />)}
  </tr>)}</>;
}

function RowLabel({ label, different, note }: { label: string; different: boolean; note?: string }) {
  const s = useStore();
  return <><span>{label}{different && <span className={styles.diffMark} aria-label={s.tr("Có khác biệt", "Different")}>·</span>}</span>{different && note && <details className={styles.rowContext} onKeyDown={closeExplanation}>
    <summary aria-label={s.tr(`Lưu ý khi so sánh: ${label}`, `Comparison context: ${label}`)}><Info size={13} aria-hidden /></summary>
    <span className={styles.explanation}>{note}</span>
  </details>}</>;
}

function ComparisonValue({ value, comparison }: { value: string; comparison: FieldComparison["cells"][number] }) {
  const { advantage, summary, detail } = comparison;
  return <td className={advantage ? styles.advantage : undefined} data-advantage={advantage}>
    {advantage ? <strong>{value}</strong> : value}
    {advantage && summary && (detail && detail !== summary ? <details className={styles.advantageReason} onKeyDown={closeExplanation}>
      <summary>{summary}<ChevronDown size={11} aria-hidden /></summary>
      <span className={styles.explanation}>{detail}</span>
    </details> : <span className={styles.reasonText}>{summary}</span>)}
  </td>;
}

function closeExplanation(event: React.KeyboardEvent<HTMLDetailsElement>) {
  if (event.key === "Escape" && event.currentTarget.open) {
    event.preventDefault();
    event.currentTarget.open = false;
    event.currentTarget.querySelector("summary")?.focus();
  }
}
