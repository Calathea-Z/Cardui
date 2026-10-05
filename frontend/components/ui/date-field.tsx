"use client";

import {
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
} from "lucide-react";
import { useLayoutEffect, useRef, useState, useSyncExternalStore } from "react";
import { createPortal } from "react-dom";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import {
  addDays,
  buildMonthGrid,
  formatDateLabel,
  formatMonthLabel,
  isDateInRange,
  monthHasSelectableDay,
  parseDateInput,
  shiftMonth,
  todayDateInput,
  weekdayLabels,
} from "./date-field-calendar";

type DateFieldProps = {
  value: string;
  onChange: (value: string) => void;
  title: string;
  placeholder?: string;
  disabled?: boolean;
  /** `row` matches transaction detail rows; `field` matches form controls. */
  variant?: "row" | "field";
  label?: string;
  /** Inclusive `YYYY-MM-DD` bound. A day before this stays visible and cannot be chosen. */
  min?: string;
  /** Inclusive `YYYY-MM-DD` bound. A day after this stays visible and cannot be chosen. */
  max?: string;
  className?: string;
  onOpenChange?: (open: boolean) => void;
};

/**
 * Date control that opens a month calendar in the themed bottom sheet.
 * The value is `YYYY-MM-DD`. An empty value shows the placeholder.
 */
export function DateField({
  value,
  onChange,
  title,
  placeholder = "Select date",
  disabled = false,
  variant = "field",
  label,
  min,
  max,
  className,
  onOpenChange,
}: DateFieldProps) {
  const [open, setOpen] = useState(false);
  const [visibleYear, setVisibleYear] = useState(
    () => visibleMonthFor(value).year,
  );
  const [visibleMonth, setVisibleMonth] = useState(
    () => visibleMonthFor(value).month,
  );
  const mounted = useSyncExternalStore(
    () => () => {},
    () => true,
    () => false,
  );
  const labelText = formatDateLabel(value);

  function setOpenState(next: boolean) {
    setOpen(next);
    onOpenChange?.(next);
  }

  /**
   * Opens the calendar on the selected month, or on the current month.
   */
  function openCalendar() {
    const next = visibleMonthFor(value);
    setVisibleYear(next.year);
    setVisibleMonth(next.month);
    setOpenState(true);
  }

  /**
   * Stores a chosen day and closes the calendar.
   */
  function selectDate(date: string) {
    onChange(date);
    setOpenState(false);
  }

  return (
    <>
      {variant === "row" ? (
        <button
          type="button"
          disabled={disabled}
          onClick={openCalendar}
          className={cn(
            "flex min-h-12 w-full items-center gap-3 text-left",
            disabled && "opacity-50",
            className,
          )}
        >
          {label ? (
            <span className="shrink-0 text-sm font-medium text-foreground">
              {label}
            </span>
          ) : null}
          <span className="flex min-w-0 flex-1 items-center justify-end gap-1.5 text-sm text-muted-foreground">
            <span className="truncate">{labelText ?? placeholder}</span>
            <ChevronDown className="size-4 shrink-0" aria-hidden="true" />
          </span>
        </button>
      ) : (
        <button
          type="button"
          disabled={disabled}
          onClick={openCalendar}
          aria-label={title}
          className={cn(
            "flex h-10 w-full min-w-0 items-center justify-between gap-2 rounded-lg border border-input bg-transparent px-2.5 text-left text-sm transition-colors outline-none",
            "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50",
            "disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50",
            className,
          )}
        >
          <span
            className={cn(
              "min-w-0 truncate",
              labelText ? "text-foreground" : "text-muted-foreground",
            )}
          >
            {labelText ?? placeholder}
          </span>
          <ChevronDown
            className="size-4 shrink-0 text-muted-foreground"
            aria-hidden="true"
          />
        </button>
      )}

      {mounted
        ? createPortal(
            <BottomSheet
              open={open}
              onClose={() => setOpenState(false)}
              title={title}
              headerAction="close"
              overlayClassName="z-[70]"
              className="z-[70]"
            >
              <DateFieldCalendar
                visibleYear={visibleYear}
                visibleMonth={visibleMonth}
                onVisibleMonthChange={(year, month) => {
                  setVisibleYear(year);
                  setVisibleMonth(month);
                }}
                value={value}
                min={min}
                max={max}
                onSelect={selectDate}
                onClear={() => selectDate("")}
                onToday={() => selectDate(todayDateInput())}
              />
            </BottomSheet>,
            document.body,
          )
        : null}
    </>
  );
}

