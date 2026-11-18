import {TextureLoader, SRGBColorSpace, LinearFilter, LinearMipmapLinearFilter} from './vendor/three/build/three.module.js';
import {normalizeMaterialName} from './material-presets.js';

const cancelled = () => Object.assign(new Error('Display request cancelled.'), {name: 'AbortError'});

/** Keep OLED artwork contrast independent of the studio tone-mapping curve. */
export function configureOLEDMaterials(materials, {maxAnisotropy = 1} = {}) {
  const names = new Set(['MAT_ScreenOuter', 'MAT_ScreenInner']);
  const anisotropy = Math.max(1, Math.min(8, Math.floor(Number(maxAnisotropy) || 1)));
  const changed = [];
  for (const material of materials) {
    if (!names.has(normalizeMaterialName(material.name))) continue;
    material.toneMapped = false;
    material.needsUpdate = true;
    for (const texture of new Set([material.map, material.emissiveMap].filter(Boolean))) {
      texture.anisotropy = anisotropy;
      texture.generateMipmaps = true;
      texture.minFilter = LinearMipmapLinearFilter;
      texture.magFilter = LinearFilter;
      texture.needsUpdate = true;
    }
    changed.push(material.name);
  }
  return changed;
}

/** Swap screen artwork only. Material tints, alpha and physical properties stay intact. */
export class DisplayModeController {
  constructor({materials, display, variant, embeddedTextures, loadTexture}) {
    this.display = display;
    this.variant = variant;
    this.embeddedTextures = embeddedTextures;
    this.loadTexture = loadTexture || (url => new TextureLoader().loadAsync(url));
    this.sequence = 0;
    this.disposed = false;
    this.originals = new Map();
    this.byName = new Map();
    this.sources = new Map();
    this.mapped = new Map();
    const names = new Set(Object.values(display?.modes || {}).flatMap(mode => Object.keys(mode.materials || {})));
    for (const material of materials) {
      const name = normalizeMaterialName(material.name);
      if (!names.has(name)) continue;
      this.originals.set(material, {map: material.map, emissiveMap: material.emissiveMap});
      if (!this.byName.has(name)) this.byName.set(name, []);
      this.byName.get(name).push(material);
    }
    for (const name of names) {
      if (!this.byName.has(name)) throw new Error('Không tìm thấy vật liệu màn hình: ' + name);
    }
    this.mode = display?.defaultMode || Object.keys(display?.modes || {})[0];
  }

  resolveTexture(value) {
    if (value === null) return null;
    const path = typeof value === 'string' ? value : value?.texture;
    const url = typeof path === 'string' ? path : path?.[this.variant] || path?.desktop || path?.mobile;
    if (!url || /^(?:[a-z]+:|\/\/)/i.test(url)) throw new Error('Screen texture must use a local relative path.');
    return url;
  }

  async sourceTexture(url) {
    if (!this.sources.has(url)) {
      let source = url;
      if (this.embeddedTextures) {
        const entry = this.embeddedTextures[url];
        if (!entry) throw new Error('Ảnh màn hình chưa được nhúng: ' + url);
        source = 'data:' + entry.mimeType + ';base64,' + entry.base64;
      }
      const promise = Promise.resolve().then(() => this.loadTexture(source)).then(texture => {
        if (this.disposed) {
          texture.dispose();
          texture.source?.data?.close?.();
          throw cancelled();
        }
        texture.colorSpace = SRGBColorSpace;
        texture.flipY = false;
        texture.needsUpdate = true;
        return texture;
      });
      this.sources.set(url, promise);
      promise.catch(() => { if (this.sources.get(url) === promise) this.sources.delete(url); });
    }
    return this.sources.get(url);
  }

  async mappedTexture(url, template) {
    const key = url + '|' + (template?.uuid || 'default');
    if (this.mapped.has(key)) return this.mapped.get(key);
    const source = await this.sourceTexture(url);
    if (this.disposed) throw cancelled();
    // Concurrent map/emissiveMap requests may have completed together.
    if (this.mapped.has(key)) return this.mapped.get(key);
    const texture = source.clone();
    if (template) {
      for (const property of ['mapping', 'channel', 'wrapS', 'wrapT', 'magFilter', 'minFilter', 'anisotropy', 'rotation', 'matrixAutoUpdate']) {
        texture[property] = template[property];
      }
      for (const property of ['offset', 'repeat', 'center', 'matrix']) texture[property].copy(template[property]);
    }
    texture.colorSpace = SRGBColorSpace;
    texture.flipY = false;
    texture.needsUpdate = true;
    this.mapped.set(key, texture);
    return texture;
  }

  restoreOriginals() {
    for (const [material, original] of this.originals) {
      if (material.map !== original.map || material.emissiveMap !== original.emissiveMap) {
        material.map = original.map;
        material.emissiveMap = original.emissiveMap;
        material.needsUpdate = true;
      }
    }
  }

  async apply(modeId) {
    if (this.disposed) throw cancelled();
    const mode = this.display?.modes?.[modeId];
    if (!mode) throw new Error('Chế độ màn hình không tồn tại: ' + modeId);
    const sequence = ++this.sequence;
    const swaps = await Promise.all(Object.entries(mode.materials || {}).flatMap(([name, value]) => {
      const url = this.resolveTexture(value);
      if (!url) return [];
      return this.byName.get(name).map(async material => {
        const original = this.originals.get(material);
        if (!original.map && !original.emissiveMap) throw new Error('Vật liệu màn hình không có map để thay: ' + name);
        const [map, emissiveMap] = await Promise.all([
          original.map ? this.mappedTexture(url, original.map) : null,
          original.emissiveMap ? this.mappedTexture(url, original.emissiveMap) : null
        ]);
        return {material, map, emissiveMap};
      });
    }));
    if (this.disposed || sequence !== this.sequence) throw cancelled();
    // Commit atomically after every requested texture decoded successfully.
    this.restoreOriginals();
    for (const {material, map, emissiveMap} of swaps) {
      material.map = map;
      material.emissiveMap = emissiveMap;
      material.needsUpdate = true;
    }
    this.mode = modeId;
    return {id: modeId, labelVi: mode.labelVi, labelEn: mode.labelEn, changedMaterials: swaps.map(s => s.material.name)};
  }

  dispose() {
    if (this.disposed) return;
    this.disposed = true;
    ++this.sequence;
    // Put embedded maps back so the owning GLB disposer releases them too.
    this.restoreOriginals();
    for (const texture of this.mapped.values()) texture.dispose();
    for (const promise of this.sources.values()) promise.then(texture => {
      texture.dispose();
      texture.source?.data?.close?.();
    }).catch(() => {});
    this.mapped.clear();
    this.sources.clear();
    this.originals.clear();
    this.byName.clear();
  }
}
