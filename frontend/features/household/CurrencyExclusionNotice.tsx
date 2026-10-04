import { Alert } from "@/components/ui/alert";
import {
  formatCurrencyExclusion,
  type CurrencyExclusion,
} from "./currencyExclusion";

/**
 * Shows when accounts or transactions were left out of a total.
 * Renders nothing when there is nothing to report.
 */
export function CurrencyExclusionNotice({
  exclusion,
}: {
  exclusion: CurrencyExclusion;
}) {
  const message = formatCurrencyExclusion(exclusion);
  if (!message) {
    return null;
  }

  return <Alert>{message}</Alert>;
}
