import { serverClient } from "../server-client";
import type { GroupDetailDto, GroupDto, SubGroupDto } from "../types";

export async function getGroups(): Promise<GroupDto[]> {
  const response = await serverClient.get<GroupDto[]>("/api/groups");
  return response.data;
}

export async function getGroupById(id: string): Promise<GroupDetailDto> {
  const response = await serverClient.get<GroupDetailDto>(`/api/groups/${id}`);
  return response.data;
}

export async function getSubGroups(groupId?: string): Promise<SubGroupDto[]> {
  const response = await serverClient.get<SubGroupDto[]>("/api/subgroups", {
    params: groupId ? { groupId } : undefined,
  });
  return response.data;
}

export async function getSubGroupById(id: string): Promise<SubGroupDto> {
  const response = await serverClient.get<SubGroupDto>(`/api/subgroups/${id}`);
  return response.data;
}
