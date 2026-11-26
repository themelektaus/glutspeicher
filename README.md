# Glutspeicher

**A self-hosted password manager with automation (AutoType) and relay capabilities.**

Glutspeicher is a password manager built with a server-client architecture: a web server that stores the credentials in a database encrypted at rest, and a Windows client that types credentials and opens RDP/SSH/web connections, optionally through a relay host.

> **Security notice: the server has no authentication.** There are no users, no login and no TLS. Everyone who can reach the HTTP port can read, change and delete all stored credentials and download the decrypted database (`GET /api/database`). The server listens on all interfaces. Run it only on a trusted network or behind a reverse proxy that enforces authentication and HTTPS, and never expose the port directly.

## Key Features

### Secure Password Storage
- **Encryption at Rest**: The LiteDB database file is encrypted with AES (CBC, 32-byte key from `GLUTSPEICHER_CRYPTO_KEY`); the API itself returns all data unencrypted
- **Automatic Backups**: Every database modification creates a timestamped, compressed, and encrypted backup
- **Self-Hosted**: Complete control over your data - no third-party services or cloud dependencies
- **In-Memory Processing**: For every request the encrypted file is read, decrypted and opened in memory; requests are serialized by a global lock

### Password Management
- **Static & Generated Passwords**: Support for both manually entered passwords and rule-based password generation
- **Password Generators**: Define custom password generation rules with configurable length and character sets
- **TOTP Support**: Built-in Time-based One-Time Password (TOTP) support for two-factor authentication
- **CSV Export**: Download passwords, generators, exports and relays as CSV (unencrypted, including all passwords)
- **Organized Storage**: Group passwords by sections and sources for easy navigation

### Advanced Connectivity
- **Protocol Support**: Native support for HTTP/HTTPS, SSH, and RDP connections
- **Relay Sessions**: Connect to remote systems through SSH relay servers for secure access to isolated networks
- **AutoType Functionality**: Automated keyboard input for username/password entry (Windows Client)
- **GlutLink Protocol**: Custom URI scheme for deep integration between server and client applications

### Modern Web Interface
- **Single Page Application**: Vanilla JavaScript frontend with an optional mobile layout (Settings)
- **Real-time SCSS Compilation**: Server-side SCSS preprocessing for modern styling
- **Component-Based Architecture**: Modular design with server-side includes for HTML, CSS, and JS
- **Multiple Pages**: Dedicated interfaces for Passwords, Exports, Generators, Relays, and Settings

### Flexible Deployment
- **Docker Support**: Pre-built Docker images available on Docker Hub
- **Cross-Platform Server**: Runs on Windows and Linux with .NET 9.0
- **Windows Service Integration**: Automatic Windows Service registration in production mode
- **Multiple Deployment Options**: Standalone binaries, Docker containers, or from source
- **Note**: .NET 9.0 has reached its end of support (November 2026); the project still targets it and gets no runtime security updates until it is moved to a supported version

## Architecture

### Server Component
Built with **ASP.NET Core 9.0**, the server provides:
- RESTful API for all CRUD operations (Passwords, Generators, Exports, Relays)
- Embedded web frontend with server-side includes and SCSS compilation
- LiteDB NoSQL database with encryption and automatic backup
- Thread-safe database operations with semaphore locking
- Custom middleware pipeline for API routing, frontend serving, and cache control

**Technology Stack:**
- .NET 9.0 (ASP.NET Core)
- LiteDB 5.0.21 (embedded NoSQL database)
- LibSassHost 1.5.0 (SCSS compilation)
- CsvCSharp 0.1.6 (CSV export functionality)

### Client Component (Windows)
Built with **.NET 9.0 Windows**, the client provides:
- **AutoType**: Automated keyboard input for password entry
- **Relay Management**: SSH tunnel creation for remote system access
- **Protocol Handler**: Custom `glut://` URI scheme support for deep integration
- **Windows Native Integration**: DPI awareness and native window management

**Technology Stack:**
- .NET 9.0 Windows
- SSH.NET 2024.2.0 (SSH connectivity)
- Newtonsoft.Json 13.0.3 (JSON processing)

### Frontend
A modern **vanilla JavaScript SPA** featuring:
- Component-based architecture with reusable UI elements
- Server-side includes for modular HTML/CSS/JS composition
- SCSS stylesheets with on-demand compilation
- Third-party libraries: jsOTP (TOTP), PapaParse (CSV), Showdown (Markdown), password-generator.js

## Getting Started

### Option 1: Docker (Recommended)

