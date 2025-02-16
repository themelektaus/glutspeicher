class SettingsPage extends Page
{
    static _ = App.use({
        type: this,
        groupType: Page,
        selector: `[data-page="settings"]`
    })
    
    init()
    {
        super.init()
        
        this.$animations = this.$.query(`.animations`)
        this.$rebuildDatabase = this.$.query(`.rebuildDatabase`)
        this.$downloadDatabase = this.$.query(`.downloadDatabase`)
    }
    
    async start()
    {
        this.data = Data.load()
        
        this.$animations.checked = this.data.animations
        this.$animations.on(`change`, () =>
        {
            this.data.animations = this.$animations.checked
            this.data.save()
            App.updateBody()
        })
        
        this.$rebuildDatabase.on(`click`, async () =>
        {
            await fetch(`api/database/rebuild`)
        })
        
        this.$downloadDatabase.on(`click`, () =>
        {
            location.href = `api/database`
        })
    }
    
    stop()
    {
        this.$animations.off(`change`)
        this.$rebuildDatabase.off(`change`)
        this.$downloadDatabase.off(`change`)
    }
}
