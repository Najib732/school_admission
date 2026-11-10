"use client";

import Link from "next/link";
import { useStudent } from "./StudentProvider";

export default function Header() {
  const { studentId, isValid, setStudentId } = useStudent();
  const showHint = studentId.length > 0 && !isValid;

  return (
    <header>
      <nav>
        <Link href="/">Classes</Link> | <Link href="/applications">My applications</Link> | <Link href="/admin">Admin</Link>
      </nav>

      <p>
        <label htmlFor="student-id">Birth registration no.: </label>
        <input
          id="student-id"
          inputMode="numeric"
          autoComplete="off"
          placeholder="17 digits"
          value={studentId}
          onChange={(e) => setStudentId(e.target.value)}
          aria-invalid={showHint}
        />{" "}
        <span className={showHint ? "error" : undefined}>
          {showHint ? `${studentId.length} of 17 digits` : isValid ? "Saved on this device" : "Needed to apply"}
        </span>
      </p>
      <hr />
    </header>
  );
}
