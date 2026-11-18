"use client";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import {
  ArrowUpRight,
  ArrowLeft,
  Menu,
  Search,
  ShoppingBag,
  UserRound,
  X,
  Heart,
} from "lucide-react";
import { useStore } from "./client";
import { Button, Select } from "./ui";
import { useQueryParams } from "./url-state";
import { BagConfirmation, CompareDock } from "./shopping-feedback";
import { BrandLogo } from "./brand-logo";
import feedbackStyles from "./shopping-feedback.module.css";
import shellStyles from "./store-shell.module.css";
export function StoreShell({ children }: { children: React.ReactNode }) {
  const s = useStore(),
    path = usePathname(),
    params = useQueryParams(),
    [menu, setMenu] = useState(false);
  const content = useRef<HTMLElement>(null);
  const menuTrigger = useRef<HTMLButtonElement>(null);
  useEffect(() => { s.dismissBagFeedback(); s.dismissCompareFeedback(); }, [path]);
  useEffect(() => {
    const preference = window.matchMedia("(prefers-reduced-motion: reduce)");
    if (preference.matches) return;
    const animation = content.current?.animate(
      [{ opacity: 0.45, transform: "translateY(6px)" }, { opacity: 1, transform: "translateY(0)" }],
      { duration: 200, easing: "cubic-bezier(.2,.7,.2,1)" },
    );
    const stop = () => animation?.cancel();
    preference.addEventListener("change", stop);
    return () => { stop(); preference.removeEventListener("change", stop); };
  }, [path]);
  useEffect(() => {
    if (!menu) return;
    const close = (event: KeyboardEvent) => {
      if (event.key === "Escape") { setMenu(false); menuTrigger.current?.focus(); }
    };
    window.addEventListener("keydown", close);
    return () => window.removeEventListener("keydown", close);
  }, [menu]);
  const other = s.locale === "vi" ? "en" : "vi";
  const switchPath =
    path.replace(/^\/(vi|en)(?=\/|$)/, `/${other}`) +
    (params.toString() ? "?" + params.toString() : "");
  if (path.endsWith("/preview") && params.get("frame") === "1")
    return <main id="main-content">{children}</main>;
  if (/\/(login|register|forgot-password|reset-password|verify-email|change-password)\/?$/.test(path))
    return <div className={shellStyles.authShell}>
      <a href="#main-content" className="skip-link">{s.tr("Đến nội dung chính", "Skip to content")}</a>
      <header className={shellStyles.authHeader}>
        <Link className="wordmark" href={`/${s.locale}`} aria-label={s.tr("BotZone — Trang chủ", "BotZone — Home")}><BrandLogo /></Link>
        <div><Link className={shellStyles.backShop} href={`/${s.locale}/products`}><ArrowLeft size={15} aria-hidden /><span>{s.tr("Về cửa hàng", "Back to the store")}</span></Link><Link className="language" href={switchPath} aria-label={s.tr("Switch to English", "Chuyển sang tiếng Việt")}>{other.toUpperCase()}</Link></div>
      </header>
      <main id="main-content" ref={content}>{children}</main>
      <p className={shellStyles.authFooter}>© 2026 BotZone · {s.tr("Mọi kết nối bắt đầu từ đây.", "Every connection begins here.")}</p>
    </div>;
  return (
    <>
      <a href="#main-content" className="skip-link">
        {s.tr("Đến nội dung chính", "Skip to content")}
      </a>
      <div className="announcement">
        {s.tr(
          "Giao miễn phí từ 2 triệu · Trải nghiệm mua sắm Apple tại BotZone",
          "Free delivery from 2,000,000 ₫ · Your Apple experience, at BotZone",
        )}
        <ArrowUpRight size={12} aria-hidden />
      </div>
      <header className="site-header">
        <Link
          className="wordmark"
          href={`/${s.locale}`}
          aria-label={s.tr("BotZone — Trang chủ", "BotZone — Home")}
        >
          <BrandLogo />
        </Link>
        <nav
          className="desktop-nav"
          aria-label={s.tr("Điều hướng chính", "Main navigation")}
        >
          <Link href={`/${s.locale}/products`}>
            {s.tr("Khám phá", "Explore")}
          </Link>
          <Link href={`/${s.locale}/products?category=iphone`}>iPhone</Link>
          <Link href={`/${s.locale}/products?category=mac`}>Mac</Link>
          <Link href={`/${s.locale}/products?category=ipad`}>iPad</Link>
          <Link href={`/${s.locale}/trade-in`}>
            {s.tr("Thu cũ đổi mới", "Trade in")}
          </Link>
          <Link href={`/${s.locale}/care`}>BotZone Care</Link>
        </nav>
        <div className="header-actions">
          <Link
            className="icon-link hide-small"
            href={`/${s.locale}/products`}
            aria-label={s.tr("Tìm kiếm", "Search")}
          >
            <Search size={20} />
          </Link>
          <Link
            className="icon-link hide-small"
            href={`/${s.locale}/account`}
            aria-label={s.tr("Tài khoản", "Account")}
          >
            <UserRound size={20} />
          </Link>
          <Link
            className="icon-link bag-link"
            data-bag-anchor
            href={`/${s.locale}/cart`}
            aria-label={s.tr("Giỏ hàng", "Shopping bag")}
          >
            <ShoppingBag size={20} />
            {s.cart.length > 0 && (
              <span key={s.bagFeedback?.sequence ?? "bag"} className={s.bagFeedback ? feedbackStyles.badgePulse : undefined}>{s.cart.reduce((n, i) => n + i.quantity, 0)}</span>
            )}
          </Link>
          <Link className="language" href={switchPath}>
            {other.toUpperCase()}
          </Link>
          <Select
            aria-label={s.tr("Tiền tệ hiển thị", "Display currency")}
            value={s.currency}
            onChange={(e) => s.setCurrency(e.target.value as "VND" | "USD")}
            className="currency-select"
          >
            <option>VND</option>
            <option>USD</option>
          </Select>
          <Button
            ref={menuTrigger}
            className="mobile-menu"
            size="icon"
            variant="ghost"
            aria-label={s.tr("Mở menu", "Open menu")}
            aria-expanded={menu}
            onClick={() => setMenu(!menu)}
          >
            {menu ? <X /> : <Menu />}
          </Button>
        </div>
      </header>
      {menu && (
        <nav className="mobile-navigation" aria-label={s.tr("Điều hướng di động", "Mobile navigation")} onClick={() => setMenu(false)}>
          {[
            ["products", s.tr("Tất cả sản phẩm", "All products")],
            ["trade-in", s.tr("Thu cũ đổi mới", "Trade in")],
            ["care", "BotZone Care"],
            ["account", s.tr("Tài khoản", "Account")],
            ["wishlist", s.tr("Yêu thích", "Wishlist")],
            ["compare", s.tr("So sánh", "Compare")],
          ].map(([href, label]) => (
            <Link key={href} href={`/${s.locale}/${href}`}>
              {label}
              <ArrowUpRight size={18} />
            </Link>
          ))}
        </nav>
      )}
      <main id="main-content" ref={content} className={`route-content ${s.compare.length && !path.endsWith("/compare") ? shellStyles.withDock : ""}`}>{children}</main>
      <CompareDock />
      <BagConfirmation />
      <footer className="site-footer">
        <div className="footer-top">
          <div>
            <Link className="wordmark" href={`/${s.locale}`} aria-label={s.tr("BotZone — Trang chủ", "BotZone — Home")}>
              <BrandLogo prominent />
            </Link>
            <h2>
              {s.tr(
                "Một thế giới.\nMọi kết nối.",
                "One world.\nAll connected.",
              )}
            </h2>
          </div>
          <div>
            <h3>{s.tr("Khám phá BotZone", "Explore BotZone")}</h3>
            <Link href={`/${s.locale}/products`}>
              {s.tr("Sản phẩm", "Products")}
            </Link>
            <Link href={`/${s.locale}/trade-in`}>
              {s.tr("Thu cũ đổi mới", "Trade in")}
            </Link>
            <Link href={`/${s.locale}/care`}>BotZone Care</Link>
            <Link href={`/${s.locale}/wishlist`}>
              <Heart size={14} />
              {s.tr("Yêu thích", "Wishlist")}
            </Link>
          </div>
          <div>
            <h3>{s.tr("Luôn bên bạn", "Here for you")}</h3>
            <Link href={`/${s.locale}/policy/shipping`}>
              {s.tr("Giao hàng", "Shipping")}
            </Link>
            <Link href={`/${s.locale}/policy/returns`}>
              {s.tr("Trả hàng & hoàn tiền", "Returns & refunds")}
            </Link>
            <Link href={`/${s.locale}/policy/privacy`}>
              {s.tr("Quyền riêng tư", "Privacy")}
            </Link>
            <Link href={`/${s.locale}/account`}>
              {s.tr("Tài khoản của tôi", "My account")}
            </Link>
          </div>
          <div className="footer-note">
            <span className="status-dot" />
            {s.tr("Không gian demo local", "Local demo environment")}
            <p>
              {s.tr(
                "Thanh toán và vận chuyển được mô phỏng. Không sử dụng tiền thật.",
                "Payments and delivery are simulated. No real money is accepted.",
              )}
            </p>
          </div>
        </div>
        <div className="footer-bottom">
          <span>
            © 2026 BotZone · {s.tr("Đồ án học phần", "Course project")}
          </span>
          <span>
            {s.tr(
              "Thiết kế dành cho những kết nối tốt hơn.",
              "Designed for better connections.",
            )}
          </span>
        </div>
      </footer>
    </>
  );
}