The fastest way to get started is using Docker. The compose files need a key of your own in `GLUTSPEICHER_CRYPTO_KEY`; without it `docker-compose up` stops with an error:

```bash
# Create .env with your own key (exactly 32 characters), see .env.example
echo "GLUTSPEICHER_CRYPTO_KEY=$(openssl rand -hex 16)" > .env

# Using pre-built image from Docker Hub
docker-compose up
```

**docker-compose.yml:**
```yaml
services:
  app:
    image: melektaus/glutspeicher
    ports:
      - 8100:80
    volumes:
      - /etc/localtime:/etc/localtime:ro
      - ./data:/app/Data
    environment:
      - "GLUTSPEICHER_CRYPTO_KEY=${GLUTSPEICHER_CRYPTO_KEY:?set GLUTSPEICHER_CRYPTO_KEY (32 characters) in .env, see .env.example}"
```

Access the application at `http://localhost:8100`

**Keep the key safe.** It must be exactly 32 ASCII characters. Without it the database and the backups in `./data` cannot be read, and if you change it later, the existing database can no longer be read. The public test key (32 zeros) is refused at start by Release builds (the Docker image is one).

**Upgrading an installation that ran with the test key** (the compose files up to v0.5.9 set it): the new version does not start with it. Before you update, download the decrypted database in the old version (Settings, or `GET /api/database`), then set a key of your own, put the downloaded file as `./data/Database.litedb` and start the new version: it encrypts it with the new key on the first request (see "Restore" under "Data Protection"). The old backups in `./data` stay encrypted with the test key; delete them once the new database works.

### Option 2: Windows Standalone

1. Download the server from the web interface of an existing instance or build from source
2. Run the executable (Release build) - it will automatically:
   - Request administrator privileges
   - Generate a 32-character encryption key (if none is set) and store it as machine-level environment variable
   - Remove and re-register the Windows Service `Glutspeicher Server` (runs as LocalSystem)
   - Start the service and open your browser at `http://localhost:5580`

On Windows a configured port 80 always becomes 5580 (Debug and Release).

### Option 3: Linux Standalone

1. Build or download the Linux server binary
2. Set the encryption key environment variable:
   ```bash
   export GLUTSPEICHER_CRYPTO_KEY="your-32-character-encryption-key"
   ```
3. Run the server:
   ```bash
   ./Glutspeicher\ Server
   ```

### Option 4: Build from Source

**Prerequisites:**
- .NET 9.0 SDK

**Build Server:**
```bash
# Development
dotnet run --project "Glutspeicher Server/Glutspeicher Server.csproj"

# Production (Windows)
dotnet publish "Glutspeicher Server/Glutspeicher Server.csproj" -c Release -r win-x64 --self-contained -o "Glutspeicher Server/Build/Windows"

# Production (Linux)
dotnet publish "Glutspeicher Server/Glutspeicher Server.csproj" -c Release -r linux-x64 --self-contained -o "Glutspeicher Server/Build/Linux"
```

**Build Client (Windows only):**
```bash
dotnet build "Glutspeicher Client/Glutspeicher Client.csproj"
```

## Configuration

### Environment Variables

**GLUTSPEICHER_CRYPTO_KEY** (Required in production)
- 32-character encryption key for database encryption
- **Debug mode**: Automatically set to `00000000000000000000000000000000`
- **Release mode (Windows)**: Auto-generated and stored as machine-level environment variable if not set
- **Docker/Linux**: Must be explicitly configured (for Docker Compose in `.env`, see `.env.example`)
- Must be exactly 32 bytes long (UTF-8), otherwise the server exits. Longer values are cut to 32 bytes.
- Release builds exit if the key is the public test key (32 zeros)
- If the key does not match an existing database, the database cannot be opened and the API answers with errors. There is no key rotation.

### appsettings.json

Located in `Glutspeicher Server/appsettings.json`:

```json
{
    "HttpPort": 80,
    "ServerVersion": "0.5.9",
    "ClientVersion": "0.3.6"
}
```

- **HttpPort**: Server listening port (default: 80; on Windows a value of 80 always becomes 5580). The server binds to all interfaces (`http://[::]:<port>`)
- **ServerVersion**: Server application version (must match build version)
- **ClientVersion**: Version of the client offered for download; only displayed in the web interface, not checked against the installed client

## Security Features

### Encryption
- **Database Encryption**: AES-CBC with a 32-byte key and a randomly generated IV per operation. There is no integrity protection (no MAC): a manipulated file is not detected reliably
- **Encrypted Backups**: All backup files are compressed (GZip) and encrypted with the same key
- **Key Storage**: Encryption key stored as machine-level environment variable (Windows) or externally managed (Docker/Linux). On Windows every local user and process can read a machine-level environment variable
- **Scope**: Encryption protects the files in `Data/` only. It does not protect against anyone who can reach the HTTP port (see the security notice at the top)

