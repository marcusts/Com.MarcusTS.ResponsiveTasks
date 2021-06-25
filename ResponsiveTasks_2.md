## Part 2 of N: Events & Messaging
Xamarin Events do not provide Task signatures. Before you write to me and tell me that you have figured out a way around this limitation, such as:

<pre lang='cs'>
public SomeConstructor()
{
   BindingContextChanged += async (sender, args) => { await SomeMethod().WithoutChangingContext(); };
}
</pre>
...  let's face facts: this event is raised as follows:
<pre lang='cs'>
BindingContextChanged?.Invoke(this, args);
</pre>
***That is an illegal root!!!***  It is ***not*** awaited.

## Solution: Responsive Tasks

The ResponsiveTasks library is a drop-in replacement for Microsoft events.  It is completely task-based.  It is multi-cast, so supports any number of listeners. And it offers many other highly nuanced capabilities that far exceed the nuts-and-bolts approach of System.Events.

#### The Old Way

Event host:
<pre lang='cs'>
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
</pre>
Event Consumer:
<pre lang='cs'>
public class MyBadConsumer
{
   public MyBadConsumer(MyBadHost host)
   {
      // Falsely rooted async call
      host.IsTrueChanged += async (b) => await HandleIsTrueChanged().WithoutChangingContext();
   }

   private Task HandleIsTrueChanged(object sender, bool e)
   {
      // Do something
      return Task.CompletedTask;
   }
}
</pre>
#### The New Way -- Responsive Tasks

This is a well-designed Event host that avoids the pitfalls of a property setter:
<pre lang='cs'>
public class MyGoodHost
{
   private bool _isTrue;

   // Defaults to AwaitAllSeparately_IgnoreFailures; fully configurable
   public IResponsiveTasks IsTrueChanged { get; set; } = new ResponsiveTasks(1);

   // Hide the illegal TPL setter; provide only read-only access
   public bool IsTrue 
   { 
      get => _isTrue;
   }
   
   // Encourage the caller to await this Task to keep it rooted and reliable
   public async Task SetIsTrue(bool isTrue)
   {
      _isTrue = isTrue;
      // The param is passed here as a simple Boolean
      await IsTrueChanged.RunTaskUsingDefaults(new object[] { isTrue }).WithoutChangingContext();
   }
}
</pre>
Event Consumer:
<pre lang='cs'>
public class MyGoodConsumer
{
   public MyGoodConsumer(MyGoodHost host)
   {
      // Subscribe to the task; safe to do here because we do not await.
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
</pre>
### Global Messages
Prism originated the idea of a **Messaging Center**. These are "tricks" to allow one part of the program to listen to another without any formal connection. 
#### The Old Way
Sender:
<pre lang='cs'>
MessagingCenter.Send<MainPage, string>(this, "Hi", "John");
</pre>
Receiver:
<pre lang='cs'>
MessagingCenter.Subscribe<MainPage> (this, "Hi", async (sender) =>
{
    // Cannot use async legally here; the message is not awaited when broadcast
    await ImproperylCallSomeTask().WithoutChangingContext();
});
</pre>
This is not magic.  The Messaging Center is simply a public static class that manages varied requests.  But it does ***not*** await, so is yet another *false root* that produces unexpected results at run-time.

#### The New Way -- Responsive Tasks
This can go anywhere, but is often placed inside the main app:
<pre lang='cs'>
public sealed class App : Application
{
   public static IResponsiveTasksDict MessagingCenterUsingResponsiveTasks { get; private set; } = new ResponsiveTasksDict();
   IResponsiveTasks MainPageChangedTask = new ResponsiveTasks(1);

   public App()
   {
      // Add the page change to the dict for coherence
      MessagingCenterUsingResponsiveTasks.Add(nameof(AssignMainPage), MainPageChangedTask);
   }

   public async Task AssignMainPage(ContentPage page)
   {
      MainPage = page;
      await PageChangedTask.AwaitAllTasksUsingDefaults(new[] { page }).WithoutChangingContext();
   }
}
</pre>
The page change task can be consumed easily from anywhere:
<pre lang='cs'>
public class MyClass
{
   public MyClass()
   {
      if (App.MessagingCenterUsingResponsiveTasks.ContainsKey(nameof(App.AssignMainPage)))
      {
         // Get the task and subscribe
         var respTask = App.MessagingCenterUsingResponsiveTasks[nameof(App.AssignMainPage)];
         respTask.AddIfNotAlreadyThere(this, HandleAppMainPageChanged);
      }
   }

   public async Task HandleAppMainPageChanged(IResponsiveTaskParams paramdict)
   {
      // Do anything; can legally await here.
   }
}
</pre>
But the new messaging center can be used for anything:
<pre lang='cs'>
public class BroadcastClass
{
   public BroadcastClass()
   {
      // Hijack he App's messaging center for our own purposes here:
      if (!App.MessagingCenterUsingResponsiveTasks.ContainsKey(nameof(DoSomethingTask)))
      {
         // Add our task for safe storage
         App.MessagingCenterUsingResponsiveTasks.Add( nameof(DoSomethingTask),DoSomethingTask);
      }
   }

   public IResponsiveTasks DoSomethingTask { get; set; } = new ResponsivTasks(1);
   
   public async Task CallDoSomethingTask(object something)
   {
      await DoSomethingTask.AwaitAllTasksUsingDefaults(new[] { something }).WithoutChangingContext();
   }
}
</pre>
This can be consumed any time by any other class:
<pre lang='cs'>
public class ConsumingClass
{
   public ConsumingClass()
   {
      if (App.MessagingCenterUsingResponsiveTasks.ContainsKey(nameof(BroadcastClass.DoSomethingTask)))
      {
         // Get the task and subscribe
         var respTask = App.MessagingCenterUsingResponsiveTasks[nameof(BroadcastClass.DoSomethingTask)];
         respTask.AddIfNotAlreadyThere(this, HandleDoSomethingTask);
      }
   }

   public async Task HandleDoSomethingTask(IResponsiveTaskParams paramdict)
   {
      // Do anything; can legally await here.
   }
}
</pre>
*To free up any ties between classes, you can develop a naming strategy for the keys to the app's MessagingCenterUsingResponsiveTasks.  Then you would not have to use nameof(), so could remain separate from where the responsive task(s) originated.*
### Important Guidance for Replacing Events and Messages with Responsive Tasks
As always with TPL, the strategy is to originate requests from a method with a Task signature. Without this, you are just transferring the false root of a task to another location, and often, hiding it from prying eyes.
