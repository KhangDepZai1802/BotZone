import type { Address, Locale } from "@/lib/contracts";

export const authModes = ["login", "register", "forgot-password", "reset-password", "verify-email", "change-password"] as const;
export type AuthMode = typeof authModes[number];
export type AuthValues = { name: string; email: string; phone: string; password: string; confirmPassword: string; currentPassword: string; saveAddress: boolean; address: Address };
export type AuthFieldError = "required" | "name" | "email" | "phone" | "password" | "confirm" | "address";
export type AuthErrors = Record<string, AuthFieldError>;

type LocaleDraft = Pick<AuthValues, "name" | "email" | "phone" | "saveAddress" | "address"> & { rememberMe: boolean };
let localeDraft: { mode: AuthMode; locale: Locale; createdAt: number; fields: LocaleDraft } | undefined;
// A one-navigation handoff between remounted locale routes, held only in this JS document.
// Passwords, confirmation/current passwords and email-link tokens never enter this cache.
export function prepareAuthLocaleDraft(mode: AuthMode, locale: Locale, values: AuthValues, rememberMe: boolean) {
  localeDraft = { mode, locale, createdAt: Date.now(), fields: {
    name: values.name, email: values.email, phone: values.phone, saveAddress: values.saveAddress,
    address: { ...values.address }, rememberMe,
  } };
}
export function readAuthLocaleDraft(mode: AuthMode, locale: Locale): LocaleDraft | undefined {
  return localeDraft?.mode === mode && localeDraft.locale === locale && Date.now() - localeDraft.createdAt < 30000 ? localeDraft.fields : undefined;
}
export function clearAuthLocaleDraft() { localeDraft = undefined; }

export function safeAuthDestination(value: string | null, locale: Locale): string {
  const fallback = `/${locale}/account`;
  if (value === "checkout") return `/${locale}/checkout`;
  if (!value || !value.startsWith("/") || value.startsWith("//") || /[\\\u0000-\u001f\u007f]|%(?:2f|5c|00|0a|0d)/i.test(value)) return fallback;
  try {
    const url = new URL(value, "https://botzone.local");
    if (url.origin !== "https://botzone.local" || !/^\/(vi|en)(?:\/|$)/.test(url.pathname)) return fallback;
    const path = url.pathname.replace(/^\/(vi|en)/, "");
    if (/^\/(?:login|register|forgot-password|reset-password|verify-email|change-password|api)(?:\/|$)/.test(path)) return fallback;
    return `/${locale}${path}${url.search}${url.hash}`;
  } catch { return fallback; }
}
export function validPhone(value: string) { return value.trim().length <= 30 && /^\+?\d{9,15}$/.test(value.trim().replace(/[\s().-]/g, "")); }
export function validNewPassword(value: string) {
  return value.length >= 6 && value.trim().length > 0;
}
export function validateAuth(mode: AuthMode, values: AuthValues): AuthErrors {
  const errors: AuthErrors = {};
  if (["login", "register", "forgot-password"].includes(mode) && (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email.trim()) || values.email.trim().length > 254)) errors.email = "email";
  if (mode === "register") {
    if (values.name.trim().length < 2 || values.name.trim().length > 100) errors.name = "name";
    if (!validPhone(values.phone)) errors.phone = "phone";
    if (values.saveAddress) for (const key of ["recipient", "phone", "province", "ward", "addressLine"] as const) {
      if (!values.address[key].trim()) errors[`address-${key}`] = "required";
      else if (key === "phone" && !validPhone(values.address.phone)) errors["address-phone"] = "phone";
      else if (key !== "phone" && (values.address[key].trim().length < 2 || values.address[key].trim().length > 300)) errors[`address-${key}`] = "address";
    }
  }
  if (mode === "login" && !values.password) errors.password = "required";
  if (["register", "reset-password", "change-password"].includes(mode)) {
    if (!validNewPassword(values.password)) errors.password = "password";
    if (!values.confirmPassword || values.password !== values.confirmPassword) errors.confirmPassword = "confirm";
  }
  if (mode === "change-password" && !values.currentPassword) errors.currentPassword = "required";
  return errors;
}
