import {SRGBColorSpace, LinearSRGBColorSpace} from './vendor/three/build/three.module.js';

// Blender/loader duplicate suffixes do not change the semantic material slot.
export function normalizeMaterialName(name) {
  return String(name || '').replace(/(?:[._]\d+)+$/, '');
}
const protectedScreenMaterials=new Set(['MAT_ScreenOuter','MAT_ScreenInner','MAT_ScreenOuterGlass']);

// All source maps explicitly declare sRGB. Numeric triples are in [0,1].
// Original GLB colors are already in Three.js working space and are copied,
// never converted a second time.
export function applyMaterialPreset(materials, originalColors, preset) {
  const changed=[];
  const colorSpace=preset?.colorSpace || 'sRGB';
  if (preset && colorSpace !== 'sRGB' && colorSpace !== 'linear-sRGB') {
    throw new Error('Unsupported preset color space: '+colorSpace);
  }
  const space=colorSpace === 'sRGB' ? SRGBColorSpace : LinearSRGBColorSpace;
  for (const material of materials) {
    const slot=normalizeMaterialName(material.name);
    if (protectedScreenMaterials.has(slot)) continue;
    const original=originalColors.get(material.uuid);
    if (original && material.color) material.color.copy(original);
    if (!preset || !material.color) continue;
    const value=preset.materials?.[slot];
    if (value === undefined) continue;
    if (Array.isArray(value)) {
      if (value.length!==3 || value.some(v=>!Number.isFinite(v)||v<0||v>1)) {
        throw new Error('Invalid normalized RGB preset for '+slot);
      }
      material.color.setRGB(value[0],value[1],value[2],space);
    } else if (typeof value==='string' && /^#[0-9a-f]{6}$/i.test(value)) {
      material.color.setStyle(value,space);
    } else {
      throw new Error('Expected #RRGGBB or normalized RGB triple for '+slot);
    }
    changed.push(material.name);
  }
  return changed;
}
