import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync, mkdirSync, writeFileSync } from "node:fs";
import { compareSpecification } from "../../src/components/specification-comparison";

type Locale = "vi" | "en";
type Comparison = { cells: { advantage: boolean; summary?: string; detail?: string }[]; note?: string };
type Specification = { modelCode: string; groups: { items: { id: string; value: Record<Locale, string> }[] }[] };
const specifications: Specification[] = JSON.parse(readFileSync(new URL("../../data/product-specifications.json", import.meta.url), "utf8"));
const locales: Locale[] = ["vi", "en"];
const viCharacters = /[đĐăĂơƠưƯ\u1ea0-\u1ef9]/u;

function value(modelCode: string, fieldId: string, locale: Locale) {
  const field = specifications.find(model => model.modelCode === modelCode)?.groups.flatMap(group => group.items).find(item => item.id === fieldId);
  assert.ok(field, `Official fixture ${modelCode}.${fieldId}`);
  return field.value[locale];
}
function winners(result: Comparison) { return result.cells.flatMap((cell, index) => cell.advantage ? [index] : []); }
function checked(fieldId: string, values: string[], locale: Locale = "en"): Comparison {
  const unchanged = [...values], result = compareSpecification(fieldId, values, locale);
  assert.deepEqual(values, unchanged, "Comparison must not rewrite original specification strings");
  assert.equal(result.cells.length, values.length, "Every selected product keeps its own output cell");
  for (const cell of result.cells) {
    assert.equal(typeof cell.advantage, "boolean");
    for (const explanation of [cell.summary, cell.detail]) if (explanation !== undefined) assert.ok(typeof explanation === "string" && explanation.trim().length > 0);
  }
  if (result.note !== undefined) assert.ok(typeof result.note === "string" && result.note.trim().length > 0);
  assert.ok(!result.cells.length || !result.cells.every(cell => cell.advantage), "An entire tied row must never be highlighted");
  return result;
}
function expectWinners(fieldId: string, values: string[], expected: number[], locale: Locale = "en") {
  const result = checked(fieldId, values, locale);
  assert.deepEqual(winners(result), expected, `${fieldId}/${locale}: ${JSON.stringify(values)}`);
  return result;
}

test("actual iPad CPU: Air eight cores loses to Pro nine/ten, with localized configuration explanation", () => {
  for (const locale of locales) {
    const result = expectWinners("cpu", [value("IPA4", "cpu", locale), value("IPP5", "cpu", locale)], [1], locale);
    const winner = result.cells[1];
    assert.match(winner.summary ?? "", /CPU/u);
    assert.ok(winner.detail?.trim(), "Conditional CPU advantage explains what is being compared");
    assert.equal(viCharacters.test([winner.summary, winner.detail, result.note].filter(Boolean).join(" ")), locale === "vi");
    assert.doesNotMatch(winner.summary ?? "", /faster|better performance|nhanh hơn|mạnh hơn/iu);
  }
});

test("CPU storage conditions and role counts cannot masquerade as core counts", () => {
  expectWinners("cpu", ["8 cores with 512 GB SSD", "10 cores with 256 GB SSD"], [1]);
  expectWinners("cpu", ["8 lõi ở 512 GB SSD", "10 lõi ở 256 GB SSD"], [1], "vi");
  expectWinners("cpu", ["8 cores: 3 performance + 5 efficiency", "8 cores: 4 performance + 4 efficiency"], []);
  expectWinners("cpu", ["8 cores", "512 GB SSD"], []);
  expectWinners("cpu", ["8 cores", "256 GB / 1 TB"], []);
});

