// *********************************************************************************
// Copyright @2021 Marcus Technical Services, Inc.
// <copyright
// file=ResponsiveTasks.cs
// company="Marcus Technical Services, Inc.">
// </copyright>
// 
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// *********************************************************************************

namespace Com.MarcusTS.ResponsiveTasks
{
   using System;
   using System.Collections.Concurrent;
   using System.Collections.Generic;
   using System.Diagnostics;
   using System.Linq;
   using System.Runtime.CompilerServices;
   using System.Threading.Tasks;
   using Com.MarcusTS.SharedUtils.Interfaces;
   using Com.MarcusTS.SharedUtils.Utils;

   /// <summary>
   /// NOTE: Nullable parameters are stored without their nullable status. This appears to be a limitation of boxing
   /// certain objects.
   /// </summary>
   public enum HowToRun
   {
      // Default
      AwaitAllConsecutively_IgnoreFailures,

      AwaitAllConsecutively_StopOnFirstFailure,
      AwaitAllCollectively,
      RunAllInParallelFromVoid,
      AwaitAllSafelyFromVoid,
      NotSet,
   }

   public enum ParamsErrorLevels
   {
      None,
      DebugWriteLine,

      //Dialog,
      //Toast,
      Throw,

      Custom,
   }

   public interface ICustomResponsiveParameterErrorHandler
   {
      Task HandleErrorMessage( ParamsErrorLevels paramsErrorLevel, string errorStr );
   }

   public interface IHostTask
   {
      object                          Host      { get; set; }
      ResponsiveTaskBroadcastDelegate TaskToRun { get; set; }
   }

   public interface IIssueResponsiveErrors
   {
      ParamsErrorLevels ParamsErrorLevel { get; set; }

      void IssueResponsiveError( ParamsErrorLevels paramsErrorLevel, string errorStr );
   }

   public interface IResponsiveTaskParams : IDictionary<object, object>
   {
      T GetTypeSafeValue<T>( object paramKey );
   }

   public interface IResponsiveTasks : IIssueResponsiveErrors, ICanRun
   {
      ICustomResponsiveParameterErrorHandler CustomErrorHandler { get; set; }

      ResponsiveTaskParams Params { get; set; }

      HowToRun RunHow { get; set; }

      IIssueResponsiveErrors TaskErrorBroadcaster { get; set; }

      /// <remarks>
      /// Does not work with <see cref="RunAllTasksInParallelFromVoid" />, as we cannot predict or control when those
      /// tasks are completed.
      /// </remarks>
      int TimeoutMilliseconds { get; set; }

      void AddIfNotAlreadyThere( object host, ResponsiveTaskBroadcastDelegate task );

      Task<bool> AwaitAllTasksCollectively( params object[] paramValues );

      Task<bool> AwaitAllTasksConsecutively( object[] paramValues, bool stopOnFirstFailure = false );

      bool AwaitAllTasksSafelyFromVoid( params object[] paramValues );

      void RemoveAllSubscribers();

      void RemoveIfThere( object host, ResponsiveTaskBroadcastDelegate task );

      void RunAllTasksInParallelFromVoid( params object[] paramValues );

      Task<bool> RunAllTasksUsingDefaults( params object[] paramValues );

      void UnsubscribeHost<T>( T host );
   }

   public delegate Task ResponsiveTaskBroadcastDelegate( IResponsiveTaskParams paramDict );

   public delegate Task<bool> ResponsiveTasksBoolDelegate( params object[] paramValues );

   public class HostTask : IHostTask
   {
      public HostTask( object host, ResponsiveTaskBroadcastDelegate task )
      {
         Host      = host;
         TaskToRun = task;
      }

      public object                          Host      { get; set; }
      public ResponsiveTaskBroadcastDelegate TaskToRun { get; set; }
   }

   public class ResponsiveTaskParams : ConcurrentDictionary<object, object>, IResponsiveTaskParams
   {
      private readonly IIssueResponsiveErrors _paramErrorBroadcaster;

      public ResponsiveTaskParams( IIssueResponsiveErrors paramErrorBroadcaster )
      {
         _paramErrorBroadcaster = paramErrorBroadcaster;
      }

