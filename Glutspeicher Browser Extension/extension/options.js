// Settings: the server URL. Saving asks for access to the server's origin and tests it with GET /api.
// Below it the keyboard shortcut of the popup, as the browser has it set.

const $form = document.querySelector(`form`)
const $url = document.querySelector(`#server-url`)
const $status = document.querySelector(`.status`)

function setStatus(text, className)
{
    $status.textContent = text
    $status.className = `status ${className ?? ``}`
}

async function test(serverUrl)
{
    setStatus(`Connecting…`)
    try
    {
        const data = await GlutServer.get(serverUrl, `api`)
        setStatus(`Connected: ${data?.name ?? `Glutspeicher`} ${data?.version ?? ``}`.trim(), `ok`)
    }
    catch (e)
    {
        setStatus(e.message, `error`)
    }
}

$form.addEventListener(`submit`, e =>
{
    e.preventDefault()

    const serverUrl = GlutServer.normalizeUrl($url.value)
    if (!serverUrl)
    {
        setStatus(`Enter the http:// or https:// address of the server.`, `error`)
        return
    }

    $url.value = serverUrl
    setStatus(`Waiting for access to ${new URL(serverUrl).origin}…`)

    // The permission request has to happen right away in the user gesture, before any await
    GlutServer.requestPermission(serverUrl).then(async granted =>
    {
        if (!granted)
        {
            setStatus(`Without access to ${new URL(serverUrl).origin} the extension cannot load the entries.`, `error`)
            return
        }

        const oldUrl = await GlutServer.getUrl()
        await GlutServer.setUrl(serverUrl)

        // Give up the access to a server that is no longer used
        if (oldUrl && GlutServer.originPattern(oldUrl) != GlutServer.originPattern(serverUrl))
        {
            await chrome.permissions.remove({ origins: [ GlutServer.originPattern(oldUrl) ] }).catch(() => {})
        }

        await test(serverUrl)
    }).catch(error => setStatus(error.message, `error`))
})

async function init()
{
    const serverUrl = await GlutServer.getUrl()
    if (!serverUrl)
    {
        $url.focus()
        return
    }

    $url.value = serverUrl

    if (await GlutServer.hasPermission(serverUrl))
    {
        await test(serverUrl)
    }
    else
    {
        setStatus(`No access to ${new URL(serverUrl).origin} yet. Click Save to allow it.`, `error`)
    }
}

// The shortcut is set by the browser (brave://extensions/shortcuts); extensions can only read it
async function showShortcut()
{
    const commands = await chrome.commands.getAll()
    const shortcut = commands.find(x => x.name == `_execute_action`)?.shortcut
    const $keys = document.querySelector(`.shortcut .keys`)
    $keys.textContent = shortcut || `Not set`
    $keys.classList.toggle(`unset`, !shortcut)
}

document.querySelector(`.shortcut .change`).addEventListener(`click`, () =>
{
    // Brave redirects it to brave://extensions/shortcuts
    chrome.tabs.create({ url: `chrome://extensions/shortcuts` })
})

// After a change on the shortcuts page and coming back to this tab
document.addEventListener(`visibilitychange`, () =>
{
    if (!document.hidden)
    {
        showShortcut()
    }
})

showShortcut()
init()
