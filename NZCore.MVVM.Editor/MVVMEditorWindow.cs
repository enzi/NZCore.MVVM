// <copyright project="Assembly-CSharp" file="MVVMEditorWindow.cs">
// Copyright © 2025 Thomas Enzenebner. All rights reserved.
// </copyright>

using UnityEditor;

namespace NZCore.MVVM.Editor
{
    public abstract class MvvmEditorApplication : EditorWindow
    {
        protected MvvmApplication App;
        protected IViewFactory ViewFactory;

        private void CreateGUI()
        {
            App = new MvvmApplication();
            ViewFactory = App.GetService<IViewFactory>();

            CreateView();
        }

        protected abstract void CreateView();
    }
}