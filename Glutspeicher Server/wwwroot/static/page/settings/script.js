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
        
        this.$downloadDatabase.on(`click`, () =>
        {
            location.href = `api/database`
        })
    }
    
    stop()
    {
        this.$animations.off(`change`)
        this.$downloadDatabase.off(`change`)
    }
}
