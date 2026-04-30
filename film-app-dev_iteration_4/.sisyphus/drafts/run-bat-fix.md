# Draft: Fix run.bat script

## Requirements (confirmed)
- The script must start **both the front‑end** and the **back‑end** of the Film‑App project when executed.

## Open Questions
1. **Current contents** – What does `run.bat` presently contain? (Paste the file contents.)
2. **Intended start commands** –
   - Which command starts the front‑end? (e.g., `npm run dev`, `pnpm start`, `yarn dev`, etc.)
   - Which command starts the back‑end? (e.g., `npm run server`, `node server.js`, `dotnet run`, etc.)
3. **Error you see** – What exact error or output appears when you run the batch file now? (Copy‑paste the console text.)
4. **Dependencies** – Do the start commands rely on any tools that must be installed first (Node, pnpm, Docker, etc.)?
5. **Environment** – Should the batch file work in:
   - Windows **Command Prompt** (`cmd.exe`) only,
   - **PowerShell**, or
   - Both (so it can be run from either shell)?
6. **Portability** – Will the script ever be executed from **WSL** or a CI pipeline, or is it strictly for a local Windows developer machine?

## Scope Boundaries
- **INCLUDE:** Fixing the batch‑file logic, adding any needed path checks, and ensuring clear error messages if a required tool is missing.
- **EXCLUDE:** Refactoring the actual front‑end or back‑end code, unless a change is required solely to make the start commands callable from the batch file.

## Technical Decisions (to be decided)
- Use pure batch syntax vs. a PowerShell wrapper.
- Whether to run the two processes **concurrently** (e.g., using `start` or `&`) or sequentially.
- How to propagate exit codes / handle failure of one side while the other is running.

## Research Findings (to be filled after we gather context)

## Test Strategy (pre‑decided)
- **Manual verification:** Run `run.bat` and confirm both servers start and stay alive.
- **Optional automated check:** A small PowerShell script that launches `run.bat`, waits a few seconds, and verifies that the expected ports are listening (e.g., 3000 for front‑end, 5000 for back‑end). We'll decide based on your preference.
