class RelaysPage extends Page
{
    static _ = App.use({
        type: this,
        groupType: Page,
        selector: `[data-page="relays"]`
    })
    
    init()
    {
        super.init()
        
        this.$add = this.$.query(`.add`)
        this.$edit = this.$.query(`.edit`)
        this.$export = this.$.query(`.export`)
        this.$delete = this.$.query(`.delete`)
        
        this.$content = this.$.query(`.content`)
        this.$content.addEventListener(`click`, async e =>
        {
            if (!e.target.hierarchy.some(x => x.classList.contains(`item`)))
            {
                this.selectedItemId = 0
                this.refreshSelection()
            }
        })
        
        this.$items = this.$content.query(`.items`)
    }
    
    async lateInit()
    {
        const dialogs = await App.updateAndGetInstances(Dialog, this.$)
        
        this.dialog = dialogs[0]
        this.dialog.hideOnBackdropClick = false
        
        const $ = this.dialog.$content
        
        this.dialog.form = {
            $inputs: [],
            
            $save: this.dialog.$bottom.create(`button`)
                .setClass(`save`)
                .setClass(`with-text`)
                .setClass(`positive`)
                .setStyle(`background-image`, `url(static/res/save.svg)`)
                .setInnerHtml(`Save`),
            
            $cancel: this.dialog.$bottom.create(`button`)
                .setClass(`cancel`)
                .setInnerHtml(`Cancel`)
        }
        
        $.queryAll(`input, textarea`).forEach($ =>
        {
            if (!$.readonly)
            {
                this.dialog.form.$inputs.push($)
            }
        })
    }
    
    async start()
    {
        this.$add.on(`click`, this.onAdd.bind(this))
        this.$edit.on(`click`, this.onEdit.bind(this))
        this.$export.on(`click`, this.onExport.bind(this))
        this.$delete.on(`click`, this.onDelete.bind(this))
    }
    
    async lateStart()
    {
        if (RelaysPage.items === undefined)
        {
            await this.load()
        }
        else
        {
            this.refresh()
        }
    }
    
    stop()
    {
        this.$add.off(`click`)
        this.$edit.off(`click`)
        this.$export.off(`click`)
        this.$delete.off(`click`)
    }
    
    async load()
    {
        App.beginLock()
    
        RelaysPage.items = []
        this.refresh()
        
        await RelaysPage.loadItems()
        this.refresh()
        
        App.endLock()
    }
    
    static async loadItems()
    {
        RelaysPage.items = (await fetchGet(`api/relays`)).data ?? []
    }
    
    async onAdd()
    {
        await this.showDialog()
    }
    
    async onEdit()
    {
        const item = RelaysPage.items.find(x => x.id == this.selectedItemId)
        await this.showDialog(item)
    }
    
    onExport()
    {
        playButtonAnimation(this.$export)
        
        location.href = `api/relays/export`
    }
    
    async onDelete()
    {
        const id = this.selectedItemId
        const result = await fetchDelete(`api/relays/${id}`)
        
        if (result.success)
        {
            RelaysPage.items.splice(RelaysPage.items.indexOf(RelaysPage.items.find(x => x.id == id)), 1)
            this.selectedItemId = 0
            this.refresh()
        }
    }
    
    refresh()
    {
        let $items = [...this.$items.queryAll(`.item`)]
        
        for (const item of RelaysPage.items)
        {
            let $item = $items.find($ => $.dataset.id == item.id)
            
            if ($item)
            {
                $items.splice($items.indexOf($item), 1)
            }
            else
            {
                $item = this.$items
                    .create(`div`)
                    .setClass(`item`, true)
                    .setData(`id`, item.id)
                
                $item.create(`div`).setClass(`hostname`, true)
                
                $item.addEventListener(`click`, () =>
                {
                    this.selectedItemId = item.id
                    this.refreshSelection()
                })
            }
            
            $item.query(`.hostname`).setInnerHtml(item.hostname)
        }
        
        for (const $item of $items)
        {
            $item.remove()
        }
        
        this.refreshSelection()
    }
    
    refreshSelection()
    {
        const $items = this.$items.queryAll(`.item`)
        $items.forEach($ => $.setClass(`selected`, $.getData(`id`) == this.selectedItemId))
        
        const item = RelaysPage.items.find(x => x.id == this.selectedItemId)
        
        if (item)
        {
            this.$edit.setAttr(`disabled`, null)
            this.$delete.setAttr(`disabled`, null)
        }
        else
        {
            this.$edit.setAttr(`disabled`, ``)
            this.$delete.setAttr(`disabled`, ``)
        }
    }
    
    async showDialog(item)
    {
        item ??= { }
        
        const form = this.dialog.form
        
        if (item.id)
        {
            this.dialog.$top.setInnerHtml(`Edit`)
            form.$inputs.forEach($ => $.value = item[$.classList[0]])
        }
        else
        {
            this.dialog.$top.setInnerHtml(`New`)
            form.$inputs.forEach($ => $.value = $.type == `number` ? 0 : ``)
        }
        
        await this.dialog.show()
        
        form.$save.on(`click`, async () =>
        {
            if (this.dialog.busy)
            {
                return
            }
            
            this.dialog.busy = true
            
            const data = { id: item.id }
            form.$inputs.forEach($ => data[$.classList[0]] = $.type == `number` ? +$.value : $.value)
            
            if (item.id)
            {
                const result = await fetchPut(`api/relays`, data)
                
                if (result.success)
                {
                    const index = RelaysPage.items.indexOf(RelaysPage.items.find(x => x.id == item.id))
                    
                    RelaysPage.items[index] = data
                    this.refresh()
                }
            }
            else
            {
                const result = await fetchPost(`api/relays`, data)
                
                if (result.success)
                {
                    const item = result.data
                    RelaysPage.items.push(item)
                    
                    this.selectedItemId = item.id
                    this.refresh()
                }
            }
            
            await this.dialog.hide()
        })
        
        form.$cancel.on(`click`, async () =>
        {
            if (this.dialog.busy)
            {
                return
            }
            
            this.dialog.busy = true
            await this.dialog.hide()
        })
        
        await this.dialog.wait()
        
        this.dialog.form.$save.off(`click`)
        this.dialog.form.$cancel.off(`click`)
    }
}
