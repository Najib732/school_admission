import api from "./axios";
import { friendlyError } from "./errors";
import type { ClassItem } from "./classes";

export interface NewClass {
  name: string;
  totalSeats: number;
}

export async function createClass(input: NewClass): Promise<ClassItem> {
  try {
    const res = await api.post<ClassItem>("/classes", input);
    return res.data;
  } catch (err) {
    throw new Error(friendlyError(err));
  }
}

// Checked before sending, so the admin sees the problem right away.
export function validateNewClass(name: string, seats: string): string | null {
  if (name.trim() === "") {
    return "Please write the class name, e.g. Class 6 - Morning Shift.";
  }
  if (name.trim().length > 200) {
    return "The class name must be at most 200 characters.";
  }
  if (!/^\d+$/.test(seats.trim()) || Number(seats) < 1 || Number(seats) > 100000) {
    return "Total seats must be a whole number between 1 and 100000.";
  }
  return null;
}
