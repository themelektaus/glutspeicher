// Settings and access to the Glutspeicher server.
// Only the server URL is stored (chrome.storage.local); passwords are never stored,
// the popup keeps them in memory while it is open.

const GlutServer = {

    // "glut.lan:5580" -> "http://glut.lan:5580/", keeps a path (server behind a proxy),
    // drops query and hash. Returns null for anything that is not http(s).
    normalizeUrl(text)
    {
        text = (text ?? ``).trim()
        if (!text)
        {
            return null
        }

        if (!/^[a-z][a-z0-9+.-]*:\/\//i.test(text))
        {
            text = `http://${text.replace(/^\/+/, ``)}`
        }

        let url
        try
        {
            url = new URL(text)
        }
        catch
        {
            return null
        }

        if (url.protocol != `http:` && url.protocol != `https:`)
        {
            return null
        }

        let path = url.pathname
        if (!path.endsWith(`/`))
        {
            path += `/`
        }

        return `${url.origin}${path}`
    },

    // Match pattern for chrome.permissions ("http://glut.lan:5580/*")
    originPattern(serverUrl)
    {
        return `${new URL(serverUrl).origin}/*`
    },

    async getUrl()
    {
        const { serverUrl } = await chrome.storage.local.get(`serverUrl`)
        return serverUrl || null
    },

    async setUrl(serverUrl)
    {
        await chrome.storage.local.set({ serverUrl })
    },

    async hasPermission(serverUrl)
    {
        return await chrome.permissions.contains({ origins: [ this.originPattern(serverUrl) ] })
    },

    // Must be called directly in a click handler (user gesture), before any await
    requestPermission(serverUrl)
    {
        return chrome.permissions.request({ origins: [ this.originPattern(serverUrl) ] })
    },

    // GET <server>/<path>, returns `data` of the ApiResult or throws an Error
    async get(serverUrl, path)
    {
        let response
        try
        {
            response = await fetch(new URL(path, serverUrl), {
                // The server has no login yet (#1); with one, its cookie goes along
                credentials: `include`,
                cache: `no-store`,
                headers: { Accept: `application/json` }
            })
        }
        catch
        {
            throw new Error(`Cannot reach the server at ${serverUrl}`)
        }

        if (!response.ok)
        {
            throw new Error(`The server answered with HTTP ${response.status}`)
        }

        let result
        try
        {
            result = await response.json()
        }
        catch
        {
            throw new Error(`The server did not answer with JSON. Is this a Glutspeicher server?`)
        }

        if (!result?.success)
        {
            throw new Error(result?.errorMessage || `The server reported an error`)
        }

        return result.data
    }
}
