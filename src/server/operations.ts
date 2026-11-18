import {randomUUID} from 'node:crypto';
import {readFile,stat} from 'node:fs/promises';
import path from 'node:path';
import {z} from 'zod';
import type {Actor,LandingSection} from '../lib/contracts';
import type {Client} from './db';
import {AppError,ensure,requireRole} from './errors';

const id=z.string().uuid();
const money=z.string().regex(/^(0|[1-9]\d*)$/).refine(v=>BigInt(v)<=9223372036854775807n,'Số tiền vượt giới hạn VND.');
const positiveMoney=money.refine(v=>BigInt(v)>0n,'Số tiền phải lớn hơn 0.');
const note=z.string().trim().min(3).max(2000);
const identifier=z.string().trim().min(3).max(80).transform(v=>v.toUpperCase());
const ids=z.array(id).min(1).max(100).refine(v=>new Set(v).size===v.length,'Không chọn trùng đơn vị hàng.');
const date=z.string().datetime({offset:true});
const condition=z.enum(['new','used','faulty','opened']);
const evidenceIds=z.array(id).max(6).default([]);
const bilingual=z.object({vi:z.string().trim().min(1).max(500),en:z.string().trim().min(1).max(500)}).strict();
const sectionSchema=z.object({id:z.string().min(1).max(80),type:z.enum(['hero','duo','collection','benefits']),title:bilingual,subtitle:bilingual,productSlug:z.string().max(160),poster:z.string().min(1).max(300),model:z.string().min(1).max(300),colors:z.array(z.string().regex(/^#[0-9a-fA-F]{6}$/)).min(1).max(8),preset:z.enum(['float','rotate','fold']),camera:z.number().min(2).max(12),tilt:z.number().min(0).max(30),light:z.number().min(0.2).max(5),speed:z.number().min(0).max(3),fold:z.number().min(0).max(180)}).strict();
const landingSchema=z.object({pageSlug:z.string().regex(/^[a-z0-9-]+$/).default('home'),revision:z.number().int().nonnegative(),sections:z.array(sectionSchema).min(1).max(20).refine(v=>new Set(v.map(s=>s.id)).size===v.length,'Mã section bị trùng.')}).strict();
async function one(c:Client,sql:string,values:unknown[]=[]){const r=await c.query(sql,values);ensure(r.rows[0],'NOT_FOUND','Không tìm thấy dữ liệu.',404);return r.rows[0];}
function customer(actor:Actor){ensure(actor.verified,'FORBIDDEN','Vui lòng xác minh email trước khi gửi yêu cầu.',403);}
function owned(actor:Actor,customerId:string){ensure(actor.id===customerId,'FORBIDDEN','Hồ sơ không thuộc tài khoản này.',403);}
function code(prefix:string){return `${prefix}-${Date.now()}-${randomUUID().slice(0,8).toUpperCase()}`;}
async function attachEvidence(c:Client,actor:Actor,table:'return_evidence'|'trade_in_evidence'|'warranty_evidence',column:'return_request_id'|'inspection_id'|'claim_id',entityId:string,mediaIds:string[]){
 for(const mediaId of mediaIds){const m=await one(c,'SELECT * FROM media_assets WHERE id=$1',[mediaId]);ensure(m.visibility==='private'&&m.validation_status==='valid'&&m.owner_user_id===actor.id,'FORBIDDEN','Chỉ dùng bằng chứng riêng tư hợp lệ do bạn tải lên.',403);await c.query(`INSERT INTO ${table}(${column},media_id) VALUES($1,$2)`,[entityId,mediaId]);}
}
async function notify(c:Client,userId:string,type:string,message:string,entityId:string){await c.query('INSERT INTO notifications(recipient_user_id,type,message,entity_id) VALUES($1,$2,$3,$4)',[userId,type,message,entityId]);}

/** A credit document records the original unit allocations; it never reprices returned goods. */
async function issueCredit(c:Client,actor:Actor,orderId:string,unitIds:string[],source:{returnRequestId?:string;warrantyClaimId?:string;shipmentId?:string},reason:string,key:string){
 requireRole(actor,'owner');
 const inv=await one(c,"SELECT * FROM invoices WHERE order_id=$1 AND lifecycle='issued' FOR UPDATE",[orderId]);
 ensure(unitIds.length>0&&new Set(unitIds).size===unitIds.length,'VALIDATION_ERROR','Phải chọn các đơn vị hàng khác nhau.');
 const rows=(await c.query(`SELECT u.*,ii.id AS invoice_item_id,ii.line_type FROM order_item_unit_amounts u JOIN order_items oi ON oi.id=u.order_item_id JOIN invoice_items ii ON ii.order_item_id=oi.id WHERE oi.order_id=$1 AND u.id=ANY($2::uuid[]) ORDER BY oi.line_no,u.unit_ordinal FOR UPDATE OF u`,[orderId,unitIds])).rows;
 ensure(rows.length===unitIds.length,'VALIDATION_ERROR','Đơn vị hàng không thuộc hóa đơn/đơn này.');
 const prior=await c.query('SELECT 1 FROM credit_note_item_units WHERE order_item_unit_amount_id=ANY($1::uuid[]) LIMIT 1',[unitIds]);
 ensure(!prior.rowCount,'STATE_CONFLICT','Một món đã có chứng từ giảm hoặc đang được duyệt.');
 const total=rows.reduce((s,r)=>s+BigInt(r.net_amount_vnd),0n);
 const cn=await one(c,`INSERT INTO credit_notes(credit_note_no,invoice_id,order_id,status,return_request_id,warranty_claim_id,shipment_id,reason,total_vnd,approved_by_user_id,event_key) VALUES($1,$2,$3,'draft',$4,$5,$6,$7,$8,$9,$10) RETURNING *`,[code('CN'),inv.id,orderId,source.returnRequestId||null,source.warrantyClaimId||null,source.shipmentId||null,reason,total.toString(),actor.id,key]);
 const groups=new Map<string,typeof rows>();
 for(const row of rows){const group=groups.get(row.invoice_item_id)||[];group.push(row);groups.set(row.invoice_item_id,group);}
 for(const [invoiceItemId,units] of groups){const amount=units.reduce((s,u)=>s+BigInt(u.net_amount_vnd),0n);const item=await one(c,'INSERT INTO credit_note_items(credit_note_id,invoice_item_id,quantity,amount_vnd) VALUES($1,$2,$3,$4) RETURNING id',[cn.id,invoiceItemId,units.length,amount.toString()]);for(const u of units)await c.query('INSERT INTO credit_note_item_units(credit_note_item_id,order_item_unit_amount_id,amount_vnd) VALUES($1,$2,$3)',[item.id,u.id,u.net_amount_vnd]);}
 await c.query("UPDATE credit_notes SET status='issued',issued_at=now() WHERE id=$1",[cn.id]);
 await restorePoints(c,orderId,rows);
 return {creditNoteId:cn.id,creditNoteNo:cn.credit_note_no,totalVnd:total.toString(),unitIds};
}
async function restorePoints(c:Client,orderId:string,units:Awaited<ReturnType<Client['query']>>['rows']){
 const o=await one(c,'SELECT customer_id,policy_snapshot FROM orders WHERE id=$1',[orderId]);
 await c.query('SELECT 1 FROM loyalty_accounts WHERE user_id=$1 FOR UPDATE',[o.customer_id]);
 const redeemed=await c.query("SELECT 1 FROM loyalty_entries WHERE order_id=$1 AND kind='redeem' LIMIT 1",[orderId]);
 if(redeemed.rowCount)for(const u of units){if(u.redeemed_points>0){const e=await c.query("INSERT INTO loyalty_entries(user_id,delta,kind,order_id,unit_id,event_key) VALUES($1,$2,'restore',$3,$4,$5) ON CONFLICT(event_key) DO NOTHING RETURNING id",[o.customer_id,u.redeemed_points,orderId,u.id,`points-restore:${u.id}`]);if(e.rowCount)await c.query('UPDATE loyalty_accounts SET balance_points=balance_points+$2 WHERE user_id=$1',[o.customer_id,u.redeemed_points]);}}
 const earned=await one(c,"SELECT COALESCE(sum(delta),0)::int AS points FROM loyalty_entries WHERE order_id=$1 AND kind='earn'",[orderId]);
 if(earned.points>0){
  const totals=await one(c,`SELECT (SELECT COALESCE(sum(net_amount_vnd),0) FROM order_items WHERE order_id=$1 AND line_type='product')::text gross,(SELECT COALESCE(sum(cu.amount_vnd),0) FROM credit_note_item_units cu JOIN credit_note_items ci ON ci.id=cu.credit_note_item_id JOIN invoice_items ii ON ii.id=ci.invoice_item_id JOIN credit_notes cn ON cn.id=ci.credit_note_id WHERE cn.order_id=$1 AND cn.status='issued' AND ii.line_type='product')::text reduced,(SELECT -COALESCE(sum(delta),0) FROM loyalty_entries WHERE order_id=$1 AND kind='reverse')::int reversed`,[orderId]);
  const gross=BigInt(totals.gross),reduced=BigInt(totals.reduced);
  const remaining=gross-reduced;
  const multiplier=String(o.policy_snapshot.loyaltyMultiplier||o.policy_snapshot.tierMultiplier||'1');
  const [whole,fraction='']=multiplier.split('.');const scale=10n**BigInt(fraction.length);const rate=BigInt(whole)*scale+BigInt(fraction||'0');
  const retained=Number(remaining*rate/(100000n*scale));const desired=Math.max(0,earned.points-retained);const delta=desired-totals.reversed;
  if(delta>0){await c.query("INSERT INTO loyalty_entries(user_id,delta,kind,order_id,event_key) VALUES($1,$2,'reverse',$3,$4)",[o.customer_id,-delta,orderId,`points-reverse:${orderId}:${desired}`]);await c.query('UPDATE loyalty_accounts SET balance_points=balance_points-$2 WHERE user_id=$1',[o.customer_id,delta]);}
  const returnedProducts=units.filter(u=>u.line_type==='product').reduce((s,u)=>s+BigInt(u.net_amount_vnd),0n);
  if(returnedProducts>0n)await c.query('UPDATE loyalty_accounts SET net_spend_vnd=GREATEST(0,net_spend_vnd-$2::bigint) WHERE user_id=$1',[o.customer_id,returnedProducts.toString()]);
 }
}
async function refundCredit(c:Client,actor:Actor,creditNoteId:string,key:string,outcome:'success'|'failed'='success'){
 requireRole(actor,'owner');ensure(process.env.DEMO_MODE==='true','FORBIDDEN','Chỉ được hoàn qua mô phỏng local.',403);
 const cn=await one(c,"SELECT cn.*,o.customer_id FROM credit_notes cn JOIN orders o ON o.id=cn.order_id WHERE cn.id=$1 AND cn.status='issued' FOR UPDATE OF cn,o",[creditNoteId]);
 const existing=await c.query("SELECT id,status,amount_vnd FROM refunds WHERE credit_note_id=$1 AND status<>'failed'",[creditNoteId]);
 if(existing.rows[0])return {refundId:existing.rows[0].id,status:existing.rows[0].status,totalVnd:existing.rows[0].amount_vnd};
 const funds=(await c.query(`SELECT ta.receipt_id,ta.order_item_unit_amount_id,pr.tender_kind,(ta.amount_vnd-COALESCE((SELECT sum(ra.amount_vnd) FROM refund_allocations ra JOIN refunds r ON r.id=ra.refund_id WHERE ra.payment_receipt_id=ta.receipt_id AND ra.order_item_unit_amount_id=ta.order_item_unit_amount_id AND r.status<>'failed'),0))::text remaining FROM order_tender_allocations ta JOIN payment_receipts pr ON pr.id=ta.receipt_id JOIN credit_note_item_units cu ON cu.order_item_unit_amount_id=ta.order_item_unit_amount_id JOIN credit_note_items ci ON ci.id=cu.credit_note_item_id WHERE ci.credit_note_id=$1 ORDER BY ta.order_item_unit_amount_id,ta.receipt_id`,[creditNoteId])).rows.filter(r=>BigInt(r.remaining)>0n);
 const total=funds.reduce((s,r)=>s+BigInt(r.remaining),0n);
 ensure(total<=BigInt(cn.total_vnd),'STATE_CONFLICT','Nguồn hoàn vượt chứng từ đã duyệt.');
 if(total===0n)return {refundId:null,totalVnd:'0',status:'no_payment_to_refund'};
 const rf=await one(c,`INSERT INTO refunds(order_id,credit_note_id,status,amount_vnd,approved_by,completed_at,idempotency_key) VALUES($1,$2,$3,$4,$5,CASE WHEN $3='completed' THEN now() ELSE NULL END,$6) RETURNING id`,[cn.order_id,creditNoteId,outcome==='success'?'completed':'failed',total.toString(),actor.id,key]);
 let credit=0n,cash=0n;
 for(const f of funds){await c.query('INSERT INTO refund_allocations(refund_id,payment_receipt_id,order_item_unit_amount_id,amount_vnd) VALUES($1,$2,$3,$4)',[rf.id,f.receipt_id,f.order_item_unit_amount_id,f.remaining]);if(f.tender_kind==='store_credit')credit+=BigInt(f.remaining);else cash+=BigInt(f.remaining);}
 if(outcome==='success'){
  if(credit>0n){await c.query('SELECT 1 FROM store_credit_accounts WHERE user_id=$1 FOR UPDATE',[cn.customer_id]);await c.query("INSERT INTO store_credit_entries(user_id,delta_vnd,kind,order_id,refund_id,event_key) VALUES($1,$2,'refund',$3,$4,$5)",[cn.customer_id,credit.toString(),cn.order_id,rf.id,`refund-credit:${rf.id}`]);await c.query('UPDATE store_credit_accounts SET balance_vnd=balance_vnd+$2 WHERE user_id=$1',[cn.customer_id,credit.toString()]);}
  if(cn.return_request_id)await c.query("UPDATE return_requests SET state='closed' WHERE id=$1",[cn.return_request_id]);
  if(cn.warranty_claim_id)await c.query("UPDATE warranty_claims SET state='refunded' WHERE id=$1",[cn.warranty_claim_id]);
  await notify(c,cn.customer_id,'refund','Khoản hoàn mô phỏng đã hoàn tất, đúng nguồn thanh toán.',cn.order_id);
 }
 return {refundId:rf.id,status:outcome==='success'?'completed':'failed',totalVnd:total.toString(),creditVnd:credit.toString(),cashVnd:cash.toString()};
}

async function validateLanding(c:Client,sections:LandingSection[]){
 const media=new Set<string>();
 for(const section of sections){
  if(['hero','duo'].includes(section.type))ensure(section.productSlug,'VALIDATION_ERROR','Section sản phẩm cần chọn model.');
  if(section.productSlug)await one(c,'SELECT id FROM products WHERE slug=$1',[section.productSlug]);
  for(const [kind,value] of [['poster',section.poster],['model',section.model]] as const){
   if(kind==='model'&&['procedural:phone','procedural:duo','phone','duo'].includes(value)){ensure(section.type!=='duo'||value.endsWith('duo'),'VALIDATION_ERROR','Section Duo cần model có bản lề mở/gập.');continue;}
   if(kind==='poster'&&/^\/media\/[a-zA-Z0-9_-]+\.(svg|png|jpg|jpeg|webp)$/.test(value)){const file=path.resolve('public','.'+value);ensure((await stat(file).catch(()=>null))?.isFile(),'VALIDATION_ERROR','Không tìm thấy ảnh poster local.');continue;}
   const assetId=value.match(/^(?:media:|\/api\/media\/)([0-9a-f-]{36})$/i)?.[1];
   ensure(assetId,'VALIDATION_ERROR','Chọn tài nguyên trong thư viện local đã kiểm tra.');
   const asset=await one(c,'SELECT * FROM media_assets WHERE id=$1',[assetId]);
   ensure(asset.visibility==='public'&&asset.validation_status==='valid','VALIDATION_ERROR','Tài nguyên chưa được duyệt công khai.');
   const root=path.resolve(process.env.UPLOAD_DIR||'uploads'),file=path.resolve(root,asset.storage_key);
   ensure(file.startsWith(root+path.sep),'VALIDATION_ERROR','Đường dẫn tài nguyên không hợp lệ.');
   ensure((await stat(file).catch(()=>null))?.isFile(),'VALIDATION_ERROR','Tệp tài nguyên không còn trên máy.');
   if(kind==='model'){
    ensure(asset.mime==='model/gltf-binary','VALIDATION_ERROR','Model cần là GLB đã kiểm tra.');
    const bytes=await readFile(file);ensure(bytes.length>=20&&bytes.toString('ascii',0,4)==='glTF'&&bytes.readUInt32LE(4)===2&&bytes.readUInt32LE(8)===bytes.length,'VALIDATION_ERROR','GLB không hợp lệ.');
    const json=JSON.parse(bytes.subarray(20,20+bytes.readUInt32LE(12)).toString('utf8'));
    ensure(![...(json.buffers||[]),...(json.images||[])].some((v:{uri?:string})=>v.uri&&!v.uri.startsWith('data:')),'VALIDATION_ERROR','GLB phải chứa texture local trong tệp.');
    ensure(section.type!=='duo'||asset.metadata.supportsFold===true,'VALIDATION_ERROR','Model chưa có bản lề/animation mở gập cho Duo.');
    ensure(section.colors.length<2||asset.metadata.supportsColor===true,'VALIDATION_ERROR','Model chưa đánh dấu vật liệu để đổi màu.');
   }else ensure(['image/jpeg','image/png','image/webp'].includes(asset.mime),'VALIDATION_ERROR','Poster phải là ảnh đã kiểm tra.');
   media.add(assetId);
  }
 }
 return [...media];
}

export async function handleOperation(c:Client,actor:Actor,command:string,input:unknown,key:string):Promise<unknown|undefined>{
 switch(command){
  case 'save_address':{
   const p=z.object({id:id.optional(),recipient:z.string().trim().min(2).max(100),phone:z.string().regex(/^[+0-9 ()-]{8,20}$/),province:z.string().min(2).max(100),ward:z.string().min(2).max(100),addressLine:z.string().min(5).max(300),country:z.literal('VN').default('VN'),isDefault:z.boolean().default(false)}).strict().parse(input);
   if(p.id){const a=await one(c,'SELECT user_id FROM addresses WHERE id=$1 FOR UPDATE',[p.id]);owned(actor,a.user_id);}
   if(p.isDefault)await c.query('UPDATE addresses SET is_default=false WHERE user_id=$1',[actor.id]);
   if(p.id)return one(c,'UPDATE addresses SET recipient=$2,phone=$3,province=$4,ward=$5,address_line=$6,is_default=$7 WHERE id=$1 RETURNING *',[p.id,p.recipient,p.phone,p.province,p.ward,p.addressLine,p.isDefault]);
   return one(c,'INSERT INTO addresses(user_id,recipient,phone,province,ward,address_line,is_default) VALUES($1,$2,$3,$4,$5,$6,$7) RETURNING *',[actor.id,p.recipient,p.phone,p.province,p.ward,p.addressLine,p.isDefault]);
  }
  case 'update_variant':{
   requireRole(actor,'owner');const p=z.object({variantId:id,priceVnd:money,active:z.boolean(),careFeeVnd:money.nullable(),standardWarrantyMonths:z.number().int().min(0).max(60)}).strict().parse(input);
   return one(c,'UPDATE product_variants SET price_vnd=$2,active=$3,care_fee_vnd=$4,standard_warranty_months=$5 WHERE id=$1 RETURNING id,sku,price_vnd,active,care_fee_vnd,standard_warranty_months',[p.variantId,p.priceVnd,p.active,p.careFeeVnd,p.standardWarrantyMonths]);
  }
  case 'update_translation':{
   requireRole(actor,'content_staff');const p=z.object({productId:id,locale:z.enum(['vi','en']),name:z.string().trim().min(1).max(200),description:z.string().trim().min(1).max(10000),seoTitle:z.string().trim().min(1).max(200),contentStatus:z.enum(['draft','published'])}).strict().parse(input);
   await one(c,'SELECT id FROM products WHERE id=$1',[p.productId]);return one(c,'INSERT INTO product_translations(product_id,locale,name,description,seo_title,content_status) VALUES($1,$2,$3,$4,$5,$6) ON CONFLICT(product_id,locale) DO UPDATE SET name=excluded.name,description=excluded.description,seo_title=excluded.seo_title,content_status=excluded.content_status RETURNING *',[p.productId,p.locale,p.name,p.description,p.seoTitle,p.contentStatus]);
  }
  case 'update_staff':{
   requireRole(actor,'owner');const p=z.object({userId:z.string().min(1),staffCode:z.string().regex(/^[A-Z0-9_-]{2,30}$/),role:z.enum(['owner','order_staff','warehouse_staff','content_staff']),active:z.boolean(),reason:note}).strict().parse(input);
   await one(c,'SELECT id FROM "user" WHERE id=$1',[p.userId]);const previous=(await c.query('SELECT * FROM staff_profiles WHERE user_id=$1 FOR UPDATE',[p.userId])).rows[0];
   if(previous?.role==='owner'&&previous.active&&(!p.active||p.role!=='owner')){const owners=await one(c,"SELECT count(*)::int n FROM staff_profiles WHERE role='owner' AND active");ensure(owners.n>1,'STATE_CONFLICT','Phải giữ ít nhất một chủ cửa hàng hoạt động.');}
   await c.query('INSERT INTO staff_profiles(user_id,staff_code,role,active) VALUES($1,$2,$3,$4) ON CONFLICT(user_id) DO UPDATE SET staff_code=excluded.staff_code,role=excluded.role,active=excluded.active',[p.userId,p.staffCode,p.role,p.active]);
   await c.query('DELETE FROM session WHERE user_id=$1',[p.userId]);return {userId:p.userId,role:p.role,active:p.active,sessionsRevoked:true};
  }
  case 'create_promotion':{
   requireRole(actor,'owner');const target=z.object({variantId:id.optional(),productId:id.optional(),categoryId:id.optional()}).strict().refine(v=>[v.variantId,v.productId,v.categoryId].filter(Boolean).length===1);
   const p=z.object({code:z.string().trim().min(2).max(40).transform(v=>v.toUpperCase()),discountType:z.enum(['fixed','percentage']),value:positiveMoney,maxDiscountVnd:money.nullable().default(null),minPurchaseVnd:money.default('0'),startsAt:date,endsAt:date,maxUses:z.number().int().positive().nullable().default(null),maxUsesPerCustomer:z.number().int().positive().nullable().default(null),allProducts:z.boolean().default(true),targets:z.array(target).max(100).default([]),active:z.boolean().default(true)}).strict().parse(input);
   ensure(new Date(p.startsAt)<new Date(p.endsAt),'VALIDATION_ERROR','Ngày kết thúc phải sau ngày bắt đầu.');ensure(p.discountType!=='percentage'||BigInt(p.value)<=100n,'VALIDATION_ERROR','Phần trăm không được vượt 100.');ensure(p.allProducts?p.targets.length===0:p.targets.length>0,'VALIDATION_ERROR','Chọn phạm vi áp dụng nhất quán.');
   const promo=await one(c,'INSERT INTO promotions(code_normalized,active,max_uses,max_uses_per_customer) VALUES($1,$2,$3,$4) ON CONFLICT(code_normalized) DO UPDATE SET active=excluded.active,max_uses=excluded.max_uses,max_uses_per_customer=excluded.max_uses_per_customer RETURNING id',[p.code,p.active,p.maxUses,p.maxUsesPerCustomer]);
   const version=await one(c,'INSERT INTO promotion_versions(promotion_id,version_no,discount_type,value,max_discount_vnd,min_purchase_vnd,starts_at,ends_at,all_products) SELECT $1,COALESCE(max(version_no),0)+1,$2,$3,$4,$5,$6,$7,$8 FROM promotion_versions WHERE promotion_id=$1 RETURNING *',[promo.id,p.discountType,p.value,p.maxDiscountVnd,p.minPurchaseVnd,p.startsAt,p.endsAt,p.allProducts]);
   for(const t of p.targets)await c.query('INSERT INTO promotion_targets(promotion_version_id,variant_id,product_id,category_id) VALUES($1,$2,$3,$4)',[version.id,t.variantId||null,t.productId||null,t.categoryId||null]);return {promotionId:promo.id,versionId:version.id,version:version.version_no};
  }
  case 'save_landing':{
   requireRole(actor,'content_staff');const p=landingSchema.parse(input);await validateLanding(c,p.sections);
   const page=await one(c,'SELECT id FROM landing_pages WHERE slug=$1',[p.pageSlug]);
   const draft=(await c.query('UPDATE landing_drafts SET sections=$3,revision=revision+1,updated_by=$4,updated_at=now() WHERE page_id=$1 AND revision=$2 RETURNING revision,sections',[page.id,p.revision,JSON.stringify(p.sections),actor.id])).rows[0];
   ensure(draft,'STATE_CONFLICT','Bản nháp đã được người khác sửa. Tải lại trước khi lưu.');return draft;
  }
  case 'publish_landing':
  case 'restore_landing':{
   requireRole(actor,'content_staff');const p=z.object({pageSlug:z.string().regex(/^[a-z0-9-]+$/).default('home'),revision:z.number().int().nonnegative(),versionId:id.optional()}).strict().parse(input);
   const d=await one(c,'SELECT d.*,p.slug FROM landing_drafts d JOIN landing_pages p ON p.id=d.page_id WHERE p.slug=$1 FOR UPDATE OF d',[p.pageSlug]);
   ensure(d.revision===p.revision,'STATE_CONFLICT','Bản nháp đã thay đổi. Hãy xem trước bản mới.');let sections=d.sections;
   if(command==='restore_landing'){ensure(p.versionId,'VALIDATION_ERROR','Chọn phiên bản cần khôi phục.');const old=await one(c,'SELECT sections FROM landing_versions WHERE id=$1 AND page_id=$2',[p.versionId,d.page_id]);sections=old.sections;}
   const parsed=landingSchema.parse({pageSlug:p.pageSlug,revision:p.revision,sections});const mediaIds=await validateLanding(c,parsed.sections);
   const version=await one(c,'INSERT INTO landing_versions(page_id,version_no,sections,created_by) SELECT $1,COALESCE(max(version_no),0)+1,$2,$3 FROM landing_versions WHERE page_id=$1 RETURNING id,version_no',[d.page_id,JSON.stringify(parsed.sections),actor.id]);
   for(const mediaId of mediaIds)await c.query('INSERT INTO landing_media_refs(landing_version_id,media_id) VALUES($1,$2)',[version.id,mediaId]);
   await c.query('INSERT INTO landing_publications(page_id,version_id,published_by) VALUES($1,$2,$3)',[d.page_id,version.id,actor.id]);
   await c.query('UPDATE landing_drafts SET sections=$2,revision=revision+1,updated_by=$3,updated_at=now() WHERE page_id=$1',[d.page_id,JSON.stringify(parsed.sections),actor.id]);return {versionId:version.id,version:version.version_no,revision:p.revision+1};
  }
  default: return handleAfterSales(c,actor,command,input,key);
 }
}

async function handleAfterSales(c:Client,actor:Actor,command:string,input:unknown,key:string):Promise<unknown|undefined>{
 switch(command){
  default:return handleInventory(c,actor,command,input,key);
 }
}
async function handleInventory(c:Client,actor:Actor,command:string,input:unknown,key:string):Promise<unknown|undefined>{
 switch(command){
  default:return undefined;
 }
}
