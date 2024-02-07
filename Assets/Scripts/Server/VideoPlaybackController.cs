using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using DG.Tweening;
using System.IO;
using System.Collections;

/// <summary>
/// Manages video playback with smooth fade transitions between videos.
/// </summary>
public class VideoPlaybackController : MonoBehaviour
{
    [Header("Video Playback Settings")]
    [Tooltip("Video player component used for displaying videos.")]
    public VideoPlayer videoPlayer;
    [Header("UI Components")]
    [Tooltip("Overlay image for fade transitions.")]
    public Image fadeOverlay;
    [Tooltip("Static screen displayed when no video is playing.")]
    public VideoPlayer staticScreen;

    [Tooltip("Duration in seconds to wait for inactivity before showing static screen.")]
    private float inactivityDuration = 30f;
    private bool isPreparingVideo = false;
    private bool isStaticScreenShown = false;

    private Coroutine inactivityCoroutine;
    private VideoConfig videoConfig;

    private void Awake()
    {
        videoConfig = VideoConfig.Load();
        inactivityDuration = videoConfig.inactivityDuration;
    }

    private void Start()
    {
        staticScreen.gameObject.SetActive(true);
        isStaticScreenShown = true;
    }

    private void OnEnable()
    {
        TCPServerManager.OnCommandReceived += HandleCommand;
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    private void OnDisable()
    {
        TCPServerManager.OnCommandReceived -= HandleCommand;
        videoPlayer.prepareCompleted -= OnVideoPrepared;
    }

    /// <summary>
    /// Handles the incoming command to either play a video or show the static screen.
    /// </summary>
    /// <param name="command">The command received from the TCP server.</param>
    private void HandleCommand(string command)
    {
        if (!isStaticScreenShown)
            ResetInactivityTimer();

        if (command.StartsWith("PlayVideo:"))
        {
            string videoIndex = command.Substring("PlayVideo:".Length);
            PrepareVideo(videoIndex);
        }
        else if (command == "Reset")
        {
            ShowStaticScreen();
        }
    }

    /// <summary>
    /// Prepares the video specified by the index for playback.
    /// </summary>
    /// <param name="videoIndex">The index of the video to play.</param>
    private void PrepareVideo(string videoIndex)
    {
        if (isPreparingVideo) return;

        isPreparingVideo = true;
        string videoPath = GetVideoPathByIndex(int.Parse(videoIndex));
        PerformFade(1, 0.5f, () =>
        {
            staticScreen.gameObject.SetActive(false);
            videoPlayer.gameObject.SetActive(true);
            isStaticScreenShown = false;
            videoPlayer.url = videoPath;
            videoPlayer.Prepare();
        });
    }

    /// <summary>
    /// Called when the video player has completed video preparation.
    /// </summary>
    /// <param name="source">The source video player.</param>
    private void OnVideoPrepared(VideoPlayer source)
    {
        isPreparingVideo = false;
        videoPlayer.Play();
        PerformFade(0, 0.5f, null);
        ResetInactivityTimer();
    }

    /// <summary>
    /// Retrieves the file path for the video associated with the given index.
    /// </summary>
    /// <param name="index">The index of the video.</param>
    /// <returns>The file path of the video.</returns>
    private string GetVideoPathByIndex(int index)
    {
        return Path.Combine(Application.streamingAssetsPath, $"{index}.mp4");
    }

    /// <summary>
    /// Displays the static screen and prepares the overlay for the next video.
    /// </summary>
    private void ShowStaticScreen()
    {
        videoPlayer.Stop();
        staticScreen.targetTexture.Release();

        PerformFade(1, 0.5f, () =>
        {
            videoPlayer.gameObject.SetActive(false);
            staticScreen.gameObject.SetActive(true);

            PerformFade(0, 0.5f, () =>
            {
                isStaticScreenShown = true;
                StopInactivityTimer();
            });
        });
    }

    /// <summary>
    /// Hahah, you guessed it. This stops the inactivity timer.
    /// </summary>
    private void StopInactivityTimer()
    {
        if (inactivityCoroutine != null)
            StopCoroutine(inactivityCoroutine);
    }

    /// <summary>
    /// Resets the inactivity timer.
    /// </summary>
    private void ResetInactivityTimer()
    {
        if (inactivityCoroutine != null)
            StopCoroutine(inactivityCoroutine);

        if (!isStaticScreenShown)
            inactivityCoroutine = StartCoroutine(InactivityCheck());
    }

    /// <summary>
    // Waits for the specified duration of inactivity before showing the idle screen
    /// </summary>
    private IEnumerator InactivityCheck()
    {
        yield return new WaitForSeconds(inactivityDuration);
        ShowStaticScreen();
    }

    /// <summary>
    /// Central method for handling fade animations to ensure they do not overlap.
    /// </summary>
    /// <param name="targetAlpha">Target alpha value for the fade.</param>
    /// <param name="duration">Duration of the fade.</param>
    /// <param name="onComplete">Action to perform after the fade completes.</param>
    private void PerformFade(float targetAlpha, float duration, System.Action onComplete)
    {
        fadeOverlay.DOKill();
        fadeOverlay.DOFade(targetAlpha, duration).OnComplete(() => onComplete?.Invoke());
    }
}