// TOTP codes (RFC 6238) like jsOTP in the web frontend: Base32 secret, HMAC-SHA1, 30 s, 6 digits.
// No browser APIs besides crypto.subtle, so test/totp.test.js can run it in Node.

const GlutTotp = {

    PERIOD: 30,
    DIGITS: 6,

    // Base32 (RFC 4648) to bytes. Spaces, dashes, lower case and "=" padding are allowed;
    // left over bits at the end are dropped. Throws on anything else.
    decodeBase32(secret)
    {
        const alphabet = `ABCDEFGHIJKLMNOPQRSTUVWXYZ234567`
        const text = String(secret ?? ``).replace(/[\s-]/g, ``).replace(/=+$/, ``).toUpperCase()
        if (!text)
        {
            throw new Error(`Empty TOTP secret`)
        }

        const bytes = []
        let buffer = 0
        let bits = 0
        for (const char of text)
        {
            const value = alphabet.indexOf(char)
            if (value < 0)
            {
                throw new Error(`Invalid TOTP secret`)
            }
            buffer = (buffer << 5 | value) & 0xffff
            bits += 5
            if (bits >= 8)
            {
                bits -= 8
                bytes.push(buffer >> bits & 0xff)
            }
        }

        if (!bytes.length)
        {
            throw new Error(`Invalid TOTP secret`)
        }
        return new Uint8Array(bytes)
    },

    // HOTP (RFC 4226) of a key (bytes) and a counter
    async hotp(key, counter, digits = GlutTotp.DIGITS)
    {
        const message = new Uint8Array(8)
        let value = counter
        for (let i = 7; i >= 0; i--)
        {
            message[i] = value % 256
            value = Math.floor(value / 256)
        }

        const cryptoKey = await crypto.subtle.importKey(`raw`, key, { name: `HMAC`, hash: `SHA-1` }, false, [ `sign` ])
        const hmac = new Uint8Array(await crypto.subtle.sign(`HMAC`, cryptoKey, message))

        const offset = hmac[hmac.length - 1] & 0x0f
        const binary =
            (hmac[offset] & 0x7f) << 24 |
            hmac[offset + 1] << 16 |
            hmac[offset + 2] << 8 |
            hmac[offset + 3]

        return String(binary % 10 ** digits).padStart(digits, `0`)
    },

    // The code of a Base32 secret at a time (ms) and the seconds it is still valid
    async generate(secret, time = Date.now())
    {
        const key = GlutTotp.decodeBase32(secret)
        const seconds = Math.floor(time / 1000)
        const code = await GlutTotp.hotp(key, Math.floor(seconds / GlutTotp.PERIOD))
        return { code, remaining: GlutTotp.PERIOD - seconds % GlutTotp.PERIOD }
    },

    // "123456" → "123 456", as in the web frontend
    format(code)
    {
        return `${code.slice(0, 3)} ${code.slice(3)}`
    }
}

if (typeof module == `object`)
{
    module.exports = GlutTotp
}
