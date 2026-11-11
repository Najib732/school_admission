"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import Alert from "@/components/Alert";
import StatusPill from "@/components/StatusPill";
import { useStudent } from "@/components/StudentProvider";
import { formatCountdown, formatDateTime, HoldItem, liveStatus, secondsLeft } from "@lib/application";
import { getMyHolds } from "@lib/applications";

const REFRESH_MS = 5000;

export default function Page() {
  const { studentId, isValid, ready } = useStudent();
  const [holds, setHolds] = useState<HoldItem[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    if (!isValid) {
      return;
    }

    let active = true;

    async function load() {
      try {
        const data = await getMyHolds(studentId);
        if (active) {
          setHolds(data);
          setLoadError(null);
        }
      } catch (err) {
        if (active) {
          setLoadError((err as Error).message);
        }
      }
    }

    load();
    const refresh = setInterval(load, REFRESH_MS);
    const tick = setInterval(() => setNow(Date.now()), 1000);
    return () => {
      active = false;
      clearInterval(refresh);
      clearInterval(tick);
    };
  }, [studentId, isValid]);

  return (
    <main>
      <h1>My applications</h1>

      {ready && !isValid ? (
        <Alert kind="info">Write the child&apos;s 17-digit birth registration number in the box at the top to see the applications.</Alert>
      ) : null}

      {isValid && loadError ? <Alert kind="warning" title="Not updating.">{loadError}</Alert> : null}

      {isValid && holds === null && !loadError ? <p>Loading applications…</p> : null}

      {isValid && holds !== null && holds.length === 0 ? (
        <p>
          No applications yet for this number. <Link href="/">Find a class</Link> to apply.
        </p>
      ) : null}

      {isValid && holds && holds.length > 0 ? (
        <table>
          <thead>
            <tr>
              <th>Class</th>
              <th>Status</th>
              <th>Details</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {holds.map((h) => {
              const status = liveStatus(h, now);
              return (
                <tr key={h.holdId}>
                  <td>{h.className}</td>
                  <td>
                    <StatusPill status={status} />
                  </td>
                  <td>{describe(h, status, now)}</td>
                  <td>
                    <Link href={`/applications/${h.holdId}`}>{status === "HELD" ? "Pay fee" : "Open"}</Link>
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

function describe(h: HoldItem, status: HoldItem["status"], now: number): string {
  switch (status) {
    case "HELD":
      return `${formatCountdown(secondsLeft(h.expiresAt, now))} left to pay (until ${formatDateTime(h.expiresAt)})`;
    case "CONFIRMED":
      return `Admission confirmed${h.confirmedAt ? ` on ${formatDateTime(h.confirmedAt)}` : ""}`;
    case "EXPIRED":
      return `Time ran out on ${formatDateTime(h.expiresAt)}`;
    case "CANCELLED":
      return `Cancelled${h.cancelledAt ? ` on ${formatDateTime(h.cancelledAt)}` : ""}`;
  }
}
