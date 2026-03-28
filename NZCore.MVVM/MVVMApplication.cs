// <copyright project="NZCore.MVVM" file="MVVMApplicationServices.cs">
// Copyright © 2025 Thomas Enzenebner. All rights reserved.
// </copyright>

using System;
using NZCore.Inject;
using UnityEngine;
using UnityEngine.UIElements;
using IServiceProvider = NZCore.Inject.IServiceProvider;

namespace NZCore.MVVM
{
    /// <summary>
    /// Application-level MVVM services container that provides global dependency injection
    /// for the MVVM framework. This is the composition root for all MVVM services.
    /// </summary>
    public class MvvmApplication
    {
        public static MvvmApplication Instance;

        private readonly UIDocument _uiDocument;
        public event Action OnReady;

        private IServiceProvider _container;
        public UIRootContainer Container;
        public UIToolkitService UIService;
        public VisualAssetStore VisualAssetStore;

        public MvvmApplication()
        {
            Instance = this;

            _container = new ServiceProvider();
            _container.RegisterSingleton(_container); // register self
            RegisterCoreServices();
        }

        public MvvmApplication(UIDocument uiDocument)
        {
            Instance = this;
            _uiDocument = uiDocument;

            _container = new ServiceProvider();
            _container.RegisterSingleton(_container); // register self
            RegisterCoreServices();
        }

        /// <summary>
        /// Creates a scoped container for window or component-specific services.
        /// </summary>
        /// <returns>A new scoped Service Provider.</returns>
        public IServiceProvider CreateScope() => _container.CreateScope();

        /// <summary>
        /// Registers core MVVM services with the application container.
        /// </summary>
        private void RegisterCoreServices()
        {
            // Core MVVM services
            _container.Register<IViewFactory, ViewFactory>(ServiceLifetime.Singleton);
            _container.Register<IViewModelManager, ViewModelManager>(ServiceLifetime.Singleton);
            _container.Register<IVisualAssetStore, VisualAssetStore>(ServiceLifetime.Singleton);

            // Navigation services (placeholder)

            // _container.Register<INavigationService>(ServiceLifetime.Singleton, 
            //     container => System.Activator.CreateInstance(
            //         System.Type.GetType("NZCore.MVVM.Navigation.NavigationService"), 
            //         container) as INavigationService);
        }

        /// <summary>
        /// Allows registration of additional application-level services.
        /// Call this during application startup after Initialize().
        /// </summary>
        /// <param name="registerAction">Action to register additional services.</param>
        public void RegisterServices(Action<IServiceProvider> registerAction)
        {
            if (_container == null)
            {
                Debug.LogError("MVVMApplicationServices must be initialized before registering additional services.");
                return;
            }

            registerAction?.Invoke(_container);
        }

        /// <summary>
        /// Resolves a service from the application container.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service instance.</returns>
        public T GetService<T>() where T : class => _container.Resolve<T>();

        public void InitializeRoot()
        {
            Container = new UIRootContainer(_uiDocument);

            _container.RegisterSingleton(Container);
            _container.Register<IUIToolkitService, UIToolkitService>(ServiceLifetime.Singleton);

            UIService = (UIToolkitService) _container.Resolve<IUIToolkitService>();

            OnReady?.Invoke();
        }

        /// <summary>
        /// Clears the application services container. 
        /// This should only be used for testing or application shutdown.
        /// </summary>
        public void Shutdown()
        {
            if (_container is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _container = null;
        }
    }
}