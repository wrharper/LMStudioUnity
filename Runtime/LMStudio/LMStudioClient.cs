/// @file
/// @brief LM Studio API client implementation for Unity
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using System.Text;

namespace LLMUnity
{
    /// <summary>
    /// LM Studio API client that communicates with a running LM Studio instance.
    /// LM Studio provides an OpenAI-compatible API on localhost:1234 by default.
    /// Supports LORA adapter merging for local models.
    /// </summary>
    public class LMStudioClient
    {
        private string apiUrl;
        private string apiKey;
        private int numRetries;
        private string currentModelPath;

        /// <summary>
        /// Initializes the LM Studio client with connection details
        /// </summary>
        /// <param name="host">Hostname or IP address (default: localhost)</param>
        /// <param name="port">Port number (default: 1234)</param>
        /// <param name="apiKey">API key if required (optional)</param>
        /// <param name="numRetries">Number of retries for failed connections</param>
        public LMStudioClient(string host = "localhost", int port = 1234, string apiKey = "", int numRetries = 5)
        {
            this.apiUrl = $"http://{host}:{port}/v1";
            this.apiKey = apiKey;
            this.numRetries = numRetries;
            this.currentModelPath = "";
        }

        /// <summary>
        /// Checks if the LM Studio server is alive and accessible
        /// </summary>
        public async Task<bool> IsServerAlive()
        {
            try
            {
                using (UnityWebRequest request = UnityWebRequest.Get($"{apiUrl}/models"))
                {
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    }

                    var asyncOp = request.SendWebRequest();
                    float timeout = Time.time + 5f;
                    while (!asyncOp.isDone && Time.time < timeout)
                    {
                        await Task.Delay(100);
                    }

                    return request.result == UnityWebRequest.Result.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the list of available models from LM Studio
        /// </summary>
        public async Task<List<string>> GetAvailableModels()
        {
            try
            {
                using (UnityWebRequest request = UnityWebRequest.Get($"{apiUrl}/models"))
                {
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    }

                    var asyncOp = request.SendWebRequest();
                    while (!asyncOp.isDone)
                    {
                        await Task.Delay(10);
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JObject.Parse(request.downloadHandler.text);
                        var models = new List<string>();
                        foreach (var model in response["data"])
                        {
                            models.Add(model["id"]?.ToString() ?? "");
                        }
                        return models;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get available models: {ex.Message}");
            }

            return new List<string>();
        }

        /// <summary>
        /// Gets the currently loaded model
        /// </summary>
        public async Task<string> GetLoadedModel()
        {
            try
            {
                var models = await GetAvailableModels();
                return models.Count > 0 ? models[0] : "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Prepares a model for use, optionally merging LORA adapters.
        /// For local models, LORA adapters are merged into a new model file.
        /// For remote models, this just validates the path.
        /// </summary>
        /// <param name="baseModelPath">Path to base model file</param>
        /// <param name="loras">List of (path, weight) tuples for LORA adapters</param>
        /// <param name="onProgress">Optional progress callback (0.0 to 1.0)</param>
        /// <returns>Path to model file (merged or original), or null on error</returns>
        public async Task<string> PrepareModelWithLora(
            string baseModelPath,
            List<(string path, float weight)> loras = null,
            Action<float> onProgress = null)
        {
            try
            {
                // If no LORA adapters, just return the base model
                if (loras == null || loras.Count == 0)
                {
                    this.currentModelPath = baseModelPath;
                    onProgress?.Invoke(1.0f);
                    return baseModelPath;
                }

                LLMUnitySetup.Log($"Preparing model with {loras.Count} LORA adapter(s) for LM Studio...");
                
                // Use the preprocessor to merge LORA adapters
                string mergedModelPath = await LoraPreprocessor.MergeLorasIntoModel(
                    baseModelPath,
                    loras,
                    onProgress);

                if (string.IsNullOrEmpty(mergedModelPath))
                {
                    LLMUnitySetup.LogError("Failed to prepare model with LORA adapters");
                    return null;
                }

                this.currentModelPath = mergedModelPath;
                LLMUnitySetup.Log($"Model ready: {Path.GetFileName(mergedModelPath)}");
                return mergedModelPath;
            }
            catch (Exception ex)
            {
                LLMUnitySetup.LogError($"Error preparing model: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the currently prepared model path (may be merged with LORA)
        /// </summary>
        public string GetCurrentModelPath() => currentModelPath;

        /// <summary>
        /// Tokenizes a string into token IDs using the model's tokenizer
        /// </summary>
        public async Task<List<int>> Tokenize(string text)
        {
            var tokens = new List<int>();
            if (string.IsNullOrEmpty(text))
                return tokens;

            // LM Studio doesn't provide a direct tokenize endpoint, 
            // so we'll estimate based on character count (rough approximation)
            // This is a limitation of using LM Studio vs a full local implementation
            return await Task.FromResult(TokenizeEstimate(text));
        }

        /// <summary>
        /// Detokenizes token IDs back to text
        /// </summary>
        public async Task<string> Detokenize(List<int> tokens)
        {
            // LM Studio doesn't provide detokenize endpoint
            // This would require the tokenizer context which isn't available via API
            return await Task.FromResult("");
        }

        /// <summary>
        /// Generates embeddings for the given text
        /// </summary>
        public async Task<List<float>> GetEmbeddings(string text)
        {
            var embeddings = new List<float>();

            try
            {
                var requestBody = new JObject
                {
                    ["input"] = text,
                    ["model"] = "text-embedding-model" // LM Studio embedding model
                };

                using (UnityWebRequest request = new UnityWebRequest($"{apiUrl}/embeddings", "POST"))
                {
                    byte[] jsonToSend = Encoding.UTF8.GetBytes(requestBody.ToString());
                    request.uploadHandler = new UploadHandlerRaw(jsonToSend);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");

                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    }

                    var asyncOp = request.SendWebRequest();
                    while (!asyncOp.isDone)
                    {
                        await Task.Delay(10);
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JObject.Parse(request.downloadHandler.text);
                        var data = response["data"];
                        if (data != null && data.Count > 0)
                        {
                            var embeddingArray = data[0]["embedding"];
                            foreach (var value in embeddingArray)
                            {
                                embeddings.Add((float)value);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"Embedding request failed: {request.error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get embeddings: {ex.Message}");
            }

            return embeddings;
        }

        /// <summary>
        /// Generates text completion with streaming support
        /// </summary>
        public async Task<string> Completion(string prompt, JObject completionParameters = null, 
            Action<string> streamCallback = null)
        {
            var fullResponse = "";

            try
            {
                var requestBody = new JObject
                {
                    ["model"] = "local-model", // LM Studio uses "local-model" as identifier
                    ["prompt"] = prompt,
                    ["stream"] = streamCallback != null // Only stream if callback is provided
                };

                // Add completion parameters if provided
                if (completionParameters != null)
                {
                    foreach (var prop in completionParameters.Properties())
                    {
                        // Map LlamaLib parameter names to OpenAI API names
                        string key = MapParameterName(prop.Name);
                        requestBody[key] = prop.Value;
                    }
                }

                // Convert max_tokens if not provided
                if (!requestBody.ContainsKey("max_tokens"))
                {
                    requestBody["max_tokens"] = requestBody["n_predict"]?.Value<int>() ?? 256;
                    requestBody.Remove("n_predict");
                }

                using (UnityWebRequest request = new UnityWebRequest($"{apiUrl}/completions", "POST"))
                {
                    byte[] jsonToSend = Encoding.UTF8.GetBytes(requestBody.ToString());
                    request.uploadHandler = new UploadHandlerRaw(jsonToSend);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");

                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    }

                    var asyncOp = request.SendWebRequest();
                    while (!asyncOp.isDone)
                    {
                        await Task.Delay(10);
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var responseText = request.downloadHandler.text;

                        if (streamCallback != null)
                        {
                            // Handle streamed response (SSE format)
                            fullResponse = ParseStreamedResponse(responseText, streamCallback);
                        }
                        else
                        {
                            // Handle single response
                            var response = JObject.Parse(responseText);
                            var choices = response["choices"];
                            if (choices != null && choices.Count > 0)
                            {
                                fullResponse = choices[0]["text"]?.ToString() ?? "";
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"Completion request failed: {request.error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get completion: {ex.Message}");
            }

            return fullResponse;
        }

        /// <summary>
        /// Applies a chat template if the model supports it
        /// </summary>
        public async Task<string> ApplyTemplate(JArray messages)
        {
            // LM Studio doesn't have a template application endpoint
            // Return the raw messages as JSON
            return await Task.FromResult(messages.ToString());
        }

        /// <summary>
        /// Sets grammar constraints (if supported by model)
        /// </summary>
        public void SetGrammar(string grammar)
        {
            // LM Studio's API doesn't directly support grammar constraints yet
            // This could be added in future versions
            Debug.LogWarning("Grammar constraints are not yet supported when using LM Studio");
        }

        /// <summary>
        /// Sets completion parameters
        /// </summary>
        public void SetCompletionParameters(JObject parameters)
        {
            // Parameters are passed per request in LM Studio
            // This method is for compatibility with the LlamaLib interface
        }

        /// <summary>
        /// Cancels a request (stub - LM Studio handles this internally)
        /// </summary>
        public void Cancel(int idSlot)
        {
            // LM Studio doesn't expose slot management via API
            // Cancellation would be done by closing the HTTP connection
            Debug.LogWarning("Request cancellation not yet implemented for LM Studio");
        }

        #region Private Helper Methods

        private string MapParameterName(string llamaParameterName)
        {
            // Map LlamaLib parameter names to OpenAI API names
            var parameterMap = new Dictionary<string, string>
            {
                ["temperature"] = "temperature",
                ["top_k"] = "top_k",
                ["top_p"] = "top_p",
                ["min_p"] = "min_p",
                ["n_predict"] = "max_tokens",
                ["typical_p"] = "typical_p",
                ["repeat_penalty"] = "frequency_penalty",
                ["repeat_last_n"] = "repeat_last_n",
                ["presence_penalty"] = "presence_penalty",
                ["frequency_penalty"] = "frequency_penalty",
                ["mirostat"] = "mirostat",
                ["mirostat_tau"] = "mirostat_tau",
                ["mirostat_eta"] = "mirostat_eta",
                ["seed"] = "seed",
                ["ignore_eos"] = "ignore_eos",
                ["n_probs"] = "top_logprobs",
                ["cache_prompt"] = "cache_prompt"
            };

            return parameterMap.ContainsKey(llamaParameterName) 
                ? parameterMap[llamaParameterName] 
                : llamaParameterName;
        }

        private List<int> TokenizeEstimate(string text)
        {
            // Rough estimation: 1 token ≈ 4 characters
            // This is a simplified approximation since LM Studio doesn't expose tokenizer via API
            var tokens = new List<int>();
            int estimatedTokenCount = Mathf.Max(1, text.Length / 4);
            for (int i = 0; i < estimatedTokenCount; i++)
            {
                tokens.Add(i);
            }
            return tokens;
        }

        private string ParseStreamedResponse(string responseText, Action<string> streamCallback)
        {
            var fullResponse = "";
            var lines = responseText.Split(new[] { "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                if (line.StartsWith("data: "))
                {
                    string jsonStr = line.Substring(6);
                    if (jsonStr == "[DONE]") break;

                    try
                    {
                        var choice = JObject.Parse(jsonStr);
                        var text = choice["choices"]?[0]?["text"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(text))
                        {
                            fullResponse += text;
                            streamCallback?.Invoke(text);
                        }
                    }
                    catch { /* Skip malformed lines */ }
                }
            }

            return fullResponse;
        }

        #endregion
    }
}
