import {
  AnimationMixer, Box3, Color, LinearFilter, LinearMipmapLinearFilter,
  LoopOnce, Material, Mesh, MeshStandardMaterial, Quaternion, SRGBColorSpace, Texture, Vector3,
  type AnimationAction, type Group, type Object3D,
} from "three";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";
import type { ModelAsset, Paint } from "./v003-assets";

const slot = (name: string) => name.replace(/(?:[._]\d+)+$/, "");
const oledSlots = new Set(["MAT_ScreenInner", "MAT_ScreenOuter"]);
export type ModelRuntime = {
  scene: Group; asset: ModelAsset; materials: Set<Material>; originals: Map<Material, Color>;
  mixer?: AnimationMixer; action?: AnimationAction; extent: number;
  legacyHinge?: Object3D; legacyRotation?: Quaternion;
  center: Vector3; bytes: number; loadMs: number; disposed: boolean;
  uv: Record<string, unknown>[];
};

export async function loadModel(asset: ModelAsset, signal: AbortSignal, anisotropy: number): Promise<ModelRuntime> {
  const start = performance.now();
  const response = await fetch(asset.url, { signal });
  if (!response.ok) throw new Error(`Model request failed (${response.status}).`);
  const bytes = await response.arrayBuffer();
  if (asset.sha256) {
    const digest = await crypto.subtle.digest("SHA-256", bytes);
    const actual = Array.from(new Uint8Array(digest), (b) => b.toString(16).padStart(2, "0")).join("");
    if (actual !== asset.sha256) throw new Error("Model checksum does not match the v003 handoff.");
  }
  if (signal.aborted) throw new DOMException("Cancelled", "AbortError");
  const gltf = await new GLTFLoader().parseAsync(bytes, "");
  const runtime: ModelRuntime = {
    scene: gltf.scene, asset, materials: new Set(), originals: new Map(), extent: 0,
    center: new Vector3(), bytes: bytes.byteLength, loadMs: performance.now() - start,
    disposed: false, uv: [],
  };
  try {
    if (signal.aborted) throw new DOMException("Cancelled", "AbortError");
    runtime.scene.traverse((object) => {
      if (!(object instanceof Mesh)) return;
      const materials: Material[] = Array.isArray(object.material) ? object.material : [object.material];
      for (const material of materials) runtime.materials.add(material);
      if (/^ScreenInner/.test(object.name)) {
        const uv = object.geometry.getAttribute("uv");
        const bounds = [Infinity, Infinity, -Infinity, -Infinity];
        if (uv) for (let i = 0; i < uv.count; i++) {
          bounds[0] = Math.min(bounds[0], uv.getX(i)); bounds[1] = Math.min(bounds[1], uv.getY(i));
          bounds[2] = Math.max(bounds[2], uv.getX(i)); bounds[3] = Math.max(bounds[3], uv.getY(i));
        }
        runtime.uv.push({ node: object.name, count: uv?.count ?? 0, bounds: uv ? bounds : null });
      }
    });
    for (const material of runtime.materials) {
      if (!(material instanceof MeshStandardMaterial)) continue;
      runtime.originals.set(material, material.color.clone());
      if (asset.version !== "v003" || !oledSlots.has(slot(material.name))) continue;
      if (!material.emissiveMap) throw new Error(`Missing embedded OLED map: ${material.name}`);
      // Only OLED receives these changes. The emission factor and atlas UVs are
      // supplied by the artist; do not repaint or recreate these textures.
      material.color.setRGB(0, 0, 0); material.map = null; material.toneMapped = false;
      const texture = material.emissiveMap;
      texture.colorSpace = SRGBColorSpace; texture.flipY = false;
      texture.generateMipmaps = true; texture.minFilter = LinearMipmapLinearFilter;
      texture.magFilter = LinearFilter; texture.anisotropy = Math.max(1, Math.min(8, anisotropy));
      texture.needsUpdate = true; material.needsUpdate = true;
    }
    const clip = gltf.animations.find((a) => a.name === "Duo_OpenClose");
    if (asset.kind === "duo") {
      const expected = ["HingePivot.quaternion", "HingeSpine.position", "ScreenInnerFlex.morphTargetInfluences", "ScreenOuter.scale"];
      if (!clip || Math.abs(clip.duration - 2) > .00001 || clip.tracks.length !== 4 || expected.some((name) => !clip.tracks.some((track) => track.name === name))) {
        throw new Error("Duo v003 requires the complete four-track, two-second clip.");
      }
      if (!runtime.scene.getObjectByName("ScreenOuterGlass")) throw new Error("Duo permanent outer glass is missing.");
    }
    if (clip && (asset.kind === "duo" || asset.version === "custom")) {
      runtime.mixer = new AnimationMixer(runtime.scene);
      runtime.action = runtime.mixer.clipAction(clip);
      runtime.action.setLoop(LoopOnce, 1); runtime.action.clampWhenFinished = true;
      runtime.action.play(); runtime.action.paused = true;
    } else if (asset.version === "custom") {
      // Older uploaded CMS models declare a single Hinge without an authored
      // clip. This compatibility path is unreachable for any bundled v003 GLB.
      runtime.legacyHinge = runtime.scene.getObjectByName("Hinge");
      runtime.legacyRotation = runtime.legacyHinge?.quaternion.clone();
    }
    // A constant framing extent avoids camera pumping while scrubbing. Recenter
    // each pose separately, without touching any animated node transforms.
    for (const progress of runtime.action || runtime.legacyHinge ? [0, .25, .5, .75, 1] : [0]) {
      sampleFold(runtime, progress);
      const size = new Box3().setFromObject(runtime.scene).getSize(new Vector3());
      runtime.extent = Math.max(runtime.extent, size.x, size.y, size.z);
    }
    if (!Number.isFinite(runtime.extent) || runtime.extent <= 0) throw new Error("Empty model bounds.");
    sampleFold(runtime, asset.kind === "duo" ? 1 : 0);
    runtime.loadMs = performance.now() - start;
    return runtime;
  } catch (error) { disposeModel(runtime); throw error; }
}

