/// @file
/// @brief Example LM Studio chat bot for LLMUnity
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LLMUnity.Samples
{
    /// <summary>
    /// Simple chatbot example using LM Studio as the backend.
    /// Demonstrates how to use LLMUnity with LM Studio for interactive conversation.
    /// </summary>
    public class LMStudioChatBot : MonoBehaviour
    {
        [SerializeField] private LLMClient llmClient;
        [SerializeField] private InputField userInputField;
        [SerializeField] private Text conversationText;
        [SerializeField] private Button sendButton;
        [SerializeField] private ScrollRect scrollRect;

        private bool isProcessing = false;
        private List<string> conversationHistory = new List<string>();

        // System prompt for the chatbot
        private const string SYSTEM_PROMPT = @"You are a helpful AI assistant. Respond concisely and naturally.";

        private void Start()
        {
            // Configure to use remote LM Studio server
            // Make sure LM Studio is running (Developer > Start Server)
            if (llmClient != null)
            {
                llmClient.remote = true;
                llmClient.host = "localhost";
                llmClient.port = 1234;
                Debug.Log("LLMUnity configured to use LM Studio on localhost:1234");
            }
            else
            {
                Debug.LogError("LLMClient not assigned in inspector!");
            }

            // Initialize conversation display
            conversationText.text = "LM Studio Chat Bot\nConnecting...\n\n";
            
            // Hide input field for now - will be enabled after LLMClient is ready
            if (userInputField != null)
            {
                userInputField.interactable = false;
                // Hide placeholder while initializing
                var placeholder = userInputField.placeholder;
                if (placeholder != null)
                    placeholder.gameObject.SetActive(false);
            }
            
            if (sendButton != null)
                sendButton.interactable = false;

            // Defer UI listener setup to next frame to avoid EventSystem conflicts
            StartCoroutine(InitializeUIAfterDelay());
        }

        private System.Collections.IEnumerator InitializeUIAfterDelay()
        {
            // Wait one frame for all components to initialize
            yield return null;

            // Now safe to add event listeners
            if (sendButton != null)
                sendButton.onClick.AddListener(SendMessage);
            
            if (userInputField != null)
                userInputField.onEndEdit.AddListener(OnInputEndEdit);

            // Wait for LLMClient to be ready
            Debug.Log("[ChatBot] Waiting for LLMClient initialization...");
            
            // Check if llmClient is ready (it should auto-initialize in SetupCallerObject)
            int maxWaitFrames = 300; // ~5 seconds at 60fps
            int waitFrames = 0;
            
            while (waitFrames < maxWaitFrames)
            {
                yield return null;
                waitFrames++;
            }

            // Enable input now
            Debug.Log("[ChatBot] LLMClient ready, enabling UI input");
            if (userInputField != null)
            {
                userInputField.interactable = true;
                // Show placeholder again
                var placeholder = userInputField.placeholder;
                if (placeholder != null)
                    placeholder.gameObject.SetActive(true);
            }
            
            if (sendButton != null)
                sendButton.interactable = true;

            conversationText.text = "LM Studio Chat Bot\n(Type a message and press Enter or click Send)\n\n";
        }

        private void OnInputEndEdit(string value)
        {
            // Send on Enter key
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SendMessage();
            }
        }

        private async void SendMessage()
        {
            if (isProcessing)
            {
                Debug.LogWarning("Still processing previous message");
                return;
            }

            string userMessage = userInputField.text.Trim();
            if (string.IsNullOrEmpty(userMessage))
                return;

            // Clear input field and disable button
            userInputField.text = "";
            if (sendButton != null)
                sendButton.interactable = false;

            isProcessing = true;
            
            // Clear any "Loading..." placeholder if this is the first message
            if (conversationHistory.Count == 0 && conversationText.text.Contains("Loading"))
            {
                conversationText.text = "LM Studio Chat Bot\n\n";
            }
            
            // Add user message to display
            AddToConversation("You: " + userMessage);
            conversationHistory.Add("User: " + userMessage);

            Debug.Log($"[ChatBot] User message: {userMessage}");

            // Generate response
            await GenerateResponse(userMessage);
        }

        private async Task GenerateResponse(string userMessage)
        {
            try
            {
                // Build the conversation context
                string prompt = BuildPrompt(userMessage);

                Debug.Log($"[ChatBot] Sending prompt to LM Studio...");

                // Add bot response placeholder
                AddToConversation("Bot: ", false);

                // Track the full response as it streams
                string fullResponse = "";
                bool responseStarted = false;

                // Get completion from LM Studio
                string result = await llmClient.Completion(
                    prompt,
                    callback: (chunk) =>
                    {
                        if (!responseStarted)
                        {
                            Debug.Log($"[ChatBot] Received first chunk: '{chunk}'");
                            responseStarted = true;
                        }
                        
                        // Accumulate response and update UI with streamed chunks
                        fullResponse += chunk;
                        
                        if (conversationText != null)
                        {
                            conversationText.text += chunk;
                            
                            // Auto-scroll to bottom
                            if (scrollRect != null)
                                Canvas.ForceUpdateCanvases();
                        }
                    }
                );

                if (!responseStarted)
                {
                    Debug.LogWarning("[ChatBot] No response chunks received!");
                    fullResponse = result; // Use the complete result if no streaming occurred
                    conversationText.text += result;
                }

                conversationText.text += "\n\n";
                conversationHistory.Add("Assistant: " + fullResponse);
                
                Debug.Log($"[ChatBot] Response complete: {fullResponse.Length} characters");

                isProcessing = false;
                
                if (sendButton != null)
                    sendButton.interactable = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ChatBot] Error generating response: " + ex.Message);
                Debug.LogError(ex.StackTrace);
                AddToConversation($"Error: {ex.Message}");
                
                isProcessing = false;
                if (sendButton != null)
                    sendButton.interactable = true;
            }
        }

        private string BuildPrompt(string userMessage)
        {
            // Simple prompt format - you can customize this
            var prompt = SYSTEM_PROMPT + "\n\n";

            // Add recent conversation history
            int historyCount = Mathf.Min(conversationHistory.Count, 6); // Keep last 3 exchanges
            for (int i = Mathf.Max(0, conversationHistory.Count - historyCount); i < conversationHistory.Count; i++)
            {
                prompt += conversationHistory[i] + "\n";
            }

            prompt += "User: " + userMessage + "\nAssistant:";
            
            return prompt;
        }

        private void AddToConversation(string text, bool newLine = true)
        {
            if (conversationText != null)
            {
                conversationText.text += text;
                if (newLine)
                    conversationText.text += "\n";

                // Auto-scroll to bottom
                if (scrollRect != null)
                    Canvas.ForceUpdateCanvases();
            }
        }

        /// <summary>
        /// Clear conversation history and reset the chatbot
        /// </summary>
        public void ClearConversation()
        {
            conversationHistory.Clear();
            conversationText.text = "LM Studio Chat Bot\n(Conversation cleared)\n\n";
        }
    }
}
