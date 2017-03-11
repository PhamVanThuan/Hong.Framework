using System.Collections.Generic;

namespace Hong.Cache.Core
{
    /// <summary>请求域缓存
    /// </summary>
    public class SessionCache
    {
        private Dictionary<string, object> _caches = new Dictionary<string, object>();

        public T Get<T>(string key)
        {
            object obj;

            if (_caches.TryGetValue(key, out obj))
            {
                return (T)obj;
            }

            return default(T);
        }

        public void Remove(string urlKey)
        {
            _caches.Remove(urlKey);
        }

        public void Set(string key, object value)
        {
            if (_caches.ContainsKey(key))
            {
                return;
            }

            _caches.Add(key, value);
        }
    }
}
