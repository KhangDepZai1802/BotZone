import * as THREE from 'three';
import {OrbitControls} from 'three/addons/controls/OrbitControls.js';
import {GLTFLoader} from 'three/addons/loaders/GLTFLoader.js';
import {RoomEnvironment} from 'three/addons/environments/RoomEnvironment.js';
import {applyMaterialPreset} from './material-presets.js';
import {DisplayModeController, configureOLEDMaterials} from './display-modes.js';

const $=s=>document.querySelector(s);
const params=new URLSearchParams(location.search);
if(params.get('capture')==='1') document.body.classList.add('capture');
const stage=$('#stage'), message=$('#load-message'), state=$('#state');
let config, renderer, scene, camera, controls, envTarget, model, mixer, action, displayController, frame=0, requestId=0, displayRequestId=0, abort, activeKey, baseExtent=.17, activeBytes=0, loadMs=0;
const meshes=[], materials=new Map(), baseColors=new Map(), reduced=matchMedia('(prefers-reduced-motion: reduce)'), coarse=matchMedia('(pointer: coarse)');
const displaySelections=new Map();
const api=window.BotZonePreview={ready:false,error:null,stats:{},setView, setFold, setColorPreset, setDisplayMode, requestRender, capture:()=>renderer.domElement.toDataURL('image/png')};
const stateText=(t,error=false)=>{state.textContent=t;state.classList.toggle('error',error)};
try{
 config=window.BotZoneEmbedded?.config || await (await fetch('./viewer-config.json')).json();
 if(window.BotZoneEmbedded){
  for(const option of $('#device').options)option.disabled=!config.assets[option.value];
 }
 renderer=new THREE.WebGLRenderer({antialias:true,alpha:false,preserveDrawingBuffer:true,powerPreference:'high-performance'});
 renderer.outputColorSpace=THREE.SRGBColorSpace;renderer.toneMapping=THREE.ACESFilmicToneMapping;renderer.toneMappingExposure=1.15;
 renderer.setPixelRatio(Math.min(devicePixelRatio,2));stage.appendChild(renderer.domElement);
 scene=new THREE.Scene();scene.background=new THREE.Color('#171719');
 camera=new THREE.PerspectiveCamera(30,1,.001,50);
 controls=new OrbitControls(camera,renderer.domElement);controls.enableDamping=true;controls.dampingFactor=.09;controls.enablePan=true;controls.autoRotateSpeed=.7;
 const pmrem=new THREE.PMREMGenerator(renderer), room=new RoomEnvironment();
 envTarget=pmrem.fromScene(room,.04);scene.environment=envTarget.texture;scene.environmentIntensity=.85;room.dispose();pmrem.dispose();
 const key=new THREE.DirectionalLight(0xffffff,2.8);key.name='PreviewKey';key.position.set(1,1.4,2);scene.add(key);
 const fill=new THREE.DirectionalLight(0xe9eeff,.6);fill.name='PreviewFill';fill.position.set(-2,.2,-1);scene.add(fill);
 const rim=new THREE.DirectionalLight(0xffffff,1.4);rim.name='PreviewRim';rim.position.set(.5,1,-2);scene.add(rim);
 scene.add(new THREE.HemisphereLight(0xffffff,0x66666a,.35));
 controls.addEventListener('change',requestRender);
 new ResizeObserver(resize).observe(stage);
 $('#device').value=params.get('device') in config.assets?params.get('device'):(config.defaultAsset in config.assets?config.defaultAsset:Object.keys(config.assets)[0]);
 $('#variant').value=params.get('variant')==='mobile'?'mobile':'desktop';
 $('#device').addEventListener('change',loadModel);$('#variant').addEventListener('change',loadModel);
 $('#color').addEventListener('change',e=>setColorPreset(e.target.value));
 $('#display-mode').addEventListener('change',e=>setDisplayMode(e.target.value));
 $('#fold').addEventListener('input',e=>setFold(Number(e.target.value)/100));
 $('#lighting').addEventListener('change',setLighting);$('#background').addEventListener('change',setBackground);
 $('#reset').addEventListener('click',()=>{const asset=config.assets[activeKey];setFold(asset.defaultFold||0);setView(asset.defaultView||'three-quarter');});
 document.querySelectorAll('[data-view]').forEach(b=>b.addEventListener('click',()=>setView(b.dataset.view)));
 document.querySelectorAll('[data-duo-screen]').forEach(b=>b.addEventListener('click',()=>{const inner=b.dataset.duoScreen==='inner';setFold(inner?1:0);setView(inner?'inner-three-quarter':'front-three-quarter');}));
 $('#auto').addEventListener('change',()=>{controls.autoRotate=$('#auto').checked&&!reduced.matches;requestRender();});
 $('#touch-mode').hidden=!coarse.matches;
 let explore=!coarse.matches;
 const setExplore=()=>{controls.enabled=explore;renderer.domElement.style.touchAction=explore?'none':'pan-y';$('#touch-mode').textContent=explore?'Kết thúc khám phá':'Khám phá 3D';$('#gesture-hint').textContent=explore?'Kéo để xoay · Cuộn để phóng gần':'Vuốt dọc để cuộn trang · Nhấn Khám phá để xoay model';};
 setExplore();$('#touch-mode').addEventListener('click',()=>{explore=!explore;setExplore();});
 reduced.addEventListener('change',()=>{if(reduced.matches){$('#auto').checked=false;controls.autoRotate=false;}requestRender();});
 document.addEventListener('visibilitychange',()=>{if(document.hidden){cancelAnimationFrame(frame);frame=0;}else requestRender();});
 $('#save-image').addEventListener('click',()=>{draw();const a=document.createElement('a');a.href=api.capture();a.download=activeKey+'-'+$('#variant').value+'-'+$('#color').value+'-'+(displayController?.mode||'original')+'.png';a.click();});
 const initialBackground=params.get('background');
 $('#background').value=['light','dark'].includes(initialBackground)?initialBackground:(config.defaultBackground||'dark');setBackground();
 if(params.get('lighting')){$('#lighting').value=params.get('lighting');setLighting();}
 resize();await loadModel();
}catch(e){fail(e);}
function fail(e){api.ready=false;api.error=String(e);stateText('Chưa thể xem model',true);message.hidden=false;message.textContent='Không tải được model: '+e.message;console.error(e);}
function disposeModel(obj){if(!obj)return;const gs=new Set(),ms=new Set(),ts=new Set();obj.traverse(n=>{if(n.geometry)gs.add(n.geometry);for(const mat of(Array.isArray(n.material)?n.material:[n.material]).filter(Boolean)){ms.add(mat);for(const v of Object.values(mat))if(v?.isTexture)ts.add(v);}});gs.forEach(g=>g.dispose());ms.forEach(m=>m.dispose());ts.forEach(t=>{t.dispose();t.source?.data?.close?.();});scene.remove(obj);}
async function loadModel(){
 const id=++requestId;abort?.abort();abort=new AbortController();api.ready=false;api.error=null;
 ++displayRequestId;displayController?.dispose();displayController=null;api.displayMode=null;$('#display-mode').disabled=true;$('#display-status').textContent='';$('#display-status').classList.remove('error');$('#display-section').removeAttribute('aria-busy');
 api.outerScreen=null;$('#screen-power-status').hidden=true;
 if(mixer){mixer.stopAllAction();if(model)mixer.uncacheRoot(model);}disposeModel(model);model=null;mixer=null;action=null;meshes.length=0;materials.clear();baseColors.clear();
 activeKey=$('#device').value;const asset=config.assets[activeKey];
 for(const option of $('#variant').options)option.disabled=!asset[option.value];
 if(!asset[$('#variant').value])$('#variant').value=asset.desktop?'desktop':'mobile';
 const override=params.get('file'),url=override&&!/^[a-z]+:|^\/\//i.test(override)?override:asset[$('#variant').value];
 $('#device-name').textContent=asset.name;$('#variant-label').textContent=$('#variant').value;$('#asset-path').textContent=url;
 $('#fold-section').hidden=activeKey!=='duo';$('#fold').disabled=true;document.querySelectorAll('[data-duo-screen]').forEach(b=>b.disabled=true);
 $('#color').replaceChildren(new Option('Màu gốc trong GLB / Original','original'),...Object.entries(asset.colors||{}).map(([k,c])=>new Option(c.label,k)));
 message.hidden=false;message.textContent='Đang tải '+asset.name+'…';stateText('Đang tải GLB');requestRender();
 const start=performance.now();
 try{
  let bytes;
  if(window.BotZoneEmbedded){
   const entry=window.BotZoneEmbedded.assets[url];
   if(!entry)throw new Error('Phiên bản model này chưa được nhúng trong bộ preview.');
   const decoded=atob(entry.base64);const array=new Uint8Array(decoded.length);for(let i=0;i<decoded.length;i++)array[i]=decoded.charCodeAt(i);bytes=array.buffer;
  }else{
   const response=await fetch(url,{signal:abort.signal});if(!response.ok)throw new Error('HTTP '+response.status+' — '+url);
   bytes=await response.arrayBuffer();
  }
  const gltf=await new GLTFLoader().parseAsync(bytes,'');
  if(id!==requestId){disposeModel(gltf.scene);return;}
  model=gltf.scene;model.name='PreviewAsset';scene.add(model);model.updateMatrixWorld(true);
  model.traverse(n=>{if(!n.isMesh)return;meshes.push(n);const original=Array.isArray(n.material)?n.material:[n.material];const clones=original.map(m=>{if(!materials.has(m.uuid)){const c=m.clone();materials.set(m.uuid,c);baseColors.set(c.uuid,c.color?.clone());}return materials.get(m.uuid);});n.material=Array.isArray(n.material)?clones:clones[0];});
  const maxAnisotropy=renderer.capabilities.getMaxAnisotropy();
  api.screenMaterialConfig={toneMapped:false,anisotropy:Math.max(1,Math.min(8,maxAnisotropy)),materials:configureOLEDMaterials(materials.values(),{maxAnisotropy})};
  const display=asset.display;
  $('#display-section').hidden=Object.keys(display?.modes||{}).length<2;
  if(display?.modes){
   $('#display-mode').replaceChildren(...Object.entries(display.modes).map(([key,mode])=>new Option(mode.label||[mode.labelVi,mode.labelEn].filter(Boolean).join(' / '),key)));
   displayController=new DisplayModeController({materials:materials.values(),display,variant:$('#variant').value,embeddedTextures:window.BotZoneEmbedded?.textures});
   const requested=params.get('display')||displaySelections.get(activeKey)||display.defaultMode;
   $('#display-mode').disabled=false;
   await setDisplayMode(display.modes[requested]?requested:display.defaultMode);
   if(id!==requestId)return;
  }
  const clip=gltf.animations.find(c=>c.name===asset.clip)||gltf.animations.find(c=>/open.*close/i.test(c.name));
  if(clip){mixer=new THREE.AnimationMixer(model);action=mixer.clipAction(clip);action.setLoop(THREE.LoopOnce,1);action.clampWhenFinished=true;action.play();action.paused=true;$('#fold').disabled=false;document.querySelectorAll('[data-duo-screen]').forEach(b=>b.disabled=false);$('#fold-help').textContent=clip.name+' · '+clip.duration.toFixed(2)+' giây';}
  else if(activeKey==='duo')$('#fold-help').textContent='File GLB chưa chứa clip mở/gập; cần kiểm tra lại asset.';
  baseExtent=0;
  for(const p of action?[0,.25,.5,.75,1]:[0]){applyFold(p);const s=new THREE.Box3().setFromObject(model).getSize(new THREE.Vector3());baseExtent=Math.max(baseExtent,s.x,s.y,s.z);}
  applyFold(0);setFold(params.has('fold')?Number(params.get('fold')):(asset.defaultFold||0));setView(params.get('view')||asset.defaultView||'three-quarter');
  if(params.get('color'))setColorPreset(params.get('color'));
  activeBytes=bytes.byteLength;loadMs=performance.now()-start;
  api.asset={key:activeKey,url,animations:gltf.animations.map(c=>({name:c.name,duration:c.duration}))};
  api.ready=true;stateText('Đã tải · GLB thực tế');message.hidden=true;resize();draw();updateStats();
 }catch(e){if(e.name!=='AbortError'&&id===requestId)fail(e);}
}
function applyFold(p){if(action){action.enabled=true;action.paused=true;action.time=THREE.MathUtils.clamp(p,0,1)*action.getClip().duration;mixer.update(0);}model?.updateMatrixWorld(true);}
function setFold(p){p=THREE.MathUtils.clamp(Number(p)||0,0,1);$('#fold').value=Math.round(p*100);$('#fold-value').value=Math.round(p*100)+'%';if(!model)return;const old=controls.target.clone();applyFold(p);updateOuterScreenStatus();const bounds=new THREE.Box3().setFromObject(model),center=bounds.getCenter(new THREE.Vector3());controls.minDistance=bounds.getBoundingSphere(new THREE.Sphere()).radius*1.05;controls.target.copy(center);camera.position.add(center.clone().sub(old));requestRender();}
function updateOuterScreenStatus(){
 const gate=config.assets[activeKey]?.outerScreenPower,node=gate&&model?.getObjectByName(gate.node);
 const off=Boolean(node&&node.scale.lengthSq()<1e-12);
 $('#screen-power-status').hidden=!off;
 api.outerScreen=node?{power:off?'off':'on',source:'GLB animated node scale',node:node.name}:null;
}
function setView(view='three-quarter'){
 if(!model)return;model.updateMatrixWorld(true);const box=new THREE.Box3().setFromObject(model),center=box.getCenter(new THREE.Vector3());
 const distance=baseExtent/(2*Math.tan(THREE.MathUtils.degToRad(camera.fov/2)))*1.22*Math.max(1,1/camera.aspect);
 const dirs={front:[0,0,1],back:[0,0,-1],left:[-1,0,0],right:[1,0,0],top:[0,1,.001],bottom:[0,-1,.001],'three-quarter':[.62,.26,-1],'front-three-quarter':[.45,.18,1],'inner-three-quarter':[.18,.15,-1]};
 const dir=new THREE.Vector3(...(dirs[view]||dirs['three-quarter'])).normalize();camera.up.set(0,1,0);camera.position.copy(center).addScaledVector(dir,distance);controls.target.copy(center);controls.minDistance=box.getBoundingSphere(new THREE.Sphere()).radius*1.05;controls.maxDistance=distance*3;camera.near=Math.max(.0001,baseExtent*.004);camera.far=distance*30;camera.updateProjectionMatrix();controls.update();
 document.querySelectorAll('[data-view]').forEach(b=>b.classList.toggle('active',b.dataset.view===view));api.currentView=view;requestRender();
}
function setColorPreset(key){
 const preset=config.assets[activeKey]?.colors?.[key];$('#color').value=preset?key:'original';
 const changed=applyMaterialPreset(materials.values(),baseColors,preset);
 api.colorPreset={id:preset?key:'original',labelVi:preset?.labelVi||'Màu gốc',labelEn:preset?.labelEn||'Original',changedMaterials:changed,colorSpace:preset?.colorSpace||'GLB working space'};
 requestRender();
}
async function setDisplayMode(key){
 const controller=displayController;if(!controller)return;
 const assetKey=activeKey,mode=config.assets[assetKey]?.display?.modes?.[key];if(!mode)return;
 const id=++displayRequestId,section=$('#display-section'),status=$('#display-status');
 $('#display-mode').value=key;section.setAttribute('aria-busy','true');status.classList.remove('error');status.textContent='Đang đổi màn hình…';
 try{
  const result=await controller.apply(key);
  if(controller!==displayController||id!==displayRequestId)return;
  api.displayMode=result;api.displayError=null;displaySelections.set(assetKey,key);
  $('#display-help').textContent=mode.description||'Đổi nội dung màn hình, giữ nguyên góc nhìn và độ mở máy.';
  status.textContent='Đã áp dụng · '+(mode.labelVi||mode.labelEn||key);section.removeAttribute('aria-busy');requestRender();
 }catch(error){
  if(error.name==='AbortError'||controller!==displayController||id!==displayRequestId)return;
  api.displayError=String(error);$('#display-mode').value=controller.mode;status.classList.add('error');status.textContent='Không đổi được màn hình: '+error.message;section.removeAttribute('aria-busy');console.error(error);
 }
}
function setBackground(){const light=$('#background').value==='light';scene.background.set(light?'#eeeeee':'#171719');$('.stage-wrap').classList.toggle('light',light);requestRender();}
function setLighting(){const hero=$('#lighting').value==='hero';scene.environmentIntensity=hero ? .65 : .85;scene.getObjectByName('PreviewKey').intensity=hero?3.3:2.8;scene.getObjectByName('PreviewFill').intensity=hero ? .25 : .6;scene.getObjectByName('PreviewRim').intensity=hero?2.4:1.4;renderer.toneMappingExposure=hero?1.05:1.15;requestRender();}
function resize(){if(!renderer)return;const {width,height}=stage.getBoundingClientRect();if(!width||!height)return;camera.aspect=width/height;camera.updateProjectionMatrix();renderer.setPixelRatio(Math.min(devicePixelRatio,$('#variant').value==='mobile'?1.5:2));renderer.setSize(width,height,false);requestRender();}
function updateStats(){
 if(!renderer)return;const b=model?new THREE.Box3().setFromObject(model).getSize(new THREE.Vector3()):new THREE.Vector3();
 api.stats={bytes:activeBytes,loadMs:Math.round(loadMs),drawCalls:renderer.info.render.calls,triangles:renderer.info.render.triangles,materials:materials.size,meshes:meshes.length,textures:renderer.info.memory.textures,dimensionsMeters:b.toArray(),webgl:renderer.getContext().getParameter(renderer.getContext().VERSION),dpr:renderer.getPixelRatio(),viewport:[stage.clientWidth,stage.clientHeight]};
 const entries={'GLB':(activeBytes/1e6).toFixed(2)+' MB','Tải + parse':Math.round(loadMs)+' ms','Draw calls':api.stats.drawCalls,'Triangles/frame':api.stats.triangles.toLocaleString(),'Mesh / material':meshes.length+' / '+materials.size,'Texture runtime':api.stats.textures,'Kích thước hiện tại':b.toArray().map(v=>(v*1000).toFixed(1)).join(' × ')+' mm','DPR':api.stats.dpr};
 $('#stats').replaceChildren(...Object.entries(entries).flatMap(([k,v])=>{const dt=document.createElement('dt'),dd=document.createElement('dd');dt.textContent=k;dd.textContent=v;return[dt,dd];}));
}
function requestRender(){if(!frame&&!document.hidden&&renderer)frame=requestAnimationFrame(draw);}
function draw(){frame=0;if(!renderer||document.hidden)return;controls.update();renderer.render(scene,camera);updateStats();if(controls.autoRotate&&!reduced.matches)requestRender();}
window.addEventListener('pagehide',()=>{cancelAnimationFrame(frame);abort?.abort();++displayRequestId;displayController?.dispose();controls?.dispose();disposeModel(model);envTarget?.dispose();renderer?.dispose();});
