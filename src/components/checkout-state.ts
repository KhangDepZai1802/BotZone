import type { Address, PaymentMethod } from "@/lib/contracts";

export const emptyAddress = (): Address => ({ recipient: "", phone: "", province: "", ward: "", addressLine: "", country: "VN" });
export const checkoutDraftKey = (userId: string) => `botzone.checkout.${userId}`;
export const checkoutRequestKey = (userId: string) => `botzone.checkout-request.${userId}`;
export type CheckoutDraft = { address: Address; method: PaymentMethod; points: string; credit: string; saveAddress: boolean };

export function readCheckoutDraft(raw: string | null): CheckoutDraft | null {
  try {
    const value = JSON.parse(raw ?? "null");
    if (!value || !value.address || !Object.keys(emptyAddress()).every(key => typeof value.address[key] === "string") || value.address.country !== "VN") return null;
    if (!["cod", "banking", "momo", "visa"].includes(value.method) || typeof value.points !== "string" || typeof value.credit !== "string" || !/^\d*$/.test(value.points) || !/^\d*$/.test(value.credit) || typeof value.saveAddress !== "boolean") return null;
    return { address: Object.fromEntries(Object.keys(emptyAddress()).map(key => [key, value.address[key]])) as Address, method: value.method, points: value.points, credit: value.credit, saveAddress: value.saveAddress };
  } catch { return null; }
}

export function savedAddress(value: Record<string, unknown>): Address {
  const text = (key: string) => typeof value[key] === "string" ? value[key] as string : "";
  return { recipient: text("recipient"), phone: text("phone"), province: text("province"), ward: text("ward"), addressLine: text("addressLine") || text("address_line"), country: "VN" };
}

export function checkoutLoginHref(locale: string) {
  return `/${locale}/login?next=${encodeURIComponent(`/${locale}/checkout`)}`;
}

// A reload after a lost response must retry the same command, including its key.
export function pendingCheckout(raw: string | null, input: unknown): { key: string; input: unknown } | null {
  try {
    const value = JSON.parse(raw ?? "null");
    return value && typeof value.key === "string" && /^[\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12}$/i.test(value.key) && JSON.stringify(value.input) === JSON.stringify(input) ? value : null;
  } catch { return null; }
}
