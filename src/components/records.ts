export type Row = Record<string, unknown>;
export const row = (value: unknown): Row =>
  value && typeof value === "object" && !Array.isArray(value)
    ? (value as Row)
    : {};
export const rows = (value: unknown): Row[] =>
  Array.isArray(value) ? value.map(row) : [];
export function str(value: Row | undefined, ...keys: string[]): string {
  for (const key of keys) {
    const v = value?.[key];
    if (v !== null && v !== undefined) return String(v);
  }
  return "";
}
export function date(value: string, locale = "vi") {
  if (!value) return "—";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return new Intl.DateTimeFormat(locale === "vi" ? "vi-VN" : "en-GB", {
    timeZone: "Asia/Ho_Chi_Minh",
    dateStyle: "medium",
    timeStyle: "short",
  }).format(d);
}
export function moneyValue(value: Row | undefined, ...keys: string[]): string {
  return str(value, ...keys) || "0";
}
