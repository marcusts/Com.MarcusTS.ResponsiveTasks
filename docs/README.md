> ## NOW MAUI READY !!!

# Fixing TPL Using Responsive Tasks:

## TPL: The Promise

When Microsoft announced the Task Parallel Library, C# programmers everywhere rejoiced.  Simplified threads?  Error handling within the library itself?  What could possibly go wrong?

Just about everything, as it turns out.

A task requires a **root** in order to be properly awaited.  For instance:

```csharp
// An awaitable task
public Task YouCanAwaitMe() { }

// A root where you an await a task
public async Task IWillAwait()
{
   await YouCanAwaitMe().WithoutChangingContext();
}
```

## TPL: The Reality

Unfortunately, a Xamarin app doesn't have any valid roots.  For instance, ***not*** at:

* Constructors
* Content changed
* Binding context changed
* Event handlers
* Global messages
* Overrides
* Property Setters

Any location that fails to provide a Task signature is a *false root*. This causes unsafe results:

```csharp 
public class FalselyRootedView : ContentView
{
   protected override async void OnBindingContextChanged()
   {
      base.OnBindingContextChanged();
      
      // Mega hack -- called from a void method (illegal!)
      await StartUpViewModel().WithoutChangingContext();
   }
   
   public virtual Task StartUpViewModel()
   {
      return Task.CompletedTask;
   }
}

// Derive and consume the falsely rooted view as if it were valid
public class FalseConsumer : FalselyRootedView
{
   pubic override async Task StartUpViewModel()
   {
      // Everything seems OK from this perspective, but this task can proceed at any time and 
      //    without our control; it was never properly awaited.  Anything relying on it will 
      //    accelerate into a race condition; variables will not be set on time; nothing can 
      //    be relied upon in a predictable order.
      await SomeOtherTask().WithoutChangingContext();
   }
}
```

Until Microsoft converts all current code signatures to Tasks, programmers are stuck using these sorts of risky mechanisms.

## Responsive Tasks is tested and proven

See the [unit tests](https://github.com/marcusts/ResponsiveTasks.UnitTests).

## Index

Each page describes a problem and its Responsive solution:

## [Part 1 of N: Pages, Views & View Models](https://github.com/marcusts/Com.MarcusTS.ResponsiveTasks/blob/main/docs/ResponsiveTasks_1.md)
## [Part 2 of N: Events & Messaging](https://github.com/marcusts/Com.MarcusTS.ResponsiveTasks/blob/main/docs/ResponsiveTasks_2.md)
## [Part 3 of N: Technical Guide](https://github.com/marcusts/Com.MarcusTS.ResponsiveTasks/blob/main/docs/ResponsiveTasks_3.md)
## [Part 4 of N: Button Pressed: Closing the Final TPL Gaps](https://github.com/marcusts/Com.MarcusTS.ResponsiveTasks/blob/main/docs/ResponsiveTasks_4.md)
## [Part 5 of N: Proofs](https://github.com/marcusts/Com.MarcusTS.ResponsiveTasks/blob/main/docs/ResponsiveTasks_5.md)

## ResponsiveTasks Is Open Source; Enjoy Our Entire Public Suite 

### *Shared Utils (MAUI Ready!)*

[GutHub](https://github.com/marcusts/Com.MarcusTS.SharedUtils)

[NuGet](https://www.nuget.org/packages/Com.MarcusTS.SharedUtils)

### *The Smart DI Container (MAUI Ready!)*

[GutHub](https://github.com/marcusts/Com.MarcusTS.SmartDI)

[NuGet](https://www.nuget.org/packages/Com.MarcusTS.SmartDI)

### *Responsive Tasks (MAUI Ready!)*

[GutHub](https://github.com/marcusts/Com.MarcusTS.ResponsiveTasks)

[NuGet](https://www.nuget.org/packages/Com.MarcusTS.ResponsiveTasks)

### *PlatformIndependentShared (MAUI Ready!)*

[GutHub](https://github.com/marcusts/PlatformIndependentShared)

[NuGet](https://www.nuget.org/packages/Com.MarcusTS.PlatformIndependentShared)

### *UI.XamForms*

[GutHub](https://github.com/marcusts/UI.XamForms)

[NuGet](https://www.nuget.org/packages/Com.MarcusTS.UI.XamForms)

### *The Modern App Demo*

[GutHub](https://github.com/marcusts/Com.MarcusTS.ModernAppDemo)

&nbsp;
![](https://gitlab.com/marcusts1/nugetimages/-/raw/master/Modern_App_Demo_Master_FINAL.gif)
