// Original BotZone illustrative geometry. CC0-1.0. Rebuild: node public/models/source/create-models.mjs
// Three.js GLTFExporter is MIT licensed. No external textures, Apple artwork or downloaded meshes.
import {Group, Mesh, MeshStandardMaterial, CylinderGeometry, SphereGeometry} from 'three';
import {RoundedBoxGeometry} from 'three/addons/geometries/RoundedBoxGeometry.js';
import {GLTFExporter} from 'three/addons/exporters/GLTFExporter.js';
import {writeFile} from 'node:fs/promises';

globalThis.FileReader = class {
  readAsArrayBuffer(blob){blob.arrayBuffer().then(value=>{this.result=value;this.onloadend?.();});}
  readAsDataURL(blob){blob.arrayBuffer().then(value=>{this.result=`data:${blob.type};base64,${Buffer.from(value).toString('base64')}`;this.onloadend?.();});}
};
const material=(name,color,metalness=.5,roughness=.28)=>{const m=new MeshStandardMaterial({color,metalness,roughness});m.name=name;return m;};
const body=material('BodyColorable','#71374d',.65), lens=material('Lens','#101722',.65,.12), rim=material('CameraRing','#a37a8a',.8,.25), flash=material('Flash','#d9d3bd',.1);
function box(group,name,size,position,mat,radius=.12){const m=new Mesh(new RoundedBoxGeometry(...size,3,radius),mat);m.name=name;m.position.set(...position);group.add(m);return m;}
function cylinder(group,name,radius,height,position,mat){const m=new Mesh(new CylinderGeometry(radius,radius,height,24),mat);m.name=name;m.position.set(...position);m.rotation.x=Math.PI/2;group.add(m);return m;}
const phone=new Group();phone.name='BotZonePhoneIllustration';
box(phone,'Body',[1.45,2.95,.15],[0,0,0],body,.19);
box(phone,'CameraIsland',[1.29,.86,.07],[0,.95,.1],body,.16);
for(const [x,y] of [[-.37,1.12],[-.37,.7],[.07,.91]]){cylinder(phone,'CameraRing',.18,.06,[x,y,.14],rim);cylinder(phone,'Lens',.145,.025,[x,y,.175],lens);}
cylinder(phone,'Flash',.065,.025,[.43,1.19,.16],flash);cylinder(phone,'Sensor',.055,.025,[.43,.68,.16],lens);
box(phone,'SideButton',[.035,.37,.05],[.732,.35,0],body,.01);
const duo=new Group();duo.name='BotZoneDuoIllustration';
const silver=material('BodyColorable','#c6c7cb',.7,.2), leftScreen=material('LeftDisplay','#789ca9',.3,.15),rightScreen=material('RightDisplay','#b2cbd1',.3,.15);
const left=new Group();left.position.x=-.68;duo.add(left);
box(left,'LeftBody',[1.35,2.05,.09],[0,0,0],silver);box(left,'LeftDisplay',[1.23,1.93,.012],[0,0,.05],leftScreen,.08);
const hinge=new Group();hinge.name='Hinge';duo.add(hinge);const right=new Group();right.position.x=.68;hinge.add(right);
box(right,'RightBody',[1.35,2.05,.09],[0,0,0],silver);box(right,'RightDisplay',[1.23,1.93,.012],[0,0,.05],rightScreen,.08);
const pivot=new Mesh(new CylinderGeometry(.036,.036,1.83,12),material('HingeMetal','#b9bdc5',.85,.15));pivot.name='HingePin';duo.add(pivot);
for(const [name,scene] of [['phone-illustration',phone],['duo-illustration',duo]]){
  const binary=await new GLTFExporter().parseAsync(scene,{binary:true,onlyVisible:true});
  await writeFile(new URL(`../${name}.glb`,import.meta.url),Buffer.from(binary));
  console.log(`${name}.glb: ${binary.byteLength} bytes`);
}
