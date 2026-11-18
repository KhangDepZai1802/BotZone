import type { NextConfig } from 'next';
const config: NextConfig = { output: 'standalone', serverExternalPackages: ['pg', 'pdfkit'], poweredByHeader: false };
export default config;
