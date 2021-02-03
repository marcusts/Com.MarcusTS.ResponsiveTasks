// *********************************************************************************
// <copyright file=DialogFactory.cs company="Marcus Technical Services, Inc.">
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
   using Acr.UserDialogs;
   using Com.MarcusTS.SharedUtils.Utils;
   using Xamarin.Forms;

   public static class DialogFactory
   {
      private const int TOAST_DISSOLVE_SECONDS = 7;
      private const int WAIT_FOREVER           = 1000000000;

      private static readonly Color DECISION_TOAST_BACKGROUND_COLOR = Color.Black;

      private static readonly Color ERROR_TOAST_BACKGROUND_COLOR = Color.Black;

      // All backgrounds are now black
      private static readonly Color INFO_TOAST_BACKGROUND_COLOR = Color.Black;

      /// <summary>
      ///    An important toast with an action that takes the user somewhere in response.
      /// </summary>
      /// <param name="toastStr">The toast message.</param>
      /// <param name="useTimeout">
      ///    Whether to remove the toast after a timeout.  Not normal for this scenario: defaults to
      ///    *false*.
      /// </param>
      /// <param name="action">The action to take once the user taps the toast.  *Required*.</param>
      public static void ShowDecisionToast
      (
         string toastStr,
         Action action,
         bool   useTimeout = false
      )
      {
         ShowToastInternal(toastStr, useTimeout: useTimeout, backgroundColor: DECISION_TOAST_BACKGROUND_COLOR,
                           action: action);
      }

      /// <summary>
      ///    An error toast to notify the user that something went wrong.
      /// </summary>
      /// <param name="toastStr">The toast message.</param>
      /// <param name="actionText">The button text</param>
      /// <param name="useTimeout">
      ///    Whether to remove the toast after a timeout.  Not normal for this scenario: defaults to
      ///    *false*.
      /// </param>
      /// <param name="action">The action to take once the user taps the toast.  Optional.</param>
      public static void ShowErrorToast
      (
         string toastStr,
         string actionText = "OK",
         bool   useTimeout = false,
         Action action     = null
      )
      {
         ShowToastInternal(toastStr, actionText, useTimeout: useTimeout, backgroundColor: ERROR_TOAST_BACKGROUND_COLOR,
                           action: action);
      }

      /// <summary>
      ///    An error toast to notify the user that an error has occurred.
      /// </summary>
      /// <param name="toastPrefix">
      ///    The first part of the toast message.
      ///    The second part will be added by this method (see below).
      /// </param>
      /// <param name="ex">The exception.</param>
      /// <param name="useTimeout">
      ///    Whether to remove the toast after a timeout.  Not normal for this scenario: defaults to
      ///    *false*.
      /// </param>
      /// <param name="action">The action to take once the user taps the toast.  Optional.</param>
      public static void ShowExceptionToast
      (
         string    toastPrefix,
         Exception ex,
         bool      useTimeout = false,
         Action    action     = null
      )
      {
         // Add the error details to the toast prefix.
         var finalToastStr = toastPrefix + " (Error details: " + ex.Message + ").";

         ShowErrorToast(finalToastStr, useTimeout: useTimeout, action: action);
      }

      /// <summary>
      ///    A passive toast in a benign color to indicate information only;
      ///    does not normally trigger an action, but can.
      /// </summary>
      /// <param name="toastStr">The toast message.</param>
      /// <param name="actionText"></param>
      /// <param name="useTimeout">Whether to remove the toast after a timeout.  Defaults to *true*.</param>
      /// <param name="action">The action to take if the user taps the toast.</param>
      public static void ShowInfoToast
      (
         string toastStr,
         string actionText = "OK",
         bool   useTimeout = true,
         Action action     = null
      )
      {
         ShowToastInternal(toastStr, actionText, useTimeout: useTimeout, backgroundColor: INFO_TOAST_BACKGROUND_COLOR,
                           action: action);
      }

      /// <summary>
      ///    Can call directly.
      /// </summary>
      /// <param name="toastStr"></param>
      /// <param name="actionText"></param>
      /// <param name="backgroundColor"></param>
      /// <param name="messageTextColor"></param>
      /// <param name="actionTextColor"></param>
      /// <param name="useTimeout"></param>
      /// <param name="toastDissolveSeconds"></param>
      /// <param name="action"></param>
      public static void ShowToastInternal
      (
         string toastStr,
         string actionText           = "OK",
         Color  backgroundColor      = default,
         Color  messageTextColor     = default,
         Color  actionTextColor      = default,
         bool   useTimeout           = true,
         int    toastDissolveSeconds = TOAST_DISSOLVE_SECONDS,
         Action action               = null
      )
      {
         if (toastStr.IsEmpty())
         {
            return;
         }

         var newConfig =
            new ToastConfig(toastStr).SetMessageTextColor
            (
               messageTextColor.IsAnEqualObjectTo(default)
                  ? Color.White
                  : messageTextColor
            );

         if (!backgroundColor.IsAnEqualObjectTo(default))
         {
            newConfig.SetBackgroundColor(backgroundColor);
         }

         newConfig.SetDuration(
            TimeSpan.FromSeconds(useTimeout && toastDissolveSeconds > 0 ? toastDissolveSeconds : WAIT_FOREVER));

         var newAction = new ToastAction();

         // Must add "SetAction"
         if (action != null || actionText.IsNotEmpty())
         {
            if (action != null)
            {
               newAction.SetAction(action);
            }

            if (actionText.IsNotEmpty())
            {
               newAction.SetText(actionText);
               newAction.SetTextColor(actionTextColor.IsAnEqualObjectTo(default(Color)) ? Color.White : actionTextColor);
            }

            newConfig.SetAction(newAction);
         }
         // ELSE skip "SetAction"

         UserDialogs.Instance.Toast(newConfig);
      }
   }
}