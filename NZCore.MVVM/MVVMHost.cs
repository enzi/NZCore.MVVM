// <copyright project="NZCore.MVVM" file="MVVMHost.cs">
// Copyright © 2026 Thomas Enzenebner. All rights reserved.
// </copyright>

using UnityEngine;
using UnityEngine.UIElements;

namespace NZCore.MVVM
{
    /// <summary>
    /// The host monobehaviour that starts the MVVM initialization process
    /// </summary>
    public class MvvmHost : MonoBehaviour
    {
        public UIDocument UIDocument;

        private MvvmApplication _app;

        private void OnEnable()
        {
            _app = new MvvmApplication(UIDocument);
            
            _app.InitializeRoot();
            
            _app.OnReady += OnInitialized;
        }

        protected virtual void OnInitialized() { }

        private void OnDisable()
        {
            if (_app == null)
            {
                return;
            }

            OnShuttingDown();
            _app.OnReady -= OnInitialized;

            UIDocument?.rootVisualElement?.Clear();

            _app.Shutdown();
            _app = null;
        }

        protected virtual void OnShuttingDown() { }
    }
}