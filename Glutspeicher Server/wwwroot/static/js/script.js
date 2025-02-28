// include app.js
// include component.js
// include data.js

// include dialog.js
// include extensions.js
// include foldout.js
// include link.js
// include page.js
// include totp.js

// include ../page/*.js

on(`load`, async () =>
{
    App.updateBody()
    
    await App.updateComponents(Page, Link)
    
    query(`#app`).setClass(`loaded`, true)
    
    const pageName = Data.load().page
    const links = App.getInstances(Link).filter(x => !x.hidden)
    const link = links.find(x => x.target == pageName || x.alternative == pageName) ?? links[0]
    
    Page.getByName(link.target).activate({ delay: 200 })
    
    App.listen()
    
    queryAll(`[SERVER_VERSION]`).forEach($ => $.setInnerHtml(SERVER_VERSION))
    queryAll(`[AGENT_VERSION]`).forEach($ => $.setInnerHtml(AGENT_VERSION))
})

function renderCircle($canvas, v, color1, color2)
{
    const ctx = $canvas.getContext('2d')
    ctx.clearRect(0, 0, $canvas.width, $canvas.height)
    
    const r = $canvas.width / 2
    const l = 2
    const p = Math.PI * 2

    ctx.beginPath()
    ctx.arc(r, r, r - l, 0, p)
    ctx.lineWidth = 4
    ctx.strokeStyle = color1
    ctx.stroke()

    ctx.beginPath()
    ctx.arc(r, r, r - l, p * (1 - v), p)
    ctx.lineWidth = 2
    ctx.strokeStyle = color2
    ctx.stroke()
}

async function writeTextToClipboard(text)
{
    await navigator.clipboard.writeText(text)
}

async function playButtonAnimation($)
{
    await new Promise(async resolve =>
    {
        $.setStyle(`transition`, `scale .1s`)
        await delay(1)
        $.setStyle(`scale`, .95)
        await delay(100)
        $.setStyle(`transition`, `scale .3s`)
        await delay(1)
        $.setStyle(`scale`, null)
        await delay(300)
        $.setStyle(`transition`, null)
        resolve()
    })
}
