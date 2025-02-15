class GeneratorsPage extends Page
{
    static _ = App.use({
        type: this,
        groupType: Page,
        selector: `[data-page="generators"]`
    })
    
    init()
    {
        super.init()
        
        this.$add = this.$.query(`.add`)
        this.$edit = this.$.query(`.edit`)
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
        this.$delete.on(`click`, this.onDelete.bind(this))
    }
    
    async lateStart()
    {
        if (GeneratorsPage.items === undefined)
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
        this.$delete.off(`click`)
    }
    
    async load()
    {
        App.beginLock()
    
        GeneratorsPage.items = []
        this.refresh()
        
        await GeneratorsPage.loadItems()
        this.refresh()
        
        App.endLock()
    }
    
    static async loadItems()
    {
        GeneratorsPage.items = (await fetchGet(`api/generators`)).data ?? []
    }
    
    async onAdd()
    {
        await this.showDialog()
    }
    
    async onEdit()
    {
        const item = GeneratorsPage.items.find(x => x.id == this.selectedItemId)
        await this.showDialog(item)
    }
    
    async onDelete()
    {
        const id = this.selectedItemId
        const result = await fetchDelete(`api/generators/${id}`)
        
        if (result.success)
        {
            GeneratorsPage.items.splice(GeneratorsPage.items.indexOf(GeneratorsPage.items.find(x => x.id == id)), 1)
            this.selectedItemId = 0
            this.refresh()
        }
    }
    
    refresh()
    {
        let $items = [...this.$items.queryAll(`.item`)]
        
        for (const item of GeneratorsPage.items)
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
                
                $item.create(`div`).setClass(`name`, true)
                
                $item.addEventListener(`click`, () =>
                {
                    this.selectedItemId = item.id
                    this.refreshSelection()
                })
            }
            
            $item.query(`.name`).setInnerHtml(item.name)
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
        
        const item = GeneratorsPage.items.find(x => x.id == this.selectedItemId)
        
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
                const result = await fetchPut(`api/generators`, data)
                
                if (result.success)
                {
                    const index = GeneratorsPage.items.indexOf(GeneratorsPage.items.find(x => x.id == item.id))
                    
                    GeneratorsPage.items[index] = data
                    this.refresh()
                }
            }
            else
            {
                const result = await fetchPost(`api/generators`, data)
                
                if (result.success)
                {
                    const item = result.data
                    GeneratorsPage.items.push(item)
                    
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
