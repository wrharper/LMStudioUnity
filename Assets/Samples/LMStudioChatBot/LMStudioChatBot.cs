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

            // Setup UI
            if (sendButton != null)
                sendButton.onClick.AddListener(SendMessage);
            
            if (userInputField != null)
                userInputField.onEndEdit.AddListener(OnInputEndEdit);

            conversationText.text = "LM Studio Chat Bot\n(Make sure LM Studio is running)\n\n";
        }

        private void OnInputEndEdit(string value)
        {
            // Send on Enter key
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SendMessage();
            }
        }

        private void SendMessage()
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
            
            // Add user message to display
            AddToConversation("You: " + userMessage);
            conversationHistory.Add("User: " + userMessage);

            // Generate response
            await GenerateResponse(userMessage);
        }

        private async Task GenerateResponse(string userMessage)
        {
            try
            {
                // Build the conversation context
                string prompt = BuildPrompt(userMessage);

                AddToConversation("Bot: ", false);

                // Stream the response
                string fullResponse = await llmClient.Completion(
                    prompt,
                    callback: (chunk) =>
                    {
                        // Update UI with streamed chunks
                        if (conversationText != null)
                        {
                            conversationText.text += chunk;
                            
                            // Auto-scroll to bottom
                            if (scrollRect != null)
                                Canvas.ForceUpdateCanvases();
                        }
                    },
                    completionCallback: () =>
                    {
                        conversationText.text += "\n\n";
                        conversationHistory.Add("Assistant: " + fullResponse);
                    }
                );

                isProcessing = false;
                
                if (sendButton != null)
                    sendButton.interactable = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error generating response: " + ex.Message);
                AddToConversation("Error: " + ex.Message);
                
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
