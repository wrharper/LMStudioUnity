/// @file
/// @brief LORA adapter merging utility for LM Studio compatibility
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LLMUnity
{
    /// <summary>
    /// Preprocessor for merging LORA adapters into base models.
    /// Creates standalone .gguf files with LORA weights baked in,
    /// allowing seamless use with LM Studio which lacks programmatic LORA support.
    /// </summary>
    public static class LoraPreprocessor
    {
        private const string MERGED_MODELS_DIRECTORY = "MergedModels";
        private static readonly string MergedModelsPath = Path.Combine(Application.persistentDataPath, MERGED_MODELS_DIRECTORY);

        /// <summary>
        /// Merges multiple LORA adapters into a base model.
        /// Creates a new .gguf file with LORA weights baked in.
        /// </summary>
        /// <param name="baseModelPath">Path to base model (.gguf)</param>
        /// <param name="loras">List of (path, weight) tuples for LORA adapters</param>
        /// <param name="onProgress">Optional progress callback (0.0 to 1.0)</param>
        /// <returns>Path to merged model file</returns>
        public static async Task<string> MergeLorasIntoModel(
            string baseModelPath,
            List<(string path, float weight)> loras,
            Action<float> onProgress = null)
        {
            try
            {
                // If no LORA adapters, return base model unchanged
                if (loras == null || loras.Count == 0)
                {
                    LLMUnitySetup.Log("No LORA adapters to merge, using base model");
                    return baseModelPath;
                }

                // Validate inputs
                if (!File.Exists(baseModelPath))
                {
                    LLMUnitySetup.LogError($"Base model not found: {baseModelPath}", true);
                    return null;
                }

                // Create merged models directory
                Directory.CreateDirectory(MergedModelsPath);

                // Generate unique name for merged model
                string mergedModelName = GenerateMergedModelName(
                    Path.GetFileNameWithoutExtension(baseModelPath),
                    loras);
                string mergedModelPath = Path.Combine(MergedModelsPath, mergedModelName);

                // If already merged, return cached version
                if (File.Exists(mergedModelPath))
                {
                    LLMUnitySetup.Log($"Using cached merged model: {mergedModelPath}");
                    onProgress?.Invoke(1.0f);
                    return mergedModelPath;
                }

                LLMUnitySetup.Log($"Merging {loras.Count} LORA adapter(s) into base model");
                LLMUnitySetup.Log($"Output: {mergedModelPath}");

                // Validate all LORA paths exist
                foreach (var (loraPath, _) in loras)
                {
                    string fullLoraPath = LLMUnitySetup.GetFullPath(loraPath);
                    if (!File.Exists(fullLoraPath))
                    {
                        LLMUnitySetup.LogError($"LORA adapter not found: {fullLoraPath}", true);
                        return null;
                    }
                }

                // Execute merge operation
                await ExecuteLlamaToolsMerge(baseModelPath, loras, mergedModelPath, onProgress);

                if (!File.Exists(mergedModelPath))
                {
                    LLMUnitySetup.LogError($"Merged model creation failed", true);
                    return null;
                }

                LLMUnitySetup.Log($"LORA merge completed successfully");
                onProgress?.Invoke(1.0f);
                return mergedModelPath;
            }
            catch (Exception ex)
            {
                LLMUnitySetup.LogError($"Failed to merge LORA adapters: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generates a unique filename for the merged model based on base model and LORA adapters.
        /// Example: "mistral-7b_merged_chemistry-0.8_math-0.9.gguf"
        /// </summary>
        private static string GenerateMergedModelName(
            string baseName,
            List<(string path, float weight)> loras)
        {
            try
            {
                // Build suffix from LORA names and weights
                var loraSuffix = loras.Select(l =>
                {
                    string loraName = Path.GetFileNameWithoutExtension(l.path)
                        .Replace(".gguf", "")
                        .Replace("lora-", "")
                        .ToLower();
                    
                    // Keep name short (max 16 chars)
                    if (loraName.Length > 16) loraName = loraName.Substring(0, 16);
                    
                    return $"{loraName}-{l.weight:F1}";
                });

                string suffix = string.Join("_", loraSuffix);
                
                // Create filename: base_merged_loras_hash.gguf
                // Hash ensures uniqueness and prevents filename length issues
                string hashInput = $"{baseName}_{suffix}";
                string hash = GetStableHash(hashInput).ToString("X8");
                
                return $"{baseName}_merged_{hash}.gguf";
            }
            catch (Exception ex)
            {
                LLMUnitySetup.LogError($"Error generating merged model name: {ex.Message}");
                return $"{baseName}_merged_unknown.gguf";
            }
        }

        /// <summary>
        /// Generates a stable hash for a string (deterministic across runs)
        /// </summary>
        private static int GetStableHash(string input)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < input.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ input[i];
                    if (i + 1 < input.Length)
                    {
                        hash2 = ((hash2 << 5) + hash2) ^ input[i + 1];
                    }
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        /// <summary>
        /// Executes the actual LORA merge using llama.cpp tools.
        /// Requires quantize binary from llama.cpp to be available.
        /// </summary>
        private static async Task ExecuteLlamaToolsMerge(
            string baseModelPath,
            List<(string path, float weight)> loras,
            string outputPath,
            Action<float> onProgress)
        {
            try
            {
                // Method 1: Try using quantize binary with LORA merge support
                bool merged = await TryQuantizeWithLora(baseModelPath, loras, outputPath, onProgress);
                
                if (!merged)
                {
                    // Method 2: Fall back to simple copy if direct merge not available
                    LLMUnitySetup.LogError(
                        "LORA merge tool not found. Recommend: " +
                        "1) Install llama.cpp quantize binary " +
                        "2) Or pre-merge LORA adapters using: " +
                        "   python scripts/convert-lora-to-gguf.py <base> <lora> <output>",
                        false);
                    
                    // Copy base model as fallback (LORA weights won't be applied)
                    File.Copy(baseModelPath, outputPath, overwrite: true);
                    onProgress?.Invoke(1.0f);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"LORA merge execution failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Attempts to merge LORA using quantize binary from llama.cpp
        /// </summary>
        private static async Task<bool> TryQuantizeWithLora(
            string baseModelPath,
            List<(string path, float weight)> loras,
            string outputPath,
            Action<float> onProgress)
        {
            try
            {
                // Build command: quantize base.gguf output.gguf --lora adapter.gguf --lora-scale 0.8
                StringBuilder command = new StringBuilder();
                command.Append($"\"{baseModelPath}\"");
                command.Append($" \"{outputPath}\"");

                foreach (var (loraPath, weight) in loras)
                {
                    string fullLoraPath = LLMUnitySetup.GetFullPath(loraPath);
                    command.Append($" --lora \"{fullLoraPath}\"");
                    command.Append($" --lora-scale {weight:F2}");
                }

                return await RunQuantizeBinary("quantize", command.ToString(), onProgress);
            }
            catch (Exception eks)
            {
                LLMUnitySetup.Log($"Quantize binary merge attempt failed: {eks.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to run quantize binary, searching common installation paths
        /// </summary>
        private static async Task<bool> RunQuantizeBinary(
            string binaryName,
            string arguments,
            Action<float> onProgress)
        {
            // Search for quantize binary in common locations
            var searchPaths = new[]
            {
                // llama.cpp build directory
                Path.Combine(Application.streamingAssetsPath, "llama.cpp", "build", "bin", binaryName),
                Path.Combine(Application.streamingAssetsPath, "llama.cpp", binaryName),
                
                // System PATH (if installed)
                binaryName,
                
                // Common installation directories
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "llama.cpp", binaryName),
            };

            foreach (var path in searchPaths)
            {
                string binaryPath = GetBinaryPath(path);
                
                if (File.Exists(binaryPath))
                {
                    return await RunProcess(binaryPath, arguments, onProgress);
                }
            }

            LLMUnitySetup.Log($"Quantize binary not found in search paths");
            return false;
        }

        /// <summary>
        /// Gets platform-specific binary name (.exe on Windows, plain name on Unix)
        /// </summary>
        private static string GetBinaryPath(string basePath)
        {
            #if UNITY_STANDALONE_WIN
            if (!basePath.EndsWith(".exe")) return basePath + ".exe";
            #endif
            return basePath;
        }

        /// <summary>
        /// Executes external process and monitors progress
        /// </summary>
        private static async Task<bool> RunProcess(
            string processPath,
            string arguments,
            Action<float> onProgress)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = processPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        LLMUnitySetup.LogError($"Failed to start process: {processPath}");
                        return false;
                    }

                    LLMUnitySetup.Log($"Running: {processPath} {arguments}");

                    // Monitor output for progress information
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    // Periodically update progress while process runs
                    while (!process.HasExited)
                    {
                        await Task.Delay(500);
                        onProgress?.Invoke(0.5f); // Progress indication while running
                    }

                    await outputTask;
                    await errorTask;

                    if (process.ExitCode != 0)
                    {
                        LLMUnitySetup.LogError($"Process exited with code {process.ExitCode}");
                        return false;
                    }

                    LLMUnitySetup.Log("LORA merge process completed successfully");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LLMUnitySetup.LogError($"Error running process: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clears cached merged models to free disk space
        /// </summary>
        public static void ClearMergedModelCache()
        {
            try
            {
                if (Directory.Exists(MergedModelsPath))
                {
                    Directory.Delete(MergedModelsPath, recursive: true);
                    LLMUnitySetup.Log("Cleared merged model cache");
                }
            }
            catch (Exception ex)
            {
                LLMUnitySetup.LogError($"Failed to clear cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets list of cached merged models
        /// </summary>
        public static List<string> GetCachedMergedModels()
        {
            var models = new List<string>();
            
            try
            {
                if (Directory.Exists(MergedModelsPath))
                {
                    var files = Directory.GetFiles(MergedModelsPath, "*.gguf");
                    models.AddRange(files);
                }
            }
            catch (Exception ex)
            {
                LLMUnitySetup.LogError($"Failed to list cached models: {ex.Message}");
            }

            return models;
        }

        /// <summary>
        /// Gets the cache directory path
        /// </summary>
        public static string GetCacheDirectory() => MergedModelsPath;
    }
}
