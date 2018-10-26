using KingNetwork.Server.Interfaces;
using KingNetwork.Shared;
using System;
using System.Net.Sockets;

namespace KingNetwork.Server
{
    /// <summary>
    /// This class is responsible for represents the client.
    /// </summary>
    public class Client : IClient
    {
        #region private members

        /// <summary>
        /// The tcp client instance from client.
        /// </summary>
        private readonly TcpClient _tcpClient;

        /// <summary>
        /// The buffer of client connection.
        /// </summary>
        private readonly byte[] _buffer;

        /// <summary>
        /// The stream of tcp client.
        /// </summary>
        private NetworkStream _stream;
        
        /// <summary>
        /// The callback of message received handler implementation.
        /// </summary>
        private readonly MessageReceivedHandler _messageReceivedHandler;

        /// <summary>
        /// The callback of client disconnected handler implementation.
        /// </summary>
        private readonly ClientDisconnectedHandler _clientDisconnectedHandler;

        #endregion

        #region properties

        /// <summary>
        /// The identifier number of client.
        /// </summary>
        public ushort Id { get; set; }

        /// <summary>
        /// The ip address of connected client.
        /// </summary>
        public string IpAddress { get; }

        /// <summary>
		/// The flag of client connection.
		/// </summary>
		public bool IsConnected => _tcpClient.IsConnected();

        #endregion

        #region delegates

        /// <summary>
		/// The delegate of message received handler from client connection.
		/// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="kingBuffer">The king buffer of received message.</param>
        public delegate void MessageReceivedHandler(IClient client, KingBuffer kingBuffer);

        /// <summary>
		/// The delegate of client disconnected handler connection.
		/// </summary>
        /// <param name="id">The identifier number of client instance.</param>
        public delegate void ClientDisconnectedHandler(ushort id);

        #endregion

        #region constructors

        /// <summary>
        /// Creates a new instance of a <see cref="Client"/>.
        /// </summary>
        /// <param name="id">The identifier number of connected client.</param>
        /// <param name="tcpClient">The tcp client from connected client.</param>
        /// <param name="messageReceivedHandler">The callback of message received handler implementation.</param>
        /// <param name="clientDisconnectedHandler">The callback of client disconnected handler implementation.</param>
        /// <param name="maxMessageBuffer">The max length of message buffer.</param>
        public Client(ushort id, TcpClient tcpClient, MessageReceivedHandler messageReceivedHandler, ClientDisconnectedHandler clientDisconnectedHandler, ushort maxMessageBuffer)
        {
            try
            {
                _tcpClient = tcpClient;
                _messageReceivedHandler = messageReceivedHandler;
                _clientDisconnectedHandler = clientDisconnectedHandler;

                _tcpClient.ReceiveBufferSize = maxMessageBuffer;
                _tcpClient.SendBufferSize = maxMessageBuffer;
                _buffer = new byte[maxMessageBuffer];
                _stream = _tcpClient.GetStream();

                Id = id;
                IpAddress = _tcpClient.Client.RemoteEndPoint.ToString();

                _stream.BeginRead(_buffer, 0, _tcpClient.ReceiveBufferSize, new AsyncCallback(ReceiveDataCallback), null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }

        #endregion

        #region public methods implementation

        /// <summary>
        /// Method responsible for send message to client.
        /// </summary>
        /// <param name="kingBuffer">The king buffer of received message.</param>
        public void SendMessage(KingBuffer kingBuffer)
        {
            try
            {
                if (IsConnected)
                {
                    _stream.Write(kingBuffer.ToArray(), 0, kingBuffer.Length());
                    _stream.Flush();

                    Console.WriteLine($"Message sent to client {Id}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }

        #endregion

        #region private methods implementation
        
        /// <summary> 	
        /// The callback from received message from connected client. 	
        /// </summary> 	
        /// <param name="asyncResult">The async result from a received message from connected client.</param>
        private void ReceiveDataCallback(IAsyncResult asyncResult)
        {
            try
            {
                if (IsConnected)
                {
                    var endRead = _stream.EndRead(asyncResult);

                    var numArray = new byte[endRead];
                    if (endRead != 0)
                    {
                        Buffer.BlockCopy(_buffer, 0, numArray, 0, endRead);

                        _stream.BeginRead(_buffer, 0, _tcpClient.ReceiveBufferSize, new AsyncCallback(ReceiveDataCallback), null);

                        Console.WriteLine($"Received message from client '{IpAddress}'.");

                        _messageReceivedHandler(this, new KingBuffer(_buffer));

                        return;
                    }
                }

                _tcpClient.Close();
                Console.WriteLine($"Client '{IpAddress}' Disconnected.");

                _clientDisconnectedHandler(Id);
            }
            catch (Exception ex)
            {
                _tcpClient.Close();
                Console.WriteLine($"Client '{IpAddress}' Disconnected.");
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }

        #endregion
    }
}