test("core ranges use strict dominance; overlap, touching bounds and identical options are not universal advantages", () => {
  const pro = value("IPP5", "cpu", "en");
  expectWinners("cpu", ["8 cores", pro, pro], [1, 2]);
  expectWinners("cpu", [pro, pro, pro], []);
  expectWinners("cpu", ["9 cores", pro], []);
  expectWinners("cpu", ["8–10 cores", "9–12 cores"], []);
  expectWinners("cpu", ["8–10 cores", "9–12 cores", "6 cores"], []);
  expectWinners("cpu", ["8–10 cores", "11–12 cores"], [1]);
  expectWinners("gpu", ["9 cores", "8 or 10 cores, configuration dependent"], []);
});

test("actual iPad GPUs: ten beats both nine-core Air models; unrelated feature counts do not change equal GPUs", () => {
  for (const locale of locales) {
    expectWinners("gpu", [value("IPA3", "gpu", locale), value("IPA4", "gpu", locale), value("IPP5", "gpu", locale)], [2], locale);
    expectWinners("gpu", [value("IPP5", "gpu", locale), value("VISION5", "gpu", locale)], [], locale);
  }
});

test("storage and memory compare declared maxima explicitly, keeping SSD conditions separate from RAM", () => {
  for (const locale of locales) {
    const result = expectWinners("memory_options", [value("IPA4", "memory_options", locale), value("IPP5", "memory_options", locale)], [1], locale);
    assert.match([result.cells[1].summary, result.cells[1].detail, result.note].filter(Boolean).join(" "), /tối đa|maximum|\bmax\b|up to/iu);
    expectWinners("storage_options", [value("IPA4", "storage_options", locale), value("IPP5", "storage_options", locale)], [1], locale);
  }
  expectWinners("memory_options", ["32 GB unified", "12 GB with 256/512 GB; 16 GB with 1/2 TB"], [0]);
  expectWinners("memory_options", ["16 / 24 / 32 GB unified", "32 GB unified"], []);
  expectWinners("storage", ["512 GB", "1 TB", "1000 GB"], [1, 2]);
});

test("weight prefers the entirely lighter range and normalizes decimal commas and kilograms", () => {
  for (const locale of locales) expectWinners("weight", [value("IPA4", "weight", locale), value("IPP5", "weight", locale)], [1], locale);
  expectWinners("weight", ["460 g", "Wi-Fi: 459 g; Cellular: 465 g"], []);
  expectWinners("weight", ["1.24 kg", "1260 g"], [0]);
  expectWinners("weight", ["1,24 kg", "1260 g"], [0], "vi");
  expectWinners("weight", ["444–446 g", "444–446 g", "464–465 g"], [0, 1]);
});

test("actual brightness compares only contexts shared by every selected product", () => {
  for (const locale of locales) {
    expectWinners("brightness", [value("IPA4", "brightness", locale), value("IPP5", "brightness", locale)], [1], locale);
    expectWinners("brightness", [value("P15", "brightness", locale), value("P17", "brightness", locale)], [1], locale);
    // P13 has no outdoor declaration: the common typical/HDR metrics tie P15 and P17.
    expectWinners("brightness", [value("P13", "brightness", locale), value("P15", "brightness", locale), value("P17", "brightness", locale)], [1, 2], locale);
  }
});

test("HDR peaks cannot outrank unmatched full-screen brightness; conflicting brightness metrics remain a tradeoff", () => {
  expectWinners("brightness", ["1000 nits full-screen", "1600 nits HDR peak"], []);
  expectWinners("brightness", ["1200 nits HDR peak", "800 nits typical; 1600 nits HDR peak"], [1]);
  const result = expectWinners("brightness", ["800 nits typical; 2000 nits HDR peak", "1000 nits typical; 1600 nits HDR peak"], []);
  assert.ok(result.note?.trim(), "A multidimensional brightness tradeoff needs an explanation");
  expectWinners("brightness", ["800 nits typical; 2000 nits HDR peak", "1000 nits typical; 1600 nits HDR peak", "600 nits typical; 1200 nits HDR peak"], []);
  expectWinners("brightness", ["800 nit tiêu chuẩn; 2000 nit đỉnh HDR", "1000 nit tiêu chuẩn; 1600 nit đỉnh HDR"], [], "vi");
});

