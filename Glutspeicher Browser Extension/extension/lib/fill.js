// Fills a login form and a one-time code. Runs in every frame of the tab through chrome.scripting.executeScript,
// so it has to stay self-contained (no access to anything outside of the function).
//
// data: { username, password, totp, pageOrigin, entryHost, publicSecondLevel, sharedSuffixes }
// (totp is the current code or empty; the two lists are GlutMatch.PUBLIC_SECOND_LEVEL and
// GlutMatch.SHARED_SUFFIXES)
// Returns { username: bool, password: bool, totp: bool, skipped: bool, foreignFrames: [host] } for the frame.
// foreignFrames (top frame only): hosts of frames of other origins; activeTab does not reach
// them, so the script does not run there at all.

function glutspeicherFill(data)
{
    const result = { username: false, password: false, totp: false, skipped: false, foreignFrames: [] }

    // Copies of GlutMatch.isIp and GlutMatch.baseDomain in lib/match.js: change both
    const isIp = host => /^\d{1,3}(\.\d{1,3}){3}$/.test(host) || host.startsWith(`[`)
    const baseDomain = host =>
    {
        if (isIp(host))
        {
            return host
        }

        const labels = host.split(`.`)
        let count = 2

        const shared = (data.sharedSuffixes || []).find(x => host.endsWith(`.${x}`))
        if (shared)
        {
            count = shared.split(`.`).length + 1
        }
        else if (labels.length >= 3 && labels.at(-1).length === 2 && (data.publicSecondLevel || []).includes(labels.at(-2)))
        {
            count = 3
        }

        return labels.slice(-count).join(`.`)
    }

    // A frame of another origin than the page only gets the credentials if it is the entry's site:
    // the same host, or one is a subdomain of the other with the same base domain
    // (the rule of GlutMatch.fitsPage, without asking, since the popup only asked about the page)
    if (window !== window.top && location.origin !== data.pageOrigin)
    {
        const host = location.hostname.toLowerCase().replace(/\.$/, ``)
        const entryHost = (data.entryHost || ``).toLowerCase()
        const fits =
            entryHost !== `` &&
            (host === entryHost ||
                (!isIp(host) && !isIp(entryHost) &&
                    (host.endsWith(`.${entryHost}`) || entryHost.endsWith(`.${host}`)) &&
                    baseDomain(host) === baseDomain(entryHost)))
        if (!fits)
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

    const isOneTimeCode = x => autocomplete(x).includes(`one-time-code`)
    const looksLikeOneTimeCode = x => /otp|2fa|mfa|one.?time/i.test(`${x.name} ${x.id}`)

    const isMarkedUsername = x => autocomplete(x).some(a => a === `username` || a === `email`)

    // A name like "loginCode" or "login-otp" asks for a code, not for the username
    const looksLikeUsername = x =>
        isMarkedUsername(x) ||
        x.type === `email` ||
        (/user|login|e-?mail|account|ident|benutzer|anmelde/i.test(`${x.name} ${x.id}`) &&
            !looksLikeOneTimeCode(x) && !/code/i.test(`${x.name} ${x.id}`))

    let usernameField = null

    if (passwordField)
    {
        // The last text field before the password field, preferably in the same form;
        // one marked as username wins; one named like a one-time code (otp, 2fa) is left out
        const before = usable.filter(x =>
            isTextField(x) &&
            (isMarkedUsername(x) || !looksLikeOneTimeCode(x)) &&
            (x.compareDocumentPosition(passwordField) & Node.DOCUMENT_POSITION_FOLLOWING)
        )
        const sameForm = passwordField.form ? before.filter(x => x.form === passwordField.form) : []
        const candidates = sameForm.length ? sameForm : before
        usernameField =
            candidates.findLast(isMarkedUsername) ??
            candidates.at(-1) ??
            null
    }
    else
    {
        // First step of a two-step login: only a field that clearly asks for the username
        usernameField = usable.find(x => isTextField(x) && looksLikeUsername(x)) ?? null
    }

    // One-time code field: the one marked as one-time-code, else the only visible text or number
    // field that is named like one, is neither the username nor the password field and whose
    // autocomplete names no other field (cc-csc, postal-code).
    // Several candidates (also one field per digit) fill nothing: the copy button helps there.
    let totpField = null

    if (data.totp)
    {
        const isCodeField = x => [ `text`, `number`, `tel` ].includes(x.type)
        const marked = usable.filter(x => isCodeField(x) && isOneTimeCode(x))
        const named = usable.filter(x =>
            isCodeField(x) &&
            x !== usernameField &&
            !looksLikeUsername(x) &&
            autocomplete(x).every(a => [ ``, `on`, `off` ].includes(a)) &&
            /otp|2fa|mfa|one.?time|verification|code|token/i.test(`${x.name} ${x.id}`) &&
            !/zip|post|promo|coupon|voucher|gift|discount|rabatt|gutschein|country|area|captcha|invit|referr/i.test(`${x.name} ${x.id}`)
        )
        const candidates = marked.length ? marked : named
        totpField = candidates.length === 1 ? candidates[0] : null
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

    if (totpField)
    {
        setValue(totpField, data.totp)
        result.totp = true
    }

    return result
}
