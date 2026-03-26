// <copyright project="NZCore.UI" file="UIRootContainer.cs">
// Copyright © 2025 Thomas Enzenebner. All rights reserved.
// </copyright>

#if UNITY_6000
using UnityEngine.UIElements;

namespace NZCore.MVVM
{
    public class UIRootContainer
    {
        public static UIRootContainer Instance;

        public UIDocument UIDocument { get; private set; }
        public VisualElement Root { get; private set; }
        public VisualElement DragContainer { get; private set; }
        public VisualElement DragImage { get; private set; }
        public VisualElement TooltipContainer { get; private set; }

        public UIRootContainer(UIDocument uiDocument)
        {
            Instance = this;

            UIDocument = uiDocument;
            Root = uiDocument.rootVisualElement.Q<VisualElement>("root");
            DragContainer = uiDocument.rootVisualElement.Q<VisualElement>("dragContainer");
            TooltipContainer = uiDocument.rootVisualElement.Q<VisualElement>("tooltipContainer");

            if (DragContainer != null)
            {
                DragImage = DragContainer.Q<VisualElement>("dragImage");
            }
        }
    }
}
#endif
