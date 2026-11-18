"use client";
import { useEffect, useRef, useState } from "react";
import { api, useStore } from "./client";
import { Button } from "./ui";

export function EmailVerification({ checkout = false }: { checkout?: boolean }) {
  const s = useStore();
  const [busy, setBusy] = useState(false), [message, setMessage] = useState(""), [error, setError] = useState("");
  const sending = useRef(false);
  useEffect(() => { setMessage(""); setError(""); }, [s.actor?.id, s.locale]);
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
      await api("/api/auth/resend-verification", { next: checkout ? `/${s.locale}/checkout` : `/${s.locale}/account` });
      setMessage(s.tr("Đã gửi liên kết xác minh. Hãy mở email, xác minh rồi quay lại đây.", "Verification link sent. Open your email, verify it, then return here."));
    } catch (e) { setError((e as Error).message); }
    finally { sending.current = false; setBusy(false); }
  }
  return <div className="panel form-stack" aria-label={s.tr("Xác minh email", "Email verification")}>
    <p>{s.tr("Xác minh email để đặt hàng:", "Verify your email to place an order:")} <strong>{s.actor.email}</strong></p>
    <div className="actions">
      <Button variant="outline" disabled={busy} onClick={() => void resend()}>{busy ? s.tr("Đang gửi…", "Sending…") : s.tr("Gửi email xác minh", "Send verification email")}</Button>
      <Button variant="ghost" disabled={busy} onClick={() => void s.refreshSession()}>{s.tr("Tôi đã xác minh — kiểm tra lại", "I have verified — check again")}</Button>
    </div>
    {message && <p role="status">{message}</p>}
    {error && <p role="alert">{error}</p>}
  </div>;
}
