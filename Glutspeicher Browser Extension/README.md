# Glutspeicher Browser Extension

A browser extension for Brave (and other Chromium browsers such as Chrome and Edge) that suggests the logins of a Glutspeicher server for the open page and fills them into its login form. It replaces the browser's own password manager.

- Version 0.2.0 (`extension/manifest.json`), Manifest V3, plain JavaScript without a build step
- The extension itself is in `extension/`; `test/` holds the tests and is not part of it

## Installation

1. Open `brave://extensions` (Chrome: `chrome://extensions`, Edge: `edge://extensions`)
2. Switch on **Developer mode**
3. Click **Load unpacked** and select the folder `Glutspeicher Browser Extension/extension`
4. Pin the extension to the toolbar (puzzle icon → pin), if you like

After updating the files, click the reload button of the extension in `brave://extensions`.

## Settings

The settings open after a click on the gear in the popup, or via `brave://extensions` → Glutspeicher → **Extension options**.

- **Server URL**: the address of the Glutspeicher web interface, e.g. `http://glutspeicher.lan:5580` or `https://pw.example.com`. Without a scheme `http://` is used; a path (server behind a reverse proxy) is kept.
- **Save** asks the browser for access to that address (one site only; the extension asks for no access to other sites up front) and tests it with `GET /api`: *Connected: Glutspeicher Server x.y.z* or an error.

Only this address is stored (`chrome.storage.local`). When the server changes, the access to the old one is given up.

