import type { HoldStatus } from "@lib/application";

export default function StatusPill({ status }: { status: HoldStatus }) {
  return <strong>{status}</strong>;
}
