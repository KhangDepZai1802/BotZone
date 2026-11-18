"""Local visual regression helper; requires PyMuPDF already installed.
Reads actual exported PDFs; does not invent screenshots or edit source documents.
"""
import json
import re
from datetime import datetime, timezone
from pathlib import Path

import pymupdf

root = Path(__file__).resolve().parents[2]
directory = root / "artifacts" / "pdf-review-fixed"
exports = json.loads((directory / "export-results.json").read_text(encoding="utf-8"))
results = []
for sample in exports["files"]:
    name = sample["Name"]
    document = pymupdf.open(directory / f"{name}.pdf")
    pages = []
    for index, page in enumerate(document):
        text = page.get_text()
        words = page.get_text("words")
        outside = [word[:5] for word in words if word[0] < -0.5 or word[1] < -0.5 or word[2] > page.rect.width + 0.5 or word[3] > page.rect.height + 0.5]
        assert not outside, (name, index + 1, "text outside page", outside)
        assert "�" not in text, (name, index + 1, "replacement glyph")
        assert len(words) > 8, (name, index + 1, "blank/bodyless page")
        if name == "invoices" and re.search(r"^TỔNG$", text, re.M):
            assert re.search(r"\b(?:G\d{2}|PAG\d{3}|BZ-HD-\d+)\b", text), "Invoice total must share a page with an actual data row."
        if name == "report" and re.search(r"^TỔNG$", text, re.M):
            assert re.search(r"\b20\d{2}-\d{2}-\d{2}\b", text), "Report total must share a page with a period row."
        assert '"pageSize"' not in text and '"quickSearch"' not in text, "PDF filters must use readable labels."
        image = directory / f"{name}-page-{index + 1}.png"
        page.get_pixmap(matrix=pymupdf.Matrix(1.2, 1.2)).save(image)
        pages.append({"page": index + 1, "words": len(words), "outsideBounds": len(outside), "image": image.name})
    if name == "invoices":
        invoice_ids = re.findall(r"\b(?:G\d{2}|PAG\d{3}|BZ-HD-\d+)\b", "\n".join(p.get_text() for p in document))
        assert len(invoice_ids) == sample["RowCount"], ("PDF invoice rows", len(invoice_ids), sample["RowCount"])
        assert len(set(invoice_ids)) == sample["RowCount"], "Invoice IDs must not repeat across pages."
    results.append({"name": name, "pages": len(document), "passed": True, "checks": pages})
(directory / "layout-results.json").write_text(json.dumps({"checkedAt": datetime.now(timezone.utc).isoformat(), "results": results}, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(results, ensure_ascii=False))
