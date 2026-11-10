import axios from "axios";

// Turns any failed request into a sentence a parent can understand.
export function friendlyError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    if (!err.response) {
      return "Can't reach the server. Check your internet connection; the page keeps trying.";
    }

    const { status, data } = err.response;

    // The backend sends its own readable message as plain text (409, 404, 400).
    if (typeof data === "string" && data.trim() !== "") {
      return data;
    }

    // Validation errors: { errors: { Name: ["Class Name Is Required"] } }
    if (data && typeof data === "object" && "errors" in data) {
      const errors = (data as { errors: Record<string, string[]> }).errors;
      const first = Object.values(errors).flat()[0];
      if (first) {
        return first;
      }
    }

    if (status === 404) {
      return "We couldn't find that. It may have been removed.";
    }
    if (status >= 500) {
      return "Something went wrong on the server. Please try again.";
    }
    return `The request was not accepted (error ${status}).`;
  }

  if (err instanceof Error && err.message) {
    return err.message;
  }
  return "Something went wrong. Please try again.";
}
