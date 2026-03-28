// <copyright project="NZCore.MVVM" file="VisualAssetStore.cs">
// Copyright © 2026 Thomas Enzenebner. All rights reserved.
// </copyright>

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NZCore.MVVM
{
    public interface IVisualAssetStore
    {
        VisualTreeAsset GetAsset(string key);
        bool TryGetAsset(string key, out VisualTreeAsset vta);
        void RegisterAssets(Dictionary<string, VisualTreeAsset> uiAssetsVisualTreeAssets);
    }

    public class VisualAssetStore : IVisualAssetStore
    {
        private readonly Dictionary<string, VisualTreeAsset> _visualTreeAssets = new();

        public VisualTreeAsset GetAsset(string key)
        {
            if (_visualTreeAssets.TryGetValue(key, out var vta))
            {
                return vta;
            }

            Debug.LogError($"VisualTreeAsset key {key} not found!");
            return null;
        }

        public bool TryGetAsset(string key, out VisualTreeAsset vta)
        {
            return _visualTreeAssets.TryGetValue(key, out vta);
        }

        public void RegisterAssets(Dictionary<string, VisualTreeAsset> uiAssetsVisualTreeAssets)
        {
            foreach (var kvPair in uiAssetsVisualTreeAssets)
            {
                if (!_visualTreeAssets.TryAdd(kvPair.Key, kvPair.Value))
                {
                    Debug.LogError($"Duplicate key {kvPair.Key} in VisualAssetStore");
                }
            }
        }
    }
}