### Data Protection
- **Automatic Backups**: Timestamped backups created on every database modification. Old backups are never removed: clean up `Data/` yourself
- **Backup Location**: `Data/Database YYYY-MM-DD HH-mm-ss.litedb.gz.encrypted`
- **Restore**: Stop the server and place a plain `Database.litedb` (e.g. from `GET /api/database`) into `Data/`; on the next request it replaces the current database and is encrypted
- **Current Database**: `Data/Database.litedb.encrypted`
- **No Cloud Storage**: All data remains on your infrastructure

### Best Practices
- Put an authenticating HTTPS reverse proxy in front of the server; the server itself has no login
- Use a strong, randomly generated 32-character encryption key (never the test key of 32 zeros)
- Store the encryption key securely and separately from the database
- Regularly backup the `Data` directory to external storage
- Use HTTPS reverse proxy (nginx, Caddy) for production deployments
- Limit network access to trusted clients only

## Advanced Features

### Password Generators
Define reusable password generation rules:
- Configurable length
- A list of questions; the answers are stored with the generator
- Passwords are derived deterministically (MD5-based) from the answers, the domain and the username, so they can be recalculated without the database
- A standalone HTML "Password Recovery File" can be downloaded for offline recalculation

### Export Configurations
Despite the name, an "Export" is a CSV source that is loaded into the password list:
- Define a name and the URI of a CSV file (e.g. a file placed in `wwwroot/static/exports/`, reachable as `/<name>.csv`)
- The browser fetches the CSV and shows its rows as passwords
- Optional JavaScript per export transforms or filters each row; it is executed with `eval` in the browser, so only use scripts and sources you trust

### Relay System
Access systems behind firewalls or in isolated networks:
- SSH tunnel creation through relay servers
- Port range management (default: 13300-13309)
- Support for RDP, SSH, and Web protocols
- The client logs in to the relay host via SSH (password, default user `root`) and creates an iptables DNAT rule from a free relay port to the target; the rule is removed when the session ends
- While a session is open the relay port accepts connections from any source address
- The SSH password of the relay is stored in the database and is part of every GlutLink that uses the relay
- Host keys (relay, SSH targets) and RDP server certificates are not verified

### AutoType (Windows Client)
Automated credential entry:
- System-wide keyboard automation
- Triggered via `glut://` protocol links (Base64-encoded JSON that contains the credentials in plain text)
- Supports username + password sequences
- The client does not talk to the server; it only receives `glut://` links from the browser
- Registering the client (start it without arguments) adds the `glut://` handler for the current user and sets `AuthenticationLevelOverride=0` for the RDP client (no certificate warnings)

## API Documentation

The server exposes a RESTful API with consistent response format:

```json
{
  "success": true,
  "errorMessage": null,
  "listInfo": null,
  "data": { /* resource data */ }
}
```

### Endpoints

All endpoints are unauthenticated.

**General**
- `GET /api` - Server name, version and list of endpoints
- `GET /api/database` - Download the decrypted database (`.litedb`)
- `GET /api/database/rebuild` - Rebuild (compact) the database

**Passwords**
- `GET /api/passwords` - List all passwords
- `GET /api/passwords/{id}` - Get password with GlutLinks
- `POST /api/passwords` - Create password
- `PUT /api/passwords` - Update password
- `DELETE /api/passwords/{id}` - Delete password
- `GET /api/passwords/export` - Export to CSV

**Generators**
- `GET /api/generators` - List all generators
- `GET /api/generators/{id}` - Get generator
- `GET /api/generators/export` - Export to CSV
- `POST /api/generators` - Create generator
- `PUT /api/generators` - Update generator
- `DELETE /api/generators/{id}` - Delete generator

**Exports**
- `GET /api/exports` - List all export configurations
- `GET /api/exports/{id}` - Get export configuration
- `GET /api/exports/export` - Export to CSV
- `POST /api/exports` - Create export configuration
- `PUT /api/exports` - Update export configuration
- `DELETE /api/exports/{id}` - Delete export configuration

**Relays**
- `GET /api/relays` - List all relay configurations
- `GET /api/relays/{id}` - Get relay configuration
- `GET /api/relays/export` - Export to CSV
- `POST /api/relays` - Create relay configuration
- `PUT /api/relays` - Update relay configuration
- `DELETE /api/relays/{id}` - Delete relay configuration

