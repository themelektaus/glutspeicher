# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Glutspeicher is a password management system with a server-client architecture. The server is an ASP.NET Core 9.0 web application that provides a REST API and web frontend, while the client is a Windows application that handles automation and relay functionality. A browser extension (Brave/Chromium) suggests and fills logins from the server.

The server has no authentication and no TLS: everyone who can reach the port can read and change everything, including `GET /api/database` (decrypted database). Keep that in mind for every change to the API or the deployment.

## Build & Run Commands

### Server

```bash
# Build the server
dotnet build "Glutspeicher Server/Glutspeicher Server.csproj"

# Run the server in development mode
dotnet run --project "Glutspeicher Server/Glutspeicher Server.csproj"

# Build for release (Windows)
dotnet publish "Glutspeicher Server/Glutspeicher Server.csproj" -c Release -r win-x64 --self-contained -o "Glutspeicher Server/Build/Windows"

# Build for release (Linux)
dotnet publish "Glutspeicher Server/Glutspeicher Server.csproj" -c Release -r linux-x64 --self-contained -o "Glutspeicher Server/Build/Linux"
```

### Client

```bash
# Build the client
# Needs "Glutspeicher Client/Deploy.targets" (gitignored, local deploy settings, may hold credentials:
# never print or commit it). On a fresh clone create it with the content <Project />.
dotnet build "Glutspeicher Client/Glutspeicher Client.csproj"

# Run the client
dotnet run --project "Glutspeicher Client/Glutspeicher Client.csproj"
```

### Browser Extension

```bash
# No build. Load "Glutspeicher Browser Extension/extension" unpacked in brave://extensions (Developer mode)

# Unit tests (matching, search, URL handling)
cd "Glutspeicher Browser Extension" && node --test "test/*.test.js"

# End-to-end test in a headless Brave with a temporary profile. GLUT_SERVER must be a scratch
# server with an empty database (the test writes entries and refuses other databases)
cd "Glutspeicher Browser Extension/test/e2e" && npm install && GLUT_SERVER=http://127.0.0.1:5591 node e2e.js
```

### Docker

```bash
# Build and run using Docker Compose (needs GLUTSPEICHER_CRYPTO_KEY in .env, see .env.example)
docker-compose up

# Run a locally published Linux build (expects it in ./app) in the .NET SDK image
docker-compose -f docker-compose-from-source.yml up
```

## Environment Configuration

### Required Environment Variables

- `GLUTSPEICHER_CRYPTO_KEY`: A 32-character encryption key used for database encryption
  - In DEBUG mode: Automatically set to "00000000000000000000000000000000"
  - In RELEASE mode (Windows): Automatically generated if not set, stored as machine-level environment variable
  - For Docker: the compose files take it from `.env` (gitignored, see `.env.example`) and refuse to start without it
  - Must be exactly 32 bytes; longer values are cut to 32 bytes, shorter ones stop the server
  - Release builds refuse the public test key (32 zeros, `AppSettings.TestCryptoKey`) and exit with -1

### Configuration Files

- `Glutspeicher Server/appsettings.json`: Contains HttpPort, ServerVersion, and ClientVersion settings
  - HttpPort: Default 80 (on Windows a value of 80 always becomes 5580, in Debug and Release)
  - ServerVersion must match `<Version>` in `Glutspeicher Server.csproj`, ClientVersion the one in `Glutspeicher Client.csproj`; both are only displayed in the frontend

## Architecture

### Server Architecture

The server follows a modular ASP.NET Core architecture:

**Entry Point** (`Program.cs`):
- Loads configuration from appsettings.json
- In RELEASE mode on Windows: Handles Windows Service registration and admin elevation
- Sets up dependency injection with LiteDbContext
- Configures middleware pipeline: API routing → Frontend → NoCache

**Database Layer** (`Context/LiteDbContext.cs`):
- Uses LiteDB for embedded NoSQL storage
- Encrypts database files at rest using AES encryption with GLUTSPEICHER_CRYPTO_KEY
- Stores encrypted database in `Data/Database.litedb.encrypted`
- Creates timestamped backups (compressed and encrypted) on every write
- Registered as transient: every request that uses it takes a global SemaphoreSlim, reads and decrypts the whole file into a MemoryStream and releases the lock on disposal
- A plain `Data/Database.litedb`, if present, replaces the encrypted database on the next request (migration and restore path)
- `GET /api/database` reads the file without that lock

**API Layer** (`Mapping/Api.*.cs`):
- Organized by resource type: Passwords, Generators, Exports, Relays
- Each resource follows CRUD pattern: GetAll, Get, Post, Put, Delete
- Additional Export endpoint (`/api/<resource>/export`) returns CSV files
- `GET /api/passwords/{id}` adds `GlutLinks` (AutoType, Connect): `glut://` + Base64 JSON with the credentials and relay data in plain text
- `GET /api` (endpoint list), `GET /api/database` (decrypted download), `GET /api/database/rebuild`
- Uses custom ApiResult response wrapper for consistent API responses
- All routes mounted via `Routing/ExtensionMethods.UseApi()`

**Frontend Middleware** (`Middleware/FrontendMiddleware.cs`):
- Serves static files from `wwwroot/` directory
- Implements server-side include system for HTML, CSS, and JS files:
  - HTML: `<!-- include path/to/file.html -->`
  - CSS: `/* include path/to/file.css */`
  - JS: `// include path/to/file.js`
- Compiles SCSS to CSS on-the-fly using LibSassHost
- Injects SERVER_VERSION, CLIENT_VERSION, and DEBUG flags into JS files
- Maps `/` to `/index.html` and handles CSV exports from `/static/exports/`

