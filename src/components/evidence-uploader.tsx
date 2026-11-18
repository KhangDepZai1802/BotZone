"use client";
import { useRef, useState } from "react";
import { ImagePlus, Trash2 } from "lucide-react";
import { uploadEvidence, useStore } from "./client";
import { Button, ErrorState } from "./ui";
export function EvidenceUploader({
  referenceType,
  referenceId,
  onChange,
  onBusyChange,
}: {
  referenceType: "order" | "entitlement";
  referenceId: string;
  onChange: (ids: string[]) => void;
  onBusyChange?: (busy: boolean) => void;
}) {
  const s = useStore(),
    input = useRef<HTMLInputElement>(null),
    [files, setFiles] = useState<
      { id: string; previewUrl: string; name: string }[]
    >([]),
    [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  async function upload(selected: FileList | null) {
    if (!selected) return;
    const list = Array.from(selected);
    if (list.length + files.length > 6) {
      setError(
        s.tr(
          "Mỗi hồ sơ tối đa sáu ảnh.",
          "A maximum of six images per request.",
        ),
      );
      return;
    }
    setBusy(true);
    onBusyChange?.(true);
    setError("");
    const updated = [...files];
    try {
      for (const file of list) {
        const result = await uploadEvidence(file, referenceType, referenceId);
        updated.push({ ...result, name: file.name });
        setFiles([...updated]);
        onChange(updated.map((x) => x.id));
      }
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
      onBusyChange?.(false);
      if (input.current) input.current.value = "";
    }
  }
  return (
    <div className="form-stack">
      <div className="row">
        <div>
          <strong>{s.tr("Ảnh bằng chứng", "Supporting images")}</strong>
          <p className="help-text">
            {s.tr(
              "JPEG, PNG, WebP · Tối đa 5 MB/ảnh, sáu ảnh. Chỉ bạn và nhân viên được phép xem.",
              "JPEG, PNG, WebP · Up to 5 MB each, six images. Visible only to you and authorized staff.",
            )}
          </p>
        </div>
        <Button
          type="button"
          variant="outline"
          disabled={busy || files.length >= 6}
          onClick={() => input.current?.click()}
        >
          <ImagePlus />
          {busy
            ? s.tr("Đang tải…", "Uploading…")
            : s.tr("Thêm ảnh", "Add images")}
        </Button>
      </div>
      <input
        ref={input}
        hidden
        type="file"
        accept="image/jpeg,image/png,image/webp"
        multiple
        onChange={(e) => void upload(e.target.files)}
      />
      {files.length > 0 && (
        <div className="evidence-grid">
          {files.map((file) => (
            <div key={file.id}>
              <img src={file.previewUrl} alt={file.name} />
              <Button
                variant="ghost"
                size="sm"
                type="button"
                disabled={busy}
                aria-label={s.tr("Bỏ ảnh", "Remove image") + " " + file.name}
                onClick={() => {
                  const updated = files.filter((x) => x.id !== file.id);
                  setFiles(updated);
                  onChange(updated.map((x) => x.id));
                }}
              >
                <Trash2 />
                {s.tr("Bỏ chọn", "Remove")}
              </Button>
            </div>
          ))}
        </div>
      )}
      {error && <ErrorState message={error} />}
    </div>
  );
}