## Project Structure

```
Glutspeicher/
├── Glutspeicher Server/          # ASP.NET Core server application
│   ├── Context/                  # LiteDB database context
│   ├── Mapping/                  # API endpoint definitions
│   ├── Middleware/               # Custom middleware (Frontend, NoCache)
│   ├── Model/                    # Data models (Password, Generator, Export, Relay)
│   ├── Routing/                  # API routing configuration
│   ├── wwwroot/                  # Static web assets
│   │   ├── static/
│   │   │   ├── css/             # SCSS stylesheets
│   │   │   ├── js/              # JavaScript modules
│   │   │   ├── page/            # SPA page components
│   │   │   └── res/             # Resources (images, icons)
│   │   └── index.html           # Main HTML entry point
│   ├── Program.cs               # Application entry point
│   └── appsettings.json         # Configuration
├── Glutspeicher Client/          # Windows client application
│   ├── Actions/                  # Client action handlers
│   ├── AutoType/                 # AutoType functionality
│   ├── Tausi.NativeWindow/       # Native Windows integration
│   ├── Program.cs               # Client entry point
│   ├── RelaySession.cs          # SSH relay management
│   └── Utils.cs                 # Utility functions
├── docker-compose.yml           # Docker Compose configuration
├── Dockerfile                   # Docker image definition
└── Glutspeicher.sln            # Visual Studio solution
```

## Use Cases

### Personal Password Management
- Store and organize personal credentials securely
- Generate strong passwords with custom rules
- Quick access via modern web interface
- Automatic backups for peace of mind

### Home Lab Administration
- Manage credentials for multiple servers and services
- SSH/RDP connection management with relay support
- TOTP support for 2FA-enabled services
- Self-hosted solution with no external dependencies

### Small Team Collaboration
- Shared password database on a trusted internal network (there are no user accounts: everyone sees and can change everything)
- Organized sections for different projects/clients
- CSV export for integration with other tools
- Timestamped backups after every change (no record of who changed what)

### Development & Testing
- Store API keys and service credentials
- Quick password generation for test accounts
- Docker deployment for containerized environments

## What Makes Glutspeicher Unique

1. **True Self-Hosting**: Complete control over your data with no cloud dependencies
2. **Hybrid Architecture**: Combined web and native client for optimal functionality
3. **Advanced Relay System**: Access isolated systems through SSH tunnels
4. **AutoType Integration**: Seamless credential entry on Windows systems
5. **Automatic Backups**: Every change creates a compressed, encrypted backup
6. **Developer-Friendly**: Clean REST API, modern tech stack, open architecture
7. **Zero Vendor Lock-in**: Standard database format (LiteDB), CSV export, source available on GitHub
8. **Deployment**: Windows Service support, Docker images

## Version Information

- **Current Server Version**: 0.5.9
- **Current Client Version**: 0.3.6
- **Target Framework**: .NET 9.0
- **Minimum Requirements**: ASP.NET Core 9.0 Runtime (server, unless published self-contained), .NET 9.0 Desktop Runtime (Windows client)

## Links

- **Docker Hub**: https://hub.docker.com/r/melektaus/glutspeicher
- **GitHub Repository**: https://github.com/themelektaus/glutspeicher
- **Server and Client Downloads**: Linked in the web interface of a deployed instance (`static/glutspeicher-*.zip`). The zip files are created by the client's `Deploy` action and are not part of the repository

## Development

### Debug Mode
- Encryption key automatically set to test value
- Port 80 becomes 5580 on Windows (also in Release mode)
- Data and `wwwroot` are resolved relative to the current directory
- Server-side includes and SCSS are resolved on every request (also in Release mode)

### Release Mode
- Windows: automatic Windows Service registration when started interactively (administrator privileges required), encryption key generation, working directory is the folder of the executable
- Linux/Docker: starts directly; `GLUTSPEICHER_CRYPTO_KEY` must be set

### Building the Client
`Glutspeicher Client.csproj` imports `Deploy.targets`, which is not part of the repository (it holds local deployment settings). On a fresh clone the build fails with MSB4019 until you create an empty one:

```xml
<Project />
```

### Contributing
Built with modern .NET practices and clean architecture. The codebase is organized for maintainability with:
- Separation of concerns (API, Middleware, Data)
- Dependency injection
- RESTful API design
- Component-based frontend

## License

The repository does not contain a license file yet. Without a license the usual copyright rules apply: the code is published, but no rights to use, change or redistribute it are granted.

---

**Glutspeicher** - German for "ember store" (Glut = embers), a vault for your digital credentials.
