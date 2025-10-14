**Project Overview:**
A simple file management website built using **.NET** with the **MVP (Model-View-Presenter)** architectural pattern and **server-side rendering (SSR)**. The application contains two main pages: **Login** and **Home**.

---

### 2. Home Page (/Home/Index)

**Behavior:**

* If the user has no uploaded files → display an empty state with an **Upload** button.
* If files exist → display a **media grid view** containing:

  * Thumbnail
  * Title
  * File Type (Picture, Video, Document)
  * Actions: **Edit | Delete | (optional) Download/View**

---

### 3. Upload Functionality

**Flow:**

1. User selects file type: Picture / Video / Document.
2. User provides **Title** and chooses a file to upload.
3. Server-side validation:

   * Check file format and size.
   * Generate a safe file name.
   * Save the file to physical storage.
   * Record metadata in the database (Title, Type, Path, Size, CreatedAt, etc.).
4. For **videos**:

   * Use **ffmpeg** to extract duration.
   * Generate a thumbnail (frame at 1 second).

---

### 4. Edit Functionality

**Behavior depends on File Type:**

* **Text file:** Allow inline text editing.
* **Picture:** Allow selecting a new image → replace physical file → update database → regenerate thumbnail.
* **Video:** Allow trimming using **ffmpeg** → generate new video file (overwrite or create new version) → update database (FilePath, DurationSeconds, UpdatedAt).

---

### 5. Delete Functionality

**Flow:**

* User clicks **Delete** → show confirmation popup.
* On confirmation:

  * Remove file record from database.
  * Delete physical file and any related thumbnails from storage.
