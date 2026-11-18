# BotZone brand assets — 17/11/2026

Logo nguồn là ảnh thương hiệu chính thức Khang gửi trong hội thoại. Dùng skill `imagegen` và công cụ tích hợp để tạo hai bản raster dùng trong dự án; không dựng chữ bằng font thay thế.

- `botzone-logo.png`: bản vuông, toàn bộ wordmark đỏ burgundy / đen trên trắng, 1254 × 1254.
- `botzone-icon.png`: biểu tượng B trên nền trắng vuông, 1254 × 1254.
- `botzone.ico`: đóng gói icon RGBA ở 16/24/32/48/64/128/256 px bằng `scripts/package-brand-icon.mjs`; cùng asset cho favicon và WinForms executable/window icon.

Web `BrandLogo` và native `BrandMark` cắt vùng hiển thị thừa, giữ tỷ lệ và bo góc tại control. PNG nguồn giữ nguyên để có thể tái sử dụng. Không dùng ảnh bản thử transparency: bản đó không đạt, không được tham chiếu trong sản phẩm.

## Prompt cuối — wordmark (built-in imagegen)

Use the FIRST reference image, the user's official Botzone logo on pure white, as the edit target. Ignore the second generated image which has incorrect transparency. Produce a clean flat square brand asset. Preserve the exact official burgundy B/apple negative-space silhouette and black 'otzone' lettering from the original, same proportions and shapes. Place complete original wordmark horizontally centered in a square 1024x1024 solid pure white (#FFFFFF) image, at 88% canvas width. The wordmark should be at the vertical center. ALL other pixels pure opaque white, no transparency anywhere. No gradient, shadow, texture, glow, decoration, or rounded-square outline. Exact logo preservation. Only reframe and trim whitespace. The application code will handle corner rounding.

## Prompt cuối — icon (built-in imagegen)

Extract ONLY the existing burgundy B mark with its exact white apple silhouette negative space from the supplied official Botzone logo. Preserve its exact shape, proportions, cutouts and burgundy color; do not redraw or stylize. Remove all black letters after the B. Center the original B by itself on a square pure white canvas, occupying 72% of the canvas height. Output clean flat app icon. Every other pixel is pure opaque white; no shadows, no texture, no gradients, no outline, no border, no additional text. Application CSS will apply rounded corners.
