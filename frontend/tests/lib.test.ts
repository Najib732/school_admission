import { AxiosError, AxiosHeaders } from "axios";
import { describe, expect, it } from "vitest";
import { validateNewClass } from "../lib/admin";
import { formatCountdown, HoldItem, liveStatus, secondsLeft } from "../lib/application";
import { ClassItem, filterClasses } from "../lib/classes";
import { friendlyError } from "../lib/errors";
import { isValidStudentId, normalizeStudentId } from "../lib/student";

function cls(name: string, availableSeats = 5): ClassItem {
  return { id: name, name, totalSeats: 10, heldSeats: 0, confirmedSeats: 10 - availableSeats, availableSeats };
}

function hold(status: HoldItem["status"], expiresAt: string): HoldItem {
  return {
    holdId: "h1",
    classId: "c1",
    className: "Class 6 - Morning Shift",
    studentId: "20152691234567890",
    status,
    createdAt: "2026-11-01T04:00:00Z",
    expiresAt,
    confirmedAt: null,
    cancelledAt: null,
  };
}

function httpError(status: number, data: unknown): AxiosError {
  const config = { headers: new AxiosHeaders() };
  return new AxiosError("failed", "ERR_BAD_RESPONSE", config, null, {
    status,
    statusText: "",
    headers: {},
    config,
    data,
  });
}

describe("student number", () => {
  it("turns Bangla digits into English digits and drops other characters", () => {
    expect(normalizeStudentId("২০১৫-২৬৯১ ২৩৪৫৬৭৮৯০")).toBe("20152691234567890");
  });

  it("keeps at most 17 digits", () => {
    expect(normalizeStudentId("201526912345678901234")).toBe("20152691234567890");
  });

  it("accepts exactly 17 digits only", () => {
    expect(isValidStudentId("20152691234567890")).toBe(true);
    expect(isValidStudentId("2015269123456789")).toBe(false);
    expect(isValidStudentId("")).toBe(false);
  });
});

describe("class search", () => {
  const classes = [cls("Class 7 - Day Shift"), cls("Class 10 - Morning Shift"), cls("Class 6 - Morning Shift"), cls("Class 6 - Day Shift", 0)];

  it("finds every shift of a class, ignoring upper and lower case", () => {
    expect(filterClasses(classes, "class 6", false).map((c) => c.name)).toEqual([
      "Class 6 - Day Shift",
      "Class 6 - Morning Shift",
    ]);
  });

  it("can hide full classes", () => {
    expect(filterClasses(classes, "class 6", true).map((c) => c.name)).toEqual(["Class 6 - Morning Shift"]);
  });

  it("sorts class numbers in natural order", () => {
    expect(filterClasses(classes, "", false).map((c) => c.name)).toEqual([
      "Class 6 - Day Shift",
      "Class 6 - Morning Shift",
      "Class 7 - Day Shift",
      "Class 10 - Morning Shift",
    ]);
  });
});

describe("countdown", () => {
  const expiresAt = "2026-11-01T04:10:00Z";
  const before = new Date("2026-11-01T04:07:18Z").getTime();
  const after = new Date("2026-11-01T04:10:01Z").getTime();

  it("shows minutes and seconds left", () => {
    expect(secondsLeft(expiresAt, before)).toBe(162);
    expect(formatCountdown(162)).toBe("02:42");
  });

  it("never goes below zero", () => {
    expect(secondsLeft(expiresAt, after)).toBe(0);
  });

  it("shows a held application as expired as soon as its time is up", () => {
    expect(liveStatus(hold("HELD", expiresAt), before)).toBe("HELD");
    expect(liveStatus(hold("HELD", expiresAt), after)).toBe("EXPIRED");
    expect(liveStatus(hold("CONFIRMED", expiresAt), after)).toBe("CONFIRMED");
  });
});

describe("create class form", () => {
  it("needs a name and a whole number of seats", () => {
    expect(validateNewClass("", "10")).not.toBeNull();
    expect(validateNewClass("Class 6 - Morning Shift", "0")).not.toBeNull();
    expect(validateNewClass("Class 6 - Morning Shift", "12.5")).not.toBeNull();
    expect(validateNewClass("Class 6 - Morning Shift", "abc")).not.toBeNull();
    expect(validateNewClass("Class 6 - Morning Shift", "120")).toBeNull();
  });
});

describe("error messages", () => {
  it("explains when the server can't be reached", () => {
    const config = { headers: new AxiosHeaders() };
    const offline = new AxiosError("Network Error", "ERR_NETWORK", config);
    expect(friendlyError(offline)).toMatch(/Can't reach the server/);
  });

  it("shows the server's own message, e.g. no seats left", () => {
    expect(friendlyError(httpError(409, "No seats left in Class 6 - Day Shift."))).toBe("No seats left in Class 6 - Day Shift.");
  });

  it("shows the first validation message", () => {
    expect(friendlyError(httpError(400, { errors: { Name: ["Class Name Is Required"] } }))).toBe("Class Name Is Required");
  });

  it("never shows raw technical text for server errors", () => {
    expect(friendlyError(httpError(500, ""))).toBe("Something went wrong on the server. Please try again.");
  });
});
