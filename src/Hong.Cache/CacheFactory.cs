using Hong.Cache.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Hong.Cache
{
    public class CacheFactory
    {
        static CacheConfiguration _CacheConfig = null;
        static object _locker = new object();

        static CacheConfiguration GetConfig()
        {
            if (_CacheConfig != null)
            {
                return _CacheConfig;
            }

            lock (_locker)
            {
                if (_CacheConfig != null)
                {
                    return _CacheConfig;
                }

                if (!File.Exists("web.json"))
                {
                    throw new FileNotFoundException("没有找到配置文件web.json");
                }

                var config = new ConfigurationBuilder()
                    .AddJsonFile("web.json")
                    .Build();

                _CacheConfig = new CacheConfiguration(config);
            }

            return _CacheConfig;
        }

        /// <summary>创建缓存管理
        /// </summary>
        /// <returns></returns>
        public static ICacheManager<TValue> CreateCacheManager<TValue>(string localCacheConfigKey = null, string remoteCacheConfigKey = null)
        {
            return new CacheManager<TValue>(GetConfig(), localCacheConfigKey, remoteCacheConfigKey);
        }
    }
}
