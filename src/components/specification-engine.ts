type Locale = "vi" | "en";
type Text = [string, string];
type Range = { min: number; max: number };
type Reading = { axes: Record<string, Range>; signature?: string };
type Rule = { read: (value: string) => Reading | null; summary: Text; detail: Text; lower?: boolean; maximum?: boolean };
export type SpecificationComparison = { cells: { advantage: boolean; summary?: string; detail?: string }[]; note?: string };

const numberPattern = "\\d+(?:[.,]\\d+)?";
const listPattern = `(${numberPattern}(?:\\s*(?:/|–|-|or|hoac)\\s*${numberPattern})*)`;
const fold = (value: string) => value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/đ/g, "d").toLowerCase();
const number = (value: string) => Number(value.replace(",", "."));
const translated = (pair: Text, locale: Locale) => pair[locale === "vi" ? 0 : 1];
function range(values: number[]): Range | null {
  return values.length && values.every(value => Number.isFinite(value) && value >= 0) ? { min: Math.min(...values), max: Math.max(...values) } : null;
}
function quantities(value: string, unit: string): number[] {
  return [...value.matchAll(new RegExp(`${listPattern}\\s*(?:-\\s*)?(?:${unit})`, "gu"))]
    .flatMap(match => [...match[1].matchAll(new RegExp(numberPattern, "g"))].map(token => number(token[0])));
}
function reading(value: Range | null, key = "value", signature?: string): Reading | null {
  return value ? { axes: { [key]: value }, signature } : null;
}
function capacity(value: string): Range | null {
  return range([...value.matchAll(new RegExp(`${listPattern}\\s*(GB|TB)\\b(?!/s)`, "g"))]
    .flatMap(match => [...match[1].matchAll(new RegExp(numberPattern, "g"))].map(token => number(token[0]) * (match[2] === "TB" ? 1000 : 1))));
}
function cores(value: string): Reading | null { return reading(range(quantities(fold(value), "(?:cores?|loi)\\b"))); }
function brightness(value: string): Reading | null {
  const axes: Record<string, Range> = {};
  for (const clause of fold(value).split(";")) {
    const key = /hdr/.test(clause) ? "hdr" : /outdoor|ngoai troi/.test(clause) ? "outdoor" : "normal";
    const values = [...clause.matchAll(new RegExp(numberPattern, "g"))].map(match => number(match[0]));
    if (!values.length || !/nit|hdr|outdoor|ngoai troi/.test(clause)) continue;
    if (!/^\s*\d/.test(clause)) return null;
    const peak = Math.max(...values); axes[key] = { min: peak, max: peak };
  }
  return Object.keys(axes).length ? { axes } : null;
}
function resolution(value: string): Reading | null {
  const pairs = [...value.matchAll(/(\d+)\s*[×x]\s*(\d+)/g)];
  if (pairs.length > 1) return null; // Different screens/case sizes are not interchangeable.
  const millions = fold(value).match(/(\d+(?:[.,]\d+)?)\s*(?:million total pixels|trieu diem anh)/);
  const pixels = pairs.length ? Number(pairs[0][1]) * Number(pairs[0][2]) : millions ? number(millions[1]) * 1e6 : null;
  if (!pixels) return null;
  const axes: Record<string, Range> = { pixels: { min: pixels, max: pixels } }, ppi = value.match(/(\d+(?:[.,]\d+)?)\s*ppi/i);
  if (ppi) axes.density = { min: number(ppi[1]), max: number(ppi[1]) };
  return { axes };
}
function weight(value: string): Reading | null {
  const normalized = fold(value).split(/;\s*(?:separate battery|pin roi)/)[0];
  const amounts = [...normalized.matchAll(new RegExp(`${listPattern}\\s*(kg|g)\\b`, "g"))]
    .flatMap(match => [...match[1].matchAll(new RegExp(numberPattern, "g"))].map(token => number(token[0]) * (match[2] === "kg" ? 1000 : 1)));
  return reading(range(amounts), "mass", /each|moi ben/.test(normalized) ? "each" : "whole");
}
function thickness(value: string): Reading | null {
  const normalized = fold(value);
  if (/open:|folded:|mo:|gap:|diameter|duong kinh|stand|chan|earbud|moi ben/.test(normalized)) return null;
  const depth = /height.*width.*depth|cao.*rong.*(?:day|sau)/.test(normalized);
  const height = /width.*depth.*height|rong.*sau.*cao/.test(normalized);
  if (!depth && !height) return null;
  const matches = [...normalized.matchAll(/(\d+(?:[.,]\d+)?)\s*[×x]\s*(\d+(?:[.,]\d+)?)\s*[×x]\s*(\d+(?:[.,]\d+)?)\s*mm/g)];
  return matches.length === 1 ? reading(range([number(matches[0][3])]), depth ? "depth" : "height", depth ? "depth" : "height") : null;
}
function chip(value: string): Reading | null {
  const normalized = value.trim().replace(/^Apple\s+/i, "");
  const match = normalized.match(/^([AMSH])(\d+)(?:\s+(Pro|Max|Ultra|Bionic))?(?=\s*(?:$|;|\+|in|trong|ở))/i);
  if (!match) return null;
  const auxiliary = normalized.match(/\+\s*([A-Z]+\d+)/i)?.[1]?.toUpperCase() ?? "";
  const tier = match[3] && match[3].toLowerCase() !== "bionic" ? match[3].toLowerCase() : "base";
  return reading(range([Number(match[2])]), "generation", `${match[1].toUpperCase()}:${tier}:${auxiliary}`);
}
function booleanFeature(value: string): Reading | null {
  const normalized = fold(value);
  if (/^(?:not supported|khong ho tro)\b/.test(normalized) || normalized === "khong") return reading(range([0]));
  if (/^(?:supported|ho tro)(?:\b|$)/.test(normalized) || /^co(?:$| tren\b| trong\b)/.test(normalized)) return reading(range([1]));
  return null;
}
function rate(value: string): Reading | null {
  const values = [...value.matchAll(/(\d+(?:[.,]\d+)?)\s*(Gb|Mb)\/s\b/g)].map(match => number(match[1]) * (match[2] === "Gb" ? 1000 : 1));
  if (values.length !== 1 || /upstream|downstream|\bvao\b|\bra\b/.test(fold(value))) return null;
  return reading(range(values));
}
function videoPlayback(value: string): Reading | null {
  const normalized = fold(value); if (/outer|inner|ngoai|trong/.test(normalized)) return null;
  const axes: Record<string, Range> = {};
  for (const clause of normalized.split(";")) {
    const amount = range(quantities(clause, "(?:hours?|gio)\\b"));
    if (amount && amount.min === amount.max) axes[/stream|truc tuyen/.test(clause) ? "streamed" : "playback"] = amount;
  }
  return Object.keys(axes).length ? { axes } : null;
}
function charging(value: string): Reading | null {
  const normalized = fold(value), target = normalized.match(/(\d+)\s*%/);
  const minutes = normalized.match(/(?:in|trong|sau|khoang)\s+(?:about\s+|khoang\s+)?(\d+(?:[.,]\d+)?)\s*(?:minutes?|phut)/);
  return target && minutes ? reading(range([number(minutes[1])]), "minutes", target[1] + "%") : null;
}
function wirelessCharging(value: string): Reading | null {
  // A case's charger compatibility is not a charging-power specification.
  if (/^(?:compatible|tuong thich|dung cung)/.test(fold(value))) return null;
  const axes: Record<string, Range> = {};
  for (const clause of value.split(";")) {
    const watts = range(quantities(clause, "W\\b")); if (!watts) continue;
    if (watts.min !== watts.max) return null;
    for (const protocol of ["MagSafe", "Qi2", "Qi"]) if (new RegExp(`\\b${protocol}\\b`, "i").test(clause)) axes[protocol] = { min: watts.max, max: watts.max };
  }
  return Object.keys(axes).length ? { axes } : null;
}
function batteryLife(value: string): Reading | null {
  const normalized = fold(value), axes: Record<string, Range> = {};
  const contexts: [string, RegExp][] = [
    ["video-streaming", /(?:hours?|gio)\s+(?:video streaming|(?:xem )?video truc tuyen)/],
    ["wireless-web", /(?:hours?|gio)\s+(?:wireless web|(?:duyet )?web khong day)/],
    ["wifi-web-video", /(?:hours?|gio)\s+(?:wi-fi web|web wi-fi|duyet web.*wi-fi|luot web.*wi-fi)/],
    ["cellular-web", /(?:hours?|gio)\s+(?:cellular web|web tren ban cellular|duyet web.*di dong|luot web.*di dong)/],
    ["normal", /(?:hours?|gio)\s+(?:normal use|(?:su dung )?thong thuong)/],
    ["low-power", /(?:hours?|gio)\s+(?:in low power|o che do nguon dien thap|o che do tiet kiem)/],
    ["anc", /(?:hours?|gio)\s+(?:listening with anc|nghe.*anc)/],
    ["general", /(?:hours?|gio)\s+(?:general use|su dung chung)/],
    ["video", /(?:hours?|gio)\s+(?:video|xem video)\s*$/],
    ["spatial", /(?:hours?|gio)\s+(?:with spatial audio|voi am thanh khong gian)/],
  ];
  for (const clause of normalized.split(/[;/]/)) {
    const hours = clause.match(/(\d+(?:[.,]\d+)?)\s*(?:hours?|gio)\b/); if (!hours) continue;
    for (const [key, expression] of contexts) if (expression.test(clause)) axes[key] = { min: number(hours[1]), max: number(hours[1]) };
  }
  return Object.keys(axes).length ? { axes } : null;
}

