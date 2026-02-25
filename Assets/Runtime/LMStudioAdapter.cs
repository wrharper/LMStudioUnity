/// @file
/// @brief LM Studio client adapter for compatibility with existing LLMUnity code
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UndreamAI.LlamaLib;

namespace LLMUnity
{
    /// <summary>
    /// Adapter that makes LMStudioClient compatible with the LlamaLib.LLMClient interface.
    /// This allows LM Studio to be used as a drop-in replacement for the local LLM server.
    /// 
    /// Usage:
    /// - Used automatically by LLMClient when remote=true
    /// - Wraps LMStudioClient (HTTP-based) to implement LLMLocal interface
    /// - Provides seamless integration with existing LLMUnity code
    /// 
    /// Features:
    /// - Full OpenAI-compatible API support via LM Studio
    /// - Streaming completions
    /// - Embedd access
    /// - LORA adapter support (via LoraPreprocessor)
    /// - Parameter mapping (converts LlamaLib params to OpenAI API format)
    /// 
    /// Limitations (documented in LM_STUDIO_SETUP.md):
    /// - Grammar constraints not supported in LM Studio API
    /// - Tokenization returns estimates (4 chars ≈ 1 token)
    /// - Detokenization estimates only
    /// 
    /// Default Configuration:
    /// - Host: localhost
    /// - Port: 1234 (LM Studio default)
    /// - API Key: optional (for authenticated servers)
    /// 
    /// Example:
    /// <code>
    /// var adapter = new LMStudioAdapter("localhost", 1234);
    /// var result = await adapter.CompletionAsync("Say hello");
    /// </code>
    /// </summary>
    public class LMStudioAdapter : LLMLocal
    {
        private LMStudioClient lmStudioClient;
        private JObject cachedParameters;

        public LMStudioAdapter(string host = "localhost", int port = 1234, string apiKey = "", int numRetries = 5)
        {
            lmStudioClient = new LMStudioClient(host, port, apiKey, numRetries);
            cachedParameters = new JObject();
            llamaLib = null; // No native library needed for LM Studio
        }

        public override bool IsServerAlive()
        {
            return lmStudioClient.IsServerAlive().Result;
        }

        public override List<int> Tokenize(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException(nameof(content));

            return lmStudioClient.Tokenize(content).Result;
        }

        public override string Detokenize(List<int> tokens)
        {
            if (tokens == null)
                throw new ArgumentNullException(nameof(tokens));

            return lmStudioClient.Detokenize(tokens).Result;
        }

        public override List<float> Embeddings(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException(nameof(content));

            return lmStudioClient.GetEmbeddings(content).Result;
        }

        public override void SetCompletionParameters(JObject parameters)
        {
            if (parameters != null)
            {
                cachedParameters = parameters;
            }
        }

        public override void SetGrammar(string grammar)
        {
            lmStudioClient.SetGrammar(grammar);
        }

        public override void Cancel(int idSlot)
        {
            lmStudioClient.Cancel(idSlot);
        }

        public override string ApplyTemplate(JArray messages)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            return lmStudioClient.ApplyTemplate(messages).Result;
        }

        public override async Task<string> CompletionAsync(string prompt, LlamaLib.CharArrayCallback callback = null, int idSlot = -1)
        {
            if (string.IsNullOrEmpty(prompt))
                throw new ArgumentNullException(nameof(prompt));

            // Convert callback to Action<string> for LMStudioClient
            Action<string> streamCallback = null;
            if (callback != null)
            {
                streamCallback = (chunk) => 
                {
                    // Wrap for IL2CPP compatibility if needed
                    try
                    {
                        callback(chunk);
                    }
                    catch { /* Ignore callback errors */ }
                };
            }

            return await lmStudioClient.Completion(prompt, cachedParameters, streamCallback);
        }

        public override JObject GetCompletionParameters()
        {
            return cachedParameters ?? new JObject();
        }

        public override void Dispose()
        {
            // LM Studio client doesn't need explicit cleanup
            base.Dispose();
        }
    }
}
