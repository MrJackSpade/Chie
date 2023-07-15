using Microsoft.Extensions.Options;

namespace LlamaApi.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static void RegisterSecret<T>(this IServiceCollection collection, IConfiguration source) where T : class
        {
            collection.Configure<T>(source.GetSection(typeof(T).Name));
            collection.AddSingleton(s => s.GetService<IOptions<T>>().Value);
        }
    }
}