      public T GetTypeSafeValue<T>( object paramKey )
      {
         if ( ContainsKey( paramKey ) )
         {
            var obj = this[ paramKey ];

            if ( obj is T t )
            {
               return t;
            }
         }

         // ELSE failed
         var errorStr =
            nameof( ResponsiveTaskParams )          + ": " + nameof( GetTypeSafeValue ) +
            ": could not convert keyed parameter '" +
            paramKey                                + "' to the type '" + typeof( T ) + "'.";

         if ( _paramErrorBroadcaster.IsNotNullOrDefault() )
         {
            _paramErrorBroadcaster.IssueResponsiveError( _paramErrorBroadcaster.ParamsErrorLevel, errorStr );
         }

         return default;
      }
   }

   // Replaces multi-cast events (or even weak events) by providing an awaitable set of tasks instead.
   public class ResponsiveTasks : List<IHostTask>, IResponsiveTasks
   {
      public const ParamsErrorLevels DEFAULT_PARAMS_ERROR_LEVEL = ParamsErrorLevels.DebugWriteLine;

      public ResponsiveTasks()
      {
         TaskErrorBroadcaster = this;
         Params               = new ResponsiveTaskParams( this );
      }

      public ResponsiveTasks( params object[] paramKeys )
         : this()
      {
         Params.Clear();

         if ( paramKeys.IsAnEmptyList() )
         {
            return;
         }

         // ELSE
         foreach ( var key in paramKeys )
         {
            Params.AddOrUpdate( key, default );
         }
      }

      public ResponsiveTasks( int paramCount = 0 )
         : this()
      {
         Params.Clear();

         if ( paramCount == 0 )
         {
            return;
         }

         // ELSE
         for ( var idx = 0; idx < paramCount; idx++ )
         {
            Params.AddOrUpdate( idx, default );
         }
      }

      public ICustomResponsiveParameterErrorHandler CustomErrorHandler   { get; set; }
      public IThreadSafeAccessor                    IsRunning            { get; } = new ThreadSafeAccessor();
      public ResponsiveTaskParams                   Params               { get; set; }
      public ParamsErrorLevels                      ParamsErrorLevel     { get; set; } = DEFAULT_PARAMS_ERROR_LEVEL;
      public HowToRun                               RunHow               { get; set; }
      public IIssueResponsiveErrors                 TaskErrorBroadcaster { get; set; }

      /// <remarks>Does not work with RunAllInParallelFromVoid, as we cannot predict or control when those tasks are completed.</remarks>
      public int TimeoutMilliseconds { get; set; }

      public void AddIfNotAlreadyThere( object host, ResponsiveTaskBroadcastDelegate taskDelegate )
      {
         var foundValue = GetExistingTask( host, taskDelegate );
         if ( foundValue.IsNullOrDefault() )
         {
            Add( new HostTask( host, taskDelegate ) );
         }
      }

      public async Task<bool> AwaitAllTasksCollectively( params object[] paramValues )
      {
         if ( IsAlreadyRunningOrEmpty() )
         {
            return true;
         }

         // ELSE
         IsRunning.SetTrue();
         AssignParamValues( paramValues );
         var timedOut = new ThreadSafeAccessor( 0 );

         try
         {
            var taskToAwait = Task.WhenAll( this.Select( ht => ht.TaskToRun( Params ) ) );

            if ( TimeoutMilliseconds > 0 )
            {
               await Task.WhenAny( TimeOutTask( TimeoutMilliseconds ), taskToAwait ).WithoutChangingContext();
            }
            else
            {
               await taskToAwait.WithoutChangingContext();
            }

            if ( timedOut.IsTrue() )
            {
               HandleTimeoutError();
               return false;
            }

            // ELSE
            return true;
         }
         catch ( Exception ex )
         {
            HandleTaskError( ex );
            return false;
         }
         finally
         {
            IsRunning.SetFalse();
         }

         // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
         // P R I V A T E   M E T H O D S
         async Task TimeOutTask( int timeoutMilliseconds )
         {
            await Task.Delay( timeoutMilliseconds ).WithoutChangingContext();
            timedOut.SetTrue();
         }

         // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
      }

