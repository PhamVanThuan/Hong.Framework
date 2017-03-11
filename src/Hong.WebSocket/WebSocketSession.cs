using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Hong.Common.Extendsion.Guard;

namespace Hong.WebSocket
{
    public class WebSocketSession
    {
        /// <summary>唯一标识
        /// </summary>
        public string Identity = string.Empty;

        /// <summary>开始时间
        /// </summary>
        public DateTime StartTime = DateTime.Now;

        /// <summary>最后消息时间
        /// </summary>
        public DateTime LastMessageTime = DateTime.Now;

        /// <summary>关联标识
        /// </summary>
        public string AssociatedIdentity = string.Empty;

        /// <summary>WebSocket
        /// </summary>
        public System.Net.WebSockets.WebSocket WebSocket = null;

        private ILogger Log;

        public WebSocketSession(ILogger log)
        {
            Log = log;
        }

        /// <summary>发送消息给某客户端
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task Send(string message)
        {
            NotNull(WebSocket, nameof(WebSocket));
            NotNullOrEmpty(message, nameof(message));

            LastMessageTime = DateTime.Now;
            ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));

            try
            {
                return WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log?.LogError("#WebSockert =>发送消息失败, 内容:" + message, ex);
                throw;
            }
        }

        /// <summary>关闭连接
        /// </summary>
        public Task Close(WebSocketCloseStatus closeCode = WebSocketCloseStatus.MandatoryExtension, string message = "服务器维护关闭")
        {
            NotNull(WebSocket, nameof(WebSocket));
            if (message == null) message = string.Empty;

            try
            {
                return WebSocket.CloseAsync(closeCode, message, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log?.LogError("#WebSocket =>关闭连接失败", ex);
                throw;
            }
        }
    }
}
