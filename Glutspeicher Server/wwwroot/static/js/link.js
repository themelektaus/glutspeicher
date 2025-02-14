class Link extends Component
{
    static _ = App.use({
        type: this,
        selector: `[data-link]`
    })
    
    start()
    {
        this.$.on(`click`, () => Page.getByName(this.target).activate())
    }
    
    get target()
    {
        return this.$.getData(`link`)
    }
    
    get alternative()
    {
        return this.$.getData(`alternative`)
    }
    
    get hidden()
    {
        return this.$.style.display == `none`
    }
    
    set hidden(value)
    {
        this.$.style.display = value ? `none` : null
    }
    
    stop()
    {
        this.$.off(`click`)
    }
}
