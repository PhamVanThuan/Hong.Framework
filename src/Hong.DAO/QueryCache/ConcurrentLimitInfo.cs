using System;
using System.Collections.Generic;

namespace Hong.DAO.QueryCache
{
    public class ConcurrentLimitInfo
    {
        /// <summary>优先作业的线程标识
        /// </summary>
        public int FirstThreadHashCode;

        /// <summary>等待的线程数量
        /// </summary>
        public volatile int WaitThreadCount = 0;

        /// <summary>本批次作业是否执行完成
        /// </summary>
        public bool WorkFinished = true;

        /// <summary>需释放的线程数量
        /// </summary>
        public int ReleaseThreadCount = 0;

        /// <summary>临时缓存查询结果
        /// </summary>
        public List<int> TempCacheQueryResult;

        /// <summary>异常信息
        /// </summary>
        public Exception Exception;
    }
}
