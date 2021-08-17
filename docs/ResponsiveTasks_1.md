## Part 1 of N: Pages, Views & View Models 

### The Old Way

A Xamarin.Forms ContentPage or ContentView provide their Content and BindingContext through simple properties.  View Models are constructed and assigned as binding contexts, but provide no support whatsoever for the *moment* of being bound.  All of these cases lack Task signatures, so are ***false roots***.
<pre lang='cs'>
public ContentView AnyContentView()
{
   private object _bindingContext;
   private object _content;

   public AnyContentView()
   {
      // Can't await, so hacked call using async void
      Content = CreateContent().FireAndForget();
   }

   public object Content 
   { 
      get => _content;
      set 
      {
         _content = value;
         
         // Can't await a property, so calling an extension 
         //    to cheat around it.
         ContentChangedTask.FireAndForget();
      }
   }
   public object BindingContext 
   {
      set 
      {
         _bindingContext  = value;
         
         // Same problem as with Content.      
         BindingContextChangedTask.FireAndForget();
      }
   
   private async Task<View> CreateContent()
   {
      var content = new StackLayout();
      
      // Any async code here
      await Task.Delay(100).WithouChangingContext();
      
      return content;
   }
   
   private Task ContentChangedTask()
   {
   }
   
   private Task BindingContextChangedTask()
   {
   }
}

public static class Utils
{
   private static async void FireAndForget(this Task task)
   {
      try
      {
         await task().WithouChangingContext();
      }
      catch (Exception ex)
      {
      }
   }
}
</pre>
These are set like so:
<pre lang='cs'>
public ContentPage AnyPage
{
   public AnyPage()
   {
      var contentView = AnyContentView();
      
      // Not obviously wrong, but the constructor at 
      //    AnyContentView illegally creates a false TPL root.
      //    Everything created there will be 'out of time'.
      Content = contentView;
      
      // Not obviously wrong, but the content view needs to run 
      //    the binding context change async to handle various tasks,
      //    so this hides another false root and another bad process.
      contentView.BindingContext = BindingContext;
      
      // Any tasks run from here will add further to the damage, 
      //    since it is identical to AnyContentView.
   }
}
</pre>

### The New Way: Responsive Tasks

#### Key concepts:

* Don't run a task from an illegal *async void* root, including a constructor or a property setter.
* Prefer ContentViews to Views.  Setting Content is critical to proper TPL flow.

**Instead:**

* Replace void property setters with Tasks  
* Prefer content setting to construction  
* Prefer binding changes to other odd cases  

These are related topics. We carefully manage key properties such as ContentView/Content *(and the PageView.Content)* as well as the BindingContext:

<pre lang='cs'>
      // *** 
      // For discussion only; verbose elements are missing. 
      // See the demo project for the complete code. 
      // ***

      // At the constructor, subscribe to content and binding context tasks
      public MyContentPage()
      {
         CallPrepare();

         PostBindingTasks.AddIfNotAlreadyThere(this, HandlePostBindingTask);
         PostContentTasks.AddIfNotAlreadyThere(this, HandlePostContentTask);
      }
      
      // Hide misbehaved void setters and overrides
      public new object BindingContext => base.BindingContext;
      public new View Content => base.Content;
      protected sealed override void OnBindingContextChanged()
      {
         base.OnBindingContextChanged();
      }      
      
      // Set the binding context using a Task.
      public async Task SetBindingContextSafely(object context)
      {
         base.BindingContext = context;

         // Raise the current class's post binding context tasks
         await RunPostBindingTasks(this).WithoutChangingContext();

         if (
            RunSubBindingContextTasksAfterAssignment &&
            BindingContext.IsNotNullOrDefault() &&
            BindingContext is IProvidePostBindingTasks bindingContextAsRunningPostBindingTasks
         )
         {
            // Raise the *view model's* post binding context tasks
            await bindingContextAsRunningPostBindingTasks.RunPostBindingTasks(this).WithoutChangingContext();
         }

         await ConsiderSettingContentBindingContext();
      }

      // Allow derivers to override the binding context Task handler.
      // Alternately, they can subscribe to the PostBindingTasks directly.
      protected virtual Task HandlePostBindingTask(IResponsiveTaskParams paramdict)
      {
         return Task.CompletedTask;
      }
      
      // ***
      // Similar code for setting Content
      // ***

</pre>

#### Consume these new tasks with other tasks to maintain the TPl chain of control

This linkage tends to run through many layers in the UI.  Eventually it reaches the App level, the *headwaters* of the TPL system, where we finally must utilize a void TPL root:

<pre lang='cs'>
    // Seal the App to prevent derivation oddities with TPL
    public sealed class App : Application
    {
      public App()
      {
         // This is the only page; all stages are views within this page.
         var masterPage = new MyContentPage();

         // IMPORTANT 
         // This is the TPL root.
         //    The app is not inheritable, so does not have side-effects.
         //    The startup process and the fade in process also do not compete with each other.
         MainThread.BeginInvokeOnMainThread(
            async () =>
            {
               MainPage = masterPage;

               // These are ResponsiveTasks methods that safely wait for completion of direct --- 
               //    and branching -- tasks before proceeding.
               await ResponsiveTaskHelper.AwaitClassPostContent(masterPage).WithouChangingContext();
               await ResponsiveTaskHelper.AwaitClassAndViewModelPostBinding(masterPage, viewModel).WithouChangingContext();
            });
      }
    }

</pre>