export function sampleFold(runtime: ModelRuntime, progress: number) {
  if (runtime.action && runtime.mixer) {
    // Re-enable after every endpoint sample. No elapsed-time playback and no
    // modulo operation: time=duration must remain the fully-open pose.
    runtime.action.enabled = true; runtime.action.paused = true;
    runtime.action.time = Math.min(1, Math.max(0, progress)) * runtime.action.getClip().duration;
    runtime.mixer.update(0);
  } else if (runtime.asset.version === "custom" && runtime.legacyHinge && runtime.legacyRotation) {
    runtime.legacyHinge.quaternion.copy(runtime.legacyRotation).multiply(
      new Quaternion().setFromAxisAngle(new Vector3(0, 1, 0), -Math.min(1, Math.max(0, progress)) * Math.PI),
    );
  }
  // Box3 reads world matrices. This GLB is displayed below a normalized,
  // tilted, recentered parent; its world center is NOT an authored-meter
  // offset. Evaluate intrinsic bounds synchronously, then restore the parent
  // before rendering. Keep the parent's child list intact (no add/remove
  // events, no allocation/cloning, and no changes to animated transforms).
  const presentationParent = runtime.scene.parent;
  try {
    runtime.scene.parent = null;
    runtime.scene.updateMatrixWorld(true);
    new Box3().setFromObject(runtime.scene).getCenter(runtime.center);
  } finally {
    runtime.scene.parent = presentationParent;
    runtime.scene.updateWorldMatrix(true, true);
  }
}

export function paintModel(runtime: ModelRuntime, paint?: Paint) {
  for (const material of runtime.materials) {
    if (!(material instanceof MeshStandardMaterial)) continue;
    const name = slot(material.name);
    // Even reset deliberately avoids OLED. Its black base and emission remain
    // unchanged across every paint operation.
    if (oledSlots.has(name)) continue;
    const original = runtime.originals.get(material);
    if (original) material.color.copy(original);
    const value = paint?.materials[name];
    if (value === undefined) continue;
    if (Array.isArray(value)) material.color.setRGB(...value, SRGBColorSpace);
    else material.color.setStyle(value, SRGBColorSpace);
  }
}

