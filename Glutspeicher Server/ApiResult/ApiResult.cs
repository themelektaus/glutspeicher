namespace Glutspeicher.Server;

public class ApiResult : IApiResult
{
    public bool Success { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ErrorMessage { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiListInfo? ListInfo { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object Data { get; set; }

    public static IApiResult Ok()
    {
        return new ApiResult
        {
            Success = true
        };
    }

    public static IApiResult OkIfTrue(bool success)
    {
        return new ApiResult
        {
            Success = success
        };
    }

    public static IApiResult Ok(object data)
    {
        return new ApiResult
        {
            Success = true,
            Data = data
        };
    }

    public static IApiResult OkIfNotNull(object data)
    {
        return new ApiResult
        {
            Success = data is not null,
            Data = data
        };
    }

    public static IApiResult Ok<T>(
        IEnumerable<T> data,
        ApiResultOptions<T> options = default
    )
    {
        var listInfo = options.Apply(data);
        return new ApiResult
        {
            Success = true,
            ListInfo = listInfo,
            Data = listInfo.List
        };
    }

    public static IApiResult Error()
    {
        return new ApiResult
        {
            Success = false
        };
    }

    public static IApiResult Error(string message)
    {
        return new ApiResult
        {
            Success = false,
            ErrorMessage = message
        };
    }
}
