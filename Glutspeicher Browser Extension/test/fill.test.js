// cd "Glutspeicher Browser Extension" && node --test "test/*.test.js"
// The frame rule of lib/fill.js (filling itself is tested in test/e2e). fill.js runs in a
// frame of another origin than the page, with no inputs in it.

const test = require(`node:test`)
const assert = require(`node:assert/strict`)
const fs = require(`node:fs`)
const path = require(`node:path`)
const vm = require(`node:vm`)
const GlutMatch = require(`../extension/lib/match.js`)

const source = fs.readFileSync(path.join(__dirname, `../extension/lib/fill.js`), `utf8`)

function fillInFrame(frameHost, entryHost)
{
    const window = { top: {} }
    const context = vm.createContext({
        window,
        location: { hostname: frameHost, origin: `https://${frameHost}`, href: `https://${frameHost}/` },
        document: { querySelectorAll: () => [] }
    })
    vm.runInContext(source, context)
    return context.glutspeicherFill({
        username: `u`,
        password: `p`,
        pageOrigin: `https://page.test`,
        entryHost,
        publicSecondLevel: GlutMatch.PUBLIC_SECOND_LEVEL,
        sharedSuffixes: GlutMatch.SHARED_SUFFIXES
    })
}

test(`a frame of another origin is only used if it fits the entry like GlutMatch.fitsPage`, () =>
{
    const cases = [
        [ `login.example.com`, `login.example.com` ],
        [ `login.example.com`, `example.com` ],
        [ `example.com`, `login.example.com` ],
        [ `www.example.com`, `login.example.com` ],
        [ `evil.co.at`, `co.at` ],
        [ `evil.wien.gv.at`, `wien.gv.at` ],
        [ `attacker.myfritz.net`, `abc123.myfritz.net` ],
        [ `abc123.myfritz.net`, `myfritz.net` ],
        [ `nas.abc123.myfritz.net`, `abc123.myfritz.net` ],
        [ `example.org`, `example.com` ],
        [ `10.0.0.1`, `10.0.0.1` ],
        [ `1.10.0.0.1`, `10.0.0.1` ],
        [ `example.com`, null ]
    ]

    for (const [ frameHost, entryHost ] of cases)
    {
        const result = fillInFrame(frameHost, entryHost)
        assert.equal(result.skipped, !GlutMatch.fitsPage(frameHost, entryHost), `${frameHost} / ${entryHost}`)
    }

    assert.equal(fillInFrame(`evil.co.at`, `co.at`).skipped, true)
    assert.equal(fillInFrame(`login.example.com`, `example.com`).skipped, false)
})
