using Hong.Cache;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hong.DAO.QueryCache
{
    public class QueryCacheManager
    {
        /// <summary>查询结果缓存管理
        /// </summary>
        static readonly ICacheManager<CacheItem> cache = CacheFactory.CreateCacheManager<CacheItem>();

        /// <summary>并发限制信息
        /// </summary>
        static readonly ConcurrentDictionary<string, ConcurrentLimitInfo> concurrentLimitInfos = new ConcurrentDictionary<string, ConcurrentLimitInfo>();

        /// <summary>并发锁
        /// </summary>
        SpinLock concurrentLimitLock = new SpinLock();

        /// <summary>缓存 KEY 管理
        /// </summary>IOKYU,MN 
        readonly QueryKeyManager queryKeyManager = new QueryKeyManager();

        /// <summary>获取缓存数据
        /// </summary>
        /// <param name="queryData">查询数据方法</param>
        /// <param name="sql">SQL 语句</param>
        /// <param name="arguments">SQL 参数</param>
        /// <returns>返回查询结果</returns>
        public async Task<List<int>> GetCacheData(Func<Task<List<int>>> queryData, string sql, params object[] arguments)
        {
            var sqlKey = queryKeyManager.GetSQLKey(sql);
            var queryKey = queryKeyManager.GetQueryKey(sql, sqlKey, arguments);

            //return await ConcurrentLimit(queryKey, async () =>
            //{
            var cacheItem = cache.TryGet(queryKey, sqlKey);
            if (cacheItem != null)
            {
                return cacheItem.IDs ?? new List<int>();
            }

            var data = await queryData();
            if (cache.TrySet(queryKey, sqlKey, new CacheItem { IDs = data }) == 0)
            {
                //日志
            }

            return data;
            //});
        }

        async Task<List<int>> ConcurrentLimit(string key, Func<Task<List<int>>> GetData)
        {
            //SemaphoreSlim
            var hashCode = Thread.CurrentThread.GetHashCode();
            var locked = false;

            var limitInfo = concurrentLimitInfos.GetOrAdd(key, (item) => new ConcurrentLimitInfo());

            concurrentLimitLock.TryEnter(ref locked);
            if (limitInfo.WorkFinished)
            {
                //初始化批次
                limitInfo.WorkFinished = false;
                limitInfo.FirstThreadHashCode = hashCode;
                limitInfo.WaitThreadCount = 0;
                limitInfo.TempCacheQueryResult = null;
                limitInfo.Exception = null;
            }
            else
            {
                limitInfo.WaitThreadCount++;
            }
            if (locked) concurrentLimitLock.Exit();

            //等待第一个线程查询数据完成
            if (limitInfo.FirstThreadHashCode != hashCode)
            {
                while (limitInfo.FirstThreadHashCode != -1)
                {
                    SpinWait.SpinUntil(() => limitInfo.FirstThreadHashCode == -1, 1);
                }

                Interlocked.Decrement(ref limitInfo.ReleaseThreadCount);
                if (limitInfo.Exception != null)
                {
                    throw limitInfo.Exception;
                }

                return limitInfo.TempCacheQueryResult;
            }

            try
            {
                limitInfo.TempCacheQueryResult = await GetData();
            }
            catch (Exception ex)
            {
                limitInfo.Exception = ex;
                limitInfo.TempCacheQueryResult = null;
            }

            //获取一个时间截断上的线程数量
            var count = limitInfo.WaitThreadCount;
            limitInfo.ReleaseThreadCount = count;
            //释放等待线程
            limitInfo.FirstThreadHashCode = -1;

            //等待批次释放完成, 从理论上实现释放数量可能会超过等释放数量但不影响执行
            if (count > 0)
            {
                while (limitInfo.ReleaseThreadCount > 0)
                {
                    SpinWait.SpinUntil(() => limitInfo.ReleaseThreadCount <= 0, 1);
                }

                //延时等待完成
                SpinWait.SpinUntil(() => false, 1);
            }
            //关闭释放,开启下一批次线程
            limitInfo.WorkFinished = true;

            if (limitInfo.Exception != null)
            {
                throw limitInfo.Exception;
            }
            return limitInfo.TempCacheQueryResult;
        }

        /// <summary>清除过期缓存, 如果缓存的内存足够可以不清理让其自动过期
        /// 此用于后台线程清理,建议单独的服务去清理
        /// </summary>
        public void ClearOveredCache()
        {

        }
    }
}
