// cd "Glutspeicher Browser Extension" && node --test "test/*.test.js"
// The frame rule of lib/fill.js, and which fields it picks for the one-time code (on a small
// fake DOM; filling in a real browser is tested in test/e2e).

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

// Fake inputs: visible unless hidden, in document order, optionally in a form
class FakeInput
{
    constructor(attributes, index)
    {
        this.attributes = attributes
        this.index = index
        this.type = attributes.type ?? `text`
        this.name = attributes.name ?? ``
        this.id = attributes.id ?? ``
        this.form = attributes.form ?? null
        this.disabled = false
        this.readOnly = false
        this.shadowRoot = null
        this._value = ``
    }

    get value() { return this._value }
    set value(value) { this._value = value }

    getAttribute(name) { return this.attributes[name] ?? null }
    getBoundingClientRect() { return this.attributes.hidden ? { width: 0, height: 0 } : { width: 100, height: 20 } }
    checkVisibility() { return !this.attributes.hidden }
    compareDocumentPosition(other) { return other.index > this.index ? 4 : 2 }
    focus() {}
    dispatchEvent() {}
}

function fillPage(fields, totp = `123456`)
{
    const inputs = fields.map((x, i) => new FakeInput(x, i))
    const window = {}
    window.top = window
    const context = vm.createContext({
        window,
        location: { hostname: `page.test`, origin: `https://page.test`, href: `https://page.test/` },
        document: { querySelectorAll: () => inputs },
        HTMLInputElement: FakeInput,
        HTMLIFrameElement: class {},
        HTMLFrameElement: class {},
        Node: { DOCUMENT_POSITION_FOLLOWING: 4 },
        InputEvent: class {},
        Event: class {}
    })
    vm.runInContext(source, context)
    const result = context.glutspeicherFill({
        username: `alice`,
        password: `secret`,
        totp,
        pageOrigin: `https://page.test`,
        entryHost: `page.test`,
        publicSecondLevel: GlutMatch.PUBLIC_SECOND_LEVEL,
        sharedSuffixes: GlutMatch.SHARED_SUFFIXES
    })
    const values = Object.fromEntries(inputs.map(x => [ x.name, x.value ]))
    return { result, values }
}

test(`login and one-time-code field on the same page: all three are filled`, () =>
{
    const { result, values } = fillPage([
        { name: `q`, type: `search` },
        { name: `user` },
        { name: `pass`, type: `password` },
        { name: `otp`, autocomplete: `one-time-code` }
    ])
    assert.deepEqual(values, { q: ``, user: `alice`, pass: `secret`, otp: `123456` })
    assert.equal(result.totp, true)
})

test(`a page with only the code field (step 2) gets only the code`, () =>
{
    for (const field of [
        { name: `challenge`, autocomplete: `one-time-code`, type: `tel` },
        { name: `totp`, type: `number` },
        { name: `mfa_code` },
        { name: `x`, id: `login-otp` }
    ])
    {
        const { result, values } = fillPage([ { name: `q`, type: `search` }, field ])
        assert.equal(values[field.name], `123456`, JSON.stringify(field))
        assert.equal(values.q, ``)
        assert.deepEqual([ result.username, result.password, result.totp ], [ false, false, true ], JSON.stringify(field))
    }
})

test(`a field named code next to the login is the code field`, () =>
{
    const { values } = fillPage([
        { name: `login` },
        { name: `password`, type: `password` },
        { name: `code` }
    ])
    assert.deepEqual(values, { login: `alice`, password: `secret`, code: `123456` })
})

test(`a code field between username and password is not taken as the username`, () =>
{
    let { result, values } = fillPage([
        { name: `user` },
        { name: `otp` },
        { name: `pass`, type: `password` }
    ])
    assert.deepEqual(values, { user: `alice`, otp: `123456`, pass: `secret` })
    assert.deepEqual([ result.username, result.password, result.totp ], [ true, true, true ])

    // A field marked as username stays the username field, whatever its name
    ;({ values } = fillPage([ { name: `otp_user`, autocomplete: `username` }, { name: `pass`, type: `password` } ]))
    assert.equal(values.otp_user, `alice`)
})

test(`a field named loginCode alone (step 2) gets the code, not the username`, () =>
{
    const { result, values } = fillPage([ { name: `loginCode` } ])
    assert.deepEqual([ result.username, result.totp, values.loginCode ], [ false, true, `123456` ])
})

test(`no code is filled without a fitting field, with several candidates or without a code`, () =>
{
    // Search field, newsletter and a hidden code field
    let { result, values } = fillPage([
        { name: `search`, type: `search` },
        { name: `newsletter`, type: `email` },
        { name: `otp`, hidden: true },
        { name: `user` },
        { name: `pass`, type: `password` }
    ])
    assert.equal(result.totp, false)
    assert.equal(values.search, ``)
    assert.equal(values.otp, ``)

    // Fields that only sound like a code
    ;({ result } = fillPage([ { name: `zipcode` }, { name: `promo_code` }, { name: `token_search`, type: `search` } ]))
    assert.equal(result.totp, false)

    // Two unmarked candidates, and one field per digit
    ;({ result } = fillPage([ { name: `sms_code` }, { name: `backup_code` } ]))
    assert.equal(result.totp, false)
    ;({ result } = fillPage(Array.from({ length: 6 }, (_, i) => ({ name: `digit${i}`, autocomplete: `one-time-code`, maxlength: `1` }))))
    assert.equal(result.totp, false)

    // A marked field wins over named ones
    ;({ result, values } = fillPage([ { name: `sms_code` }, { name: `app`, autocomplete: `one-time-code` } ]))
    assert.deepEqual([ result.totp, values.app, values.sms_code ], [ true, `123456`, `` ])

    // The username field is never the code field
    ;({ result, values } = fillPage([ { name: `user_token`, autocomplete: `username` } ]))
    assert.deepEqual([ result.username, result.totp, values.user_token ], [ true, false, `alice` ])

    // Invitation, discount and voucher codes in English and German
    for (const name of [ `invitation_code`, `discount_code`, `gutscheincode`, `rabattcode` ])
    {
        ;({ result, values } = fillPage([ { name } ]))
        assert.deepEqual([ result.totp, values[name] ], [ false, `` ], name)
    }

    // A named field whose autocomplete names another field (card security code, postal code)
    for (const field of [ { name: `securityCode`, autocomplete: `cc-csc` }, { name: `shipping_code`, autocomplete: `shipping postal-code` } ])
    {
        ;({ result, values } = fillPage([ field ]))
        assert.deepEqual([ result.totp, values[field.name] ], [ false, `` ], field.name)
    }
    ;({ result } = fillPage([ { name: `otp_code`, autocomplete: `off` } ]))
    assert.equal(result.totp, true)

    // Entry without TOTP
    ;({ result, values } = fillPage([ { name: `otp`, autocomplete: `one-time-code` } ], ``))
    assert.deepEqual([ result.totp, values.otp ], [ false, `` ])
})
