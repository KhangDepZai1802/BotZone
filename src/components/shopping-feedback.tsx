"use client";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { ArrowRight, Check, GitCompareArrows, Plus, ShoppingBag, Trash2, X } from "lucide-react";
import type { CartItem } from "@/lib/contracts";
import { useStore } from "./client";
import { Button } from "./ui";
import { productPhoto, productPhotoStyle } from "./product-photo";
import { variantLabel } from "./product-labels";
import styles from "./shopping-feedback.module.css";

export function useCompareMessage() {
  const s = useStore(), feedback = s.compareFeedback;
  const name = s.products.find(p => p.id === feedback?.productId)?.name ?? "";
  if (!feedback) return "";
  const messages = {
    added: s.tr(`Đã thêm ${name} vào so sánh.`, `${name} added to comparison.`),
    removed: s.tr(`Đã bỏ ${name} khỏi so sánh.`, `${name} removed from comparison.`),
    limit: s.tr("Đã đủ 3 sản phẩm. Bỏ một lựa chọn để thêm sản phẩm khác.", "You have 3 products. Remove one to add another."),
    category: s.tr("Hãy chọn sản phẩm cùng nhóm để so sánh công bằng.", "Choose products in the same category to compare."),
    cleared: s.tr("Đã xóa tất cả lựa chọn so sánh.", "All comparison selections cleared."),
  };
  return messages[feedback.kind];
}

export function CompareDock() {
  const s = useStore(), path = usePathname(), message = useCompareMessage();
  const products = s.compare.map(id => s.products.find(p => p.id === id)).filter(p => !!p);
  const sequence = s.compareFeedback?.sequence;
  useEffect(() => {
    if (!sequence) return;
    const timer = window.setTimeout(s.dismissCompareFeedback, 6500);
    return () => window.clearTimeout(timer);
  }, [sequence]);
  if (path.endsWith("/compare") || (!products.length && !message)) return null;
  const category = products[0]?.category;
  return <aside className={styles.dock} aria-label={s.tr("Lựa chọn so sánh", "Comparison selection")} data-testid="compare-dock">
    <div className={styles.dockTop}>
      <strong><GitCompareArrows size={19} aria-hidden />{s.tr("So sánh sản phẩm", "Compare products")} <span>{products.length}/3</span></strong>
      {products.length ? <button type="button" className={styles.clear} onClick={s.clearCompare}><Trash2 size={14} aria-hidden />{s.tr("Xóa tất cả", "Clear all")}</button> : <button type="button" className={styles.icon} onClick={s.dismissCompareFeedback} aria-label={s.tr("Đóng thông báo", "Dismiss notification")}><X size={18} /></button>}
    </div>
    {products.length > 0 && <div className={styles.dockBody}>
      <div className={styles.slots}>{[0, 1, 2].map(index => {
        const product = products[index];
        if (!product) return <Link key={index} href={`/${s.locale}/products${category ? `?category=${category}` : ""}`} className={styles.emptySlot} aria-label={s.tr("Chọn thêm sản phẩm cùng nhóm", "Choose another product in this category")}><Plus size={18} /><span>{s.tr("Thêm sản phẩm", "Add product")}</span></Link>;
        const photo = productPhoto(product);
        return <div className={styles.slot} key={product.id}>
          <span className={styles.thumb}><img src={photo.path} alt="" width={64} height={64} style={productPhotoStyle(photo)} /></span>
          <span className={styles.slotName}>{product.name}</span>
          <button type="button" className={styles.remove} onClick={() => s.toggleCompare(product.id)} aria-label={s.tr(`Bỏ ${product.name} khỏi so sánh`, `Remove ${product.name} from comparison`)}><X size={14} /></button>
        </div>;
      })}</div>
      <Link className={`button button-primary ${styles.open}`} href={`/${s.locale}/compare`}>{s.tr("Mở bảng so sánh", "Open comparison")}<ArrowRight size={16} aria-hidden /></Link>
    </div>}
    <p className={styles.dockStatus} role="status" aria-live="polite" aria-atomic="true">{message || s.tr("Chọn tối đa 3 sản phẩm cùng nhóm.", "Choose up to 3 products in one category.")}</p>
  </aside>;
}

export function AddToBagButton({ item, disabled }: { item: CartItem; disabled?: boolean }) {
  const s = useStore();
  const [pending, setPending] = useState(0), [added, setAdded] = useState(false);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);
  useEffect(() => () => { if (timer.current) clearTimeout(timer.current); }, []);
  useEffect(() => { setAdded(false); }, [item.variantId, item.care]);
  const add = async () => {
    setPending(n => n + 1);
    const success = await s.addCart(item);
    setPending(n => Math.max(0, n - 1));
    if (success) {
      setAdded(true);
      if (timer.current) clearTimeout(timer.current);
      timer.current = setTimeout(() => setAdded(false), 1900);
    }
  };
  return <Button className={styles.addButton} disabled={disabled} aria-busy={pending > 0} onClick={add} data-added={added} data-testid="add-to-bag">
    {added && !pending ? <Check size={18} aria-hidden /> : <ShoppingBag size={18} aria-hidden />}
    {pending ? s.tr("Đang thêm…", "Adding…") : added ? s.tr("Đã thêm vào túi", "Added to your bag") : s.tr("Thêm vào giỏ hàng", "Add to bag")}
  </Button>;
}

