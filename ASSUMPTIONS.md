# Assumptions

Things the brief does not say, what I decided, and why.

## Classes

- **A class is one shift** (e.g. `Class 6 - Morning Shift`). `POST /classes` only takes `name` and `totalSeats`.
- **Class names are unique** (ignoring case), so the admin can't create the same class twice. A duplicate returns 409.
- **Seats: 1 to 100,000.** No editing or deleting classes, because the brief doesn't ask for it.
- **No admin login**, because the brief says there is no login.

## Students and applications

- **The student is the 17-digit birth registration number** in `X-Student-Id`. Anything else returns 400. Bangla digits are accepted.
- **One active (`HELD` or `CONFIRMED`) application per student per class.** After it expires or is cancelled, the student can apply again.
- **A student can apply to several classes at once**, because the brief only limits one class.
- **Someone else's application returns 404**, not 403, so application ids can't be guessed.
- **Only a `HELD` application can be cancelled.** Cancelling a `CONFIRMED` one would need a refund, so it returns 409.
- **Paying twice returns 200** and counts the seat once. Paying after the time runs out returns 409.

## Expiry

- **A background worker frees expired holds every 5 seconds**, so nobody has to call an API. An overdue hold is already reported as `EXPIRED` and can't be paid, even before the worker runs.
- **The server decides expiry.** The browser countdown is only for display.

## Other

- **Payment is simulated.** "Pay admission fee" always succeeds, as the brief says.
- **Next.js instead of Vite.** The brief needs React 18+; Vite was only a suggestion.
- **The student number is saved in `localStorage`**, so it is still there after a refresh or reopening the browser.
