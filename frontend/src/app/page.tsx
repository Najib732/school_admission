"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Alert from "@/components/Alert";
import { useStudent } from "@/components/StudentProvider";
import { applyForClass, ClassItem, filterClasses, getClasses } from "@lib/classes";

const REFRESH_MS = 3000;

export default function Page() {
  const router = useRouter();
  const { studentId, isValid } = useStudent();
  const [classes, setClasses] = useState<ClassItem[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [needsStudent, setNeedsStudent] = useState(false);
  const [applyingId, setApplyingId] = useState<string | null>(null);

  // Seat numbers refresh every few seconds without reloading the page.
  useEffect(() => {
    let active = true;

    async function load() {
      try {
        const data = await getClasses();
        if (active) {
          setClasses(data);
          setLoadError(null);
        }
      } catch (err) {
        if (active) {
          setLoadError((err as Error).message);
        }
      }
    }

    load();
    const timer = setInterval(load, REFRESH_MS);
    return () => {
      active = false;
      clearInterval(timer);
    };
  }, []);

  async function apply(item: ClassItem) {
    setActionError(null);
    if (!isValid) {
      setNeedsStudent(true);
      return;
    }

    setApplyingId(item.id);
    try {
      const hold = await applyForClass(item.id, studentId);
      router.push(`/applications/${hold.holdId}`);
    } catch (err) {
      setActionError((err as Error).message);
      setApplyingId(null);
    }
  }

  return (
    <main>
      <h1>Classes</h1>
      <p>Seats update every 3 seconds.</p>

      {needsStudent && !isValid ? (
        <Alert kind="error">Write the child&apos;s 17-digit birth registration number in the box at the top of the page first.</Alert>
      ) : null}
      {actionError ? <Alert kind="error">{actionError}</Alert> : null}
      {loadError ? (
        <Alert kind="warning" title={classes ? "Showing the last numbers we got." : "Classes could not be loaded."}>
          {loadError}
        </Alert>
      ) : null}

      {classes === null && !loadError ? <p>Loading classes…</p> : null}
      {classes !== null && classes.length === 0 ? <p>No classes yet. The school admin can add them on the Admin page.</p> : null}

      {classes !== null && classes.length > 0 ? (
        <table>
          <thead>
            <tr>
              <th>Class</th>
              <th>Total</th>
              <th>Held</th>
              <th>Confirmed</th>
              <th>Available</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {filterClasses(classes, "", false).map((c) => {
              const full = c.availableSeats <= 0;
              return (
                <tr key={c.id}>
                  <td>{c.name}</td>
                  <td>{c.totalSeats}</td>
                  <td>{c.heldSeats}</td>
                  <td>{c.confirmedSeats}</td>
                  <td>{c.availableSeats}</td>
                  <td>
                    <button type="button" disabled={full || applyingId !== null} onClick={() => apply(c)}>
                      {full ? "Full" : applyingId === c.id ? "Applying…" : "Apply"}
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      ) : null}
    </main>
  );
}
