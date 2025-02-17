Extensions = { }

Object.defineProperties(
    Object.prototype,
    {
        getValue:
        {
            value: function(path)
            {
                let value = this
                    
                for (const key of path.split(`.`))
                {
                    value = value[key]
                }
                
                return value
            }
        },
        setValue:
        {
            value: function(path, value)
            {
                let keys = path.split(`.`)
                let lastKey = keys.pop()
                
                let property = this
                for (const key of keys)
                {
                    property = property[key]
                }
                
                property[lastKey] = value
            }
        }
    }
)

Object.defineProperties(
    String.prototype,
    {
        toDateTimeArray:
        {
            value: function()
            {
                if (!this)
                {
                    return null
                }
                
                const result = this.replace(
                    /([0-9]{4})-([0-9]{2})-([0-9]{2})T([0-9]{2}):([0-9]{2})(.*?)/g,
                    '$3.$2.$1 $4:$5'
                ).split(` `)
                
                if (result.length >= 2)
                {
                    result[1] = result[1]?.substr(0, 5) ?? ``
                }
                else
                {
                    result.push(``)
                }
                
                return result
            }
        },
        toTimeSpanArray:
        {
            value: function()
            {
                if (!this)
                {
                    return null
                }
                
                const a = this.replace(
                    /([0-9]{2}):([0-9]{2}):([0-9]{2}).*/g,
                    '$1 $2 $3'
                ).split(' ')
                
                return [ +a[0], +a[1], +a[2] ]
            }
        },
        toTimeSpanHtml:
        {
            value: function()
            {
                const array = this.toTimeSpanArray()
                return array ? array.map(x => x < 10 ? `0${x}` : `${x}`).join(`:`) : ``
            }
        },
        replaceNewLine:
        {
            value: function(replacement)
            {
                return this.replace(
                    /([^>\r\n]?)(\r\n|\n\r|\r|\n)/g,
                    '$1' + replacement + '$2'
                )
            }
        }
    }
)

Object.defineProperties(
    Array.prototype,
    {
        sortBy:
        {
            value: function(path)
            {
                return this.sort((a, b) =>
                {
                    const _a = (a.getValue(path) ?? ``).toLowerCase()
                    const _b = (b.getValue(path) ?? ``).toLowerCase()
                    return _a < _b ? -1 : (_a > _b ? 1 : 0)
                })
            }
        },
        distinct:
        {
            value: function()
            {
                return this.filter((x, i, a) => a.indexOf(x) == i)
            }
        }
    }
)

