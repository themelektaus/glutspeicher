// cd "Glutspeicher Browser Extension" && node --test "test/*.test.js"

const test = require(`node:test`)
const assert = require(`node:assert/strict`)
const GlutTotp = require(`../extension/lib/totp.js`)

// Secret of RFC 6238, appendix B (SHA-1): the ASCII text "12345678901234567890"
const RFC_SECRET = `GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ`

const RFC_VECTORS = [
    [ 59, `94287082` ],
    [ 1111111109, `07081804` ],
    [ 1111111111, `14050471` ],
    [ 1234567890, `89005924` ],
    [ 2000000000, `69279037` ],
    [ 20000000000, `65353130` ]
]

test(`decodeBase32 decodes the RFC secret`, () =>
{
    assert.equal(Buffer.from(GlutTotp.decodeBase32(RFC_SECRET)).toString(`ascii`), `12345678901234567890`)
})

test(`hotp gives the 8-digit codes of RFC 6238 (SHA-1)`, async () =>
{
    const key = GlutTotp.decodeBase32(RFC_SECRET)
    for (const [ seconds, code ] of RFC_VECTORS)
    {
        assert.equal(await GlutTotp.hotp(key, Math.floor(seconds / 30), 8), code, `T = ${seconds}`)
    }
})

test(`generate gives the last 6 digits of the RFC codes and the remaining seconds`, async () =>
{
    for (const [ seconds, code ] of RFC_VECTORS)
    {
        const result = await GlutTotp.generate(RFC_SECRET, seconds * 1000)
        assert.equal(result.code, code.slice(-6), `T = ${seconds}`)
        assert.equal(result.remaining, 30 - seconds % 30, `T = ${seconds}`)
    }

    // Milliseconds are cut, not rounded: 59.9 s is still in the step of 59 s
    assert.equal((await GlutTotp.generate(RFC_SECRET, 59900)).code, `287082`)
    assert.equal((await GlutTotp.generate(RFC_SECRET, 60000)).remaining, 30)
})

test(`Base32 variants give the same code`, async () =>
{
    const expected = (await GlutTotp.generate(RFC_SECRET, 59000)).code
    for (const variant of [
        RFC_SECRET.toLowerCase(),
        `GEZD GNBV GY3T QOJQ GEZD GNBV GY3T QOJQ`,
        ` gezd-gnbv-gy3t-qojq-gezd-gnbv-gy3t-qojq\n`,
        `${RFC_SECRET}====`
    ])
    {
        assert.equal((await GlutTotp.generate(variant, 59000)).code, expected, JSON.stringify(variant))
    }

    // A 16-character secret (80 bits) as many sites use it
    assert.equal(GlutTotp.decodeBase32(`JBSWY3DPEHPK3PXP`).length, 10)
    assert.match((await GlutTotp.generate(`JBSWY3DPEHPK3PXP`)).code, /^\d{6}$/)
})

test(`invalid secrets throw`, async () =>
{
    for (const secret of [ ``, `   `, null, undefined, `====`, `ABC1`, `ABC8`, `ABCÄ`, `A`, `otpauth://totp/x?secret=JBSWY3DPEHPK3PXP` ])
    {
        await assert.rejects(GlutTotp.generate(secret), JSON.stringify(secret))
    }
})

test(`format splits the code like the web frontend`, () =>
{
    assert.equal(GlutTotp.format(`123456`), `123 456`)
})
