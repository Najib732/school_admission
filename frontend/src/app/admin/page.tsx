"use client";

import { FormEvent, useEffect, useState } from "react";
import Alert from "@/components/Alert";
import { createClass, validateNewClass } from "@lib/admin";
import { ClassItem, filterClasses, getClasses } from "@lib/classes";

const REFRESH_MS = 3000;

export default function Page() {
  const [name, setName] = useState("");
  const [seats, setSeats] = useState("");
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [classes, setClasses] = useState<ClassItem[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);

  async function loadClasses() {
    try {
      setClasses(await getClasses());
      setLoadError(null);
    } catch (err) {
      setLoadError((err as Error).message);
    }
  }

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

  async function submit(e: FormEvent) {
    e.preventDefault();
    setSuccess(null);

    const problem = validateNewClass(name, seats);
    if (problem) {
      setFormError(problem);
      return;
    }

    setFormError(null);
    setSaving(true);
    try {
      const created = await createClass({ name: name.trim(), totalSeats: Number(seats) });
      setSuccess(`${created.name} created with ${created.totalSeats} seats.`);
      setName("");
      setSeats("");
      await loadClasses();
    } catch (err) {
      setFormError((err as Error).message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <main>
      <h1>Create a class</h1>
      <p>Each shift is its own class, e.g. Class 6 - Morning Shift.</p>

      <form onSubmit={submit} noValidate>
        {success ? <Alert kind="success">{success}</Alert> : null}
        {formError ? <Alert kind="error">{formError}</Alert> : null}

        <p>
          <label htmlFor="class-name">Class name: </label>
          <input id="class-name" value={name} maxLength={200} onChange={(e) => setName(e.target.value)} />
        </p>
        <p>
          <label htmlFor="total-seats">Total seats: </label>
          <input id="total-seats" inputMode="numeric" value={seats} onChange={(e) => setSeats(e.target.value)} />
        </p>
        <button type="submit" disabled={saving}>
          {saving ? "Creating…" : "Create class"}
        </button>
      </form>

      <h2>Existing classes</h2>
      {loadError ? <Alert kind="warning">{loadError}</Alert> : null}
      {classes === null && !loadError ? <p>Loading…</p> : null}
      {classes !== null && classes.length === 0 ? <p>No classes yet.</p> : null}
      {classes !== null && classes.length > 0 ? (
        <ul>
          {filterClasses(classes, "", false).map((c) => (
            <li key={c.id}>
              {c.name}: {c.totalSeats} seats, {c.availableSeats} available
            </li>
          ))}
        </ul>
      ) : null}
    </main>
  );
}
