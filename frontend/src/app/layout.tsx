import type { Metadata } from "next";
import { connection } from "next/server";
import Header from "@/components/Header";
import { StudentProvider } from "@/components/StudentProvider";
import "./globals.css";

export const metadata: Metadata = {
  title: "School Admission",
  description: "Apply for a class, pay the admission fee and follow your applications.",
};

export default async function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  // Wait for a real request so API_URL (set by docker-compose.yml) is read when the page loads,
  // not frozen at build time.
  await connection();
  const apiUrl = process.env.API_URL ?? "";
  const script = `window.__API_URL__=${JSON.stringify(apiUrl).replace(/</g, "\\u003c")};`;

  return (
    <html lang="en">
      <body>
        <script dangerouslySetInnerHTML={{ __html: script }} />
        <StudentProvider>
          <Header />
          {children}
        </StudentProvider>
      </body>
    </html>
  );
}
