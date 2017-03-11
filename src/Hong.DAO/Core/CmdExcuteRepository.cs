using System.Collections.Generic;

namespace Hong.DAO.Core
{
    public class CmdExcuteRepository
    {
        private Dictionary<string, CmdExcuteCacheItem> queryCommands = new Dictionary<string, CmdExcuteCacheItem>();

        public CmdExcuteCacheItem GetRepository(string cmdKey)
        {
            CmdExcuteCacheItem item = null;

            if (queryCommands.TryGetValue(cmdKey,out item))
            {
                return item;
            }

            lock (queryCommands)
            {
                item = new CmdExcuteCacheItem();

                try
                {
                    queryCommands.Add(cmdKey, item);
                }
                catch { }

                return item;
            }
        }
    }
}
