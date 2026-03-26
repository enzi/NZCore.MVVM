// <copyright project="NZCore.UI" file="UIToolkitService.cs">
// Copyright © 2025 Thomas Enzenebner. All rights reserved.
// </copyright>

#if UNITY_6000
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NZCore.UI;
using NZCore.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace NZCore.MVVM
{
    public interface IUIToolkitService
    {
        VisualElement GetRoot(string containerName = null);

        VisualElement AddInterface(string assetKey, bool visibleOnInstantiate = true);
        VisualElement AddInterface(string assetKey, string containerName = null, bool visibleOnInstantiate = true);
        VisualElement AddInterface(string assetKey, VisualElement rootContainer, bool visibleOnInstantiate = true);

        (VisualElement, T) AddBindableInterface<T>(string uniqueKey, string assetKey, string elementName = null, int order = 0, bool visibleOnInstantiate = true)
            where T : class, IViewModelBindingNotify, new();
        (VisualElement, T) AddBindableInterface<T>(string uniqueKey, string assetKey, string containerName = null, string elementName = null, int order = 0, bool visibleOnInstantiate = true)
            where T : class, IViewModelBindingNotify, new();
        (VisualElement, T) AddBindableInterface<T>(string uniqueKey, string assetKey, VisualElement rootContainer, string elementName = null, int order = 0, bool visibleOnInstantiate = true)
            where T : class, IViewModelBindingNotify, new();

        VisualElement AddPanel(string uniqueKey, VisualTreeAsset asset, string elementName = null, int order = 0, bool visibleOnInstantiate = true);
        VisualElement AddPanel(string uniqueKey, VisualTreeAsset asset, VisualElement rootContainer, string elementName = null, int order = 0, bool visibleOnInstantiate = true);
        VisualElement AddPanel(string uniqueKey, string assetKey, string elementName = null, int order = 0, bool visibleOnInstantiate = true);
        VisualElement AddPanel(string uniqueKey, string assetKey, string containerName = null, string elementName = null, int order = 0, bool visibleOnInstantiate = true);
        VisualElement AddPanel(string uniqueKey, string assetKey, VisualElement rootContainer, string elementName = null, int order = 0, bool visibleOnInstantiate = true);

        (VisualElement, T) AddBindablePanel<T>(string uniqueKey, string assetKey, string containerName = null, string elementName = null, int order = 0, bool visibleOnInstantiate = true)
            where T : class, IViewModelBindingNotify, new();
        (VisualElement, T) AddBindablePanel<T>(string uniqueKey, string assetKey, VisualElement rootContainer, string elementName = null, int order = 0, bool visibleOnInstantiate = true)
            where T : class, IViewModelBindingNotify, new();

        IViewModelBindingNotify RemovePanel(string uniqueKey);

        void AddViewAsInterface(string uniqueKey, VisualElement view, VisualElement container);
        void AddViewAsPanel(string uniqueKey, VisualElement view, VisualElement container, int order);
        void SetExclusiveView(string uniqueKey, VisualElement view, VisualElement container);
        VisualElement ReplaceView(string uniqueKey, VisualElement newView, VisualElement container);

        bool TryLoad(string uniqueKey, string assetKey, out VisualElement ve, bool visibleOnInstantiate = true, string elementName = null);
        VisualElement CloneAndAdd(string uniqueKey, VisualTreeAsset asset, bool visibleOnInstantiate = true, string elementName = null);
        bool TryLoad<T>(string uniqueKey, string assetKey, VisualElement rootContainer, out (VisualElement ve, T binding) container, bool visibleOnInstantiate = true, string elementName = null)
            where T : class, IViewModelBindingNotify, new();
        bool TryLoad<T>(string uniqueKey, string assetKey, out (VisualElement ve, T binding) container, bool visibleOnInstantiate = true, string elementName = null)
            where T : class, IViewModelBindingNotify, new();
        void Load<T>(string uniqueKey, VisualTreeAsset asset, out (VisualElement ve, T binding) container, bool visibleOnInstantiate = true, string elementName = null)
            where T : class, IViewModelBindingNotify, new();

        bool TryUnload(string uniqueKey, out (VisualElement Element, IViewModelBindingNotify Binding) container);
        bool UnloadOrderedPanel();

        void RegisterElement(string key, VisualElement element);
        bool TryGet(string key, out VisualElement element);
    }

    [UsedImplicitly]
    public class UIToolkitService : IUIToolkitService
    {
        private readonly UIRootContainer _container;
        private readonly IVisualAssetStore _visualAssetStore;

        private readonly Dictionary<string, (VisualElement Element, IViewModelBindingNotify Binding)> _loadedPanels = new();
        private readonly Dictionary<string, VisualElement> _registeredElements = new();

        private readonly List<OrderedElement> _sortedPanels = new();

        public UIToolkitService(UIRootContainer container, IVisualAssetStore visualAssetStore)
        {
            _container = container;
            _visualAssetStore = visualAssetStore;
        }

        public VisualElement GetRoot(string containerName = null) => containerName == null ? _container.Root : _container.Root.Q<VisualElement>(containerName);

        public VisualElement AddInterface(string assetKey, bool visibleOnInstantiate = true) => AddInterface(assetKey, _container.Root, visibleOnInstantiate);

        public VisualElement AddInterface(string assetKey, string containerName = null, bool visibleOnInstantiate = true) =>
            AddInterface(assetKey, GetRoot(containerName), visibleOnInstantiate);

        public VisualElement AddInterface(string assetKey, VisualElement rootContainer, bool visibleOnInstantiate = true)
        {
            if (_visualAssetStore.TryGetAsset(assetKey, out var asset))
            {
                return asset.CloneSingleTree(rootContainer, visibleOnInstantiate);
            }

            Debug.LogError($"Key {assetKey} was not found in assets!");
            return null;
        }

        public (VisualElement, T) AddBindableInterface<T>(string uniqueKey, string assetKey, string elementName = null, int order = 0,
            bool visibleOnInstantiate = true)
            where T : class, IViewModelBindingNotify, new() =>
            AddBindableInterface<T>(uniqueKey, assetKey, _container.Root, elementName, order, visibleOnInstantiate);

        public (VisualElement, T) AddBindableInterface<T>(string uniqueKey, string assetKey, string containerName = null, string elementName = null,
            int order = 0, bool visibleOnInstantiate = true)
            where T : class, IViewModelBindingNotify, new()
        {
            var rootContainer = GetRoot(containerName);
            return AddBindableInterface<T>(uniqueKey, assetKey, rootContainer, elementName, order, visibleOnInstantiate);
        }

        public (VisualElement, T) AddBindableInterface<T>(string uniqueKey, string assetKey, VisualElement rootContainer, string elementName = null,
            int order = 0, bool visibleOnInstantiate = true)
            where T : class, IViewModelBindingNotify, new()
        {
            if (string.IsNullOrEmpty(assetKey))
            {
                return (_container.Root, default);
            }

            if (TryLoad<T>(uniqueKey, assetKey, out var container, visibleOnInstantiate, elementName))
            {
                rootContainer.Add(container.ve);
            }

            return container;
        }

        public VisualElement AddPanel(string uniqueKey, VisualTreeAsset asset, string elementName = null, int order = 0, bool visibleOnInstantiate = true)
        {
            var ve = CloneAndAdd(uniqueKey, asset, visibleOnInstantiate, elementName);
            AddAsSortablePanel(ve, order);
            return ve;
        }

        public VisualElement AddPanel(string uniqueKey, VisualTreeAsset asset, VisualElement rootContainer, string elementName = null, int order = 0,
            bool visibleOnInstantiate = true)
        {
            var ve = CloneAndAdd(uniqueKey, asset, visibleOnInstantiate, elementName);
            AddAsSortablePanel(rootContainer, ve, order);
            return ve;
        }

        public VisualElement AddPanel(string uniqueKey, string assetKey, string elementName = null, int order = 0, bool visibleOnInstantiate = true) =>
            AddPanel(uniqueKey, assetKey, _container.Root, elementName, order, visibleOnInstantiate);

        public VisualElement AddPanel(string uniqueKey, string assetKey, string containerName = null, string elementName = null, int order = 0,
            bool visibleOnInstantiate = true) => AddPanel(uniqueKey, assetKey, GetRoot(containerName), elementName, order, visibleOnInstantiate);

        public VisualElement AddPanel(string uniqueKey, string assetKey, VisualElement rootContainer, string elementName = null, int order = 0,
            bool visibleOnInstantiate = true)
        {
            if (string.IsNullOrEmpty(assetKey))
            {
                return _container.Root;
            }

            if (TryLoad(uniqueKey, assetKey, out var ve, visibleOnInstantiate, elementName))
            {
                AddAsSortablePanel(rootContainer, ve, order);
            }

            return ve;
        }

        private void AddAsSortablePanel(VisualElement ve, int order)
        {
            AddAsSortablePanel(_container.Root, ve, order);
        }

        private void AddAsSortablePanel(VisualElement rootContainer, VisualElement ve, int order)
        {
            var oe = new OrderedElement(ve, order);
            _sortedPanels.Add(oe);
            _sortedPanels.Sort();

            var index = _sortedPanels.IndexOf(oe);
            rootContainer.Insert(index, ve);
        }

        public (VisualElement, T) AddBindablePanel<T>(string uniqueKey, string assetKey, string containerName = null, string elementName = null, int order = 0,
            bool visibleOnInstantiate = true)
            where T : class, IViewModelBindingNotify, new() =>
            AddBindablePanel<T>(uniqueKey, assetKey, GetRoot(containerName), elementName, order, visibleOnInstantiate);

        public (VisualElement, T) AddBindablePanel<T>(string uniqueKey, string assetKey, VisualElement rootContainer, string elementName = null, int order = 0,
            bool visibleOnInstantiate = true)
            where T : class, IViewModelBindingNotify, new()
        {
            if (string.IsNullOrEmpty(assetKey))
            {
                return (_container.Root, default);
            }

            if (TryLoad<T>(uniqueKey, assetKey, out var container, visibleOnInstantiate, elementName))
            {
                var oe = new OrderedElement(container.ve, order);
                _sortedPanels.Add(oe);
                _sortedPanels.Sort();

                var index = _sortedPanels.IndexOf(oe);
                rootContainer.Insert(index, container.ve);
            }

            return container;
        }

        public IViewModelBindingNotify RemovePanel(string uniqueKey) => TryUnload(uniqueKey, out var panel) ? panel.Binding : null;

        public void AddViewAsInterface(string uniqueKey, VisualElement view, VisualElement container)
        {
            container.Add(view);
            _loadedPanels.Add(uniqueKey, (view, null));
            if (view is IViewTransition t)
            {
                t.AnimateEnter(view);
            }
        }

        public void AddViewAsPanel(string uniqueKey, VisualElement view, VisualElement container, int order)
        {
            AddAsSortablePanel(container, view, order);
            _loadedPanels.Add(uniqueKey, (view, null));
            if (view is IViewTransition t)
            {
                t.AnimateEnter(view);
            }
        }

        public void SetExclusiveView(string uniqueKey, VisualElement view, VisualElement container)
        {
            string evictKey = null;
            foreach (var (key, entry) in _loadedPanels)
            {
                if (entry.Element.parent == container)
                {
                    evictKey = key;
                    break;
                }
            }

            if (evictKey != null)
            {
                TryUnload(evictKey, out _);
            }

            AddViewAsInterface(uniqueKey, view, container);
        }

        public VisualElement ReplaceView(string uniqueKey, VisualElement newView, VisualElement container)
        {
            VisualElement oldView = null;
            if (TryUnload(uniqueKey, out var oldEntry))
            {
                oldView = oldEntry.Element;
            }

            container.Add(newView);
            _loadedPanels.Add(uniqueKey, (newView, default));

            return oldView;
        }

        public bool TryLoad(string uniqueKey, string assetKey, out VisualElement ve, bool visibleOnInstantiate = true, string elementName = null)
        {
            if (string.IsNullOrEmpty(assetKey))
            {
                ve = _container.Root;
                return true;
            }

            if (_visualAssetStore.TryGetAsset(assetKey, out var asset))
            {
                ve = CloneAndAdd(uniqueKey, asset, visibleOnInstantiate, elementName);
                return true;
            }
            else
            {
                Debug.LogError($"Key {assetKey} was not found in assets!");
                ve = _container.Root;
                return false;
            }
        }

        public VisualElement CloneAndAdd(string uniqueKey, VisualTreeAsset asset, bool visibleOnInstantiate = true, string elementName = null)
        {
            var ve = asset.CloneSingleTree(visibleOnInstantiate);

            if (elementName != null)
            {
                ve.name = elementName;
            }

            _loadedPanels.Add(uniqueKey, (ve, null));

            return ve;
        }

        public bool TryLoad<T>(string uniqueKey, string assetKey, VisualElement rootContainer, out (VisualElement ve, T binding) container,
            bool visibleOnInstantiate = true, string elementName = null)
            where T : class, IViewModelBindingNotify, new()
        {
            if (string.IsNullOrEmpty(assetKey))
            {
                container = (_container.Root, default);
                return true;
            }

            if (_visualAssetStore.TryGetAsset(assetKey, out var asset))
            {
                var ve = asset.CloneSingleTree(rootContainer, visibleOnInstantiate);
                var binding = new T();
                ve.dataSource = binding;

                if (elementName != null)
                {
                    ve.name = elementName;
                }

                _loadedPanels.Add(uniqueKey, (ve, binding));
                container = (ve, binding);
                return true;
            }
            else
            {
                Debug.LogError($"Key {assetKey} was not found in assets!");
                container = (_container.Root, default);
                return false;
            }
        }

        public bool TryLoad<T>(string uniqueKey, string assetKey, out (VisualElement ve, T binding) container, bool visibleOnInstantiate = true,
            string elementName = null)
            where T : class, IViewModelBindingNotify, new()
        {
            if (string.IsNullOrEmpty(assetKey))
            {
                container = (_container.Root, default);
                return true;
            }

            if (_visualAssetStore.TryGetAsset(assetKey, out var asset))
            {
                Load(uniqueKey, asset, out container, visibleOnInstantiate, elementName);
                return true;
            }
            else
            {
                Debug.LogError($"Key {assetKey} was not found in assets!");
                container = (_container.Root, null);
                return false;
            }
        }

        public void Load<T>(string uniqueKey, VisualTreeAsset asset, out (VisualElement ve, T binding) container, bool visibleOnInstantiate = true,
            string elementName = null)
            where T : class, IViewModelBindingNotify, new()
        {
            var ve = asset.CloneSingleTree(visibleOnInstantiate);
            var binding = new T();
            ve.dataSource = binding;

            if (elementName != null)
            {
                ve.name = elementName;
            }

            _loadedPanels.Add(uniqueKey, (ve, binding));
            container = (ve, binding);
        }

        public bool TryUnload(string uniqueKey, out (VisualElement Element, IViewModelBindingNotify Binding) container)
        {
            if (_loadedPanels.Remove(uniqueKey, out container))
            {
                if (TryFind(container.Element, out var orderedElement))
                {
                    _sortedPanels.Remove(orderedElement);
                }

                var element = container.Element;
                if (element is IViewTransition t)
                {
                    t.AnimateExit(element, element.RemoveFromHierarchy);
                }
                else
                {
                    element.RemoveFromHierarchy();
                }

                return true;
            }

            return false;
        }

        public bool UnloadOrderedPanel()
        {
            if (_sortedPanels.Count == 0)
            {
                return false;
            }

            var topPanel = _sortedPanels[^1];
            // todo, destroy or hide?
            _sortedPanels.RemoveAt(_sortedPanels.Count - 1);

            return true;
        }

        private bool TryFind(VisualElement element, out OrderedElement foundElement)
        {
            foreach (var orderedElement in _sortedPanels)
            {
                if (orderedElement.VisualElement == element)
                {
                    foundElement = orderedElement;
                    return true;
                }
            }

            foundElement = default;
            return false;
        }

        public void RegisterElement(string key, VisualElement element)
        {
            _registeredElements.Add(key, element);
        }

        public bool TryGet(string key, out VisualElement element) => _registeredElements.TryGetValue(key, out element);

        private readonly struct OrderedElement : IComparable<OrderedElement>, IEquatable<OrderedElement>
        {
            private readonly VisualElement _ve;
            private readonly int _order;

            public VisualElement VisualElement => _ve;

            public OrderedElement(VisualElement visualElement, int order)
            {
                _ve = visualElement;
                _order = order;
            }

            public int CompareTo(OrderedElement other) => _order.CompareTo(other._order);

            public bool Equals(OrderedElement other) => _ve.Equals(other._ve);

            public override int GetHashCode() => _ve.GetHashCode();
        }
    }
}
#endif
