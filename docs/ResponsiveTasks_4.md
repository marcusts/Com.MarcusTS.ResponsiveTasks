## Part 4 of N: Button Pressed: Closing the Final TPL Gaps
---
Theoretically, **ResponsiveTasks** *"fixes"* the irritating, out-of-order chaos that TP has wrought upon us.  But there are still a few gaps:

#### 1. When the App Starts Up
**Startup** is still itself a *false root*, though we have taken steps to fix it. We sealed the App and then awaited using **ResponsiveTasks** so that all tasks are safely awaited before proceeding.  But what if a background thread gets launched and returns unexpectedly with:

```csharp 
Device.BeginInvokeOnMainThread()
```
?

Our strategy is not bullet-proof.

#### 2. When The User Does Something
We already discussed lack of Task signatures for events and overrides.  But one of these is deadly because it occurs so often: **button pressed**. All the user ever does is it to read data ***except*** when they have to accept an edit or to take an action *(including navigation)*.  All of those are **buttons**.

Here's the old-style way of handling button presses:

```csharp
anyButton.Clicked += async (sender, arg) => await SomeTask();
```

But this is obviously the same as any **async-void** call. It's illegal because it purports to run using await, but the await does ***not*** actually wait under ***any*** circumstances.  So whatever occurs afterwards will be ***out-of-time*** and ***unreliable***.

This is the new-style MVVM way:

```csharp
public class AsyncCommand : ICommand
```

The command purports to make *"everything"* async. Unfortunately, it ***can't***.  Remember: TPL is all about **roots**, so the ***way*** a thing is called is what it is -- ***not*** how it is consumed.

```csharp
anyButton = new Button(Command = new AnyAsyncCommand(async () => SomeTask()));
```

This masks the problem by directly associating the command and the button. But this is what happens inside the actual code (*(Thanks Microsoft !!!)*:

```csharp
// Pseudo code for button up or down
if (command != null)
{
   command.Execute();
}
```

It is also *async-void*, which is a *false root*.

### Welcome To Our Old Friend, the Progress Bar

Progress bars have been around since web pages first appeared.  They show how long a process might take to complete.  But they have an even more important role: ***blocking*** the UI to ***prevent*** user input.

As soon as the user taps any button, the progress bar should switch on.  The bar should include a shield to cover the UI. This is usually quite subtle: a slight loss of opacity without a pale gray hue. There should be no way that the user can do anything after a button tap until the progress bar itself disappears.  This also prevents the dreaded button "double tap" issue.

In the ResponsiveTasksDemo, the *"IsBusy"* progress spinner appears whenever the user selects a tab at the bottom of the screen:

```csharp
public async Task SetSelectionKey(string newState)
{
   if (SelectionKey.IsDifferentThan(newState))
   {
      // Turn on the progress spinner
      _spinnerHost.IsBusyShowing = true;
...
```

The spinner gets turned off when all of the awaits have completed through the **ResponsiveTasks** methods:

```csharp
private async Task ChangeContentView<InterfaceT, ClassT>(object viewModel)
...
   await ResponsiveTaskHelper.AwaitClassAndViewModelPostBinding(
      newViewAsAbleToSetBindingContextSafely, viewModel);

   _spinnerHost.IsBusyShowing = false;
}
```

This is what it looks like from the user's perspective:

![The user starts at the Dashboard.](Dashboard_Tab.jpg)

![The user taps "Account".  The progress spinner appears.  All screen input is blocked by a transparent shield.](Progress_Spinner.jpg)

![The Account screen opens. Once all tasks are safely awaited, the shield disappears and the user can tap again.](Account_Tab.jpg)

Since the spiner is a familiar mechanism, they user never really *"misses"* the input they are prevented from issuing. Even if they try to tap *(and fail)*, they are not surprised.  So this is an elegant solution to a sticky problem.

The same technique applies at app startup *(Problem #1 above)*.  The spinner is switched on at startup and then off again once the entire UI is loaded and all tasks have been safely awaited.

