"use client";

import { createContext, useContext, useEffect, useState } from "react";
import { isValidStudentId, loadStudentId, normalizeStudentId, saveStudentId } from "@lib/student";

interface StudentContextValue {
  studentId: string;
  isValid: boolean;
  // false until the saved number has been read from this device
  ready: boolean;
  setStudentId: (value: string) => void;
}

const StudentContext = createContext<StudentContextValue | null>(null);

export function StudentProvider({ children }: { children: React.ReactNode }) {
  const [studentId, setValue] = useState("");
  const [ready, setReady] = useState(false);

  useEffect(() => {
    // localStorage only exists in the browser
    setValue(loadStudentId());
    setReady(true);
  }, []);

  function setStudentId(value: string) {
    const clean = normalizeStudentId(value);
    setValue(clean);
    saveStudentId(clean);
  }

  return (
    <StudentContext.Provider value={{ studentId, isValid: isValidStudentId(studentId), ready, setStudentId }}>
      {children}
    </StudentContext.Provider>
  );
}

export function useStudent(): StudentContextValue {
  const value = useContext(StudentContext);
  if (!value) {
    throw new Error("useStudent must be used inside StudentProvider");
  }
  return value;
}