const coreDetail: Text = ["So sánh số lõi công bố, không phải benchmark tốc độ. Toàn khoảng cấu hình phải vượt đối thủ; số GB không phải số lõi.", "Compares published core counts, not benchmark speed. The whole configuration range must exceed alternatives; storage GB is not a core count."];
const optionDetail: Text = ["So mức tối đa hãng hỗ trợ, không khẳng định SKU đang chọn có cấu hình tối đa. Giữ các điều kiện dung lượng/cấu hình được nêu.", "Compares manufacturer-supported maxima, not a selected shop SKU. The stated storage/configuration conditions still apply."];
const versionDetail: Text = ["Chỉ nêu thế hệ/chuẩn mới hơn trong cùng dòng có thể đối chiếu; không suy ra tốc độ thực tế hay hiệu năng tổng thể.", "Indicates a newer generation/standard in comparable families, not real-world speed or overall performance."];
const rules: Record<string, Rule> = {
  storage: { read: value => reading(capacity(value)), maximum: true, summary: ["Dung lượng lớn hơn", "More storage"], detail: ["Dung lượng lớn nhất trong các SKU đang bán; tồn kho hiển thị riêng.", "Largest capacity among active shop configurations; stock is shown separately."] },
  storage_options: { read: value => reading(capacity(value)), maximum: true, summary: ["Dung lượng tối đa lớn hơn", "Higher maximum storage"], detail: optionDetail },
  memory_options: { read: value => {
    const clauses = value.split(";").map(clause => capacity(clause.split(/\s+(?:with|for|ở|cho)\s+/iu)[0]));
    return clauses.every(Boolean) ? reading(range(clauses.flatMap(item => [item!.min, item!.max]))) : null;
  }, maximum: true, summary: ["RAM tối đa lớn hơn", "Higher maximum RAM"], detail: optionDetail },
  cpu: { read: cores, summary: ["Nhiều lõi CPU hơn", "More CPU cores"], detail: coreDetail },
  gpu: { read: cores, summary: ["Nhiều lõi GPU hơn", "More GPU cores"], detail: coreDetail },
  neural_engine: { read: cores, summary: ["Nhiều lõi Neural Engine hơn", "More Neural Engine cores"], detail: coreDetail },
  memory_bandwidth: { read: value => reading(range(quantities(value, "GB/s\\b"))), summary: ["Băng thông bộ nhớ cao hơn", "Higher memory bandwidth"], detail: ["GB/s là băng thông, không phải dung lượng RAM. Giữ điều kiện cấu hình; không suy ra tốc độ mọi tác vụ.", "GB/s is bandwidth, not RAM capacity. Configuration conditions apply; it is not an overall speed score."] },
  refresh_rate: { read: value => reading(range(quantities(value, "Hz\\b"))), maximum: true, summary: ["Tần số quét tối đa cao hơn", "Higher maximum refresh rate"], detail: ["So Hz tối đa công bố. Không tự gán 60Hz cho sản phẩm thiếu dữ liệu.", "Compares published maximum Hz. Missing values are not assumed to be 60Hz."] },
  brightness: { read: brightness, summary: ["Sáng hơn ở điều kiện đối chiếu", "Brighter in matched conditions"], detail: ["Tách độ sáng thông thường/toàn màn hình, HDR và ngoài trời. Chỉ so điều kiện có ở tất cả sản phẩm; không so đỉnh HDR với độ sáng thông thường.", "Separates normal/full-screen, HDR and outdoor brightness. Only conditions known for all products count; HDR peak is not compared with normal brightness."] },
  resolution: { read: resolution, summary: ["Ưu thế về điểm ảnh", "Pixel-count advantage"], detail: ["So số điểm ảnh và ppi. Nhiều điểm ảnh không luôn là mật độ cao hơn. Màn hình/kích thước vỏ khác nhau cần đối chiếu riêng.", "Compares pixel count and ppi. More pixels do not necessarily mean higher density. Different screens/case sizes require separate matching."] },
  chip: { read: chip, summary: ["Chip thế hệ mới hơn", "Newer chip generation"], detail: versionDetail },
  wifi: { read: value => { const match = value.match(/Wi-Fi\s*(\d+)(E)?\b/i); return match ? reading(range([Number(match[1]) + (match[2] ? .5 : 0)])) : null; }, summary: ["Chuẩn Wi-Fi mới hơn", "Newer Wi-Fi standard"], detail: versionDetail },
  bluetooth: { read: value => /^\d+(?:\.\d+)?$/.test(value.trim()) ? reading(range([Number(value)])) : null, summary: ["Chuẩn Bluetooth mới hơn", "Newer Bluetooth standard"], detail: versionDetail },
  ports: { read: rate, summary: ["Tốc độ cổng công bố cao hơn", "Higher advertised port data rate"], detail: ["So tốc độ truyền dữ liệu cùng đơn vị, không xếp hạng đầu cắm. Cần cáp/thiết bị tương thích; không gộp cổng vào/ra khác nhau.", "Compares matching data-rate units, not connector names. Compatible cables/devices are required; distinct upstream/downstream ports are not conflated."] },
  data_rate: { read: rate, summary: ["Tốc độ dữ liệu cao hơn", "Higher stated data rate"], detail: ["Chỉ so tốc độ công bố cùng đơn vị và điều kiện.", "Only compares rates stated in matching units and conditions."] },
  weight: { read: weight, lower: true, summary: ["Nhẹ hơn", "Lighter"], detail: ["Cùng phạm vi đo, toàn khoảng cấu hình phải nhẹ hơn. Không cộng pin rời vào kính hoặc trộn mỗi tai với cả bộ.", "Matching measurement scopes; the whole configuration range must be lighter. Separate batteries and per-earbud/whole-device masses are not mixed."] },
  case_weight: { read: weight, lower: true, summary: ["Hộp nhẹ hơn", "Lighter case"], detail: ["Chỉ so khối lượng hộp, không phải pin hay độ bảo vệ.", "Compares case mass, not battery life or protection."] },
  dimensions: { read: thickness, lower: true, summary: ["Thân máy mỏng hơn", "Thinner body"], detail: ["Chỉ so độ dày/cao khi thứ tự kích thước rõ ràng. Không xếp hạng theo thể tích hoặc trộn trạng thái gập/mở.", "Only compares thickness/height when dimension order is explicit. Volume is not a score; folded and unfolded states are not mixed."] },
  battery_capacity: { read: value => reading(range(quantities(value, "Wh\\b"))), summary: ["Năng lượng pin lớn hơn", "Higher battery energy"], detail: ["So Wh, không kết luận dùng lâu hơn. Thời lượng phụ thuộc thiết bị và tác vụ.", "Compares Wh, not runtime. Battery life depends on the device and workload."] },
  video_playback: { read: videoPlayback, summary: ["Thời lượng video công bố dài hơn", "Longer stated video playback"], detail: ["So cùng chế độ phát/streaming; không trộn màn hình trong/ngoài. Số đo hãng công bố không bảo đảm thời lượng thực tế.", "Matches playback/streaming scenarios, not inner versus outer screens. Manufacturer figures do not guarantee actual runtime."] },
  battery_life: { read: batteryLife, summary: ["Thời lượng công bố dài hơn", "Longer stated runtime"], detail: ["Chỉ so cùng tác vụ Wi-Fi, streaming, ANC hoặc chế độ nguồn. Không lấy số giờ lớn nhất của tác vụ khác.", "Only matches identical Wi-Fi, streaming, ANC or power-mode workloads, not the largest number from another scenario."] },
  wired_charging: { read: charging, lower: true, summary: ["Đạt cùng mức pin sớm hơn", "Less time to the same charge level"], detail: ["So số phút tới cùng phần trăm pin, giữ yêu cầu củ sạc/cáp. Công suất củ sạc không phải tốc độ sạc máy.", "Compares minutes to the same charge percentage with adapter/cable requirements retained. Adapter power is not device charging speed."] },
  fast_charging: { read: charging, lower: true, summary: ["Đạt cùng mức pin sớm hơn", "Reaches the same charge level sooner"], detail: ["Không trộn phút sạc với số giờ sử dụng sau sạc.", "Charging minutes are not mixed with usage hours gained after charging."] },
  wireless_charging: { read: wirelessCharging, summary: ["Công suất sạc không dây cao hơn", "Higher wireless charging power"], detail: ["Tách giao thức MagSafe/Qi2/Qi; phải dùng bộ sạc tương thích.", "Matches MagSafe/Qi2/Qi separately; compatible chargers are required."] },
};
for (const field of ["pressure_sensitivity", "tilt_sensitivity", "hover", "haptic_feedback", "find_my"]) rules[field] = {
  read: booleanFeature, summary: ["Có tính năng này", "Includes this feature"], detail: ["So có/không hỗ trợ được nêu rõ; điều kiện thiết bị, ứng dụng và khu vực vẫn áp dụng.", "Compares explicitly supported versus unsupported features; device, app and regional conditions still apply."],
};
const notes: Record<string, Text> = {
  panel: ["OLED tự phát sáng; IPS/LCD dùng đèn nền. Khác biệt công nghệ không phải điểm chất lượng tổng thể; xem thêm độ sáng, Hz và mật độ điểm ảnh.", "OLED is self-emissive; IPS/LCD uses a backlight. Technology is not an overall quality score; also compare brightness, Hz and pixel density."],
  display_size: ["Màn hình lớn/nhỏ phù hợp nhu cầu khác nhau; không chọn bên thắng chỉ từ số inch.", "Larger/smaller displays suit different needs; inches alone do not determine a winner."],
  main_camera: ["MP, khẩu độ và cảm biến đo các khía cạnh khác nhau. Không suy ra ảnh đẹp hơn từ MP; cần cùng ống kính và phép thử.", "Megapixels, aperture and sensors measure different aspects. MP alone does not establish image quality; matching lenses and tests are needed."],
  front_camera: ["Không xếp hạng chất lượng ảnh chỉ bằng MP hay tên tính năng.", "Image quality is not ranked solely by MP or feature names."],
  water_resistance: ["IP và độ sâu/thời gian thử có phạm vi khác nhau. Không coi IP cao hơn là bảo đảm dùng được trong mọi môi trường nước.", "IP and depth/time tests have different scopes. A higher IP code does not guarantee every water use."],
};

