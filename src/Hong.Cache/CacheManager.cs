using Hong.Cache.Configuration;
using Hong.Cache.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Hong.Common.Extendsion;
using Hong.Common.Tools;
using static Hong.Common.Extendsion.Guard;
using Microsoft.Extensions.Logging;

namespace Hong.Cache
{
    /*
     * 每次获取返回的对象版本加1
     * 添加或更新时缓存时使用当前版本, 操作成功后当前对象的版本加1
     */
    public class CacheManager<TValue> : ICacheManager<TValue>
    {
        internal CacheManager(CacheConfiguration configuration, string localCacheConfigKey = null, string remoteCacheConfigKey = null, ILoggerFactory loggerFactory = null)
        {
            LocalCache = configuration.CreateLocalHandle<byte[]>(localCacheConfigKey);
            RemoteCaches = configuration.CreateRemoteHandle<byte[]>(remoteCacheConfigKey);

            MessageManager = configuration.MessageManager;
            if (MessageManager != null)
            {
                MessageManager.OnUpdate += OnUpdate;
                MessageManager.OnRemove += OnRemove;
                MessageManager.OnClear += OnClear;
            }

            var type = typeof(TValue);
            CacheSet = type.GetTypeInfo().GetCustomAttribute<CacheSetAttribute>();
            if (CacheSet == null)
            {
                throw new Exception(type.FullName + "必须添加CacheSet属性设置");
            }

            var fInfo = type.GetTypeInfo().GetField("Version");
            if (fInfo != null)
            {
                GetVersion = Reflection.GetField<TValue, int>(fInfo);
            }
            else
            {
                GetVersion = Reflection.GetProperty<TValue, int>("Version");
            }

            Log = loggerFactory?.CreateLogger("CacheManager");
        }

        ILogger Log;

        /// <summary>订阅消息
        /// </summary>
        SyncMessageManager MessageManager;

        /// <summary>本地缓存
        /// </summary>
        ICache<byte[]> LocalCache;

        /// <summary>远程缓存
        /// </summary>
        ICache<byte[]> RemoteCaches;

        /// <summary>远程缓存失败重试间隔时间(单位毫秒)
        /// </summary>
        public int RemoteTryWait = 10;

        /// <summary>缓存设置
        /// </summary>
        CacheSetAttribute CacheSet;

        /// <summary>获取版本号
        /// </summary>
        Func<TValue, int> GetVersion;

        /// <summary>Session缓存
        /// </summary>
        public SessionCache SessionCache
        {
            get
            {
                return ServiceProvider.GetRequestServices<SessionCache>();
            }
        }

        #region TryClear

        public async Task<bool> TryClear()
        {
            if (LocalCache != null && !await LocalCache.TryClear())
            {
                return false;
            }

            if (RemoteCaches != null && !await Try(() => RemoteCaches.TryClear(), RemoteCaches.TryCount))
            {
                return false;
            }

            return true;
        }

        private async Task<bool> TryClearLocal()
        {
            if (LocalCache != null && !await LocalCache.TryClear())
            {
                return false;
            }

            return true;
        }

        public async Task<bool> TryClear(string region)
        {
            if (LocalCache != null && !await LocalCache.TryClear(region))
            {
                return false;
            }

            if (RemoteCaches != null &&
                !await Try(() => RemoteCaches.TryClear(region), RemoteCaches.TryCount))
            {
                return false;
            }

            MessageManager?.NotifyClear(region);
            return true;
        }

        private async Task<bool> TryClearLocal(string region)
        {
            if (string.IsNullOrEmpty(region))
            {
                if (LocalCache != null && !await LocalCache.TryClear())
                {
                    return false;
                }
            }
            else if (LocalCache != null && !await LocalCache.TryClear(region))
            {
                return false;
            }

            return true;
        }
        #endregion

        #region TryGet

        public TValue TryGet(string key) => TryGet(key, CacheSet.DefaultRegion);

