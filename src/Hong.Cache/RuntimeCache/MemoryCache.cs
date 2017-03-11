using System;
using System.Collections.Concurrent;
using System.Linq;
using Hong.Cache.Core;
using static Hong.Common.Extendsion.Guard;
using Microsoft.Extensions.Logging;

namespace Hong.Cache.RuntimeCache
{
    public class MemoryCache<TValue> : Cache<TValue>
    {
        private ConcurrentDictionary<string, CacheItem<TValue>> _cache = new ConcurrentDictionary<string, CacheItem<TValue>>();

        readonly ILogger log = null;

        internal MemoryCache(MemoryCacheConfiguration configuration, ILoggerFactory loggerFactory = null)
        {
            log = loggerFactory?.CreateLogger("MemoryCache");
        }

        public override async System.Threading.Tasks.Task<bool> TryClear()
        {
            return await new System.Threading.Tasks.Task<bool>(() =>
            {
                _cache.Clear();
                return true;
            });
        }

        public override async System.Threading.Tasks.Task<bool> TryClear(string region)
        {
            return await new System.Threading.Tasks.Task<bool>(() =>
            {
                var key = region + RegionKeyDecollator;
                CacheItem<TValue> cacheItem = null;

                foreach (var item in _cache
                    .Where(p => p.Key.StartsWith(key, StringComparison.Ordinal)))
                {
                    _cache.TryRemove(item.Key, out cacheItem);
                }

                return true;
            });
        }

        public override TValue TryGet(string key, string region)
        {
            if (!string.IsNullOrWhiteSpace(region))
            {
                key = BuildKey(key, region);
            }

            CacheItem<TValue> cacheItem = null;

            if (_cache.TryGetValue(key, out cacheItem))
            {
                if (cacheItem.Expired)
                {
                    TryRemove(key, region);
                    return default(TValue);
                }

                cacheItem.LastAccessedUtc = DateTime.Now;

                return cacheItem.Value;
            }

            return default(TValue);
        }

        public override bool TryRemove(string key, string region)
        {
            NotNullOrEmpty(key, nameof(key));

            CacheItem<TValue> cacheItem = null;

            if (!string.IsNullOrWhiteSpace(region))
            {
                key = BuildKey(key, region);
            }

            return _cache.TryRemove(key, out cacheItem);
        }

        public override short TrySet(string key, string region, TValue value, long version, ExpirationMode expirationMode = ExpirationMode.None, int cacheTimeSpan = 0)
        {
            if (!string.IsNullOrWhiteSpace(region))
            {
                key = BuildKey(key, region);
            }

            var cacheItem = Get(key);
            if (cacheItem == null)
            {
                cacheItem = new CacheItem<TValue>(key, region, value, version, expirationMode, cacheTimeSpan);
                try
                {
                    if (_cache.TryAdd(key, cacheItem))
                    {
                        return 1;
                    }
                }
                catch (OverflowException ex)
                {
                    log?.LogError("#Method =>TrySet =>TryAdd", ex);
                    return 0;
                }
            }
            else if (System.Threading.Monitor.TryEnter(cacheItem, 10))
            {
                if (version <= cacheItem.Version)
                {
                    System.Threading.Monitor.Exit(cacheItem);
                    return -1;
                }

                cacheItem.Reset(value, version, expirationMode, cacheTimeSpan);
                System.Threading.Monitor.Exit(cacheItem);

                return 1;
            }
            else
            {
                log?.LogError("#Method =>TrySet =>Monitor.TryEnter", "等待超时");
            }

            return 0;
        }

        public override bool TryAdd(string key, string region, TValue value, long version, ExpirationMode expirationMode = ExpirationMode.None, int cacheTimeSpan = 0)
        {
            if (!string.IsNullOrWhiteSpace(region))
            {
                key = BuildKey(key, region);
            }

            var item = new CacheItem<TValue>(key, region, value, version, expirationMode, cacheTimeSpan);

            try
            {
                return _cache.TryAdd(key, item);
            }
            catch (OverflowException ex)
            {
                log?.LogError("#Method =>TryAdd =>TryAdd", ex);
                return false;
            }
        }

        public override bool TryExpire(string key, string region, ExpirationMode expirationMode, int cacheTimeSpan)
        {
            if (!string.IsNullOrWhiteSpace(region))
            {
                key = BuildKey(key, region);
            }

            var cacheItem = Get(key);
            if (cacheItem == null) return false;

            cacheItem.ReSetExpiration(expirationMode, cacheTimeSpan);

            return true;
        }

        public override short TryUpdate(string key, string region, TValue value, long version)
        {
            if (!string.IsNullOrWhiteSpace(region))
            {
                key = BuildKey(key, region);
            }

            var cacheItem = Get(key);
            if (cacheItem == null) return 0;

            if (System.Threading.Monitor.TryEnter(cacheItem, 10))
            {
                if (version <= cacheItem.Version)
                {
                    System.Threading.Monitor.Exit(cacheItem);
                    return -1;
                }

                cacheItem.Value = value;
                cacheItem.LastAccessedUtc = DateTime.Now;
                cacheItem.Version = version;
                System.Threading.Monitor.Exit(cacheItem);

                return 1;
            }
            else
            {
                log?.LogError("#Method =>TryUpdate =>Monitor.TryEnter", "等待超时");
            }

            return 0;
        }

        CacheItem<TValue> Get(string key)
        {
            CacheItem<TValue> cacheItem = null;

            if (_cache.TryGetValue(key, out cacheItem))
            {
                return cacheItem;
            }

            return null;
        }

        /// <summary>清理已过期项目
        /// </summary>
        void ClearExpiredItem()
        {
            var removed = 0;

            foreach (var item in _cache.Values)
            {
                if (item.Expired)
                {
                    TryRemove(item.Key); removed++;
                }
            }

            if (removed > 0)
            {

            }
        }
    }
}