type DateFieldCalendarProps = {
  visibleYear: number;
  visibleMonth: number;
  onVisibleMonthChange: (year: number, month: number) => void;
  value: string;
  min?: string;
  max?: string;
  onSelect: (value: string) => void;
  onClear: () => void;
  onToday: () => void;
};

/**
 * Month grid inside the date sheet.
 * Arrow keys move by day or week. A day outside the bounds cannot be chosen.
 */
function DateFieldCalendar({
  visibleYear,
  visibleMonth,
  onVisibleMonthChange,
  value,
  min,
  max,
  onSelect,
  onClear,
  onToday,
}: DateFieldCalendarProps) {
  const days = buildMonthGrid(visibleYear, visibleMonth);
  const today = todayDateInput();
  const todayAllowed = isDateInRange(today, min, max);
  const [focusDate, setFocusDate] = useState(() =>
    startingFocusDate(value, today, min, max),
  );
  const dayRefs = useRef(new Map<string, HTMLButtonElement>());

  useLayoutEffect(() => {
    dayRefs.current.get(focusDate)?.focus();
  }, [focusDate, visibleYear, visibleMonth]);

  /**
   * Moves keyboard focus by a number of days, and turns the page when needed.
   * A day outside the bounds is left unfocused.
   */
  function moveFocus(date: string, delta: number) {
    const next = addDays(date, delta);
    if (!isDateInRange(next, min, max)) {
      return;
    }

    const parsed = parseDateInput(next);
    if (
      parsed &&
      (parsed.year !== visibleYear || parsed.month !== visibleMonth)
    ) {
      onVisibleMonthChange(parsed.year, parsed.month);
    }

    setFocusDate(next);
  }

  /**
   * Shows another month when that month still has a choosable day.
   */
  function showShiftedMonth(deltaMonths: number) {
    const next = shiftMonth(visibleYear, visibleMonth, deltaMonths);
    if (!monthHasSelectableDay(next.year, next.month, min, max)) {
      return;
    }

    onVisibleMonthChange(next.year, next.month);
  }

  const previousMonth = shiftMonth(visibleYear, visibleMonth, -1);
  const nextMonth = shiftMonth(visibleYear, visibleMonth, 1);
  const previousYear = shiftMonth(visibleYear, visibleMonth, -12);
  const nextYear = shiftMonth(visibleYear, visibleMonth, 12);

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center justify-between gap-2">
        <div className="flex gap-1">
          <Button
            type="button"
            variant="ghost"
            size="icon"
            aria-label="Previous year"
            disabled={
              !monthHasSelectableDay(
                previousYear.year,
                previousYear.month,
                min,
                max,
              )
            }
            onClick={() => showShiftedMonth(-12)}
          >
            <ChevronsLeft className="size-4" />
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            aria-label="Previous month"
            disabled={
              !monthHasSelectableDay(
                previousMonth.year,
                previousMonth.month,
                min,
                max,
              )
            }
            onClick={() => showShiftedMonth(-1)}
          >
            <ChevronLeft className="size-4" />
          </Button>
        </div>
        <p className="text-sm font-medium text-foreground">
          {formatMonthLabel(visibleYear, visibleMonth)}
        </p>
        <div className="flex gap-1">
          <Button
            type="button"
            variant="ghost"
            size="icon"
            aria-label="Next month"
            disabled={
              !monthHasSelectableDay(nextMonth.year, nextMonth.month, min, max)
            }
            onClick={() => showShiftedMonth(1)}
          >
            <ChevronRight className="size-4" />
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            aria-label="Next year"
            disabled={
              !monthHasSelectableDay(nextYear.year, nextYear.month, min, max)
            }
            onClick={() => showShiftedMonth(12)}
          >
            <ChevronsRight className="size-4" />
          </Button>
        </div>
      </div>

      <div role="grid" aria-label={formatMonthLabel(visibleYear, visibleMonth)}>
        <div role="row" className="grid grid-cols-7">
          {weekdayLabels.map((weekday) => (
            <div
              key={weekday}
              role="columnheader"
              className="py-1 text-center text-xs font-medium text-muted-foreground"
            >
              {weekday}
            </div>
          ))}
        </div>
        {chunkWeeks(days).map((week) => (
          <div key={week[0]?.date} role="row" className="grid grid-cols-7">
            {week.map((day) => {
              const disabled = !isDateInRange(day.date, min, max);
              const selected = day.date === value;
              const isToday = day.date === today;

              return (
                <button
                  key={day.date}
                  ref={(node) => {
                    if (node) {
                      dayRefs.current.set(day.date, node);
                    } else {
                      dayRefs.current.delete(day.date);
                    }
                  }}
                  type="button"
                  role="gridcell"
                  disabled={disabled}
                  aria-selected={selected}
                  aria-current={isToday ? "date" : undefined}
                  aria-label={formatDateLabel(day.date) ?? day.date}
                  onClick={() => onSelect(day.date)}
                  onKeyDown={(event) => {
                    const delta = arrowDelta(event.key);
                    if (delta === null) {
                      return;
                    }

                    event.preventDefault();
                    moveFocus(day.date, delta);
                  }}
                  className={cn(
                    "mx-auto flex size-10 items-center justify-center rounded-lg text-sm",
                    "focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none",
                    day.inMonth
                      ? "text-foreground"
                      : "text-muted-foreground/55",
                    selected && "bg-primary text-primary-foreground",
                    isToday && !selected && "text-primary ring-1 ring-primary",
                    !selected && !disabled && "hover:bg-accent",
                    disabled && "pointer-events-none opacity-30",
                  )}
                >
                  {Number(day.date.slice(8, 10))}
                </button>
              );
            })}
          </div>
        ))}
      </div>

      <div className="flex items-center justify-between border-t border-border/70 pt-2">
        <Button type="button" variant="ghost" onClick={onClear}>
          Clear
        </Button>
        <Button
          type="button"
          variant="ghost"
          onClick={onToday}
          disabled={!todayAllowed}
        >
          Today
        </Button>
      </div>
    </div>
  );
}

