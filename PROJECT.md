# Technical Specification Document

## Project Overview

This project is a server-rendered **ASP.NET Core MVC** application for managing media files, including pictures, videos, and documents.

**Technology Stack:**

* **Architecture:** Traditional ASP.NET Core MVC (no standalone Web API layer)
* **Backend & UI:** .NET 9 (Razor views + Bootstrap 5)
* **Client Enhancements:** Progressive enhancement with vanilla JavaScript where needed (no SPA framework)
* **Database:** SQLite 3 (EF Core, local `app.db`)

The application consists of two main pages:

* **Login / Register** (Account management)
* **Home** (Media file management)

---

## 1. Authentication Pages

### 1.1 Login Page

**Route:** `/Account/Login`

**Functionality:**

* User enters email and password.
* On successful login, redirect to **Home** page.
* On failure, display validation message.
* If user has no account → click **Register** link.

### 1.2 Register (Popup)

**Trigger:** From Login page → click `Register` button.

**UI/UX Flow:**

* Popup form appears with fields: `Email`, `Password`, `Confirm Password`.
* Validation for email format and password strength.
* On successful registration:

  * Popup auto closes.
  * Option 1: Auto login.
  * Option 2: Require manual login (configurable).

**Backend Process (MVC):**

* POST `/Account/Register` (standard MVC form post or AJAX-enhanced form targeting the same action).
* Hash password using ASP.NET Core Identity.
* Store user record in SQLite via EF Core.
* Return a redirect to `/MediaFiles/Index` when auto-login is enabled, otherwise redisplay the login view with a success banner.

---

## 2. Home Page

**Route:** `/Home`

**Purpose:** Manage uploaded media files (Picture, Video, Document).

### 2.1 Empty State

* If user has **no uploaded files** → show message:

  * “No files found.”
  * Button: `Upload` → opens Upload Form.

### 2.2 File Grid View

* Display media grid with the following columns:

  * **Thumbnail** (for picture/video; default icon for documents)
  * **Title**
  * **Type** (Picture | Video | Document)
  * **Actions**: `Edit | Delete | Download/View`

### 2.3 File Item Actions

#### Edit

* Opens Edit Form depending on file type (details in section 3.2).

#### Delete

* Shows confirmation dialog.
* On confirm:

  * Delete DB record.
  * Delete physical file (and thumbnail if exists).

#### Download/View

* Opens file in new tab or downloads it depending on MIME type.

---

## 3. File Management

### 3.1 Upload

**Trigger:** `Upload` button.

**Form Fields:**

* `Title` (text input)
* `FileType` (Picture | Video | Document)
* `File` (input type="file")

**Validation Rules:**

* Allowed formats:

  * **Picture:** .jpg, .jpeg, .png, .webp
  * **Video:** .mp4, .mov, .avi, .mkv
  * **Document:** .pdf, .docx, .txt
* Max file size (configurable, e.g., 50 MB)

**Backend Workflow:**

1. Validate file type and size.
2. Rename file securely (unique ID, timestamp).
3. Save physical file in `/uploads/{userId}/`.
4. Store metadata in SQLite (table: `MediaFiles`):

   * Id
   * UserId
   * Title
   * FileType
   * FilePath
   * FileSize
   * DurationSeconds (for video)
   * CreatedAt / UpdatedAt

**For Video Files:**

* Use **FFmpeg** to:

  * Extract duration (seconds)
  * Generate thumbnail (frame at 1s)

---

### 3.2 Edit

**Behavior differs by FileType:**

#### Text Document

* Edit text inline or in popup.
* Update content and title.

#### Picture

* Select new image to replace existing file.
* Replace physical file and update DB.
* Regenerate thumbnail.

#### Video

* Trim video using FFmpeg.
* Generate new file (overwrite or versioned).
* Update DB fields: `FilePath`, `DurationSeconds`, `UpdatedAt`.

---

### 3.3 Delete

**Flow:**

1. Confirm deletion.
2. Remove DB record.
3. Delete corresponding physical file and thumbnail.

**MVC Endpoint:** POST `/MediaFiles/Delete/{id}` (anti-forgery protected form submission)

---

## 4. MVC Controller Structure

### Controllers & Actions

* **AccountController**

  * `Login` (GET/POST) — handles credential validation and return-url logic.
  * `Register` (GET/POST) — creates new users, optionally signs them in immediately.
  * `Logout` (POST) — ends the authenticated session.

* **MediaFilesController**

  * `Index` (GET) — lists the user's media files.
  * `Upload` (GET/POST) — renders upload form and processes media submissions.
  * `Edit` (GET/POST) — updates metadata or replaces files.
  * `Delete` (POST) — removes media using anti-forgery-protected form posts.
  * `Download` (GET) — streams the requested file.

* **AlbumsController**

  * `Index` (GET) — lists albums.
  * `Create` (GET/POST) — adds new albums.
  * `Edit` (GET/POST) — updates album metadata.
  * `Delete` (POST) — removes album entries.

### Services

* **AuthService:** Extends ASP.NET Core Identity for policy configuration and user management helpers.
* **MediaService:** File handling, ffmpeg integration, metadata extraction.
* **StorageService:** Physical file operations.

---

## 5. Database Schema (SQLite)

### Table: `Users`

| Column       | Type     | Notes  |
| ------------ | -------- | ------ |
| Id           | INTEGER  | PK     |
| Email        | TEXT     | Unique |
| PasswordHash | TEXT     |        |
| CreatedAt    | DATETIME |        |

### Table: `MediaFiles`

| Column          | Type     | Notes                  |
| --------------- | -------- | ---------------------- |
| Id              | INTEGER  | PK                     |
| UserId          | INTEGER  | FK to Users.Id         |
| Title           | TEXT     |                        |
| FileType        | TEXT     | Picture/Video/Document |
| FilePath        | TEXT     | Physical path          |
| FileSize        | INTEGER  | Bytes                  |
| DurationSeconds | INTEGER  | Nullable               |
| CreatedAt       | DATETIME |                        |
| UpdatedAt       | DATETIME |                        |

---

## 6. File System Structure

```
/uploads/
  └── {userId}/
      ├── pictures/
      ├── videos/
      ├── documents/
      └── thumbnails/
```

---

## 7. Security Considerations

* Validate file extensions & MIME types.
* Use server-side file renaming to prevent path traversal.
* Limit upload size.
* Enforce cookie-based authentication with `[Authorize]` on all media management controllers.
* Sanitize user inputs (title, filename, etc.).

---

## 8. Future Enhancements

* Add search/filter/sort in media grid.
* Add pagination.
* Support multiple file uploads.
* Add public sharing links with expiration.
* Store files in external storage (S3, Azure Blob).

---

**Author:** H_RoomCast Project Team
**Version:** 1.0.0
**Last Updated:** 2025-10-09
