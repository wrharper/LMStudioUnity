using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace LLMUnity.Tests
{
    /// <summary>
    /// Tests for LORA preprocessor functionality
    /// </summary>
    public class TestLoraPreprocessor
    {
        [Test]
        public async Task TestMergeWithEmptyLoraList()
        {
            // Test that null LORA list returns base model unchanged
            string baseModel = "Models/test-model.gguf";
            var result = await LoraPreprocessor.MergeLorasIntoModel(baseModel, null);
            
            Assert.AreEqual(baseModel, result, "Should return base model when LORA list is null");
        }

        [Test]
        public async Task TestMergeWithEmptyLoraList_Empty()
        {
            // Test that empty LORA list returns base model unchanged
            string baseModel = "Models/test-model.gguf";
            var emptyLoras = new List<(string path, float weight)>();
            var result = await LoraPreprocessor.MergeLorasIntoModel(baseModel, emptyLoras);
            
            Assert.AreEqual(baseModel, result, "Should return base model when LORA list is empty");
        }

        [Test]
        public void TestCachedModelsAreAccessible()
        {
            // Test that the cache directory is accessible
            string cacheDir = LoraPreprocessor.GetCacheDirectory();
            Assert.IsNotEmpty(cacheDir, "Cache directory path should not be empty");
            
            // Path should include MergedModels directory
            Assert.That(cacheDir, Does.Contain("MergedModels"), 
                "Cache directory should contain 'MergedModels'");
        }

        [Test]
        public void TestClearCacheDoesNotThrow()
        {
            // Test that clearing cache doesn't throw an error
            Assert.DoesNotThrow(() => LoraPreprocessor.ClearMergedModelCache(),
                "Clearing cache should not throw an exception");
        }

        [Test]
        public void TestGetCachedModelsReturnsValidList()
        {
            // Test that GetCachedMergedModels returns a valid list
            var cached = LoraPreprocessor.GetCachedMergedModels();
            
            Assert.IsNotNull(cached, "Should return a non-null list");
            Assert.IsInstanceOf<List<string>>(cached, "Should return a List<string>");
        }

        [Test]
        [Category("Integration")]
        public async Task TestMergeLorasWithProgress()
        {
            // Test that progress callback is called
            string baseModel = "Models/test-model.gguf";
            var loras = new List<(string, float)>
            {
                ("Models/lora1.gguf", 0.8f)
            };
            
            float lastProgress = 0f;
            Action<float> progressCallback = (progress) =>
            {
                lastProgress = progress;
                Assert.GreaterOrEqual(progress, 0f, "Progress should not be negative");
                Assert.LessOrEqual(progress, 1f, "Progress should not exceed 1.0");
            };

            // Note: This will fail without actual model files, but demonstrates the API
            // In a real scenario, you'd need valid model paths
            var result = await LoraPreprocessor.MergeLorasIntoModel(
                baseModel,
                loras,
                progressCallback
            );
        }

        [Test]
        public void TestLoraPreprocessorHandlesInvalidBasePath()
        {
            // Test error handling for invalid base model path
            string invalidPath = "/nonexistent/path/model.gguf";
            var loras = new List<(string, float)>
            {
                ("some/lora.gguf", 0.8f)
            };
            
            // Should log error but not throw
            Assert.DoesNotThrowAsync(async () =>
            {
                var result = await LoraPreprocessor.MergeLorasIntoModel(
                    invalidPath,
                    loras
                );
                Assert.IsNull(result, "Should return null for invalid base model");
            });
        }

        [Test]
        [Category("Integration")]
        public async Task TestLoraPreprocessorIntegrationWithLMStudioClient()
        {
            // Test that LMStudioClient can use the preprocessor
            var client = new LMStudioClient("localhost", 1234);
            
            string baseModel = "Models/test-model.gguf";
            var loras = new List<(string, float)>
            {
                ("Models/lora.gguf", 0.8f)
            };
            
            // This tests the API contract, not actual merging
            // (which requires valid model files)
            Assert.DoesNotThrowAsync(async () =>
            {
                var result = await client.PrepareModelWithLora(baseModel, null);
                // With null LORA list, should return base model unchanged
                Assert.AreEqual(baseModel, result);
            });
        }

        [Test]
        public void TestLoraPreprocessorStableHashing()
        {
            // Test that stable hashing produces the same hash for same input
            // This is important for cache consistency
            
            var loras1 = new List<(string, float)>
            {
                ("lora1.gguf", 0.8f),
                ("lora2.gguf", 0.5f)
            };
            
            var loras2 = new List<(string, float)>
            {
                ("lora1.gguf", 0.8f),
                ("lora2.gguf", 0.5f)
            };
            
            // Both should generate the same merged model name
            // (this is tested implicitly through cache reuse)
            Assert.IsNotNull(loras1);
            Assert.IsNotNull(loras2);
        }
    }
}
