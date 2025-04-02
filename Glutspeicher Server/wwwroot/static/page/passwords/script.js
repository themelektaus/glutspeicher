class PasswordsPage extends Page
{
    static _ = App.use({
        type: this,
        groupType: Page,
        selector: `[data-page="passwords"]`
    })
    
    init()
    {
        super.init()
        
        this.$add = this.$.query(`.add`)
        this.$edit = this.$.query(`.edit`)
        this.$export = this.$.query(`.export`)
        this.$delete = this.$.query(`.delete`)
        
        this.$content = this.$.query(`.content`)
        
        this.$items = this.$content.query(`.items`)
        
        this.$clone = this.$.query(`.clone`)
        this.$autoType = this.$.query(`.autoType`)
        this.$connect = this.$.query(`.connect`)
        
        this.$unloadExport = this.$.query(`.unloadExport`)
        
        this.$search = this.$.query(`.search`)
        this.$searchInput = this.$search.query(`input`)
        
        this.$noLimit = this.$.query(`.no-limit`)
            .setClass(`hidden`, true)
        
        this.$noLimit.addEventListener(`click`, async () =>
        {
            this.$noLimit.setClass(`hidden`, true)
            this.noLimit = true
            await this.refresh()
        })
    }
    
    async lateInit()
    {
        const dialogs = await App.updateAndGetInstances(Dialog, this.$)
        
        this.dialog = dialogs[0]
        this.dialog.hideOnBackdropClick = false
        
        const $ = this.dialog.$content
        
        this.dialog.form = {
            $inputs: [],
            
            $generator: this.dialog.$content.query(`.generator`),
            $generatedPassword: this.dialog.$content.query(`.generatedPassword`),
            $generatedPasswordInfo: this.dialog.$content.query(`.generatedPasswordInfo`),
            
            getDomain: function()
            {
                let domain = $.query(`.uri`).value?.trim() || ``
                if (domain)
                {
                    if (!domain.startsWith(`//`) && !domain.includes(`://`))
                    {
                        domain = `//${domain}`
                    }
                    
                    const a = create(`a`)
                    a.href = domain
                    domain = a.hostname
                    
                    while (domain.split(`.`).length > 2)
                    {
                        domain = [...domain.split(`.`)]
                        domain.splice(0, 1)
                        domain = domain.join(`.`)
                    }
                }
                return domain
            },
            
            getUsername: function()
            {
                return $.query(`.username`).value?.trim() || ``
            },
            
            refreshGeneratedPassword: async function()
            {
                const generator = (await PasswordsPage.getGenerators()).find(x => x.id == this.$generator.getData(`value`))
                if (generator?.length)
                {
                    const domain = this.getDomain()
                    const username = this.getUsername()
                    
                    const newValue = generatePassword(
                        generator.length,
                        generator.answers + domain + username
                    )
                    
                    const dirty = this.$generatedPassword.getData(`value`) != newValue
                    this.$generatedPassword.value = newValue
                    this.$generatedPassword.setClass(`dirty`, dirty)
                    this.$generatedPassword.parentNode.setStyle(`display`, null)
                    this.$generatedPasswordInfo.query(`.domain`).setInnerHtml(domain)
                    this.$generatedPasswordInfo.query(`.username`).setInnerHtml(username)
                    this.$generatedPasswordInfo.setStyle(`display`, null)
                    $.query(`.password`).parentNode.setStyle(`display`, `none`)
                }
                else
                {
                    this.$generatedPassword.value = ``
                    this.$generatedPassword.parentNode.setStyle(`display`, `none`)
                    this.$generatedPasswordInfo.setStyle(`display`, `none`)
                    $.query(`.password`).parentNode.setStyle(`display`, null)
                }
            },
            
            $relay: this.dialog.$content.query(`.relay`),
            
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
        
        $.query(`.uri`).addEventListener(`input`, async () =>
        {
            await this.dialog.form.refreshGeneratedPassword()
        })
        
        $.query(`.username`).addEventListener(`input`, async () =>
        {
            await this.dialog.form.refreshGeneratedPassword()
        })
        
        const dialog1 = dialogs[1]
        dialog1.$window.setStyle(`max-width`, `22em`)
        dialog1.$content.setStyle(`padding`, `1em`)
        
        const $button1 = this.dialog.form.$generator.parentNode.query(`button`)
        $button1.addEventListener(`click`, async () =>
        {
            this.dialog.busy = true
            
            dialog1.$content.clearInnerHtml()
            
            const $ul = dialog1.$content.create(`ul`)
            
            for (const generator of await PasswordsPage.getGenerators())
            {
                $ul.create(`li`)
                    .setInnerHtml(generator.name)
                    .setClass(`active`, generator.id == this.dialog.form.$generator.getData(`value`))
                    .addEventListener(`click`, async () =>
                    {
                        this.dialog.form.$generator.value = generator.name
                        this.dialog.form.$generator.setData(`value`, generator.id)
                        await this.dialog.form.refreshGeneratedPassword()
                        await dialog1.hide()
                    })
            }
            
            await dialog1.show()
            await dialog1.wait()
            
            this.dialog.busy = false
        })
        
        const dialog2 = dialogs[2]
        dialog2.$window.setStyle(`max-width`, `22em`)
        dialog2.$content.setStyle(`padding`, `1em`)
        
        const $button2 = this.dialog.form.$relay.parentNode.query(`button`)
        $button2.addEventListener(`click`, async () =>
        {
            this.dialog.busy = true
            
            dialog2.$content.clearInnerHtml()
            
            const $ul = dialog2.$content.create(`ul`)
            
            for (const relay of await PasswordsPage.getRelays())
            {
                $ul.create(`li`)
                    .setInnerHtml(relay.name)
                    .setClass(`active`, relay.id == this.dialog.form.$relay.getData(`value`))
                    .addEventListener(`click`, async () =>
                    {
                        this.dialog.form.$relay.value = relay.name
                        this.dialog.form.$relay.setData(`value`, relay.id)
                        await dialog2.hide()
                    })
            }
            
            await dialog2.show()
            await dialog2.wait()
            
            this.dialog.busy = false
        })
        
        this.infoDialog = dialogs[3]
        this.infoDialog.$content.setClass(`markdown-html`)
    }
    
    async start()
    {
        this.$add.on(`click`, this.onAdd.bind(this))
        this.$edit.on(`click`, this.onEdit.bind(this))
        this.$export.on(`click`, this.onExport.bind(this))
        this.$delete.on(`click`, this.onDelete.bind(this))
        
        this.$clone.on(`click`, this.onClone.bind(this))
        this.$autoType.on(`click`, this.onAutoType.bind(this))
        this.$connect.on(`click`, async () =>
        {
            await this.onConnect()
            
            if (Data.load().autoTypeOnConnect)
            {
                await delay(1000)
                this.onAutoType({ buttonAimation: false })
            }
        })
        
        this.$unloadExport.on(`click`, this.onUnloadExport.bind(this))
        
        on(`keydown`, e =>
        {
            if (App.isLocked)
            {
                return
            }
            
            let selectedItemId = null
            let $selectedItem
            
            switch (e.key)
            {
                case `Escape`:
                    e.preventDefault()
                    selectedItemId = 0
                    break
                
                case `ArrowUp`:
                    e.preventDefault()
                    
                    if (this.selectedItemId)
                    {
                        selectedItemId = [...this.$items.queryAll(`[hidden="false"]:has(~ .selected)`)].pop()?.dataset.id ?? null
                    }
                    else
                    {
                        selectedItemId = this.$items.query(`[hidden="false"]`)?.dataset.id
                    }
                    break
                    
                case `ArrowDown`:
                    e.preventDefault()
                    
                    if (this.selectedItemId)
                    {
                        selectedItemId = this.$items.query(`.selected ~ [hidden="false"]`)?.dataset.id ?? null
                    }
                    else
                    {
                        selectedItemId = this.$items.query(`[hidden="false"]`)?.dataset.id
                    }
                    break
                
                case `Enter`:
                    e.preventDefault()
                    
                    if (!this.$connect.hasClass(`hidden`))
                    {
                        this.onConnect({ buttonAimation: false })
                    }
                    break
                    
                case `a`:
                    if (e.ctrlKey)
                    {
                        e.preventDefault()
                        
                        if (this.$autoType.getAttribute(`disabled`) === null)
                        {
                            this.onAutoType({ buttonAimation: false })
                        }
                    }
                    break
                
                case `x`:
                    if (e.ctrlKey)
                    {
                        e.preventDefault()
                        
                        $selectedItem = this.$items.query(`.selected`)
                        if ($selectedItem)
                        {
                            $selectedItem.query(`.username button.copy`).click()
                        }
                    }
                    break
                    
                case `c`:
                    if (e.ctrlKey)
                    {
                        e.preventDefault()
                        
                        $selectedItem = this.$items.query(`.selected`)
                        if ($selectedItem)
                        {
                            $selectedItem.query(`.password button.copy`).click()
                        }
                    }
                    break
                
                case `v`:
                    if (e.ctrlKey)
                    {
                        e.preventDefault()
                        
                        $selectedItem = this.$items.query(`.selected`)
                        if ($selectedItem)
                        {
                            $selectedItem.query(`.totp button.copy`).click()
                        }
                    }
                    break
            }
            
            if (selectedItemId !== null)
            {
                selectedItemId = +selectedItemId
                if (this.selectedItemId != selectedItemId)
                {
                    this.selectedItemId = selectedItemId
                    this.refreshSelection()
                }
            }
        })
        
        this.$searchInput.on(`blur`, e =>
        {
            if (!App.isLocked)
            {
                this.$searchInput.focus()
            }
        })
        
        this.$searchInput.on(`input`, async e =>
        {
            this.noLimit = false
            await this.search()
        })
        
        this.$searchInput.focus()
    }
    
    async lateStart()
    {
        if (PasswordsPage.items === undefined || PasswordsPage.refreshNextTime)
        {
            if (PasswordsPage.items === undefined && !PasswordsPage.refreshNextTime)
            {
                PasswordsPage.items = null
            }
            
            if (PasswordsPage.refreshNextTime)
            {
                this.selectedItemId = 0
            }
            
            PasswordsPage.refreshNextTime = false
            
            if (PasswordsPage.items !== undefined)
            {
                App.beginLock()
                
                this.noLimit = false
                
                this.$.query(`.loading`).setStyle(`display`, null)
                
                const items = PasswordsPage.items
                
                PasswordsPage.items = []
                await this.refresh()
                
                if (items)
                {
                    PasswordsPage.items = items
                }
                else
                {
                    await PasswordsPage.loadItems()
                }
                
                await this.refresh(() => this.$.query(`.loading`).setStyle(`display`, `none`))
                
                App.endLock()
            }
        }
    }
    
    stop()
    {
        this.$add.off(`click`)
        this.$edit.off(`click`)
        this.$export.off(`click`)
        this.$delete.off(`click`)
        
        this.$clone.off(`click`)
        this.$autoType.off(`click`)
        this.$connect.off(`click`)
        
        this.$unloadExport.off(`click`)
        
        off(`keydown`)
        this.$searchInput.off(`blur`)
        this.$searchInput.off(`input`)
    }
    
    static async loadItems()
    {
        PasswordsPage.isExport = false
        PasswordsPage.items = (await fetchGet(`api/passwords`)).data ?? []
    }
    
    async onAdd()
    {
        await this.showDialog()
    }
    
    async onEdit()
    {
        await this.showDialog(this.getSelectedItem())
    }

    onExport()
    {
        playButtonAnimation(this.$export)
        
        location.href = `api/passwords/export`
    }
    
    async onDelete()
    {
        const id = this.selectedItemId
        const result = PasswordsPage.isExport
            ? { success: true }
            : (await fetchDelete(`api/passwords/${id}`))
        
        if (result.success)
        {
            PasswordsPage.items.splice(PasswordsPage.items.indexOf(PasswordsPage.items.find(x => x.id == id)), 1)
            this.selectedItemId = 0
            await this.refresh()
        }
    }
    
    async refresh(onBeforeSearch)
    {
        let $items = [...this.$items.queryAll(`.item`)]
        
        for (const item of PasswordsPage.items)
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
                
                $item.create(`div`).setClass(`keyword`, true).setClass(`name`, true)
                $item.create(`div`).setClass(`keyword`, true).setClass(`username`, true)
                $item.create(`div`).setClass(`password`, true)
                $item.create(`div`).setClass(`totp`, true)
                $item.create(`div`).setClass(`keyword`, true).setClass(`uri`, true)
                $item.create(`div`).setClass(`keyword`, true).setClass(`source`, true)
                $item.create(`div`).setClass(`keyword`, true).setClass(`section`, true)
                
                $item.addEventListener(`mousedown`, () =>
                {
                    this.selectedItemId = item.id
                    this.refreshSelection()
                })
            }
            
            if (onBeforeSearch && !this.noLimit)
            {
                $item.setStyle(`display`, `none`)
            }
            
            let scheme = ``
            let uri = item.uri ?? ``
            
            if (uri)
            {
                if (uri.includes(`://`))
                {
                    const uriParts = uri.split(`://`, 2)
                    scheme = uriParts[0]
                    uri = uriParts[1]
                }
                
                if (scheme)
                {
                    scheme = `<span class="scheme">${scheme}</span>`
                }
                
                if (uri.endsWith('/'))
                {
                    uri = uri.slice(0, -1)
                }
                
                uri = `${scheme}<span class="url">${uri}</span>`
            }
            
            if (item.relayId)
            {
                uri = `<span class="relay">relay</span>${uri}`
            }
            
            const $name = $item.query(`.name`)
            $name.clearInnerHtml()
            $name.create(`span`).setInnerHtml(item.name)
            
            if (item.description)
            {
                const $button = $name.create(`button`).setClass(`info`, true)
                $button.addEventListener(`click`, () =>
                {
                    let html = item.description.replace(/([^>\r\n]?)(\r\n|\n\r|\r|\n)/g, `$1<br>$2`)
                    html = (new showdown.Converter()).makeHtml(html)
                    this.infoDialog.$content.setInnerHtml(html)
                    this.infoDialog.show()
                    this.infoDialog.wait()
                })
            }
            
            if (item.username)
            {
                const username = item.username.replace(/"/g, "&quot;")
                $item.query(`.username`).setInnerHtml(
                    `<span>${username}</span>` +
                    `<button class="copy" data-value="${username}"></button>`
                )
            }
            else
            {
                $item.query(`.username`).clearInnerHtml()
            }
            
            let password = item.generatedPassword || item.password
            
            if (password)
            {
                const passwordLength = password.length + 1
                password = password.replace(/"/g, "&quot;")
                $item.query(`.password`).setClass(`generated`, item.generatedPassword).setInnerHtml(
                    `<span>${password}</span>` +
                    `<span>${Array(passwordLength).join(`•`)}</span>` +
                    `<button class="eye"></button>` +
                    `<button class="copy" data-value="${password}"></button>`
                )
            }
            else
            {
                $item.query(`.password`).clearInnerHtml()
            }
            
            if (item.totp)
            {
                $item.query(`.totp`)
                    .clearInnerHtml()
                    .create(`x-totp`)
                    .setData(`value`, item.totp)
            }
            else
            {
                $item.query(`.totp`).clearInnerHtml()
            }
            
            $item.query(`.uri`).setInnerHtml(uri)
            $item.query(`.source`).setInnerHtml(item.source)
            $item.query(`.section`).setInnerHtml(item.section)
        }
        
        for (const $item of $items)
        {
            $item.remove()
        }
        
        if (onBeforeSearch)
        {
            onBeforeSearch()
        }
        
        await App.updateComponents(Totp)
        
        this.refreshSelection()
        
        await this.search()
    }
    
    refreshSelection()
    {
        const $items = this.$items.queryAll(`.item`)
        $items.forEach($ =>
        {
            if ($.getData(`id`) == this.selectedItemId)
            {
                $.setClass(`selected`, true)
                $.scrollIntoView({ block: `nearest` })
            }
            else
            {
                $.setClass(`selected`, false)
            }
        })
        
        const item = this.getSelectedItem()
        
        if (item)
        {
            const uri = item.uri ?? ``
            
            this.$edit.setAttr(`disabled`, null)
            this.$delete.setAttr(`disabled`, null)
            
            this.$clone.setAttr(`disabled`, null)
            this.$autoType.setAttr(`disabled`, null)
            
            if (uri.startsWith(`rdp://`))
            {
                this.$connect.setClass(`hidden`, false)
                    .setData(`scheme`, `rdp`)
                    .setStyle(`background-image`, `url(static/res/remotedesktop.svg`)
                    .setInnerHtml(`RDP`)
            }
            else if (uri.startsWith(`ssh://`))
            {
                this.$connect.setClass(`hidden`, false)
                    .setData(`scheme`, `ssh`)
                    .setStyle(`background-image`, `url(static/res/console.svg`)
                    .setInnerHtml(`SSH`)
            }
            else if (uri && (!uri.includes(`://`) || uri.startsWith(`http://`) || uri.startsWith(`https://`)))
            {
                this.$connect.setClass(`hidden`, false)
                    .setData(`scheme`, `web`)
                    .setStyle(`background-image`, `url(static/res/externallink.svg`)
                    .setInnerHtml(uri.startsWith(`http://`) ? `HTTP` : `HTTPS`)
            }
            else
            {
                this.$connect.setClass(`hidden`, true)
                    .setData(`scheme`, null)
                    .setStyle(`background-image`, null)
                    .clearInnerHtml()
            }
        }
        else
        {
            this.$edit.setAttr(`disabled`, ``)
            this.$delete.setAttr(`disabled`, ``)
            
            this.$clone.setAttr(`disabled`, ``)
            this.$autoType.setAttr(`disabled`, ``)
            this.$connect.setClass(`hidden`, true)
                .setData(`scheme`, null)
                .setStyle(`background-image`, null)
                .clearInnerHtml()
        }
        
        this.$unloadExport.setClass(`hidden`, !PasswordsPage.isExport)
        this.$export.setAttr(`disabled`, PasswordsPage.isExport ? `` : null)
    }
    
    async showDialog(item)
    {
        if (App.isLocked)
        {
            return
        }
        
        item ??= { }
        
        const form = this.dialog.form
        this.dialog.$top.setInnerHtml(item.id ? `Edit` : `New`)
        form.$inputs.forEach($ => $.value = item[$.classList[0]] ?? ($.type == `number` ? 0 : ``))
        
        const generator = (await PasswordsPage.getGenerators()).find(x => x.id == (item.generatorId ?? 0)) ?? { id: item.generatorId, name: item.generatorId }
        form.$generator.value = generator.name
        form.$generator.setData(`value`, generator.id)
        
        const relay = (await PasswordsPage.getRelays()).find(x => x.id == (item.relayId ?? 0)) ?? { id: item.relayId, hostname: item.relayId }
        form.$relay.value = relay.name
        form.$relay.setData(`value`, relay.id)
        
        form.$generatedPassword.setData(`value`, item.generatedPassword || ``)
        
        await form.refreshGeneratedPassword()
        await this.dialog.show()
        
        this.$searchInput.blur()
        
        let needsRefresh = false
        
        form.$save.on(`click`, async () =>
        {
            if (this.dialog.busy)
            {
                return
            }
            
            this.dialog.busy = true
            
            const data = { id: item.id }
            form.$inputs.forEach($ => data[$.classList[0]] = $.type == `number` ? +$.value : $.value)
            
            data.generatorId = parseInt(form.$generator.getData(`value`))
            data.relayId = parseInt(form.$relay.getData(`value`))
            
            if (item.id)
            {
                const result = PasswordsPage.isExport
                    ? { success: true }
                    : (await fetchPut(`api/passwords`, data))
                
                if (result.success)
                {
                    const index = PasswordsPage.items.indexOf(PasswordsPage.items.find(x => x.id == item.id))
                    PasswordsPage.items[index] = data
                    needsRefresh = true
                }
            }
            else
            {
                const result = await fetchPost(`api/passwords`, data)
                
                if (result.success)
                {
                    const item = result.data
                    PasswordsPage.items.push(item)
                    this.selectedItemId = item.id
                    needsRefresh = true
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
        
        this.$searchInput.focus()
        
        if (needsRefresh)
        {
            await this.refresh()
        }
        
        this.dialog.form.$save.off(`click`)
        this.dialog.form.$cancel.off(`click`)
    }
    
    getSelectedItem()
    {
        return PasswordsPage.items.find(x => x.id == this.selectedItemId)
    }
    
    
    
    #searching
    #nextSearchText
    #searchDelayTimer
    
    async search()
    {
        this.$noLimit.setClass(`hidden`, true)
        
        this.#nextSearchText = this.$searchInput.value
        this.#searchDelayTimer = 50
        
        App.beginLock()
        
        while (this.#searching)
        {
            await delay(100)
        }
        
        while (this.#searchDelayTimer > 0)
        {
            this.#searchDelayTimer--
            await delay(10)
        }
        
        if (this.#nextSearchText != this.$searchInput.value)
        {
            App.endLock()
            return
        }
        
        const text = this.#nextSearchText
            .toLowerCase()
            .split(` `)
            .filter(x => x)
        
        this.#searching = true
        
        const $items = [...this.$items.queryAll(`.item`)]
        $items.forEach($ => $.setAttr(`hidden`, `true`))
        
        const $visibleItems = []
        
        if (text.length == 0)
        {
            if (!this.noLimit && $items.length > 100)
            {
                for (const $ of $items.slice(0, 100))
                {
                    $.setAttr(`hidden`, `false`)
                }
                
                this.$noLimit.setClass(`hidden`, false)
                this.$noLimit.setInnerHtml(`Load all ${$items.length} items`)
            }
            else
            {
                for (const $ of $items)
                {
                    $.setAttr(`hidden`, `false`)
                }
            }
        }
        else
        {
            let counter = 0
            
            for (const $ of $items)
            {
                const itemText = [...$.queryAll(`.keyword`)]
                    .map(x => x.innerText).join(` `)
                    .toLowerCase()
                
                if (text.some(x => !itemText.includes(x)))
                {
                    $.setAttr(`hidden`, `true`)
                }
                else
                {
                    if (this.noLimit)
                    {
                        $.setAttr(`hidden`, `false`)
                        continue
                    }
                    
                    if (++counter >= 100)
                    {
                        this.$noLimit.setClass(`hidden`, false)
                        this.$noLimit.setInnerHtml(`Load all ${counter} items`)
                        $.setAttr(`hidden`, `true`)
                    }
                    else
                    {
                        $.setAttr(`hidden`, `false`)
                    }
                }
            }
        }
        
        for (const $ of $items)
        {
            if ($.getAttribute(`hidden`) == `true`)
            {
                $.setStyle(`display`, `none`)
            }
        }
        
        let counter = 0
        
        for (const $ of $items)
        {
            if ($.getAttribute(`hidden`) == `false`)
            {
                $.setStyle(`display`, null)
                
                if (++counter > $items.length / 20)
                {
                    counter -= $items.length / 20
                    await delay(10)
                }
            }
        }
        
        if (!this.selectedItemId || this.$items.query(`.item.selected[hidden="true"]`))
        {
            const $targetSelection = this.$items.query(`.item[hidden="false"]`)
            
            if ($targetSelection)
            {
                const selectedItemId = +$targetSelection.dataset.id
                
                if (this.selectedItemId != selectedItemId)
                {
                    this.selectedItemId = selectedItemId
                    this.refreshSelection()
                }
            }
            else
            {
                if (this.selectedItemId)
                {
                    this.selectedItemId = 0
                    this.refreshSelection()
                }
            }
        }
        
        this.#searching = false
        
        App.endLock()
    }
    
    
    
    async onClone()
    {
        let item = this.getSelectedItem()
        item = JSON.parse(JSON.stringify(item))
        item.id = 0
        item.name += ` (Clone)`
        await this.showDialog(item)
    }
    
    onAutoType(options)
    {
        if (options?.buttonAimation !== false)
        {
            playButtonAnimation(this.$autoType)
        }
        
        const item = this.getSelectedItem()
        
        const data = {
            type: `AutoType`,
            title: item.name,
            text: [
                item.username,
                item.generatedPassword || item.password
            ]
        }
        
        location.href = `glut://${btoa(JSON.stringify(data))}`
    }
    
    async onRelay(rules, type, loadAdditionalData, useWebCommandLine)
    {
        const item = this.getSelectedItem()
        
        let uri = item.uri
        
        if (!uri.includes(`://`))
        {
            uri = `https://${uri}`
        }
        
        const index = rules.findIndex(x => uri.startsWith(`${x.scheme}://`))
        
        if (index != -1)
        {
            const rule = rules[index]
            
            const hostnameAndPort = uri.substr(rule.scheme.length + 3).split(`:`, 2)
            
            if (hostnameAndPort.length < 2)
            {
                hostnameAndPort.push(rule.defaultPort)
            }
            
            const data = {
                type: type,
                hostname: hostnameAndPort[0],
                port: +hostnameAndPort[1]
            }
            
            if (loadAdditionalData)
            {
                loadAdditionalData(data, item)
            }
            
            if (item.relayId)
            {
                const relay = (await PasswordsPage.getRelays()).find(x => x.id == item.relayId)
                
                if (relay)
                {
                    if (useWebCommandLine && relay.webCommandLine)
                    {
                        data.webCommandLine = relay.webCommandLine
                    }
                    else
                    {
                        data.relayHostname = relay.hostname
                        data.relaySshPort = relay.sshPort
                        data.relaySshUsername = relay.sshUsername
                        data.relaySshPassword = relay.sshPassword
                        data.relayMinPort = relay.minPort
                        data.relayMaxPort = relay.maxPort
                    }
                }
            }
            
            location.href = `glut://${btoa(JSON.stringify(data))}`
        }
    }
    
    async onConnect(options)
    {
        if (options?.buttonAimation !== false)
        {
            playButtonAnimation(this.$connect)
        }
        
        switch (this.$connect.getData(`scheme`))
        {
            case `rdp`:
                await this.onRelay([{ scheme: `rdp`, defaultPort: `3389` }], `Mstsc`, (data, item) => {
                    data.username = item.username
                    data.password = item.generatedPassword || item.password
                })
            
            case `ssh`:
                await this.onRelay([{ scheme: `ssh`, defaultPort: `22` }], `Ssh`, (data, item) => {
                    data.username = item.username
                    data.password = item.generatedPassword || item.password
                })
                break
            
            case `web`:
                await this.onRelay([{ scheme: `http`, defaultPort: `80` }, { scheme: `https`, defaultPort: `443` }], `Web`, (data, item) => {
                    data.name = item.name,
                    data.uri = item.uri.includes(`://`) ? item.uri : `https://${item.uri}`
                }, true)
                break
        }
    }
    
    
    
    onUnloadExport()
    {
        App.lock(() =>
        {
            PasswordsPage.items = null
            PasswordsPage.isExport = false
            PasswordsPage.refreshNextTime = true
            
            Page.getByName(`passwords`).activate()
        })
    }
    
    
    
    static async getGenerators()
    {
        if (GeneratorsPage.items === undefined)
        {
            await GeneratorsPage.loadItems()
        }
        
        return [ { id: 0, name: `None` }, ...GeneratorsPage.items ]
    }
    
    static async getRelays()
    {
        if (RelaysPage.items === undefined)
        {
            await RelaysPage.loadItems()
        }
        
        return [ { id: 0, name: `None` }, ...RelaysPage.items ]
    }
}
