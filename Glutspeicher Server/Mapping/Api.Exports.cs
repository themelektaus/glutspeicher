using LiteDB;
using Microsoft.AspNetCore.Mvc;

namespace Glutspeicher.Server.Mapping;

public static partial class Api
{
    public static class Exports
    {
        static ILiteCollection<Model.Export> Collection(LiteDbContext liteDbContext)
        {
            return liteDbContext.Database.GetCollection<Model.Export>();
        }

        public static IApiResult GetAll(LiteDbContext liteDbContext)
        {
            return ApiResult.Ok(
                Collection(liteDbContext).FindAll().ToList()
            );
        }

        public static IResult Export(LiteDbContext liteDbContext)
        {
            var data = Collection(liteDbContext).FindAll().ToList();
            return Results.File(
                ToCsv(data),
                "application/octet-stream",
                $"{nameof(Glutspeicher)} {nameof(Exports)} {Now:yyyy-MM-dd HH-mm-ss}.csv"
            );
        }

        public static IApiResult Get(LiteDbContext liteDbContext, long id)
        {
            return ApiResult.OkIfNotNull(
                Collection(liteDbContext).FindById(id)
            );
        }

        public static IApiResult Post(LiteDbContext liteDbContext, [FromBody] Model.Export data)
        {
            data ??= new();
            Collection(liteDbContext).Insert(data);
            liteDbContext.SetDirty();
            return ApiResult.Ok(data);
        }

        public static IApiResult Put(LiteDbContext liteDbContext, [FromBody] Model.Export data)
        {
            var result = Collection(liteDbContext).Update(data);
            liteDbContext.SetDirty();
            return ApiResult.OkIfTrue(result);
        }

        public static IApiResult Delete(LiteDbContext liteDbContext, long id)
        {
            var result = Collection(liteDbContext).Delete(id);
            liteDbContext.SetDirty();
            return ApiResult.OkIfTrue(result);
        }
    }
}
