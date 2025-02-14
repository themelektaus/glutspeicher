class Component
{
    static idSets = []
    
    connect($)
    {
        const attributes = [...$.attributes]
        this.$originAttributes = attributes.map(x => [x.nodeName, x.nodeValue])
        this.$ = $
        
        this.id = App.register(this)
        
        this.idSet.ids.push(this.id)
        
        this.#updateId()
    }
    
    get idSet()
    {
        let idSet = Component.idSets.find(x => x.$ == this.$)
        if (!idSet)
        {
            idSet = { $: this.$, ids: [] }
            Component.idSets.push(idSet)
        }
        return idSet
    }
    
    #updateId()
    {
        this.$.id = this.idSet.ids.join(`_`)
    }
    
    dispose()
    {
        App.unregister(this)
        
        this.idSet.ids = this.idSet.ids.filter(x => x.id != this.id)
        this.id = undefined
        
        this.#updateId()
        
        if (this.idSet.ids.length)
        {
            return
        }
        
        while (this.$.attributes.length > 0)
        {
            this.$.removeAttribute(this.$.attributes[0].name)
        }
        
        for (const attribute of this.$originAttributes)
        {
            this.$.setAttribute(...attribute)
            
        }
    }
}
