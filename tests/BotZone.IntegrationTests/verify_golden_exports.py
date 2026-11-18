"""Independent file/BA checks for the stable sample; performs no database writes."""
import hashlib
import json
import re
import zipfile
from datetime import datetime, timezone
from decimal import Decimal
from pathlib import Path
from xml.etree import ElementTree as ET

import pymupdf

root = Path(__file__).resolve().parents[2]
expected = json.loads((root / "expected-results.json").read_text(encoding="utf-8"))
folder = root / "examples" / "exports"
manifest = json.loads((folder / "manifest.json").read_text(encoding="utf-8"))
refs = expected["invoiceRefs"]
assert manifest["rowCount"] == 8 and manifest["totalVnd"] == expected["totals"]["issuedVnd"]
assert sum(int(i["totalVnd"]) for i in expected["invoices"]) == 97_130_000
assert sum(int(m["issuedVnd"]) for m in expected["months"]) == 97_130_000
assert len(expected["months"]) == 36 and len(expected["quarters"]) == 12
for metric in ("issuedVnd", "adjustmentsVnd", "netVnd"):
    assert sum(int(x[metric]) for x in expected["months"]) == int(expected["totals"][metric])
    assert sum(int(x[metric]) for x in expected["quarters"]) == int(expected["totals"][metric])
    assert sum(int(x[metric]) for x in expected["years"]) == int(expected["totals"][metric])
for kind, entry in manifest["files"].items():
    content = (root / entry["path"]).read_bytes()
    assert len(content) == entry["bytes"] and hashlib.sha256(content).hexdigest() == entry["sha256"]

ns = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
with zipfile.ZipFile(folder / "golden.xlsx") as archive:
    strings = ["".join(si.itertext()) for si in ET.fromstring(archive.read("xl/sharedStrings.xml")).findall("s:si", ns)]
    workbook = ET.fromstring(archive.read("xl/workbook.xml"))
    relations = ET.fromstring(archive.read("xl/_rels/workbook.xml.rels"))
    targets = {r.attrib["Id"]: r.attrib["Target"] for r in relations}

    def rows(sheet_name):
        sheet = next(s for s in workbook.findall("s:sheets/s:sheet", ns) if s.attrib["name"] == sheet_name)
        target = targets[sheet.attrib["{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id"]]
        path = target.lstrip("/") if target.startswith("/") else "xl/" + target
        result = []
        for row in ET.fromstring(archive.read(path)).findall("s:sheetData/s:row", ns):
            cells = {}
            for cell in row.findall("s:c", ns):
                column = re.match(r"[A-Z]+", cell.attrib["r"]).group()
                value = cell.find("s:v", ns)
                raw = "" if value is None else value.text or ""
                cells[column] = strings[int(raw)] if cell.attrib.get("t") == "s" else raw
            result.append(cells)
        return result

    invoices = [r for r in rows("HoaDon") if r.get("A") in refs]
    assert [r["A"] for r in invoices] == refs
    assert sum(Decimal(r["H"]) for r in invoices) == 97_130_000
    assert sum(Decimal(r["I"]) for r in invoices) == 3_500_000
    assert sum(Decimal(r["J"]) for r in invoices) == 93_630_000
    items = [r for r in rows("ChiTietHoaDon") if r.get("A") in refs]
    assert len(items) == 15
    assert any(r.get("L", "").startswith("000") for r in items)
    conditions = {r.get("A"): r.get("B") for r in rows("DieuKienLoc")}
    assert conditions["invoiceCount"] == "8" and conditions["reportOrListTotalVnd"] == "97130000"
    assert conditions["readAt"] == manifest["readAt"] or datetime.fromisoformat(conditions["readAt"]) == datetime.fromisoformat(manifest["readAt"])

pdf = pymupdf.open(folder / "golden.pdf")
text = "\n".join(page.get_text() for page in pdf)
assert re.findall(r"^G\d{2}$", text, re.M) == refs
assert "97.130.000 VND" in text and "93.630.000 VND" in text
assert "Hóa đơn phù hợp" in text and "Điều kiện lọc" in text and '"pageSize"' not in text
pages = []
for index, page in enumerate(pdf):
    words = page.get_text("words")
    assert not any(w[0] < -0.5 or w[1] < -0.5 or w[2] > page.rect.width + 0.5 or w[3] > page.rect.height + 0.5 for w in words)
    page_text = page.get_text()
    if re.search(r"^TỔNG$", page_text, re.M):
        assert re.search(r"^G\d{2}$", page_text, re.M)
    image = folder / f"golden-page-{index + 1}.png"
    page.get_pixmap(matrix=pymupdf.Matrix(1.3, 1.3)).save(image)
    pages.append({"page": index + 1, "words": len(words), "outsideBounds": 0, "image": image.name})
manifest.update({"status": "verified", "verifiedAt": datetime.now(timezone.utc).isoformat(), "verification": {"expectedSource": "expected-results.json (BA literals, no DB derivation)", "xlsxInvoiceRows": 8, "xlsxDetailRows": 15, "xlsxSerialLeadingZeroPreserved": True, "issuedVnd": "97130000", "adjustmentsVnd": "3500000", "netVnd": "93630000", "bothFilesSha256Verified": True, "sameReadAtVerified": True, "pdfPages": pages}})
(folder / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(manifest["verification"], ensure_ascii=False))
