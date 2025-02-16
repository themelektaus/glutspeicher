class CsvsPage extends Page
{
    static _ = App.use({
        type: this,
        groupType: Page,
        selector: `[data-page="csvs"]`
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
        
        this.$loadCsv = this.$.query(`.loadCsv`)
        this.$unloadCsv = this.$.query(`.unloadCsv`)
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
        
        this.$loadCsv.on(`click`, this.onLoadCsv.bind(this))
        this.$unloadCsv.on(`click`, this.onUnloadCsv.bind(this))
    }
    
    async lateStart()
    {
        if (CsvsPage.items === undefined)
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
        
        this.$loadCsv.off(`click`)
        this.$unloadCsv.off(`click`)
    }
    
    async load()
    {
        App.beginLock()
    
        CsvsPage.items = []
        this.refresh()
        
        await CsvsPage.loadItems()
        this.refresh()
        
        App.endLock()
    }
    
    static async loadItems()
    {
        CsvsPage.items = (await fetchGet(`api/csvs`)).data ?? []
    }
    
    async onAdd()
    {
        await this.showDialog()
    }
    
    async onEdit()
    {
        const item = CsvsPage.items.find(x => x.id == this.selectedItemId)
        await this.showDialog(item)
    }
    
    async onDelete()
    {
        const id = this.selectedItemId
        const result = await fetchDelete(`api/csvs/${id}`)
        
        if (result.success)
        {
            CsvsPage.items.splice(CsvsPage.items.indexOf(CsvsPage.items.find(x => x.id == id)), 1)
            this.selectedItemId = 0
            this.refresh()
        }
    }
    
    refresh()
    {
        let $items = [...this.$items.queryAll(`.item`)]
        
        for (const item of CsvsPage.items)
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
        
        const item = CsvsPage.items.find(x => x.id == this.selectedItemId)
        
        if (item)
        {
            this.$edit.setAttr(`disabled`, null)
            this.$delete.setAttr(`disabled`, null)
            
            this.$loadCsv.setAttr(`disabled`, null)
        }
        else
        {
            this.$edit.setAttr(`disabled`, ``)
            this.$delete.setAttr(`disabled`, ``)
            
            this.$loadCsv.setAttr(`disabled`, ``)
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
                const result = await fetchPut(`api/csvs`, data)
                
                if (result.success)
                {
                    const index = CsvsPage.items.indexOf(CsvsPage.items.find(x => x.id == item.id))
                    
                    CsvsPage.items[index] = data
                    this.refresh()
                }
            }
            else
            {
                const result = await fetchPost(`api/csvs`, data)
                
                if (result.success)
                {
                    const item = result.data
                    CsvsPage.items.push(item)
                    
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
    
    
    
    async onLoadCsv()
    {
        playButtonAnimation(this.$loadCsv)
        
        const csvItem = CsvsPage.items.find(x => x.id == this.selectedItemId)
        
        const response = await fetch(csvItem.uri)
        const text = await response.text()
        
        const rows = Papa.parse(text, { header: true })
        
        const items = []
        
        for (const row of rows.data)
        {
            const item = eval(`(function(row) { ${csvItem.script} })(${JSON.stringify(row)})`)
            
            items.push({
                id: -items.length - 1,
                name: item.name ?? ``,
                uri: item.uri ?? ``,
                username: item.username ?? ``,
                password: item.password ?? ``,
                generatorId: item.generatorId ?? 0,
                generatedPassword: item.generatedPassword ?? null,
                description: item.description ?? null,
                totp: item.totp ?? null,
                source: item.source ?? ``,
                section: item.section ?? ``,
                relayId: item.relayId ?? 0
            })
        }
        
        PasswordsPage.items = items
        PasswordsPage.refreshNextTime = true
    }
    
    async onUnloadCsv()
    {
        playButtonAnimation(this.$unloadCsv)
        
        PasswordsPage.items = null
        PasswordsPage.refreshNextTime = true
    }
}
