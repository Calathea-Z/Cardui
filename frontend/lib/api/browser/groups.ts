import { browserClient } from "../browser-client";
import type {
  CreateSubGroupDto,
  GroupDetailDto,
  GroupDto,
  SubGroupDto,
  UpdateSubGroupDto,
} from "../types";

export async function getGroups(): Promise<GroupDto[]> {
  const response = await browserClient.get<GroupDto[]>("/api/groups");
  return response.data;
}

export async function getGroupById(id: string): Promise<GroupDetailDto> {
  const response = await browserClient.get<GroupDetailDto>(`/api/groups/${id}`);
  return response.data;
}

export async function getSubGroups(groupId?: string): Promise<SubGroupDto[]> {
  const response = await browserClient.get<SubGroupDto[]>("/api/subgroups", {
    params: groupId ? { groupId } : undefined,
  });
  return response.data;
}

export async function getSubGroupById(id: string): Promise<SubGroupDto> {
  const response = await browserClient.get<SubGroupDto>(`/api/subgroups/${id}`);
  return response.data;
}

export async function createSubGroup(
  dto: CreateSubGroupDto,
): Promise<SubGroupDto> {
  const response = await browserClient.post<SubGroupDto>("/api/subgroups", dto);
  return response.data;
}

export async function updateSubGroup(
  id: string,
  dto: UpdateSubGroupDto,
): Promise<SubGroupDto> {
  const response = await browserClient.patch<SubGroupDto>(
    `/api/subgroups/${id}`,
    dto,
  );
  return response.data;
}

export async function deleteSubGroup(id: string): Promise<void> {
  await browserClient.delete(`/api/subgroups/${id}`);
}
