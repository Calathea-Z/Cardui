import { serverClient } from "../server-client";
import type { GroupDto, SubGroupDto } from "../types";

/**
 * GET /api/groups
 * Lists category groups for server-rendered pages.
 */
export async function getGroups(): Promise<GroupDto[]> {
  const response = await serverClient.get<GroupDto[]>("/api/groups");
  return response.data;
}

/**
 * GET /api/subgroups
 * Lists subgroups. A group id limits the list to that group.
 */
export async function getSubGroups(groupId?: string): Promise<SubGroupDto[]> {
  const response = await serverClient.get<SubGroupDto[]>("/api/subgroups", {
    params: groupId ? { groupId } : undefined,
  });
  return response.data;
}
