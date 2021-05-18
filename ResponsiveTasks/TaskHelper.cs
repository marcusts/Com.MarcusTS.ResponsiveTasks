// Copyright (c) 2019  Marcus Technical Services, Inc. <marcus@marcusts.com>
//
// This file, TaskHelper.cs, is a part of a program called AccountViewMobile.
//
// AccountViewMobile is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Permission to use, copy, modify, and/or distribute this software
// for any purpose with or without fee is hereby granted, provided
// that the above copyright notice and this permission notice appear
// in all copies.
//
// AccountViewMobile is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// For the complete GNU General Public License,
// see <http://www.gnu.org/licenses/>.

namespace Com.MarcusTS.ResponsiveTasks
{
   using System;
   using System.Threading;
   using System.Threading.Tasks;
   using SharedUtils.Utils;
   using Xamarin.Forms;

   /// <summary>
   /// Class TaskHelper.
   /// </summary>
   public static class TaskHelper
   {
      private const int MILLISECONDS_BETWEEN_DELAYS = 25;
      
      /// <summary>
      /// Redundant code; difficult to eliminate.  See <see cref="AwaitClassPostBinding"/>.
      /// </summary>
      public static async Task AwaitClassPostConstruction(Func<IProvidePostConstructionTasks> newClassCreator, int maxDelay)
      {
         ErrorUtils.IssueArgumentErrorIfTrue(newClassCreator.IsNullOrDefault(), "New class creator required" );
         
         var cancellationTokenSource = CreateCancellationTokenSource(maxDelay);
         // ReSharper disable once PossibleNullReferenceException
         var newClass = newClassCreator.Invoke();

         while (!cancellationTokenSource.Token.IsCancellationRequested && !newClass.IsPostConstructionCompleted.IsTrue())
         {
            await Task.Delay(MILLISECONDS_BETWEEN_DELAYS, cancellationTokenSource.Token).WithoutChangingContext();
         }
      }

      /// <summary>
      /// Redundant code; difficult to eliminate.  See <see cref="AwaitClassPostConstruction"/>.
      /// </summary>
      public static async Task AwaitClassPostBinding(ICanSetBindingContextSafely newClass, object context, int maxDelay)
      {
         ErrorUtils.IssueArgumentErrorIfTrue(newClass.IsNullOrDefault(), "New class creator required");
         
         var cancellationTokenSource = CreateCancellationTokenSource(maxDelay);

         // ReSharper disable once PossibleNullReferenceException
         await newClass.SetBindingContextSafely(context).WithoutChangingContext();

         while (!cancellationTokenSource.Token.IsCancellationRequested && !newClass.IsPostBindingCompleted.IsTrue())
         {
            // Standard delay
            await Task.Delay(MILLISECONDS_BETWEEN_DELAYS, cancellationTokenSource.Token).WithoutChangingContext();
         }
      }
      
      private static CancellationTokenSource CreateCancellationTokenSource(int maxDelay)
      {
         var cancellationTokenSource = new CancellationTokenSource();
         cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(maxDelay));
         return cancellationTokenSource;
      }
   }
}
