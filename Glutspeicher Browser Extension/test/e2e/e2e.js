// End-to-end test of the extension in a separate, headless Brave (or Chrome) with its own
// temporary profile. See ../../README.md ("Tests") for how to run it.
//
// GLUT_SERVER  URL of a Glutspeicher server with an EMPTY scratch database (never the real one):
//              the test writes its own entries into it. Required.
// BROWSER_PATH Path of brave.exe/chrome.exe (default: Brave on Windows)
//
// The test pages are served by this script on 127.0.0.1:5592 (and localhost:5592 for the
// cross-origin frame).
const puppeteer = require('puppeteer-core');
const fs = require('fs');
const http = require('http');
const os = require('os');
const path = require('path');

const EXT = path.resolve(__dirname, '../../extension');
const TMP = path.join(os.tmpdir(), `glutspeicher-e2e-${Date.now()}`);
const EXT_TEST = path.join(TMP, 'extension');
const SERVER = process.env.GLUT_SERVER ? new URL(process.env.GLUT_SERVER).href.replace(/\/?$/, '/') : null;
const SITE_PORT = 5592;
const SITE = `http://127.0.0.1:${SITE_PORT}/`;
const BROWSER_PATH = process.env.BROWSER_PATH || 'C:/Program Files/BraveSoftware/Brave-Browser/Application/brave.exe';
const sleep = ms => new Promise(r => setTimeout(r, ms));

let failed = 0;
function check(name, ok, info) {
  if (!ok) failed++;
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${info !== undefined ? '  -> ' + JSON.stringify(info) : ''}`);
}

const SEED = [
  { name: 'Test Login', uri: `http://127.0.0.1:${SITE_PORT}/login`, username: 'alice', password: 'alice-pass', section: 'Tests' },
  { name: 'Localhost Frame', uri: 'localhost', username: 'bob', password: 'bob-pass' },
  { name: 'Generated Example', uri: 'example.com', username: 'carol', password: 'static-wrong', generatedPassword: 'gen-123' },
  { name: 'Server RDP', uri: 'rdp://127.0.0.1', username: 'admin', password: 'rdp-pass' },
  { name: 'Evil <img src=x onerror=alert(1)>', uri: 'other.org', username: '<b>mallory</b>', password: 'evil"pass' },
  { name: 'Wien Amt', uri: 'www.wien.gv.at', username: 'dave', password: 'wien-pass', source: 'Gemeinde' },
  { name: 'Other gv.at', uri: 'https://bund.gv.at', username: 'eve', password: 'bund-pass' },
  ...Array.from({ length: 120 }, (_, i) => ({ name: `Bulk ${i + 1}`, uri: `bulk${i + 1}.test`, username: `user${i + 1}`, password: `pw${i + 1}` })),
];

// Writes the test entries, but only into an empty database (or one that holds just them)
async function seed() {
  const items = (await (await fetch(SERVER + 'api/passwords')).json()).data ?? [];
  const names = new Set(SEED.map(x => x.name));
  if (items.some(x => !names.has(x.name))) {
    throw new Error(`${SERVER} holds other entries. Run the test only against a scratch server with an empty database.`);
  }
  if (items.length == SEED.length) return;
  if (items.length) throw new Error(`${SERVER} holds only some of the test entries. Start again with an empty database.`);
  for (const item of SEED) {
    await fetch(SERVER + 'api/passwords', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(item) });
  }
}

function serveSite() {
  const types = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript' };
  const server = http.createServer((req, res) => {
    const file = path.join(__dirname, 'site', path.basename(new URL(req.url, SITE).pathname));
    if (!fs.existsSync(file)) { res.writeHead(404); res.end(); return; }
    res.writeHead(200, { 'Content-Type': types[path.extname(file)] ?? 'application/octet-stream' });
    res.end(fs.readFileSync(file));
  });
  return new Promise(resolve => server.listen(SITE_PORT, '127.0.0.1', () => resolve(server)));
}

// Copy of the extension that already holds the permission for the test server only
// (the permission prompt of chrome.permissions.request cannot be answered in headless mode)
function makeTestCopy() {
  fs.cpSync(EXT, EXT_TEST, { recursive: true });
  const manifestPath = path.join(EXT_TEST, 'manifest.json');
  const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
  manifest.host_permissions = [`${new URL(SERVER).origin}/*`];
  fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2));
}

async function launch(profile) {
  return puppeteer.launch({
    executablePath: BROWSER_PATH,
    headless: true, pipe: true, enableExtensions: true,
    userDataDir: path.join(TMP, profile),
    args: ['--no-first-run', '--no-default-browser-check'],
  });
}

