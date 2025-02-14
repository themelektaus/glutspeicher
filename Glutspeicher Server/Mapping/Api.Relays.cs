using LiteDB;
using Microsoft.AspNetCore.Mvc;

namespace Glutspeicher.Server.Mapping;

public static partial class Api
{
    public static class Relays
    {
        static ILiteCollection<Model.Relay> Collection(LiteDbContext liteDbContext)
        {
            return liteDbContext.Database.GetCollection<Model.Relay>();
        }

        public static IApiResult GetAll(LiteDbContext liteDbContext)
        {
            return ApiResult.Ok(
                Collection(liteDbContext).FindAll().ToList()
            );
        }

        public static IApiResult Get(LiteDbContext liteDbContext, long id)
        {
            return ApiResult.OkIfNotNull(
                Collection(liteDbContext).FindById(id)
            );
        }

        public static IApiResult Post(LiteDbContext liteDbContext, [FromBody] Model.Relay data)
        {
            data ??= new();
            Collection(liteDbContext).Insert(data);
            return ApiResult.Ok(data);
        }

        public static IApiResult Put(LiteDbContext liteDbContext, [FromBody] Model.Relay data)
        {
            return ApiResult.OkIfTrue(
                Collection(liteDbContext).Update(data)
            );
        }

        public static IApiResult Delete(LiteDbContext liteDbContext, long id)
        {
            return ApiResult.OkIfTrue(
                Collection(liteDbContext).Delete(id)
            );
        }
    }
}
