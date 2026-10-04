import { ApiUnavailableBanner } from "@/components/api-unavailable-banner";

type PageApiErrorBannerProps = {
  message: string;
};

/**
 * Places the API-unavailable alert in the page content column.
 * The message is the banner's detail line.
 */
export function PageApiErrorBanner({ message }: PageApiErrorBannerProps) {
  return (
    <div className="mx-auto w-full max-w-6xl px-6 pt-8">
      <ApiUnavailableBanner message={message} />
    </div>
  );
}