const textureState = (texture: Texture | null) => texture ? {
  uuid: texture.uuid, name: texture.name, colorSpace: texture.colorSpace, flipY: texture.flipY,
  minFilter: texture.minFilter, magFilter: texture.magFilter, anisotropy: texture.anisotropy,
  channel: texture.channel, offset: texture.offset.toArray(), repeat: texture.repeat.toArray(),
} : null;
const materialState = (material: Material) => ({
  name: material.name, toneMapped: material.toneMapped,
  ...(material instanceof MeshStandardMaterial ? {
    color: material.color.toArray(), hex: `#${material.color.getHexString()}`,
    emissive: material.emissive.toArray(), emissiveIntensity: material.emissiveIntensity,
    map: textureState(material.map), emissiveMap: textureState(material.emissiveMap),
  } : {}),
});
export function modelSnapshot(runtime: ModelRuntime) {
  const outer = runtime.scene.getObjectByName("ScreenOuter");
  const glass = runtime.scene.getObjectByName("ScreenOuterGlass");
  const hinge = runtime.scene.getObjectByName("HingePivot");
  const flex = runtime.scene.getObjectByName("ScreenInnerFlex");
  const spine = runtime.scene.getObjectByName("HingeSpine");
  const clip = runtime.action?.getClip();
  return {
    modelUrl: runtime.asset.url, version: runtime.asset.version, mobile: runtime.asset.mobile,
    sha256: runtime.asset.sha256, bytes: runtime.bytes, loadMs: Math.round(runtime.loadMs),
    animationSource: runtime.action ? "authored-clip" : runtime.legacyHinge ? "legacy-custom-hinge" : "static",
    authoredCenter: runtime.center.toArray(), authoredExtent: runtime.extent,
    worldBounds: (() => { const box = new Box3().setFromObject(runtime.scene); return { min: box.min.toArray(), max: box.max.toArray() }; })(),
    clip: clip ? { name: clip.name, duration: clip.duration, time: runtime.action!.time, progress: runtime.action!.time / clip.duration,
      tracks: clip.tracks.map((t) => t.name), loop: runtime.action!.loop, clampWhenFinished: runtime.action!.clampWhenFinished } : null,
    outer: outer ? { scale: outer.scale.toArray(), visible: outer.visible, power: outer.scale.lengthSq() < 1e-12 ? "off" : "on" } : null,
    glass: glass ? { scale: glass.scale.toArray(), visible: glass.visible,
      materials: glass instanceof Mesh ? (Array.isArray(glass.material) ? glass.material : [glass.material]).map(materialState) : [] } : null,
    hinge: hinge ? { quaternion: hinge.quaternion.toArray(), position: hinge.position.toArray() } : null,
    spine: spine ? { position: spine.position.toArray() } : null,
    screens: { materials: [...runtime.materials].filter((m) => oledSlots.has(slot(m.name))).map(materialState),
      uvBounds: runtime.uv, morphInfluences: flex instanceof Mesh ? [...(flex.morphTargetInfluences ?? [])] : [] },
  };
}

export function disposeModel(runtime: ModelRuntime) {
  if (runtime.disposed) return;
  runtime.disposed = true;
  runtime.mixer?.stopAllAction(); runtime.mixer?.uncacheRoot(runtime.scene);
  const geometries = new Set<Mesh["geometry"]>();
  const materials = new Set<Material>(); const textures = new Set<Texture>();
  runtime.scene.traverse((object) => {
    if (!(object instanceof Mesh)) return;
    geometries.add(object.geometry);
    for (const material of (Array.isArray(object.material) ? object.material : [object.material])) {
      materials.add(material);
      for (const value of Object.values(material)) if (value instanceof Texture) textures.add(value);
    }
  });
  for (const geometry of geometries) geometry.dispose();
  for (const material of materials) material.dispose();
  for (const texture of textures) { texture.dispose(); (texture.source?.data as ImageBitmap | undefined)?.close?.(); }
  runtime.originals.clear(); runtime.materials.clear();
}
