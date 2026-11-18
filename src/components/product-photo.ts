import type { Product, ProductPhoto, Variant } from "@/lib/contracts";
import type { CSSProperties } from "react";

export function productPhoto(product: Product, variant?: Variant): ProductPhoto {
  const color = variant?.options.color;
  return product.photos?.find((photo) => color && photo.variantColors.includes(color))
    ?? product.photos?.find((photo) => photo.path === product.poster)
    ?? { path: product.poster, alt: product.name, variantColors: [] };
}

export function productPhotoStyle(photo: ProductPhoto): CSSProperties {
  const scale = photo.framingScale;
  return { "--photo-scale": typeof scale === "number" && Number.isFinite(scale) ? Math.min(3, Math.max(1, scale)) : 1 } as CSSProperties;
}
