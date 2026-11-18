import assert from "node:assert/strict";
import test from "node:test";
import { clearAuthLocaleDraft, prepareAuthLocaleDraft, readAuthLocaleDraft, safeAuthDestination, validateAuth, validNewPassword, validPhone, type AuthValues } from "../../src/components/auth-helpers";

const registration = (): AuthValues => ({name:"Demo Customer", email:"customer@example.test", phone:"0901234567", password:"BotZoneDemo!2026", confirmPassword:"BotZoneDemo!2026", currentPassword:"", saveAddress:false,
  address:{recipient:"",phone:"",province:"",ward:"",addressLine:"",country:"VN"}});

test("locale draft transfers only non-secret fields to the same auth mode and intended locale", () => {
  const values = registration(); values.currentPassword="CurrentSecret!2026";
  prepareAuthLocaleDraft("register", "vi", values, true);
  const draft = readAuthLocaleDraft("register", "vi");
  assert.equal(draft?.email, values.email); assert.equal(draft?.rememberMe, true);
  assert.equal(readAuthLocaleDraft("login", "vi"), undefined);
  assert.equal(readAuthLocaleDraft("register", "en"), undefined);
  assert.equal(JSON.stringify(draft).includes(values.password), false);
  assert.equal(JSON.stringify(draft).includes(values.currentPassword), false);
  assert.equal(Object.hasOwn(draft!, "confirmPassword"), false);
  clearAuthLocaleDraft(); assert.equal(readAuthLocaleDraft("register", "vi"), undefined);
});

test("auth return destinations remain within localized storefront routes", () => {
  assert.equal(safeAuthDestination("checkout", "vi"), "/vi/checkout");
  assert.equal(safeAuthDestination("/vi/checkout?step=review#address", "en"), "/en/checkout?step=review#address");
  for (const value of [null,"https://evil.test","//evil.test","/\\evil.test","javascript:alert(1)","/vi/%2f%2fevil.test","/vi/%5cevil.test","/vi/../../api/commands","/api/commands","/vi/login","/en/reset-password?token=secret","/vi/\ncheckout"]) {
    assert.equal(safeAuthDestination(value, "en"), "/en/account", String(value));
  }
});
test("register requires customer identity and a matching password without requiring an address", () => {
  assert.deepEqual(validateAuth("register", registration()), {});
  assert.equal(validateAuth("register", {...registration(),phone:""}).phone, "phone");
  assert.equal(validateAuth("register", {...registration(),name:"A"}).name, "name");
  assert.equal(validateAuth("register", {...registration(),email:"not-an-email"}).email, "email");
  assert.equal(validateAuth("register", {...registration(),confirmPassword:"OtherDemo!2026"}).confirmPassword, "confirm");
  assert.equal(validateAuth("register", {...registration(),password:"weak"}).password, "password");
});
test("optional delivery address becomes complete and valid only when saving it", () => {
  const values = {...registration(),saveAddress:true};
  assert.deepEqual(Object.keys(validateAuth("register", values)).sort(), ["address-addressLine","address-phone","address-province","address-recipient","address-ward"]);
  values.address={recipient:"Demo Customer",phone:"+84 901 234 567",province:"HCM",ward:"Ward 1",addressLine:"12 Test Road",country:"VN"};
  assert.deepEqual(validateAuth("register", values), {});
  values.address.phone="123";
  assert.equal(validateAuth("register", values)["address-phone"], "phone");
});
test("password recovery preserves login compatibility and validates every new-password flow", () => {
  assert.deepEqual(validateAuth("login", {...registration(),password:"legacy"}), {});
  assert.equal(validateAuth("login", {...registration(),password:""}).password, "required");
  assert.deepEqual(validateAuth("forgot-password", {...registration(),password:""}), {});
  assert.deepEqual(validateAuth("verify-email", {...registration(),email:"",password:""}), {});
  assert.equal(validateAuth("change-password", registration()).currentPassword, "required");
  assert.deepEqual(validateAuth("reset-password", registration()), {});
  for (const password of ["123456", "aaaaaa", "ABCDEF", "!!!!!!", "tiếngViệt", "lowercase123!", "UpperLowerOnly!", "UpperLower1234"]) {
    assert.equal(validNewPassword(password), true);
    for (const mode of ["register", "reset-password", "change-password"] as const) {
      assert.deepEqual(validateAuth(mode, {...registration(), password, confirmPassword: password, currentPassword: "existing"}), {});
    }
  }
  for (const password of ["", "12345", "aA1!", "      "]) assert.equal(validNewPassword(password), false);
  assert.equal(validNewPassword("BotZoneDemo!2026"), true);
  assert.equal(validPhone("090 123 4567"), true);
  assert.equal(validPhone("+84901234567"), true);
  assert.equal(validPhone("12345"), false);
});
