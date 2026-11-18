"use client";
import * as React from "react";
import { Slot } from "@radix-ui/react-slot";
import * as DialogPrimitive from "@radix-ui/react-dialog";
import { cva, type VariantProps } from "class-variance-authority";
import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";
import { AlertCircle, LoaderCircle, PackageOpen, X } from "lucide-react";
import { useOptionalStore } from "./client";
export { Select } from "./select";
export const cn = (...inputs: ClassValue[]) => twMerge(clsx(inputs));
const buttonVariants = cva("button", {
  variants: {
    variant: {
      default: "button-primary",
      secondary: "button-secondary",
      ghost: "button-ghost",
      outline: "button-outline",
      danger: "button-danger",
    },
    size: { default: "", sm: "button-sm", icon: "button-icon" },
  },
  defaultVariants: { variant: "default", size: "default" },
});
export function Button({
  className,
  variant,
  size,
  asChild = false,
  ...props
}: React.ComponentProps<"button"> &
  VariantProps<typeof buttonVariants> & { asChild?: boolean }) {
  const C = asChild ? Slot : "button";
  return (
    <C
      type={asChild ? undefined : "button"}
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  );
}
export function Field({
  label,
  hint,
  children,
}: React.PropsWithChildren<{ label: string; hint?: string }>) {
  const labelId = React.useId();
  const hintId = `${labelId}-hint`;
  return (
    <label className="field">
      <span id={labelId}>{label}</span>
      {React.Children.map(children, (child) =>
        React.isValidElement<Record<string, unknown>>(child)
          ? React.cloneElement(child, {
              "aria-labelledby": labelId,
              "aria-describedby": hint
                ? hintId
                : child.props["aria-describedby"],
            })
          : child,
      )}
      {hint && <small id={hintId}>{hint}</small>}
    </label>
  );
}
export function Input(props: React.InputHTMLAttributes<HTMLInputElement>) {
  return <input {...props} className={cn("input", props.className)} />;
}
export function Textarea(
  props: React.TextareaHTMLAttributes<HTMLTextAreaElement>,
) {
  return <textarea {...props} className={cn("input", props.className)} />;
}
export function Busy({ text }: { text?: string }) {
  const s = useOptionalStore();
  return (
    <div className="state" role="status">
      <LoaderCircle className="spin" aria-hidden />
      {text ?? s?.tr("Đang tải dữ liệu…", "Loading…") ?? "Loading…"}
    </div>
  );
}
export function ErrorState({
  message,
  retry,
}: {
  message: string;
  retry?: () => void;
}) {
  const s = useOptionalStore();
  return (
    <div className="notice error" role="alert">
      <AlertCircle aria-hidden />
      <div>
        <strong>{message}</strong>
        {retry && (
          <Button variant="outline" size="sm" onClick={retry}>
            {s?.tr("Thử lại", "Retry") ?? "Retry"}
          </Button>
        )}
      </div>
    </div>
  );
}
export function Empty({
  title,
  description,
  children,
}: React.PropsWithChildren<{ title: string; description?: string }>) {
  return (
    <div className="empty">
      <PackageOpen size={36} aria-hidden />
      <h3>{title}</h3>
      {description && <p>{description}</p>}
      {children}
    </div>
  );
}
export function Badge({
  children,
  tone = "neutral",
}: React.PropsWithChildren<{
  tone?: "neutral" | "success" | "warning" | "danger";
}>) {
  return <span className={`badge badge-${tone}`}>{children}</span>;
}
export function Modal({
  open,
  onOpenChange,
  title,
  children,
  description,
  className,
}: React.PropsWithChildren<{
  open: boolean;
  onOpenChange: (v: boolean) => void;
  title: string;
  description?: string;
  className?: string;
}>) {
  const returnFocus = React.useRef<HTMLElement | null>(null);
  const s = useOptionalStore();
  return (
    <DialogPrimitive.Root open={open} onOpenChange={onOpenChange}>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="modal-overlay" />
        <DialogPrimitive.Content className={`modal-content ${className ?? ""}`}
          {...(!description ? { "aria-describedby": undefined } : {})}
          onOpenAutoFocus={() => { returnFocus.current = document.activeElement as HTMLElement; }}
          onCloseAutoFocus={(event) => {
            if (returnFocus.current?.isConnected) {
              event.preventDefault();
              returnFocus.current.focus({ preventScroll: true });
            }
          }}
        >
          <div className="modal-heading">
            <div>
              <DialogPrimitive.Title>{title}</DialogPrimitive.Title>
              {description && (
                <DialogPrimitive.Description>
                  {description}
                </DialogPrimitive.Description>
              )}
            </div>
            <DialogPrimitive.Close asChild>
              <Button variant="ghost" size="icon" aria-label={s?.tr("Đóng", "Close") ?? "Close"}>
                <X />
              </Button>
            </DialogPrimitive.Close>
          </div>
          {children}
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  );
}
export function Money({
  value,
}: {
  value: string | number | bigint | undefined | null;
}) {
  const s = useOptionalStore();
  try {
    return <>{new Intl.NumberFormat(s?.locale === "en" ? "en-US" : "vi-VN").format(BigInt(value ?? 0))} ₫</>;
  } catch {
    return <>—</>;
  }
}
export function DataTable({
  rows,
  columns,
}: {
  rows: Record<string, unknown>[];
  columns?: string[];
}) {
  const s = useOptionalStore();
  const cols = columns ?? Object.keys(rows[0] ?? {});
  return rows.length ? (
    <div
      className="table-scroll"
      tabIndex={0}
      role="region"
      aria-label={s?.tr("Bảng dữ liệu", "Data table") ?? "Data table"}
    >
      <table>
        <thead>
          <tr>
            {cols.map((c) => (
              <th key={c}>{c}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((r, i) => (
            <tr key={String(r.id ?? i)}>
              {cols.map((c) => (
                <td key={c}>{renderCell(r[c], s?.locale)}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  ) : (
    <Empty title={s?.tr("Chưa có dữ liệu", "No records yet") ?? "No records yet"} />
  );
}
export function renderCell(value: unknown, locale = "vi"): React.ReactNode {
  if (value === null || value === undefined) return "—";
  if (typeof value === "boolean") return locale === "en" ? (value ? "Yes" : "No") : (value ? "Có" : "Không");
  if (typeof value === "object")
    return <span className="cell-json">{JSON.stringify(value)}</span>;
  return String(value);
}
