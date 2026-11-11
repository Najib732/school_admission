import api, { studentHeader } from "./axios";
import { friendlyError } from "./errors";
import type { HoldItem } from "./application";

export async function getMyHolds(studentId: string): Promise<HoldItem[]> {
  try {
    const res = await api.get<HoldItem[]>("/me/holds", studentHeader(studentId));
    return res.data;
  } catch (err) {
    throw new Error(friendlyError(err));
  }
}
