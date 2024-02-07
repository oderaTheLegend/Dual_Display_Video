using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <summary>
/// Manages the TCP server operations, including starting the server, listening for connections,
/// and processing incoming messages.
/// </summary>
public class TCPServerManager : MonoBehaviour
{
    [Header("Server Configuration")]
    [Tooltip("Port number on which the TCP server listens.")]
    private int port = 3000;
    private string ipAddress = "127.0.0.1";
    private bool serverRunning = false;
    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    private TcpClient connectedTcpClient;
    private VideoConfig videoConfig;
    public static event Action<string> OnCommandReceived;

    private void Awake()
    {
        videoConfig = VideoConfig.Load();
        ipAddress = videoConfig.ipAddress;
        port = videoConfig.port;
    }

    void Start() => InitializeServer();

    void OnApplicationQuit() => ShutdownServer();

    /// <summary>
    /// Initializes the TCP server and starts listening for incoming connections.
    /// </summary>
    private void InitializeServer()
    {
        serverRunning = true;
        tcpListenerThread = new Thread(() =>
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(this.ipAddress);
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                Debug.Log("Server started.");

                while (serverRunning)
                {
                    TcpClient connectedTcpClient = tcpListener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, connectedTcpClient);
                }
            }
            catch (SocketException e)
            {
                Debug.LogError($"SocketException: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start server: {e.Message}. Retrying in 5 seconds...");
                Thread.Sleep(5000);
            }
        })
        {
            IsBackground = true
        };
        tcpListenerThread.Start();
    }

    /// <summary>
    /// Listens for incoming TCP connections and processes incoming messages.
    /// </summary>
    private void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        try
        {
            using NetworkStream stream = client.GetStream();
            byte[] bytes = new byte[1024];
            int length;
            while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                string clientMessage = Encoding.ASCII.GetString(bytes, 0, length);
                UnityMainThreadDispatcher.Instance.Enqueue(() => OnCommandReceived?.Invoke(clientMessage));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling client: {e.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    /// <summary>
    /// Shuts down the server and releases all resources.
    /// </summary>
    private void ShutdownServer()
    {
        tcpListenerThread?.Abort();
        tcpListener?.Stop();
        connectedTcpClient?.Close();
        Debug.Log("Server shut down.");
    }
}