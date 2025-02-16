using LiteDB;
using Microsoft.AspNetCore.Mvc;

namespace Glutspeicher.Server.Mapping;

public static partial class Api
{
    public static class Passwords
    {
        static ILiteCollection<Model.Password> Collection(LiteDbContext liteDbContext)
        {
            return liteDbContext.Database.GetCollection<Model.Password>();
        }

        public static IApiResult GetAll(LiteDbContext liteDbContext)
        {
            return ApiResult.Ok(
                Collection(liteDbContext).FindAll().OrderBy(x => x.Name).ThenBy(x => x.Username).ThenBy(x => x.Uri).ToList()
            );
        }

        public static IResult Export(LiteDbContext liteDbContext)
        {
            var data = Collection(liteDbContext).FindAll().ToList();
            return Results.File(
                ToCsv(data),
                "application/octet-stream",
                $"{nameof(Glutspeicher)} {nameof(Passwords)} {Now:yyyy-MM-dd HH-mm-ss}.csv"
            );
        }

        public static IApiResult Get(LiteDbContext liteDbContext, long id)
        {
            return ApiResult.OkIfNotNull(
                Collection(liteDbContext).FindById(id)
            );
        }

        public static IApiResult Post(LiteDbContext liteDbContext, [FromBody] Model.Password data)
        {
            data ??= new();
            Collection(liteDbContext).Insert(data);
            return ApiResult.Ok(data);
        }

        public static IApiResult Put(LiteDbContext liteDbContext, [FromBody] Model.Password data)
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
