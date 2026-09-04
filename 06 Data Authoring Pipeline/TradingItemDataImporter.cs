#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// 화물 JSON을 ID 기준으로 기존 ScriptableObject에 반영하는 핵심 로직
public static class TradingItemDataImporter
{
    public static void Import(
        string jsonFilePath,
        string outputFolder,
        TradingItemDataBase database)
    {
        if (database == null)
            throw new ArgumentNullException(nameof(database));

        Directory.CreateDirectory(outputFolder);

        JObject jsonObject = JObject.Parse(File.ReadAllText(jsonFilePath));
        Dictionary<int, TradingItemData> existingItems = IndexExistingItems(database);
        List<TradingItemData> importedItems = new(jsonObject.Count);
        HashSet<int> importedIds = new();

        Undo.RecordObject(database, "Update Item Database");

        foreach (KeyValuePair<string, JToken> pair in jsonObject)
        {
            JToken itemToken = pair.Value;
            int itemId = itemToken.Value<int>("id");

            if (!importedIds.Add(itemId))
                throw new InvalidDataException($"중복된 화물 ID: {itemId}");

            bool isNewItem = !existingItems.TryGetValue(itemId, out TradingItemData itemData);

            if (isNewItem)
            {
                itemData = ScriptableObject.CreateInstance<TradingItemData>();
            }
            else
            {
                // 기존 참조를 유지한 채 데이터 갱신
                Undo.RecordObject(itemData, "Update Item Data");
            }

            ApplyFields(itemData, itemToken);

            if (isNewItem)
            {
                string assetName = string.IsNullOrWhiteSpace(itemData.debugName)
                    ? $"Item_{itemId}"
                    : itemData.debugName;
                string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                    Path.Combine(outputFolder, $"{assetName}.asset"));
                AssetDatabase.CreateAsset(itemData, assetPath);
            }

            EditorUtility.SetDirty(itemData);
            importedItems.Add(itemData);
        }

        database.allItems.Clear();
        database.allItems.AddRange(importedItems);
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Dictionary<int, TradingItemData> IndexExistingItems(
        TradingItemDataBase database)
    {
        Dictionary<int, TradingItemData> itemsById = new();

        foreach (TradingItemData item in database.allItems)
        {
            if (item != null)
                itemsById[item.id] = item;
        }

        return itemsById;
    }

    private static void ApplyFields(TradingItemData item, JToken token)
    {
        item.id = token.Value<int>("id");
        item.debugName = token.Value<string>("debug_name");
        item.itemName = token.Value<string>("name");
        item.description = token.Value<string>("description");
        item.planet = ParseEnum(token.Value<string>("planet"), ItemPlanet.Default);
        item.tier = ParseEnum(token.Value<string>("tier"), ItemTierLevel.Default);
        item.type = ParseEnum(token.Value<string>("type"), ItemCategory.Default);
        item.temperatureMin = token.Value<float>("temperature_min");
        item.temperatureMax = token.Value<float>("temperature_max");
        item.shape = token.Value<int>("shape");
        item.costBase = token.Value<int>("cost_base");
        item.costMin = token.Value<int>("cost_min");
        item.costMax = token.Value<int>("cost_max");
        item.costChangerate = token.Value<float>("cost_changerate");
        item.capacity = token.Value<int>("capacity");
    }

    private static T ParseEnum<T>(string value, T fallback) where T : struct
    {
        return Enum.TryParse(value, true, out T parsed) ? parsed : fallback;
    }
}
#endif
