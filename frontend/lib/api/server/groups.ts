import { serverClient } from "../server-client";
import type { GroupDto, SubGroupDto } from "../types";

export async function getGroups(): Promise<GroupDto[]> {
  const response = await serverClient.get<GroupDto[]>("/api/groups");
  return response.data;
}

export async function getSubGroups(groupId?: string): Promise<SubGroupDto[]> {
  const response = await serverClient.get<SubGroupDto[]>("/api/subgroups", {
    params: groupId ? { groupId } : undefined,
  });
  return response.data;
}