test("resolution uses pixel count and density, not one side length, and does not flatten multiple displays", () => {
  for (const locale of locales) expectWinners("resolution", [value("IPA4", "resolution", locale), value("IPP5", "resolution", locale)], [1], locale);
  expectWinners("resolution", ["1920 × 1080; 400 ppi", "1080 × 1920; 400 ppi"], []);
  const result = expectWinners("resolution", ["3840 × 2160; 163 ppi", "2560 × 1600; 227 ppi"], []);
  assert.ok(result.note?.trim(), "More pixels but lower density is an explained tradeoff");
  expectWinners("resolution", ["1920 × 1080; 400 ppi", "2560 × 1440; 460 ppi", "2560 × 1440; 460 ppi"], [1, 2]);
  expectWinners("resolution", [value("DUO", "resolution", "en"), value("P18MAX", "resolution", "en")], []);
});

test("chip generation requires the same family, tier and auxiliary chip set", () => {
  expectWinners("chip", ["A15 Bionic", "A18"], [1]);
  expectWinners("chip", ["Apple M4", "Apple M5"], [1]);
  expectWinners("chip", ["Apple M4", "Apple M5", "Apple M5"], [1, 2]);
  expectWinners("chip", ["Apple M4 Pro", "Apple M5"], []);
  expectWinners("chip", ["A18", "A20 Pro"], []);
  expectWinners("chip", ["Apple M5", "A20"], []);
  expectWinners("chip", ["Apple M2 + R1", "Apple M5 + R1"], [1]);
  expectWinners("chip", ["Apple M2 + R2", "Apple M5 + R1"], []);
  for (const locale of locales) {
    const result = expectWinners("chip", [value("WATCH11", "chip", locale), value("WATCH12", "chip", locale)], [1], locale);
    assert.ok(result.cells[1].summary?.trim());
    assert.doesNotMatch(result.cells[1].summary ?? "", /faster|better performance|nhanh hơn|mạnh hơn/iu);
  }
});

test("unknowns, unsupported attributes and qualitative display technology do not produce invented winners", () => {
  for (const unknown of ["", "—", "unknown", "not disclosed"]) {
    expectWinners("cpu", ["8 cores", "10 cores", unknown], []);
    expectWinners("brightness", ["500 nits", unknown], []);
    expectWinners("resolution", ["2560 × 1440; 460 ppi", unknown], []);
  }
  expectWinners("panel", ["Liquid Retina IPS; P3", "Ultra Retina XDR tandem OLED; P3"], []);
  expectWinners("display_size", ["6.1 inches", "6.9 inches"], []);
  expectWinners("color", ["Silver", "Black"], []);
  expectWinners("cpu", [], []);
  expectWinners("cpu", ["10 cores"], []);
});

test("memory bandwidth compares GB/s ranges, not configuration memory sizes", () => {
  for (const locale of locales) {
    expectWinners("memory_bandwidth", [value("MBA4", "memory_bandwidth", locale), value("MBA5", "memory_bandwidth", locale)], [1], locale);
    expectWinners("memory_bandwidth", [value("MBA5", "memory_bandwidth", locale), value("MINI6", "memory_bandwidth", locale)], [], locale);
  }
  expectWinners("memory_bandwidth", ["120 GB/s with 32 GB", "153 GB/s with 16 GB"], [1]);
  expectWinners("memory_bandwidth", ["120 GB/s", "32 GB unified memory"], []);
});

