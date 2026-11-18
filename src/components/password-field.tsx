"use client";
import { useId, useState, type InputHTMLAttributes } from "react";
import { Eye, EyeOff } from "lucide-react";
import { useStore } from "./client";
import styles from "./auth-page.module.css";

type Props = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & { label: string; hint?: string; error?: string };
export function PasswordField({ label, hint, error, id, ...props }: Props) {
  const s = useStore(), generatedId = useId(), inputId = id ?? generatedId;
  const [visible, setVisible] = useState(false);
  const description = [hint ? `${inputId}-hint` : "", error ? `${inputId}-error` : ""].filter(Boolean).join(" ") || undefined;
  return <div className={styles.field}>
    <label htmlFor={inputId}>{label}</label>
    <div className={styles.passwordWrap}>
      <input {...props} id={inputId} className={styles.input} type={visible ? "text" : "password"} aria-invalid={!!error} aria-describedby={description} />
      <button className={styles.passwordToggle} type="button" aria-controls={inputId} aria-pressed={visible}
        aria-label={visible ? s.tr(`Ẩn ${label.toLowerCase()}`, `Hide ${label.toLowerCase()}`) : s.tr(`Hiện ${label.toLowerCase()}`, `Show ${label.toLowerCase()}`)}
        disabled={props.disabled} onClick={() => setVisible(value => !value)}>
        {visible ? <EyeOff size={19} aria-hidden /> : <Eye size={19} aria-hidden />}
      </button>
    </div>
    {hint && <small id={`${inputId}-hint`} className={styles.hint}>{hint}</small>}
    {error && <small id={`${inputId}-error`} className={styles.fieldError}>{error}</small>}
  </div>;
}
