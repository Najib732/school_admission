import api, { studentHeader } from "./axios";
import { friendlyError } from "./errors";
import type { HoldItem } from "./application";

export interface ClassItem {
  id: string;
  name: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
}

export async function getClasses(): Promise<ClassItem[]> {
  try {
    const res = await api.get<ClassItem[]>("/classes");
    return res.data;
  } catch (err) {
    throw new Error(friendlyError(err));
  }
}

export async function applyForClass(classId: string, studentId: string): Promise<HoldItem> {
  try {
    const res = await api.post<HoldItem>(`/classes/${classId}/holds`, null, studentHeader(studentId));
    return res.data;
  } catch (err) {
    throw new Error(friendlyError(err));
  }
}

// Parents search by class name: "class 6", "Class 6", "morning" all match.
export function filterClasses(classes: ClassItem[], query: string, onlyFree: boolean): ClassItem[] {
  const q = query.trim().toLowerCase();
  return classes
    .filter((c) => (q === "" || c.name.toLowerCase().includes(q)) && (!onlyFree || c.availableSeats > 0))
    .sort((a, b) => a.name.localeCompare(b.name, undefined, { numeric: true, sensitivity: "base" }));
}
