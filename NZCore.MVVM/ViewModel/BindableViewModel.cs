// <copyright project="NZCore.MVVM" file="BindableViewModel.cs">
// Copyright © 2026 Thomas Enzenebner. All rights reserved.
// </copyright>

#if UNITY_2023_2_OR_NEWER
using System;
using System.Runtime.CompilerServices;
using NZCore.UI;
using Unity.Collections;
using UnityEngine.UIElements;

namespace NZCore.MVVM
{
    /// <summary>
    /// Base class for ViewModels that need to support UI Toolkit data binding.
    /// </summary>
    [ObservableObject]
    public abstract unsafe partial class BindableViewModel<T> : BindableViewModel, IViewModelBindingNotify<T>
        where T : unmanaged
    {
        private readonly T* _data;
        
        public ref T Value => ref *_data;

        public BindableViewModel()
        {
            _data = AllocatorManager.Allocate<T>(Allocator.Persistent);
            *_data = default;
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            AllocatorManager.Free(Allocator.Persistent, _data);
        }

        public virtual void OnPropertyChanging(in FixedString64Bytes property) { }
        public virtual void OnPropertyChanged(in FixedString64Bytes property) => base.OnPropertyChanged(property.ToString());
    }
    
    
    /// <summary>
    /// Do not implement this, use BindableViewModel<T> instead!
    /// </summary>
    public abstract class BindableViewModel : ViewModel, INotifyBindablePropertyChanged
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        public override void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            base.OnPropertyChanged(propertyName);
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(propertyName));
        }

        internal override void OnRegisterViewModel() { }
        internal override void OnUnregisterViewModel() { }

        protected override void OnDispose()
        {
            base.OnDispose();
            propertyChanged = null;
        }
    }
}
#endif
