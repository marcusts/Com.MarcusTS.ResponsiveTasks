## Part 3 of N: Technical Guide
---
### Creating Responsive Tasks with Parameters
The examples provided here are quite basic.  Let's delve into the full capabilities of ResponsiveTasks by looking at how the can be created and consumed.

**Remember:** When creating a ResponsiveTask, the parameter count ***must*** be known and ***obeyed*** when raising that task.

This is the *default* ResponsiveTask declaration:
<pre lang='cs'>
public IResponsiveTask BasicRespTask { get; set; } = new ResponsiveTask(1);
</pre>
It has ***one*** parameter. So when raised:
<pre lang='cs'>
public async Task RaiseBasicRespTask(object someParam)
{
   await BasicRespTask.RunAllTasksUsingDefaults(
      new[] { someParam }).WithoutChangingContext();
}
</pre>
The parameters to the task were never declared, so they were automatically created using integers. On consumption, one ***must*** refer to the parameters exactly like this:
<pre lang='cs'>
public async Task HandleBasicRespTask(object[] paramDict)
{
   //  You don't know or care what type the variable is.
   var retrievedParam = paramDict[0];
}
</pre>
For better type safety, use this approach:
<pre lang='cs'>
public async Task HandleBasicRespTask(object[] paramDict)
{
   // If the parameter is not an object, this raises an error, 
   //    though the error handling defaults to
   //    You will not receive a parameter unless the type matches.
   var retrievedParam = paramDict.GetTypeSafeValue<object>(0);
}
</pre>
Another option is to declare the task with *named* params:
<pre lang='cs'>
public IResponsiveTask NamedRespTask { get; set; } = 
   new ResponsiveTask("param1", "param2");
</pre>
This doesn't anything much about the broadcast:
<pre lang='cs'>
public async Task RaiseBasicRespTask(int someParam1, string someParam2)
{
   // With params of varying types, the compiler will ask 
   //    they be boxed as an object array:
   await BasicRespTask.RunAllTasksUsingDefaults(
      new object[] { someParam1, someParam2 }).WithoutChangingContext();
}
</pre>
But on consumption, you ***must*** request the params by name or you will receive an error and no params in return:
<pre lang='cs'>
public async Task HandleNamedRespTask(object[] paramDict)
{
   // Either:
   var retrievedParam1 = paramDict["param1"];
   var retrievedParam2 = paramDict["param2"];
   // Or:
   var retrievedParam1 = paramDict.GetTypeSafeValue<int>("param1");
   var retrievedParam2 = paramDict.GetTypeSafeValue<string>("param2");
}
</pre>
---
### Raising ResponsiveTasks for Different Effects
**ResponsiveTasks** are first declared and then *raised*, just like events.  But how they are raised makes a big difference in the result. The default is the safest, and is what occurs when you raise a **Responsive Task** like this:
<pre lang='cs'>
// No parameters in this example
private IResponsiveTasks AnyTask { get; set; } = new ResponsiveTask();

public async Task RaiseATask()
{  
   // Raise the task with the default RunHow property, which is never stated literally
   // No params are passed because none were declared
   await AnyTask.RunAllTasksUsingDefaults().WithoutChangingContext();
   
   // This is the same as:
   // await AnyTask.AwaitAllTasksConsecutively(false).WithoutChangingContext();
}
</pre>
But you can easily set the default RunHow property for any task manually:
<pre lang='cs'>
// Set RunHow at the constructor
private IResponsiveTasks AnyTask { get; set; } = 
   new ResponsiveTask { RunHow = HowToRun.AwaitAllCollectively };

