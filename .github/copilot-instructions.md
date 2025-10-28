# GitHub Copilot Instructions for Room_Cast

## Project Overview

Room_Cast is a file management web application built with **ASP.NET Core 9.0** using the **MVP (Model-View-Presenter)** architectural pattern with server-side rendering (SSR). The application allows users to upload, manage, and edit various media files including pictures, videos, and documents.

## Technology Stack

- **Backend Framework**: ASP.NET Core 9.0 (C#)
- **Database**: SQLite with Entity Framework Core 9.0
- **Authentication**: ASP.NET Core Identity
- **Frontend Styling**: Tailwind CSS 3.4
- **Media Processing**: FFmpeg (via @ffmpeg/ffmpeg and @ffmpeg/core npm packages)
- **Architecture Pattern**: Model-View-Presenter (MVP)

## Project Structure

```
Room_Cast/
├── src/RoomCast/
│   ├── Controllers/       # MVC Controllers
│   ├── Models/            # Domain models and ViewModels
│   ├── Views/             # Razor views
│   ├── Services/          # Business logic and services
│   ├── Data/              # EF Core DbContext and migrations
│   ├── Options/           # Configuration options
│   ├── Styles/            # Tailwind CSS source
│   ├── wwwroot/           # Static files and media storage
│   └── RoomCast.csproj    # Project file
├── package.json           # Node.js dependencies (Tailwind, FFmpeg)
└── RoomCast.sln          # Visual Studio solution
```

## Core Features

1. **User Authentication**: Login/Signup using ASP.NET Core Identity
2. **Media Upload**: Support for pictures, videos, and documents with metadata storage
3. **Media Management**: View, edit, and delete uploaded files
4. **Video Processing**: 
   - Extract video duration using FFmpeg
   - Generate thumbnails at 1-second mark
   - Trim videos with FFmpeg
5. **Image Editing**: Replace and regenerate thumbnails
6. **Text File Editing**: Inline text editing for document files

## Development Guidelines

### Code Conventions

- **Nullable Reference Types**: Enabled - always handle null cases appropriately
- **Implicit Usings**: Enabled in the project
- **Target Framework**: .NET 9.0
- **Code Style**: Follow standard C# conventions and naming guidelines
- **ViewModels**: Located in `Models/ViewModels/` directory

### Architecture Pattern

This project follows the **MVP (Model-View-Presenter)** pattern:
- **Models**: Domain entities (`Album`, `MediaFile`, `AlbumFile`, `ApplicationUser`)
- **Views**: Razor views for rendering UI
- **Presenters/Controllers**: ASP.NET Core controllers handling presentation logic

### Database

- **Provider**: Entity Framework Core with SQLite
- **Migrations**: Use EF Core migrations for schema changes
- **DbContext**: `ApplicationDbContext` in `Data/ApplicationDbContext.cs`
- **User Secrets**: Configured with ID `acbf176f-f408-46ca-bea6-afb5a8e55f3c`

### Media File Handling

- **Storage Locations**:
  - Images: `wwwroot/media/images/`
  - Videos: `wwwroot/media/videos/`
  - Documents: `wwwroot/media/docs/`
  - Uploads: `wwwroot/uploads/`
- **Video Processing**: Use FFmpeg services in `Services/MediaPreview/`
- **File Validation**: Check file formats and sizes on upload
- **Safe File Names**: Generate safe file names server-side

## Build and Run

### Prerequisites
- .NET 9.0 SDK
- Node.js (for Tailwind CSS)

### Build Commands

```bash
# Restore .NET dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --project src/RoomCast/RoomCast.csproj
```

### Frontend/CSS

```bash
# Install npm dependencies
npm install

# Build Tailwind CSS (production)
npm run build:tailwind

# Watch Tailwind CSS changes (development)
npm run watch:tailwind
```

### Database Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project src/RoomCast

# Update database
dotnet ef database update --project src/RoomCast
```

## Testing

Currently, the project does not have an established test suite. When adding tests:
- Create a separate test project following .NET conventions
- Use xUnit, NUnit, or MSTest as the testing framework
- Follow the arrange-act-assert pattern

## Common Tasks

### Adding a New Media Type
1. Update `MediaFile` model with new type
2. Add corresponding folder in `wwwroot/media/`
3. Update controllers to handle new media type
4. Add/update views for the new media type

### Adding a New Page
1. Create controller in `Controllers/`
2. Create corresponding view in `Views/[ControllerName]/`
3. Add navigation links if needed
4. Update routing if using custom routes

### Modifying Database Schema
1. Update model classes in `Models/`
2. Create migration: `dotnet ef migrations add <Name> --project src/RoomCast`
3. Review migration in `Data/Migrations/`
4. Apply migration: `dotnet ef database update --project src/RoomCast`

## Code Quality

- Ensure all code follows C# best practices
- Use async/await for I/O operations
- Properly dispose of resources (use `using` statements)
- Validate user inputs server-side
- Handle exceptions appropriately
- Use dependency injection for services

## Security Considerations

- Never commit sensitive data or credentials
- Use User Secrets for development secrets
- Validate and sanitize all user inputs
- Implement proper authorization checks
- Use parameterized queries (EF Core handles this)
- Validate file uploads (type, size, content)
- Generate safe file names to prevent path traversal

## Dependencies

### NuGet Packages
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (9.0.9)
- Microsoft.AspNetCore.Identity.UI (9.0.9)
- Microsoft.EntityFrameworkCore.Sqlite (9.0.9)
- Microsoft.EntityFrameworkCore.SqlServer (9.0.9)
- Microsoft.EntityFrameworkCore.Design (9.0.8)

### npm Packages
- tailwindcss (3.4.14)
- @tailwindcss/forms (0.5.7)
- @tailwindcss/typography (0.5.12)
- @ffmpeg/ffmpeg (0.12.6)
- @ffmpeg/core (0.12.6)
- autoprefixer (10.4.16)

## Additional Notes

- The project uses Tailwind CSS for styling - avoid inline styles
- Media files are stored in the file system, not in the database
- The application uses server-side rendering (SSR)
- Library dependencies are managed via LibMan (see `libman.json` if present)
