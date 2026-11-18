"use client";
import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import type { Actor } from "@/lib/contracts";
import { api, useStore } from "./client";
import { Button } from "./ui";

export function EmailVerification({ checkout = false }: { checkout?: boolean }) {
  const s = useStore();
  const [busy, setBusy] = useState(false), [message, setMessage] = useState(""), [error, setError] = useState("");
  const [verificationHref, setVerificationHref] = useState("");
  const sending = useRef(false);
  useEffect(() => { setMessage(""); setError(""); setVerificationHref(""); }, [s.actor?.id, s.locale]);
  useEffect(() => {
    if (!s.actor || s.actor.verified) return;
    const check = () => { void s.refreshSession(); };
    window.addEventListener("focus", check);
    return () => window.removeEventListener("focus", check);
  }, [s.actor?.id, s.actor?.verified, s.refreshSession]);
  if (!s.actor || s.actor.verified) return null;
  async function resend() {
    if (sending.current) return;
    sending.current = true; setBusy(true); setError(""); setMessage("");
    try {
      const result = await api<{ verified?: boolean; deliveryMode?: string; verificationHref?: string }>("/api/auth/resend-verification", { next: checkout ? `/${s.locale}/checkout` : `/${s.locale}/account` });
      if (result.verified) { await s.refreshSession(); return; }
      setVerificationHref(result.verificationHref ?? "");
      setMessage(result.deliveryMode === "local_demo"
        ? s.tr("Thư xác minh được gửi vào hộp thư demo, không gửi đến Gmail. Bấm liên kết bên dưới để xác minh và quay lại thanh toán.", "Verification mail goes to the demo inbox, not Gmail. Open the link below to verify and return to checkout.")
        : s.tr("Đã gửi liên kết xác minh. Hãy mở email, xác minh rồi quay lại đây.", "Verification link sent. Open your email, verify it, then return here."));
    } catch (e) { setError((e as Error).message); }
    finally { sending.current = false; setBusy(false); }
  }
  async function check() {
    if (sending.current) return;
    sending.current = true; setBusy(true); setError("");
    try {
      const session = await api<{ actor: Actor | null }>("/api/session");
      await s.refreshSession();
      if (!session.actor?.verified) setMessage(s.tr("Email chưa được xác minh. Hãy bấm Gửi email xác minh, rồi mở liên kết xác minh. Nút kiểm tra không tự xác minh email.", "Email is not verified yet. Send the verification email, then open its verification link. Checking alone does not verify your email."));
    } catch (e) { setError((e as Error).message); }
    finally { sending.current = false; setBusy(false); }
  }
  return <section id="email-verification" tabIndex={-1} className="panel form-stack" aria-label={s.tr("Xác minh email", "Email verification")}>
    <p>{s.tr("Xác minh email để đặt hàng:", "Verify your email to place an order:")} <strong>{s.actor.email}</strong></p>
    <div className="actions">
      <Button variant="outline" disabled={busy} onClick={() => void resend()}>{busy ? s.tr("Đang gửi…", "Sending…") : s.tr("Gửi email xác minh", "Send verification email")}</Button>
      <Button variant="ghost" disabled={busy} onClick={() => void check()}>{s.tr("Tôi đã xác minh — kiểm tra lại", "I have verified — check again")}</Button>
    </div>
    {message && <p role="status">{message}</p>}
    {verificationHref && <Link className="button button-primary" href={verificationHref}>{s.tr("Mở liên kết xác minh demo", "Open demo verification link")}</Link>}
    {error && <p role="alert">{error}</p>}
  </section>;
}
