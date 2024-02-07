using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using TMPro;

/// <summary>
/// Manages the client-side user interface, handling button interactions and visual feedback.
/// </summary>
public class ClientUIManager : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("The UI element that indicates if the client is connected to the server.")]
    [SerializeField] GameObject connectionStatusIndicator;
    [Tooltip("Buttons for selecting videos.")]
    [SerializeField] Button[] videoButtons;
    [SerializeField] TMP_Text statusText;
    [SerializeField] float revealDuration = 1f;
    [SerializeField] float displayDuration = 3f;
    [Header("Button Animation")]
    [Tooltip("The transform that should be rotated when a button is used.")]
    [SerializeField] Transform[] buttonParents;
    [Tooltip("The default rotation of the buttons when they are not used.")]
    [SerializeField] Vector3 defaultRotation = Vector3.zero;
    [Tooltip("The rotation of the buttons when they are used.")]
    [SerializeField] Vector3 usedRotation = new Vector3(0, 0, -114);
    [Header("Animation Settings")]
    [Tooltip("Time it takes for the button to rotate back to the default position.")]
    [SerializeField] float rotationResetDuration = 0.5f;
    [Header("Button Colors")]
    [Tooltip("The color of the button circle when it is interactable.")]
    [SerializeField] Color interactableColor = Color.white;
    [Tooltip("The color of the button hover when it is interactable.")]
    [SerializeField] Color hoverActiveColor = Color.green;
    [SerializeField] ClientMessageHandler messageHandler;

    private Button currentlySelectedButton = null;

    public Image[] buttonCircles;
    public Image[] buttonHovers;

    void Start()
    {
        InitializeButtonListeners();
        UpdateConnectionStatus(false);
    }

    private void InitializeButtonListeners()
    {
        //StatusTextUpdate("You are not connected");

        foreach (Button button in videoButtons)
            button.onClick.AddListener(() => OnVideoButtonClicked(button));
    }

    /// <summary>
    /// Handles each button clicked
    /// </summary>
    private void OnVideoButtonClicked(Button button)
    {
        int buttonIndex = System.Array.IndexOf(videoButtons, button);
        if (buttonIndex < 0)
        {
            Debug.LogError("Button not found in array.");
            return;
        }

        if (button == currentlySelectedButton)
            return;

        if (currentlySelectedButton != null)
            ResetSelection(System.Array.IndexOf(videoButtons, currentlySelectedButton));

        currentlySelectedButton = button;
        AnimateSelection(buttonIndex);

        messageHandler.SendMessageToServer($"PlayVideo:{buttonIndex}");

        StatusTextUpdate($"Video {buttonIndex + 1} is playing currently.");
    }

    /// <summary>
    /// Little animation for the selection
    /// </summary>
    private void AnimateSelection(int buttonIndex)
    {
        buttonParents[buttonIndex].DORotate(usedRotation, rotationResetDuration);

        buttonCircles[buttonIndex].color = interactableColor;
        buttonHovers[buttonIndex].color = hoverActiveColor;

        videoButtons[buttonIndex].interactable = false;
    }

    public void StatusTextUpdate(string text) => statusText.text = text;

    /// <summary>
    /// Video has ended so will reset buttons
    /// </summary>
    public void OnVideoEnded()
    {
        StatusTextUpdate("Please pick another video");

        if (currentlySelectedButton != null)
        {
            int buttonIndex = System.Array.IndexOf(videoButtons, currentlySelectedButton);
            if (buttonIndex >= 0)
                ResetSelection(buttonIndex);

            currentlySelectedButton = null;
        }
    }

    /// <summary>
    /// Resets the request selection, allowing new requests.
    /// </summary>
    private void ResetSelection(int buttonIndex)
    {
        buttonParents[buttonIndex].DORotate(defaultRotation, 0.5f);

        buttonCircles[buttonIndex].color = interactableColor;
        buttonHovers[buttonIndex].color = interactableColor;
        videoButtons[buttonIndex].interactable = true;
    }

    public void UpdateConnectionStatus(bool isConnected)
    {
        foreach (var button in videoButtons)
        {
            button.interactable = isConnected;
        }

        if (isConnected)
        {
            connectionStatusIndicator.GetComponent<Image>().color = Color.green;

            connectionStatusIndicator.GetComponent<Image>().DOFade(0, 1)
                .OnComplete(() => connectionStatusIndicator.SetActive(false));
        }
        else
        {
            connectionStatusIndicator.SetActive(true);
            connectionStatusIndicator.GetComponent<Image>().color = Color.red;
        }

        DOVirtual.DelayedCall(1, () =>
        {
            connectionStatusIndicator.SetActive(!isConnected);
        });
    }

    /// <summary>
    /// Resets the UI elements related to video selection.
    /// </summary>
    public void ResetVideoSelectionUI()
    {
        OnVideoEnded();

        for (int i = 0; i < videoButtons.Length; i++)
        {
            videoButtons[i].interactable = true;
            buttonParents[i].DORotate(defaultRotation, rotationResetDuration);
            buttonCircles[i].color = interactableColor;
            buttonHovers[i].color = interactableColor;
        }
    }

    /// <summary>
    /// Resets the entire UI to its initial state.
    /// </summary>
    public void ResetEntireUI()
    {
        ResetVideoSelectionUI();
        connectionStatusIndicator.GetComponent<Image>().DOFade(1, 0.5f);
    }
}