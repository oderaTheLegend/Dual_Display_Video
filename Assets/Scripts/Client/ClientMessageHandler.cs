using UnityEngine;
using System;

/// <summary>
/// Handles sending and receiving messages to/from the server.
/// </summary>
public class ClientMessageHandler : MonoBehaviour
{
    [SerializeField] TCPConnection tcpConnection;
    [SerializeField] ClientUIManager uiManager;

    void Start()
    {
        if (tcpConnection != null)
            tcpConnection.RegisterOnMessageReceivedCallback(ProcessServerMessage);
    }

    /// <summary>
    /// Sends a message to the server.
    /// </summary>
    /// <param name="message">The message to send.</param>
    public void SendMessageToServer(string message) => tcpConnection?.DispatchMessage(message);

    /// <summary>
    /// Processes messages received from the server.
    /// </summary>
    /// <param name="message">The received message.</param>
    private void ProcessServerMessage(string message)
    {
        Debug.Log($"Received message from server: {message}");

        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            if (message == "VideoEnded")
            {
                Debug.Log("Server indicates the video has ended.");
                uiManager.ResetVideoSelectionUI();
            }
            else if (message.StartsWith("Reset"))
            {
                Debug.Log("Server requested a reset.");
                tcpConnection.Disconnect();
                uiManager.ResetEntireUI();
            }
        });
    }
}