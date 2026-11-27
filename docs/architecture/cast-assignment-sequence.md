# Assign a role to a movie cast member

```mermaid
sequenceDiagram
    participant Client
    participant Controller as MoviesController
    participant Service as CastAssignmentService
    participant DB as SQL Server via MovieDbContext

    Client->>Controller: POST /api/movies/{movieId}/actors/{actorId}
    Controller->>Service: AssignRoleAsync(movieId, actorId, role)
    Service->>DB: Check movie and actor existence
    alt Movie or actor is missing
        Service-->>Controller: ResourceMissing
        Controller-->>Client: 404 Not Found
    else Both exist
        Service->>DB: Look up cast link
        alt Link already has a role
            Service-->>Controller: RoleAlreadyAssigned
            Controller-->>Client: 409 Conflict
        else No link exists
            Service->>DB: Insert link with requested role
            alt Insert succeeds
                Service-->>Controller: Created
                Controller-->>Client: 201 Created
            else Competing request inserted the link
                Service-->>Controller: RoleAlreadyAssigned
                Controller-->>Client: 409 Conflict
            else Movie or actor was deleted
                Service-->>Controller: ResourceMissing
                Controller-->>Client: 404 Not Found
            end
        else Link exists with a null role
            Service->>DB: Update only where role is null
            alt One row updated
                Service-->>Controller: Updated
                Controller-->>Client: 204 No Content
            else Zero rows updated
                Service->>DB: Recheck link existence
                alt Link still exists
                    Service-->>Controller: RoleAlreadyAssigned
                    Controller-->>Client: 409 Conflict
                else Link is missing
                    Service-->>Controller: ResourceMissing
                    Controller-->>Client: 404 Not Found
                end
            end
        end
    end
```

The diagram starts after request validation. The conditional update prevents a concurrent request from overwriting a role. When an insert races with another write, the service maps SQL Server's primary-key and foreign-key failures to the outcomes shown above.

[Controller](https://github.com/RakivoJoy/ReEducation-MovieApi/blob/main/src/MovieApi/Controllers/MoviesController.cs) · [Service](https://github.com/RakivoJoy/ReEducation-MovieApi/blob/main/src/MovieApi/Services/CastAssignmentService.cs) · [Integration tests](https://github.com/RakivoJoy/ReEducation-MovieApi/blob/main/tests/MovieApi.IntegrationTests/MoviesApiTests.cs)