async function openPopup(browser, ext, page) {
  await page.bringToFront();
  await page.triggerExtensionAction(ext);
  const target = await browser.waitForTarget(t => t.url().endsWith('/popup.html'), { timeout: 5000 });
  const popup = await target.asPage();
  await popup.waitForFunction(() => {
    const h = document.querySelector('.heading').textContent;
    return (h && !h.startsWith('Loading')) || !document.querySelector('.message').hidden;
  }, { timeout: 5000 });
  return popup;
}

async function popupState(popup) {
  return popup.evaluate(() => ({
    heading: document.querySelector('.heading').textContent,
    names: [...document.querySelectorAll('.list li .name')].map(x => x.textContent),
    selected: [...document.querySelectorAll('.list li')].findIndex(x => x.getAttribute('aria-selected') == 'true'),
    message: document.querySelector('.message').hidden ? null : document.querySelector('.message .text').textContent,
    action: document.querySelector('.message .action').hidden ? null : document.querySelector('.message .action').textContent,
    confirm: document.querySelector('.confirm').hidden ? null : document.querySelector('.confirm .text').textContent,
    more: document.querySelector('.more').hidden ? null : document.querySelector('.more').textContent,
    focus: document.activeElement?.className || document.activeElement?.tagName,
  }));
}

async function waitClosed(popup) {
  for (let i = 0; i < 30; i++) {
    if (popup.isClosed()) return true;
    await sleep(100);
  }
  return popup.isClosed();
}

async function values(page, frameSelector) {
  let frame = page.mainFrame();
  if (frameSelector) frame = await (await page.$(frameSelector)).contentFrame();
  return frame.evaluate(() => Object.fromEntries([...document.querySelectorAll('input')].map(x => [x.name || x.id, x.value])));
}

async function runWithoutPermission() {
  console.log('--- Original extension (no host permission yet)');
  const browser = await launch('profile-a');
  try {
    const id = await browser.installExtension(EXT);
    const ext = (await browser.extensions()).get(id);
    const page = await browser.newPage();
    await page.goto(SITE + 'login.html');

    let popup = await openPopup(browser, ext, page);
    let s = await popupState(popup);
    check('popup without server URL asks to open the settings', s.message == 'No Glutspeicher server is set yet.' && s.action == 'Open settings', s);
    await popup.close();

    const options = await browser.newPage();
    await options.goto(`chrome-extension://${id}/options.html`);
    await options.evaluate(serverUrl => chrome.storage.local.set({ serverUrl }), SERVER);
    await options.reload();
    await sleep(500);
    const status = await options.$eval('.status', e => e.textContent);
    check('options without permission say so', status.startsWith('No access to ' + new URL(SERVER).origin), status);
    const stored = await options.evaluate(() => chrome.storage.local.get(null));
    check('storage holds only the server URL', JSON.stringify(stored) == JSON.stringify({ serverUrl: SERVER }), stored);

    popup = await openPopup(browser, ext, page);
    s = await popupState(popup);
    check('popup without permission offers to allow access', s.message?.includes('no access to ' + new URL(SERVER).origin) && s.action == 'Allow access', s);
    await popup.close();
  } finally {
    await browser.close();
  }
}

