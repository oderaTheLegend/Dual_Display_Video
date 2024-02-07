using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Manages the TCP connection to the server, including connecting, disconnecting, and sending messages.
/// </summary>
public class TCPConnection : MonoBehaviour
{
    [SerializeField] ClientUIManager uiManager;
    private TcpClient socketConnection;
    private Thread clientThread;
    private bool isRunning = false;

    private string serverIP = "127.0.0.1";
    private int port = 3000;

    [SerializeField] float retryInterval = 5.0f;

    public delegate void MessageReceivedHandler(string message);
    public event MessageReceivedHandler OnMessageReceived;

    void Awake() => LoadConfig();

    void Start() => ConnectToServer();

    /// <summary>
    /// Loads configuration settings from VideoConfig.
    /// </summary>
    private void LoadConfig()
    {
        VideoConfig config = VideoConfig.Load();
        serverIP = config.ipAddress;
        port = config.port;
    }

    /// <summary>
    /// Initiates connection to the server in a separate thread.
    /// </summary>
    public void ConnectToServer()
    {
        if (isRunning)
            return;

        isRunning = true;
        clientThread = new Thread(new ThreadStart(ConnectionThread));
        clientThread.IsBackground = true;
        clientThread.Start();
    }

    /// <summary>
    /// The connection thread method, attempts to connect to the server and retries on failure.
    /// </summary>
    private void ConnectionThread()
    {
        while (isRunning)
        {
            try
            {
                socketConnection = new TcpClient(serverIP, port);
                Debug.Log("Connected to server.");

                UnityMainThreadDispatcher.Instance.Enqueue(() => uiManager.UpdateConnectionStatus(true));

                Thread receiveThread = new Thread(ReceiveData);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                break;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect: {ex.Message}");
                Thread.Sleep(TimeSpan.FromSeconds(retryInterval));
            }
        }

        if (!socketConnection.Connected)
            UnityMainThreadDispatcher.Instance.Enqueue(() => uiManager.UpdateConnectionStatus(false));
    }

    /// <summary>
    /// Registers a callback to be invoked when a message is received from the server.
    /// </summary>
    /// <param name="handler">The method to call when a message is received.</param>
    public void RegisterOnMessageReceivedCallback(MessageReceivedHandler handler) => OnMessageReceived += handler;

    private void ReceiveData()
    {
        try
        {
            using NetworkStream stream = socketConnection.GetStream();
            byte[] receivedBuffer = new byte[1024];
            while (isRunning && socketConnection.Connected)
            {
                int bytesRead = stream.Read(receivedBuffer, 0, receivedBuffer.Length);
                if (bytesRead > 0)
                {
                    string receivedMessage = Encoding.ASCII.GetString(receivedBuffer, 0, bytesRead);
                    Debug.Log("Message received: " + receivedMessage);

                    UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        OnMessageReceived?.Invoke(receivedMessage);
                    });
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Receive data exception: " + e.Message);
        }
    }

    /// <summary>
    /// Sends message to server.
    /// </summary>
    public void DispatchMessage(string message)
    {
        if (socketConnection == null || !socketConnection.Connected)
        {
            Debug.LogError("Not connected to server.");
            return;
        }

        try
        {
            NetworkStream stream = socketConnection.GetStream();
            if (stream.CanWrite)
            {
                byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(message);
                stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                Debug.Log("Message sent to server: " + message);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error sending message: " + ex.Message);
        }
    }

    /// <summary>
    /// Disconnects and cleans up the connection.
    /// </summary>
    public void Disconnect()
    {
        DispatchMessage("Reset");

        isRunning = false;

        socketConnection?.Close();
        socketConnection = null;

        if (clientThread != null && clientThread.IsAlive)
            clientThread.Interrupt();

        Debug.Log("Disconnected from server.");
    }

    void OnApplicationQuit()
    {
        Disconnect();
        UnityMainThreadDispatcher.Instance.Enqueue(() => uiManager.UpdateConnectionStatus(false));
    }
}