export function BagConfirmation() {
  const s = useStore(), path = usePathname();
  const feedback = s.bagFeedback, panel = useRef<HTMLElement>(null), previousPath = useRef(path);
  const [position, setPosition] = useState({ top: 92, right: 20 });
  const [paused, setPaused] = useState(false);
  useEffect(() => { setPaused(false); }, [feedback?.sequence]);
  const dismiss = () => {
    if (panel.current?.contains(document.activeElement)) document.querySelector<HTMLAnchorElement>('[data-bag-anchor]')?.focus({ preventScroll: true });
    s.dismissBagFeedback();
  };
  useEffect(() => {
    if (previousPath.current !== path) { s.dismissBagFeedback(); previousPath.current = path; }
  }, [path]);
  useEffect(() => {
    if (!feedback || paused) return;
    const timer = window.setTimeout(s.dismissBagFeedback, 6500);
    return () => window.clearTimeout(timer);
  }, [feedback?.sequence, paused]);
  useLayoutEffect(() => {
    if (!feedback) return;
    const measure = () => {
      const anchor = document.querySelector('[data-bag-anchor]')?.getBoundingClientRect();
      const header = document.querySelector('.site-header')?.getBoundingClientRect();
      if (anchor && header) {
        const next = { top: Math.max(12, header.bottom + 10), right: Math.max(20, window.innerWidth - anchor.right - 12) };
        setPosition(previous => previous.top === next.top && previous.right === next.right ? previous : next);
      }
    };
    let frame = 0;
    const schedule = () => { if (!frame) frame = requestAnimationFrame(() => { frame = 0; measure(); }); };
    measure(); window.addEventListener("resize", schedule); window.addEventListener("scroll", schedule, { passive: true });
    const escape = (event: KeyboardEvent) => { if (event.key === "Escape") dismiss(); };
    const outside = (event: PointerEvent) => { if (panel.current && !panel.current.contains(event.target as Node) && !(event.target as Element).closest?.('[data-bag-anchor]')) s.dismissBagFeedback(); };
    window.addEventListener("keydown", escape); window.addEventListener("pointerdown", outside);
    return () => { cancelAnimationFrame(frame); window.removeEventListener("resize", schedule); window.removeEventListener("scroll", schedule); window.removeEventListener("keydown", escape); window.removeEventListener("pointerdown", outside); };
  }, [feedback?.sequence]);
  if (!feedback) return null;
  const product = s.products.find(p => p.variants.some(v => v.id === feedback.variantId));
  const variant = product?.variants.find(v => v.id === feedback.variantId);
  if (!product || !variant) return null;
  const photo = productPhoto(product, variant);
  return <section ref={panel} className={styles.bagPanel} style={position} aria-label={s.tr("Sản phẩm vừa thêm vào túi", "Recently added to your bag")} data-testid="bag-confirmation"
    onMouseEnter={() => setPaused(true)} onMouseLeave={() => setPaused(false)} onFocusCapture={() => setPaused(true)} onBlurCapture={event => { if (!event.currentTarget.contains(event.relatedTarget)) setPaused(false); }}>
    <div className={styles.bagHeading}><span><Check size={16} aria-hidden />{s.tr("Đã thêm vào túi", "Added to your bag")}</span><button type="button" className={styles.icon} onClick={dismiss} aria-label={s.tr("Đóng xác nhận giỏ hàng", "Dismiss bag confirmation")}><X size={18} /></button></div>
    <div className={styles.bagItem} key={feedback.sequence}>
      <span className={styles.bagImage}><img src={photo.path} alt="" width={92} height={100} style={productPhotoStyle(photo)} /></span>
      <div><strong>{product.name}</strong><p>{variantLabel(variant)}</p>{feedback.care && <p>+ BotZone Care</p>}<p>{s.tr(`Số lượng trong túi: ${feedback.lineQuantity}`, `Quantity in bag: ${feedback.lineQuantity}`)}</p><b>{s.money(variant.priceVnd)}</b></div>
    </div>
    <p className={styles.bagStatus} role="status" aria-live="polite" aria-atomic="true">{s.tr(`Đã thêm ${feedback.quantity} ${product.name}. Túi có ${feedback.totalQuantity} sản phẩm.`, `Added ${feedback.quantity} ${product.name}. Your bag has ${feedback.totalQuantity} items.`)}</p>
    <div className={styles.bagActions}><Link className="button button-primary" href={`/${s.locale}/cart`} onClick={s.dismissBagFeedback}>{s.tr("Xem túi hàng", "View your bag")}<ArrowRight size={16} /></Link><button type="button" className="button button-ghost" onClick={dismiss}>{s.tr("Tiếp tục khám phá", "Keep exploring")}</button></div>
  </section>;
}
