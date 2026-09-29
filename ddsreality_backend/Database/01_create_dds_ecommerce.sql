-- DDS CMS: first installation only. Run in SQL Server Management Studio.
-- Database: dds_ecommerce. All new CMS tables use the [dds] schema.
-- Existing dbo tables are not modified. Back up the database before installation.
IF DB_ID(N'dds_ecommerce') IS NULL
    EXEC(N'CREATE DATABASE [dds_ecommerce]');
GO
USE [dds_ecommerce];
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS (SELECT 1 FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id=s.schema_id WHERE s.name=N'dds')
    THROW 51000, 'DDS schema already has tables. First-install script stopped; do not drop existing data. Use a reviewed migration for upgrades.', 1;

IF SCHEMA_ID(N'dds') IS NULL EXEC(N'CREATE SCHEMA [dds];');


CREATE TABLE [dds].[AdminUsers] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(120) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [SecurityStamp] nvarchar(max) NOT NULL,
    [FailedAttempts] int NOT NULL,
    [LockoutUntilUtc] datetime2 NULL,
    [Active] bit NOT NULL,
    [Version] bigint NOT NULL,
    CONSTRAINT [PK_AdminUsers] PRIMARY KEY ([Id])
);


CREATE TABLE [dds].[AuditEvents] (
    [Id] bigint NOT NULL IDENTITY,
    [Actor] nvarchar(120) NOT NULL,
    [Action] nvarchar(500) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditEvents] PRIMARY KEY ([Id])
);


CREATE TABLE [dds].[Enquiries] (
    [Id] int NOT NULL IDENTITY,
    [SiteId] nvarchar(20) NOT NULL,
    [ProjectId] int NULL,
    [ProjectTitle] nvarchar(160) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Phone] nvarchar(30) NOT NULL,
    [Email] nvarchar(120) NULL,
    [Message] nvarchar(2000) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Notes] nvarchar(3000) NOT NULL,
    [Version] bigint NOT NULL,
    CONSTRAINT [PK_Enquiries] PRIMARY KEY ([Id])
);


CREATE TABLE [dds].[Sites] (
    [SiteId] nvarchar(20) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Tagline] nvarchar(300) NOT NULL,
    [LogoPath] nvarchar(500) NOT NULL,
    [AboutTitle] nvarchar(100) NOT NULL,
    [AboutBody] nvarchar(max) NOT NULL,
    [AboutImage] nvarchar(500) NOT NULL,
    [PrivacyText] nvarchar(max) NOT NULL,
    [Address] nvarchar(1000) NOT NULL,
    [Phone] nvarchar(30) NOT NULL,
    [Email] nvarchar(120) NULL,
    [WhatsApp] nvarchar(20) NOT NULL,
    [Facebook] nvarchar(500) NOT NULL,
    [Instagram] nvarchar(500) NOT NULL,
    [YouTube] nvarchar(500) NOT NULL,
    [FooterText] nvarchar(300) NOT NULL,
    [MetaDescription] nvarchar(160) NOT NULL,
    [PrimaryColor] nvarchar(max) NOT NULL,
    [AccentColor] nvarchar(max) NOT NULL,
    [ShowTeam] bit NOT NULL,
    [ShowTestimonials] bit NOT NULL,
    [ShowReels] bit NOT NULL,
    [EnableAnimations] bit NOT NULL,
    [Version] bigint NOT NULL,
    CONSTRAINT [PK_Sites] PRIMARY KEY ([SiteId])
);


CREATE TABLE [dds].[Entries] (
    [Id] int NOT NULL IDENTITY,
    [SiteId] nvarchar(20) NOT NULL,
    [Kind] nvarchar(20) NOT NULL,
    [Title] nvarchar(160) NOT NULL,
    [Slug] nvarchar(160) NOT NULL,
    [Subtitle] nvarchar(300) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [ImagePath] nvarchar(500) NOT NULL,
    [AltText] nvarchar(200) NOT NULL,
    [LinkUrl] nvarchar(500) NOT NULL,
    [ButtonText] nvarchar(50) NOT NULL,
    [Price] nvarchar(100) NOT NULL,
    [Location] nvarchar(300) NOT NULL,
    [Configuration] nvarchar(100) NOT NULL,
    [PropertyType] nvarchar(100) NOT NULL,
    [ReraNumber] nvarchar(100) NOT NULL,
    [Possession] nvarchar(100) NOT NULL,
    [MapUrl] nvarchar(max) NOT NULL,
    [Amenities] nvarchar(max) NOT NULL,
    [Highlights] nvarchar(max) NOT NULL,
    [MetaDescription] nvarchar(160) NOT NULL,
    [Published] bit NOT NULL,
    [Featured] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [UpdatedUtc] datetime2 NOT NULL,
    [Version] bigint NOT NULL,
    CONSTRAINT [PK_Entries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Entries_Sites_SiteId] FOREIGN KEY ([SiteId]) REFERENCES [dds].[Sites] ([SiteId]) ON DELETE NO ACTION
);


CREATE TABLE [dds].[Media] (
    [Id] int NOT NULL IDENTITY,
    [ContentEntryId] int NOT NULL,
    [Kind] nvarchar(20) NOT NULL,
    [Caption] nvarchar(160) NOT NULL,
    [ImagePath] nvarchar(500) NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_Media] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Media_Entries_ContentEntryId] FOREIGN KEY ([ContentEntryId]) REFERENCES [dds].[Entries] ([Id]) ON DELETE CASCADE
);


CREATE UNIQUE INDEX [IX_AdminUsers_Email] ON [dds].[AdminUsers] ([Email]);


CREATE INDEX [IX_Enquiries_SiteId_CreatedUtc] ON [dds].[Enquiries] ([SiteId], [CreatedUtc]);


CREATE INDEX [IX_Entries_SiteId_Kind_Published_SortOrder] ON [dds].[Entries] ([SiteId], [Kind], [Published], [SortOrder]);


CREATE UNIQUE INDEX [IX_Entries_SiteId_Kind_Slug] ON [dds].[Entries] ([SiteId], [Kind], [Slug]);


CREATE INDEX [IX_Media_ContentEntryId] ON [dds].[Media] ([ContentEntryId]);



COMMIT TRANSACTION;
GO
-- Next: configure connection string, run dotnet run -- --seed, then create an admin.
