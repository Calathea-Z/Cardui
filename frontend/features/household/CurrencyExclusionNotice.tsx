import { Alert } from "@/components/ui/alert";
import {
  formatCurrencyExclusion,
  type CurrencyExclusion,
} from "./currencyExclusion";

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
