# MovieApi Readability Refactor Backlog

Make MovieApi easier for a mid-level engineer to read and change. Keep the current HTTP contracts, SQL Server schema, query behavior, and concurrency guarantees. Start implementation after approval.

## Prioritization

Score Impact, Confidence, and Ease from 1 to 10. ICE is their product. Use the [ICE backlog template](templates/ICE-BACKLOG.md) for status and acceptance criteria. Work on one item at a time and commit each item's implementation and backlog update together.

| ID | Backlog item | Impact | Confidence | Ease | ICE | Status |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| READ-01 | Make movie read queries easier to scan | 9 | 9 | 8 | 648 | Done |
| READ-02 | Separate cast assignment decisions from database writes | 9 | 9 | 7 | 567 | Done |
| READ-03 | Clarify movie write paths and concurrency handling | 8 | 9 | 8 | 576 | Ready |
| READ-04 | Clarify review collection and creation behavior | 8 | 9 | 8 | 576 | Ready |
| READ-05 | Separate startup setup from the seed transaction | 8 | 9 | 8 | 576 | Ready |
| READ-06 | Organize SQL Server API tests around the resources they exercise | 7 | 8 | 6 | 336 | Ready |

## Backlog items

### READ-01 Make movie read queries easier to scan

Improve the structure of the catalog filters and the nested detail projection in [MoviesController](../src/MovieApi/Controllers/MoviesController.cs). Make the summary, optional details, reviews, and cast visually distinct. Keep the projection in EF's query expression and explain why the two collections use a split query. [SOLID-04](TODO-SOLID.md#solid-04-move-movie-read-projections-into-a-focused-query-class) remains deferred until a separate query class has a clear purpose.

**Acceptance criteria**

- [x] Readers can identify each filter and response section without deciphering a dense constructor call.
- [x] SQL Server integration tests cover combined filters, both detail routes, missing movies, and the existing null and ordering behavior. Filtering and projection run in the database.

**Completion**

Verified both detail routes preserve absent details and return reviews and cast in identifier order. The existing filtering and missing-resource tests passed.

### READ-02 Separate cast assignment decisions from database writes

In [CastAssignmentService](../src/MovieApi/Services/CastAssignmentService.cs), make the new-link and existing-link paths easy to follow. Use focused private methods where they help name the two operations. Explain why insert constraint failures can follow successful prechecks and why the conditional write includes a null-role predicate.

**Acceptance criteria**

- [x] The service entry point reads as a decision about the current cast link, and the SQL Server-specific error handling sits beside the write it interprets.
- [x] Existing tests verify 201, 204, 404, and 409 outcomes. Preserve the conditional update and the behavior when a concurrent request inserts a competing link or removes a resource.

**Completion**

Verified create, update, conflict, and missing-resource outcomes, including a new SQL Server test for an actor deleted after the precheck.

### READ-03 Clarify movie write paths and concurrency handling

Review create, update, and delete in [MoviesController](../src/MovieApi/Controllers/MoviesController.cs). Make the order of validation, persistence, and response clear. Replace comments that narrate basic EF calls with explanations of decisions such as the follow-up lookup after a concurrent update failure.

**Acceptance criteria**

- [ ] Each write action states its intent through names and control flow. Comments explain concurrency and relationship behavior where it matters.
- [ ] Preserve the create response and location, update semantics, missing-resource responses, and delete behavior. Verify these through the existing integration tests.

### READ-04 Clarify review collection and creation behavior

Make it clear in [ReviewsController](../src/MovieApi/Controllers/ReviewsController.cs) why the collection query starts from the movie and why creation checks the movie again after SQL Server reports a constraint failure. Keep unrelated constraint failures visible.

**Acceptance criteria**

- [ ] The code clearly distinguishes a movie with no reviews, a missing movie, and an insert failure that does not establish that the movie disappeared.
- [ ] Preserve review ordering, reason for the fallback check, and HTTP status and location behavior. Verify the existing lifecycle and concurrent-delete cases.

### READ-05 Separate startup setup from the seed transaction

In [SeedDataExtensions](../src/MovieApi/Extensions/SeedDataExtensions.cs), make the development guard, migration, and transactional catalog insert readable as distinct steps. Keep the SQL Server lock configuration close to its transaction and document the lock's lifetime and purpose.

**Acceptance criteria**

- [ ] The startup entry point makes it clear when it opens a scope, migrates, and attempts to seed the catalog.
- [ ] The lock, empty-catalog check, and inserts remain in the same transaction. Verify concurrent startup and preservation of existing catalogs through the seed integration tests.

### READ-06 Organize SQL Server API tests around the resources they exercise

After the production refactors, split the mixed [MoviesApiTests](../tests/MovieApi.IntegrationTests/MoviesApiTests.cs) into focused groups where it improves navigation. Keep the existing host and isolated database lifecycle. Share setup only where the groups genuinely use the same setup.

**Acceptance criteria**

- [ ] Readers can find movie, actor, review, and cast scenarios by test class and follow Arrange–Act–Assert without tracing unrelated setup.
- [ ] Preserve all current production behavior checks, including the SQL Server race tests, and verify the full solution suite against SQL Server.

## Implementation boundaries

Keep changes local to the affected methods and classes. Use Single Responsibility when it makes an existing workflow clearer. Skip new layers, interfaces, and dependency changes unless a later backlog item explicitly justifies them. Add comments that explain intent, constraints, or concurrency decisions. Keep XML documentation aligned with the code. Run the relevant tests for each item and the full solution suite before reporting the refactor complete.
