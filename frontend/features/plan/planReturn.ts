/**
 * Where an editor should send the person back.
 * `plan` returns to the working preview. `setup` returns to the setup groups.
 */
export type PlanReturnFrom = "plan" | "setup";

/**
 * Adds the return query to an existing editor route.
 * The route itself stays the same. The query only chooses the back link.
 */
export function planReturnHref(path: string, from: PlanReturnFrom = "plan") {
  return `${path}?from=${from}`;
}
