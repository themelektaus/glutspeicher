// Popup: suggestions for the active tab, search over all entries, filling.
// The entries (with passwords and TOTP secrets) live only in this page; it is gone when the popup
// closes. TOTP codes are computed here and are never stored either.
// Every value from the server goes into the page with textContent only (never innerHTML).

const $ = selector => document.querySelector(selector)

const $search = $(`.search input`)
const $message = $(`.message`)
const $heading = $(`.heading`)
const $list = $(`.list`)
const $more = $(`.more`)
const $confirm = $(`.confirm`)

const state = {
    serverUrl: null,
    tab: null,
    pageHost: null,
    items: [],
    suggestions: [],
    shown: [],
    selected: -1,
    onConfirm: null,
    totpViews: []
}

function showMessage(text, actionText, action)
{
    $message.hidden = false
    $message.querySelector(`.text`).textContent = text

    const $action = $message.querySelector(`.action`)
    $action.hidden = !actionText
    $action.textContent = actionText ?? ``
    $action.onclick = action ?? null
}

function hideMessage()
{
    $message.hidden = true
}

function toast(text)
{
    const $toast = document.createElement(`div`)
    $toast.className = `toast`
    $toast.textContent = text
    document.body.append($toast)
    setTimeout(() => $toast.remove(), 1500)
}

function hostOf(item)
{
    return GlutMatch.entryHost(item.uri) ?? (item.uri ?? ``).trim()
}

function render()
{
    const text = $search.value.trim()

    if (text)
    {
        const result = GlutMatch.search(state.items, text)
        state.shown = result.items
        $heading.textContent = result.total == 1 ? `1 entry found` : result.total ? `${result.total} entries found` : `No entries found`
        $more.hidden = result.total <= result.items.length
        $more.textContent = `Showing the first ${result.items.length}. Type more to narrow down the search.`
    }
    else
    {
        state.shown = state.suggestions
        $more.hidden = true

        if (!state.pageHost)
        {
            $heading.textContent = `Nothing to fill on this page. Type to search.`
        }
        else if (state.suggestions.length)
        {
            $heading.textContent = `Suggestions for ${state.pageHost}`
        }
        else
        {
            $heading.textContent = `No suggestions for ${state.pageHost}. Type to search.`
        }
    }

    state.totpViews = []
    $list.replaceChildren(...state.shown.map((item, index) => createItem(item, index)))
    select(state.shown.length ? 0 : -1)
    updateTotp()
}

function createItem(item, index)
{
    const $item = document.createElement(`li`)
    $item.id = `item-${index}`
    $item.setAttribute(`role`, `option`)

    const $text = document.createElement(`div`)
    $text.className = `text`

    const $name = document.createElement(`div`)
    $name.className = `name`
    $name.textContent = item.name || `(no name)`

    const $details = document.createElement(`div`)
    $details.className = `details`
    for (const value of [ item.username, hostOf(item) ])
    {
        if (value)
        {
            const $span = document.createElement(`span`)
            $span.textContent = value
            $details.append($span)
        }
    }

    $text.append($name, $details)
    $item.title = [ item.name, item.username, item.uri ].filter(x => x).join(`\n`)

    const password = GlutMatch.password(item)
    $item.append(
        $text,
        ...(item.totp ? [ createTotp(item) ] : []),
        createCopyButton(`user`, `Copy username`, item.username, `Username copied`),
        createCopyButton(`key`, `Copy password`, password, `Password copied`)
    )

    $item.addEventListener(`mousemove`, () =>
    {
        if (state.selected != index)
        {
            select(index, false)
        }
    })

    $item.addEventListener(`click`, () => fill(item))

    return $item
}

function createCopyButton(icon, title, value, doneText)
{
    const $button = document.createElement(`button`)
    $button.className = `icon`
    $button.title = title
    $button.style.backgroundImage = `url(icons/${icon}.svg)`
    $button.disabled = !value
    $button.hidden = !value
    $button.addEventListener(`click`, async e =>
    {
        e.stopPropagation()
        await navigator.clipboard.writeText(value)
        toast(doneText)
    })
    return $button
}

