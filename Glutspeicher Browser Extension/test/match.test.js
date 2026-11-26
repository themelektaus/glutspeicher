// cd "Glutspeicher Browser Extension" && node --test "test/*.test.js"

const test = require(`node:test`)
const assert = require(`node:assert/strict`)
const GlutMatch = require(`../extension/lib/match.js`)

test(`entryHost reads entries without scheme as https and skips other schemes`, () =>
{
    assert.equal(GlutMatch.entryHost(`example.com`), `example.com`)
    assert.equal(GlutMatch.entryHost(` Example.COM/login `), `example.com`)
    assert.equal(GlutMatch.entryHost(`https://www.example.com:8443/admin`), `www.example.com`)
    assert.equal(GlutMatch.entryHost(`http://192.168.1.10:5580`), `192.168.1.10`)
    assert.equal(GlutMatch.entryHost(`//example.com`), `example.com`)
    assert.equal(GlutMatch.entryHost(`rdp://server.lan`), null)
    assert.equal(GlutMatch.entryHost(`ssh://server.lan:22`), null)
    assert.equal(GlutMatch.entryHost(`ftp://server.lan`), null)
    assert.equal(GlutMatch.entryHost(``), null)
    assert.equal(GlutMatch.entryHost(null), null)
})

test(`pageHost only accepts http(s) pages`, () =>
{
    assert.equal(GlutMatch.pageHost(`https://login.example.com/x?y`), `login.example.com`)
    assert.equal(GlutMatch.pageHost(`brave://newtab/`), null)
    assert.equal(GlutMatch.pageHost(`file:///C:/x.html`), null)
    assert.equal(GlutMatch.pageHost(undefined), null)
})

test(`baseDomain keeps registry second levels like gv.at and co.uk`, () =>
{
    assert.equal(GlutMatch.baseDomain(`login.example.com`), `example.com`)
    assert.equal(GlutMatch.baseDomain(`example.com`), `example.com`)
    assert.equal(GlutMatch.baseDomain(`www.wien.gv.at`), `wien.gv.at`)
    assert.equal(GlutMatch.baseDomain(`shop.bank.co.uk`), `bank.co.uk`)
    assert.equal(GlutMatch.baseDomain(`localhost`), `localhost`)
    assert.equal(GlutMatch.baseDomain(`10.0.0.1`), `10.0.0.1`)
})

test(`rank: same host, then subdomain, then base domain`, () =>
{
    const { RANK_HOST, RANK_SUBDOMAIN, RANK_BASE_DOMAIN, RANK_NONE } = GlutMatch

    assert.equal(GlutMatch.rank(`example.com`, `example.com`), RANK_HOST)
    assert.equal(GlutMatch.rank(`login.example.com`, `example.com`), RANK_SUBDOMAIN)
    assert.equal(GlutMatch.rank(`example.com`, `login.example.com`), RANK_SUBDOMAIN)
    assert.equal(GlutMatch.rank(`login.example.com`, `www.example.com`), RANK_BASE_DOMAIN)
    assert.equal(GlutMatch.rank(`example.com`, `example.org`), RANK_NONE)
    assert.equal(GlutMatch.rank(`notexample.com`, `example.com`), RANK_NONE)
    assert.equal(GlutMatch.rank(`192.168.1.10`, `192.168.1.10`), RANK_HOST)
    assert.equal(GlutMatch.rank(`192.168.1.10`, `192.168.1.11`), RANK_NONE)
    assert.equal(GlutMatch.rank(`localhost`, `localhost`), RANK_HOST)
    assert.equal(GlutMatch.rank(null, `example.com`), RANK_NONE)
    assert.equal(GlutMatch.rank(`example.com`, null), RANK_NONE)
})

test(`rank does not match different owners under a registry or TLD`, () =>
{
    const { RANK_NONE } = GlutMatch

    assert.equal(GlutMatch.rank(`www.wien.gv.at`, `bund.gv.at`), RANK_NONE)
    assert.equal(GlutMatch.rank(`evil.co.uk`, `bank.co.uk`), RANK_NONE)
    assert.equal(GlutMatch.rank(`evil.co.at`, `co.at`), RANK_NONE)
    assert.equal(GlutMatch.rank(`example.at`, `at`), RANK_NONE)
    assert.equal(GlutMatch.rank(`nas.lan`, `router.lan`), RANK_NONE)
})

test(`suggestions are ordered by rank and keep the server order within a rank`, () =>
{
    const items = [
        { name: `A base`, uri: `www.example.com` },
        { name: `B rdp`, uri: `rdp://login.example.com` },
        { name: `C sub`, uri: `example.com` },
        { name: `D other`, uri: `example.org` },
        { name: `E host`, uri: `https://login.example.com/` },
        { name: `F host`, uri: `login.example.com` },
        { name: `G empty`, uri: null }
    ]

    assert.deepEqual(
        GlutMatch.suggestions(items, `login.example.com`).map(x => x.name),
        [ `E host`, `F host`, `C sub`, `A base` ]
    )
    assert.deepEqual(GlutMatch.suggestions(items, null), [])
})

test(`search works like the web frontend`, () =>
{
    const items = [
        { name: `Mail`, username: `alice@example.com`, uri: `mail.example.com`, section: `Private` },
        { name: `Router`, username: `admin`, uri: `http://192.168.1.1`, source: `Home` },
        { name: `Mail work`, username: `bob`, uri: `outlook.office.com`, section: `Work` }
    ]

    assert.deepEqual(GlutMatch.search(items, `mail`).items.map(x => x.name), [ `Mail`, `Mail work` ])
    assert.deepEqual(GlutMatch.search(items, `MAIL  work`).items.map(x => x.name), [ `Mail work` ])
    assert.deepEqual(GlutMatch.search(items, `home admin`).items.map(x => x.name), [ `Router` ])
    assert.deepEqual(GlutMatch.search(items, `private`).items.map(x => x.name), [ `Mail` ])
    assert.equal(GlutMatch.search(items, `nothing`).total, 0)
    assert.equal(GlutMatch.search(items, `  `).total, 0)

    const many = Array.from({ length: 150 }, (_, i) => ({ name: `Bulk ${i}` }))
    const result = GlutMatch.search(many, `bulk`)
    assert.equal(result.items.length, 100)
    assert.equal(result.total, 150)
})

test(`password prefers the generated password`, () =>
{
    assert.equal(GlutMatch.password({ password: `static`, generatedPassword: `generated` }), `generated`)
    assert.equal(GlutMatch.password({ password: `static`, generatedPassword: `` }), `static`)
    assert.equal(GlutMatch.password({ password: `static`, generatedPassword: null }), `static`)
    assert.equal(GlutMatch.password({}), ``)
})

test(`isInsecurePage warns for http outside of the own machine and private networks`, () =>
{
    assert.equal(GlutMatch.isInsecurePage(`http://example.com/login`), true)
    assert.equal(GlutMatch.isInsecurePage(`http://8.8.8.8/`), true)
    assert.equal(GlutMatch.isInsecurePage(`https://example.com/`), false)
    assert.equal(GlutMatch.isInsecurePage(`http://localhost:8080/`), false)
    assert.equal(GlutMatch.isInsecurePage(`http://127.0.0.1:5580/`), false)
    assert.equal(GlutMatch.isInsecurePage(`http://192.168.1.1/`), false)
    assert.equal(GlutMatch.isInsecurePage(`http://10.1.2.3/`), false)
    assert.equal(GlutMatch.isInsecurePage(`http://172.20.0.1/`), false)
    assert.equal(GlutMatch.isInsecurePage(`http://172.32.0.1/`), true)
})