      public async Task<bool> AwaitAllTasksConsecutively( object[] paramValues, bool stopOnFirstFailure = false )
      {
         if ( IsAlreadyRunningOrEmpty() )
         {
            return true;
         }

         IsRunning.SetTrue();
         AssignParamValues( paramValues );
         var timedOut = new ThreadSafeAccessor( 0 );

         var useTimeout            = TimeoutMilliseconds > 0;
         var remainingMilliseconds = TimeoutMilliseconds;

         var stopWatch = new Stopwatch();
         stopWatch.Start();

         try
         {
            foreach ( var task in this )
            {
               try
               {
                  if ( useTimeout )
                  {
                     await Task.WhenAny( TimeOutTask( remainingMilliseconds ),
                        task.TaskToRun.Invoke( Params ) ).WithoutChangingContext();

                     // ELSE decrement the remaining milliseconds
                     remainingMilliseconds -= (int)stopWatch.ElapsedMilliseconds;

                     if ( timedOut.IsTrue() || ( remainingMilliseconds <= 0 ) )
                     {
                        HandleTimeoutError();
                        return false;
                     }
                  }
                  else
                  {
                     await task.TaskToRun.Invoke( Params ).WithoutChangingContext();
                  }
               }
               catch ( Exception ex )
               {
                  HandleTaskError( ex );

                  if ( stopOnFirstFailure )
                  {
                     return false;
                  }
               }
            }
         }
         finally
         {
            IsRunning.SetFalse();
            stopWatch.Stop();
         }

         // ELSE
         return true;

         // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
         // P R I V A T E M E T H O D S
         async Task TimeOutTask( int milliseconds )
         {
            await Task.Delay( milliseconds ).WithoutChangingContext();
            timedOut.SetTrue();
         }

         // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
      }

      public bool AwaitAllTasksSafelyFromVoid( params object[] paramValues )
      {
         if ( IsAlreadyRunningOrEmpty() )
         {
            return true;
         }

         // ELSE
         IsRunning.SetTrue();
         AssignParamValues( paramValues );
         var timedOut = new ThreadSafeAccessor( 0 );

         try
         {
            var taskToAwait = Task.WhenAll( this.Select( ht => ht.TaskToRun( Params ) ) );

            if ( TimeoutMilliseconds > 0 )
            {
               Task.WhenAny( TimeOutTask( TimeoutMilliseconds ), taskToAwait ).WaitFromVoid();
            }
            else
            {
               taskToAwait.WaitFromVoid();
            }

            if ( timedOut.IsTrue() )
            {
               HandleTimeoutError();
               return false;
            }

            // ELSE
            return true;
         }
         catch ( Exception ex )
         {
            HandleTaskError( ex );
            return false;
         }
         finally
         {
            IsRunning.SetFalse();
         }

         // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
         // P R I V A T E   M E T H O D S
         async Task TimeOutTask( int timeoutMilliseconds )
         {
            await Task.Delay( timeoutMilliseconds ).WithoutChangingContext();
            timedOut.SetTrue();
         }

         // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
      }

      public virtual void IssueResponsiveError( ParamsErrorLevels paramsErrorLevel, string errorStr )
      {
         switch ( paramsErrorLevel )
         {
            //case ParamsErrorLevels.Dialog:
            //   DialogFactory.ShowErrorToast(errorStr);
            //   break;

            //case ParamsErrorLevels.Toast:
            //   DialogFactory.ShowErrorToast(errorStr, useTimeout: true);
            //   break;

            case ParamsErrorLevels.DebugWriteLine:
               Debug.WriteLine( errorStr );
               break;

            case ParamsErrorLevels.Custom:
               CustomErrorHandler?.HandleErrorMessage( paramsErrorLevel, errorStr );
               break;

            case ParamsErrorLevels.Throw:
               ErrorUtils.ThrowArgumentError( errorStr );
               break;
         }
      }

      public void RemoveAllSubscribers()
      {
         Clear();
      }

      public void RemoveIfThere( object host, ResponsiveTaskBroadcastDelegate task )
      {
         var foundValue = GetExistingTask( host, task );
         if ( foundValue.IsNotNullOrDefault() )
         {
            Remove( foundValue );
         }
      }

      public void RunAllTasksInParallelFromVoid( params object[] paramValues )
      {
         if ( IsAlreadyRunningOrEmpty() )
         {
            return;
         }

         IsRunning.SetTrue();
         AssignParamValues( paramValues );

         try
         {
            foreach ( var task in this )
            {
               task.TaskToRun.Invoke( Params ).RunParallel();
            }
         }
         catch ( Exception ex )
         {
            HandleTaskError( ex );
         }
         finally
         {
            IsRunning.SetFalse();
         }
      }

