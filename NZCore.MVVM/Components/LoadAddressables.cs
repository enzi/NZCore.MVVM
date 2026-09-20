// <copyright project="NZCore.UI" file="LoadAddressables.cs">
// Copyright © 2026 Thomas Enzenebner. All rights reserved.
// </copyright>

using Unity.Collections;
using Unity.Entities;
using UnityEngine.UIElements;

namespace NZCore.MVVM
{
    public struct LoadAddressablesRequest : IComponentData
    {
        public bool PrintLoadedAssets;
    }

    public struct LoadCustomUIAssetsRequest : IBufferElementData
    {
        public FixedString128Bytes Key;
        public UnityObjectRef<VisualTreeAsset> Asset;
    }
}
