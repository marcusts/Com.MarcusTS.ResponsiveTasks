
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

## The Proof is in the Output

This is a digested Debug output from the demo when I originally created it. The demo called Tasks from all of the forbidden areas, including constructors. It was otherwise well-behaved -- at least according to Microsoft's guidance.  So it resembles code that anyone would produce who has done the reading:

| Location                            | Task Type                            | First/Last |
| :---                                | :---                                 | :---                   |
| Views.Subviews.DashboardView  &nbsp;|&nbsp; RunPostConstructionTasks &nbsp;|&nbsp; FIRST	&nbsp;|&nbsp;
| ViewModels.DashboardViewModel &nbsp;|&nbsp; RunPostConstructionTasks &nbsp;|&nbsp; FIRST	&nbsp;|&nbsp;
| ViewModels.DashboardViewModel &nbsp;|&nbsp; RunPostBindingTasks      &nbsp;|&nbsp; FIRST	&nbsp;|&nbsp;
| Views.Subviews.DashboardView  &nbsp;|&nbsp; RunPostBindingTasks      &nbsp;|&nbsp; FIRST	&nbsp;|&nbsp;
|-------------------------------&nbsp;|&nbsp;--------------------------&nbsp;|&nbsp;------------&nbsp;|&nbsp;
| Views.Subviews.DashboardView  &nbsp;|&nbsp; RunPostConstructionTasks &nbsp;|&nbsp; LAST	&nbsp;|&nbsp;
| ViewModels.DashboardViewModel &nbsp;|&nbsp; RunPostConstructionTasks &nbsp;|&nbsp; LAST	&nbsp;|&nbsp; 
| ViewModels.DashboardViewModel &nbsp;|&nbsp; RunPostBindingTasks      &nbsp;|&nbsp; LAST	&nbsp;|&nbsp; 
| Views.Subviews.DashboardView  &nbsp;|&nbsp; RunPostBindingTasks      &nbsp;|&nbsp; LAST	&nbsp;|&nbsp;
|-------------------------------&nbsp;|&nbsp;--------------------------&nbsp;|&nbsp;------------&nbsp;|&nbsp;



*Everything runs immediately and on top of each other.  Nothing ever forms properly before something else piles on top and tries to rely on some imagined statefulness. This is what causes programs to hang and to crash.*

## The Most Obvious Solution is Also the Worst

So how do we achieve atomic completeness for each Task with no overlaps?  How about this:

```csharp
public async void IncorrectlyRaiseATaskWithABlockingCall()
{
   await SomeTask.Wait().WithoutChangingContext();
}

```

Ironically, this solves concurrency issues because it only proceeds ***after*** completing a task.  But that comes at an enormous cost: ***100% of the UI thread***. The user immediately senses their keyboard has died. **Wait** is a rusty razor blade in the bottom of your tool-belt.

## The Right Solution: Responsive Tasks

The Responsive Tasks library handles all of the dilemmas mentioned here using a thread-safe "wait" strategy, plus base classes that support Tasks everywhere.  You can easily copy the code samples into your own base views or view models, so this approach is not dogmatic.

Here is the output in the ***final*** demo. Everything is orderly now.  Every process is stateful and predictable:

| Location                            | Task Type                            | First/Last |
| :---                                | :---                                 | :---                   |
| Views.Subviews.DashboardView  &nbsp;|&nbsp; RunPostConstructionTasks &nbsp;|&nbsp; FIRST	&nbsp;|&nbsp;
| Views.Subviews.DashboardView  &nbsp;|&nbsp; RunPostConstructionTasks &nbsp;|&nbsp; LAST	&nbsp;|&nbsp;
|------------------------------------&nbsp;|&nbsp;------------------------------&nbsp;|&nbsp;-------&nbsp;|&nbsp;
| ViewModels.DashboardViewModel &nbsp;|&nbsp; RunPostConstructionTasks &nbsp;|&nbsp; FIRST	&nbsp;|&nbsp;
| ViewModels.DashboardViewModel &nbsp;|&nbsp; RunPostConstructionTasks &nbsp;|&nbsp; LAST	&nbsp;|&nbsp; 
|------------------------------------&nbsp;|&nbsp;------------------------------&nbsp;|&nbsp;-------&nbsp;|&nbsp;
| ViewModels.DashboardViewModel &nbsp;|&nbsp; RunPostBindingTasks      &nbsp;|&nbsp; FIRST	&nbsp;|&nbsp;
| ViewModels.DashboardViewModel &nbsp;|&nbsp; RunPostBindingTasks      &nbsp;|&nbsp; LAST	&nbsp;|&nbsp; 
|------------------------------------&nbsp;|&nbsp;------------------------------&nbsp;|&nbsp;-------&nbsp;|&nbsp;
| Views.Subviews.DashboardView  &nbsp;|&nbsp; RunPostBindingTasks      &nbsp;|&nbsp; FIRST	&nbsp;|&nbsp;
| Views.Subviews.DashboardView  &nbsp;|&nbsp; RunPostBindingTasks      &nbsp;|&nbsp; LAST	&nbsp;|&nbsp;
|------------------------------------&nbsp;|&nbsp;------------------------------&nbsp;|&nbsp;-------&nbsp;|&nbsp;

## Responsive Tasks is tested and proven

See the [unit tests](https://github.com/marcusts/ResponsiveTasks.UnitTests).

## Index

Each page describes a problem and its Responsive solution:

## [Part 1 of N: Pages, Views & View Models](https://github.com/marcusts/Com.MarcusTS.ResponsiveTasks/blob/main/docs/ResponsiveTasks_1.md)
## [Part 2 of N: Events & Messaging](https://github.com/marcusts/Com.MarcusTS.ResponsiveTasks/blob/main/docs/ResponsiveTasks_2.md)
## [Part 3 of N: Technical Guide](https://github.com/marcusts/Com.MarcusTS.ResponsiveTasks/blob/main/docs/ResponsiveTasks_3.md)
## [Part 4 of N: Button Pressed: Closing the Final TPL Gaps](https://github.com/marcusts/Com.MarcusTS.ResponsiveTasks/blob/main/docs/ResponsiveTasks_4.md)

## Responsive Tasks Is Open Source; Enjoy Our Other Offerings

If you find value in this software, consider these other related projects:

### *Shared Utils*

[GutHub](https://github.com/marcusts/Com.MarcusTS.SharedUtils)

[NuGet](https://www.nuget.org/packages/Com.MarcusTS.SharedUtils)

### *The Smart DI Container*

[GutHub](https://github.com/marcusts/Com.MarcusTS.SmartDI)

[NuGet](https://www.nuget.org/packages/Com.MarcusTS.SmartDI)

### *Shared Forms*

[GutHub](https://github.com/marcusts/Com.MarcusTS.SharedForms)

[NuGet](https://www.nuget.org/packages/Com.MarcusTS.SharedForms)

### *Responsive Tasks*

[GutHub](https://github.com/marcusts/Com.MarcusTS.ResponsiveTasks)

[NuGet](https://www.nuget.org/packages/Com.MarcusTS.ResponsiveTasks)

### *Responsive Tasks - Xamarin.Forms Support*

[GutHub](https://github.com/marcusts/Com.MarcusTS.ResponsiveTasks.XamFormsSupport)

[NuGet](https://www.nuget.org/packages/Com.MarcusTS.ResponsiveTasks.XamFormsSupport)

### *The Modern App Demo*

[GutHub](https://github.com/marcusts/Com.MarcusTS.ModernAppDemo)

&nbsp;
![](https://gitlab.com/marcusts1/nugetimages/-/raw/master/Modern_App_Demo_Master_FINAL.gif)

