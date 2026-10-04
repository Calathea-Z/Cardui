import type { TransactionImportInspectDto } from "@/lib/api/types";

type ImportSampleRowsProps = {
  inspect: TransactionImportInspectDto;
};

/**
 * Shows sample rows from the inspected file.
 * A blank header is labeled by column number, and an empty sample renders nothing.
 */
export function ImportSampleRows({ inspect }: ImportSampleRowsProps) {
  if (inspect.sampleRows.length === 0) {
    return null;
  }

  return (
    <figure className="flex flex-col gap-2">
      <figcaption className="text-sm font-medium">
        Sample from the file
      </figcaption>
      <div className="overflow-x-auto rounded-lg border border-border">
        <table className="w-full border-collapse text-left text-xs">
          <thead className="bg-muted/40">
            <tr>
              {inspect.headers.map((header, index) => (
                <th
                  key={`${header}-${index}`}
                  className="border-b border-border px-3 py-2 font-medium whitespace-nowrap"
                >
                  {header.trim() || `Column ${index + 1}`}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {inspect.sampleRows.map((row, rowIndex) => (
              <tr key={rowIndex} className="border-b border-border">
                {inspect.headers.map((_, cellIndex) => (
                  <td key={cellIndex} className="px-3 py-2 whitespace-nowrap">
                    {row[cellIndex] ?? ""}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </figure>
  );
}
