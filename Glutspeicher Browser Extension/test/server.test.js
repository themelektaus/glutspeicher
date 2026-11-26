// cd "Glutspeicher Browser Extension" && node --test "test/*.test.js"

const test = require(`node:test`)
const assert = require(`node:assert/strict`)
const fs = require(`node:fs`)
const path = require(`node:path`)
const vm = require(`node:vm`)

// lib/server.js is a plain browser script that declares GlutServer
const context = vm.createContext({ URL })
vm.runInContext(`${fs.readFileSync(path.join(__dirname, `../extension/lib/server.js`), `utf8`)}\nthis.GlutServer = GlutServer`, context)
const { GlutServer } = context

test(`normalizeUrl adds http:// and a trailing slash`, () =>
{
    assert.equal(GlutServer.normalizeUrl(`glut.lan:5580`), `http://glut.lan:5580/`)
    assert.equal(GlutServer.normalizeUrl(`  http://glut.lan:5580  `), `http://glut.lan:5580/`)
    assert.equal(GlutServer.normalizeUrl(`https://pw.example.com/glutspeicher`), `https://pw.example.com/glutspeicher/`)
    assert.equal(GlutServer.normalizeUrl(`https://pw.example.com/glutspeicher/?x=1#y`), `https://pw.example.com/glutspeicher/`)
    assert.equal(GlutServer.normalizeUrl(`HTTP://Glut.LAN`), `http://glut.lan/`)
})

test(`normalizeUrl refuses empty values and other schemes`, () =>
{
    assert.equal(GlutServer.normalizeUrl(``), null)
    assert.equal(GlutServer.normalizeUrl(`   `), null)
    assert.equal(GlutServer.normalizeUrl(undefined), null)
    assert.equal(GlutServer.normalizeUrl(`ftp://glut.lan`), null)
    assert.equal(GlutServer.normalizeUrl(`http://`), null)
})

test(`originPattern is the origin with /*`, () =>
{
    assert.equal(GlutServer.originPattern(`http://glut.lan:5580/`), `http://glut.lan:5580/*`)
    assert.equal(GlutServer.originPattern(`https://pw.example.com/glutspeicher/`), `https://pw.example.com/*`)
})
