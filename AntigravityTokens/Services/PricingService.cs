using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AntigravityTokens.Models;

namespace AntigravityTokens.Services
{
    public class PricingService
    {
        private readonly string _configFilePath;
        private readonly Dictionary<string, ModelPricing> _pricingDict = new(StringComparer.OrdinalIgnoreCase);

        public PricingSettings Settings { get; private set; } = new();

        public PricingService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDir = Path.Combine(appData, "AntigravityTokens");
            Directory.CreateDirectory(appDir);
            _configFilePath = Path.Combine(appDir, "pricing.json");

            LoadDefaults();
            LoadFromFile();
        }

        private void LoadDefaults()
        {
            _pricingDict.Clear();
            AddPricing("gemini-3.8-flash", "Gemini 3.8 Flash", 0.15, 0.60);
            AddPricing("gemini-3.7-flash", "Gemini 3.7 Flash", 0.15, 0.60);
            AddPricing("gemini-3.7-flash-exp-b", "Gemini 3.7 Flash Exp B", 0.15, 0.60);
            AddPricing("gemini-3.7-flash-exp-a", "Gemini 3.7 Flash Exp A", 0.15, 0.60);
            AddPricing("gemini-3.7-flash-control", "Gemini 3.7 Flash Control", 0.15, 0.60);
            AddPricing("gemini-3p7-flash-exp-d", "Gemini 3.7 Flash Exp D", 0.15, 0.60);
            AddPricing("gemini-3-flash-a", "Gemini 3 Flash A", 0.15, 0.60);
            AddPricing("gemini-3.6-flash", "Gemini 3.6 Flash", 0.15, 0.60);
            AddPricing("gemini-default", "Gemini Default", 0.15, 0.60);
            AddPricing("gemini-pro-default", "Gemini Pro Default", 1.25, 5.00);
            AddPricing("gemini-pro-a", "Gemini Pro A", 1.25, 5.00);
            AddPricing("claude-opus-4-6-thinking", "Claude Opus 4.6 (Thinking)", 15.00, 75.00);
            AddPricing("claude-3-5-sonnet", "Claude 3.5 Sonnet", 3.00, 15.00);
            AddPricing("default", "默认兜底 (Fallback)", 0.20, 0.80);
        }

        private void AddPricing(string modelId, string displayName, double promptPrice, double outputPrice)
        {
            _pricingDict[modelId] = new ModelPricing
            {
                ModelId = modelId,
                DisplayName = displayName,
                PromptPricePerMillion = promptPrice,
                OutputPricePerMillion = outputPrice
            };
        }

        public void LoadFromFile()
        {
            if (!File.Exists(_configFilePath))
                return;

            try
            {
                string json = File.ReadAllText(_configFilePath);
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Settings", out var settingsProp))
                {
                    Settings = JsonSerializer.Deserialize<PricingSettings>(settingsProp.GetRawText()) ?? new PricingSettings();
                }

                if (doc.RootElement.TryGetProperty("Pricing", out var pricingProp))
                {
                    var customList = JsonSerializer.Deserialize<List<ModelPricing>>(pricingProp.GetRawText());
                    if (customList != null)
                    {
                        foreach (var item in customList)
                        {
                            _pricingDict[item.ModelId] = item;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PricingService] Load error: {ex.Message}");
            }
        }

        public void SaveToFile()
        {
            try
            {
                var payload = new
                {
                    Settings,
                    Pricing = new List<ModelPricing>(_pricingDict.Values)
                };
                string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PricingService] Save error: {ex.Message}");
            }
        }

        public IEnumerable<ModelPricing> GetAllPricing() => _pricingDict.Values;

        public void UpdatePricing(ModelPricing item)
        {
            _pricingDict[item.ModelId] = item;
            SaveToFile();
        }

        public void ResetDefaults()
        {
            LoadDefaults();
            Settings = new PricingSettings();
            SaveToFile();
        }

        public ModelPricing GetRate(string model)
        {
            if (_pricingDict.TryGetValue(model, out var exact))
                return exact;

            string mLower = model.ToLowerInvariant();
            if (mLower.Contains("flash"))
                return _pricingDict.GetValueOrDefault("gemini-3.8-flash", _pricingDict["default"]);
            if (mLower.Contains("pro"))
                return _pricingDict.GetValueOrDefault("gemini-pro-default", _pricingDict["default"]);
            if (mLower.Contains("claude") || mLower.Contains("opus"))
                return _pricingDict.GetValueOrDefault("claude-opus-4-6-thinking", _pricingDict["default"]);

            return _pricingDict["default"];
        }

        public double CalculateCost(long promptTokens, long outputTokens, string model)
        {
            var rate = GetRate(model);
            double promptUsd = (promptTokens / 1_000_000.0) * rate.PromptPricePerMillion;
            double outputUsd = (outputTokens / 1_000_000.0) * rate.OutputPricePerMillion;
            double totalUsd = promptUsd + outputUsd;

            if (Settings.Currency == "CNY")
                return totalUsd * Settings.UsdToCnyRate;

            return totalUsd;
        }
    }
}
