type Kind = "error" | "warning" | "success" | "info";

export default function Alert({ kind, title, children }: { kind: Kind; title?: string; children: React.ReactNode }) {
  const className = kind === "error" || kind === "warning" ? "error" : kind === "success" ? "success" : undefined;
  return (
    <p role={kind === "error" || kind === "warning" ? "alert" : "status"} className={className}>
      {title ? <strong>{title} </strong> : null}
      {children}
    </p>
  );
}
