import { StorefrontPage } from "@/components/storefront";
export default async function Page({
  params,
}: {
  params: Promise<{ locale: string; path?: string[] }>;
}) {
  const { path } = await params;
  return <StorefrontPage path={path ?? []} />;
}
