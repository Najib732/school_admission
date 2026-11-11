"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import Alert from "@/components/Alert";
import StatusPill from "@/components/StatusPill";
import { useStudent } from "@/components/StudentProvider";
import {
  cancelHold,
  confirmHold,
  formatCountdown,
  formatDateTime,
  getHold,
  HoldItem,
  liveStatus,
  secondsLeft,
} from "@lib/application";

const REFRESH_MS = 5000;

export default function Page() {
  const { holdId } = useParams<{ holdId: string }>();
  const { studentId, isValid, ready } = useStudent();
  const [hold, setHold] = useState<HoldItem | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [now, setNow] = useState(() => Date.now());

  const load = useCallback(async () => {
    try {
      const data = await getHold(holdId);
      setHold(data);
      setLoadError(null);
    } catch (err) {
      setLoadError((err as Error).message);
    }
  }, [holdId]);

  // The status comes from the server every few seconds; the clock ticks every second.
  useEffect(() => {
    let active = true;

    async function refresh() {
      try {
        const data = await getHold(holdId);
        if (active) {
          setHold(data);
          setLoadError(null);
        }
      } catch (err) {
        if (active) {
          setLoadError((err as Error).message);
        }
      }
    }

    refresh();
    const refreshTimer = setInterval(refresh, REFRESH_MS);
    const tick = setInterval(() => setNow(Date.now()), 1000);
    return () => {
      active = false;
      clearInterval(refreshTimer);
      clearInterval(tick);
    };
  }, [holdId]);

  async function pay() {
    setActionError(null);
    setNotice(null);
    setBusy(true);
    try {
      setHold(await confirmHold(holdId, studentId));
      setNotice("Payment received. The admission is confirmed.");
    } catch (err) {
      setActionError((err as Error).message);
      await load();
    } finally {
      setBusy(false);
    }
  }

  async function cancel() {
    setActionError(null);
    setNotice(null);
    setBusy(true);
    try {
      await cancelHold(holdId, studentId);
      setNotice("The application was cancelled and the seat is free for others.");
      await load();
    } catch (err) {
      setActionError((err as Error).message);
      await load();
    } finally {
      setBusy(false);
    }
  }

  if (!hold) {
    return (
      <main>
        <p>
          <Link href="/applications">← My applications</Link>
        </p>
        {loadError ? <Alert kind="error">{loadError}</Alert> : <p>Loading the application…</p>}
      </main>
    );
  }

  const status = liveStatus(hold, now);
  const ownsIt = isValid && hold.studentId === studentId;
  const canAct = status === "HELD" && ownsIt && !busy;

  return (
    <main>
      <p>
        <Link href="/applications">← My applications</Link>
      </p>

      <h1>{hold.className}</h1>
      <p>
        Status: <StatusPill status={status} />
      </p>

      {notice ? <Alert kind="success">{notice}</Alert> : null}
      {actionError ? <Alert kind="error">{actionError}</Alert> : null}
      {loadError ? <Alert kind="warning" title="Not updating.">{loadError}</Alert> : null}
      {ready && status === "HELD" && !ownsIt ? (
        <Alert kind="info">
          To pay or cancel, write the birth registration number this application was made with ({hold.studentId}) in the
          box at the top.
        </Alert>
      ) : null}

      {status === "HELD" ? (
        <p>
          <strong>{formatCountdown(secondsLeft(hold.expiresAt, now))}</strong> left to pay the admission fee. If it
          isn&apos;t paid by {formatDateTime(hold.expiresAt)}, the seat goes back to other applicants.
        </p>
      ) : null}
      {status === "EXPIRED" ? (
        <p>Time is up. The seat was released. You can apply again if the class still has free seats.</p>
      ) : null}
      {status === "CONFIRMED" ? (
        <p>Admission confirmed{hold.confirmedAt ? ` on ${formatDateTime(hold.confirmedAt)}` : ""}.</p>
      ) : null}
      {status === "CANCELLED" ? (
        <p>This application was cancelled{hold.cancelledAt ? ` on ${formatDateTime(hold.cancelledAt)}` : ""}.</p>
      ) : null}

      <table>
        <tbody>
          <tr>
            <th>Student (birth registration no.)</th>
            <td>{hold.studentId}</td>
          </tr>
          <tr>
            <th>Application ID</th>
            <td>{hold.holdId}</td>
          </tr>
          <tr>
            <th>Applied at</th>
            <td>{formatDateTime(hold.createdAt)}</td>
          </tr>
          <tr>
            <th>Pay before</th>
            <td>{formatDateTime(hold.expiresAt)}</td>
          </tr>
        </tbody>
      </table>

      {status === "HELD" || status === "EXPIRED" ? (
        <p>
          <button type="button" disabled={!canAct} onClick={pay}>
            {busy ? "Please wait…" : "Pay admission fee"}
          </button>{" "}
          {status === "HELD" ? (
            <button type="button" disabled={!canAct} onClick={cancel}>
              Cancel application
            </button>
          ) : (
            <Link href="/">Apply again</Link>
          )}
        </p>
      ) : null}
    </main>
  );
}
