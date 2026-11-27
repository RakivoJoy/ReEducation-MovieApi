# MovieApi Documentation Backlog

Build a DocFX site that combines a C# API reference with Markdown guides and Mermaid diagrams of MovieApi and its SQL Server schema. Generate a downloadable site from a pull request when it has the `gen-docfx` label.

## Scope and review

Approve this backlog before implementation starts. After approval, work through the items independently where dependencies permit and use focused commits. Update each item's status and check only verified acceptance criteria.

Source edits are limited to C# XML documentation and explanatory comments. DocFX setup can add documentation configuration, a pinned tool dependency, and the GitHub workflow. Application behavior, API contracts, SQL Server schema, and test code stay outside this effort. Record any refactoring ideas for later. Review prose, comments, links, and diagrams against the source. Run checks only for DocFX generation and its GitHub label behavior. Existing CI follows its own rules.

## Prioritization

Score Impact, Confidence, and Ease from 1 to 10. ICE is their product. Start with higher-scoring items where dependencies allow. Statuses follow the [ICE backlog template](templates/ICE-BACKLOG.md). DOC-01 through DOC-09 were approved for this pass.

| ID | Backlog item | Impact | Confidence | Ease | ICE | Status |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| DOC-01 | Configure the DocFX site and API reference | 10 | 9 | 6 | 540 | Done |
| DOC-02 | Generate DocFX through the `gen-docfx` PR label | 10 | 9 | 6 | 540 | Done |
| DOC-03 | Write a reusable DocFX guide | 8 | 9 | 8 | 576 | Done |
| DOC-04 | Write a reusable C# comments guide | 9 | 9 | 8 | 648 | Done |
| DOC-05 | Write a reusable Mermaid guide | 8 | 9 | 8 | 576 | Done |
| DOC-06 | Document movie controller contracts | 9 | 9 | 7 | 567 | Done |
| DOC-07 | Document actor and review controller contracts | 8 | 9 | 7 | 504 | Done |
| DOC-08 | Document request and response DTOs | 9 | 9 | 7 | 567 | Done |
| DOC-09 | Document the cast assignment workflow | 9 | 9 | 7 | 567 | Done |
| DOC-10 | Document EF Core entities and mappings | 9 | 9 | 7 | 567 | Done |
| DOC-11 | Document application startup and demo seeding | 8 | 9 | 7 | 504 | Done |
| DOC-12 | Draw the application UML class diagram | 9 | 9 | 7 | 567 | Done |
| DOC-13 | Draw the SQL Server entity-relationship diagram | 9 | 9 | 7 | 567 | Done |
| DOC-14 | Draw the cast assignment UML sequence diagram | 7 | 9 | 7 | 441 | Done |
| DOC-15 | Write the architecture overview and organize navigation | 8 | 9 | 7 | 504 | Done |
| DOC-16 | Review and organize agent instructions | 7 | 8 | 7 | 392 | Done |

## Backlog items

### DOC-01 Configure the DocFX site and API reference

Add a pinned DocFX .NET tool and a documentation configuration under `docs/`. Generate API metadata from `src/MovieApi/MovieApi.csproj` using the .NET 10 SDK and use DocFX's `modern` template for Mermaid. [DocFX .NET API generation](https://dotnet.github.io/docfx/docs/dotnet-api-docs.html) · [DocFX Mermaid support](https://dotnet.github.io/docfx/docs/markdown.html#mermaid-diagrams)

**Acceptance criteria**

- [x] The docset generates HTML for a Markdown landing page and the MovieApi C# API reference. Its API input is limited to the application project, with generated migrations omitted from the reference.
- [x] Generated output stays out of Git. The setup generates the reference from the existing project without changing C# compiler settings or requiring a running API or SQL Server.

### DOC-02 Generate DocFX through the `gen-docfx` PR label

After DOC-01, create the `gen-docfx` repository label and a standalone DocFX workflow. Generate and upload the HTML as a GitHub Actions artifact for review.

