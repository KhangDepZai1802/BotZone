"use client";
import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
} from "react";
import type { Actor, CartItem, Locale, Product } from "@/lib/contracts";
import { usePathname } from "next/navigation";
const requestLocale = (): Locale => typeof window !== "undefined" && /^\/en(?:\/|$)/.test(window.location.pathname) ? "en" : "vi";
const localized = (vi: string, en: string) => requestLocale() === "en" ? en : vi;
const connectionError = () => Object.assign(new Error(localized("Không kết nối được máy chủ. Vui lòng thử lại.", "Unable to connect. Please try again.")), { code: "NETWORK", status: 0 });
const responseError = (data: { error?: { message?: string; code?: string }; message?: string }, status: number) =>
  Object.assign(new Error(data.error?.message ?? data.message ?? localized("Không thể hoàn tất yêu cầu.", "Unable to complete the request.")), { code: data.error?.code, status });
let csrfToken: string | null = null;
let csrfRequest: Promise<string> | null = null;
export async function refreshCsrf() {
  csrfToken = null;
  csrfRequest = null;
  return getCsrf();
}
async function getCsrf(): Promise<string> {
  if (csrfToken) return csrfToken;
  if (!csrfRequest)
    csrfRequest = fetch("/api/auth/csrf", {
      headers: { "Accept-Language": requestLocale() },
      cache: "no-store",
      credentials: "same-origin",
    }).catch(() => { throw connectionError(); })
      .then(async (response) => {
        const data = await response.json();
        if (!response.ok || !data.token)
          throw responseError(data, response.status);
        csrfToken = data.token;
        return data.token;
      })
      .finally(() => {
        csrfRequest = null;
      });
  return csrfRequest;
}
export async function api<T>(
  url: string,
  body?: unknown,
  method?: string,
): Promise<T> {
  const verb = method ?? (body === undefined ? "GET" : "POST");
  const headers: Record<string, string> = { "Accept-Language": requestLocale() };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (!["GET", "HEAD", "OPTIONS"].includes(verb))
    headers["X-CSRF-TOKEN"] = await getCsrf();
  const res = await fetch(url, {
    method: verb,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    cache: "no-store",
    credentials: "same-origin",
  }).catch(() => { throw connectionError(); });
  const data = await res
    .json()
    .catch(() => ({ error: { message: `HTTP ${res.status}` } }));
  if (!res.ok) {
    if (res.status === 401) csrfToken = null;
    throw responseError(data, res.status);
  }
  if (
    [
      "/api/auth/login",
      "/api/auth/logout",
      "/api/auth/reset-password",
      "/api/auth/change-password",
    ].includes(url)
  )
    await refreshCsrf();
  return data;
}
export async function uploadEvidence(
  file: File,
  referenceType: "order" | "entitlement",
  referenceId: string,
): Promise<{ id: string; previewUrl: string }> {
  if (
    !["image/jpeg", "image/png", "image/webp"].includes(file.type) ||
    file.size > 5 * 1024 * 1024
  )
    throw new Error(
      localized("Chọn JPEG, PNG hoặc WebP tối đa 5 MB.", "Choose JPEG, PNG or WebP up to 5 MB."),
    );
  const form = new FormData();
  form.set("file", file);
  form.set("visibility", "private");
  form.set("referenceType", referenceType);
  form.set("referenceId", referenceId);
  const response = await fetch("/api/media", {
    method: "POST",
    body: form,
    headers: { "X-CSRF-TOKEN": await getCsrf(), "Accept-Language": requestLocale() },
    credentials: "same-origin",
    cache: "no-store",
  });
  const data = await response.json();
  if (!response.ok)
    throw responseError(data, response.status);
  return data;
}
export const command = <T = Record<string, unknown>,>(
  name: string,
  input: unknown,
  key = crypto.randomUUID(),
) => api<T>("/api/commands", { command: name, key, input });
export function useResource<T>(url: string | null) {
  const locale = usePathname()?.startsWith("/en") ? "en" : "vi";
  const [data, setData] = useState<T | null>(null),
    [error, setError] = useState(""),
    [loading, setLoading] = useState(!!url),
    generation = useRef(0);
  const refresh = useCallback(async () => {
    const request = ++generation.current;
    if (!url) {
      setData(null);
      setLoading(false);
      return;
    }
    setLoading(true);
    setError("");
    try {
      const result = await api<T>(url);
      if (request === generation.current) setData(result);
    } catch (e) {
      if (request === generation.current) setError((e as Error).message);
    } finally {
      if (request === generation.current) setLoading(false);
    }
  }, [url, locale]);
  useEffect(() => {
    void refresh();
    return () => {
      generation.current++;
    };
  }, [refresh]);
  return { data, error, loading, refresh, setData };
}
export type BagFeedback = { sequence: number; variantId: string; care: boolean; quantity: number; lineQuantity: number; totalQuantity: number };
export type CompareFeedback = { sequence: number; kind: "added" | "removed" | "limit" | "category" | "cleared"; productId?: string };
type Store = {
  locale: Locale;
  actor: Actor | null;
  sessionLoading: boolean;
  refreshSession: () => Promise<void>;
  products: Product[];
  categories: { code: string; nameVi: string; nameEn: string }[];
  catalogueError: string;
  catalogueLoading: boolean;
  refreshCatalogue: () => Promise<void>;
  currency: "VND" | "USD";
  setCurrency: (v: "VND" | "USD") => void;
  money: (v: string) => string;
  cart: CartItem[];
  setCart: (v: CartItem[]) => void;
  addCart: (v: CartItem) => Promise<boolean>;
  bagFeedback: BagFeedback | null;
  dismissBagFeedback: () => void;
  promotionCode: string;
  setPromotionCode: (v: string) => void;
  wishlist: string[];
  toggleWish: (id: string) => void;
  compare: string[];
  toggleCompare: (id: string) => void;
  clearCompare: () => void;
  compareFeedback: CompareFeedback | null;
  dismissCompareFeedback: () => void;
  notice: string;
  notify: (text: string) => void;
  tr: (vi: string, en: string) => string;
};
const StoreContext = createContext<Store | null>(null);
export function useOptionalStore() {
  return useContext(StoreContext);
}
export function useStore() {
  const v = useContext(StoreContext);
  if (!v) throw Error("StoreProvider missing");
  return v;
}
export function StoreProvider({
  locale,
  children,
}: {
  locale: Locale;
  children: React.ReactNode;
}) {
  const [actor, setActor] = useState<Actor | null>(null),
    [sessionLoading, setSessionLoading] = useState(true),
    [currency, setCurrencyState] = useState<"VND" | "USD">("VND"),
    [cart, setCartState] = useState<CartItem[]>([]),
    [promotionCode, setPromotionCode] = useState(""),
    [wishlist, setWishlist] = useState<string[]>([]),
    [compare, setCompare] = useState<string[]>([]),
    [bagFeedback, setBagFeedback] = useState<BagFeedback | null>(null),
    [compareFeedback, setCompareFeedback] = useState<CompareFeedback | null>(null),
    [notice, setNotice] = useState(""),
    [ready, setReady] = useState(false);
  const merged = useRef<string | null>(null);
  const cartRef = useRef<CartItem[]>([]), compareRef = useRef<string[]>([]);
  const cartQueue = useRef<Promise<unknown>>(Promise.resolve());
  const feedbackSequence = useRef(0);
  const catalogue = useResource<{
    products: Product[];
    categories: Store["categories"];
    exchangeRateVnd: string;
  }>(`/api/catalogue?locale=${locale}`);
  const notify = useCallback((text: string) => {
    setNotice(text);
  }, []);
  const refreshSession = useCallback(async () => {
    try {
      const d = await api<{ actor: Actor | null }>("/api/session");
      setActor(d.actor);
    } catch {
      setActor(null);
    } finally {
      setSessionLoading(false);
    }
  }, []);
  useEffect(() => {
    document.documentElement.lang = locale;
    setNotice(""); setBagFeedback(null); setCompareFeedback(null);
  }, [locale]);
  useEffect(() => {
    if (!actor) merged.current = null;
  }, [actor]);
  useEffect(() => {
    if (!ready || !actor) return;
    let live = true;
    api<{ wishlist?: string[] }>("/api/account")
      .then(async (account) => {
        if (!live) return;
        const ids = [...new Set([...(account.wishlist ?? []), ...wishlist])];
        setWishlist(ids);
        localStorage.setItem("botzone.wishlist", JSON.stringify(ids));
        await command("sync_wishlist", { productIds: ids });
      })
      .catch((e) => notify(e.message));
    return () => {
      live = false;
    };
  }, [actor?.id, ready]);
  useEffect(() => {
    void refreshSession();
    try {
      const storedCart = JSON.parse(localStorage.getItem("botzone.cart") ?? "[]");
      cartRef.current = Array.isArray(storedCart) ? storedCart : [];
      setCartState(cartRef.current);
      setWishlist(JSON.parse(localStorage.getItem("botzone.wishlist") ?? "[]"));
      const storedCompare = JSON.parse(localStorage.getItem("botzone.compare") ?? "[]");
      compareRef.current = Array.isArray(storedCompare) ? [...new Set(storedCompare.filter((id): id is string => typeof id === "string"))].slice(0, 3) : [];
      setCompare(compareRef.current);
      setCurrencyState(
        localStorage.getItem("botzone.currency") === "USD" ? "USD" : "VND",
      );
      setPromotionCode(localStorage.getItem("botzone.promotion") ?? "");
    } catch {}
    setReady(true);
  }, [refreshSession]);
  useEffect(() => {
    if (!ready || !catalogue.data) return;
    const known = compareRef.current.map(id => catalogue.data!.products.find(p => p.id === id)).filter((p): p is Product => !!p);
    const valid = known.filter(p => p.category === known[0]?.category).slice(0, 3).map(p => p.id);
    if (JSON.stringify(valid) !== JSON.stringify(compareRef.current)) {
      compareRef.current = valid; setCompare(valid);
    }
    localStorage.setItem("botzone.compare", JSON.stringify(valid));
  }, [ready, catalogue.data]);
  useEffect(() => {
    if (!notice) return;
    const t = setTimeout(() => setNotice(""), 6500);
    return () => clearTimeout(t);
  }, [notice]);
  useEffect(() => {
    if (!ready) return;
    localStorage.setItem("botzone.cart", JSON.stringify(cart));
    localStorage.setItem("botzone.promotion", promotionCode);
  }, [cart, promotionCode, ready]);
  useEffect(() => {
    if (!ready || !actor || merged.current === actor.id) return;
    merged.current = actor.id;
    api<{ items: CartItem[]; promotionCode: string }>("/api/cart")
      .then(async (remote) => {
        const all = new Map<string, CartItem>();
        for (const x of [...remote.items, ...cart]) {
          const k = `${x.variantId}:${x.care}`;
          const old = all.get(k);
          all.set(k, {
            ...x,
            quantity: Math.max(old?.quantity ?? 0, x.quantity),
          });
        }
        const items = [...all.values()];
        cartRef.current = items;
        setCartState(items);
        const code = promotionCode || remote.promotionCode || "";
        setPromotionCode(code);
        await command("save_cart", { items, promotionCode: code });
      })
      .catch((e) => notify(e.message));
  }, [actor, ready, cart, promotionCode, notify]);
  const setCart = (items: CartItem[]) => {
    cartRef.current = items;
    setCartState(items);
    if (actor)
      void command("save_cart", { items, promotionCode }).catch((e) =>
        notify(e.message),
      );
  };
  const addCart = (item: CartItem): Promise<boolean> => {
    const save = async () => {
      const current = cartRef.current;
      const old = current.find(x => x.variantId === item.variantId && x.care === item.care);
      const items = old ? current.map(x => x === old ? { ...x, quantity: x.quantity + item.quantity } : x) : [...current, item];
      try {
        // Serialize clicks so authenticated confirmation follows a successful save,
        // and rapid additions cannot overwrite a preceding quantity.
        if (actor) await command("save_cart", { items, promotionCode });
        cartRef.current = items; setCartState(items);
        setBagFeedback({ sequence: ++feedbackSequence.current, ...item, lineQuantity: (old?.quantity ?? 0) + item.quantity, totalQuantity: items.reduce((n, x) => n + x.quantity, 0) });
        return true;
      } catch (error) { notify((error as Error).message); return false; }
    };
    const pending = cartQueue.current.then(save, save);
    cartQueue.current = pending;
    return pending;
  };
  const setCurrency = (v: "VND" | "USD") => {
    setCurrencyState(v);
    localStorage.setItem("botzone.currency", v);
  };
  const toggleWish = (id: string) => {
    const v = wishlist.includes(id)
      ? wishlist.filter((x) => x !== id)
      : [...wishlist, id];
    setWishlist(v);
    localStorage.setItem("botzone.wishlist", JSON.stringify(v));
    if (actor)
      void command("sync_wishlist", { productIds: v }).catch((e) =>
        notify(e.message),
      );
  };
  const toggleCompare = (id: string) => {
    const current = compareRef.current;
    const feedback = (kind: CompareFeedback["kind"]) => setCompareFeedback({ sequence: ++feedbackSequence.current, kind, productId: id });
    const commit = (ids: string[]) => { compareRef.current = ids; setCompare(ids); localStorage.setItem("botzone.compare", JSON.stringify(ids)); };
    if (current.includes(id)) {
      commit(current.filter(x => x !== id)); feedback("removed");
      return;
    }
    if (current.length >= 3) { feedback("limit"); return; }
    const p = catalogue.data?.products.find((x) => x.id === id);
    if (!p) return;
    if (
      current.length &&
      catalogue.data?.products.find((x) => x.id === current[0])?.category !==
        p?.category
    ) {
      feedback("category"); return;
    }
    commit([...current, id]); feedback("added");
  };
  const clearCompare = () => {
    compareRef.current = []; setCompare([]); localStorage.setItem("botzone.compare", "[]");
    setCompareFeedback({ sequence: ++feedbackSequence.current, kind: "cleared" });
  };
  const money = (v: string) => {
    try {
      const n = BigInt(v);
      if (currency === "VND")
        return (
          new Intl.NumberFormat(locale === "vi" ? "vi-VN" : "en-US").format(n) +
          " ₫"
        );
      const rate = BigInt(catalogue.data?.exchangeRateVnd ?? "25000");
      const cents = (n * 100n + rate / 2n) / rate;
      return new Intl.NumberFormat(locale === "vi" ? "vi-VN" : "en-US", { style: "currency", currency: "USD" })
        .formatToParts(cents / 100n).map(part => part.type === "fraction" ? String(cents % 100n).padStart(2, "0") : part.value).join("");
    } catch {
      return "—";
    }
  };
  return (
    <StoreContext.Provider
      value={{
        locale,
        actor,
        sessionLoading,
        refreshSession,
        products:
          catalogue.data?.products.map((p) => ({
            ...p,
            category: p.category.toLowerCase(),
          })) ?? [],
        categories:
          catalogue.data?.categories.map((c) => ({
            ...c,
            code: c.code.toLowerCase(),
          })) ?? [],
        catalogueError: catalogue.error,
        catalogueLoading: catalogue.loading,
        refreshCatalogue: catalogue.refresh,
        currency,
        setCurrency,
        money,
        cart,
        setCart,
        addCart,
        bagFeedback,
        dismissBagFeedback: () => setBagFeedback(null),
        promotionCode,
        setPromotionCode,
        wishlist,
        toggleWish,
        compare,
        toggleCompare,
        clearCompare,
        compareFeedback,
        dismissCompareFeedback: () => setCompareFeedback(null),
        notice,
        notify,
        tr: (vi, en) => (locale === "vi" ? vi : en),
      }}
    >
      {children}
      {notice && (
        <div className="toast" role="status">
          {notice}
          <button aria-label={locale === "vi" ? "Đóng thông báo" : "Dismiss notification"} onClick={() => setNotice("")}>
            ×
          </button>
        </div>
      )}
    </StoreContext.Provider>
  );
}