// The current code of an entry, as a copy button with the remaining time as a ring.
// updateTotp() fills it in and runs every second while the popup is open.
function createTotp(item)
{
    const $totp = document.createElement(`span`)
    $totp.className = `totp`

    const $button = document.createElement(`button`)
    $button.className = `code`
    $button.title = `Copy code`
    $button.hidden = true

    const $value = document.createElement(`span`)
    $value.className = `value`

    const $ring = document.createElement(`span`)
    $ring.className = `ring`

    $button.append($value, $ring)

    const $invalid = document.createElement(`span`)
    $invalid.className = `invalid`
    $invalid.textContent = `Invalid TOTP`
    $invalid.hidden = true

    $button.addEventListener(`click`, async e =>
    {
        e.stopPropagation()
        // Computed again: the shown code may just have run out
        const { code } = await GlutTotp.generate(item.totp)
        await navigator.clipboard.writeText(code)
        toast(`Code copied`)
    })

    $totp.append($button, $invalid)
    state.totpViews.push({ item, $button, $value, $ring, $invalid })
    return $totp
}

async function updateTotp()
{
    const now = Date.now()
    for (const view of state.totpViews)
    {
        try
        {
            const { code, remaining } = await GlutTotp.generate(view.item.totp, now)
            view.$value.textContent = GlutTotp.format(code)
            view.$ring.style.setProperty(`--part`, remaining / GlutTotp.PERIOD)
            view.$button.title = `Copy code (valid for ${remaining} s)`
            view.$button.hidden = false
            view.$invalid.hidden = true
        }
        catch
        {
            view.$button.hidden = true
            view.$invalid.hidden = false
        }
    }
}

setInterval(updateTotp, 1000)

function select(index, scroll = true)
{
    state.selected = index

    for (const $item of $list.children)
    {
        $item.setAttribute(`aria-selected`, $item.id == `item-${index}` ? `true` : `false`)
    }

    const $selected = $list.children[index]
    if ($selected)
    {
        $search.setAttribute(`aria-activedescendant`, $selected.id)
        if (scroll)
        {
            $selected.scrollIntoView({ block: `nearest` })
        }
    }
    else
    {
        $search.removeAttribute(`aria-activedescendant`)
    }
}

function showConfirm(text, onConfirm)
{
    state.onConfirm = onConfirm
    $confirm.querySelector(`.text`).textContent = text
    $confirm.hidden = false
    // Cancel has the focus: Enter alone must not fill into a page that does not fit
    $confirm.querySelector(`.cancel`).focus()
}

function hideConfirm()
{
    state.onConfirm = null
    $confirm.hidden = true
    $search.focus()
}

$confirm.querySelector(`.fill`).addEventListener(`click`, () =>
{
    const onConfirm = state.onConfirm
    hideConfirm()
    onConfirm?.()
})

$confirm.querySelector(`.cancel`).addEventListener(`click`, hideConfirm)

function fill(item)
{
    hideMessage()

    if (!state.pageHost)
    {
        showMessage(`This page cannot be filled. Use the copy buttons instead.`)
        return
    }

    const entryHost = GlutMatch.entryHost(item.uri)
    const warnings = []

    // Also for entries that only share the base domain: on "attacker.example.net" the entry
    // of "abc123.example.net" is a suggestion, but may belong to someone else
    if (!GlutMatch.fitsPage(state.pageHost, entryHost))
    {
        const savedFor = entryHost ?? ((item.uri ?? ``).trim() || `no website`)
        warnings.push(`“${item.name || item.username || `This entry`}” is saved for ${savedFor}, but this page is ${state.pageHost}.`)
    }

    if (GlutMatch.isInsecurePage(state.tab.url))
    {
        warnings.push(`This page is not encrypted (HTTP): the credentials can be read on their way to ${state.pageHost}.`)
    }

    if (warnings.length)
    {
        showConfirm(`${warnings.join(`\n\n`)}\n\nFill in the credentials anyway?`, () => fillNow(item, entryHost))
        return
    }

    fillNow(item, entryHost)
}

