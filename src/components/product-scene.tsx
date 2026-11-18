"use client";
import React, { Component, useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { Canvas, useFrame, useThree } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { ACESFilmicToneMapping, Group, PerspectiveCamera, PMREMGenerator, SRGBColorSpace } from "three";
import { RoomEnvironment } from "three/addons/environments/RoomEnvironment.js";
import type { LandingSection } from "@/lib/contracts";
import { Box, RotateCcw } from "lucide-react";
import { Button, Modal } from "./ui";
import { useStore } from "./client";
import { availablePaints, resolveAsset, type ModelAsset, type Paint } from "./v003-assets";
import { disposeModel, loadModel, modelSnapshot, paintModel, sampleFold, type ModelRuntime } from "./model-runtime";
import styles from "./product-scene.module.css";

declare global { interface Window { __BOTZONE_3D__?: Record<string, Readonly<Record<string, unknown>>> } }
let exploringScene: string | null = null;
const visibleScenes = new Map<string, { element: HTMLElement; ratio: number }>();
function chooseScene() {
  let best = "", distance = Infinity;
  if (!exploringScene) for (const [id, entry] of visibleScenes) {
    const rect = entry.element.getBoundingClientRect();
    const d = Math.abs((rect.top + rect.bottom) / 2 - window.innerHeight / 2);
    if (entry.ratio > .12 && d < distance) { best = id; distance = d; }
  }
  window.dispatchEvent(new CustomEvent("botzone-scene-active", { detail: best }));
}
class SceneBoundary extends Component<{ children: React.ReactNode; onError: () => void }, { failed: boolean }> {
  state = { failed: false };
  static getDerivedStateFromError() { return { failed: true }; }
  componentDidCatch() { this.props.onError(); }
  render() { return this.state.failed ? null : this.props.children; }
}
function Studio({ onError }: { onError: () => void }) {
  const { gl, scene, invalidate } = useThree();
  useEffect(() => {
    const pmrem = new PMREMGenerator(gl), room = new RoomEnvironment();
    const environment = pmrem.fromScene(room, .04);
    scene.environment = environment.texture; scene.environmentIntensity = .85;
    room.dispose(); pmrem.dispose(); invalidate();
    const canvas = gl.domElement;
    canvas.addEventListener("webglcontextlost", onError);
    return () => {
      // R3F deliberately loses context after unmount; detach first.
      canvas.removeEventListener("webglcontextlost", onError);
      scene.environment = null; environment.dispose();
    };
  }, [gl, scene, invalidate, onError]);
  return null;
}
type SceneProps = {
  asset: ModelAsset; paint?: Paint; fold: number; explore: boolean;
  config?: Partial<LandingSection>; angle: number; view: "inner" | "outer";
  pointerTilt: React.RefObject<{ x: number; y: number }>;
  sceneId: string; onReady: () => void; onError: () => void;
};
function AssetModel(props: SceneProps) {
  const { asset, paint, fold, explore, config, angle, view, sceneId } = props;
  const { gl, invalidate, size, camera } = useThree();
  const [runtime, setRuntime] = useState<ModelRuntime | null>(null);
  const parent = useRef<Group>(null), offset = useRef<Group>(null);
  const smoothPointer = useRef({ x: 0, y: 0 });
  const handlers = useRef(props); handlers.current = props;
  const ready = useRef(false), frames = useRef(0), frameTimes = useRef<number[]>([]), lastReport = useRef(0);
  const reportKey = `${sceneId}:${explore ? "explore" : "inline"}`;
  useEffect(() => {
    const controller = new AbortController(); let owned: ModelRuntime | undefined;
    ready.current = false; setRuntime(null);
    loadModel(asset, controller.signal, gl.capabilities.getMaxAnisotropy()).then((loaded) => {
      if (controller.signal.aborted) { disposeModel(loaded); return; }
      owned = loaded; setRuntime(loaded); invalidate();
    }).catch((error: unknown) => {
      if (!controller.signal.aborted) { console.warn("BotZone 3D: using poster fallback", error); handlers.current.onError(); }
    });
    return () => {
      controller.abort(); if (owned) disposeModel(owned);
      if (window.__BOTZONE_3D__) delete window.__BOTZONE_3D__[reportKey];
    };
  }, [asset, gl, invalidate, reportKey]);
  useLayoutEffect(() => {
    if (!runtime) return;
    sampleFold(runtime, fold / 100);
    offset.current?.position.copy(runtime.center).multiplyScalar(-1);
    invalidate(2);
  }, [runtime, fold, invalidate]);
  useLayoutEffect(() => { if (runtime) { paintModel(runtime, paint); invalidate(); } }, [runtime, paint, invalidate]);
  useLayoutEffect(() => {
    if (!(camera instanceof PerspectiveCamera)) return;
    const aspect = size.width / Math.max(1, size.height);
    const cmsZoom = Math.min(1.5, Math.max(.95, (config?.camera ?? 6.5) / 6.5));
    const distance = 3 / (2 * Math.tan(camera.fov * Math.PI / 360)) * 1.24 * Math.max(1, 1 / aspect) * cmsZoom;
    camera.position.set(0, 0, distance); camera.lookAt(0, 0, 0); camera.updateProjectionMatrix(); invalidate();
  }, [camera, size.width, size.height, config?.camera, invalidate]);
  useEffect(() => { invalidate(); }, [angle, view, config?.preset, config?.tilt, config?.speed, invalidate]);
  useFrame(({ clock }, delta) => {
    if (!runtime || runtime.disposed || !parent.current) return;
    const t = clock.getElapsedTime() * (config?.speed ?? 1), moving = !explore;
    // Time-based easing keeps the response soft at different frame rates.
    // The pointer only moves the presentation parent, never the authored rig.
    const target = props.pointerTilt.current, pointer = smoothPointer.current;
    const follow = 1 - Math.exp(-6 * Math.min(delta, .05));
    pointer.x += ((moving ? target.x : 0) - pointer.x) * follow;
    pointer.y += ((moving ? target.y : 0) - pointer.y) * follow;
    const baseY = asset.kind === "duo" && view === "inner" ? Math.PI + .16 : -.32;
    const preset = config?.preset ?? "float";
    // Rotate is a bounded presentation turn, float adds gentle vertical drift.
    // Fold holds the CMS/slider pose in the complete clip; it never auto-wraps.
    const idleYaw = Math.sin(t * .25) * (preset === "rotate" ? .2 : .055);
    // Only the presentation parent tilts; the clip owns all GLB transforms.
    parent.current.rotation.set((config?.tilt ?? 5) * Math.PI / 180 + (moving ? pointer.y * .11 : 0),
      baseY + angle + (moving ? idleYaw + pointer.x * .22 : 0),
      (asset.kind === "duo" ? -.035 : -.09) - (moving ? pointer.x * .028 : 0));
    parent.current.position.set(moving ? pointer.x * .10 : 0,
      (moving && preset === "float" ? Math.sin(t * .45) * .025 : 0) + (moving ? pointer.y * .07 : 0), 0);
    frames.current++;
    const now = performance.now(); frameTimes.current.push(now);
    frameTimes.current = frameTimes.current.filter((time) => time > now - 1000);
    if (new URLSearchParams(window.location.search).get("debug3d") === "1" && (now - lastReport.current > 120 || !moving)) {
      lastReport.current = now;
      parent.current.updateMatrixWorld(true);
      // JSON copies expose observations, never scene objects or mutators.
      const snapshot = JSON.parse(JSON.stringify({ ...modelSnapshot(runtime), sceneId, mode: explore ? "explore" : "inline", view,
        presentation: { rotation: [parent.current.rotation.x, parent.current.rotation.y, parent.current.rotation.z],
          position: parent.current.position.toArray(), pointerTarget: target, pointerSmoothed: pointer },
        paint: paint?.id ?? "original", camera: { position: camera.position.toArray(), quaternion: camera.quaternion.toArray(), near: camera.near, far: camera.far },
        renderer: { frames: frames.current, frame: gl.info.render.frame, calls: gl.info.render.calls,
          triangles: gl.info.render.triangles, textures: gl.info.memory.textures, geometries: gl.info.memory.geometries,
          dpr: gl.getPixelRatio(), fps: frameTimes.current.length > 1 ? (frameTimes.current.length - 1) * 1000 / (now - frameTimes.current[0]) : 0,
          fpsWindowMs: frameTimes.current.length > 1 ? now - frameTimes.current[0] : 0, viewport: [size.width, size.height],
          drawingBuffer: [gl.domElement.width, gl.domElement.height] } }));
      window.__BOTZONE_3D__ ??= {}; window.__BOTZONE_3D__[reportKey] = Object.freeze(snapshot);
    }
    if (!ready.current) { ready.current = true; handlers.current.onReady(); invalidate(); }
    if (moving) invalidate();
  });
  if (!runtime) return null;
  return <group ref={parent}><group scale={3 / runtime.extent}><group ref={offset}><primitive object={runtime.scene} dispose={null} /></group></group></group>;
}
function SceneCanvas(props: SceneProps) {
  // The GLB is normalized to extent 3, so this tight frustum still covers the
  // entire 2..14 orbit range. It resolves the authored 4µm Pro Max glass/OLED
  // separation that z-fights with a generic .01..100 camera.
  return <SceneBoundary onError={props.onError}>
    <Canvas dpr={[1, props.asset.mobile ? 1.5 : 2]} frameloop="demand" camera={{ position: [0, 0, 6.5], fov: 30, near: .25, far: 40 }}
      gl={{ antialias: true, alpha: true, powerPreference: "low-power" }}
      style={{ touchAction: props.explore ? "none" : "pan-y", pointerEvents: props.explore ? "auto" : "none" }}
      onCreated={({ gl }) => { gl.outputColorSpace = SRGBColorSpace; gl.toneMapping = ACESFilmicToneMapping; gl.toneMappingExposure = 1.15; }}>
      <Studio onError={props.onError} />
      <directionalLight position={[1, 1.4, 2]} intensity={2.8 * Math.min(1.25, Math.max(.75, (props.config?.light ?? 2) / 2))} />
      <directionalLight position={[-2, .2, -1]} intensity={.6} color="#e9eeff" />
      <directionalLight position={[.5, 1, -2]} intensity={1.4} /><hemisphereLight args={["#ffffff", "#66666a", .35]} />
      <AssetModel {...props} />
      {props.explore && <OrbitControls enablePan={false} enableZoom={false} enableDamping minDistance={2} maxDistance={14} makeDefault />}
    </Canvas>
  </SceneBoundary>;
}
export default function ProductScene({ duo = false, poster, sceneId, config }: { duo?: boolean; poster: string; sceneId: string; config?: Partial<LandingSection> }) {
  const s = useStore(), container = useRef<HTMLDivElement>(null), returnFocus = useRef<HTMLButtonElement>(null), scroll = useRef(0);
  const pointerTilt = useRef({ x: 0, y: 0 });
  const [mobile, setMobile] = useState<boolean | null>(null), [active, setActive] = useState(false);
  const [motion, setMotion] = useState(false), [failed, setFailed] = useState(false), [explore, setExplore] = useState(false);
  const [color, setColor] = useState("original"), [fold, setFold] = useState(Math.min(100, Math.max(0, (config?.fold ?? (duo ? 180 : 0)) / 1.8)));
  const [reset, setReset] = useState(0), [angle, setAngle] = useState(0);
  const [view, setView] = useState<"inner" | "outer">(duo ? "inner" : "outer");
  const [ready, setReady] = useState(false), [exploreReady, setExploreReady] = useState(false), [pageVisible, setPageVisible] = useState(true);
  const asset = useMemo(() => mobile === null ? null : resolveAsset(config?.model, duo, mobile), [mobile, config?.model, duo]);
  const colors = useMemo(() => availablePaints(asset?.kind ?? (duo ? "duo" : "promax"), config?.colors), [asset?.kind, duo, config?.colors]);
  const paint = colors.find((item) => item.id === color);
  const darkBackdrop = paint && ["silver", "glacier", "trang-anh-sao"].includes(paint.id);
  const onError = useCallback(() => { setFailed(true); setReady(false); setExploreReady(false); }, []);
  const onReady = useCallback(() => setReady(true), []), onExploreReady = useCallback(() => setExploreReady(true), []);
  useEffect(() => {
    const mq = matchMedia("(max-width: 767px)"), reduced = matchMedia("(prefers-reduced-motion: reduce)");
    const update = () => { setMobile(mq.matches); setMotion(!reduced.matches); };
    const visibility = () => setPageVisible(document.visibilityState === "visible");
    update(); visibility(); mq.addEventListener("change", update); reduced.addEventListener("change", update);
    document.addEventListener("visibilitychange", visibility);
    return () => { mq.removeEventListener("change", update); reduced.removeEventListener("change", update); document.removeEventListener("visibilitychange", visibility); };
  }, []);
  useEffect(() => {
    if (!(active && motion || explore) || !asset) return;
    const canvas = document.createElement("canvas");
    try { const gl = canvas.getContext("webgl2"); if (!gl) setFailed(true); else gl.getExtension("WEBGL_lose_context")?.loseContext(); }
    catch { setFailed(true); }
  }, [active, motion, explore, asset]);
  useEffect(() => { setReady(false); setExploreReady(false); }, [asset, active, pageVisible, explore]);
  useEffect(() => {
    const element = container.current; if (!element) return;
    const onActive = (event: Event) => setActive((event as CustomEvent<string>).detail === sceneId);
    window.addEventListener("botzone-scene-active", onActive);
    const io = new IntersectionObserver((entries) => { visibleScenes.set(sceneId, { element, ratio: entries[0].intersectionRatio }); chooseScene(); }, { threshold: [0, .12, .3, .5, .7, 1] });
    io.observe(element);
    return () => { io.disconnect(); visibleScenes.delete(sceneId); if (exploringScene === sceneId) exploringScene = null; window.removeEventListener("botzone-scene-active", onActive); chooseScene(); };
  }, [sceneId]);
  useEffect(() => {
    pointerTilt.current = { x: 0, y: 0 };
    if (!active || !motion || !pageVisible || failed || explore) return;
    const element = container.current;
    const surface = element?.closest<HTMLElement>("[data-product-hero]") ?? element;
    if (!surface) return;
    const resetPointer = () => { pointerTilt.current = { x: 0, y: 0 }; };
    const move = (event: PointerEvent) => {
      if (event.pointerType !== "mouse" || !matchMedia("(hover: hover) and (pointer: fine)").matches) return;
      const rect = surface.getBoundingClientRect();
      const clamp = (value: number) => Math.max(-1, Math.min(1, value));
      pointerTilt.current = { x: clamp((event.clientX - rect.left) / Math.max(1, rect.width) * 2 - 1),
        y: clamp(1 - (event.clientY - rect.top) / Math.max(1, rect.height) * 2) };
    };
    surface.addEventListener("pointermove", move, { passive: true });
    surface.addEventListener("pointerleave", resetPointer);
    surface.addEventListener("pointercancel", resetPointer);
    window.addEventListener("blur", resetPointer);
    window.addEventListener("scroll", resetPointer, { passive: true });
    return () => {
      surface.removeEventListener("pointermove", move);
      surface.removeEventListener("pointerleave", resetPointer);
      surface.removeEventListener("pointercancel", resetPointer);
      window.removeEventListener("blur", resetPointer);
      window.removeEventListener("scroll", resetPointer);
      resetPointer();
    };
  }, [active, motion, pageVisible, failed, explore]);
  const close = (value: boolean) => {
    exploringScene = value ? sceneId : null;
    if (value) { setExploreReady(false); scroll.current = window.scrollY; }
    setExplore(value); chooseScene();
    if (!value) requestAnimationFrame(() => { window.scrollTo(0, scroll.current); returnFocus.current?.focus({ preventScroll: true }); chooseScene(); });
  };
  const resetView = () => { setReset((n) => n + 1); setExploreReady(false); setColor("original"); setAngle(0); setFold(duo ? 100 : 0); setView(duo ? "inner" : "outer"); };
  const inline = Boolean(asset && active && pageVisible && motion && !failed && !explore);
  const props = asset ? { asset, paint, fold, config, angle, view, sceneId, onError, pointerTilt } : null;
  return <div className={`scene-container ${styles.scene}`} ref={container} data-scene-id={sceneId}
    data-model-version={asset?.version} data-model-url={asset?.url} data-mobile={mobile} data-ready={inline && ready} data-failed={failed}>
    <img className="scene-poster" style={{ opacity: inline && ready ? 0 : 1 }} src={poster} alt={duo ? "iPhone Duo — BotZone 3D v003" : "iPhone 18 Pro Max Burgundy — BotZone 3D v003"} />
    {inline && props && <div className="scene-canvas"><SceneCanvas {...props} explore={false} onReady={onReady} /></div>}
    <div className="scene-controls actions">
      <button ref={returnFocus} className="button button-outline button-sm" data-testid="explore-3d" onClick={() => close(true)}><Box size={13} />{s.tr("Khám phá 3D", "Explore in 3D")}</button>
    </div>
    <Modal className={styles.dialog} open={explore} onOpenChange={close} title={duo ? "iPhone Duo" : "iPhone 18 Pro Max"} description={s.tr("Kéo để xoay. Mô hình và màu sắc mang tính tham khảo.", "Drag to rotate. Model and colors are illustrative.")}>
      <div className={`explore-canvas ${styles.explore}`} data-backdrop={darkBackdrop ? "dark" : "light"} data-scene-id={sceneId} data-ready={exploreReady && pageVisible} data-model-url={asset?.url}>
        {(!exploreReady || failed || !pageVisible) && <img className="explore-poster" src={poster} alt={s.tr("Ảnh sản phẩm", "Product image")} />}
        {!failed && pageVisible && props && <SceneCanvas key={reset} {...props} explore onReady={onExploreReady} />}
      </div>
      {failed ? <p className="help-text" role="status">{s.tr("Không thể tải mô hình 3D trên thiết bị này. Bạn vẫn có thể xem ảnh, thông tin và mua sản phẩm.", "3D is unavailable on this device. Product images, information and purchasing remain available.")}</p> :
        <div className={`explore-controls ${styles.controls}`} data-duo={duo}>
          <div className={styles.paintRow} role="group" aria-label={s.tr("Màu mô hình", "Model color")}>
            <Button size="sm" variant="outline" aria-pressed={color === "original"} data-testid="color-original" onClick={() => setColor("original")}>{s.tr("Màu gốc", "Original")}</Button>
            <div className="swatches">{colors.map((c) => <button className={`swatch ${color === c.id ? "active" : ""}`} key={c.id} style={{ background: c.swatch }} onClick={() => setColor(c.id)} aria-label={s.tr(c.vi, c.en)} aria-pressed={color === c.id} />)}</div>
          </div>
          <div className={styles.sliders} data-duo={duo}>
            {duo && <label className={`field ${styles.slider}`}>
              <span className={styles.sliderLabel}><span>{s.tr("Độ mở", "Open amount")}</span><output aria-hidden="true">{Math.round(fold)}%</output></span>
              <input data-testid="duo-fold" type="range" min="0" max="100" step="1" value={fold} onChange={(e) => setFold(Number(e.target.value))} aria-label={s.tr("Độ mở", "Open amount")} aria-valuetext={`${Math.round(fold)}%`} />
            </label>}
            <label className={`field ${styles.slider}`}>
              <span className={styles.sliderLabel}><span>{s.tr("Góc xoay", "Rotation")}</span><output aria-hidden="true">{Math.round(angle * 180 / Math.PI)}°</output></span>
              <input data-testid="model-rotation" type="range" min="-180" max="180" value={Math.round(angle * 180 / Math.PI)} onChange={(e) => setAngle(Number(e.target.value) * Math.PI / 180)} aria-label={s.tr("Xoay bằng bàn phím", "Keyboard rotation")} aria-valuetext={s.tr(`${Math.round(angle * 180 / Math.PI)} độ`, `${Math.round(angle * 180 / Math.PI)} degrees`)} />
            </label>
          </div>
          <div className={styles.controlActions}>
            {duo && <Button className={styles.sideToggle} size="sm" variant="ghost" data-testid="duo-side-view" data-view={view} onClick={() => { setView(view === "inner" ? "outer" : "inner"); setAngle(0); }}>{view === "inner" ? s.tr("Xem mặt ngoài", "Show outer side") : s.tr("Xem mặt trong", "Show inner side")}</Button>}
            <Button className={styles.reset} variant="outline" size="sm" onClick={resetView}><RotateCcw />{s.tr("Đặt lại góc nhìn", "Reset view")}</Button>
          </div>
        </div>}
    </Modal>
  </div>;
}
