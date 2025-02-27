class Dialog extends Component
{
    static _ = App.use({
        type: this,
        selector: `x-dialog`
    })
    
    init()
    {
        const html = this.$.innerHTML
        this.$.clearInnerHtml()
        this.$loading = create(`div`).setClass(`loading`, true)
        this.$window = this.$.create(`div`).setClass(`dialog-window`, true)
        this.$top = this.$window.create(`div`).setClass(`dialog-top`, true)
        this.$content = this.$window.create(`div`).setClass(`dialog-content`, true).setInnerHtml(html)
        this.$bottom = this.$window.create(`div`).setClass(`dialog-bottom`, true)
        
        this.onKeyDownBinding = this.onKeyDown.bind(this)
    }
    
    async load(callback)
    {
        await callback(this.$content)
        
        this.$loading.remove()
    }
    
    #busy = false
    
    get busy()
    {
        return this.#busy
    }
    
    set busy(value)
    {
        this.#busy = value
        this.$.setClass(`busy`, this.#busy)
    }
    
    async show()
    {
        this.busy = false
        this.visible = true
        
        App.beginLock()
        
        $body.add(this.$)
        
        await delay()
        
        this.$.setClass(`visible`, true)
        
        await delay(110)
        
        this.$.query(`input`)?.focus()
    }
    
    clear()
    {
        this.$content.clearInnerHtml().add(this.$loading)
    }
    
    async wait()
    {
        addEventListener(`keydown`, this.onKeyDownBinding)
        
        this.$.on(`click`, async e =>
        {
            if (!(this.hideOnBackdropClick ?? true))
            {
                return
            }
            
            if (this.busy)
            {
                return
            }
            
            if (!e.target.hierarchy.includes(this.$window))
            {
                await this.hide()
            }
        })
        
        await new Promise(async resolve =>
        {
            while (this.visible)
            {
                await delay(60)
            }
            
            resolve()
        })
    }
    
    async onKeyDown(e)
    {
        if (e.key == `Escape`)
        {
            await this.hide()
        }
    }
    
    async hide()
    {
        removeEventListener(`keydown`, this.onKeyDownBinding)
        
        this.$.off(`click`)
        this.$.setClass(`visible`, false)
        
        await delay(110)
        
        App.endLock()
        
        this.visible = false
    }
}
