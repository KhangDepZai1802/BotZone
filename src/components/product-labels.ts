import type { Product, Variant } from "@/lib/contracts";

export function variantLabel(variant?: Variant) {
  return variant ? Object.values(variant.optionLabels ?? variant.options).filter(Boolean).join(" · ") : "";
}

export function categoryLabel(product: Product, categories: { code: string; nameVi: string; nameEn: string }[], locale: "vi" | "en") {
  const category = categories.find(item => item.code.toLowerCase() === product.category.toLowerCase());
  return category ? locale === "vi" ? category.nameVi : category.nameEn : "Apple";
}

export function startingPrice(product: Product) {
  const prices = product.variants.filter(v => v.active).map(v => BigInt(v.priceVnd));
  return prices.length ? prices.reduce((a, b) => a < b ? a : b).toString() : null;
}