- **Keyboard shortcut**: the shortcut that opens the popup, as the browser has it set (in the browser's language, e.g. *Alt+Shift+G* or *Umschalttaste+Alt+G*), or *Not set*. Extensions cannot change it themselves: **Change shortcut** opens `brave://extensions/shortcuts`.

## Use

Click the extension icon or press **Alt+Shift+G** (change it at `brave://extensions/shortcuts`, see Settings).

- **Suggestions**: entries whose address fits the open page, best first:
  1. same host (`login.example.com`)
  2. one is a subdomain of the other (entry `example.com` on `login.example.com`)
  3. same base domain (entry `www.example.com` on `login.example.com`); the popup asks before it fills these

  The base domain is the last two labels, or three under known registry second levels such as `gv.at`, `co.at`, `or.at` and `co.uk`, so `wien.gv.at` does not fit `bund.gv.at`. Under known shared domains, where every subdomain may have another owner (dynamic DNS and hosting such as `myfritz.net`, `duckdns.org`, `ddns.net`, `dyndns.org`, `no-ip.org`, `github.io`, `netlify.app`, `azurewebsites.net`, `blogspot.com`, and `priv.at`, `ltd.uk`, `me.uk`, `plc.uk`; the list is `SHARED_SUFFIXES` in `lib/match.js`), the owner's label belongs to it: `attacker.myfritz.net` does not fit `abc123.myfritz.net`, `nas.abc123.myfritz.net` does. IP addresses only fit exactly. Entries with `rdp://` or `ssh://` are never suggested; entries without scheme count as `https://` (like the server does).
- **Search**: type into the search field. Like in the web interface, the text is split at spaces and every term must occur in name, username, address, source or section. At most 100 hits are shown.
- **Fill**: <kbd>↑</kbd>/<kbd>↓</kbd> select, <kbd>Enter</kbd> or a click fills the selected entry, <kbd>Esc</kbd> clears the search or closes the popup. The person and key buttons copy the username or the password.

The password is the generated one if the entry has one, otherwise the static one (as in the web interface).

### TOTP codes

Entries with a TOTP secret show their current code in the row (`123 456`, like the web interface), with a ring for the time it is still valid; it runs on every second while the popup is open. A click on the code copies it (*Code copied*). The code is computed like in the web interface: Base32 secret (spaces, dashes, lower case and `=` are fine), HMAC-SHA1, 30 seconds, 6 digits (RFC 6238, `lib/totp.js` with WebCrypto). A secret that is no Base32, also an `otpauth://` URI, shows *Invalid TOTP*. A wrong clock on the computer gives wrong codes.

Filling an entry with TOTP also fills the code, computed right before filling:

- The code field is a visible text, number or phone field marked `autocomplete="one-time-code"`. Without such a mark it is the only field named like a code (*otp*, *totp*, *2fa*, *mfa*, *one time*, *verification*, *code*, *token* in name or id; not *zip*, *promo*, *coupon* and the like) that is neither the username nor the password field. If there are several candidates, also one field per digit, no code is filled: use the copy button there.
- Login and code on one page: all three are filled. When the code is asked for on a page of its own after the login, open the popup again there: then only the code is filled.
- The questions before filling apply to the code as well.

### How filling works

The popup runs a script in the open tab (`chrome.scripting.executeScript`, with the `activeTab` permission the click on the icon grants):

- The password field is the visible `input[type=password]` marked `autocomplete="current-password"`, or else the first one that is not marked `new-password` (change-password forms keep their new-password fields empty).
- The username field is the last visible text, e-mail or phone field before the password field, preferably in the same form; one marked `autocomplete="username"`/`email` wins. One-time-code fields are left out.
- **Two-step logins**: without a password field only a field that clearly asks for the username is filled (`autocomplete="username"`/`email`, `type=email`, or a name/id like *user*, *login*, *email*). Open the popup again on the password step.
- The values are set through the native `value` setter and followed by `input` and `change` events, so pages built with React, Vue or Angular take them over.
- Frames of the same site are filled too. Frames of another site get the credentials only if they are the site of the entry (same host, or a sub/parent domain with the same base domain). Note: `activeTab` reaches only the page's own site, so login forms in frames of other sites (embedded logins) cannot be filled; the popup says so, use the copy buttons there.

### Questions before filling

The popup asks before it fills ("Fill anyway" / "Cancel", Cancel has the focus):

- if the entry is not saved for this host or a sub/parent domain of it: an entry picked by search for another site, and also a suggestion that only shares the base domain (entry `abc123.example.net` on `attacker.example.net` may belong to someone else), and
- if the page is plain `http://` outside of `localhost` and private networks (10.x, 172.16–31.x, 192.168.x).

## Security

- **The server has no login and no TLS** (see the main README). Every time the popup opens, it loads **all entries including the passwords** with `GET /api/passwords`. Use the server only in a trusted network, or put it behind a reverse proxy with HTTPS and enter that `https://` address.
- Passwords and TOTP secrets are held only in the memory of the popup while it is open; they are never written to the browser's storage, and neither are the codes. Copied passwords and codes stay in the clipboard.
- Every value from the server is shown as text (`textContent`), never as HTML.
- Requests are sent with `credentials: "include"`, so a login of the server (once it has one, #1) can work through its cookie.
- Entries from CSV sources (Exports) are not part of `GET /api/passwords` and therefore not offered.

## Permissions

| Permission | Why |
|---|---|
| `storage` | the server URL |
| `activeTab` | read the address of the open tab and fill it, only after a click on the icon or the shortcut |
| `scripting` | run the fill script in the open tab |
| optional host access to the server's origin | load the entries; asked for when the server URL is saved |

## Tests

Unit tests of the matching, the search, the URL handling, the TOTP codes (RFC 6238 test vectors) and the choice of the code field (Node.js 18 or newer):

```bash
cd "Glutspeicher Browser Extension"
node --test "test/*.test.js"
```

End-to-end test in a separate, headless Brave with a temporary profile (your own Brave profile is not touched). It needs a Glutspeicher server with an **empty scratch database**: the test writes its entries into it and refuses to run against a database with other entries. Never point it at the real server.

```bash
# Scratch server: build, then run from an empty folder with wwwroot and appsettings.json
dotnet build "Glutspeicher Server/Glutspeicher Server.csproj" -o /tmp/glut-test/bin
cp -r "Glutspeicher Server/wwwroot" "Glutspeicher Server/appsettings.json" /tmp/glut-test/
(cd /tmp/glut-test && dotnet "bin/Glutspeicher Server.dll" --HttpPort 5591) &

cd "Glutspeicher Browser Extension/test/e2e"
npm install
GLUT_SERVER=http://127.0.0.1:5591 node e2e.js
```

`BROWSER_PATH` sets another browser (default: Brave in `C:\Program Files`). The test serves its pages on port 5592 (single-step and two-step login, login with a code on a second step, React-like value tracking, change-password form, same-site and cross-site frames, a page without login) and triggers the extension like a click on its icon. Headless browsers cannot answer the permission prompt, so the main part runs with a copy of the extension that holds the access to the test server already; the prompt itself is checked by hand.
