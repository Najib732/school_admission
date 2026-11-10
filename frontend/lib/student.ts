// The student's 17-digit birth registration number, kept on this device so it survives a
// refresh or closing the browser.

const STORAGE_KEY = "school-admission.studentId";
const BANGLA_DIGITS = "০১২৩৪৫৬৭৮৯";

// Accepts Bangla digits too, and drops spaces or dashes people type.
export function normalizeStudentId(input: string): string {
  const latin = input.replace(/[০-৯]/g, (d) => String(BANGLA_DIGITS.indexOf(d)));
  return latin.replace(/\D/g, "").slice(0, 17);
}

export function isValidStudentId(value: string): boolean {
  return /^\d{17}$/.test(value);
}

export function loadStudentId(): string {
  try {
    return normalizeStudentId(window.localStorage.getItem(STORAGE_KEY) ?? "");
  } catch {
    return "";
  }
}

export function saveStudentId(value: string): void {
  try {
    window.localStorage.setItem(STORAGE_KEY, value);
  } catch {
    // Private mode or blocked storage: the number just won't be remembered.
  }
}