        public TValue TryGet(string key, string region)
        {
            TValue obj = default(TValue);
            byte[] bytes = null;
            var sessionCache = SessionCache;

            #region Session缓存

            if (sessionCache != null)
            {
                obj = sessionCache.Get<TValue>(key);

                if (obj != null)
                {
                    return obj;
                }
            }

            #endregion

            #region 本地缓存

            if (LocalCache != null)
            {
                bytes = LocalCache.TryGet(key, region);
                if (bytes != null)
                {
                    obj = bytes.Deserialize<TValue>();
                    if (obj == null)
                    {
                        Log?.LogError("#Method =>TryGet", "反序化失败, 类型:" + typeof(TValue).FullName);
                        throw new Exception("反序化失败");
                    }

                    sessionCache?.Set(key, obj);

                    return obj;
                }
            }

            #endregion

            #region 远程缓存

            if (RemoteCaches != null)
            {
                bytes = Try(() => RemoteCaches.TryGet(key, region), RemoteCaches.TryCount);
                if (bytes != null)
                {
                    obj = bytes.Deserialize<TValue>();

                    if (obj == null)
                    {
                        Log?.LogError("#Method =>TryGet", "反序化失败, 类型:" + typeof(TValue).FullName);
                        throw new Exception("反序化失败");
                    }
                }
            }

            #endregion

            if (obj == null)
            {
                return default(TValue);
            }
            var version = GetVersion(obj);

            #region 放置本地缓存

            if (LocalCache != null)
            {
                if (LocalCache.TrySet(key, region, bytes, version, CacheSet.ExpirationMode, CacheSet.LocalCacheTime) != 1)
                {
                    return default(TValue);
                }
            }

            #endregion

            sessionCache?.Set(key, obj);

            return obj;
        }

        #endregion

        #region TryRemove

        public bool TryRemove(string key) => TryRemove(key, CacheSet.DefaultRegion);

        public bool TryRemove(string key, string region)
        {
            bool localRemoved = false;

            if (LocalCache != null)
            {
                if (!LocalCache.TryRemove(key, region))
                {
                    return false;
                }

                localRemoved = true;
            }

            if (RemoteCaches == null)
            {
                return true;
            }

            if (!Try(() => RemoteCaches.TryRemove(key, region), RemoteCaches.TryCount))
            {
                return false;
            }

            if (localRemoved)
            {
                MessageManager?.NotifyRemove(key, region);
            }

            return true;
        }

        bool TryRemoveLocalCache(string key, string region)
        {
            if (LocalCache != null && !LocalCache.TryRemove(key, region))
            {
                return false;
            }

            return true;
        }

        #endregion

        #region TrySet

        public short TrySet(string key, TValue value) => TrySet(key, CacheSet.DefaultRegion, value);

        public short TrySet(string key, string region, TValue value)
        {
            var version = GetVersion(value);
            short result = 0;
            var serializedValue = value.Serialize();
            bool localSeted = false;

            if (LocalCache != null)
            {
                result = LocalCache.TrySet(key, region, serializedValue, version, CacheSet.ExpirationMode, CacheSet.LocalCacheTime);
                if (result != 1)
                {
                    return result;
                }

                localSeted = true;
            }

            if (RemoteCaches != null)
            {
                result = Try(() => RemoteCaches.TrySet(key, region, serializedValue, version, CacheSet.ExpirationMode, CacheSet.RemoteCacheTime), RemoteCaches.TryCount);
            }

            if (localSeted && result == 1)
            {
                MessageManager?.NotifyUpdate(key, region);
            }

            return result;
        }

        #endregion

        #region TryUpdate

        public short TryUpdate(string key, TValue value) => TryUpdate(key, CacheSet.DefaultRegion, value);

        public short TryUpdate(string key, string region, TValue value)
        {
            short result = 1;
            var serializedValue = value.Serialize();
            var version = GetVersion(value);
            bool localUpdated = false;

            if (LocalCache != null)
            {
                result = LocalCache.TryUpdate(key, region, serializedValue, version);
                if (result != 1)
                {
                    return result;
                }

                localUpdated = true;
            }

            if (RemoteCaches != null)
            {
                result = Try(() => RemoteCaches.TryUpdate(key, region, serializedValue, version), RemoteCaches.TryCount);
            }

            if (localUpdated && result == 1)
            {
                MessageManager?.NotifyUpdate(key, region);
            }

            return result;
        }

        #endregion

        #region TryAdd

        public bool TryAdd(string key, TValue value) => TryAdd(key, CacheSet.DefaultRegion, value);

        public bool TryAdd(string key, string region, TValue value)
        {
            var serializedValue = value.Serialize();
            var version = GetVersion(value);

            if (LocalCache != null && !LocalCache.TryAdd(key, region, serializedValue, version, CacheSet.ExpirationMode, CacheSet.LocalCacheTime))
            {
                return false;
            }

            bool result = true;
            if (RemoteCaches != null)
            {
                result = Try(() => RemoteCaches.TryAdd(key, region, serializedValue, version, CacheSet.ExpirationMode, CacheSet.RemoteCacheTime), RemoteCaches.TryCount);
            }

            return result;
        }