test("port data rates normalize units without scoring connector names, counts or asymmetric port groups", () => {
  for (const locale of locales) {
    expectWinners("ports", [value("P15", "ports", locale), value("P18PRO", "ports", locale)], [1], locale);
    expectWinners("ports", [value("IPA4", "ports", locale), value("IPP5", "ports", locale)], [1], locale);
    expectWinners("ports", [value("STUDIO22", "ports", locale), value("STUDIO26", "ports", locale)], [], locale);
  }
  expectWinners("ports", ["USB-C; USB 2 up to 480 Mb/s", "USB-C; USB 3 up to 0.48 Gb/s"], []);
  expectWinners("ports", ["2 × Thunderbolt 4 up to 40 Gb/s", "4 × Thunderbolt 4 up to 40 Gb/s"], []);
  expectWinners("ports", ["USB-C", "Lightning"], []);
  expectWinners("ports", ["Front: 10 Gb/s; rear: 40 Gb/s", "USB 3 20 Gb/s"], []);
  expectWinners("data_rate", ["480 Mb/s", "10 Gb/s"], [1]);
});

test("dimensions compare an explicit common axis and describe depth rather than inventing a volume score", () => {
  for (const locale of locales) {
    expectWinners("dimensions", [value("IPA4", "dimensions", locale), value("IPP5", "dimensions", locale)], [1], locale);
    const tv = expectWinners("dimensions", [value("TVA19", "dimensions", locale), value("TV3", "dimensions", locale)], [1], locale);
    assert.match(tv.cells[1].summary ?? "", /depth|chiều sâu/u);
    expectWinners("dimensions", [value("DUO", "dimensions", locale), value("P18MAX", "dimensions", locale)], [], locale);
  }
  expectWinners("dimensions", ["150 × 70 × 8 mm (height × width × depth)", "300 × 200 × 7 mm (width × depth × height)"], []);
  expectWinners("dimensions", ["150 × 70 × 8 mm (height × width × depth)", "160 × 80 × 8 mm (height × width × depth)"], []);
  expectWinners("dimensions", ["150 × 70 × 8 mm", "160 × 80 × 7 mm"], []);
});

test("video playback compares matched playback and streaming scenarios, retaining tradeoffs", () => {
  for (const locale of locales) {
    expectWinners("video_playback", [value("P13", "video_playback", locale), value("P18MAX", "video_playback", locale)], [1], locale);
    expectWinners("video_playback", [value("DUO", "video_playback", locale), value("P18MAX", "video_playback", locale)], [], locale);
  }
  expectWinners("video_playback", ["Up to 20 hours; streamed 15 hours", "Up to 22 hours; streamed 12 hours"], []);
  expectWinners("video_playback", ["Up to 20 hours", "streamed 22 hours"], []);
  expectWinners("video_playback", ["Up to 20 hours; streamed 15 hours", "Up to 22 hours"], [1]);
});

test("battery life uses the actual localized workload instead of the largest hour count", () => {
  for (const locale of locales) {
    expectWinners("battery_life", [value("VISION2", "battery_life", locale), value("VISION5", "battery_life", locale)], [1], locale);
    expectWinners("battery_life", [value("AP2", "battery_life", locale), value("AP3", "battery_life", locale)], [1], locale);
    expectWinners("battery_life", [value("MBA5", "battery_life", locale), value("IPA4", "battery_life", locale)], [], locale);
  }
  expectWinners("battery_life", ["18 hours video streaming; 15 hours wireless web", "20 hours video streaming; 14 hours wireless web"], []);
  expectWinners("battery_life", ["24 hours normal use; 38 hours in Low Power Mode", "30 hours normal use; 36 hours in Low Power Mode"], []);
  expectWinners("battery_life", ["18 giờ video trực tuyến / 15 giờ web không dây", "20 giờ video trực tuyến / 16 giờ web không dây"], [1], "vi");
  expectWinners("battery_life", ["10 giờ web Wi-Fi / video; 9 giờ web trên bản Cellular", "11 giờ web Wi-Fi / video; 10 giờ web trên bản Cellular"], [1], "vi");
  expectWinners("battery_life", ["24 giờ thông thường; 38 giờ ở Chế độ nguồn điện thấp", "25 giờ thông thường; 39 giờ ở Chế độ nguồn điện thấp"], [1], "vi");
});

