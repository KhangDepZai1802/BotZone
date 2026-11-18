import Link from "next/link";
export default function NotFound() {
  return (
    <main className="page-wrap empty">
      <span className="eyebrow">404</span>
      <h1>Trang này chưa có.</h1>
      <p>Trở về BotZone để tiếp tục khám phá.</p>
      <Link className="button button-primary" href="/vi">
        Về trang chủ
      </Link>
    </main>
  );
}
