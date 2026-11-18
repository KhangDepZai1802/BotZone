import assert from "node:assert/strict";
import test from "node:test";
import { checkoutDraftKey, checkoutLoginHref, checkoutRequestKey, emptyAddress, pendingCheckout, readCheckoutDraft, savedAddress } from "../../src/components/checkout-state";

test("checkout drafts retain incomplete shipping fields and payment choices after a reload", () => {
  const draft = { address: { ...emptyAddress(), recipient: "Khang", addressLine: "121 Trần Bình Trọng" }, method: "momo", points: "", credit: "", saveAddress: false };
  assert.deepEqual(readCheckoutDraft(JSON.stringify(draft)), draft);
  assert.notEqual(checkoutDraftKey("customer-a"), checkoutDraftKey("customer-b"));
  assert.notEqual(checkoutRequestKey("customer-a"), checkoutDraftKey("customer-a"));
  for (const value of ["broken", "null", "{}", JSON.stringify({ ...draft, method: "cash" }), JSON.stringify({ ...draft, address: { ...draft.address, country: "US" } }), JSON.stringify({ ...draft, credit: -1 })]) {
    assert.equal(readCheckoutDraft(value), null);
  }
});

test("saved SQL addresses prefill the checkout contract without losing street information", () => {
  assert.deepEqual(savedAddress({ recipient: "Khang", phone: "0901234567", province: "TPHCM", ward: "Bến Thành", address_line: "121 Trần Bình Trọng", is_default: true }), { recipient: "Khang", phone: "0901234567", province: "TPHCM", ward: "Bến Thành", addressLine: "121 Trần Bình Trọng", country: "VN" });
});

test("guest checkout returns through login in the selected language", () => {
  assert.equal(checkoutLoginHref("vi"), "/vi/login?next=%2Fvi%2Fcheckout");
  assert.equal(checkoutLoginHref("en"), "/en/login?next=%2Fen%2Fcheckout");
});

test("a lost placement response reuses its idempotency key only for identical input", () => {
  const input = { items: [{ variantId: "sku", quantity: 1 }], quoteHash: "current", address: { ...emptyAddress(), recipient: "Khang" }, saveAddress: true };
  const request = { key: "6862ad61-f81d-46f2-b2dd-cab4d94d7850", input };
  const raw = JSON.stringify(request);
  assert.deepEqual(pendingCheckout(raw, input), request);
  assert.equal(pendingCheckout(raw, { ...input, quoteHash: "new-price" }), null);
  assert.equal(pendingCheckout(raw, { ...input, address: emptyAddress() }), null);
  assert.equal(pendingCheckout(raw, { ...input, saveAddress: false }), null);
  assert.equal(pendingCheckout("{}", input), null);
  assert.equal(pendingCheckout("broken", input), null);
});