test("charging time matches target percentage; adapter wattage and post-charge listening are not speed", () => {
  for (const locale of locales) {
    expectWinners("wired_charging", [value("P13", "wired_charging", locale), value("P18PRO", "wired_charging", locale)], [1], locale);
    expectWinners("fast_charging", [value("WATCH11", "fast_charging", locale), value("WATCH12", "fast_charging", locale)], [], locale);
    expectWinners("fast_charging", [value("AP2", "fast_charging", locale), value("APMAX2", "fast_charging", locale)], [], locale);
  }
  expectWinners("wired_charging", ["50% in 30 minutes with a 20 W adapter", "50% in 30 minutes with a 60 W adapter"], []);
  expectWinners("wired_charging", ["50% in 30 minutes", "80% in 35 minutes"], []);
  expectWinners("wired_charging", ["Fast charging with 70 W adapter", "Fast charging with 100 W adapter"], []);
});

test("wireless charging keeps protocols separate and rejects passive case compatibility", () => {
  for (const locale of locales) {
    expectWinners("wireless_charging", [value("P13", "wireless_charging", locale), value("P16", "wireless_charging", locale)], [1], locale);
    expectWinners("wireless_charging", [value("P13", "wireless_charging", locale), value("CASE18MAX", "wireless_charging", locale)], [], locale);
  }
  expectWinners("wireless_charging", ["MagSafe up to 25 W", "Qi up to 15 W"], []);
  expectWinners("wireless_charging", ["MagSafe up to 15 W; Qi up to 10 W", "MagSafe up to 25 W; Qi up to 7.5 W"], []);
  expectWinners("wireless_charging", ["MagSafe 25 W and Qi 7.5 W", "MagSafe 15 W and Qi 10 W"], []);
});

test("Wi-Fi and Bluetooth compare declared standards without inferring throughput or undocumented versions", () => {
  for (const locale of locales) {
    expectWinners("wifi", [value("MBA4", "wifi", locale), value("MBA5", "wifi", locale)], [1], locale);
    expectWinners("bluetooth", [value("P13", "bluetooth", locale), value("P17", "bluetooth", locale)], [1], locale);
  }
  expectWinners("wifi", ["Wi-Fi 6 (802.11ax) 2×2 MIMO", "Wi-Fi 6E (802.11ax)"], [1]);
  expectWinners("wifi", ["802.11n", "Wi-Fi 6"], []);
  expectWinners("wifi", ["Wi-Fi 7; Apple N1", "Wi-Fi 7 (802.11be) 2×2 MIMO"], []);
  expectWinners("bluetooth", ["Supported", "6"], []);
  expectWinners("bluetooth", ["5.3", "5.3", "5.0"], [0, 1]);
});

test("feature support handles real Vietnamese yes/no and leading negation without treating unsupported as supported", () => {
  for (const locale of locales) for (const fieldId of ["pressure_sensitivity", "haptic_feedback", "find_my"]) {
    expectWinners(fieldId, [value("PENCILC", fieldId, locale), value("PENCILPRO", fieldId, locale)], [1], locale);
  }
  for (const fieldId of ["tilt_sensitivity", "hover"]) for (const locale of locales) {
    expectWinners(fieldId, [value("PENCILC", fieldId, locale), value("PENCILPRO", fieldId, locale)], [], locale);
  }
  expectWinners("pressure_sensitivity", ["Not supported", "Supported on compatible iPad"], [1]);
  expectWinners("pressure_sensitivity", ["Không hỗ trợ", "Hỗ trợ trên iPad tương thích"], [1], "vi");
  expectWinners("pressure_sensitivity", ["Not supported", "Not supported"], []);
  expectWinners("pressure_sensitivity", ["Supported", "Support status unknown"], []);
});

