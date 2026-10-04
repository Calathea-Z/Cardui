"use client";

import { Button } from "@/components/ui/button";
import type { TransactionImportBatchDto } from "@/lib/api/types";
import type { ImportBusy } from "./importCsv";
import { formatBatchDate } from "./importCsvFormat";

type OpenImportListProps = {
  batches: TransactionImportBatchDto[];
  pendingUndoId: string | null;
  busy: ImportBusy;
  onAsk: (importId: string) => void;
  onCancel: () => void;
  onConfirm: (importId: string) => void;
};

/**
 * Lists imports the household can still undo.
 * Undo archives every transaction from that file, and an empty list renders nothing.
 */
export function OpenImportList({
  batches,
  pendingUndoId,
  busy,
  onAsk,
  onCancel,
  onConfirm,
}: OpenImportListProps) {
  const isWorking = busy !== null;
  if (batches.length === 0) {
    return null;
  }

  return (
    <section className="flex flex-col gap-2 border-t border-border pt-4">
      <h3 className="text-sm font-medium">Undo an import</h3>
      {batches.map((batch) => (
        <div
          key={batch.id}
          className="flex flex-col gap-2 rounded-lg border border-border px-3 py-2"
        >
          <p className="text-sm">
            {batch.fileName} · {batch.importedCount} on {batch.accountName} ·{" "}
            {formatBatchDate(batch.createdAt)}
          </p>
          {pendingUndoId === batch.id ? (
            <div className="flex flex-col gap-2">
              <p className="text-xs text-muted-foreground">
                This archives every transaction from that file.
              </p>
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="destructive"
                  disabled={isWorking}
                  onClick={() => onConfirm(batch.id)}
                >
                  {busy === "undo"
                    ? "Archiving"
                    : "Archive imported transactions"}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  disabled={isWorking}
                  onClick={onCancel}
                >
                  Cancel
                </Button>
              </div>
            </div>
          ) : (
            <Button
              type="button"
              variant="outline"
              className="self-start"
              disabled={isWorking}
              onClick={() => onAsk(batch.id)}
            >
              Undo
            </Button>
          )}
        </div>
      ))}
    </section>
  );
}
