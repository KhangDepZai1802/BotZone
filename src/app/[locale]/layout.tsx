import { notFound } from "next/navigation";
import { NextIntlClientProvider } from "next-intl";
import { StoreProvider } from "@/components/client";
import { StoreShell } from "@/components/store-shell";
import { Suspense } from "react";
import type { Metadata } from "next";
export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }): Promise<Metadata> {
  const { locale } = await params;
  return {
    title: { default: locale === "en" ? "BotZone — One world. All connected." : "BotZone — Một thế giới. Mọi kết nối.", template: "%s · BotZone" },
    description: locale === "en"
      ? "Explore Apple products, trade in your device and discover BotZone Care in our local demo store."
      : "Khám phá sản phẩm Apple, thu cũ đổi mới và BotZone Care tại cửa hàng demo local BotZone.",
  };
}
export default async function LocaleLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  if (locale !== "vi" && locale !== "en") notFound();
  return (
    <NextIntlClientProvider
      locale={locale}
      messages={{}}
      timeZone="Asia/Ho_Chi_Minh"
      formats={{}}
      now={new Date()}
    >
      <Suspense
          fallback={
            <div className="page-wrap" role="status">
              BotZone…
            </div>
          }
        >
        <StoreProvider locale={locale}>
          <StoreShell>{children}</StoreShell>
        </StoreProvider>
      </Suspense>
    </NextIntlClientProvider>
  );
}
