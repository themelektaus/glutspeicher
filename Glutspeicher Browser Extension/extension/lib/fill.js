// Fills a login form. Runs in every frame of the tab through chrome.scripting.executeScript,
// so it has to stay self-contained (no access to anything outside of the function).
//
// data: { username, password, pageOrigin, entryHost }
// Returns { username: bool, password: bool, skipped: bool, foreignFrames: [host] } for the frame.
// foreignFrames (top frame only): hosts of frames of other origins; activeTab does not reach
// them, so the script does not run there at all.

function glutspeicherFill(data)
{
    const result = { username: false, password: false, skipped: false, foreignFrames: [] }

    // A frame of another origin than the page only gets the credentials if it is the entry's site
    if (window !== window.top && location.origin !== data.pageOrigin)
    {
        const host = location.hostname.toLowerCase()
        const entryHost = (data.entryHost || ``).toLowerCase()
        if (!entryHost || (host !== entryHost && !host.endsWith(`.${entryHost}`) && !entryHost.endsWith(`.${host}`)))
        {
            result.skipped = true
            return result
        }
    }

    // All inputs, also those in open shadow roots, in document order
    const inputs = []
    const frames = []
    const collect = root =>
    {
        for (const element of root.querySelectorAll(`*`))
        {
            if (element instanceof HTMLInputElement)
            {
                inputs.push(element)
            }
            else if (element instanceof HTMLIFrameElement || element instanceof HTMLFrameElement)
            {
                frames.push(element)
            }
            if (element.shadowRoot)
            {
                collect(element.shadowRoot)
            }
        }
    }
    collect(document)

    if (window === window.top)
    {
        for (const frame of frames)
        {
            try
            {
                const url = new URL(frame.src, location.href)
                if (/^https?:$/.test(url.protocol) && url.origin !== location.origin && !result.foreignFrames.includes(url.hostname))
                {
                    result.foreignFrames.push(url.hostname)
                }
            }
            catch
            {
            }
        }
    }

    const isUsable = element =>
    {
        if (element.disabled || element.readOnly)
        {
            return false
        }
        const rect = element.getBoundingClientRect()
        if (rect.width < 2 || rect.height < 2)
        {
            return false
        }
        return element.checkVisibility({ visibilityProperty: true, opacityProperty: false })
    }

    const usable = inputs.filter(isUsable)
    const autocomplete = element => (element.getAttribute(`autocomplete`) || ``).toLowerCase().split(/\s+/)

    // Password field: the one marked as current password, else the first one that is no new password
    const passwordFields = usable.filter(x => x.type === `password`)
    const passwordField =
        passwordFields.find(x => autocomplete(x).includes(`current-password`)) ??
        passwordFields.find(x => !autocomplete(x).includes(`new-password`)) ??
        passwordFields[0] ??
        null

    const isTextField = x =>
        [ `text`, `email`, `tel` ].includes(x.type) &&
        !autocomplete(x).includes(`one-time-code`)

    const looksLikeUsername = x =>
        autocomplete(x).some(a => a === `username` || a === `email`) ||
        x.type === `email` ||
        /user|login|e-?mail|account|ident|benutzer|anmelde/i.test(`${x.name} ${x.id}`)

    let usernameField = null

    if (passwordField)
    {
        // The last text field before the password field, preferably in the same form;
        // one marked as username wins
        const before = usable.filter(x =>
            isTextField(x) &&
            (x.compareDocumentPosition(passwordField) & Node.DOCUMENT_POSITION_FOLLOWING)
        )
        const sameForm = passwordField.form ? before.filter(x => x.form === passwordField.form) : []
        const candidates = sameForm.length ? sameForm : before
        usernameField =
            candidates.findLast(x => autocomplete(x).some(a => a === `username` || a === `email`)) ??
            candidates.at(-1) ??
            null
    }
    else
    {
        // First step of a two-step login: only a field that clearly asks for the username
        usernameField = usable.find(x => isTextField(x) && looksLikeUsername(x)) ?? null
    }

    // Set the value like typing does, so React, Vue and Angular see it
    const setValue = (element, value) =>
    {
        element.focus()
        const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, `value`).set
        setter.call(element, value)
        element.dispatchEvent(new InputEvent(`input`, { bubbles: true, composed: true, inputType: `insertText`, data: value }))
        element.dispatchEvent(new Event(`change`, { bubbles: true }))
    }

    if (usernameField && data.username)
    {
        setValue(usernameField, data.username)
        result.username = true
    }

    if (passwordField && data.password)
    {
        setValue(passwordField, data.password)
        result.password = true
    }

    return result
}
