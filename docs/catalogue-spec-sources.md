# Nguồn thông số kỹ thuật danh mục / Catalogue specification sources

Đối chiếu ngày **13/11/2026** với trang sản phẩm, thông số kỹ thuật và tài liệu hỗ trợ chính thức của Apple. Dữ liệu trong [product-specifications.json](../data/product-specifications.json) bao phủ **35/35 model**, **609 mục thông số song ngữ**, dùng **75 mã trường chung**. Tên model khớp chính xác `data/catalogue-demo.json`. Đây là dữ liệu theo model; không thay thế lựa chọn SKU, giá hoặc tồn kho.

Verified on **13 November 2026** against Apple's official product, technical-specification and support pages. The dataset covers **all 35 catalogue models**, with **609 bilingual fields** and **75 shared field identifiers**. Model names exactly match the catalogue fixture. Specifications describe the model family; selected SKU, price and stock remain separate.

## Quy tắc dữ liệu / Data rules

- `storage_options`, `memory_options`, `case_sizes` và các tùy chọn do hãng công bố mô tả những cấu hình được hỗ trợ, không khẳng định cửa hàng có bán mọi cấu hình. / Manufacturer storage, memory and case options describe supported configurations, not every SKU currently sold by the store.
- Mỗi `id` có cùng ý nghĩa, nhãn VI/EN và nhóm ở tất cả model. ID của nhóm và thông số là `snake_case`; `highlightIds` trỏ tới các trường thật trong cùng model. / Shared IDs retain the same meaning, bilingual labels and group across models; highlights reference existing fields.
- Số liệu được diễn đạt ngắn gọn từ nguồn hãng. Không dùng tin đồn, thông số suy ra từ đời máy gần giống, RAM iPhone không được hãng công bố hoặc tốc độ xung do bên thứ ba suy đoán. / Values are concise factual paraphrases; no rumors, neighboring-model inference, unpublished iPhone RAM or third-party clock-speed estimates.
- Số liệu pin và sạc là kết quả tối đa hoặc điều kiện thử nghiệm Apple công bố; không phải cam kết thời lượng sử dụng thực tế. Công suất cáp là mức truyền công suất hỗ trợ, không bảo đảm mọi thiết bị sạc ở mức đó. / Battery and charging values retain Apple's rated maximum or test conditions. Cable power support does not imply every device charges at that rate.
- Cellular, Ethernet, vật liệu, cỡ vỏ, trọng lượng và cấu hình CPU/RAM có điều kiện ngay trong giá trị. Không gán tính năng của bản Cellular cho SKU Wi-Fi/GPS. / Conditional variants are identified in the value; Cellular features are not assigned to Wi-Fi/GPS SKUs.
- Kích thước màn hình là đường chéo theo cách gọi của hãng; góc bo tròn làm diện tích hiển thị thực tế nhỏ hơn. Dung lượng hệ thống sau định dạng thấp hơn dung lượng danh nghĩa. / Display sizes follow Apple's diagonal naming; rounded corners reduce viewable area and formatted storage is lower than nominal capacity.
- Tương thích phụ kiện tuân theo danh sách thiết bị/phiên bản hệ điều hành của Apple. Các tính năng âm thanh, ứng dụng sức khỏe, dịch vụ và UWB còn tùy phần mềm, nội dung hoặc khu vực; chỉ nêu phần cứng cảm biến, không đưa ra công dụng chẩn đoán. / Accessory compatibility follows Apple's lists; software, content and regional requirements still apply. Sensor descriptions make no diagnostic claims.
- Không có giá, nhãn demo, `priceDisclosure` hoặc ghi chú kỹ thuật nội bộ trong các hàng thông số hiển thị cho khách hàng. / Display fields contain no prices, demo labels, price disclosures or internal implementation metadata.

## Ngoại lệ đã xử lý / Resolved ambiguities and omissions

