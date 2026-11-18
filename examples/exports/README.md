# Mẫu xuất golden thực

[golden.xlsx](golden.xlsx) và [golden.pdf](golden.pdf) được tạo trong **một lần xuất của API C#**, dùng owner đã xác thực và bộ lọc `invoiceNo = G01 OR ... OR invoiceNo = G08`. Không chứa75 PAG hoặc các hóa đơn thử giao dịch khác.

- 8 hóa đơn, tổng phát hành **97.130.000 VND**.
- 15 dòng chi tiết trong XLSX; số0 đầu serial còn nguyên.
- Chứng từ giảm **3.500.000 VND**, ròng **93.630.000 VND**.
- PDF một trang; đã render, kiểm chữ không vượt biên, quan sát tiếng Việt và dòng tổng cùng bảng.
- Hai file cùng `exportId` và `readAt`; file size/SHA-256 và kết quả kiểm độc lập lưu ở [manifest.json](manifest.json).

Dữ liệu giả lập theo BA, không phải giá thị trường/chứng từ pháp lý. Expected độc lập ở [expected-results.json](../../expected-results.json). Ảnh [golden-page-1.png](golden-page-1.png) render từ PDF thật, không thay cho file PDF.

Tạo lại chỉ khi chủ động muốn ghi metadata/file export mới (không chạy seed hoặc nghiệp vụ):

```powershell
node tests/api/export-golden.mjs
python tests/BotZone.IntegrationTests/verify_golden_exports.py
```

Script mặc định API `http://127.0.0.1:5080`, có thể chọn `BOTZONE_API_URL`. Bước kiểm Python dùng zip/XML đọc XLSX và PyMuPDF local để đọc/render PDF; đối chiếu literal BA, ID, tổng, readAt và checksum. Không tuyên bố đã mở SaveFileDialog hoặc Excel bằng thao tác người dùng.
