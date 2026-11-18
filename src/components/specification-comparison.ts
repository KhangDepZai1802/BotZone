import type { Product } from "@/lib/contracts";
export { compareSpecification } from "./specification-engine";
import { compareSpecification } from "./specification-engine";

export function advantageIndices(fieldId: string, values: string[]): number[] {
  return compareSpecification(fieldId, values, "en").cells.flatMap((cell, index) => cell.advantage ? [index] : []);
}

export function storageFromVariant(product: Product): string[] {
  return [...new Set(product.variants.filter(variant => variant.active).map(variant => variant.options.storage || variant.options.ssd).filter((value): value is string => !!value))];
}
