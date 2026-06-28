import { useRef, useState, type DragEvent } from "react";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Upload } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Modal } from "@/components/ui/modal";
import {
  previewImport,
  commitImport,
  type ImportPreviewResult,
  type ImportRowPreview,
} from "@/features/transactions/transactionsApi";
import { resolveTransactionError } from "@/features/transactions/resolveTransactionError";

type ModalStep = "idle" | "previewing" | "preview" | "committing" | "done";

function rowErrorMessage(row: ImportRowPreview, t: TFunction): string {
  if (!row.errorCode) return "";
  return t(`transactions.importRowErrors.${row.errorCode}`, { subject: row.errorSubject ?? "" });
}

export function ImportTransactionsModal({
  workspaceId,
  onClose,
}: {
  workspaceId: string;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [step, setStep] = useState<ModalStep>("idle");
  const [file, setFile] = useState<File | null>(null);
  const [previewResult, setPreviewResult] = useState<ImportPreviewResult | null>(null);
  const [importedCount, setImportedCount] = useState(0);
  const [dragOver, setDragOver] = useState(false);
  const [previewError, setPreviewError] = useState<unknown>(null);
  const [commitError, setCommitError] = useState<unknown>(null);

  const previewMutation = useMutation({
    mutationFn: (f: File) => previewImport(workspaceId, f),
    onSuccess: (result) => {
      setPreviewResult(result);
      setStep("preview");
    },
    onError: (err) => {
      setPreviewError(err);
      setStep("idle");
    },
  });

  const commitMutation = useMutation({
    mutationFn: (f: File) => commitImport(workspaceId, f),
    onSuccess: (count) => {
      void queryClient.invalidateQueries({ queryKey: ["transactions", workspaceId] });
      void queryClient.invalidateQueries({ queryKey: ["financial-accounts", workspaceId] });
      setImportedCount(count);
      setStep("done");
    },
    onError: (err) => {
      setCommitError(err);
      setStep("preview");
    },
  });

  const handleFile = (selected: File) => {
    setFile(selected);
    setPreviewError(null);
    setPreviewResult(null);
    setStep("previewing");
    previewMutation.mutate(selected);
  };

  const handleDrop = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setDragOver(false);
    const dropped = e.dataTransfer.files[0];
    if (dropped) { handleFile(dropped); }
  };

  const handleCommit = () => {
    if (!file) { return; }
    setCommitError(null);
    setStep("committing");
    commitMutation.mutate(file);
  };

  const handleBackToIdle = () => {
    setPreviewResult(null);
    setCommitError(null);
    setStep("idle");
  };

  const isBlocking = step === "previewing" || step === "committing";
  const errorRows = previewResult?.rows.filter((r) => !r.isValid) ?? [];
  const canCommit = previewResult && previewResult.errorCount === 0 && previewResult.validCount > 0;

  return (
    <Modal onBackdropClick={isBlocking ? () => {} : onClose} size="md">
      <h2 className="mb-5 text-base font-semibold">{t("transactions.importModal.title")}</h2>

      {/* ── idle ── */}
      {step === "idle" && (
        <>
          <div
            onClick={() => fileInputRef.current?.click()}
            onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
            onDragLeave={() => setDragOver(false)}
            onDrop={handleDrop}
            className={cn(
              "flex cursor-pointer flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed border-border px-4 py-8 transition-colors hover:bg-accent/40",
              dragOver && "border-primary bg-primary/5",
            )}
          >
            <Upload className="h-6 w-6 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">{t("transactions.importModal.dropzone")}</p>
            <p className="text-xs text-muted-foreground/60">{t("transactions.importModal.constraints")}</p>
          </div>
          <input
            ref={fileInputRef}
            type="file"
            accept=".csv,text/csv"
            className="hidden"
            onChange={(e) => {
              const f = e.target.files?.[0];
              if (f) { handleFile(f); }
              e.target.value = "";
            }}
          />
          {previewError && (
            <p className="mt-3 text-sm text-destructive">
              {resolveTransactionError(previewError, t as TFunction)}
            </p>
          )}
          <div className="mt-4 flex justify-end">
            <Button variant="secondary" onClick={onClose}>
              {t("common.cancel")}
            </Button>
          </div>
        </>
      )}

      {/* ── previewing ── */}
      {step === "previewing" && (
        <div className="flex items-center justify-center py-12">
          <p className="text-sm text-muted-foreground">{t("transactions.importModal.previewing")}</p>
        </div>
      )}

      {/* ── preview ── */}
      {step === "preview" && previewResult && (
        <>
          <div className="mb-4 flex gap-4 rounded-md border border-border bg-muted/30 px-4 py-3 text-sm">
            <span className="text-green-600 dark:text-green-400">
              ✓ {t("transactions.importModal.validRows", { count: previewResult.validCount })}
            </span>
            {previewResult.errorCount > 0 && (
              <span className="text-destructive">
                ✗ {t("transactions.importModal.errorRows", { count: previewResult.errorCount })}
              </span>
            )}
          </div>

          {errorRows.length > 0 && (
            <div className="mb-4 max-h-56 overflow-y-auto rounded-md border border-border">
              <table className="w-full text-xs">
                <thead className="sticky top-0 bg-muted/80">
                  <tr>
                    <th className="px-3 py-2 text-left font-medium text-muted-foreground">#</th>
                    <th className="px-3 py-2 text-left font-medium text-muted-foreground">
                      {t("transactions.createModal.nameLabel")}
                    </th>
                    <th className="px-3 py-2 text-left font-medium text-muted-foreground">
                      {t("transactions.importModal.errorColumn")}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {errorRows.map((row) => (
                    <tr key={row.rowNumber} className="border-t border-border">
                      <td className="px-3 py-2 tabular-nums text-muted-foreground">{row.rowNumber}</td>
                      <td className="max-w-[100px] truncate px-3 py-2">{row.name || "—"}</td>
                      <td className="px-3 py-2 text-destructive">
                        {rowErrorMessage(row, t as TFunction)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {commitError && (
            <p className="mb-3 text-sm text-destructive">
              {resolveTransactionError(commitError, t as TFunction)}
            </p>
          )}

          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={handleBackToIdle}>
              {t("common.cancel")}
            </Button>
            {canCommit && (
              <Button disabled={commitMutation.isPending} onClick={handleCommit}>
                {commitMutation.isPending
                  ? t("transactions.importModal.importing")
                  : t("transactions.importModal.confirmButton", { count: previewResult.validCount })}
              </Button>
            )}
          </div>
        </>
      )}

      {/* ── committing ── */}
      {step === "committing" && (
        <div className="flex items-center justify-center py-12">
          <p className="text-sm text-muted-foreground">{t("transactions.importModal.importing")}</p>
        </div>
      )}

      {/* ── done ── */}
      {step === "done" && (
        <>
          <p className="mb-5 text-sm text-muted-foreground">
            {t("transactions.importModal.success", { count: importedCount })}
          </p>
          <div className="flex justify-end">
            <Button onClick={onClose}>{t("transactions.importModal.close")}</Button>
          </div>
        </>
      )}
    </Modal>
  );
}