test("battery energy and accessory mass describe their own quantities without becoming runtime scores", () => {
  for (const locale of locales) {
    const result = expectWinners("battery_capacity", [value("IPA4", "battery_capacity", locale), value("IPP5", "battery_capacity", locale)], [1], locale);
    assert.doesNotMatch(result.cells[1].summary ?? "", /runtime|longer|lâu hơn/iu);
    expectWinners("case_weight", [value("AP2", "case_weight", locale), value("AP3", "case_weight", locale)], [1], locale);
  }
  expectWinners("weight", ["5.5 g each", "10 g whole set"], []);
  expectWinners("battery_capacity", ["28.93 Wh", "31.29 Wh"], [1]);
  expectWinners("battery_capacity", ["28.93 Wh", "5000 mAh"], []);
  expectWinners("neural_engine", ["16 cores", "16 cores", "8 cores"], [0, 1]);
});

test("official corpus pair/triple permutations preserve product identity, locale parity and tied-row neutrality", () => {
  const fields = ["cpu", "gpu", "chip", "memory_options", "storage_options", "refresh_rate", "brightness", "resolution", "weight", "memory_bandwidth", "ports", "dimensions", "video_playback", "battery_life", "wired_charging", "wireless_charging", "wifi", "bluetooth", "neural_engine", "data_rate", "case_weight", "battery_capacity", "fast_charging", "pressure_sensitivity", "tilt_sensitivity", "hover", "haptic_feedback", "find_my"];
  const permutations = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];
  const report: { passed: boolean; fields: Record<string, { uniqueDeclarations: number; pairs: number; triples: number; calls: number }>; lastCase?: unknown; error?: string } = { passed: false, fields: {} };
  try {
    for (const fieldId of fields) {
      const declarations = new Map<string, { modelCode: string; value: Record<Locale, string> }>();
      for (const model of specifications) {
        const field = model.groups.flatMap(group => group.items).find(item => item.id === fieldId);
        if (field) declarations.set(JSON.stringify(field.value), { modelCode: model.modelCode, value: field.value });
      }
      const entries = [...declarations.values()], counts = { uniqueDeclarations: entries.length, pairs: 0, triples: 0, calls: 0 }; report.fields[fieldId] = counts;
      for (const entry of entries) for (const locale of locales) {
        expectWinners(fieldId, [entry.value[locale], entry.value[locale], entry.value[locale]], [], locale); counts.calls++;
      }
      function verify(group: typeof entries) {
        const orderings = group.length === 2 ? [[0, 1], [1, 0]] : permutations;
        report.lastCase = { fieldId, models: group.map(entry => entry.modelCode) };
        const reference = winners(checked(fieldId, group.map(entry => entry.value.en), "en")); counts.calls++;
        for (const locale of locales) for (const order of orderings) {
          const actual = winners(checked(fieldId, order.map(index => group[index].value[locale]), locale)).map(index => order[index]).sort((a, b) => a - b); counts.calls++;
          assert.deepEqual(actual, reference, `Winner identity changed with locale/order: ${JSON.stringify(report.lastCase)} ${locale} ${order}`);
        }
      }
      for (let a = 0; a < entries.length; a++) for (let b = a + 1; b < entries.length; b++) {
        counts.pairs++; verify([entries[a], entries[b]]);
        for (let c = b + 1; c < entries.length; c++) { counts.triples++; verify([entries[a], entries[b], entries[c]]); }
      }
    }
    report.passed = true; delete report.lastCase;
  } catch (error) { report.error = (error as Error).message; throw error; }
  finally {
    if (process.env.BOTZONE_SPEC_CORPUS_REPORT === "1") {
      mkdirSync("artifacts/comparison", { recursive: true });
      writeFileSync("artifacts/comparison/detailed-corpus.json", JSON.stringify({ at: new Date().toISOString(), source: "data/product-specifications.json", coverage: "All distinct official declarations for supported numeric fields; all pairs/triples and all their index permutations in both locales.", ...report }, null, 2));
    }
  }
});
