/// @file
/// @brief Minimal LM Studio setup template in Unity
using UnityEngine;
using LLMUnity;

/// <summary>
/// Copy this template and modify for your needs.
/// This is the absolute minimum code needed to use LM Studio with LLMUnity.
/// </summary>
public class LMStudioMinimalTemplate : MonoBehaviour
{
    // Step 1: Add the LLMClient reference
    [SerializeField] private LLMClient llmClient;

    // Step 2: Configure in Start()
    private void Start()
    {
        // Configure for LM Studio
        llmClient.remote = true;
        llmClient.host = "localhost";
        llmClient.port = 1234;
        
        // Optional: Test connection
        LLMStudioSetup.TestLMStudioConnection("localhost", 1234);
    }

    // Step 3: Use it!
    public async void GenerateResponse()
    {
        string response = await llmClient.Completion("Hello, how are you?");
        Debug.Log("Response: " + response);
    }

    // Optional: Use streaming for real-time feedback
    public async void GenerateResponseWithStreaming()
    {
        await llmClient.Completion(
            "Write a short poem",
            callback: (chunk) => Debug.Log("Chunk: " + chunk)
        );
    }
}

/*
 * SETUP CHECKLIST:
 * 1. Download LM Studio from lmstudio.ai
 * 2. Download a model (e.g., neural-chat)
 * 3. Start the server (Developer > Start Server)
 * 4. Create empty GameObject, add this script
 * 5. Assign LLMClient component in inspector
 * 6. Run and test
 * 
 * TROUBLESHOOTING:
 * - If "Server is not alive": Make sure LM Studio is running and "Start Server" is clicked
 * - If no response: Check that a model is loaded in LM Studio
 * - If slow: Enable GPU in LM Studio settings
 */
