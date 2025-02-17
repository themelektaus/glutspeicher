class ExportsPage extends Page
{
    static _ = App.use({
        type: this,
        groupType: Page,
        selector: `[data-page="exports"]`
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
        
        this.$loadExport = this.$.query(`.loadExport`)
        this.$loadAllExports = this.$.query(`.loadAllExports`)
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
        
        this.$loadExport.on(`click`, this.onLoadExport.bind(this))
        this.$loadAllExports.on(`click`, this.onLoadAllExports.bind(this))
    }
    
    async lateStart()
    {
        if (ExportsPage.items === undefined)
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
        
        this.$loadExport.off(`click`)
        this.$loadAllExports.off(`click`)
    }
    
    async load()
    {
        App.beginLock()
    
        ExportsPage.items = []
        this.refresh()
        
        await ExportsPage.loadItems()
        this.refresh()
        
        App.endLock()
    }
    
    static async loadItems()
    {
        ExportsPage.items = (await fetchGet(`api/exports`)).data ?? []
    }
    
    async onAdd()
    {
        await this.showDialog()
    }
    
    async onEdit()
    {
        const item = ExportsPage.items.find(x => x.id == this.selectedItemId)
        await this.showDialog(item)
    }
    
    onExport()
    {
        playButtonAnimation(this.$export)
        
        location.href = `api/exports/export`
    }
    
    async onDelete()
    {
        const id = this.selectedItemId
        const result = await fetchDelete(`api/exports/${id}`)
        
        if (result.success)
        {
            ExportsPage.items.splice(ExportsPage.items.indexOf(ExportsPage.items.find(x => x.id == id)), 1)
            this.selectedItemId = 0
            this.refresh()
        }
    }
    
    refresh()
    {
        let $items = [...this.$items.queryAll(`.item`)]
        
        for (const item of ExportsPage.items)
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
                $item.create(`div`).setClass(`uri`, true)
                
                $item.addEventListener(`click`, () =>
                {
                    this.selectedItemId = item.id
                    this.refreshSelection()
                })
            }
            
            $item.query(`.name`).setInnerHtml(item.name)
            $item.query(`.uri`).setInnerHtml(item.uri)
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
        
        const item = ExportsPage.items.find(x => x.id == this.selectedItemId)
        
        if (item)
        {
            this.$edit.setAttr(`disabled`, null)
            this.$delete.setAttr(`disabled`, null)
            
            this.$loadExport.setAttr(`disabled`, null)
        }
        else
        {
            this.$edit.setAttr(`disabled`, ``)
            this.$delete.setAttr(`disabled`, ``)
            
            this.$loadExport.setAttr(`disabled`, ``)
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
                const result = await fetchPut(`api/exports`, data)
                
                if (result.success)
                {
                    const index = ExportsPage.items.indexOf(ExportsPage.items.find(x => x.id == item.id))
                    
                    ExportsPage.items[index] = data
                    this.refresh()
                }
            }
            else
            {
                const result = await fetchPost(`api/exports`, data)
                
                if (result.success)
                {
                    const item = result.data
                    ExportsPage.items.push(item)
                    
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
    
    
    
    async onLoadExport()
    {
        const items = await this.loadExport(this.selectedItemId)
        
        for (const i in items)
        {
            items[i].id = -(+i + 1)
        }
        
        PasswordsPage.items = items
        PasswordsPage.isExport = true
        PasswordsPage.refreshNextTime = true
        
        Page.getByName(`passwords`).activate()
    }
    
    async onLoadAllExports()
    {
        const allItems = []
        
        for (const id of ExportsPage.items.map(x => x.id))
        {
            const items = await this.loadExport(id)
            allItems.push(...items)
        }
        
        for (const i in allItems)
        {
            allItems[i].id = -(+i + 1)
        }
        
        PasswordsPage.items = allItems
        PasswordsPage.isExport = true
        PasswordsPage.refreshNextTime = true
        
        Page.getByName(`passwords`).activate()
    }
    
    async loadExport(id)
    {
        const exportItem = ExportsPage.items.find(x => x.id == id)
        
        const response = await fetch(exportItem.uri)
        
        if (!response.ok)
        {
            return []
        }
        
        const text = await response.text()
        
        const rows = Papa.parse(text, { header: true })
        
        const items = []
        
        for (const row of rows.data)
        {
            const item = {
                name: row.Name ?? ``,
                uri: row.Uri ?? ``,
                username: row.Username ?? ``,
                password: row.Password ?? ``,
                generatorId: +(row.GeneratorId || 0),
                generatedPassword: row.GeneratedPassword ?? null,
                description: row.Description ?? null,
                totp: row.Totp ?? null,
                source: row.Source ?? ``,
                section: row.Section ?? ``,
                relayId: +(row.RelayId || 0)
            }
            
            if (exportItem.script)
            {
                const scriptItem = eval(`(function(row) { ${exportItem.script} })(${JSON.stringify(row)})`)
                
                if (!scriptItem)
                {
                    continue
                }
                
                for (const key in scriptItem)
                {
                    item[key] = scriptItem[key]
                }
            }
            
            items.push({
                name: item.name ?? ``,
                uri: item.uri ?? ``,
                username: item.username ?? ``,
                password: item.password ?? ``,
                generatorId: +(item.generatorId || 0),
                generatedPassword: item.generatedPassword ?? null,
                description: item.description ?? null,
                totp: item.totp ?? null,
                source: item.source ?? ``,
                section: item.section ?? ``,
                relayId: +(item.relayId || 0)
            })
        }
        
        return items
    }
}
