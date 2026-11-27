IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127090738_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Actors] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [BirthYear] int NOT NULL,
        CONSTRAINT [PK_Actors] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127090738_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Movies] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [Year] int NOT NULL,
        [Genre] nvarchar(max) NOT NULL,
        [Duration] int NOT NULL,
        CONSTRAINT [PK_Movies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127090738_InitialSqlServer'
)
BEGIN
    CREATE TABLE [MovieActor] (
        [MovieId] int NOT NULL,
        [ActorId] int NOT NULL,
        [Role] nvarchar(200) NULL,
        CONSTRAINT [PK_MovieActor] PRIMARY KEY ([MovieId], [ActorId]),
        CONSTRAINT [FK_MovieActor_Actors_ActorId] FOREIGN KEY ([ActorId]) REFERENCES [Actors] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MovieActor_Movies_MovieId] FOREIGN KEY ([MovieId]) REFERENCES [Movies] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127090738_InitialSqlServer'
)
BEGIN
    CREATE TABLE [MovieDetails] (
        [Id] int NOT NULL IDENTITY,
        [Synopsis] nvarchar(max) NOT NULL,
        [Language] nvarchar(max) NOT NULL,
        [Budget] decimal(19,4) NOT NULL,
        [MovieId] int NOT NULL,
        CONSTRAINT [PK_MovieDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MovieDetails_Movies_MovieId] FOREIGN KEY ([MovieId]) REFERENCES [Movies] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127090738_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Reviews] (
        [Id] int NOT NULL IDENTITY,
        [ReviewerName] nvarchar(max) NOT NULL,
        [Comment] nvarchar(max) NOT NULL,
        [Rating] int NOT NULL,
        [MovieId] int NOT NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Reviews_Rating] CHECK ([Rating] BETWEEN 1 AND 5),
        CONSTRAINT [FK_Reviews_Movies_MovieId] FOREIGN KEY ([MovieId]) REFERENCES [Movies] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127090738_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_MovieActor_ActorId] ON [MovieActor] ([ActorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127090738_InitialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MovieDetails_MovieId] ON [MovieDetails] ([MovieId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127090738_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Reviews_MovieId] ON [Reviews] ([MovieId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127090738_InitialSqlServer'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261127090738_InitialSqlServer', N'10.0.15');
END;

COMMIT;
GO
