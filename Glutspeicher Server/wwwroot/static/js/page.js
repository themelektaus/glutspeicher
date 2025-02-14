class Page extends Component
{
    static getActives()
    {
        return App.getInstances().filter(x => x.isActive)
    }
    
    static getByName(name)
    {
        return App.getInstances().find(x => x.name == name)
    }
    
    init()
    {
        this.$parent = this.$.parentNode
        this.$.remove()
    }
    
    get isActive()
    {
        return this.$.parentNode ? true : false
    }
    
    get name()
    {
        return this.$.getData(`page`)
    }
    
    async activate(options)
    {
        await App.lock(async () =>
        {
            const data = Data.load()
            
            for (const link of App.getInstances())
            {
                link.$.setClass(
                    `active`,
                    link.target == this.name || link.alternative == this.name
                )
            }
            
            const activePages = Page.getActives()
            
            if (activePages.length == 1)
            {
                activePages[0].$
                    .setClass(`visible`, false)
                    .setClass(`fadeout`, true)
                
                await delay(data.animations ? 110 : 10)
            }
            
            if (activePages.length > 0)
            {
                for (const page of activePages)
                {
                    page.$.remove()
                }
                
                await App.updateComponents(Page)
                await App.updateComponentsExcept(Page, Link)
            }
            
            for (const page of activePages)
            {
                page.$.setClass(`fadeout`, false)
            }
            
            this.$parent.add(this.$)
            
            await delay()
            await delay()
            
            if (options?.delay)
            {
                await delay(options.delay)
            }
            
            this.$.setClass(`visible`, true)
            
            App.updateComponents(Page)
            
            data.page = this.name
            data.save()
            
            await Promise.all([
                delay(data.animations ? 110 : 10),
                App.updateComponentsExcept(Page, Link)
            ])
        })
    }
}