**Acceptance criteria**

- [x] Adding `gen-docfx` to an open PR starts the job. Opening, reopening, or updating a PR with the label also runs the job. An unlabeled PR and an unrelated label change skip generation.
- [x] A hosted labeled run uploads the generated HTML, and an unlabeled run shows DocFX skipped. The workflow runs independently of the `run-tests` label and uses read-only repository access.

The draft PR's [unlabeled run](https://github.com/RakivoJoy/ReEducation-MovieApi/actions/runs/45996360375) skipped generation. Adding `gen-docfx` triggered a [successful hosted run with the HTML artifact](https://github.com/RakivoJoy/ReEducation-MovieApi/actions/runs/45996420020). The remaining event conditions were reviewed against the workflow.

The [hosted DocFX job](https://github.com/RakivoJoy/ReEducation-MovieApi/actions/runs/46000220559) took 36 seconds, so PR generation remains opt-in. The workflow now also generates the artifact when `main` advances. The first hosted `main` run can be verified after the workflow merges.

### DOC-03 Write a reusable DocFX guide

Add `docs/guides/docfx.md` for contributors who add or update documentation. Finalize examples after DOC-01 and DOC-02.

**Acceptance criteria**

- [x] The guide covers docset layout, the pinned tool, generation commands, how to download the PR artifact, and how to add an article or API cross-reference.
- [x] The guide explains where to look when a type, link, or article is missing and uses existing repository paths in its examples.

### DOC-04 Write a reusable C# comments guide

Add `docs/guides/csharp-comments.md` and use its conventions for DOC-06 through DOC-11.

**Acceptance criteria**

- [x] Show when to use XML documentation tags such as `summary`, `param`, `returns`, `remarks`, and `see`, with MovieApi examples that also make sense in IntelliSense.
- [x] Show when an inline comment should explain the reason for an implementation choice. Include one example where the code is already clear enough on its own.

### DOC-05 Write a reusable Mermaid guide

Add `docs/guides/mermaid.md` and use it for DOC-12 through DOC-14.

**Acceptance criteria**

- [x] Include small, copyable examples for Mermaid `classDiagram`, `sequenceDiagram`, and `erDiagram`, with guidance on which question each diagram answers.
- [x] Explain how to name components, represent cardinality, link to implementation sources, and update diagrams when the model changes.

### DOC-06 Document movie controller contracts

Add XML comments to `MoviesController` and its actions. Group the work around the movie list, detail, and write endpoints.

**Acceptance criteria**

- [x] Public actions describe their purpose, route parameters or body inputs, and relevant outcomes supported by the implementation and existing tests.
- [x] Comments explain the list and detail representations, the details route, and the delegation of cast assignment to the service.

### DOC-07 Document actor and review controller contracts

Add XML comments to `ActorsController` and `ReviewsController` and explain the relationship between actors, movies, and reviews.

**Acceptance criteria**

- [x] Actor actions explain the responses associated with the supported operations. Review actions explain which routes are movie-scoped and which address an individual review.
- [x] Review comments describe observable outcomes, including a missing parent movie, in terms of the current behavior.

### DOC-08 Document request and response DTOs

Document the movie, actor, review, and cast DTOs with comments focused on what an API consumer needs.

**Acceptance criteria**

- [x] Request comments explain meaningful input constraints and units where the source establishes them. Response comments explain important distinctions between summaries, detail responses, standalone actors, and cast entries.
- [x] Positional records and nullable fields are documented where their semantics matter. Comments agree with the existing validation attributes and projections.

### DOC-09 Document the cast assignment workflow

Add XML comments to `CastAssignmentService`, `AssignRoleAsync`, and `CastAssignmentResult`. Use short inline notes where they help explain the update and exception handling.

**Acceptance criteria**

- [x] Comments distinguish adding a cast link from assigning a role to an existing link with a null role.
- [x] The comments explain duplicate assignment, missing resources, and why the conditional update is needed for concurrent requests.

### DOC-10 Document EF Core entities and mappings

Document the domain entities and `MovieDbContext` through XML comments and targeted notes on the mapping configuration.

**Acceptance criteria**

- [x] Describe movie details, reviews, and cast links, including the join entity and the optional role.
- [x] Explain the relevant persistence constraints and link them to the context or schema. Leave generated migration source alone.

**Completion**

Reviewed the comments against the EF mappings and SQL Server schema.

### DOC-11 Document application startup and demo seeding

Add concise comments to the startup and seed code where they explain decisions that may be missed on a first reading.

**Acceptance criteria**

- [x] Comments explain the development startup conditions, the scoped database context, and when migrations run.
- [x] Comments explain how the SQL Server transaction lock and empty-catalog check protect seeding from concurrent startup and preserve existing data.

**Completion**

Reviewed the environment and configuration guards, scoped context, and transaction against the startup code.

### DOC-12 Draw the application UML class diagram

Add `docs/architecture/application-uml.md` with a Mermaid class diagram of the main application types.

**Acceptance criteria**

- [x] The diagram shows the main controllers, DTOs, cast assignment service, `MovieDbContext`, and domain entities, with dependency directions checked against the source.
- [x] An accompanying note explains the boundary between HTTP contracts and persistence models and links to the relevant source.

**Completion**

Reviewed type and dependency directions against the controller, DTO, service, and entity declarations.

### DOC-13 Draw the SQL Server entity-relationship diagram

Add `docs/architecture/database-erd.md` with a Mermaid ER diagram based on the SQL Server migration and schema script.

**Acceptance criteria**

- [x] Show `Movies`, `MovieDetails`, `Reviews`, `Actors`, and `MovieActor` with primary and foreign keys, relationship directions, and cardinalities.
- [x] Note the unique movie-details link, composite cast key, nullable role, and rating constraint alongside the diagram, with links to the schema and mapping.

**Completion**

Verified the cardinalities, keys, nullability, cascades, and rating constraint against the checked-in SQL Server schema.

### DOC-14 Draw the cast assignment UML sequence diagram

After DOC-09, add `docs/architecture/cast-assignment-sequence.md` with a Mermaid sequence diagram for assigning an actor role to a movie.

**Acceptance criteria**

- [x] Show the client, controller, service, and database interactions for a new link and for an existing link with no role.
- [x] Include the missing resource and already-assigned outcomes with the corresponding HTTP responses. Link to the implementation and the relevant integration tests.

**Completion**

Traced each outcome through the service and controller and checked the supporting integration tests by inspection.

### DOC-15 Write the architecture overview and organize navigation

Add a short architecture entry point and update the Docs README and DocFX navigation to surface the API reference, architecture pages, and contributor guides. Coordinate with DOC-01 and finalize after the guides and diagrams are written.

**Acceptance criteria**

- [x] A reader can start from the docs index or site navigation and find the overview, UML diagrams, database ER diagram, API reference, and reusable guides.
- [x] The overview describes the current request path and persistence boundary. Links are reviewed against the repository layout and included in the final DocFX generation review under DOC-02.

**Completion**

Generated the DocFX site and checked that the overview, diagrams, API reference, and three guides were present.

### DOC-16 Review and organize agent instructions

After the three reusable guides are drafted, review `AGENTS.md` for sections that would be easier to maintain as focused references. Keep repository-wide instructions readily visible.

**Acceptance criteria**

- [x] Link from `AGENTS.md` to the DocFX, comments, and Mermaid guides where appropriate. Move existing material only if it has a clear home and is still discoverable when its instructions apply.
- [x] Preserve the Git, approval, testing, and learning policies. Record the rationale if the root file is clearer with the remaining material kept together.

**Completion**

Linked each guide at the point of use. Kept the remaining instructions in AGENTS.md because Git, approval, testing, and learning policies apply across the repository and are easier to find together.
