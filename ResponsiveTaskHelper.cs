
// #define DEFEAT_TASK_WAITER
// #define DEFEAT_CANCEL_TOKEN

// *********************************************************************************
// Copyright @2021 Marcus Technical Services, Inc.
// <copyright
// file=ResponsiveTaskHelper.cs
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
   using System.Threading;
   using System.Threading.Tasks;
   using SharedUtils.Utils;
   using Xamarin.Forms;

   /// <summary>
   ///    Class TaskHelper.
   /// </summary>
   public static class ResponsiveTaskHelper
   {
      
      private const int DEFAULT_MAX_DELAY = 10000;
      
#if !DEFEAT_TASK_WAITER
      private const int MILLISECONDS_BETWEEN_DELAYS = 25;
#endif

      public static async Task AwaitClassAndViewModelPostBinding(ICanSetBindingContextSafely view, object context,
         int maxDelay = DEFAULT_MAX_DELAY)
      {
         ErrorUtils.IssueArgumentErrorIfFalse(view.IsNotNullOrDefault(), nameof(view) + " required");
         ErrorUtils.IssueArgumentErrorIfFalse(view is BindableObject newClassAsaBindableObject,
            nameof(view) + " must be a bindable object");
         ErrorUtils.IssueArgumentErrorIfFalse(context.IsNotNullOrDefault(), nameof(context) + " required");

#if !DEFEAT_CANCEL_TOKEN         
         var cancellationTokenSource = CreateCancellationTokenSource(maxDelay);
#endif         

         // ReSharper disable once PossibleNullReferenceException
         await view.SetBindingContextSafely(context).WithoutChangingContext();

#if !DEFEAT_TASK_WAITER
         while
         (
               
#if !DEFEAT_CANCEL_TOKEN
            !cancellationTokenSource.Token.IsCancellationRequested
            &&
            (
#endif
               view.IsPostBindingCompleted.IsFalse()
               ||
               (
                  view.RunSubBindingContextTasksAfterAssignment
                  &&
                  context is IProvidePostBindingTasks contextAsPostBindingTasksProvider
                  &&
                  contextAsPostBindingTasksProvider.IsPostBindingCompleted.IsFalse()
               )
            )
#if !DEFEAT_CANCEL_TOKEN
         )
#endif
         {

#if DEFEAT_CANCEL_TOKEN
            await Task.Delay(MILLISECONDS_BETWEEN_DELAYS).WithoutChangingContext();
#else
            await Task.Delay(MILLISECONDS_BETWEEN_DELAYS, cancellationTokenSource.Token).WithoutChangingContext();
#endif

         }
#endif
      }

         public static async Task AwaitClassPostBinding(IProvidePostBindingTasks newClass,
         int                                                                  maxDelay = DEFAULT_MAX_DELAY)
      {
         ErrorUtils.IssueArgumentErrorIfFalse(newClass.IsNotNullOrDefault(), "New class required");

         var cancellationTokenSource = CreateCancellationTokenSource(maxDelay);

         // ReSharper disable once PossibleNullReferenceException
         await newClass.RunPostBindingTasks(newClass).WithoutChangingContext();

#if !DEFEAT_TASK_WAITER
         // ReSharper disable once PossibleNullReferenceException
         while (!cancellationTokenSource.Token.IsCancellationRequested && newClass.IsPostBindingCompleted.IsFalse())
         {
            await Task.Delay(MILLISECONDS_BETWEEN_DELAYS, cancellationTokenSource.Token).WithoutChangingContext();
         }
#endif
      }

      public static async Task AwaitClassPostContent
         (
            ICanSetContentSafely contentView, 
            View content = default,
            int  maxDelay = DEFAULT_MAX_DELAY
         )
      {
         ErrorUtils.IssueArgumentErrorIfFalse(contentView.IsNotNullOrDefault(), nameof(contentView) + " required");
         
         // Content can be default; if so, the class must produce its own content

         var cancellationTokenSource = CreateCancellationTokenSource(maxDelay);

         // ReSharper disable once PossibleNullReferenceException
         await contentView.SetContentSafely(content).WithoutChangingContext();

#if !DEFEAT_TASK_WAITER
         // ReSharper disable once PossibleNullReferenceException
         while (!cancellationTokenSource.Token.IsCancellationRequested && contentView.IsPostContentCompleted.IsFalse())
         {
            await Task.Delay(MILLISECONDS_BETWEEN_DELAYS, cancellationTokenSource.Token).WithoutChangingContext();
         }
#endif
      }

      private static CancellationTokenSource CreateCancellationTokenSource(int maxDelay)
      {
         var cancellationTokenSource = new CancellationTokenSource();
         cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(maxDelay));
         return cancellationTokenSource;
      }
   }
}