"use client";
import { ErrorState } from "@/components/ui";
export default function Error({ reset }: { error: Error; reset: () => void }) {
  return (
    <div className="page-wrap">
      <ErrorState
        message="Không tải được trang. Vui lòng thử lại."
        retry={reset}
      />
    </div>
  );
}
