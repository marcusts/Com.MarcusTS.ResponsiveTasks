https://demo.hedgedoc.org/s/sp4x1TFrP

# Fixing TPL, Part 1 of N: Responsive Tasks

When Microsoft announced the Task Parallel Library, C# programmers everywhere rejoiced.  Simplified threads?  Error handling within the library itself?  What could possibly go wrong?

Just about everything, as it turns out.

A task requires a **root** in order to be properly awaited.  For instance:

```
// An awaitable task
public Task YouCanAwaitMe() { }

// A root where you can await a task
public async Task IWillAwait()
{
await YouCanAwaitMe()
}
```

But there a lot of dead spots in an app where there is no root.  For instance:

* A constructor
* Most overrides
* Properties
* Event handlers

Any method that fails to provide a Task signature is a *false root*. Xamarin.Forms doesn't currently provide many legal roots.  They have to fabricated through programming tricks.  This causes unsafe results:

```
public class FalselyRootedView : ContentView
{
   protected override async void OnBindingContextChanged()
   {
      base.OnBindingContextChanged();
      
      // Mega hack -- called from a void method (illegal!)
      await StartUpViewModel().ConfigureAwait(false);
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
      await SomeOtherTask();
   }
}
```

Until Microsoft converts all current code signatures to Tasks, programmers are stuck using these sorts of risky mechanisms.

## Case In Point: Event Handlers

Before you write to me and tell me that you have figured out a way around this limitation, such as:

```
public SomeConstructor()
{
   BindingContextChanged += async (sender, args) => { await SomeMethod(); };
}
```
Please face facts: this event was raised as follows:
```
BindingContextChanged?.Invoke(this, args);
```
***That is an illegal root!!!***  It is ***not*** awaited.

## Solution: Responsive Tasks

The ResponsiveTasks library is a drop-in replacement for Microsoft events.  It is completely task-based.  It is multi-cast, so supports any number of listeners. And it offers many other highly nuanced capabilities that far exceed the nuts-and-bolts approach of System.Events.

### The Old Way -- Events

Event host:
```
public class MyBadHost
{
   private bool _isTrue;
      
   public event EventHandler<bool> IsTrueChanged;

   public bool IsTrue
   {
      get => _isTrue;
      set
      {
         _isTrue = value;
         IsTrueChanged?.Invoke(this, _isTrue);
      }
   }
}
```
Event Consumer:
```
public class MyBadConsumer
{
   public MyBadConsumer(MyBadHost host)
   {
      // Falsely rooted async call
      host.IsTrueChanged += async (b) => await HandleIsTrueChanged();
   }

   private Task HandleIsTrueChanged(object sender, bool e)
   {
      // Do something
      return Task.CompletedTask;
   }
}
```
### The New Way -- Responsive Tasks

Event host:
```
public class MyGoodHost
{
   private bool _isTrue;

   // Defaults to AwaitAllSeparately_IgnoreFailures; fully configurable
   public IResponsiveTasks IsTrueChanged { get; set; } = new ResponsiveTasks(1);

   public bool IsTrue
   {
      get => _isTrue;
      set
      {
         _isTrue = value;
         
         // Can still use this, though improperly rooted
         //    FireAndForget is a standard utility that runs a Task from a void 
         //    signature using try/catch.  It doesn't cure any ills; it just 
         //    isolates and protects better than loose code. 
         SetIsTrue(_isTrue).FireAndForget();
      }
   }

   // Properly designed for awaiting a Task
   public async Task SetIsTrue(bool isTrue)
   {
      // The param is passed here as a simple Boolean
      await IsTrueChanged.RunTaskUsingDefaults(new object[] { isTrue });
   }
}
```
Event Consumer:
```
public class MyGoodConsumer
{
   public MyGoodConsumer(MyGoodHost host)
   {
      // Subscribe to the task; not illegal
      host.IsTrueChanged.AddIfNotAlreadyThere(this, HandleIsTrueChanged);
   }

   // Handle the task using a task
   private Task HandleIsTrueChanged(IResponsiveTaskParams paramDict)
   {
      // Get the params formally and with type safety in the first position:
      var boolParam = paramDict.GetTypeSafeValue<bool>(0);
      
      // OR instead of this, just fuh-get-about-it:
      boolParam = (bool)paramDict[0];
      
      // Do something with the param
      return Task.CompletedTask;
   }
}
```
### Responsive Tasks Features

#### On Creating Tasks
* Can assign any parameter count; on firing the task, the provided parameters must match that count or an error will result.
* Can run the tasks in parallel or consecutively (default).
* Can respond to errors as the tasks run or not (default).
* Can set the error level (debug output vs. modal dialog, etc.)
* Can provide a custom error handler

 #### On Handling Tasks
 * Can get the parameters with type safety (recommended)
 * Can get parameters directly through array referencing (unsafe)
 * Upon request to handle a hosted task, if that subscription already exists, it is ignored - NO duplicate subscriptions as with events!
 * Very well-behaved storage model; subscribed tasks do not mis-behaved like subscribed events do on disposal of the listening class.
