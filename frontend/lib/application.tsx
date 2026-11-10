import api, { studentHeader } from "./axios";
import { friendlyError } from "./errors";

export type HoldStatus = "HELD" | "CONFIRMED" | "EXPIRED" | "CANCELLED";

export interface HoldItem {
  holdId: string;
  classId: string;
  className: string;
  studentId: string;
  status: HoldStatus;
  createdAt: string;
  expiresAt: string;
  confirmedAt: string | null;
  cancelledAt: string | null;
}

export async function getHold(holdId: string): Promise<HoldItem> {
  try {
    const res = await api.get<HoldItem>(`/holds/${holdId}`);
    return res.data;
  } catch (err) {
    throw new Error(friendlyError(err));
  }
}

export async function confirmHold(holdId: string, studentId: string): Promise<HoldItem> {
  try {
    const res = await api.post<HoldItem>(`/holds/${holdId}/confirm`, null, studentHeader(studentId));
    return res.data;
  } catch (err) {
    throw new Error(friendlyError(err));
  }
}

export async function cancelHold(holdId: string, studentId: string): Promise<void> {
  try {
    await api.delete(`/holds/${holdId}`, studentHeader(studentId));
  } catch (err) {
    throw new Error(friendlyError(err));
  }
}

export function secondsLeft(expiresAt: string, now: number): number {
  return Math.max(0, Math.floor((new Date(expiresAt).getTime() - now) / 1000));
}

// A HELD application whose time has run out is shown as EXPIRED right away,
// without waiting for the next refresh from the server.
export function liveStatus(hold: HoldItem, now: number): HoldStatus {
  if (hold.status === "HELD" && secondsLeft(hold.expiresAt, now) === 0) {
    return "EXPIRED";
  }
  return hold.status;
}

export function formatCountdown(totalSeconds: number): string {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

export function formatDateTime(value: string): string {
  return new Date(value).toLocaleString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  });
}
