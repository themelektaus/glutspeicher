// Matching of password entries to the page in the active tab, and the search.
// No browser APIs here, so test/match.test.js can run it in Node.

const GlutMatch = {

    RANK_NONE: 0,
    RANK_BASE_DOMAIN: 1,
    RANK_SUBDOMAIN: 2,
    RANK_HOST: 3,

    // Second-level labels under a country TLD that are registries, not owners
    // ("wien.gv.at" is the base domain, not "gv.at")
    PUBLIC_SECOND_LEVEL: [ `ac`, `co`, `com`, `edu`, `go`, `gob`, `gouv`, `gov`, `gv`, `ne`, `net`, `or`, `org` ],

    // Host of a web entry, or null for entries that are no web pages (rdp://, ssh://, ...).
    // An entry without scheme is read as https:// (like Api.Passwords.Get on the server).
    entryHost(uri)
    {
        uri = (uri ?? ``).trim()
        if (!uri)
        {
            return null
        }

        if (uri.startsWith(`//`))
        {
            uri = `https:${uri}`
        }
        else if (!uri.includes(`://`))
        {
            uri = `https://${uri}`
        }

        if (!/^https?:\/\//i.test(uri))
        {
            return null
        }

        return this.urlHost(uri)
    },

    // Host of a page URL, or null if it is no http(s) page (brave://, file://, ...)
    pageHost(url)
    {
        if (!/^https?:\/\//i.test(url ?? ``))
        {
            return null
        }

        return this.urlHost(url)
    },

    urlHost(url)
    {
        try
        {
            const host = new URL(url).hostname.toLowerCase().replace(/\.$/, ``)
            return host || null
        }
        catch
        {
            return null
        }
    },

    isIp(host)
    {
        return /^\d{1,3}(\.\d{1,3}){3}$/.test(host) || host.startsWith(`[`)
    },

    // "login.example.com" -> "example.com", "www.wien.gv.at" -> "wien.gv.at";
    // IP addresses and single labels stay as they are
    baseDomain(host)
    {
        if (this.isIp(host))
        {
            return host
        }

        const labels = host.split(`.`)
        let count = 2

        if (labels.length >= 3 && labels.at(-1).length == 2 && this.PUBLIC_SECOND_LEVEL.includes(labels.at(-2)))
        {
            count = 3
        }

        return labels.slice(-count).join(`.`)
    },

    // How well an entry host fits the page host: RANK_HOST (same host),
    // RANK_SUBDOMAIN (one is a subdomain of the other), RANK_BASE_DOMAIN, RANK_NONE
    rank(pageHost, entryHost)
    {
        if (!pageHost || !entryHost)
        {
            return this.RANK_NONE
        }

        if (pageHost == entryHost)
        {
            return this.RANK_HOST
        }

        if (this.isIp(pageHost) || this.isIp(entryHost))
        {
            return this.RANK_NONE
        }

        if (pageHost.endsWith(`.${entryHost}`) || entryHost.endsWith(`.${pageHost}`))
        {
            // "co.at" as entry must not match every "*.co.at"
            if (this.baseDomain(pageHost) == this.baseDomain(entryHost))
            {
                return this.RANK_SUBDOMAIN
            }
            return this.RANK_NONE
        }

        const base = this.baseDomain(pageHost)
        if (base.includes(`.`) && base == this.baseDomain(entryHost))
        {
            return this.RANK_BASE_DOMAIN
        }

        return this.RANK_NONE
    },

    // Entries that fit the page, best first; within a rank in the order of the server
    // (name, username, uri)
    suggestions(items, pageHost)
    {
        if (!pageHost)
        {
            return []
        }

        return items
            .map((item, index) => ({ item, index, rank: this.rank(pageHost, this.entryHost(item.uri)) }))
            .filter(x => x.rank > this.RANK_NONE)
            .sort((a, b) => b.rank - a.rank || a.index - b.index)
            .map(x => x.item)
    },

    // Like the search of the web frontend: the text is split at spaces, every term
    // must occur in name, username, uri, source or section (case-insensitive)
    search(items, text, limit = 100)
    {
        const terms = (text ?? ``).toLowerCase().split(` `).filter(x => x)
        if (terms.length == 0)
        {
            return { items: [], total: 0 }
        }

        const found = items.filter(item =>
        {
            const itemText = [ item.name, item.username, item.uri, item.source, item.section ]
                .filter(x => x)
                .join(` `)
                .toLowerCase()

            return terms.every(x => itemText.includes(x))
        })

        return { items: found.slice(0, limit), total: found.length }
    },

    // The password to fill: the generated one if there is one, like the web frontend
    password(item)
    {
        return item.generatedPassword || item.password || ``
    },

    // Pages without TLS outside of the own machine and private networks
    isInsecurePage(url)
    {
        if (!/^http:\/\//i.test(url ?? ``))
        {
            return false
        }

        const host = this.urlHost(url) ?? ``

        if (host == `localhost` || host.endsWith(`.localhost`) || host == `[::1]`)
        {
            return false
        }

        const ip = host.match(/^(\d+)\.(\d+)\.(\d+)\.(\d+)$/)
        if (ip)
        {
            const [ a, b ] = [ +ip[1], +ip[2] ]
            const isPrivate = a == 10 || a == 127 || (a == 172 && b >= 16 && b <= 31) || (a == 192 && b == 168) || (a == 169 && b == 254)
            return !isPrivate
        }

        return true
    }
}

if (typeof module == `object`)
{
    module.exports = GlutMatch
}
