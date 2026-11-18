"use client";
import { useEffect, useRef, useState, type FormEvent, type InputHTMLAttributes } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { ArrowRight, Check, CheckCircle2, Heart, LoaderCircle, PackageCheck, ShieldCheck } from "lucide-react";
import type { Address } from "@/lib/contracts";
import { api, useStore } from "./client";
import { PasswordField } from "./password-field";
import { authModes, clearAuthLocaleDraft, prepareAuthLocaleDraft, readAuthLocaleDraft, safeAuthDestination, validateAuth, type AuthErrors, type AuthFieldError, type AuthMode, type AuthValues } from "./auth-helpers";
import styles from "./auth-page.module.css";

const initialValues = (): AuthValues => ({ name: "", email: "", phone: "", password: "", confirmPassword: "", currentPassword: "", saveAddress: false,
  address: { recipient: "", phone: "", province: "", ward: "", addressLine: "", country: "VN" } });
const headings: Record<AuthMode, [string, string]> = {
  login: ["Chào bạn trở lại.", "Welcome back."], register: ["Bắt đầu kết nối.", "Make yourself at home."],
  "forgot-password": ["Tìm lại kết nối.", "Let’s get you back in."], "reset-password": ["Một khởi đầu mới.", "A fresh start."],
  "verify-email": ["Xác minh email.", "Verify your email."], "change-password": ["Đổi mật khẩu.", "Change your password."],
};
const labels: Record<string, [string, string]> = {
  name: ["Họ và tên", "Full name"], email: ["Email", "Email"], phone: ["Số điện thoại", "Phone number"],
  password: ["Mật khẩu", "Password"], confirmPassword: ["Xác nhận mật khẩu", "Confirm password"], currentPassword: ["Mật khẩu hiện tại", "Current password"],
  "address-recipient": ["Người nhận", "Recipient"], "address-phone": ["Số điện thoại người nhận", "Recipient phone number"],
  "address-province": ["Tỉnh / Thành phố", "Province / City"], "address-ward": ["Phường / Xã", "Ward"], "address-addressLine": ["Số nhà, tên đường", "Street address"],
};
const validationMessages: Record<AuthFieldError, [string, string]> = {
  required: ["Vui lòng điền thông tin này.", "Please complete this field."], name: ["Họ tên cần từ 2 đến 100 ký tự.", "Use between 2 and 100 characters for your name."],
  email: ["Nhập địa chỉ email hợp lệ.", "Enter a valid email address."], phone: ["Nhập số điện thoại gồm 9–15 chữ số.", "Enter a phone number with 9–15 digits."],
  password: ["Dùng mật khẩu có ít nhất 6 ký tự.", "Use a password with at least 6 characters."],
  confirm: ["Mật khẩu xác nhận chưa khớp.", "The confirmation password does not match."],
  address: ["Nhập từ 2 đến 300 ký tự.", "Enter between 2 and 300 characters."],
};
const successMessages: Record<string, [string, string]> = {
  register: ["Tài khoản đã được tạo. Hãy mở email và làm theo liên kết xác minh để hoàn tất đăng ký.", "Your account is ready. Open your email and follow the verification link to finish signing up."],
  "forgot-password": ["Nếu tài khoản tồn tại, hướng dẫn đặt lại mật khẩu đã được gửi đến email của bạn.", "If the account exists, password reset instructions have been sent to your email."],
  "reset-password": ["Đã đặt lại mật khẩu. Bạn có thể đăng nhập bằng mật khẩu mới.", "Your password has been reset. You can sign in with your new password."],
  "verify-email": ["Email đã được xác minh. Bạn có thể tiếp tục mua sắm.", "Your email is verified. You can continue shopping."],
  "change-password": ["Đã đổi mật khẩu và kết thúc các phiên đăng nhập. Vui lòng đăng nhập lại.", "Your password has changed and all sessions have ended. Please sign in again."],
};

