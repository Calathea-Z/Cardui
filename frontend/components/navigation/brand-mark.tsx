export function BrandMark() {
  return (
    <div className="mb-10">
      <p className="font-brand text-[2.35rem] leading-none tracking-tight text-sidebar-foreground">
        Tortoise
      </p>
      <p className="mt-2 text-[11px] font-medium tracking-[0.2em] text-sidebar-primary uppercase">
        Personal ledger
      </p>
      <svg className="brand-ink-stroke mt-3" viewBox="0 0 120 14" aria-hidden>
        <path d="M2 8 C 24 8, 36 5, 54 8 S 88 11, 118 7" />
      </svg>
    </div>
  );
}
