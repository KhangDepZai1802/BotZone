"""Read-only PostgreSQL schema documentation. Never prints or writes credentials."""
import argparse, hashlib, html, json, os, re, shutil, subprocess
from urllib.parse import urlparse, unquote
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'docs/database'
def capture(database, config, owner_env):
    settings=json.loads((ROOT/config).read_text(encoding='utf-8-sig'))
    fields=dict(part.split('=',1) for part in settings['ConnectionStrings']['BotZone'].split(';') if '=' in part)
    fields={key.strip().lower():value.strip() for key,value in fields.items()}
    env=os.environ.copy()
    for key,source in [('PGHOST','host'),('PGPORT','port'),('PGUSER','username'),('PGPASSWORD','password')]:
        if source in fields: env[key]=fields[source]
    if owner_env:
        values=dict(line.split('=',1) for line in (ROOT/owner_env).read_text(encoding='utf-8-sig').splitlines() if '=' in line and not line.lstrip().startswith('#'))
        owner=urlparse(values['MIGRATION_DATABASE_URL'].strip().strip('"').strip("'"))
        env.update(PGHOST=owner.hostname,PGPORT=str(owner.port or 5432),PGUSER=unquote(owner.username or ''),PGPASSWORD=unquote(owner.password or ''))
    env.update(PGDATABASE=database or fields['database'],PGCLIENTENCODING='UTF8',PGOPTIONS='-c default_transaction_read_only=on')
    psql=shutil.which('psql') or r'C:\Program Files\PostgreSQL\18\bin\psql.exe'
    process=subprocess.run([psql,'-X','-q','-A','-t','-v','ON_ERROR_STOP=1','-f',str(OUT/'capture-schema.sql')],env=env,capture_output=True,encoding='utf-8')
    if process.returncode: raise RuntimeError('Read-only schema capture failed: '+process.stderr)
    data=json.loads(process.stdout)
    assert data['transactionReadOnly']=='on'
    path=OUT/'schema-snapshot.json';path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    return data

def group(name):
    if name in {'user','account','session','verification','auth_sessions','staff_profiles','customer_profiles','addresses'} or name.startswith(('AspNet','identity_')):return 'Identity & people'
    if name.startswith(('product','variant','attribute','categor','compatib')):return 'Catalogue'
    if name.startswith(('landing','media')):return 'Landing & media'
    if name.startswith(('cart','wishlist','promotion','benefit')):return 'Cart & promotions'
    if name.startswith(('order','payment','shipment')):return 'Orders & payments'
    if name.startswith(('invoice','credit_note','refund')):return 'Invoices & refunds'
    if name.startswith(('inventory','stock','purchase','goods','supplier','warehouse')):return 'Inventory & purchasing'
    if name.startswith(('return','trade_in')):return 'Returns & trade in'
    if name.startswith('warranty'):return 'Warranty'
    if name.startswith(('store_credit','loyalty')):return 'Customer ledgers'
    return 'System & audit'