export function AuthPage({ initial }: { initial: string }) {
  const s = useStore(), router = useRouter(), params = useSearchParams();
  const mode: AuthMode = authModes.includes(initial as AuthMode) ? initial as AuthMode : "login";
  const [values, setValues] = useState<AuthValues>(() => {
    const draft = readAuthLocaleDraft(mode, s.locale);
    if (!draft) return initialValues();
    const { rememberMe: _remember, ...fields } = draft;
    return { ...initialValues(), ...fields };
  }), [rememberMe, setRememberMe] = useState(() => readAuthLocaleDraft(mode, s.locale)?.rememberMe ?? false);
  const [errors, setErrors] = useState<AuthErrors>({}), [failure, setFailure] = useState<Error | null>(null);
  const [success, setSuccess] = useState(""), [busy, setBusy] = useState(false);
  const errorSummary = useRef<HTMLDivElement>(null), requestEpoch = useRef(0), submitting = useRef(false);
  const addressPanel = useRef<HTMLDivElement>(null);
  const destination = safeAuthDestination(params.get("returnUrl") ?? params.get("next"), s.locale);
  const nextQuery = destination === `/${s.locale}/account` ? "" : `?next=${encodeURIComponent(destination)}`;
  const authHref = (route: string) => `/${s.locale}/${route}${nextQuery}`;
  const label = (key: string) => s.tr(...labels[key]);
  const fieldError = (key: string) => errors[key] ? s.tr(...validationMessages[errors[key]]) : undefined;
  const linkRequired = mode === "reset-password" || mode === "verify-email";
  const missingLink = linkRequired && (!params.get("userId") || !params.get("token"));
  const needsAccount = mode === "change-password" && !s.sessionLoading && !s.actor;
  const newPassword = ["register", "reset-password", "change-password"].includes(mode);

  useEffect(() => {
    clearAuthLocaleDraft();
    requestEpoch.current++; submitting.current = false; setBusy(false); setErrors({}); setFailure(null); setSuccess("");
    setValues(previous => ({ ...previous, password: "", confirmPassword: "", currentPassword: "" }));
    return () => { requestEpoch.current++; };
  }, [mode, s.locale]);
  useEffect(() => {
    const transferDraft = (event: MouseEvent) => {
      if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
      const link = event.target instanceof Element ? event.target.closest<HTMLAnchorElement>("a[href]") : null;
      if (!link) return;
      const target = new URL(link.href, location.href), nextLocale = s.locale === "vi" ? "en" : "vi";
      if (target.origin === location.origin && target.pathname.replace(/\/$/, "") === `/${nextLocale}/${mode}`)
        prepareAuthLocaleDraft(mode, nextLocale, values, rememberMe);
      else clearAuthLocaleDraft();
    };
    document.addEventListener("click", transferDraft, true);
    window.addEventListener("pagehide", clearAuthLocaleDraft);
    return () => { document.removeEventListener("click", transferDraft, true); window.removeEventListener("pagehide", clearAuthLocaleDraft); };
  }, [mode, s.locale, values, rememberMe]);
  const update = <K extends keyof AuthValues>(key: K, value: AuthValues[K]) => {
    setValues(previous => ({ ...previous, [key]: value })); setFailure(null);
    setErrors(previous => { const next = { ...previous }; delete next[key]; if (key === "password") delete next.confirmPassword; return next; });
  };
  const updateAddress = (key: keyof Address, value: string) => {
    setValues(previous => ({ ...previous, address: { ...previous.address, [key]: value } }));
    setErrors(previous => { const next = { ...previous }; delete next[`address-${key}`]; return next; });
  };
  function errorMessage(error: Error) {
    const code = (error as Error & { code?: string }).code;
    const known: Record<string, [string, string]> = {
      LOGIN_FAILED: ["Email hoặc mật khẩu không đúng, hoặc tài khoản đang bị khóa.", "The email or password is incorrect, or the account is locked."],
      PASSWORD_RESET_REQUIRED: ["Tài khoản cần đặt lại mật khẩu qua email trước khi đăng nhập.", "Reset your password by email before signing in."],
      TOKEN_INVALID: ["Liên kết không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu liên kết mới.", "This link is invalid or expired. Please request a new link."],
      TOKEN_USED: ["Email đã được xác minh. Bạn có thể quay lại đăng nhập.", "This email is already verified. You can return to sign in."],
      UNAUTHENTICATED: ["Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", "Your session has expired. Please sign in again."],
    };
    return code && known[code] ? s.tr(...known[code]) : error.message || s.tr("Chưa thể hoàn tất yêu cầu. Vui lòng thử lại.", "We could not complete your request. Please try again.");
  }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (submitting.current || missingLink || needsAccount) return;
    const found = validateAuth(mode, values); setErrors(found); setFailure(null); setSuccess("");
    if (Object.keys(found).length) { requestAnimationFrame(() => errorSummary.current?.focus()); return; }
    submitting.current = true; setBusy(true); const ticket = ++requestEpoch.current;
    try {
      if (mode === "login") {
        await api("/api/auth/login", { email: values.email.trim(), password: values.password, clientType: "web", rememberMe });
        if (ticket !== requestEpoch.current) return;
        await s.refreshSession(); if (ticket !== requestEpoch.current) return;
        clearAuthLocaleDraft(); setValues(previous => ({ ...previous, password: "" })); router.replace(destination);
      } else if (mode === "register") {
        const address = values.saveAddress ? Object.fromEntries(Object.entries(values.address).map(([key, value]) => [key, value.trim()])) : undefined;
        await api("/api/auth/register", { name: values.name.trim(), email: values.email.trim(), phone: values.phone.trim(), password: values.password, ...(address ? { address } : {}) });
      } else if (mode === "forgot-password") await api("/api/auth/forgot-password", { email: values.email.trim() });
      else if (mode === "reset-password") { await api("/api/auth/reset-password", { userId: params.get("userId"), token: params.get("token"), password: values.password }); if (ticket === requestEpoch.current) await s.refreshSession(); }
      else if (mode === "verify-email") { await api("/api/auth/verify-email", { userId: params.get("userId"), token: params.get("token") }); if (ticket === requestEpoch.current) await s.refreshSession(); }
      else if (mode === "change-password") { await api("/api/auth/change-password", { currentPassword: values.currentPassword, newPassword: values.password }); if (ticket === requestEpoch.current) await s.refreshSession(); }
      if (ticket !== requestEpoch.current) return;
      if (mode !== "login") { clearAuthLocaleDraft(); setSuccess(mode); setValues(previous => ({ ...previous, password: "", confirmPassword: "", currentPassword: "" })); }
    } catch (error) {
      if (ticket === requestEpoch.current) { setFailure(error instanceof Error ? error : new Error(s.tr("Chưa thể hoàn tất yêu cầu.", "We could not complete your request."))); requestAnimationFrame(() => errorSummary.current?.focus()); }
    } finally { if (ticket === requestEpoch.current) { submitting.current = false; setBusy(false); } }
  }
  function textField(key: "name" | "email" | "phone", props: InputHTMLAttributes<HTMLInputElement> = {}) {
    const error = fieldError(key);
    return <div className={styles.field}><label htmlFor={`auth-${key}`}>{label(key)}</label>
      <input {...props} id={`auth-${key}`} name={key} className={styles.input} value={values[key]} onChange={event => update(key, event.target.value)} disabled={busy} required aria-invalid={!!error} aria-describedby={error ? `auth-${key}-error` : undefined} />
      {error && <small className={styles.fieldError} id={`auth-${key}-error`}>{error}</small>}
    </div>;
  }
  const intro = mode === "register" ? s.tr("Tạo tài khoản để lưu lựa chọn, theo dõi đơn hàng và chăm sóc thiết bị của bạn.", "Create an account to save your favorites, follow orders and care for your devices.")
    : mode === "forgot-password" ? s.tr("Nhập email đã đăng ký. Chúng tôi sẽ gửi cho bạn liên kết đặt lại mật khẩu.", "Enter your account email. We will send you a link to reset your password.")
    : mode === "verify-email" ? s.tr("Xác nhận địa chỉ email để hoàn tất tài khoản và tiếp tục đặt hàng.", "Confirm your email to finish your account and continue to checkout.")
    : newPassword ? s.tr("Chọn mật khẩu mới cho tài khoản BotZone của bạn.", "Choose a new password for your BotZone account.")
    : s.tr("Đăng nhập để tiếp tục những trải nghiệm của bạn tại BotZone.", "Sign in to pick up where you left off at BotZone.");
  const successText = successMessages[success];
  return <div className={styles.page} data-auth-page={mode}>
    <aside className={styles.story} aria-label={s.tr("Tài khoản BotZone của bạn", "Your BotZone account")}>
      <div><span className={styles.storyMark}>{s.tr("TÀI KHOẢN BOTZONE", "BOTZONE ACCOUNT")}</span>
        <h2>{s.tr("Một tài khoản.\nMọi kết nối.", "One account.\nEvery connection.")}</h2>
        <p className={styles.storyIntro}>{s.tr("Từ lựa chọn đầu tiên đến những trải nghiệm dài lâu. BotZone luôn ở đây cùng bạn.", "From your first choice to everything that follows. Make room for what connects you.")}</p>
      </div>
      <ul className={styles.benefits}>
        <li><Heart size={21} aria-hidden /><span>{s.tr("Lưu những sản phẩm bạn yêu thích.", "Keep your favorite products close.")}</span></li>
        <li><PackageCheck size={21} aria-hidden /><span>{s.tr("Theo dõi đơn hàng trong cùng một nơi.", "Follow every order in one place.")}</span></li>
        <li><ShieldCheck size={21} aria-hidden /><span>{s.tr("Quản lý quyền lợi và chăm sóc sau mua.", "Manage your benefits and after-sales care.")}</span></li>
      </ul>
    </aside>
    <section className={`${styles.content} ${mode === "register" ? styles.register : ""}`} aria-labelledby="auth-title">
      <p className={styles.eyebrow}>{s.tr("TÀI KHOẢN BOTZONE", "YOUR BOTZONE")}</p>
      <h1 className={styles.heading} id="auth-title">{s.tr(...headings[mode])}</h1>
      <p className={styles.intro}>{intro}</p>
      {mode === "login" && s.actor && <div className={`${styles.notice} ${styles.session}`}><ShieldCheck size={19} aria-hidden /><div>
        <p>{s.tr("Bạn đang đăng nhập với", "You are signed in as")} {s.actor.email}.</p>
        <Link className={`${styles.link} ${styles.noticeLink}`} href={destination}>{s.tr("Tiếp tục với tài khoản này", "Continue with this account")}</Link>
      </div></div>}
      {missingLink ? <div className={`${styles.notice} ${styles.noticeError}`} role="alert"><ShieldCheck size={20} aria-hidden /><div>
        <p>{s.tr("Liên kết chưa đầy đủ. Hãy mở liên kết từ email đã nhận.", "This link is incomplete. Open the full link from your email.")}</p>
        {mode === "reset-password" && <Link className={`${styles.link} ${styles.noticeLink}`} href={authHref("forgot-password")}>{s.tr("Yêu cầu liên kết mới", "Request a new link")}</Link>}
      </div></div> : needsAccount && !successText ? <div className={styles.notice}><ShieldCheck size={20} aria-hidden /><div>
        <p>{s.tr("Đăng nhập trước khi đổi mật khẩu.", "Sign in before changing your password.")}</p><Link className={`${styles.link} ${styles.noticeLink}`} href={authHref("login")}>{s.tr("Đăng nhập", "Sign in")}</Link>
      </div></div> : successText ? <div className={styles.notice} role="status"><CheckCircle2 size={22} aria-hidden /><div>
        <p>{s.tr(...successText)}</p>
        <Link className={`${styles.link} ${styles.noticeLink}`} href={success === "verify-email" && s.actor ? destination : authHref("login")}>{s.tr("Tiếp tục", "Continue")} →</Link>
      </div></div> : <form className={styles.form} onSubmit={submit} noValidate aria-busy={busy}>
        {(Object.keys(errors).length > 0 || failure) && <div className={styles.summary} ref={errorSummary} role="alert" tabIndex={-1} aria-labelledby="auth-error-title">
          <h2 id="auth-error-title">{failure ? errorMessage(failure) : s.tr("Kiểm tra lại thông tin bên dưới.", "Please check the details below.")}</h2>
          {Object.keys(errors).length > 0 && <ul>{Object.entries(errors).map(([key]) => <li key={key}><a href={`#auth-${key}`} onClick={event => { event.preventDefault(); document.getElementById(`auth-${key}`)?.focus(); }}>{label(key)}: {fieldError(key)}</a></li>)}</ul>}
          {(failure as Error & { code?: string } | null)?.code === "PASSWORD_RESET_REQUIRED" && <Link className={styles.link} href={authHref("forgot-password")}>{s.tr("Đặt lại mật khẩu", "Reset password")}</Link>}
        </div>}
        {mode === "register" && textField("name", { autoComplete: "name", maxLength: 100 })}
        {mode === "register" ? <div className={styles.grid}>{textField("email", { type: "email", autoComplete: "email", maxLength: 254, spellCheck: false, autoCapitalize: "none" })}{textField("phone", { type: "tel", autoComplete: "tel", inputMode: "tel" })}</div>
          : ["login", "forgot-password"].includes(mode) && textField("email", { type: "email", autoComplete: "email", maxLength: 254, spellCheck: false, autoCapitalize: "none" })}
        {mode === "change-password" && <PasswordField id="auth-currentPassword" name="currentPassword" label={label("currentPassword")} autoComplete="current-password" value={values.currentPassword} onChange={event => update("currentPassword", event.target.value)} error={fieldError("currentPassword")} disabled={busy} required />}
        {!["forgot-password", "verify-email"].includes(mode) && <PasswordField id="auth-password" name="password" label={newPassword && mode !== "register" ? s.tr("Mật khẩu mới", "New password") : label("password")} autoComplete={mode === "login" ? "current-password" : "new-password"} minLength={newPassword ? 6 : undefined} value={values.password} onChange={event => update("password", event.target.value)} error={fieldError("password")} hint={newPassword ? s.tr("Ít nhất 6 ký tự.", "At least 6 characters.") : undefined} disabled={busy} required />}
        {newPassword && <PasswordField id="auth-confirmPassword" name="confirmPassword" label={label("confirmPassword")} autoComplete="new-password" value={values.confirmPassword} onChange={event => update("confirmPassword", event.target.value)} error={fieldError("confirmPassword")} disabled={busy} required />}
        {mode === "login" && <div className={styles.optionRow}><label className={styles.checkbox}>
          <input type="checkbox" name="rememberMe" checked={rememberMe} onChange={event => setRememberMe(event.target.checked)} disabled={busy} />
          <span>{s.tr("Ghi nhớ đăng nhập", "Remember me")}<small>{s.tr("Trên thiết bị này, tối đa 8 giờ.", "On this device, for up to 8 hours.")}</small></span>
        </label><Link className={styles.link} href={authHref("forgot-password")}>{s.tr("Quên mật khẩu?", "Forgot password?")}</Link></div>}
        {mode === "register" && <div className={styles.addressOption}>
          <label className={styles.checkbox}><input type="checkbox" name="saveAddress" checked={values.saveAddress} disabled={busy} aria-controls="auth-address" aria-expanded={values.saveAddress} onChange={event => {
            const checked = event.target.checked;
            if (!checked && addressPanel.current?.contains(document.activeElement)) event.currentTarget.focus({ preventScroll: true });
            setValues(previous => ({ ...previous, saveAddress: checked, address: { ...previous.address, recipient: previous.address.recipient || previous.name, phone: previous.address.phone || previous.phone } }));
            setErrors(previous => Object.fromEntries(Object.entries(previous).filter(([key]) => !key.startsWith("address-"))));
          }} /><span>{s.tr("Lưu địa chỉ giao hàng", "Save a delivery address")}<small>{s.tr("Không bắt buộc. Bạn có thể bổ sung khi thanh toán.", "Optional. You can add it at checkout.")}</small></span></label>
          <div className={styles.addressDisclosure} id="auth-address" ref={addressPanel} data-open={values.saveAddress} aria-hidden={!values.saveAddress} inert={!values.saveAddress}>
            <div className={styles.addressClip}><div className={styles.addressContent}>
              <fieldset className={styles.addressBox} disabled={busy || !values.saveAddress}><legend>{s.tr("Địa chỉ giao hàng", "Delivery address")}</legend>
            <div className={styles.grid}>{(["recipient", "phone", "province", "ward", "addressLine"] as const).map(key => {
              const field = `address-${key}`, error = fieldError(field);
              return <div className={styles.field} key={key}><label htmlFor={`auth-${field}`}>{label(field)}</label><input className={styles.input} id={`auth-${field}`} name={field} required type={key === "phone" ? "tel" : "text"} autoComplete={key === "recipient" ? "shipping name" : key === "phone" ? "shipping tel" : key === "province" ? "shipping address-level1" : key === "ward" ? "shipping address-level2" : "shipping address-line1"} value={values.address[key]} onChange={event => updateAddress(key, event.target.value)} aria-invalid={!!error} aria-describedby={error ? `auth-${field}-error` : undefined} />{error && <small className={styles.fieldError} id={`auth-${field}-error`}>{error}</small>}</div>;
            })}<div className={styles.field}><label htmlFor="auth-country">{s.tr("Quốc gia", "Country")}</label><input className={styles.input} id="auth-country" value={s.tr("Việt Nam", "Vietnam")} readOnly /></div></div>
              </fieldset>
            </div></div>
          </div>
        </div>}
        <button className={styles.submit} type="submit" disabled={busy || mode === "change-password" && s.sessionLoading}>
          {busy ? <LoaderCircle size={18} className="spin" aria-hidden /> : mode === "verify-email" ? <Check size={18} aria-hidden /> : null}
          {busy ? s.tr("Đang xử lý…", "Please wait…") : mode === "login" ? s.tr("Đăng nhập", "Sign in") : mode === "register" ? s.tr("Tạo tài khoản", "Create account") : mode === "forgot-password" ? s.tr("Gửi liên kết đặt lại", "Send reset link") : mode === "verify-email" ? s.tr("Xác minh email", "Verify email") : s.tr("Lưu mật khẩu mới", "Save new password")}
          {!busy && <ArrowRight size={17} aria-hidden />}
        </button>
      </form>}
      <div className={styles.footer}>
        {mode === "login" ? <>{s.tr("Chưa có tài khoản?", "New to BotZone?")}<Link className={styles.link} href={authHref("register")}>{s.tr("Đăng ký ngay", "Create an account")}</Link></>
          : mode === "register" ? <>{s.tr("Đã có tài khoản?", "Already have an account?")}<Link className={styles.link} href={authHref("login")}>{s.tr("Đăng nhập", "Sign in")}</Link></>
          : <Link className={styles.link} href={authHref("login")}>{s.tr("Quay lại đăng nhập", "Back to sign in")}</Link>}
      </div>
    </section>
  </div>;
}
