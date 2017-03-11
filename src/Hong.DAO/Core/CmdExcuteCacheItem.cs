using System.Collections.Concurrent;
using System.Data.Common;

namespace Hong.DAO.Core
{
    public class CmdExcuteCacheItem
    {
        ConcurrentStack<DbCommand> _commands = new ConcurrentStack<DbCommand>();

        public DbCommand GetCommand(SessionConnection conn)
        {
            DbCommand cmd = null;

            if (_commands.TryPop(out cmd))
            {
                cmd.Connection = conn.CurrentDbConnection;
                return cmd;
            }

            return conn.CurrentDbConnection.CreateCommand();
        }

        public void Push(DbCommand command)
        {
            command.Connection = null;

            _commands.Push(command);
        }
    }
}