        #endregion

        #region TryExpire

        public bool TryExpire(string key, ExpirationMode expirationMode, int localCacheTimeSpan, int remoteCacheTimeSpan)
            => TryExpire(key, CacheSet.DefaultRegion, expirationMode, localCacheTimeSpan, remoteCacheTimeSpan);

        public bool TryExpire(string key, string region, ExpirationMode expirationMode, int localCacheTimeSpan, int remoteCacheTimeSpan)
        {
            if (LocalCache != null && !LocalCache.TryExpire(key, region, expirationMode, localCacheTimeSpan))
            {
                return false;
            }

            if (RemoteCaches == null)
            {
                return true;
            }

            return Try(() => RemoteCaches.TryExpire(key, region, expirationMode, remoteCacheTimeSpan), RemoteCaches.TryCount);
        }

        #endregion

        #region Flush
        public short FlushUpdate(string key) => FlushUpdate(key, CacheSet.DefaultRegion);
        public short FlushUpdate(string key, string region)
        {
            var sessionCache = SessionCache;
            NotNull(sessionCache, nameof(SessionCache));

            var value = sessionCache.Get<TValue>(key);
            if (value == null)
            {
                return 1;
            }

            return TryUpdate(key, region, value);
        }

        public bool FlushAdd(string key) => FlushAdd(key, CacheSet.DefaultRegion);
        public bool FlushAdd(string key, string region)
        {
            var sessionCache = SessionCache;
            NotNull(sessionCache, nameof(SessionCache));

            var value = sessionCache.Get<TValue>(key);
            if (value == null)
            {
                return true;
            }

            return TryAdd(key, region, value);
        }
        #endregion

        #region 重试执行

        /// <summary>重试操作
        /// </summary>
        /// <param name="func">操作</param>
        /// <param name="tryCount">重试次数</param>
        /// <returns>true 成功, false 失败</returns>
        bool Try(Func<bool> func, int tryCount)
        {
            if (func())
            {
                return true;
            }

            while (tryCount-- > 0)
            {
                Task.Delay(RemoteTryWait).Wait();

                if (func())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>重试操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">操作</param>
        /// <param name="tryCount">重试次数</param>
        /// <returns>返回操作结果</returns>
        T Try<T>(Func<T> func, int tryCount)
        {
            var result = func();
            if (result != null)
            {
                return result;
            }

            while (tryCount-- > 0)
            {
                Task.Delay(RemoteTryWait).Wait();

                result = func();
                if (result != null)
                {
                    return result;
                }
            }

            Log?.LogWarning("#Method =>Try", "已超超过重试次数");
            return default(T);
        }

        /// <summary>重试操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">操作</param>
        /// <param name="tryCount">重试次数</param>
        /// <returns>返回操作结果</returns>
        async Task<T> Try<T>(Func<Task<T>> func, int tryCount)
        {
            var result = await func();
            if (result != null)
            {
                return result;
            }

            while (tryCount-- > 0)
            {
                Task.Delay(RemoteTryWait).Wait();

                result = await func();
                if (result != null)
                {
                    return result;
                }
            }

            Log?.LogWarning("#Method =>Try", "已超超过重试次数");
            return default(T);
        }

        #endregion

        #region 订阅消息
        void OnUpdate(object sender, SyncMessage msg)
        {
#if DEBUG
            Log?.LogInformation("#Method =>OnUpdate", "Key:{" + msg.Key + "}, Region:{" + msg.Region + "}");
#endif
            TryRemoveLocalCache(msg.Key, msg.Region);
        }

        void OnRemove(object sender, SyncMessage msg)
        {
#if DEBUG
            Log?.LogInformation("#Method =>OnRemove", "Key:{" + msg.Key + "}, Region:{" + msg.Region + "}");
#endif
            TryRemoveLocalCache(msg.Key, msg.Region);
        }

        async void OnClear(object sender, SyncMessage msg)
        {
#if DEBUG
            Log?.LogInformation("#Method =>OnClearRegion", "Region:{" + msg.Region + "}");
#endif
            await TryClearLocal(msg.Region);
        }
        #endregion
    }
}
