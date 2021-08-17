
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
