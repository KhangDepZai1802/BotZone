/** Small runtime contract transcribed from the supplied v003 manifest/viewer-config.
 * The original handoff and full hashes are retained in docs/3d/v003/source. */
export type DeviceKind = "promax" | "duo" | "custom";
export type ModelAsset = {
  kind: DeviceKind; version: "v003" | "custom"; url: string; mobile: boolean;
  sha256?: string;
};
export type Paint = {
  id: string; vi: string; en: string; swatch: string;
  materials: Record<string, string | [number, number, number]>;
};
export const paints: Record<"promax" | "duo", Paint[]> = {
  promax: [
    { id: "burgundy", vi: "Burgundy", en: "Burgundy", swatch: "#54393e", materials: { MAT_Body: "#54393E", MAT_Back: "#38262A", MAT_Frame: "#5B3D44", MAT_CameraRing: "#54393E", MAT_Logo: "#553B43", MAT_Antenna: "#513943" } },
    { id: "black", vi: "Đen", en: "Black", swatch: "#27282b", materials: { MAT_Body: "#27282B", MAT_Back: "#1B1C1F", MAT_Frame: "#323337", MAT_CameraRing: "#28292D", MAT_Logo: "#35363B", MAT_Antenna: "#3E3F43" } },
    { id: "silver", vi: "Bạc", en: "Silver", swatch: "#c5c7c9", materials: { MAT_Body: "#C5C7C9", MAT_Back: "#BDC0C2", MAT_Frame: "#CBCDCE", MAT_CameraRing: "#AEB1B5", MAT_Logo: "#8E9298", MAT_Antenna: "#9D9FA2" } },
    { id: "glacier", vi: "Băng Thanh", en: "Glacier", swatch: "#afbec8", materials: { MAT_Body: "#AFBEC8", MAT_Back: "#9EAEB9", MAT_Frame: "#B7C6CE", MAT_CameraRing: "#9DADB9", MAT_Logo: "#8597A5", MAT_Antenna: "#8F9DA6" } },
  ],
  duo: [
    { id: "troi-dem", vi: "Trời Đêm", en: "Night Sky", swatch: "#2b3440", materials: { MAT_Frame: [.18, .235, .3], MAT_Back: [.17, .205, .25], MAT_CameraIsland: [.14, .185, .24], MAT_CameraRing: [.2, .26, .33], MAT_Logo: [.09, .13, .19], MAT_Antenna: [.08, .1, .13] } },
    { id: "trang-anh-sao", vi: "Trắng Ánh Sao", en: "Star White", swatch: "#dbdbd4", materials: { MAT_Frame: [.65, .64, .6], MAT_Back: [.86, .86, .83], MAT_CameraIsland: [.87, .87, .84], MAT_CameraRing: [.68, .68, .64], MAT_Logo: [.65, .65, .62], MAT_Antenna: [.46, .47, .46] } },
  ],
};
const hashes = {
  promax: { desktop: "a9b94e46477b13a5e18e148f4039d3321890de3d9f2053a31d73ff4d5a5a8f3a", mobile: "aedce816e094688a777b1daacc0414f9c1c3a3705a1eef2de79640b95300d0c8" },
  duo: { desktop: "6be09394df5059d77f84c76e0b2bc4d9143cf030bd2a76f07f20479f94acb32d", mobile: "35999aa5fb952cdcd06edad76505076f1e3fd490ae66440be4211eaa509c1628" },
};
export function resolveAsset(model: string | undefined, duo: boolean, mobile: boolean): ModelAsset {
  let kind: DeviceKind = duo ? "duo" : "promax";
  if (model === "v003:iphone-duo" || model === "procedural:duo") kind = "duo";
  else if (model === "v003:iphone-18-pro-max" || model === "procedural:phone") kind = "promax";
  else if (model && /^(\/api\/media\/|\/models\/)/.test(model)) {
    // Old bundled illustration URLs also resolve to the supplied replacement.
    if (model === "/models/duo-illustration.glb") kind = "duo";
    else if (model === "/models/phone-illustration.glb") kind = "promax";
    else if (/\/v003\/iphone-duo-(?:desktop|mobile)\.glb/.test(model)) kind = "duo";
    else if (/\/v003\/iphone-18-pro-max-(?:desktop|mobile)\.glb/.test(model)) kind = "promax";
    else return { kind: "custom", version: "custom", url: model, mobile };
  }
  const quality = mobile ? "mobile" : "desktop";
  const sha256 = hashes[kind][quality];
  const name = kind === "duo" ? "iphone-duo" : "iphone-18-pro-max";
  return { kind, version: "v003", mobile, sha256, url: `/models/v003/${name}-${quality}.glb?v=${sha256.slice(0, 12)}` };
}
/** CMS hexadecimal swatches select the closest supplied, per-material paint.
 * Screens, lenses, glass, screws and gaskets are never in a paint map. */
export function availablePaints(kind: DeviceKind, colors?: string[]): Paint[] {
  if (kind === "custom") return (colors ?? ["#71374d"]).map((c) => ({ id: c, vi: c, en: c, swatch: c, materials: { BodyColorable: c } }));
  const list = paints[kind];
  if (!colors?.length) return list;
  const rgb = (hex: string) => [1, 3, 5].map((at) => parseInt(hex.slice(at, at + 2), 16));
  const selected = colors.filter((c) => /^#[0-9a-f]{6}$/i.test(c)).map((color) => {
    const value = rgb(color);
    return list.reduce((best, item) => {
      const distance = (p: Paint) => rgb(p.swatch).reduce((n, v, i) => n + (v - value[i]) ** 2, 0);
      return distance(item) < distance(best) ? item : best;
    });
  });
  return [...new Map([list[0], ...selected].map((p) => [p.id, p])).values()];
}
