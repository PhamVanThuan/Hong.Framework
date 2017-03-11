﻿using Hong.Common.Extendsion;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Hong.DAO.Core
{
    public abstract class DataBase
    {
        protected readonly ILogger Log;

        internal DataBase(ILoggerFactory loggerFactory = null)
        {
            Log = loggerFactory?.CreateLogger("DAO");
        }

        ///// <summary>当前连接
        ///// </summary>
        internal static SessionConnection Connection
        {
            get
            {
                var conn = ServiceProvider.GetRequestServices<SessionConnection>();
                return conn;
            }
        }

        /// <summary>数据库配置
        /// </summary>
        protected DataConfiguration Configuration = DataConfigurationManager.Configuration();

        /// <summary>执行命令
        /// </summary>
        /// <param name="cmd"></param>
        protected async Task<int> ExecuteNonQuery(DbCommand cmd)
        {
#if DEBUG
            Stopwatch watch = new Stopwatch();
            watch.Start();
#endif

            var conn = Connection;
            try
            {
                await conn.Open();
            }
            catch (Exception ex)
            {
                Log?.LogError("#Method =>ExecuteNonQuery =>conn.Open()", ex);
                throw;
            }

#if DEBUG
            watch.Stop();
            var s = watch.Elapsed.TotalMilliseconds;
            watch.Restart();
#endif

            try
            {
                return await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log?.LogError("#Method =>ExecuteNonQuery", ex);
                throw;
            }
            finally
            {
                conn.Close();

#if DEBUG
                watch.Stop();
                s = watch.Elapsed.TotalMilliseconds;
#endif
            }
        }

        /// <summary>执行命令
        /// </summary>
        /// <param name="cmd"></param>
        protected async Task<object> ExecuteScalar(DbCommand cmd)
        {
            var conn = Connection;

#if DEBUG
            Stopwatch watch = new Stopwatch();
            watch.Start();
#endif
            try
            {
                await conn.Open();
            }
            catch (Exception ex)
            {
                Log?.LogError("#Method =>ExecuteNonQuery =>conn.Open()", ex);
                throw;
            }
#if DEBUG
            watch.Stop();
            var s = watch.Elapsed.TotalMilliseconds;
            watch.Restart();
#endif

            try
            {
                return await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log?.LogError("#Method =>ExecuteScalar", ex);
                throw;
            }
            finally
            {
                conn.Close();

#if DEBUG
                watch.Stop();
                s = watch.Elapsed.TotalMilliseconds;
#endif
            }
        }

        /// <summary>通用添加参数
        /// </summary>
        /// <param name="cmd">命令</param>
        /// <param name="values">参数</param>
        protected void AddGeneralParams(DbCommand cmd, object[] values)
        {
            var index = 0;
            var ps = cmd.Parameters;

            foreach (object item in values)
            {
                var p = cmd.CreateParameter();

                p.ParameterName = Configuration.ParamerNameStr + index++;
                p.Value = item;
                p.DbType = ToDbType(item.GetType());
                p.Direction = System.Data.ParameterDirection.Input;

                ps.Add(p);
            }
        }

        System.Data.DbType ToDbType(Type type)
        {
            return (System.Data.DbType)Enum.Parse(typeof(System.Data.DbType), type.Name);
        }

        /// <summary>更新完成事件
        /// </summary>
        public EventHandler<int> UpdatedEvent { get; set; }

        /// <summary>删除完成事件
        /// </summary>
        public EventHandler<int> DeletedEvent { get; set; }

        /// <summary>新增完成事件
        /// </summary>
        public EventHandler<int> AddedEvent { get; set; }
    }
}
