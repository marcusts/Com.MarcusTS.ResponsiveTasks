
namespace ResponsiveTasks
{
   using System.Collections.Concurrent;
   using System.Collections.Generic;
   using System.Diagnostics;
   using System.Linq;
   using System.Threading.Tasks;
   using Com.MarcusTS.SharedUtils.Utils;

   public enum HowToRun
   {
      AwaitAllSeparately_IgnoreFailures,
      AwaitAllSeparately_StopOnFirstFailure,
      RunWhenAll,
      TaskRunWhenAll,
      TaskRunSeparately_ReturnOnMainThread,
      TaskRunSeparately_IgnoreThread
   }

   public enum ParamsErrorLevels
   {
      None,
      DebugWriteLine,
      Dialog,
      Throw,
      Custom
   }

   // Ensures that any task provided to us contains a single parameter
   //    that we can use to pass any number of parameters when we call the Task
   public delegate Task ResponsiveTaskBroadcastDelegate(IResponsiveTaskParams paramDict);

   public interface ICustomResponsiveParameterErrorHandler
   {
      Task HandleErrorMessage(string mess);
   }

   public interface IHaveResponsiveTasksDict
   {
      IResponsiveTasksDict TasksDict { get; set; }
   }

   public interface IHostTask
   {
      object Host { get; set; }
      ResponsiveTaskBroadcastDelegate TaskToRun { get; set; }
   }

   public interface IIssueResponsiveErrors
   {
      void IssueResponsiveError(string errorStr);
   }

   public interface IResponsiveTaskParams : IDictionary<object, object>
   {
      T GetTypeSafeValue<T>(object paramKey);
   }

   public interface IResponsiveTasks : IIssueResponsiveErrors
   {
      ICustomResponsiveParameterErrorHandler CustomErrorHandler { get; set; }

      ResponsiveTaskParams Params { get; set; }

      ParamsErrorLevels ParamsErrorLevel { get; set; }

      HowToRun RunHow { get; set; }

      void AddIfNotAlreadyThere(object host, ResponsiveTaskBroadcastDelegate task);

      void RemoveIfThere(object host, ResponsiveTaskBroadcastDelegate task);

      Task<bool?> RunTaskAwaitingAllSeparatelyIgnoreFailures(params object[] paramValues);

      Task<bool?> RunTaskAwaitingAllSeparatelyStopOnFirstFailure(params object[] paramValues);

      Task<bool?> RunTaskUsingDefaults(params object[] paramValues);

      Task<bool?> RunTaskWhenAll(params object[] paramValues);

      void RunVoidSeparately(params object[] paramValues);

      void RunVoidUsingDefaults(params object[] paramValues);

      void RunVoidWhenAll(params object[] paramValues);

      void UnsubscribeHost<T>(T host);
   }

   public interface IResponsiveTasksDict : IDictionary<object, IResponsiveTasks>
   {
   }

   public class HostTask : IHostTask
   {
      public HostTask(object host, ResponsiveTaskBroadcastDelegate task)
      {
         Host = host;
         TaskToRun = task;
      }

      public object Host { get; set; }
      public ResponsiveTaskBroadcastDelegate TaskToRun { get; set; }
   }

   public class ResponsiveTaskParams : ConcurrentDictionary<object, object>, IResponsiveTaskParams
   {
      private readonly IIssueResponsiveErrors _errorBroadcaster;

      public ResponsiveTaskParams(IIssueResponsiveErrors errorBroadcaster)
      {
         _errorBroadcaster = errorBroadcaster;
      }

      public T GetTypeSafeValue<T>(object paramKey)
      {
         if (ContainsKey(paramKey))
         {
            var obj = this[paramKey];

            if (obj.GetType() == typeof(T))
            {
               return (T)obj;
            }
         }

         // ELSE failed
         var errorStr =
            nameof(ResponsiveTaskParams) + ": " + nameof(GetTypeSafeValue) + ": could not convert keyed parameter '" + paramKey + "' to the type '" + typeof(T) + "'.";

         _errorBroadcaster?.IssueResponsiveError(errorStr);

         return default;
      }
   }

   // Replaces multi-cast events (or even weak events) by providing an awaitable set of tasks instead.
   public class ResponsiveTasks : List<IHostTask>, IResponsiveTasks
   {
      public ResponsiveTasks()
      {
         Params = new ResponsiveTaskParams(this);
      }

      public ResponsiveTasks(params object[] paramKeys)
         : this()
      {
         if (paramKeys.IsNotAnEmptyList())
         {
            foreach (var key in paramKeys)
            {
               Params[key] = default;
            }
         }
      }

      public ResponsiveTasks(int paramCount = 0)
         : this()
      {
         if (paramCount > 0)
         {
            for (var idx = 0; idx < paramCount; idx++)
            {
               Params[idx] = default;
            }
         }
      }

      public ICustomResponsiveParameterErrorHandler CustomErrorHandler { get; set; }
      public ResponsiveTaskParams Params { get; set; }
      public ParamsErrorLevels ParamsErrorLevel { get; set; }
      public HowToRun RunHow { get; set; }
      private bool AllRanSuccessfully => true; // this.All(t => t.TaskToRun(Params).IsCompletedSuccessfully);

      public void AddIfNotAlreadyThere(object host, ResponsiveTaskBroadcastDelegate taskDelegate)
      {
         var foundValue = GetExistingTask(host, taskDelegate);
         if (foundValue.IsNullOrDefault())
         {
            Add(new HostTask(host, taskDelegate));
         }
      }

