// *********************************************************************************
// <copyright file=TaskHelper.cs company="Marcus Technical Services, Inc.">
//     Copyright @2019 Marcus Technical Services, Inc.
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

namespace ResponsiveTasks
{
   using System;
   using System.Diagnostics;
   using System.Threading.Tasks;
   using Com.MarcusTS.SharedUtils.Utils;
   using Xamarin.Essentials;
   using Xamarin.Forms;

   /// <summary>
   ///    Class TaskHelper.
   /// </summary>
   public static class TaskHelper
   {
      /// <summary>
      ///    Runs the parallel.
      /// </summary>
      /// <param name="task">The task.</param>
      /// <param name="taskCallback">The task callback.</param>
      /// <param name="actionCallback">The action callback.</param>
      public static void RunParallel
      (
         this Task   task,
         Task   taskCallback   = default,
         Action actionCallback = default
      )
      {
         try
         {
            Task.Run
            (
               async () =>
               {
                  await task.WithoutChangingContext();

                  if (taskCallback.IsNotNullOrDefault())
                  {
                     await MainThread.InvokeOnMainThreadAsync(async () => { await taskCallback.WithoutChangingContext(); });
                  }
                  else if (actionCallback.IsNotNullOrDefault())
                  {
                     await MainThread.InvokeOnMainThreadAsync(() => { actionCallback?.Invoke(); });
                  }
               }
            );
         }
         catch (Exception ex)
         {
            Debug.WriteLine(nameof(RunParallel) + " error ->" + ex.Message + "<-");
         }
      }

      /// <summary>
      /// Runs a void without changing the context (configure await is false).
      /// </summary>
      /// <param name="task">The task.</param>
      /// <returns>Task.</returns>
      public static async Task WithoutChangingContext(this Task task) =>
#if AVOID_CONTEXT_MANAGEMENT
         await task;
#else
         await task.ConfigureAwait(false);

#endif
   }
}