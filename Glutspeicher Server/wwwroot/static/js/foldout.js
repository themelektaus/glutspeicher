class Foldout extends Component
{
    static _ = App.use({
        type: this,
        selector: `x-foldout`
    })
    
    init()
    {
        this.$title = create(`div`)
            .setClass(`title`, true)
            .setInnerHtml(this.$.getData(`title`))
        
        this.$content = create(`div`)
            .setClass(`content`, true)
        
        for (const $child of [...this.$.children])
        {
            this.$content.add($child)
        }
        
        this.$.add(this.$title)
        this.$.add(this.$content)
    }
    
    start()
    {
        this.$title.on(`click`, () =>
        {
            this.$.setClass(`visible`)
        })
    }
    
    stop()
    {
        this.$title.off(`click`)
    }
}
