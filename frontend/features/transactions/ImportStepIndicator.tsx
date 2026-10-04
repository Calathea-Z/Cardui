import { importStepHeading, importSteps, type ImportStep } from "./importCsv";

type ImportStepIndicatorProps = {
  step: ImportStep;
};

/**
 * Lists the four import steps and marks the current one.
 * Steps already passed use the foreground color, and later steps stay muted.
 */
export function ImportStepIndicator({ step }: ImportStepIndicatorProps) {
  const currentIndex = importSteps.indexOf(step);

  return (
    <ol aria-label="Import steps" className="flex flex-col gap-1">
      {importSteps.map((item, index) => {
        const current = item === step;
        const label = `${index + 1}. ${importStepHeading(item)}`;
        return (
          <li key={item} aria-current={current ? "step" : undefined}>
            {current ? (
              <h3 className="text-base font-semibold text-foreground">
                {label}
              </h3>
            ) : (
              <p
                className={
                  index < currentIndex
                    ? "text-sm text-foreground"
                    : "text-sm text-muted-foreground"
                }
              >
                {label}
              </p>
            )}
          </li>
        );
      })}
    </ol>
  );
}
