# NZCore.MVVM User Guide

A guide to building Unity applications with the MVVM architecture.

## Table of Contents
- [Architecture Overview](#architecture-overview)
- [Quick Start](#quick-start)
- [Models](#models)
- [ViewModels](#viewmodels)
- [Views](#views)
- [IViewFactory](#iviewfactory)
- [ViewModelManager](#viewmodelmanager)
- [Data Binding](#data-binding)
- [Observable Collections](#observable-collections)
- [UiHandle](#uihandle)
- [Best Practices](#best-practices)

## Architecture Overview

NZCore.MVVM separates concerns across two parallel class hierarchies, both in the `NZCore.MVVM` namespace:

**ViewModel layer** — pure C#, no VisualElement dependency:
```
ViewModel
├── RootViewModel         (manages a section; auto-registers with ViewModelManager)
│   └── RootViewModel<T>  (strongly-typed model access)
└── ChildViewModel        (managed by a parent RootViewModel)
    └── ChildViewModel<T> (strongly-typed model access)

BindableViewModel         (for standalone ViewModels using Unity's native binding system)
```

**View layer** — extends VisualElement:
```
View
├── RootView   (paired with RootViewModel; manages ChildViews)
└── ChildView  (paired with ChildViewModel; managed by a RootView)
```

**Pairing**: A `View` and its `ViewModel` are linked by calling `view.InitializeView(viewModel)` (done automatically by `IViewFactory`). After that, `view.ViewModel` points to the VM, and `viewModel.AssociatedView` points to the View. The ViewModel is set as the `dataSource` for Unity's binding system.

## Quick Start

### 1. Define a Model

```csharp
using NZCore.MVVM;

public class UserModel : ObservableModel
{
    private string _name = "John";

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}
```

### 2. Create a ViewModel (pure C#)

```csharp
using NZCore.MVVM;

public class UserViewModel : ViewModel<UserModel>
{
    protected override void OnInitialize()
    {
        // Called once after the ServiceProvider is set
    }
}
```

### 3. Create a View (VisualElement)

```csharp
using NZCore.MVVM;
using UnityEngine.UIElements;

public class UserView : View
{
    public override void CreateView()
    {
        var nameField = new TextField("Name:") { bindingPath = nameof(UserViewModel.Model) + "." + nameof(UserModel.Name) };
        Add(nameField);
    }

    public override void RemoveView() { } // use for custom cleanup when removed (remove is transient removal of view, model stays alive)
    public override void DeleteView(ViewModel viewInitiator) { } // use for custom cleanup when deleted (happens when model is deleted)
}
```

### 4. Create and Display via IViewFactory

```csharp
// Resolve from DI
var factory = serviceProvider.Resolve<IViewFactory>();

// Create the ViewModel
var viewModel = factory.CreateViewModel<UserViewModel>(new UserModel());

// Create the View and link it to the ViewModel
var view = factory.InitializeView<UserView>(viewModel);

// Add to UI hierarchy
rootVisualElement.Add(view);
```

## Models

`Model` is the base class for all data objects. Each instance has a unique `Hash128` GUID.

```csharp
public abstract class Model
{
    public Hash128 Guid { get; }
    public IServiceProvider Container;          // Set by the factory
    public virtual void Cleanup() { }
    public virtual void ClearCache() { }
}
```

### ObservableModel

Adds `INotifyPropertyChanged` / `INotifyPropertyChanging` and `SetProperty<T>()`:

```csharp
using NZCore.MVVM;

public class UserModel : ObservableModel
{
    private string _firstName;
    private string _lastName;

    public string FirstName
    {
        get => _firstName;
        set => SetProperty(ref _firstName, value);
    }

    public string LastName
    {
        get => _lastName;
        set => SetProperty(ref _lastName, value);
    }

    // Computed property with manual notification
    public string FullName => $"{FirstName} {LastName}";

    public override void OnPropertyChanged(string propertyName = "")
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(FirstName) || propertyName == nameof(LastName))
        {
            base.OnPropertyChanged(nameof(FullName));
        }
    }
}
```

### BindableModel (Unity 2023.2+)

Extends `ObservableModel` with `INotifyBindablePropertyChanged` for Unity's native UI Toolkit binding runtime:

```csharp
using NZCore.MVVM;

public class UserModel : BindableModel
{
    private string _name;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}

// or use source generator
public partial class UserModel2 : BindableModel
{
    [ObservableProperty]
    private string _name;
}
```

## ViewModels

`ViewModel` is a pure C# class. It holds the model, raises property-change notifications, and resolves services via `GetService<T>()`.

### Key members

| Member | Description |
|---|---|
| `Model` | The associated `Model` instance |
| `ServiceProvider` | The injected `IServiceProvider` |
| `ViewModelManager` | The `IViewModelManager` resolved from the container |
| `AssociatedView` | The `View` linked to this ViewModel |
| `Initialize(IServiceProvider)` | Called once by the factory |
| `GetService<T>()` | Resolves a service from the container |
| `SetProperty<T>(ref field, value)` | Sets a property and raises `PropertyChanged` |
| `OnInitialize()` | Override to run setup after DI is wired |
| `OnModelChanged(old, new)` | Override to react to model replacement |
| `OnDispose()` | Override to release subscriptions |

### ViewModel lifecycle

```csharp
using NZCore.MVVM;

public class MyViewModel : ViewModel<MyModel>
{
    protected override void OnInitialize()
    {
        // DI is ready, ServiceProvider is set
        var eventBus = GetService<IEventBus>();
        eventBus.Subscribe<SomeEvent>(OnSomeEvent);
    }

    protected override void OnModelChanged(MyModel oldModel, MyModel newModel)
    {
        // Fired when ViewModel.Model is replaced
    }

    protected override void OnDispose()
    {
        GetService<IEventBus>().Unsubscribe<SomeEvent>(OnSomeEvent);
        base.OnDispose();
    }

    private void OnSomeEvent(SomeEvent e) { }
}
```

### BindableViewModel (Unity 2023.2+)

Use `BindableViewModel` when your ViewModel is used as a `dataSource` with Unity's native binding system. It implements `INotifyBindablePropertyChanged`:

```csharp
using NZCore.MVVM;

public class CounterViewModel : BindableViewModel
{
    private int _count;

    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value); // triggers INotifyBindablePropertyChanged
    }
}

// or with source generator
public partial class CounterViewModel : BindableViewModel
{
    [ObservableProperty]
    private int _count;
}
```

### RootViewModel

Extends `ViewModel`. Auto-registers itself with `IViewModelManager.RegisterRootViewModel` during `OnInitialize()`. Paired with a `RootView`.

```csharp
using NZCore.MVVM;

public class MainViewModel : RootViewModel<MainModel>
{
    protected override void OnInitialize()
    {
        base.OnInitialize(); // registers with ViewModelManager
    }
}
```

### ChildViewModel

Extends `ViewModel`. Paired with a `ChildView` and managed by a parent `RootViewModel`. Auto-registers with `IViewModelManager.RegisterChildViewModel` when the parent is set.

```csharp
using NZCore.MVVM;

public class UserChildViewModel : ChildViewModel<UserModel>
{
    protected override void OnModelChanged(UserModel oldModel, UserModel newModel)
    {
        // React to the model being set or replaced
    }
}
```

## Views

`View` extends `VisualElement`. It holds a reference to its `ViewModel` and calls `CreateView()` once during initialization.

### Key members

| Member | Description |
|---|---|
| `ViewModel` | The ViewModel linked to this View |
| `Model` | Shortcut to `ViewModel.Model` |
| `ViewModelManager` | Shortcut to `ViewModel.ViewModelManager` |
| `GetService<T>()` | Resolves via the ViewModel's ServiceProvider |
| `InitializeView(ViewModel)` | Links the VM, sets `dataSource`, calls `CreateView()` |
| `InstantiateLayout()` | Override to load UXML before `CreateView()` |
| `CreateView()` | **Abstract.** Build the UI here. |
| `OnModelChanged(old, new)` | Called when the ViewModel's model is replaced |
| `RemoveView()` | Remove from hierarchy; keep underlying data |
| `DeleteView(initiator)` | Remove from hierarchy; destroy underlying data |

### RootView

Manages a list of `ChildView`s. Use `AddChildView(childView, childViewModel)` or the factory shortcut.

```csharp
using NZCore.MVVM;
using UnityEngine.UIElements;

public class MainView : RootView
{
    public override void CreateView()
    {
        Add(new Label("Main View"));

        // Use the factory to create child view+viewmodel pairs
        var factory = GetService<IViewFactory>();
        var userModel = new UserModel { Name = "Alice" };
        factory.CreateChildView<UserChildView, UserChildViewModel>(this, userModel);
    }

    public override void RemoveView() => ViewModel?.OnUnregisterViewModel();
    public override void DeleteView(ViewModel viewInitiator) => ViewModel?.OnUnregisterViewModel();
}
```

### ChildView

Managed by a parent `RootView`. Calling `RemoveView()` or `DeleteView()` automatically removes it from the parent.

```csharp
using NZCore.MVVM;
using UnityEngine.UIElements;

public class UserChildView : ChildView
{
    public override void CreateView()
    {
        var nameLabel = new Label();
        nameLabel.SetBinding("text", new DataBinding
        {
            dataSourcePath = new PropertyPath(nameof(UserModel.Name))
        });
        Add(nameLabel);
    }
}
```

## IViewFactory

All types in `NZCore.MVVM`. Register `IViewFactory` → `ViewFactory` as a singleton in your DI container.

### ViewModel-only creation

```csharp
// Creates + initializes a ViewModel (uses the factory's injected IServiceProvider)
TViewModel vm = factory.CreateViewModel<TViewModel>();
TViewModel vm = factory.CreateViewModel<TViewModel>(existingModel);
TViewModel vm = factory.CreateViewModel<TViewModel, TModel>(); // also creates a new TModel
TViewModel vm = factory.CreateViewModel<TViewModel, TModel>(existingModel);
```

### Linking a View to an existing ViewModel

```csharp
TView view = factory.InitializeView<TView>(viewModel);
```

### UXML-based View creation

```csharp
// Finds the first TView element inside the UXML, injects deps, links to viewModel
TView view = factory.CreateViewFromUxml<TView>(uxmlKey, viewModel);

// Same but no ViewModel wiring (standalone VisualElement from UXML)
TView view = factory.CreateViewFromUxml<TView>(uxmlKey);
```

### Paired RootView + RootViewModel

```csharp
// Creates a RootViewModel, initializes it, creates the RootView, calls InitializeView
TRootView view = factory.CreateRootView<TRootView, TRootViewModel>();
TRootView view = factory.CreateRootView<TRootView, TRootViewModel, TModel>();          // also creates a new TModel
TRootView view = factory.CreateRootView<TRootView, TRootViewModel, TModel>(model);     // uses existing model
```

### Paired ChildView + ChildViewModel

```csharp
// Creates the pair, wires them, adds to rootView's UIElements hierarchy
TChildView cv = factory.CreateChildView<TChildView, TChildViewModel>(rootView);
TChildView cv = factory.CreateChildView<TChildView, TChildViewModel>(rootView, model);
TChildView cv = factory.CreateChildView<TChildView, TChildViewModel, TModel>(rootView);         // new TModel
TChildView cv = factory.CreateChildView<TChildView, TChildViewModel, TModel>(rootView, model);  // existing model

// Detached: wired but NOT added to UIElements hierarchy (for manual parenting)
TChildView cv = factory.CreateDetachedChildView<TChildView, TChildViewModel>(rootView, model);
```

## ViewModelManager

`ViewModelManager` (implementing `IViewModelManager`) tracks `RootViewModel`s and their `ChildViewModel`s by model GUID. Register it as a singleton.

`RootViewModel` auto-registers itself during `OnInitialize()`. `ChildViewModel` auto-registers when assigned a parent via `AddChildView`.

### Lookup API

```csharp
// Get a child ViewModel by model or GUID under a specific root
ChildViewModel child = viewModelManager.GetChildViewModel(model, rootViewModel);
TChildVM child    = viewModelManager.GetChildViewModel<TChildVM>(model, rootViewModel);
TChildVM child    = viewModelManager.GetChildViewModel<TChildVM>(guid, rootViewModel);

// Get all children of a root
IReadOnlyCollection<ChildViewModel> children = viewModelManager.GetChildViewModels(rootViewModel);

// Get all registered roots
IReadOnlyCollection<RootViewModel> roots = viewModelManager.GetRootViewModels();

// Model registry
viewModelManager.AddModel(model);
viewModelManager.RemoveModel(model);
Model m   = viewModelManager.GetModel(guid);
ViewModel vm = viewModelManager.GetViewModel(guid);

// Full reset
viewModelManager.Clear();
```

## Data Binding

`View.SetupDataBinding()` sets `dataSource = ViewModel` by default. Use Unity's UI Toolkit binding paths on elements you add in `CreateView()`.

### UXML template

```xml
<ui:UXML>
    <ui:TextField name="name-field" binding-path="Model.Name" label="Name:" />
    <ui:IntegerField name="age-field" binding-path="Model.Age" label="Age:" />
    <ui:Label name="display-name" binding-path="Model.FullName" />
</ui:UXML>
```

### Programmatic binding

```csharp
public override void CreateView()
{
    var visualTree = ...; // load VisualTreeAsset
    visualTree.CloneTree(this);

    // Query elements and set binding paths (dataSource is already ViewModel)
    this.Q<TextField>("name-field").bindingPath = $"{nameof(ViewModel<UserModel>.Model)}.{nameof(UserModel.Name)}";
}
```

### Loading UXML via VisualAssetStore

```csharp
public override void InstantiateLayout()
{
    InstantiateLayout("my-view-key"); // key into IVisualAssetStore
}

public override void CreateView()
{
    // Elements from UXML are already in the hierarchy
    var label = this.Q<Label>("title-label");
}
```

## Observable Collections

`ObservableCollection<T>` (in `NZCore.MVVM`) implements `IList<T>`, `INotifyCollectionChanged`, and `INotifyPropertyChanged`.

```csharp
using NZCore.MVVM;

public class TodoViewModel : BindableViewModel
{
    public ObservableCollection<string> Items { get; } = new();

    private void AddItem(string text)
    {
        Items.Add(text); // CollectionChanged fires automatically
    }

    private void ReplaceAll(IEnumerable<string> newItems)
    {
        Items.ReplaceAll(newItems);
    }

    protected override void OnDispose()
    {
        base.OnDispose();
        Items.Dispose();
    }
}
```

## UiHandle

`UiHandle<TViewModel, TModel, TView>` is a struct that owns the full lifecycle of a paired `BindableViewModel` + `View` registered with `IUIToolkitService`. It is designed for use in Burst-compatible ECS systems where the handle must live in unmanaged memory, but it works equally well in managed code.

**Requires Unity 6000+** (`#if UNITY_6000`).

### Type constraints

| Type parameter | Constraint | Purpose |
|---|---|---|
| `TViewModel` | `BindableViewModel`, `IViewModelBindingNotify<TModel>`, `new()` | The ViewModel; must expose the unmanaged model via `IViewModelBindingNotify` |
| `TModel` | `unmanaged`, `IModelBinding` | The model struct; pinned in memory so Burst jobs can write to it directly |
| `TView` | `View` | The paired View |

Because `TModel` is `unmanaged`, the struct can store a raw pointer to the model data inside the pinned ViewModel, giving Burst systems zero-copy write access.

### Declaration

```csharp
// Typically a field on a system or manager
private UiHandle<HudViewModel, HudModel, HudView> _hud;
```

Constructor parameters: `uniqueKey` (identifies the panel in `IUIToolkitService`), `assetKey` (key into `IVisualAssetStore` for the UXML, or empty string to skip UXML loading), `priority` (sort order for `LoadPanel`), `visibleOnInstantiate`.

```csharp
_hud = new UiHandle<HudViewModel, HudModel, HudView>(
    uniqueKey: "hud",
    assetKey: "HudView",
    priority: 10
);
```

### Loading

Choose one load mode when showing the UI:

```csharp
// Sorted by priority alongside other panels
var (ve, viewModel) = _hud.LoadPanel();

// Unsorted — just added to the container
var (ve, viewModel) = _hud.LoadInterface();

// Exclusive — evicts any existing view tracked under the same key
var (ve, viewModel) = _hud.LoadExclusive();
```

All three variants accept an optional `containerName` (or `VisualElement container`) to target a named root, and an optional `elementName` to set `view.name` after creation.

### Accessing the model from Burst

After loading, `ref TModel Model` returns a ref to the model data pinned inside the ViewModel. Burst jobs can hold a pointer to it:

```csharp
// In a Burst-compatible system:
ref var model = ref _hud.Model;
model.Health = currentHealth;
model.Ammo   = currentAmmo;
```

### Accessing the ViewModel and View

```csharp
TViewModel vm   = _hud.ViewModel;
TView      view = _hud.View;
```

### Unloading

```csharp
_hud.Unload(); // removes from IUIToolkitService, calls Unload() on ViewModel, frees GCHandles
```

### Full example

```csharp
using NZCore.MVVM;



// ViewModel bridges the unmanaged model to the binding system
public partial class HudViewModel : BindableViewModel, IViewModelBindingNotify<HudModel.Data>
{
    private Data _data;
    public ref Data Value => ref _data;
    // Expose bindable properties that forward to Value.*
    
    [ObservableProperty] 
    private int _health;
    
    // Unmanaged model — writable from Burst jobs
    public struct Data : IModelBindingNotify
    {
        private int _health;
        
        public int Health
        {
            get => _health;
            set 
            {
                if (_health != value)
                {
                    _health = value;
                    this.Notify();
                }
            }
        }
    }
    
    // boilerplate to notify
    public void OnPropertyChanged(in FixedString64Bytes property)
    {
        base.OnPropertyChanged(property.ToString());
    }
}

// View builds the UI
public class HudView : View
{
    // boilerplate for views that are created via uxml
    public override void CreateView() { }
    public override void RemoveView() { }
    public override void DeleteView(ViewModel viewInitiator) { }
}

// System that owns the handle
public partial struct HudSystem : ISystem, ISystemStartStop
{
    private UiHandle<HudViewModel, HudModel, HudView> _hud;

    public void OnCreate(ref SystemState state)
    {
        // do NOT create and load in OnCreate
        // always use OnStartRunning, init order is dependent on it
        state.RequireForUpdate<HudEnabled>();
        state.RequireForUpdate<UIAssetsLoaded>() // when you load via uxml the visual asset store has to be loaded first 
    }
    
    public void OnStartRunning(ref SystemState state)
    {
        _hud = new("hud-id", "hud-view-key", priority: 1);
        _hud.LoadPanel();
        // _hud.Model.Init(); optional when you want to allocate NativeContainers for example 
    }
    public void OnStopRunning(ref SystemState state)
    {
        // _hud.Model.Dispose(); optional when you need dispose
        _hud.Unload();
    }

    public void OnUpdate(int health, int ammo)
    {
        ref var model = ref _hud.Model;
        model.Health = health;
        model.Ammo   = ammo;
    }

    public void OnDestroy(ref SystemState state) { }
}
```

## Best Practices

### Service Registration order

```csharp
// Infrastructure first, then business services, then MVVM services
container.Register<IVisualAssetStore, VisualAssetStore>(ServiceLifetime.Singleton);
container.Register<IViewModelManager, ViewModelManager>(ServiceLifetime.Singleton);
container.Register<IViewFactory, ViewFactory>(ServiceLifetime.Singleton);

// Business services
container.Register<IUserService, UserService>(ServiceLifetime.Scoped);
```

### Always call base in RootViewModel.OnInitialize

`RootViewModel.OnInitialize()` registers the ViewModel with `ViewModelManager`. Skipping the base call breaks the lookup system.

```csharp
protected override void OnInitialize()
{
    base.OnInitialize(); // required — registers with ViewModelManager
    // your setup here
}
```

### Dispose ViewModels when removing Views

`RemoveChildView` on `RootView` already calls `childView.ViewModel?.Dispose()`. For manually managed ViewModels:

```csharp
var view = factory.InitializeView<MyView>(viewModel);
// ...later...
view.RemoveFromHierarchy();
viewModel.Dispose();
```

### Use strongly-typed generic base classes

Prefer `ViewModel<TModel>`, `RootViewModel<TModel>`, `ChildViewModel<TModel>` over the untyped base classes. They give you a strongly-typed `Model` property and typed override hooks (`OnModelChanged(T old, T new)`).

### Use BindableViewModel/BindableModel for Unity's native binding

Unity's runtime binding system requires `INotifyBindablePropertyChanged`. Use `BindableViewModel` and `BindableModel` (requires Unity 2023.2+) when you rely on `dataSource` + `bindingPath` without manually calling `Bind()`.

### Memory Management

```csharp
// Dispose ObservableCollections in OnDispose
protected override void OnDispose()
{
    _myCollection.Dispose();
    base.OnDispose();
}

// Unsubscribe event handlers
protected override void OnDispose()
{
    GetService<IEventBus>().Unsubscribe<MyEvent>(OnMyEvent);
    base.OnDispose();
}
```