public async Task RaiseATask()
{  
   // Raise as per the original example -- using 'defaults'
   await AnyTask.RunAllTasksUsingDefaults().WithoutChangingContext();
   
   // This is *now* the same as:
   // await AnyTask.AwaitAllTasksCollectively().WithoutChangingContext();
}
</pre>
Here are the available values for RunHow, and what they accomplish:
#### *AwaitAllConsecutively_IgnoreFailures *(default)**
>Awaits each task handler/subscriber and moves on to the next one after completion, even if there is an error, which is ***ignored***.
#### *AwaitAllConsecutively_StopOnFirstFailure*
>Awaits each task handler/subscriber and moves on to the next one after completion, but ***stops*** on any error and ***abandons*** all tasks thereafter.
#### *AwaitAllCollectively*
>Awaits using WhenAll so all task handlers/subscribers run at the same time, and finish after the ***longest*** of these completes.
>
>***Important:*** *Errors are ignored because the group process is cannot be easily canceled.*
#### *RunAllInParallel*
>Similar to **AwaitAllCollectively** except there is no await.  All handlers/subscribers run at the same time, on a **background thread**.  The run-time flow continues ***immediately***, before any tasks have completed. Use this with ***extreme*** precaution!
>
>***Important:*** *Errors are ignored because the group process is launched and then left to run on its own.*

#### The Cancellation Dilemma
When we use the term *error* here, we mean ***any*** error, including task cancellation, which throws a C# run-time exception. However, **ResponsiveTasks** do not manage how cancellation affects other running tasks.  That is ***your*** responsibility, and it is a big one.

When you assign a handler for a **ResponsiveTask**, it might support cancellation:
<pre lang='cs'>
public class MyClass
{
   public MyClass()
   {
      SomeResponsiveTask.AddIfNotAlreadyThere(this, HandleSomeResponsiveTask);
   }
   
   private async Task TaskWithCancellation(CancellationTokenSource tokenSource)
   {
      var randomNumber = *{any random number}*;
      await Task.Delay(randomNumber).WithoutChangingContext();
      // Self-cancel for demo purposes.
      tokenSource.Cancel();
   }
   
   private async Task HandleSomeResponsiveTask(object[] paramDict)
   {
      var CancellationTokenSource tokenSource = new CancellationTokenSource();
      await TaskWithCancellation(tokenSource).WithoutChangingContext();
   }
}
</pre>
Let's say you do this several times, in different places, all listening/handling **SomeResponsiveTask**.  Now the task gets raised with RunHow set to *AwaitAllCollectively*, where errors are not managed at all:
<pre lang='cs'>
public class SomeClass
{
   public IResponsiveTask SomeResponsiveTask { get; set; } = 
      new ResponsiveTasks { RunHow = HowToRun.AwaitAllCollectively };
   
   public async Task RaiseSomeResponsiveTask()
   {
      // Though we await, there is no way to manage errors, including task cancellation
      await SomeResponsiveTask.RunAllTasksUsingDefaults().WithoutChangingContext();
   }
}
</pre>
Your handlers start dying.  But you assumed that would all succeed.  So it is your job to see to it that on failure, they all fail as a group. This is a bit daunting, so cannot be covered here.

The best option for now is to run using RunHow.AwaitAllConsecutively_StopOnFirstFailure if errors are indeed a concern.  In this example, that would cause the ResponsiveTask to halt and abandon all tasks.  It does ***not*** cause all tasks to stop.  They ***continue*** running and their results are ***ignored***.  This wastes CPU time, so is not an ideal solution.

---
### How XAML Affects Xamarin and Responsive Tasks
There is more divisive issue in Xamarin programming than the peculiar existence of XAML, an XML/Text language that purports to *"make our lives easier"* by creating nested declarations for controls and views inside pages.

XAML is only here because of the Expression Blend editor.  That editor output XAML so it could be digested by WPF (and now Xamarin).  But the editor was intended for amateurs to ***prevent*** them from worrying about learning any ***real*** programming. Nobody uses it anymore. Yet XAML remains.

This is far more than a semantic discussion, or a personal choice.  XAML has very real consequences on modern apps.  As we advance into neww and better ways to program, including **ResponsiveTasks**, XAML feels ever more antiquated and burdensome.