export function compareSpecification(fieldId: string, values: string[], locale: Locale): SpecificationComparison {
  const empty = values.map(() => ({ advantage: false }));
  if (values.length < 2) return { cells: empty };
  const rule = rules[fieldId];
  if (!rule) return { cells: empty, note: translated(notes[fieldId] ?? ["Thông số mô tả/điều kiện sử dụng không có thang hơn/kém chung. Giữ chi tiết để đối chiếu, không tự chọn bên thắng.", "Descriptive specs/usage conditions have no universal ranking. Details remain available without inventing a winner."], locale) };
  const parsed = values.map(value => !value.trim() || value.trim() === "—" || /unknown|unverified|chưa xác minh|chưa rõ/i.test(value) ? null : rule.read(value));
  const explain = translated(rule.detail, locale);
  if (parsed.some(value => !value)) return { cells: empty, note: translated(["Chưa đủ dữ liệu cùng điều kiện ở tất cả sản phẩm. ", "Matching data is unavailable for every product. "], locale) + explain };
  const readings = parsed as Reading[];
  if (new Set(readings.map(value => value.signature ?? "")).size !== 1) return { cells: empty, note: translated(["Khác dòng hoặc phạm vi đo, không xếp hạng trực tiếp. ", "Different families/scopes cannot be ranked directly. "], locale) + explain };
  const keys = Object.keys(readings[0].axes).filter(key => readings.every(value => value.axes[key]));
  if (!keys.length) return { cells: empty, note: translated(["Không có điều kiện đo chung. ", "There is no shared measurement scenario. "], locale) + explain };
  const axis = (r: Reading, key: string): Range => rule.maximum ? { min: r.axes[key].max, max: r.axes[key].max } : r.axes[key];
  const same = (a: Range, b: Range) => a.min === b.min && a.max === b.max;
  const atLeast = (a: Range, b: Range) => same(a, b) || (rule.lower ? a.max <= b.min : a.min >= b.max);
  const strictly = (a: Range, b: Range) => rule.lower ? a.max < b.min : a.min > b.max;
  const cells = readings.map((candidate, index) => {
    const others = readings.filter((_, other) => other !== index);
    const advantage = others.every(other => keys.every(key => atLeast(axis(candidate, key), axis(other, key)))) && others.some(other => keys.some(key => strictly(axis(candidate, key), axis(other, key))));
    let summary = rule.summary;
    if (fieldId === "dimensions" && advantage) summary = keys.includes("depth") ? ["Độ dày/chiều sâu nhỏ hơn", "Less thickness/depth"] : ["Chiều cao thân máy nhỏ hơn", "Lower body height"];
    if (fieldId === "resolution" && advantage) {
      const pixels = keys.includes("pixels") && others.some(other => strictly(axis(candidate, "pixels"), axis(other, "pixels")));
      const density = keys.includes("density") && others.some(other => strictly(axis(candidate, "density"), axis(other, "density")));
      summary = pixels && density ? ["Nhiều điểm ảnh, mật độ cao hơn", "More pixels and higher density"] : pixels ? ["Nhiều điểm ảnh hơn", "More display pixels"] : ["Mật độ điểm ảnh cao hơn", "Higher pixel density"];
    }
    return advantage ? { advantage, summary: translated(summary, locale), detail: explain } : { advantage: false };
  });
  const equal = readings.every(r => keys.every(key => same(axis(r, key), axis(readings[0], key))));
  return { cells, note: (!equal && !cells.some(cell => cell.advantage) ? translated(["Có sự đánh đổi hoặc khoảng cấu hình chồng lấn; không có bên vượt trội toàn dòng. ", "Trade-offs or overlapping ranges mean no product dominates the whole row. "], locale) : "") + explain };
}
