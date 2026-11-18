import test from "node:test";
import assert from "node:assert/strict";
import { advantageIndices, storageFromVariant } from "../../src/components/specification-comparison";
import type { Product } from "../../src/lib/contracts";

test("capacity normalizes units and shared-unit lists; generation is distinct from subjective display size", () => {
  assert.deepEqual(advantageIndices("storage", ["128GB", "256GB"]), [1]);
  assert.deepEqual(advantageIndices("storage_options", ["128 / 256 / 512 GB", "256 / 512 GB / 1 / 2 TB"]), [1]);
  assert.deepEqual(advantageIndices("storage_options", ["512 GB / 1 / 2 / 4 TB SSD", "256 / 512 GB / 1 / 2 TB SSD (M6)"]), [0]);
  assert.deepEqual(advantageIndices("storage", ["1,5 TB", "1000 GB"]), [0]);
  assert.deepEqual(advantageIndices("chip", ["A15", "A18"]), [1]);
  assert.deepEqual(advantageIndices("display_size", ["6.1 inches", "6.9 inches"]), []);
});

test("conditional memory does not confuse SSD capacity with RAM in either locale", () => {
  assert.deepEqual(advantageIndices("memory_options", ["12 GB unified", "12 GB with 256/512 GB; 16 GB with 1/2 TB"]), [1]);
  assert.deepEqual(advantageIndices("memory_options", ["32 GB thống nhất", "12 GB ở 256/512 GB; 16 GB ở 1/2 TB"]), [0]);
  assert.deepEqual(advantageIndices("memory_options", ["16 / 24 / 32 GB unified", "16 GB unified"]), [0]);
});

test("ties and missing values never create a fabricated sole winner", () => {
  assert.deepEqual(advantageIndices("storage", ["512GB", "512GB"]), []);
  assert.deepEqual(advantageIndices("storage", ["256GB", "512GB", "512GB"]), [1, 2]);
  assert.deepEqual(advantageIndices("storage", ["512GB", "—"]), []);
  assert.deepEqual(advantageIndices("storage", ["1TB", "unknown", "256GB"]), []);
  assert.deepEqual(advantageIndices("storage", ["512GB"]), []);
  assert.deepEqual(advantageIndices("storage", ["512GB", "120 GB/s"]), []);
  assert.deepEqual(advantageIndices("storage", ["512GB", "1Tb"]), []);
});

test("refresh-rate comparison uses declared maximum Hz and does not infer missing specs", () => {
  assert.deepEqual(advantageIndices("refresh_rate", ["90 / 96 / 100Hz", "90 / 96 / 100 / 120Hz"]), [1]);
  assert.deepEqual(advantageIndices("refresh_rate", ["60Hz", "Adaptive ProMotion 10–120Hz"]), [1]);
  assert.deepEqual(advantageIndices("refresh_rate", ["120 / 60Hz", "100Hz"]), [0]);
  assert.deepEqual(advantageIndices("refresh_rate", ["—", "Adaptive ProMotion up to 120Hz"]), []);
});

test("shop storage uses only active raw storage/SSD options, never model maxima or translated colors", () => {
  const product = { variants: [
    { active: true, options: { storage: "256GB", color: "White" } },
    { active: true, options: { storage: "256GB" } },
    { active: false, options: { storage: "2TB" } },
    { active: true, options: { ssd: "512GB", ram: "16GB" } },
  ] } as Product;
  assert.deepEqual(storageFromVariant(product), ["256GB", "512GB"]);
});