async function fillNow(item, entryHost)
{
    // The code right before filling, so it is as fresh as possible; an invalid secret fills no code
    const totp = item.totp ? await GlutTotp.generate(item.totp).then(x => x.code, () => ``) : ``

    let results
    try
    {
        results = await chrome.scripting.executeScript({
            target: { tabId: state.tab.id, allFrames: true },
            func: glutspeicherFill,
            args: [ {
                username: item.username ?? ``,
                password: GlutMatch.password(item),
                totp,
                pageOrigin: new URL(state.tab.url).origin,
                entryHost,
                publicSecondLevel: GlutMatch.PUBLIC_SECOND_LEVEL,
                sharedSuffixes: GlutMatch.SHARED_SUFFIXES
            } ]
        })
    }
    catch (e)
    {
        showMessage(`This page cannot be filled: ${e.message}`)
        return
    }

    if (results.some(x => x.result?.username || x.result?.password || x.result?.totp))
    {
        window.close()
        return
    }

    if (results.some(x => x.result?.skipped))
    {
        showMessage(`No login fields found. A frame of another site was left out because it does not fit the entry.`)
        return
    }

    const foreignFrames = results.find(x => x.frameId == 0)?.result?.foreignFrames ?? []
    if (foreignFrames.length)
    {
        showMessage(`No login fields found on this page. Frames of other sites (${foreignFrames.join(`, `)}) cannot be filled; use the copy buttons instead.`)
        return
    }

    showMessage(`No login fields found on this page.`)
}

async function load()
{
    hideMessage()
    $heading.textContent = `Loading entries…`
    $list.replaceChildren()

    try
    {
        state.items = await GlutServer.get(state.serverUrl, `api/passwords`) ?? []
    }
    catch (e)
    {
        state.items = []
        $heading.textContent = ``
        showMessage(e.message, `Try again`, load)
        return
    }

    state.suggestions = GlutMatch.suggestions(state.items, state.pageHost)
    render()
}

async function init()
{
    [ state.tab ] = await chrome.tabs.query({ active: true, currentWindow: true })
    state.pageHost = GlutMatch.pageHost(state.tab?.url)
    state.serverUrl = await GlutServer.getUrl()

    if (!state.serverUrl)
    {
        showMessage(`No Glutspeicher server is set yet.`, `Open settings`, openSettings)
        return
    }

    if (!await GlutServer.hasPermission(state.serverUrl))
    {
        showMessage(`The extension has no access to ${new URL(state.serverUrl).origin} yet.`, `Allow access`, async () =>
        {
            // First call in the click handler: the browser asks only during a user gesture
            if (await GlutServer.requestPermission(state.serverUrl))
            {
                await load()
            }
        })
        return
    }

    await load()
}

function openSettings()
{
    chrome.runtime.openOptionsPage()
    window.close()
}

$(`header .settings`).addEventListener(`click`, openSettings)
$(`header .reload`).addEventListener(`click`, load)

$search.addEventListener(`input`, render)

document.addEventListener(`keydown`, e =>
{
    if (!$confirm.hidden)
    {
        if (e.key == `Escape`)
        {
            e.preventDefault()
            hideConfirm()
        }
        return
    }

    switch (e.key)
    {
        case `ArrowDown`:
            e.preventDefault()
            if (state.shown.length)
            {
                select(Math.min(state.selected + 1, state.shown.length - 1))
            }
            break

        case `ArrowUp`:
            e.preventDefault()
            if (state.shown.length)
            {
                select(Math.max(state.selected - 1, 0))
            }
            break

        case `Enter`:
            if (document.activeElement === $search && state.shown[state.selected])
            {
                e.preventDefault()
                fill(state.shown[state.selected])
            }
            break

        case `Escape`:
            e.preventDefault()
            if ($search.value)
            {
                $search.value = ``
                render()
            }
            else
            {
                window.close()
            }
            break
    }
})

init()
