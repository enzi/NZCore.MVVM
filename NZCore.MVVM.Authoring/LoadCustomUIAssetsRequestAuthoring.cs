// <copyright project="NZCore.UI.Authoring" file="LoadCustomUIAssetsRequestAuthoring.cs">
// Copyright © 2026 Thomas Enzenebner. All rights reserved.
// </copyright>

using System.Collections.Generic;
using NZCore.MVVM;
using Unity.Entities;
using UnityEngine;

namespace NZCore.UI.Authoring
{
    public class LoadCustomUIAssetsRequestAuthoring : MonoBehaviour
    {
        public List<CustomUIAsset> CustomAssets;

        private class LoadCustomUIAssetsRequestBaker : Baker<LoadCustomUIAssetsRequestAuthoring>
        {
            public override void Bake(LoadCustomUIAssetsRequestAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var customAssets = AddBuffer<LoadCustomUIAssetsRequest>(entity);
                if (authoring.CustomAssets == null)
                {
                    return;
                }

                foreach (var customAsset in authoring.CustomAssets)
                {
                    DependsOn(customAsset.Asset);
                    customAssets.Add(new LoadCustomUIAssetsRequest
                    {
                        Key = customAsset.Key,
                        Asset = customAsset.Asset
                    });
                }
            }
        }
    }
}