**Models** (`Model/`):
- Password: Core entity with static/generated passwords, TOTP support, relay connections
- Generator: Password generation rules
- Export: a CSV source (URI + optional JavaScript per row) that the frontend loads into the password list
- Relay: Client relay connection definitions

### Frontend Structure

The frontend is a vanilla JavaScript SPA located in `wwwroot/static/`:
- `js/`: Core JavaScript modules (app.js, data.js, component.js, page.js, extensions.js)
- `css/`: SCSS stylesheets with component-based organization
- `page/`: Individual page modules (passwords, generators, exports, relays, settings)
- Uses server-side includes to compose pages from reusable components
- SCSS files are compiled on-demand by the server

### Client Architecture

Located in `Glutspeicher Client/`:
- Windows-only application for automation and relay functionality
- Handles AutoType functionality (keyboard automation)
- Manages relay sessions for remote connections (SSH to the relay host, iptables DNAT rules)
- Does not call the server API: it is started by the `glut://` URL handler with Base64 JSON; `type` selects the action class in `Actions/` (AutoType, Mstsc, Ssh, Web, Deploy), the other keys set its public fields
- Log (`ClientLog.cs`): written to `%TEMP%\glutspeicher-client-*.log` only when an action failed, with secret fields masked (`*password*`, `*secret*`, `*token*`, `text`), shown in Notepad and deleted afterwards; every start removes leftover logs, also the `tmp*.tmp` logs of clients up to 0.3.6 (those held credentials in plain text). A new action field that holds a secret must match these names
- `Deploy` action: packs the builds into zip files and copies them to a target folder (used for the downloads in the web interface)

### Browser Extension Architecture

Located in `Glutspeicher Browser Extension/` (details in its README.md):
- `extension/`: Manifest V3, plain JavaScript without build, English UI in the colors of the web frontend. `test/` is outside of it on purpose (Chrome refuses to load folders with `_*` files such as `node_modules`)
- `lib/server.js`: server URL (the only value in `chrome.storage.local`), access via `optional_host_permissions` requested for the server's origin when it is saved, `fetch` with `credentials: "include"` (for a later login, #1). No CORS needed and no server change: the extension has host access to the server
- `lib/match.js`: no browser APIs (tested in Node). Suggestions: same host > subdomain > base domain (with registry second levels like `gv.at`, `co.uk` and shared suffixes like `myfritz.net`, `github.io`, `SHARED_SUFFIXES`); `fitsPage` (same host or sub/parent domain) decides whether filling needs no question; `rdp://`/`ssh://` left out, entries without scheme read as `https://`. Search like `search()` in `wwwroot/static/page/passwords/script.js`. Password is `generatedPassword || password`
- `popup.js`: loads `GET /api/passwords` on every open and keeps it in memory only; shows every server value with `textContent` (never `innerHTML`, see #5); asks before filling an entry that does not `fitsPage` (also base-domain suggestions) or into a plain-HTTP page
- `lib/totp.js`: TOTP codes like jsOTP in the frontend (Base32, HMAC-SHA1 via `crypto.subtle`, 30 s, 6 digits, RFC 6238), no other browser APIs (tested in Node). The popup shows the code of entries with `totp` (copy button, ring, every second) and computes it again right before filling; codes and secrets stay in the popup's memory
- `lib/fill.js`: `glutspeicherFill`, injected with `chrome.scripting.executeScript` (`activeTab`, `allFrames`); must stay self-contained. Native `value` setter plus `input`/`change` events. Frames of other origins get credentials only if they fit the entry like `fitsPage` (copy of `baseDomain` in fill.js, the lists come as arguments); `activeTab` does not reach them anyway. Code field: the one `autocomplete="one-time-code"` field, else the only visible field named like a code that is neither username nor password; with several candidates no code. Returns `{ username, password, totp, skipped, foreignFrames }`
- `options.js`: server URL, and the popup's shortcut read with `chrome.commands.getAll()` (`_execute_action`, Alt+Shift+G in the manifest); a button opens `chrome://extensions/shortcuts`
- The version is in `extension/manifest.json` (`version`) and in the README

## Key Technical Details

### Encryption & Security

- All database files are encrypted at rest using AES-CBC with a 32-byte key, without integrity protection (no MAC)
- Encryption key must be exactly 32 bytes/characters
- IV is randomly generated per encryption operation and prepended to encrypted data
- Database is decrypted into memory for every request
- No authentication, no TLS, no CORS configuration; the server binds to all interfaces

### API Patterns

- All API endpoints return IApiResult with consistent structure:
  ```json
  {
    "success": bool,
    "errorMessage": string | null,
    "listInfo": { /* pagination/filtering info */ } | null,
    "data": object | array | null
  }
  ```
- Use `SetDirty()` on LiteDbContext after any write operation to trigger backup on disposal

### Build System

- Targets .NET 9.0 (end of support reached in November 2026)
- Uses LibSassHost for SCSS compilation (native binaries for Windows and Linux)
- Windows builds can run as Windows Service
- Build output goes to `Build/Windows/` or `Build/Linux/`
- wwwroot content is copied to build directories

## Development Notes

- The server port automatically shifts from 80 to 5580 on Windows (Debug and Release)
- `Glutspeicher Server/Data/` in the working folder holds real data: never change or delete it. For tests run the built server from a scratch folder (copy `wwwroot` and `appsettings.json` there, start with `--HttpPort <port>`)
- There are no automated tests for server and client; the browser extension has Node unit tests and an end-to-end test (see "Browser Extension" above)
- Database backups are automatically created with timestamps in `Data/` directory
- Frontend includes are resolved at request time, so changes are immediately visible
- SCSS compilation happens on every request in both DEBUG and RELEASE modes
- Use `SetDirty()` on LiteDbContext after modifications to ensure persistence
