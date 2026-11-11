# AI usage

**Tool:** Claude (Anthropic), in Claude Code.

## Where I used it

- **Database design.** I designed the tables first (`class` and `order`), then reviewed them with Claude and fixed what was missing:
  - `order` is a reserved SQL word, so the table became `Holds`.
  - The id types didn't match.
  - The status list had no `EXPIRED`.
  - The 17-digit student id needed its own type.
  - We also worked out how to keep the seat counters correct under parallel requests: a row lock, one conditional `UPDATE`, and database constraints.
- **UI.** We made a sample design of all screens first, then built the real pages from it.
- **Frontend.** Claude wrote the frontend code following my own frontend style guide. I reviewed each step and commit.
- **Docker.** Before this project I already knew how to run a project with Docker and write a Dockerfile (SDK, project version, URL). But I had no experience running the whole project from one `docker-compose.yml` at the root. I had also always used a `.env` file, while here every value is read from `docker-compose.yml`. I used Claude to learn and set up both.

## Where the AI was wrong, and how I caught it

- **Paying returned the old status** (`HELD` instead of `CONFIRMED`). EF Core returned a cached row. I caught it by calling the API by hand, and it was fixed with `AsNoTracking`.
- **Changing `API_URL` seemed to have no effect.** The old Next.js server was still running, so the new one couldn't start. I caught it by checking which process held port 3000.
- **Next.js 16 works differently from what the AI knew** (`cacheComponents`). It read the installed version's docs and used `await connection()` instead of guessing.
- **A lint error** (`setState` inside an effect). `eslint` caught it, and it was fixed.
- **Mixed EF Core versions** gave a build warning. Fixed by pinning the same version.

Everything was checked by building, by the 11 backend tests, and by clicking through the app in a browser.