      public void IssueResponsiveError(string errorStr)
      {
         switch (ParamsErrorLevel)
         {
            case ParamsErrorLevels.Dialog:
               DialogFactory.ShowErrorToast(errorStr);
               break;

            case ParamsErrorLevels.DebugWriteLine:
               Debug.WriteLine(errorStr);
               break;

            case ParamsErrorLevels.Custom:
               CustomErrorHandler?.HandleErrorMessage(errorStr);
               break;

            case ParamsErrorLevels.Throw:
               ErrorUtils.ThrowArgumentError(errorStr);
               break;
         }
      }

      public void RemoveIfThere(object host, ResponsiveTaskBroadcastDelegate task)
      {
         var foundValue = GetExistingTask(host, task);
         if (foundValue.IsNotNullOrDefault())
         {
            Remove(foundValue);
         }
      }

      public async Task<bool?> RunTaskAwaitingAllSeparately(bool stopOnFirstFailure = false)
      {
         if (!this.Any())
         {
            return true;
         }

         try
         {
            foreach (var task in this)
            {
               await task.TaskToRun.Invoke(Params).WithoutChangingContext();

#if RESPOND_TO_FAILURES
            // TODO
            // Re-entry
            if (!task.TaskToRun(Params).IsCompletedSuccessfully && stopOnFirstFailure)
            {
               return false;
            }
#endif

            }

            return AllRanSuccessfully;
         }
         catch
         {
            return false;
         }
      }

      public Task<bool?> RunTaskAwaitingAllSeparatelyIgnoreFailures(params object[] paramValues)
      {
         AugmentParams(paramValues);
         return RunTaskAwaitingAllSeparately();
      }

      public Task<bool?> RunTaskAwaitingAllSeparatelyStopOnFirstFailure(params object[] paramValues)
      {
         AugmentParams(paramValues);
         return RunTaskAwaitingAllSeparately(true);
      }

      public Task<bool?> RunTaskUsingDefaults(params object[] paramValues)
      {
         AugmentParams(paramValues);

         var result = Extensions.EmptyNullableBool;

         switch (RunHow)
         {
            case HowToRun.AwaitAllSeparately_IgnoreFailures:
               return RunTaskAwaitingAllSeparately();

            case HowToRun.AwaitAllSeparately_StopOnFirstFailure:
               return RunTaskAwaitingAllSeparately(true);

            case HowToRun.RunWhenAll:
               return RunTaskWhenAll();

            case HowToRun.TaskRunSeparately_IgnoreThread:
               RunVoidSeparately(false);
               // Cannot know the result
               break;

            case HowToRun.TaskRunSeparately_ReturnOnMainThread:
               RunVoidSeparately(true);
               // Cannot know the result
               break;

            case HowToRun.TaskRunWhenAll:
               RunVoidWhenAll();
               // Cannot know the result
               break;

            default:
               result = false;
               break;
         }

         // ELSE FAIL
         return Task.FromResult(result);
      }

      public Task<bool?> RunTaskWhenAll(params object[] paramValues)
      {
         AugmentParams(paramValues);
         return RunTaskWhenAll();
      }

      public async Task<bool?> RunTaskWhenAll()
      {
         if (!this.Any())
         {
            return true;
         }

         try
         {
            await Task.WhenAll(this.Select(ht => ht.TaskToRun(Params))).WithoutChangingContext();

            return AllRanSuccessfully;
         }
         catch
         {
            return false;
         }
      }

      public void RunVoidSeparately(params object[] paramValues)
      {
         AugmentParams(paramValues);

         if (!this.Any())
         {
            return;
         }

         try
         {
            foreach (var task in this)
            {
               task.TaskToRun.Invoke(Params).RunParallel();
            }
         }
         catch
         {
            // ignored
         }
      }

      public void RunVoidUsingDefaults(params object[] paramValues)
      {
         AugmentParams(paramValues);
         RunTaskUsingDefaults().RunParallel();
      }

      public void RunVoidWhenAll(params object[] paramValues)
      {
         AugmentParams(paramValues);
         RunTaskWhenAll().RunParallel();
      }

      private void AugmentParams(object[] paramValues)
      {
         if (!Params.Any() || paramValues.IsAnEmptyList())
         {
            // Nothing to do
            return;
         }

         if (Params.Count != paramValues.Length)
         {
            var errorStr =
               nameof(ResponsiveTasks) + ": " + nameof(AugmentParams) + ": received " + paramValues.Length + "parameters for broadcast. Expected " + Params.Count + ".";
            IssueResponsiveError(errorStr);

            return;
         }

         // ELSE success
         for (var idx = 0; idx < Params.Count; idx++)
         {
            Params[idx] = paramValues[idx];
         }
      }

      private IHostTask GetExistingTask(object host, ResponsiveTaskBroadcastDelegate taskDelegate)
      {
         return this.FirstOrDefault(hostTask =>
            hostTask.Host.IsNotAnEqualReferenceTo(host) && hostTask.TaskToRun.IsNotAnEqualReferenceTo(taskDelegate));
      }

      public void UnsubscribeHost<T>(T host)
      {
         if (this.IsAnEmptyList())
         {
            return;
         }

         // Look for any hosts to this task that are of the base type
         var subscriptionsToRemove = this.Where(KeyValuePair => KeyValuePair.Host is T).ToArray();

         if (subscriptionsToRemove.IsAnEmptyList())
         {
            return;
         }

         // ELSE
         foreach (var subscription in subscriptionsToRemove)
         {
            Remove(subscription);
         }
      }
   }

   public class ResponsiveTasksDict : Dictionary<object, IResponsiveTasks>, IResponsiveTasksDict
   {
   }
}