Object.defineProperties(
    EventTarget.prototype,
    {
        $body:
        {
            get: function()
            {
                return document.body
            }
        },
        query:
        {
            value: function()
            {
                return (this instanceof Window ? document : this)
                    .querySelector(...arguments)
            }
        },
        queryAll:
        {
            value: function()
            {
                return (this instanceof Window ? document : this)
                    .querySelectorAll(...arguments)
            }
        },
        hierarchy:
        {
            get: function()
            {
                const hierarchy = []
                
                if (this instanceof Window)
                {
                    return hierarchy
                }
                
                let current = this
                
                while (current && !(current instanceof Document))
                {
                    hierarchy.push(current)
                    current = current.parentNode
                }
                
                return hierarchy
            }
        },
        create:
        {
            value: function()
            {
                const $ = document.createElement(...arguments)
                
                if (!(this instanceof Window))
                {
                    this.add($)
                }
                
                return $
            }
        },
        add:
        {
            value: function()
            {
                this.appendChild(...arguments)
                return this
            }
        },
        setAttr:
        {
            value: function(key, value)
            {
                if (value === null || value === undefined)
                {
                    this.removeAttribute(key)
                }
                else
                {
                    this.setAttribute(key, value)
                }
                return this
            }
        },
        getData:
        {
            value: function(key, defaultValue)
            {
                return this.dataset[key] ?? defaultValue ?? null
            }
        },
        setData:
        {
            value: function(key, value)
            {
                this.dataset[key] = value
                return this
            }
        },
        hasClass:
        {
            value: function()
            {
                return this.classList.contains(...arguments)
            }
        },
        setClass:
        {
            value: function()
            {
                this.classList.toggle(...arguments)
                return this
            }
        },
        getStyle:
        {
            value: function(key)
            {
                return this.style[key] ?? null
            }
        },
        setStyle:
        {
            value: function(key, value)
            {
                this.style[key] = value
                return this
            }
        },
        on:
        {
            value: function()
            {
                Extensions.eventListeners ??= []
                Extensions.eventListeners.push([this, arguments])
                this.addEventListener(...arguments)
                return this
            }
        },
        off:
        {
            value: function()
            {
                Extensions.eventListeners ??= []
                
                for (let i = 0; i < Extensions.eventListeners.length; i++)
                {
                    const eventListener = Extensions.eventListeners[i]
                    
                    if (this != eventListener[0])
                    {
                        continue
                    }
                    
                    if (arguments.length && arguments[0] != eventListener[1][0])
                    {
                        continue
                    }
                    
                    this.removeEventListener(...eventListener[1])
                    Extensions.eventListeners.splice(i--, 1)
                }
                
                return this
            }
        },
        clearInnerHtml:
        {
            value: function()
            {
                return this.setInnerHtml(``)
            }
        },
        setInnerHtml:
        {
            value: function(html)
            {
                this.innerHTML = html
                return this
            }
        },
        addInnerHtml:
        {
            value: function(html)
            {
                this.innerHTML += html
                return this
            }
        },
        addInnerHtmlAsync:
        {
            value: async function(html)
            {
                let $ = create(`div`).setInnerHtml(html)
                await delay(3)
                document.createDocumentFragment().add($.firstChild)
                await delay(3)
                $ = $.firstChild
                this.add($)
                await delay(7)
                $.setClass(`visible`, true)
                await delay(7)
            }
        }
    }
)

Object.defineProperties(
    Window.prototype,
    {
        defaultHeaders:
        {
            get: function()
            {
                //const data = Data.load()
                return { }
            }
        },
        fetchGet:
        {
            value: async function(url, data)
            {
                const queryString = data ? `?${new URLSearchParams(data)}` : ``
                const response = await fetch(`${url}${queryString}`, {
                    headers: defaultHeaders
                })
                return await response.json()
            }
        },
        fetchPost:
        {
            value: async function(url, data)
            {
                const headers = defaultHeaders
                headers['Content-Type'] = `application/json`
                const response = await fetch(url, {
                    method: `post`,
                    headers: headers,
                    body: JSON.stringify(data)
                })
                return await response.json()
            }
        },
        fetchPut:
        {
            value: async function(url, data)
            {
                const headers = defaultHeaders
                headers['Content-Type'] = `application/json`
                const response = await fetch(url, {
                    method: `put`,
                    headers: headers,
                    body: JSON.stringify(data)
                })
                return await response.json()
            }
        },
        fetchDelete:
        {
            value: async function(url)
            {
                const response = await fetch(url, {
                    method: `delete`,
                    headers: defaultHeaders
                })
                return await response.json()
            }
        },
        delay:
        {
            value: function(ms)
            {
                if (ms > 0)
                {
                    return new Promise(next => setTimeout(next, ms))
                }
                else
                {
                    return new Promise(next => requestAnimationFrame(next))
                }
            }
        },
        debugLog:
        {
            value: function(key, value, colors)
            {
                if (!DEBUG)
                {
                    return
                }
                
                const badge = key.trim()
                
                let spaces = ``
                for (let i = 0; i < key.length - badge.length; i++)
                {
                    spaces += ` `
                }
                
                colors ??= []
                colors[0] ??= `#111`
                colors[1] ??= `#999`
                colors[2] ??= `#ddd`
                
                console.log(
                    `${spaces}%c${badge}%c ${value}`,
                    `color: ${colors[0]}; ` +
                    `background-color: ${colors[1]}; ` +
                    `border-radius: 10em; ` +
                    `padding: 0 .5em; ` +
                    `font-weight: bold; `,
                    `color: ${colors[2]}; `
                )
            }
        }
    }
)