1. **Mac mini M6:** trang thông số hiện xác nhận M6, CPU/GPU 12 lõi, Thunderbolt 4 và Ethernet 2,5 Gb nhưng phần trọng lượng còn ghi rõ “M4” và “M4 Pro”. Đã **bỏ trường trọng lượng**, không chuyển số 0,67/0,73 kg sang M6. Phần M5 Pro của cùng trang cũng không được gán cho model M6. / The current page identifies M6 elsewhere but labels its weight rows M4/M4 Pro. Weight is omitted, and M5 Pro specifications are not applied to the M6 model. [Nguồn / Source](https://www.apple.com/vn/mac-mini/specs/).
2. **iPhone 16:** trang bán hàng hiện chỉ hiển thị một số dung lượng. Dataset dùng trang hỗ trợ riêng của iPhone 16 cho các dung lượng hãng từng công bố: 128/256/512 GB. Đây không phải danh sách SKU đang bán. / The model-specific support page establishes the manufacturer's 128/256/512 GB options; it does not assert current store availability. [Nguồn / Source](https://support.apple.com/en-us/121029).
3. **iPad:** chỉ lấy số liệu 11 inch cho Air M3/M4 và Pro M5, không lấy trọng lượng/độ sáng/pin của bản 13 inch. Pro M5 ghi CPU 9 lõi, RAM 12 GB ở 256/512 GB; CPU 10 lõi, RAM 16 GB ở 1/2 TB. / Only the 11-inch versions are used, with storage-dependent M5 Pro CPU/memory conditions explicit. [Nguồn / Source](https://www.apple.com/vn/ipad-pro/specs/).
4. **iPhone Duo:** camera phía trong được Apple gọi là FaceTime dưới màn hình, khẩu độ ƒ/1,8; trang nguồn không nêu số megapixel nên không thêm số này. Touch ID ở nút sườn được ghi riêng, không dùng Face ID từ các iPhone khác. / Inner-camera resolution is omitted because the source does not publish it; side-button Touch ID is explicitly distinguished. [Nguồn / Source](https://www.apple.com/vn/iphone-duo/specs/).
5. **Apple Pencil:** bảng so sánh hãng xác nhận Pencil Pro có lực nhấn, bóp, xoay thân, phản hồi rung và Tìm; Pencil USB-C không có các chức năng đó. Trang tương thích chính thức nêu iPhone Duo cho Pencil USB-C. / Negative feature values come from Apple's comparison table, not inference. [So sánh / Comparison](https://www.apple.com/apple-pencil/), [USB-C compatibility](https://support.apple.com/en-gb/121318).
6. **Studio Display 2022:** trang thông số nguồn không nêu tần số quét trong phần màn hình nên không sao chép 60Hz từ trang đời 2026. Mỗi đời giữ đúng cổng, camera, khối lượng theo chân đế và phiên bản hệ điều hành tối thiểu. / Refresh rate is omitted from the 2022 entry rather than copied from the 2026 page. [Nguồn / Source](https://support.apple.com/en-ca/111890).
7. **Apple Watch:** thông số cân nặng là vỏ nhôm GPS, không gồm dây; các kích thước khác được ghi rõ. Series 12 có cả vỏ gốm, Series 11 không được kế thừa tùy chọn đó. / Listed weights explicitly identify aluminum GPS cases without bands; ceramic options apply only to Series 12. [Series 12](https://www.apple.com/apple-watch-series-12/specs/), [Series 11](https://support.apple.com/en-la/125093).
8. **AirPods Pro 2:** dùng đúng bản hộp MagSafe USB-C năm 2023, không nhầm hộp Lightning năm 2022. AirPods Max dùng đúng thế hệ 2. / AirPods Pro 2 refers specifically to the 2023 USB-C case version; AirPods Max is the second generation. [Pro 2 USB-C](https://support.apple.com/en-us/111834), [Max 2](https://www.apple.com/vn/airpods-max/specs/).

## Danh mục nguồn / Model source index

<!-- SOURCE_INDEX_START -->

| Model | Số mục / Fields | Nguồn chính thức / Official sources |
| --- | ---: | --- |
| `P13` — iPhone 13 | 24 | [iPhone 13 — Apple technical specifications](https://support.apple.com/en-us/111872) |
| `P14` — iPhone 14 | 24 | [iPhone 14 — Apple technical specifications](https://support.apple.com/en-gb/111850) |
| `P15` — iPhone 15 | 24 | [iPhone 15 — Apple technical specifications](https://support.apple.com/en-mide/111831) |
| `P16` — iPhone 16 | 24 | [iPhone 16 — Apple technical specifications](https://support.apple.com/en-us/121029) |
| `P17` — iPhone 17 | 25 | [iPhone 17 — Apple technical specifications](https://www.apple.com/vn/iphone-17/specs/) |
| `P18PRO` — iPhone 18 Pro | 26 | [iPhone 18 Pro — Apple technical specifications](https://www.apple.com/vn/iphone-18-pro/specs/) |
| `P18MAX` — iPhone 18 Pro Max | 26 | [iPhone 18 Pro Max — Apple technical specifications](https://www.apple.com/vn/iphone-18-pro/specs/) |
| `DUO` — iPhone Duo | 26 | [iPhone Duo — Apple technical specifications](https://www.apple.com/vn/iphone-duo/specs/) |
| `MBA5` — MacBook Air 13-inch (M5) | 22 | [MacBook Air 13-inch (M5) — Apple technical specifications](https://www.apple.com/vn/macbook-air/specs/) |
| `MBA4` — MacBook Air 13-inch (M4) | 22 | [MacBook Air 13-inch (M4) — Apple technical specifications](https://support.apple.com/en-la/122209) |
| `MINI6` — Mac mini (M6) | 14 | [Mac mini (M6) — Apple technical specifications](https://www.apple.com/vn/mac-mini/specs/) |
| `IPA4` — iPad Air 11-inch (M4) | 21 | [iPad Air 11-inch (M4) — Apple technical specifications](https://www.apple.com/vn/ipad-air/specs/) |
| `IPA3` — iPad Air 11-inch (M3) | 21 | [iPad Air 11-inch (M3) — Apple technical specifications](https://support.apple.com/en-us/122241) |
| `IPP5` — iPad Pro 11-inch (M5) | 23 | [iPad Pro 11-inch (M5) — Apple technical specifications](https://www.apple.com/vn/ipad-pro/specs/) |
| `WATCH12` — Apple Watch Series 12 | 19 | [Apple Watch Series 12 — Apple technical specifications](https://www.apple.com/apple-watch-series-12/specs/) |
| `WATCH11` — Apple Watch Series 11 | 19 | [Apple Watch Series 11 — Apple technical specifications](https://support.apple.com/en-la/125093) |
| `AP3` — AirPods Pro 3 | 18 | [AirPods Pro 3 — Apple technical specifications](https://www.apple.com/vn/airpods-pro/specs/) |
| `AP2` — AirPods Pro (2nd generation, USB-C) | 18 | [AirPods Pro (2nd generation, USB-C) — Apple technical specifications](https://support.apple.com/en-us/111834) |
| `APMAX2` — AirPods Max 2 | 14 | [AirPods Max 2 — Apple technical specifications](https://www.apple.com/vn/airpods-max/specs/) |
| `TVA19` — Apple TV 4K (A19) | 14 | [Apple TV 4K (A19) — Apple technical specifications](https://www.apple.com/vn/apple-tv-4k/specs/) |
| `TV3` — Apple TV 4K (3rd generation) | 13 | [Apple TV 4K (3rd generation) — Apple technical specifications](https://support.apple.com/en-gb/111839) |
| `HOMEPOD2` — HomePod (2nd generation) | 14 | [HomePod (2nd generation) — Apple technical specifications](https://www.apple.com/homepod-2nd-generation/specs/); [Apple Support — Set up a HomePod stereo pair](https://support.apple.com/guide/homepod/set-up-a-homepod-stereo-pair-apd1ed62a52a/homepod) |
| `VISION5` — Apple Vision Pro (M5) | 20 | [Apple Vision Pro (M5) — Apple technical specifications](https://www.apple.com/apple-vision-pro/specs/) |
| `VISION2` — Apple Vision Pro (M2) | 20 | [Apple Vision Pro (M2) — Apple technical specifications](https://support.apple.com/en-la/117810) |
| `STUDIO26` — Studio Display (2026) | 16 | [Studio Display (2026) — Apple technical specifications](https://www.apple.com/vn/studio-display/specs/) |
| `STUDIO22` — Studio Display (2022) | 15 | [Studio Display (2022) — Apple technical specifications](https://support.apple.com/en-ca/111890) |
| `AIRTAG2` — AirTag (2nd generation) | 11 | [AirTag (2nd generation) — Apple technical specifications](https://support.apple.com/en-au/126203) |
| `AIRTAG1` — AirTag (1st generation) | 11 | [AirTag (1st generation) — Apple technical specifications](https://support.apple.com/en-gb/111847) |
| `PENCILPRO` — Apple Pencil Pro | 11 | [Apple Pencil Pro — Apple technical specifications](https://support.apple.com/en-ie/120123); [Apple Pencil — feature and compatibility comparison](https://www.apple.com/apple-pencil/) |
| `PENCILC` — Apple Pencil (USB-C) | 12 | [Apple Pencil (USB-C) — Apple technical specifications](https://support.apple.com/en-gb/121318); [Apple Pencil — feature and compatibility comparison](https://www.apple.com/apple-pencil/) |
| `KEYBOARDC` — Magic Keyboard (USB-C) | 9 | [Magic Keyboard (USB-C) — Apple technical specifications](https://support.apple.com/en-gb/121955) |
| `MOUSEC` — Magic Mouse (USB-C) | 10 | [Magic Mouse (USB-C) — Apple technical specifications](https://support.apple.com/en-gb/121931) |
| `POWER20` — 20W USB-C Power Adapter | 6 | [20W USB-C Power Adapter — Apple technical specifications](https://www.apple.com/shop/product/mwvv3am/a/20w-usb-c-power-adapter) |
| `CABLE60` — 60W USB-C Charge Cable (1 m) | 8 | [60W USB-C Charge Cable (1 m) — Apple technical specifications](https://www.apple.com/shop/product/mw493am/a/60w-usb-c-charge-cable-1-m) |
| `CASE18MAX` — iPhone 18 Pro Max Silicone Case with MagSafe | 9 | [iPhone 18 Pro Max Silicone Case with MagSafe — Apple technical specifications](https://www.apple.com/shop/product/mk914zm/a/iphone-18-pro-max-silicone-case-with-magsafe-burgundy) |

<!-- SOURCE_INDEX_END -->

## Bằng chứng kiểm tra / Validation evidence

[validation.json](../artifacts/specifications-research/validation.json) ghi số model, số mục, SHA-256 của dataset và các kiểm tra cấu trúc đã chạy: đủ mã model, tên chính xác, VI/EN không rỗng, ID duy nhất trong model, nhãn/nhóm chung nhất quán, highlights hợp lệ và chỉ dùng nguồn HTTPS của `www.apple.com`/`support.apple.com`.

[retrieval.json](../artifacts/specifications-research/retrieval.json) lưu URL đã truy cập, thời điểm, HTTP status, tiêu đề và SHA-256 của HTML nguồn cho 35 lần đọc ban đầu. File `<modelCode>.txt` cùng thư mục giữ phần nội dung dùng để đối chiếu. Một số lượt mở qua công cụ web nhận HTTP 429; đọc HTTP trực tiếp các URL tương ứng đã thành công với HTTP 200 và được lưu trong nhật ký. Hai tài liệu bổ sung Apple Pencil/HomePod được mở riêng qua công cụ web, không được tính vào 35 bản HTML ban đầu.

The validation artifact records the dataset hash and executed structural checks. These checks **do not independently prove every factual assertion**; facts were manually compared against the retained official source text. The retrieval log records 35 successful direct HTTP source reads; supplemental Pencil comparison and HomePod stereo-pair instructions were separately reviewed in the browser tool. No database, backend or frontend files were changed by this research task.

Tái tạo file từ các giá trị đã tuyển chọn: `python artifacts/specifications-research/build-specifications.py`. Lệnh không truy cập mạng và không ghi database; thời điểm `verifiedAt` được cố định tại đợt đối chiếu này, không tự tăng khi chạy lại. Nếu cập nhật thông số sau này, phải đọc lại nguồn trước khi đổi giá trị và mốc xác minh.

Rebuild with the command above from the curated values. It performs no network or database writes. Rebuilding alone does not constitute a fresh factual verification; reread official sources before changing future values or verification dates.
