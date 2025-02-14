class Totp extends Component
{
    static _ = App.use({
        type: this,
        selector: `x-totp`
    })
    
    init()
    {
        this.$value = this.$.create(`span`)
        this.$canvas = this.$.create(`canvas`)
            .setAttr(`width`, `20`)
            .setAttr(`height`, `20`)
        
        this.$copy = this.$.create(`button`).setClass(`copy`)
        this.$copy.addEventListener(`click`, async () =>
        {
            const text = this.$value.innerText.replace(` `, ``).trim()
            if (text)
            {
                await writeTextToClipboard(text)
            }
        })
        
        if (Totp.isRendering)
        {
            return
        }
        
        Totp.isRendering = true
        
        new Promise(async () =>
        {
            while (true)
            {
                for (const instance of App.getInstances(Totp))
                {
                    const value = instance.$.getData(`value`)
                    try
                    {
                        const totp = _jsOTP.getOtp(value)
                        instance.$value.setInnerHtml(totp.slice(0, 3) + ` ` + totp.slice(3))
                        instance.$canvas.setStyle(`visibility`, `visible`)
                        instance.$copy.setStyle(`display`, null)
                    }
                    catch
                    {
                        instance.$value.setInnerHtml(`Error`)
                        instance.$canvas.setStyle(`visibility`, `hidden`)
                        instance.$copy.setStyle(`display`, `none`)
                    }
                }
                
                await delay(1000)
            }
        })
        
        new Promise(async () =>
        {
            while (true)
            {
                const time = 150 - Math.round(new Date().getTime() / 200 % 150)
                
                for (const instance of App.getInstances(Totp))
                {
                    const $ = instance.$canvas
                    renderCircle($, time / 150, `#fff3`, `#fff`)
                }
                
                await delay(200)
            }
        })
    }
}
