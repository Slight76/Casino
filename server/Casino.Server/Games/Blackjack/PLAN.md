# Blackjack issue execution plan

## Scope

Open `game:blackjack` issues:

1. `#6` `fix(blackjack): tighten PlaceBet locking to remove TOCTOU window`
2. `#10` `refactor(blackjack): dedupe ace-reduction loop between BestTotal and IsSoft`
3. `#13` `chore(blackjack): decide double-after-split allowance`

Explicitly out of scope for this plan:

- `#9 feat: authenticate table creation endpoints` (not tagged `game:blackjack`)
- existing client lint failures found during baseline validation

## Subagent ownership

### Subagent A — issue #6 (locking bug)

Owns only:

- `server/Casino.Server/Hubs/BlackjackHub.cs`
- focused backend tests added under `server/Casino.Server.Tests/` only if required

Must not edit:

- `Hand.cs`
- `BlackjackRules.cs`
- `BlackjackEngine.cs`
- `server/Casino.Server/Games/Blackjack/README.md`

Goal:

- collapse `PlaceBet` validation + round-start trigger into one critical section
- preserve existing broadcast/timer behavior after the lock is released

### Subagent B — issue #10 (ace-loop refactor)

Owns only:

- `server/Casino.Server/Games/Blackjack/Hand.cs`
- `server/Casino.Server.Tests/HandTests.cs`

Must not edit:

- hub code
- engine turn logic
- rules/docs/DTOs

Goal:

- extract the duplicated ace-reduction logic once
- keep `BestTotal()` and `IsSoft` behavior identical

### Subagent C — issue #13 (double-after-split rule)

Owns only:

- `server/Casino.Server/Games/Blackjack/BlackjackRules.cs`
- `server/Casino.Server/Games/Blackjack/BlackjackEngine.cs`
- `server/Casino.Server/Games/Blackjack/BlackjackDtos.cs`
- `server/Casino.Server/Games/Blackjack/README.md`
- `server/Casino.Server.Tests/EngineTests.cs`

May edit client code only if server-driven action availability changes require UI alignment.

Must not edit:

- `Hand.cs` (avoid overlap with issue `#10`)
- `BlackjackHub.cs` (avoid overlap with issue `#6`)

Goal:

- make the rule explicit instead of leaving double-after-split as implicit behavior
- enforce the decision in engine logic and server DTO/action availability
- cover both the chosen rule and its edge cases in tests

## Parallel execution rules

- Subagents A and B can run in parallel immediately.
- Subagent C can also run in parallel, but it must not touch `Hand.cs`; any double-after-split restriction must be implemented at the rules/engine/DTO layer.
- No subagent may edit another subagent's owned files.
- If a task expands beyond its owned files, stop and hand the dependency back to the coordinator instead of freelancing into another lane.

## Merge order

1. Merge issue `#10` first if ready, because it is isolated and low risk.
2. Merge issue `#6` next once concurrency behavior and regression checks pass.
3. Merge issue `#13` last, because it changes game rules, tests, and rule documentation together.

## Required lifecycle for every subagent

1. Re-read the assigned issue before editing code.
2. Make the smallest possible change set inside the owned files only.
3. Add or update focused backend tests for the assigned behavior.
4. Run targeted tests first.
5. Run `dotnet test Casino.sln` before handing work back.
6. Scan changed files for secrets before commit.
7. Run final automated review/security validation before completion.
8. Report progress with:
   - what changed
   - what was tested
   - any uncertainty or follow-up needed
9. Do not pick up a second Blackjack issue in the same workstream.

## Coordinator checklist

- confirm the issue is still open and still tagged `game:blackjack`
- assign exactly one issue per subagent
- reject any change set that crosses ownership boundaries without approval
- verify each subagent followed the lifecycle above
- combine only after all three workstreams are individually green
