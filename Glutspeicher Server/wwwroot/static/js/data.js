class Data
{
    static key = `Glutspeicher-Data`
    
    constructor(data)
    {
        this.page = data?.page ?? `passwords`
        this.animations = data?.animations ?? true
    }
    
    static load()
    {
        const json = localStorage.getItem(Data.key)
        
        let data
        try { data = JSON.parse(json) ?? { } } catch { }
        
        return new Data(data)
    }
    
    save()
    {
        const json = JSON.stringify(this)
        localStorage.setItem(Data.key, json)
    }
}
