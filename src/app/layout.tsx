import type { Metadata } from "next";
import "@/styles/globals.css";
export const metadata: Metadata = {
  title: {
    default: "BotZone — Một thế giới. Mọi kết nối.",
    template: "%s · BotZone",
  },
  description:
    "Khám phá thế giới Apple tại BotZone. Mua sắm, thu cũ đổi mới và BotZone Care trong trải nghiệm demo local.",
  icons: {
    icon: [{ url: "/brand/botzone-icon.png", type: "image/png" }],
    apple: [{ url: "/brand/botzone-icon.png", type: "image/png" }],
  },
};
export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="vi">
      <body>{children}</body>
    </html>
  );
}