async function runWithPermission() {
  console.log('--- Test copy (permission for the test server)');
  makeTestCopy();
  const browser = await launch('profile-b');
  try {
    const id = await browser.installExtension(EXT_TEST);
    const ext = (await browser.extensions()).get(id);

    // Options
    const options = await browser.newPage();
    await options.goto(`chrome-extension://${id}/options.html`);
    await options.type('#server-url', 'ftp://x');
    await options.click('button[type=submit]');
    let status = await options.$eval('.status', e => e.className + '|' + e.textContent);
    check('options refuse a non-http URL', status.includes('error'), status);
    await options.$eval('#server-url', e => e.value = '');
    await options.type('#server-url', SERVER.replace(/^http:\/\//, ''));
    await options.click('button[type=submit]');
    await options.waitForFunction(() => /Connected|error/.test(document.querySelector('.status').className + document.querySelector('.status').textContent), { timeout: 5000 });
    status = await options.$eval('.status', e => e.className + '|' + e.textContent);
    check('options save and test the server', /^status ok\|Connected: Glutspeicher Server \d/.test(status), status);
    check('options normalize the URL', await options.$eval('#server-url', e => e.value) == SERVER);

    const page = await browser.newPage();

    // Single-step login, suggestions, Enter fills
    await page.goto(SITE + 'login.html');
    let popup = await openPopup(browser, ext, page);
    let s = await popupState(popup);
    check('suggestions for 127.0.0.1 (rdp entry left out)', s.heading == 'Suggestions for 127.0.0.1' && JSON.stringify(s.names) == '["Test Login"]' && s.selected == 0, s);
    check('search field has the focus', s.focus == '' || s.focus == 'INPUT', s.focus);
    await popup.keyboard.press('Enter');
    check('popup closes after filling', await waitClosed(popup));
    let v = await values(page);
    check('single-step: username and password filled, nothing else', v.user == 'alice' && v.pass == 'alice-pass' && v.q == '' && v.newsletter == '' && v.hiddenuser == '' && v.otp == '', v);
    await page.click('#f button');
    check('single-step: form submits the values', (await page.$eval('#out', e => e.textContent)).includes('"pass":"alice-pass"'));

    // Search with limit, keyboard selection
    await page.goto(SITE + 'login.html');
    popup = await openPopup(browser, ext, page);
    await popup.type('.search input', 'bulk');
    s = await popupState(popup);
    check('search finds 120 entries and shows 100', s.heading == '120 entries found' && s.names.length == 100 && s.more?.startsWith('Showing the first 100'), { heading: s.heading, n: s.names.length, more: s.more });
    await popup.type('.search input', ' 11');
    s = await popupState(popup);
    check('search with two terms', JSON.stringify(s.names) == '["Bulk 11","Bulk 110","Bulk 111","Bulk 112","Bulk 113","Bulk 114","Bulk 115","Bulk 116","Bulk 117","Bulk 118","Bulk 119"]', s.names);
    await popup.keyboard.press('ArrowDown');
    await popup.keyboard.press('ArrowDown');
    await popup.keyboard.press('ArrowUp');
    await popup.keyboard.press('ArrowDown');
    s = await popupState(popup);
    check('arrow keys move the selection', s.selected == 2, s.selected);
    await popup.keyboard.press('Escape');
    s = await popupState(popup);
    check('Escape clears the search and shows the suggestions again', s.heading == 'Suggestions for 127.0.0.1' && s.names.length == 1, s);

    // Values from the server only as text
    await popup.type('.search input', 'evil');
    s = await popupState(popup);
    const injected = await popup.evaluate(() => document.querySelectorAll('.list img, .list b').length);
    check('names are shown as text, not HTML (#5)', s.names[0] == 'Evil <img src=x onerror=alert(1)>' && injected == 0, { names: s.names, injected });

    // Entry of another site: confirmation first
    await popup.$eval('.search input', e => e.value = '');
    await popup.type('.search input', 'generated');
    await popup.keyboard.press('Enter');
    s = await popupState(popup);
    check('entry of another site asks first', s.confirm?.includes('is saved for example.com, but this page is 127.0.0.1') && s.focus == 'cancel', s);
    await popup.keyboard.press('Enter'); // Cancel has the focus
    s = await popupState(popup);
    v = await values(page);
    check('Enter on the question cancels and fills nothing', s.confirm == null && v.user == '' && !popup.isClosed(), { s, v });
    await popup.keyboard.press('Enter');
    await popup.click('.confirm .fill');
    check('popup closes after "Fill anyway"', await waitClosed(popup));
    v = await values(page);
    check('generated password wins over the static one', v.user == 'carol' && v.pass == 'gen-123', v);

    // Two-step login
    await page.goto(SITE + 'twostep.html');
    popup = await openPopup(browser, ext, page);
    await popup.keyboard.press('Enter');
    await waitClosed(popup);
    v = await values(page);
    check('two-step: step 1 fills the username only', v.email == 'alice' && v.pass == '' && Object.values(v)[0] == '', v);
    await page.click('#f1 button');
    popup = await openPopup(browser, ext, page);
    await popup.keyboard.press('Enter');
    await waitClosed(popup);
    await page.click('#step2 button');
    const out = await page.$eval('#out', e => e.textContent);
    check('two-step: step 2 fills the password', out == '{"pass":"alice-pass","email":"alice"}', out);

    // React-like value tracking
    await page.goto(SITE + 'react.html');
    popup = await openPopup(browser, ext, page);
    await popup.keyboard.press('Enter');
    await waitClosed(popup);
    await page.click('#b');
    const reactOut = await page.$eval('#out', e => e.textContent);
    check('React-like field sees the values', reactOut == '{"u":"alice","p":"alice-pass"}', reactOut);

    // Change-password form: only the current password
    await page.goto(SITE + 'signup.html');
    popup = await openPopup(browser, ext, page);
    await popup.keyboard.press('Enter');
    await waitClosed(popup);
    v = await values(page);
    check('change-password form: current password only', v.user == 'alice' && v.current == 'alice-pass' && v.new == '' && v.confirm == '', v);

    // Page without login fields
    await page.goto(SITE + 'search-only.html');
    popup = await openPopup(browser, ext, page);
    await popup.keyboard.press('Enter');
    await sleep(500);
    s = await popupState(popup);
    v = await values(page);
    check('page without login fields: message, nothing filled', s.message == 'No login fields found on this page.' && !popup.isClosed() && v.q == '' && v.comment == '', { s, v });
    await popup.close();

    // Same-origin iframe
    await page.goto(SITE + 'frame-same.html');
    popup = await openPopup(browser, ext, page);
    await popup.keyboard.press('Enter');
    check('same-origin frame: popup closes', await waitClosed(popup));
    v = await values(page, '#same');
    check('same-origin frame: filled', v.user == 'alice' && v.pass == 'alice-pass', v);

    // Cross-origin iframe (localhost in 127.0.0.1)
    await page.goto(SITE + 'frame.html');
    await sleep(500);
    popup = await openPopup(browser, ext, page);
    await popup.keyboard.press('Enter');
    await sleep(500);
    s = await popupState(popup);
    v = await values(page, '#cross');
    check('cross-origin frame: entry of the page is not filled into it', v.user == '' && v.pass == '' && s.message == 'No login fields found on this page. Frames of other sites (localhost) cannot be filled; use the copy buttons instead.', { message: s.message, v });
    await popup.$eval('.search input', e => e.value = '');
    await popup.type('.search input', 'localhost frame');
    await popup.keyboard.press('Enter');
    await popup.click('.confirm .fill');
    await sleep(700);
    s = await popupState(popup).catch(() => ({ closed: true }));
    v = await values(page, '#cross');
    check('cross-origin frame: not reachable with activeTab, popup says so', v.user == '' && s.message?.includes('(localhost) cannot be filled'), { message: s.message, v });
    if (!popup.isClosed()) await popup.close();

    // Page on localhost
    await page.goto('http://localhost:5592/login.html');
    popup = await openPopup(browser, ext, page);
    s = await popupState(popup);
    check('suggestions for localhost', s.heading == 'Suggestions for localhost' && JSON.stringify(s.names) == '["Localhost Frame"]', s);
    await popup.keyboard.press('Escape');
    check('Escape with empty search closes the popup', await waitClosed(popup));

    // Not a web page
    await page.goto('about:blank');
    popup = await openPopup(browser, ext, page);
    s = await popupState(popup);
    check('page that cannot be filled', s.heading == 'Nothing to fill on this page. Type to search.' && s.names.length == 0, s);
    await popup.type('.search input', 'test login');
    await popup.keyboard.press('Enter');
    s = await popupState(popup);
    check('filling there gives a message', s.message == 'This page cannot be filled. Use the copy buttons instead.', s);
    await popup.close();

    // Copy button
    await page.goto(SITE + 'search-only.html');
    popup = await openPopup(browser, ext, page);
    await popup.click('.list li button[title="Copy password"]');
    await sleep(200);
    const toast = await popup.$eval('.toast', e => e.textContent).catch(e => 'no toast: ' + e.message);
    v = await values(page);
    check('copy password shows a toast and does not fill', toast == 'Password copied' && !popup.isClosed() && v.q == '', { toast, v });
    await popup.close();

    // Server error
    await options.evaluate(serverUrl => chrome.storage.local.set({ serverUrl }), SERVER + 'nope/');
    popup = await openPopup(browser, ext, page);
    s = await popupState(popup);
    check('server error is shown with "Try again"', s.message?.startsWith('The server') && s.action == 'Try again', s);
    await popup.close();
    await options.evaluate(serverUrl => chrome.storage.local.set({ serverUrl }), SERVER);

    // Screenshots
    await page.goto(SITE + 'login.html');
    popup = await openPopup(browser, ext, page);
    await popup.screenshot({ path: path.join(TMP, 'shot-popup.png') });
    await popup.type('.search input', 'generated');
    await popup.keyboard.press('Enter');
    await popup.screenshot({ path: path.join(TMP, 'shot-confirm.png') });
    await popup.close();
    await options.setViewport({ width: 390, height: 800 });
    await options.reload();
    await sleep(500);
    await options.screenshot({ path: path.join(TMP, 'shot-options-390.png'), fullPage: true });
    await options.setViewport({ width: 1100, height: 700 });
    await options.screenshot({ path: path.join(TMP, 'shot-options.png') });
  } finally {
    await browser.close();
  }
}

(async () => {
  if (!SERVER) throw new Error('Set GLUT_SERVER to the URL of a scratch Glutspeicher server.');
  await seed();
  fs.mkdirSync(TMP, { recursive: true });
  const site = await serveSite();
  try {
    await runWithoutPermission();
    await runWithPermission();
  } finally {
    site.close();
    // Keep only the screenshots
    for (const name of ['profile-a', 'profile-b', 'extension']) {
      fs.rmSync(path.join(TMP, name), { recursive: true, force: true, maxRetries: 5, retryDelay: 200 });
    }
  }
  console.log(`Screenshots: ${TMP}`);
  console.log(failed ? `${failed} FAILED` : 'ALL PASSED');
  process.exit(failed ? 1 : 0);
})().catch(e => { console.error(e); process.exit(2); });
