/// @file
/// @brief Quick setup helper for LM Studio configuration with error handling
using UnityEngine;
using System;

namespace LLMUnity
{
    /// <summary>
    /// Helper utilities for LM Studio setup, diagnostics, and error handling.
    /// Provides quick configuration and troubleshooting tools for LM Studio integration.
    /// 
    /// Common Errors & Solutions:
    /// 1. "Connection refused" → LM Studio not running or wrong host/port
    /// 2. "No model loaded" → Download and select model in LM Studio GUI
    /// 3. "Timeout" → LM Studio server is slow or unreachable
    /// 4. "Grammar not supported" → Use temperature/top-p instead of grammar
    /// 
    /// Quick Start:
    /// <code>
    /// // In Start() or Awake():
    /// LMStudioSetup.ConfigureForLMStudio(llmClient);
    /// await LMStudioSetup.ValidateConnection(llmClient);
    /// </code>
    /// </summary>
    public static class LMStudioSetup
    {
        private const string DEFAULT_HOST = "localhost";
        private const int DEFAULT_PORT = 1234;

        /// <summary>
        /// Configures an LLMClient to use LM Studio on localhost:1234
        /// </summary>
        public static void ConfigureForLMStudio(LLMClient client, string host = DEFAULT_HOST, int port = DEFAULT_PORT, string apiKey = "")
        {
            if (client == null)
            {
                Debug.LogError("[LM Studio] LLMClient is null - cannot configure");
                return;
            }

            client.remote = true;
            client.host = host;
            client.port = port;
            if (!string.IsNullOrEmpty(apiKey))
                client.APIKey = apiKey;

            Debug.Log($"[LM Studio] ✓ LLMClient configured for LM Studio at {host}:{port}");
        }

        /// <summary>
        /// Validates LM Studio connection and provides detailed diagnostics
        /// </summary>
        public static async System.Threading.Tasks.Task<bool> ValidateConnection(
            LLMClient client, 
            bool verbose = true)
        {
            if (client == null)
            {
                Debug.LogError("[LM Studio] LLMClient is null");
                return false;
            }

            if (!client.remote)
            {
                if (verbose) Debug.LogWarning("[LM Studio] LLMClient is in local mode, not remote");
                return false;
            }

            var lmStudioClient = new LMStudioClient(client.host, client.port, client.APIKey);
            return await ValidateConnection(client.host, client.port, verbose);
        }

        /// <summary>
        /// Quick test to verify LM Studio is running with detailed error reporting
        /// </summary>
        public static async System.Threading.Tasks.Task<bool> ValidateConnection(
            string host = DEFAULT_HOST, 
            int port = DEFAULT_PORT, 
            bool verbose = true)
        {
            if (verbose) Debug.Log($"[LM Studio] Checking connection to {host}:{port}...");

            var client = new LMStudioClient(host, port);
            
            try
            {
                bool isAlive = await client.IsServerAlive();
                
                if (!isAlive)
                {
                    Debug.LogError($"[LM Studio] ✗ Cannot connect to server at {host}:{port}");
                    Debug.LogError($"[LM Studio] Troubleshooting steps:");
                    Debug.LogError($"  1. Is LM Studio installed? Download from https://lmstudio.ai");
                    Debug.LogError($"  2. Is LM Studio running? Open the application");
                    Debug.LogError($"  3. Is the server started? Go to Developer tab > click 'Start Server'");
                    Debug.LogError($"  4. Check the port is {port} (visible in Developer tab)");
                    Debug.LogError($"  5. Check firewall isn't blocking port {port}");
                    if (host != "localhost" && host != "127.0.0.1")
                    {
                        Debug.LogError($"  6. For remote server: check network connectivity to {host}");
                    }
                    return false;
                }

                if (verbose) Debug.Log($"[LM Studio] ✓ Server is online at {host}:{port}");

                // Try to get loaded model
                var models = await client.GetAvailableModels();
                if (models == null || models.Count == 0)
                {
                    Debug.LogWarning($"[LM Studio] ⚠ No model loaded");
                    Debug.LogWarning($"[LM Studio] Solution: Open LM Studio > Search tab > Download a model (e.g., Mistral, Llama 2)");
                    return false;
                }

                if (verbose)
                {
                    Debug.Log($"[LM Studio] ✓ Loaded model: {models[0]}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LM Studio] ✗ Connection validation failed: {ex.Message}");
                Debug.LogError($"[LM Studio] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Get detailed information about the connected LM Studio server
        /// </summary>
        public static async System.Threading.Tasks.Task GetServerInfo(
            string host = DEFAULT_HOST, 
            int port = DEFAULT_PORT)
        {
            var client = new LMStudioClient(host, port);
            
            Debug.Log($"========== LM Studio Server Information ==========");
            Debug.Log($"Host: {host}");
            Debug.Log($"Port: {port}");
            Debug.Log($"API URL: http://{host}:{port}/v1");
            Debug.Log("");
            
            try
            {
                bool isAlive = await client.IsServerAlive();
                Debug.Log($"Server Status: {(isAlive ? "✓ Online" : "✗ Offline")}");
                
                if (isAlive)
                {
                    try
                    {
                        var models = await client.GetAvailableModels();
                        if (models != null && models.Count > 0)
                        {
                            Debug.Log($"");
                            Debug.Log($"Loaded Models ({models.Count}):");
                            foreach (var model in models)
                            {
                                Debug.Log($"  • {model}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("No models loaded in LM Studio");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error getting model info: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"Server is offline - ensure it's running and 'Start Server' is clicked");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ Failed to get server info: {ex.Message}");
            }
            
            Debug.Log($"==================================================");
        }

        /// <summary>
        /// Test a single completion to verify full functionality
        /// </summary>
        public static async System.Threading.Tasks.Task TestCompletion(
            string host = DEFAULT_HOST,
            int port = DEFAULT_PORT,
            string prompt = "Say 'Hello' and nothing else")
        {
            Debug.Log($"[LM Studio] Testing completion with prompt: \"{prompt}\"");
            
            var client = new LMStudioClient(host, port);
            
            try
            {
                bool isAlive = await client.IsServerAlive();
                if (!isAlive)
                {
                    Debug.LogError($"[LM Studio] Server not reachable at {host}:{port}");
                    return;
                }

                string response = await client.Completion(prompt);
                Debug.Log($"[LM Studio] ✓ Completion successful!");
                Debug.Log($"[LM Studio] Response: {response}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LM Studio] ✗ Completion failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Diagnostic report for debugging connection issues
        /// </summary>
        public static async System.Threading.Tasks.Task PrintDiagnosticReport(
            LLMClient client = null,
            string host = DEFAULT_HOST,
            int port = DEFAULT_PORT)
        {
            Debug.Log($"========== LM Studio Diagnostic Report ==========");
            Debug.Log($"Date: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Debug.Log($"");

            if (client != null)
            {
                Debug.Log($"LLMClient Configuration:");
                Debug.Log($"  Remote Mode: {client.remote}");
                Debug.Log($"  Host: {client.host}");
                Debug.Log($"  Port: {client.port}");
                Debug.Log($"  API Key: {(string.IsNullOrEmpty(client.APIKey) ? "(none)" : "(set)")}");
                Debug.Log($"");
                host = client.host;
                port = client.port;
            }

            Debug.Log($"Server Information:");
            await GetServerInfo(host, port);
            
            Debug.Log($"");
            Debug.Log($"Validation:");
            bool isValid = await ValidateConnection(host, port, false);
            Debug.Log($"  Connection Status: {(isValid ? "✓ Valid" : "✗ Invalid")}");
            
            Debug.Log($"==================================================");
        }
    }
}

