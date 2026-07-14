// <copyright project="NZCore.UI" file="UIHelperV2.cs">
// Copyright © 2025 Thomas Enzenebner. All rights reserved.
// </copyright>

#if UNITY_6000
using System;
using NZCore.UI;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UIElements;

namespace NZCore.MVVM
{
    /// <summary>
    /// Like UIHelper, but additionally creates and wires up a View (NZCore.MVVM) for the panel.
    /// The binding (T) is created as a transient via DI instead of new T() inside UIToolkitService.
    /// Use this when the panel has a corresponding View subclass that handles UXML construction.
    /// </summary>
    public unsafe struct UiHandle<TViewModel, TModel, TView>
        where TViewModel : BindableViewModel, IViewModelBindingNotify<TModel>, new()
        where TModel : unmanaged, IModelBinding
        where TView : View
    {
        private readonly FixedString128Bytes _uniqueKey;
        private readonly FixedString128Bytes _assetKey;
        private readonly int _priority;
        private readonly bool _visibleOnInstantiate;

        private TModel* _data;
        
        public UiHandle(string uniqueKey, string assetKey, int priority = 0, bool visibleOnInstantiate = true)
        {
            _uniqueKey = uniqueKey ?? new FixedString128Bytes();
            _assetKey = assetKey ?? new FixedString128Bytes();
            _priority = priority;
            _visibleOnInstantiate = visibleOnInstantiate;

            _data = null;
        }

        public ref TModel Model => ref UnsafeUtility.AsRef<TModel>(_data);

        /// <summary>
        /// Just adds the view to the container, no sorting.
        /// </summary>
        public (VisualElement ve, TViewModel viewModel) LoadInterface(string containerName = null, string elementName = null) =>
            LoadInterface(MvvmApplication.Instance.UIService.GetRoot(containerName), elementName);

        public (VisualElement ve, TViewModel viewModel) LoadInterface(VisualElement container, string elementName = null)
        {
            var (view, viewModel) = InternalCreateView(elementName);
            MvvmApplication.Instance.UIService.AddViewAsInterface(_uniqueKey.ToString(), view, container);

            return (view, viewModel);
        }

        /// <summary>
        /// Adds the view as a sorted panel via UIToolkitService (_priority determines order).
        /// </summary>
        public (VisualElement ve, TViewModel viewModel) LoadPanel(string containerName = null, string elementName = null) =>
            LoadPanel(MvvmApplication.Instance.UIService.GetRoot(containerName), elementName);

        public (VisualElement ve, TViewModel viewModel) LoadPanel(VisualElement container, string elementName = null)
        {
            var (view, viewModel) = InternalCreateView(elementName);
            MvvmApplication.Instance.UIService.AddViewAsPanel(_uniqueKey.ToString(), view, container, _priority);

            return (view, viewModel);
        }

        /// <summary>
        /// Ensures only one view occupies the container — evicts any existing tracked view first.
        /// </summary>
        public (VisualElement ve, TViewModel viewModel) LoadExclusive(string containerName = null, string elementName = null) =>
            LoadExclusive(MvvmApplication.Instance.UIService.GetRoot(containerName), elementName);

        public (VisualElement ve, TViewModel viewModel) LoadExclusive(VisualElement container, string elementName = null)
        {
            var (view, viewModel) = InternalCreateView(elementName);
            MvvmApplication.Instance.UIService.SetExclusiveView(_uniqueKey.ToString(), view, container);
            return (view, viewModel);
        }

        public void Unload()
        {
            MvvmApplication.Instance.UIService.RemovePanel(_uniqueKey.ToString());

            if (_data == null)
            {
                return;
            }

            if (IViewModelBindingNotify.TryGet((IntPtr)_data, out var vm) && vm is TViewModel viewModel)
            {
                viewModel.Unload();
                viewModel.Dispose(); // free the native TModel that _data points to
            }

            _data = null;
        }

        private (TView view, TViewModel viewModel) InternalCreateView(string elementName)
        {
            var viewFactory = MvvmApplication.Instance.GetService<IViewFactory>();

            // Create and initialize ViewModel
            var viewModel = viewFactory.CreateViewModel<TViewModel>();
            viewModel.Load();

            _data = (TModel*)UnsafeUtility.AddressOf(ref viewModel.Value);

            // Create and initialize View
            var assetKey = _assetKey.ToString();
            var view = !string.IsNullOrEmpty(assetKey)
                ? viewFactory.CreateViewFromUxml<TView>(assetKey, viewModel)
                : viewFactory.InitializeView<TView>(viewModel);

            if (elementName != null)
            {
                view.name = elementName;
            }

            return (view, viewModel);
        }
    }
}
#endif