      public Task<bool> RunAllTasksUsingDefaults( params object[] paramValues )
      {
         return RunAllTasksUsingDefaults_Internal( RunHow, paramValues );
      }

      public void UnsubscribeHost<T>( T host )
      {
         if ( this.IsAnEmptyList() )
         {
            return;
         }

         // ELSE

         // Look for any hosts to this task that are of the base type
         var subscriptionsToRemove = this.Where( KeyValuePair => KeyValuePair.Host is T ).ToArray();

         if ( subscriptionsToRemove.IsAnEmptyList() )
         {
            return;
         }

         // ELSE

         foreach ( var subscription in subscriptionsToRemove )
         {
            Remove( subscription );
         }
      }

      protected virtual async Task<bool> RunAllTasksUsingDefaults_Internal(
         HowToRun runHow, params object[] paramValues )
      {
         // False by default
         var result = new ThreadSafeAccessor( 0 );

         switch ( runHow )
         {
            case HowToRun.AwaitAllConsecutively_IgnoreFailures:
               if ( await AwaitAllTasksConsecutively( paramValues ).WithoutChangingContext() )
               {
                  result.SetTrue();
               }

               break;

            case HowToRun.AwaitAllConsecutively_StopOnFirstFailure:
               if ( await AwaitAllTasksConsecutively( paramValues, true ).WithoutChangingContext() )
               {
                  result.SetTrue();
               }

               break;

            case HowToRun.AwaitAllCollectively:
               if ( await AwaitAllTasksCollectively( paramValues ).WithoutChangingContext() )
               {
                  result.SetTrue();
               }

               break;

            case HowToRun.AwaitAllSafelyFromVoid:
               if ( AwaitAllTasksSafelyFromVoid( paramValues ) )
               {
                  result.SetTrue();
               }

               break;

            case HowToRun.RunAllInParallelFromVoid:
               RunAllTasksInParallelFromVoid( paramValues );

               // Can't get a result from parallel void
               result.SetTrue();
               break;
         }

         // ELSE FAIL
         return result.IsTrue();
      }

      private void AssignParamValues( object[] paramValues )
      {
         if ( Params.Count != paramValues?.Length )
         {
            var errorStr =
               nameof( ResponsiveTasks )              +
               this[ 0 ].Host                         + ": "                        +
               this[ 0 ].TaskToRun                    + ": "                        +
               ": "                                   + nameof( AssignParamValues ) +
               ": received "                          +
               paramValues?.Length                    +
               " parameters for broadcast. Expected " + Params.Count + ".";
            TaskErrorBroadcaster?.IssueResponsiveError( ParamsErrorLevel, errorStr );

            return;
         }

         if ( !Params.Any() || paramValues.IsAnEmptyList() )
         {
            // Nothing to do
            return;
         }

         // ELSE success
         var keyIdx = 0;
         foreach ( var key in Params.Keys )
         {
            Params[ key ] = paramValues[ keyIdx ];
            keyIdx++;
         }
      }

      private IHostTask GetExistingTask( object host, ResponsiveTaskBroadcastDelegate taskDelegate )
      {
         var retTask = this.FirstOrDefault( hostTask => hostTask.Host.IsAnEqualReferenceTo( host )
                                                      &&

                                                        // Do *not* use IAnEqualReferenceTo
                                                        hostTask.TaskToRun.Equals( taskDelegate ) );
         return retTask;
      }

      private void HandleTaskError( Exception ex, [CallerMemberName] string memberName = "" )
      {
         var errorStr = memberName + ": ERROR or CANCELLATION on task execution:" + ex.Message + ".";
         TaskErrorBroadcaster?.IssueResponsiveError( ParamsErrorLevel, errorStr );
      }

      private void HandleTimeoutError( [CallerMemberName] string memberName = "" )
      {
         var errorStr = memberName + ": Task timed out. Exceeded ->" + TimeoutMilliseconds + "<- milliseconds.";
         TaskErrorBroadcaster?.IssueResponsiveError( ParamsErrorLevel, errorStr );
      }

      private bool IsAlreadyRunningOrEmpty()
      {
         if ( IsRunning.IsTrue() || this.IsAnEmptyList() )
         {
            return true;
         }

         return false;
      }
   }
}