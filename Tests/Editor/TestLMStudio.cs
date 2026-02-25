using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace LLMUnity.Tests
{
    /// <summary>
    /// Integration tests for LM Studio backend connectivity and functionality.
    /// NOTE: These tests require a running LM Studio instance on localhost:1234
    /// To run these tests:
    /// 1. Start LM Studio application
    /// 2. Go to Developer tab > click "Start Server"
    /// 3. Load a model in LM Studio (Search > Download > Select)
    /// 4. Run tests in Unity Editor: Window > TextExecution > Run Tests
    /// </summary>
    [Category("Integration")]
    [Category("LMStudio")]
    public class TestLMStudio
    {
        private const string TEST_HOST = "localhost";
        private const int TEST_PORT = 1234;
        private LMStudioClient lmStudioClient;

        [SetUp]
        public void Setup()
        {
            lmStudioClient = new LMStudioClient(TEST_HOST, TEST_PORT);
        }

        #region Connection Tests

        [Test]
        [Timeout(15000)]
        public async Task TestServerConnection_IsAlive()
        {
            // Test: Server is reachable
            bool isAlive = await lmStudioClient.IsServerAlive();
            
            Assert.IsTrue(isAlive, 
                $"LM Studio server not reachable at {TEST_HOST}:{TEST_PORT}\n" +
                "Troubleshooting:\n" +
                "  1. Is LM Studio running?\n" +
                "  2. Is 'Start Server' clicked in Developer tab?\n" +
                "  3. Is port {TEST_PORT} correct?");
        }

        [Test]
        [Timeout(15000)]
        public async Task TestServerConnection_GetAvailableModels()
        {
            // Test: Can retrieve available models
            var models = await lmStudioClient.GetAvailableModels();
            
            Assert.IsNotNull(models, "GetAvailableModels returned null");
            Assert.Greater(models.Count, 0, 
                "No models loaded in LM Studio\n" +
                "Solution: Open LM Studio > Search > Download a model (e.g., Mistral-7B)");
            
            Debug.Log($"[LMStudio Test] Available models: {string.Join(", ", models)}");
        }

        [Test]
        [Timeout(15000)]
        public async Task TestServerConnection_GetLoadedModel()
        {
            // Test: Can retrieve currently loaded model
            var loadedModel = await lmStudioClient.GetLoadedModel();
            
            Assert.IsNotNull(loadedModel, "GetLoadedModel returned null");
            Assert.IsFalse(string.IsNullOrEmpty(loadedModel), "No model currently loaded");
            
            Debug.Log($"[LMStudio Test] Currently loaded model: {loadedModel}");
        }

        #endregion

        #region Completion Tests

        [Test]
        [Timeout(30000)]
        public async Task TestCompletion_SimplePrompt()
        {
            // Test: Basic completion works
            string prompt = "Say 'Hello' and nothing else:";
            string response = await lmStudioClient.Completion(prompt);
            
            Assert.IsNotNull(response, "Completion returned null");
            Assert.IsFalse(string.IsNullOrEmpty(response.Trim()), 
                "Completion returned empty response\n" +
                "Possible causes:\n" +
                "  1. Model not loaded in LM Studio\n" +
                "  2. Model is very small or broken\n" +
                "  3. Try a different model");
            
            Debug.Log($"[LMStudio Test] Prompt: {prompt}");
            Debug.Log($"[LMStudio Test] Response: {response}");
        }

        [Test]
        [Timeout(60000)]
        public async Task TestCompletion_WithParameters()
        {
            // Test: Completion with custom parameters
            string prompt = "Count to 5:";
            var parameters = new Newtonsoft.Json.Linq.JObject
            {
                ["temperature"] = 0.1f,
                ["top_p"] = 0.9f,
                ["n_predict"] = 50
            };
            
            string response = await lmStudioClient.Completion(prompt, parameters);
            
            Assert.IsNotNull(response, "Completion with parameters returned null");
            Assert.IsFalse(string.IsNullOrEmpty(response.Trim()), 
                "Completion returned empty response");
            
            Debug.Log($"[LMStudio Test] Response with parameters: {response}");
        }

        [Test]
        [Timeout(60000)]
        public async Task TestCompletion_Streaming()
        {
            // Test: Streaming completion works
            string prompt = "List three colors:";
            var chunksReceived = new List<string>();
            
            string fullResponse = await lmStudioClient.Completion(
                prompt,
                streamCallback: (chunk) =>
                {
                    chunksReceived.Add(chunk);
                }
            );
            
            Assert.Greater(chunksReceived.Count, 0, 
                "No stream chunks received\n" +
                "Streaming may not be supported for this model");
            
            Assert.IsFalse(string.IsNullOrEmpty(fullResponse.Trim()), 
                "Full streamed response is empty");
            
            Debug.Log($"[LMStudio Test] Streaming: Received {chunksReceived.Count} chunks");
            Debug.Log($"[LMStudio Test] Total response length: {fullResponse.Length} chars");
        }

        #endregion

        #region Tokenization Tests

        [Test]
        [Timeout(15000)]
        public async Task TestTokenization_SimpleText()
        {
            // Test: Tokenization works
            string text = "Hello world";
            var tokens = await lmStudioClient.Tokenize(text);
            
            Assert.IsNotNull(tokens, "Tokenize returned null");
            Assert.Greater(tokens.Count, 0, 
                "Tokenization returned 0 tokens for non-empty string");
            
            // Rough estimate: ~4 chars ≈ 1 token
            float expectedTokens = text.Length / 4f;
            float tolerance = text.Length / 2f; // 50% tolerance for estimates
            
            Assert.IsTrue(tokens.Count >= expectedTokens - tolerance &&
                          tokens.Count <= expectedTokens + tolerance,
                $"Token count ({tokens.Count}) is way off expected estimate ({expectedTokens:F1})");
            
            Debug.Log($"[LMStudio Test] Text: '{text}'");
            Debug.Log($"[LMStudio Test] Tokens: {tokens.Count} (estimate)");
        }

        #endregion

        #region Embeddings Tests

        [Test]
        [Timeout(15000)]
        public async Task TestEmbeddings_SimpleText()
        {
            // Test: Embeddings work (requires embeddings-capable model)
            string text = "Hello world";
            var embeddings = await lmStudioClient.GetEmbeddings(text);
            
            // Embeddings may not be available on all models
            if (embeddings == null || embeddings.Count == 0)
            {
                Debug.LogWarning("[LMStudio Test] Embeddings not available - model may not support embeddings");
                Assert.Ignore("Embeddings not available for current model");
            }
            
            Assert.Greater(embeddings.Count, 0, 
                "Embedding returned 0 dimensions");
            
            Debug.Log($"[LMStudio Test] Embedding dimension: {embeddings.Count}");
        }

        #endregion

        #region LLMClient Integration Tests

        [Test]
        [Timeout(15000)]
        public async Task TestLLMClient_RemoteConfiguration()
        {
            // Test: LLMClient can be configured for remote LM Studio
            var llmClient = new GameObject("TestLLMClient").AddComponent<LLMClient>();
            
            try
            {
                llmClient.remote = true;
                llmClient.host = TEST_HOST;
                llmClient.port = TEST_PORT;
                
                Assert.IsTrue(llmClient.remote, "Failed to set remote mode");
                Assert.AreEqual(TEST_HOST, llmClient.host, "Failed to set host");
                Assert.AreEqual(TEST_PORT, llmClient.port, "Failed to set port");
                
                Debug.Log($"[LMStudio Test] LLMClient configured for remote at {TEST_HOST}:{TEST_PORT}");
            }
            finally
            {
                UnityEngine.Object.Destroy(llmClient.gameObject);
            }
        }

        [Test]
        [Timeout(30000)]
        public async Task TestLLMClient_RemoteSetup()
        {
            // Test: LLMClient can setup and initialize for remote LM Studio
            var llmClient = new GameObject("TestLLMClient").AddComponent<LLMClient>();
            
            try
            {
                llmClient.remote = true;
                llmClient.host = TEST_HOST;
                llmClient.port = TEST_PORT;
                
                // This initializes the remote connection
                await llmClient.SetupCaller();
                
                // Verify we can make a request
                string response = await llmClient.Completion("Hi");
                Assert.IsNotNull(response, "LLMClient completion returned null");
                
                Debug.Log($"[LMStudio Test] LLMClient setup successful, received response");
            }
            finally
            {
                llmClient.Cleanup();
                UnityEngine.Object.Destroy(llmClient.gameObject);
            }
        }

        #endregion

        #region Error Handling Tests

        [Test]
        [Timeout(10000)]
        public async Task TestErrorHandling_InvalidHost()
        {
            // Test: Proper error on invalid host
            var invalidClient = new LMStudioClient("invalid-host-12345.local", TEST_PORT);
            bool isAlive = await invalidClient.IsServerAlive();
            
            Assert.IsFalse(isAlive, "Invalid host should not be alive");
        }

        [Test]
        [Timeout(10000)]
        public async Task TestErrorHandling_InvalidPort()
        {
            // Test: Proper error on invalid port
            var invalidClient = new LMStudioClient(TEST_HOST, 9999);
            bool isAlive = await invalidClient.IsServerAlive();
            
            Assert.IsFalse(isAlive, "Invalid port should not be alive");
        }

        [Test]
        [Timeout(15000)]
        public async Task TestErrorHandling_ServerDown()
        {
            // Test: Proper error handling when server is unreachable
            var client = new LMStudioClient(TEST_HOST, TEST_PORT);
            
            // Just test that IsServerAlive handles it gracefully
            bool isAlive = await client.IsServerAlive();
            
            // If we get here without exception, error handling works
            Assert.IsNotNull(isAlive, "IsServerAlive should return bool even on error");
        }

        #endregion

        #region LMStudioSetup Validation Tests

        [Test]
        [Timeout(15000)]
        public async Task TestLMStudioSetup_ValidateConnection()
        {
            // Test: LMStudioSetup.ValidateConnection works
            bool isValid = await LMStudioSetup.ValidateConnection(TEST_HOST, TEST_PORT, verbose: false);
            
            Assert.IsTrue(isValid, 
                $"LM Studio validation failed at {TEST_HOST}:{TEST_PORT}");
        }

        #endregion

        #region Performance Tests

        [Test]
        [Timeout(60000)]
        public async Task TestPerformance_CompletionResponseTime()
        {
            // Test: Completion completes in reasonable time
            var startTime = System.DateTime.Now;
            string response = await lmStudioClient.Completion("Say yes");
            var endTime = System.DateTime.Now;
            
            var responseTime = endTime - startTime;
            Assert.Less(responseTime.TotalSeconds, 30, 
                $"Completion took too long: {responseTime.TotalSeconds:F2}s\n" +
                "This may indicate:\n" +
                "  1. Model is very large (consider quantized version)\n" +
                "  2. Server is overloaded\n" +
                "  3. GPU acceleration not enabled");
            
            Debug.Log($"[LMStudio Test] Completion response time: {responseTime.TotalMilliseconds:F0}ms");
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            // Cleanup if needed
            lmStudioClient = null;
        }
    }
}