def generate(data):
    tables=data['tables'];fk=[(t,k) for t in tables for k in t['constraints'] if k['kind']=='f']
    cols=sum(len(t['columns']) for t in tables);indices=sum(len(t['indexes']) for t in tables)
    digest=hashlib.sha256((OUT/'schema-snapshot.json').read_bytes()).hexdigest()
    meta=f"Captured `{data['capturedAt']}` from `{data['database']}` as `{data['reader']}` using a repeatable-read, read-only transaction. Public schema: **{len(tables)} tables, {cols} columns, {len(fk)} foreign keys, {indices} indexes**. Metadata only; no customer/order rows or credentials exported."
    escape=lambda x:str(x if x is not None else '—').replace('|','\\|').replace('\n','<br>')
    doc=['# BotZone — actual PostgreSQL data dictionary','',meta,'',f"Snapshot SHA-256: `{digest}`.",'','Source of truth: [schema-snapshot.json](schema-snapshot.json), captured by [capture-schema.sql](capture-schema.sql). EF model: `src/BotZone.Infrastructure/Data/BotZoneDbContext.cs`, partial mappings and `Migrations/BotZoneDbContextModelSnapshot.cs`. Database defaults/checks/triggers below are catalog evidence; do not infer them from C# nullable annotations.','', '## Migration history','']
    doc += [f"- `{m['MigrationId']}` — EF `{m['ProductVersion']}`" for m in data['migrations']]
    doc += ['','## Reading this dictionary','','`NULL=Yes` means PostgreSQL permits null. `—` means no default/comment/constraint entry exists. Constraint types: `p` primary key, `f` foreign key, `u` unique, `c` CHECK, `n` NOT NULL, `x` exclusion. FK definitions include update/delete actions; `DEFERRABLE`/`INITIALLY DEFERRED` are recorded separately. Partial/functional indexes are preserved verbatim. `bigint` VND values cross the API as decimal strings. PostgreSQL enum labels are listed at the end.','','## Table index','']
    groups={}
    for t in tables:groups.setdefault(group(t['name']),[]).append(t)
    for g,items in groups.items():doc += [f"- **{g}:** "+', '.join(f"[{t['name']}](#{t['name'].lower()})" for t in items)]
    for t in tables:
        doc+=['',f"## {t['name']}",'',f"Domain: {group(t['name'])}."+(f" {t['comment']}" if t['comment'] else ''),'','| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |','| --- | --- | --- | --- | --- | --- | --- |']
        for c in t['columns']:
            keys=[('PK' if k['kind']=='p' else 'FK → '+str(k['referencedTable']) if k['kind']=='f' else 'UNIQUE')+': '+k['name'] for k in t['constraints'] if k['kind'] in 'pfu' and c['name'] in (k['columns'] or [])]
            doc.append('| '+' | '.join(map(escape,[c['name'],c['type'],'Yes' if c['nullable'] else 'No',c['default'],'; '.join(keys) or None,('/'.join(filter(None,[c['identity'],c['generated']]))) or None,c['comment']]))+' |')
        for heading,records in [('Constraints',t['constraints']),('Indexes',t['indexes']),('Triggers',t['triggers'])]:
            doc+=['',f'### {heading}','']
            if not records:doc+=['None.'];continue
            for k in records:
                doc += [f"**`{k['name']}`**"+(f" — type `{k['kind']}`, validated `{k['validated']}`, deferrable `{k['deferrable']}`, initially deferred `{k['initiallyDeferred']}`." if heading=='Constraints' else f" — unique `{k['unique']}`, primary `{k['primary']}`, valid `{k['valid']}`." if heading=='Indexes' else f" — enabled `{k['enabled']}`."),'','```sql',k['definition'],'```','']
    doc+=['','## Enum types','']
    for e in data['enums']:doc += [f"- **`{e['name']}`:** "+', '.join(f"`{label}`" for label in e['labels'])]
    doc+=['','## Views','']
    if not data['views']:doc+=['No public views/materialized views were present at capture.']
    for v in data['views']:doc += [f"### {v['name']}",'','```sql',v['definition'],'```']
    doc+=['','## Database functions','','Full function bodies are preserved in `schema-snapshot.json`; these include trigger guards that enforce financial/custody invariants beyond ordinary CHECK/FK constraints.','']
    doc += [f"- `{f['name']}({f['signature']})`" for f in data['functions']]
    (OUT/'data-dictionary.md').write_text('\n'.join(doc)+'\n',encoding='utf-8')
    # Local standalone SVG: every physical table and every FK edge. Domain cards
    # keep names legible; hover an edge/table for the exact constraint/columns.
    palette=['#6b263d','#315a74','#3e6b55','#725b2e','#59528a','#866071','#3c6875','#817044','#586a45','#6b597c','#58616b']
    positions={};cards=[];width=2200;y=90
    grouped=list(groups.items())
    for batch in range(0,len(grouped),3):
        row=grouped[batch:batch+3];height=max(100+len(items)*55 for _,items in row)
        for col,(g,items) in enumerate(row):
            x=30+col*725;color=palette[(batch+col)%len(palette)];cards.append((x,y,695,height-18,g,color))
            for i,t in enumerate(items):positions[t['name']]=(x+20,y+52+i*55,655,43,color)
        y+=height+18
    svg=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{y+90}" viewBox="0 0 {width} {y+90}" role="img" aria-labelledby="title desc">','<title id="title">BotZone physical PostgreSQL ERD</title>',f'<desc id="desc">{len(tables)} tables and {len(fk)} foreign keys captured from the database. Arrows point from child to referenced parent. Hover table or relationship for metadata.</desc>','<defs><marker id="arrow" viewBox="0 0 8 8" refX="7" refY="4" markerWidth="6" markerHeight="6" orient="auto"><path d="M0,0 L8,4 L0,8 Z" fill="#68717c"/></marker></defs>','<style>text{font-family:Segoe UI,Arial,sans-serif}.edge{fill:none;stroke:#68717c;stroke-width:1;opacity:.16;marker-end:url(#arrow)}.edge:hover{stroke:#a11c45;stroke-width:4;opacity:1}.node:hover rect{stroke:#111;stroke-width:3}.heading{font-size:23px;font-weight:600}.table{font-size:17px;font-weight:600}.meta{font-size:11px;fill:#59616a}</style>',f'<rect width="100%" height="100%" fill="#f5f6f8"/><text x="30" y="35" font-size="27" font-weight="700">BotZone · physical ERD</text><text x="30" y="61" font-size="14">{len(tables)} tables · {len(fk)} foreign keys · child → parent · hover for full definition · zoom in to inspect</text>']
    for x,yy,w,h,g,c in cards:svg += [f'<rect x="{x}" y="{yy}" width="{w}" height="{h}" rx="14" fill="white" stroke="#d5d9df"/><text class="heading" x="{x+20}" y="{yy+32}" fill="{c}">{html.escape(g)}</text>']
    for t,k in fk:
        if k['referencedTable'] not in positions:continue
        ax,ay,aw,ah,_=positions[t['name']];bx,by,bw,bh,_=positions[k['referencedTable']];start=(ax+aw,ay+ah/2);end=(bx+bw,by+bh/2);bend=max(start[0],end[0])+20+(sum(map(ord,k['name']))%45)
        svg+=[f'<path class="edge" d="M{start[0]},{start[1]} C{bend},{start[1]} {bend},{end[1]} {end[0]},{end[1]}"><title>{html.escape(t["name"]+": "+k["name"]+" — "+k["definition"])}</title></path>']
    for t in tables:
        x,yy,w,h,c=positions[t['name']];pk=', '.join(col for k in t['constraints'] if k['kind']=='p' for col in k['columns']);detail='\n'.join(f"{a['name']}: {a['type']}"+(' NULL' if a['nullable'] else ' NOT NULL') for a in t['columns'])
        svg += [f'<g class="node"><title>{html.escape(t["name"]+"\n"+detail)}</title><rect x="{x}" y="{yy}" width="{w}" height="{h}" rx="7" fill="#fafbfc" stroke="{c}"/><text class="table" x="{x+12}" y="{yy+19}" fill="#20242b">{html.escape(t["name"])}</text><text class="meta" x="{x+12}" y="{yy+35}">{html.escape("PK: "+(pk or "none")+" · "+str(len(t['columns']))+" columns")}</text></g>']
    svg += [f'<text x="30" y="{y+30}" font-size="13">Captured {html.escape(data["capturedAt"])} · Full column/default/check/index definitions: data-dictionary.md</text>','</svg>'];(OUT/'erd.svg').write_text('\n'.join(svg),encoding='utf-8')
    md=['# BotZone database ERD','',meta,'','[Open the standalone SVG](erd.svg) · [Full data dictionary](data-dictionary.md) · [Raw captured metadata](schema-snapshot.json)','','The SVG includes every physical public table and foreign key. Arrows go from the referencing child to the referenced parent; hover an edge for the complete FK definition or a table for its columns. Use browser zoom for the full graph. The domain diagrams below omit cross-domain edges to keep each view readable; the relationship register includes all edges, including composite keys and those crossing domains.','','## Reproduce from the local clone','','```powershell','python docs/database/capture-schema.py --database '+data['database']+' --owner-env config/legacy.env','```','','Credentials are read from ignored local configuration and passed only through the child process environment. `--owner-env` reads only `MIGRATION_DATABASE_URL` from the preserved ignored configuration because the runtime role cannot SELECT `__EFMigrationsHistory`; it does not run legacy migrations. The script sets `default_transaction_read_only=on`; SQL uses `BEGIN ... REPEATABLE READ READ ONLY` and ends with `ROLLBACK`. It writes only local documentation. To regenerate documents from the captured evidence without connecting:','','```powershell','python docs/database/capture-schema.py --from-snapshot','```','','The captured five EF migrations are listed in the dictionary. Existing legacy account/session tables are retained physical objects for preservation; ASP.NET Identity plus `auth_sessions` is the active authentication path. `schema_migrations` records historical SQL adoption; `__EFMigrationsHistory` owns future migrations. No schema/model/API changes are made by this generator.','']
    for g,items in groups.items():
        names={t['name'] for t in items};md += [f'## {g}','','```mermaid','erDiagram']
        for t in items:
            alias=re.sub('[^A-Za-z0-9_]','_',t['name']);pk={col for k in t['constraints'] if k['kind']=='p' for col in k['columns']};shown=[c for c in t['columns'] if c['name'] in pk] or t['columns'][:1]
            md += [f'  {alias} {{']
            for column in shown:md += [f'    {re.sub("[^A-Za-z0-9_]","_",column["type"])} {column["name"]}'+(' PK' if column['name'] in pk else '')]
            md += ['  }']
        for t,k in fk:
            if t['name'] in names and k['referencedTable'] in names:
                nullable=any(c['nullable'] for c in t['columns'] if c['name'] in k['columns']);parent='o|' if nullable else '||';label=', '.join(k['columns']);md += [f'  {k["referencedTable"]} {parent}--o{{ {t["name"]} : "{label}"']
        md += ['```','']
    md += ['## Complete foreign-key register','','Cardinality diagrams use `0..many` on the child side as a conservative overview. Unique FK constraints that narrow a relation to one-to-one are specified exactly in the dictionary. A nullable FK may have no parent; composite-FK semantics and delete/update actions are preserved below.','','| Child table | Columns | Parent table | Referenced columns | Constraint / actions |','| --- | --- | --- | --- | --- |']
    for t,k in fk:md += ['| '+' | '.join(map(escape,[t['name'],', '.join(k['columns']),k['referencedTable'],', '.join(k['referencedColumns']),k['name']+': '+k['definition']]))+' |']
    (OUT/'erd.md').write_text('\n'.join(md)+'\n',encoding='utf-8')
    print(json.dumps({'database':data['database'],'readOnly':data['transactionReadOnly'],'tables':len(tables),'columns':cols,'foreignKeys':len(fk),'indexes':indices,'migrations':len(data['migrations']),'snapshotSha256':digest}))

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--database');parser.add_argument('--config',default='config/backend.local.json');parser.add_argument('--owner-env');parser.add_argument('--from-snapshot',action='store_true');args=parser.parse_args()
    generate(json.loads((OUT/'schema-snapshot.json').read_text(encoding='utf-8')) if args.from_snapshot else capture(args.database,args.config,args.owner_env))
