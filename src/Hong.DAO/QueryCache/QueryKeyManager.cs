using Hong.Cache;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Hong.Common.Extendsion;
using System;
using System.Collections.Concurrent;

namespace Hong.DAO.QueryCache
{
    /// <summary>查询缓存管理
    /// </summary>
    public class QueryKeyManager
    {
        /// <summary>SQLKey缓存管理
        /// </summary>
        static readonly ICacheManager<SQLKeyItem> sqlKeyCache = CacheFactory.CreateCacheManager<SQLKeyItem>();

        /// <summary>记录是否已分析SQL
        /// </summary>
        static readonly Dictionary<string, string> querySQLKeys = new Dictionary<string, string>();

        /// <summary>截取表名
        /// </summary>
        static readonly Regex regexTableName = new Regex(@"(from|join)\s+([^\(^\s]+)", RegexOptions.IgnoreCase);

        /// <summary>判断参数是否数字相关
        /// </summary>
        static readonly Regex regexNumberAlphabet = new Regex(@"^[0-9\s,\./]*$");


        /// <summary>获取 SQL 语句的表名
        /// </summary>
        /// <param name="sql">SQL 语句</param>
        /// <returns>返回 SQL 关联的表名</returns>
        List<string> ParseTableName(string sql)
        {
            List<string> tables = new List<string>();

            var matchs = regexTableName.Matches(sql);
            for (var index = 0; index < matchs.Count; index++)
            {
                tables.Add(matchs[index].Groups[2].Value);
            }

            return tables;
        }

        /// <summary>获取 SQL 语句 Key
        /// </summary>
        /// <param name="sql">SQL 语句</param>
        /// <returns>返回 SQL 语句对应的 MD5</returns>
        public string GetSQLKey(string sql)
        {
            string sqlKey = null;

            if (querySQLKeys.TryGetValue(sql, out sqlKey))
            {
                return sqlKey;
            }

            lock (regexTableName)
            {
                if (querySQLKeys.TryGetValue(sql, out sqlKey))
                {
                    return sqlKey;
                }

                sqlKey = Security.GetMD532(sql);

                //保存SQL和Table关系到缓存,本地不缓存,只用于清除Talbe相关SQL的缓存使用
                foreach (var table in ParseTableName(sql))
                {
                    if (sqlKeyCache.TrySet(sqlKey, table, new SQLKeyItem() { SQLKey = sqlKey }) == 0)
                    {
                        throw new Exception("更新查询缓存Table_SQLKey失败");
                    }
                }

                querySQLKeys.Add(sql, sqlKey);

                return sqlKey;
            }
        }

        /// <summary>获取查询 Key
        /// </summary>
        /// <param name="sql">SQL 语句</param>
        /// <param name="sqlKey">返回 SQL 语句 Key</param>
        /// <param name="ps">查询参数</param>
        /// <returns>返回查询 Key</returns>
        public string GetQueryKey(string sql,string sqlKey, params object[] ps)
        {
            string key = sqlKey;

            if (ps == null)
            {
                key = sqlKey + "/0";
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in ps)
                {
                    sb.Append(item.ToString());
                }

                key = sb.ToString();
                sb = null;

                //查询参数存在非数字时不缓存，此时应使用其他查询方式或方案
                if (!regexNumberAlphabet.IsMatch(key))
                {
                    return null;
                }

                key = sqlKey + "/" + key;

                //长度超过150时MD5压缩
                if (key.Length > 150)
                {
                    key = Security.GetMD532(key);
                }
            }

            return key;
        }
    }
}
