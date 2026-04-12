using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [Header("Portal Settings")]
    public string targetSceneName;

    [Header("UI")]
    public GameObject interactUI; // assign your interact icon

    private bool playerInRange = false;

    void Start()
    {
        if (interactUI != null)
            interactUI.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange) return;

        // Keyboard / controller input
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            Teleport();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Player entered portal range.");

            if (interactUI != null)
                interactUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Player exited the portal range.");

            if (interactUI != null)
                interactUI.SetActive(false);
        }
    }

    public void OnInteractButtonPressed()
    {
        if (playerInRange)
        {
            Teleport();
        }
    }

    void Teleport()
    {
        SceneManager.LoadScene(targetSceneName);
    }
}