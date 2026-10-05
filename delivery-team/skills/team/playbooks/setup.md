
# Playbook: setup (CONSTRAINTS.md and repo wiring)

Run once per repo, interactively. Every change is shown to the user before it's written.

## 1. Detect before asking
Read and report in three lines: solution file, target frameworks, test framework and runner, existing analyzers (`Directory.Build.props`, `.editorconfig`, package references), CI system, current warning policy. Run the full test suite once with coverage (coverlet) to measure today's numbers.

## 2. Four questions, each with a default
Ask in one round, each with the default stated:
1. Beyond the floor, which dimensions to enforce: coverage of changed lines, dependency vulnerabilities, secret scanning, mutation testing on changed files, architecture rules? **Default:** coverage + vulnerabilities + secrets.
2. When a check fails mid-task: block or warn? **Default:** block on the floor, warn on the rest for the first two weeks.
3. Target numbers, or measure today and hold the line (ratchet)? **Default:** ratchet.
4. Slowest acceptable check when a task finishes? **Default:** 90 seconds; slower checks run at board or in CI.

## 3. Write `CONSTRAINTS.md`
```
# Constraints
Last reviewed: <date>
## Floor (always enforced)
- Warnings stay errors; no new `#pragma warning disable`, `[SuppressMessage]`, `<NoWarn>` without an Exceptions row
- No skipped, deleted or assertion-stripped tests without a reviewed reason
- No `NotImplementedException`, empty catch, or `[ExcludeFromCodeCoverage]` added to production code
- No secrets in source
- This file is never weakened to make a change pass
## Enforced with numbers
| Dimension | Rule | Checked by | Runs at |
## Measured, not yet enforced (ratchets)
| Metric | Today | Direction |
## Exceptions
| ID | Rule | Path | Reason | Owner | Expires |
```
Every enforced row names its command. Suggested commands: `dotnet build -warnaserror`, `dotnet format --verify-no-changes`, `dotnet test --collect:"XPlat Code Coverage"` + changed-line intersection, `dotnet list package --vulnerable --include-transitive`, `gitleaks detect --redact --no-banner`, `dotnet stryker --mutate <changed files>`. At least one row is an external check the team's own tests can't satisfy.

## 4. Compiler-enforced rules (show the diff, apply on a yes)
- `Directory.Build.props`: `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<AnalysisLevel>latest-recommended</AnalysisLevel>`, `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`. On a large existing repo, propose this per project or as a ratchet.
- `Microsoft.CodeAnalysis.BannedApiAnalyzers` + `BannedSymbols.txt`: `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow` (use TimeProvider), `System.Net.Http.HttpClient.#ctor` (use IHttpClientFactory), `System.Threading.Thread.Sleep`, `System.Threading.Tasks.Task.Wait`.

## 5. Agent context
- `.claude/settings.json`: allow `dotnet build|test|format|restore|list`, ask on `dotnet ef database update`, env `DOTNET_NOLOGO=1`, `DOTNET_CLI_TELEMETRY_OPTOUT=1`.
- `CLAUDE.md` (short, pointers only): solution and test commands, architecture in one paragraph, and "Read CONSTRAINTS.md before writing code. Use GLOSSARY.md terms."
- `GLOSSARY.md` and `docs/adr/` are created lazily, when the first term or decision is settled.
- Add `.claude/work/` to `.gitignore`.

## 6. Trial
Run the floor guard and the enforced checks on the current branch. Show the results. Adjust anything the user disagrees with before finishing.