/**
 * Month to show first. A stored date wins. Otherwise the calendar opens on today.
 */
function visibleMonthFor(value: string) {
  return (
    parseDateInput(value) ??
    parseDateInput(todayDateInput()) ?? {
      year: 2000,
      month: 1,
    }
  );
}

/**
 * Day that receives focus when the calendar opens.
 * The stored date wins, then today, then the earliest bound.
 */
function startingFocusDate(
  value: string,
  today: string,
  min?: string,
  max?: string,
) {
  if (value && isDateInRange(value, min, max)) {
    return value;
  }

  if (isDateInRange(today, min, max)) {
    return today;
  }

  return min ?? max ?? today;
}

/**
 * Arrow-key step in days. Other keys are null so the button keeps them.
 */
function arrowDelta(key: string) {
  switch (key) {
    case "ArrowLeft":
      return -1;
    case "ArrowRight":
      return 1;
    case "ArrowUp":
      return -7;
    case "ArrowDown":
      return 7;
    default:
      return null;
  }
}

/**
 * Splits the 42-day grid into weeks.
 */
function chunkWeeks<T>(days: T[]) {
  const weeks: T[][] = [];
  for (let index = 0; index < days.length; index += 7) {
    weeks.push(days.slice(index, index + 7));
  }

  return weeks;
}
