using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages application-wide functions such as reloading the current scene.
/// </summary>
public class ApplicationManager : MonoBehaviour
{
    void Update() => CheckForReloadInput();

    /// <summary>
    /// Checks for specific key inputs to perform application-wide actions.
    /// </summary>
    private void CheckForReloadInput()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
            ReloadCurrentScene();
    }

    /// <summary>
    /// Reloads the current active scene.
    /// </summary>
    private void ReloadCurrentScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
        Debug.Log("Scene reloaded.");
    }
}