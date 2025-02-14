class App
{
    static groups = []
    
    static lockLevel = 0
    
    static get isLocked()
    {
        return App.lockLevel > 0
    }
    
    static beginLock()
    {
        App.lockLevel++
        
        query(`#app`).setClass(`locked`, true)
    }
    
    static endLock()
    {
        App.lockLevel--
        
        if (App.lockLevel <= 0)
        {
            App.lockLevel = 0
            
            query(`#app`).setClass(`locked`, false)
        }
    }
    
    static async lock(callback)
    {
        while (App.lockLevel > 0)
        {
            await delay(100)
        }
        
        App.beginLock()
        await callback()
        App.endLock()
    }
    
    static use(options)
    {
        if (!options?.type)
        {
            console.warn(`options.type is null`)
            return
        }
        
        if (!options?.selector)
        {
            console.warn(`options.selector is null`)
            return
        }
        
        const groupType = options.groupType ?? options.type ?? Component
        
        let group = App.groups.find(x => x.type == groupType)
        
        if (!group)
        {
            group = {
                type: groupType,
                lastId: 0,
                components: [],
                instances: []
            }
            
            App.groups.push(group)
        }
        
        const component = {
            group: group,
            type: options.type,
            selector: options.selector,
            $queue: []
        }
        
        group.components.push(component)
    }
    
    static getGroups()
    {
        if (arguments.length)
        {
            return App.groups.filter(x => [...arguments].includes(x.type))
        }
        else
        {
            return App.groups
        }
    }
    
    static getComponents()
    {
        return App.getGroups(...arguments)
            .map(x => x.components)
            .flat()
            .distinct()
    }
    
    static async updateAndGetInstance()
    {
        const instances = await App.updateAndGetInstances(...arguments)
        return instances.length ? instances[0] : null
    }
    
    static async updateAndGetInstances(type, $parent, ...selectors)
    {
        await App.updateComponents(type, $parent)
        
        selectors.push(...App.getComponents(type).map(x => x.selector))
        
        let $$ = []
        
        for (const selector of selectors)
        {
            $$.push(...$parent.queryAll(selector))
        }
        
        $$ = $$.distinct()
        
        return App.getInstances(type).filter(x => $$.includes(x.$))
    }
    
    static getInstances()
    {
        return App.getGroups(...arguments)
            .map(x => x.instances)
            .flat()
    }
    
    static register(instance)
    {
        const group = App.getComponents()
            .find(x => x.type.name == instance.constructor.name)
            .group
        
        group.lastId++
        group.instances.push(instance)
        
        const id = `${group.type.name.toLowerCase()}-${group.lastId}`
        
        return id
    }
    
    static unregister(instance)
    {
        const group = App.getComponents()
            .find(x => x.type.name == instance.constructor.name)
            .group
        
        group.instances = group.instances.filter(x => x != instance)
    }
    
    static enqueueComponent(type, $)
    {
        App.getComponents().find(x => x.type == type).$queue.push($)
    }
    
    static async updateComponentsExcept()
    {
        if (arguments.length)
        {
            const groupTypes = App.groups
                .map(x => x.type)
                .filter(x => ![...arguments].includes(x))
            
            await App.updateComponents(...groupTypes)
        }
        else
        {
            await App.updateComponents()
        }
    }
    
    static async updateComponents()
    {
        const colors = {
            init:        [ `#321`, `#c96` , `#fc9`  ],
            initEmpty:   [ `#321`, `#c966`, `#fc96` ],
            start:       [ `#131`, `#6c6`,  `#9f9`  ],
            startEmpty:  [ `#131`, `#6c66`, `#9f96` ],
            update:      [ `#113`, `#66d`,  `#99f`  ],
            updateEmpty: [ `#113`, `#66d6`, `#99f6` ],
            stop:        [ `#311`, `#b33`,  `#d66`  ],
            stopEmpty:   [ `#311`, `#b336`, `#d666` ],
        }
        
        const $parents = [...arguments].filter(x => x instanceof EventTarget)
        
        const components = App.getComponents(
            ...([...arguments].filter(x => !(x instanceof EventTarget)))
        )
        
        const lateInstances = []
        
        for (const component of components)
        {
            const $$ = [ ...queryAll(component.selector), ...component.$queue ]
            
            for (const $parent of $parents)
            {
                $$.push(...$parent.queryAll(component.selector))
            }
            
            component.$queue = []
            
            for (const $ of $$)
            {
                let instance = component.group.instances.find(x => x.$ == $)
                
                if (!instance)
                {
                    instance = new component.type
                    instance.connect($)
                    
                    if (instance.init)
                    {
                        debugLog(`        init`, instance.$.id, colors.init)
                        await instance.init()
                    }
                    else
                    {
                        debugLog(`        init`, instance.$.id, colors.initEmpty)
                    }
                    
                    if (instance.lateInit)
                    {
                        lateInstances.push(instance)
                    }
                }
            }
        }
        
        for (const instance of lateInstances)
        {
            debugLog(`   late init`, instance.$.id, colors.init)
            await instance.lateInit()
        }
        
        lateInstances.length = 0
        
        const instances = App.getInstances(...arguments)
        
        for (const instance of instances)
        {
            if ($body.contains(instance.$))
            {
                if (!instance.started)
                {
                    instance.started = true
                    
                    if (instance.start)
                    {
                        debugLog(`       start`, instance.$.id, colors.start)
                        await instance.start()
                    }
                    else
                    {
                        debugLog(`       start`, instance.$.id, colors.startEmpty)
                    }
                    
                    if (instance.lateStart)
                    {
                        lateInstances.push(instance)
                    }
                }
            }
        }
        
        for (const instance of lateInstances)
        {
            debugLog(`  late start`, instance.$.id, colors.start)
            await instance.lateStart()
        }
        
        lateInstances.length = 0
        
        for (const instance of instances)
        {
            if (instance.started)
            {
                if (instance.update)
                {
                    debugLog(`      update`, instance.$.id, colors.update)
                    await instance.update()
                }
                else
                {
                    debugLog(`      update`, instance.$.id, colors.updateEmpty)
                }
                
                if (instance.lateUpdate)
                {
                    lateInstances.push(instance)
                }
            }
        }
        
        for (const instance of lateInstances)
        {
            debugLog(` late update`, instance.$.id, colors.update)
            await instance.lateUpdate()
        }
        
        lateInstances.length = 0
        
        for (const instance of instances)
        {
            if (!$body.contains(instance.$))
            {
                if (instance.started)
                {
                    instance.started = false
                    
                    if (instance.stop)
                    {
                        debugLog(`        stop`, instance.$.id, colors.stop)
                        await instance.stop()
                    }
                    else
                    {
                        debugLog(`        stop`, instance.$.id, colors.stopEmpty)
                    }
                
                    if (instance.lateStop)
                    {
                        lateInstances.push(instance)
                    }
                }
            }
        }
        
        for (const instance of lateInstances)
        {
            debugLog(`   late stop`, instance.$.id, colors.stop)
            await instance.lateStop()
        }
    }
    
    static updateBody()
    {
        $body.setClass(`animations`, Data.load().animations)
        $body.setClass(`debug`, DEBUG)
    }
    
    static listen()
    {
        on(`click`, async e =>
        {
            if (App.isLocked)
            {
                return
            }
            
            const $ = e.target
            const classList = $.classList
            
            if ($.dataset.value && classList.contains(`copy`))
            {
                await writeTextToClipboard($.dataset.value)
                return
            }
            
            if (classList.contains(`eye`))
            {
                $.parentNode.setClass(`revealed`)
                return
            }
            
            //const hierarchy = e.target.hierarchy
            //
            //for (const $ of hierarchy)
            //{
            //}
        })
    }
}
