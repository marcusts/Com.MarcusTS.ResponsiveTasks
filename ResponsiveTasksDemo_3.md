## Part 3 of N: Technical Guide
### Creating Responsive Tasks with Parameters
The examples provided here are quite basic.  Let's delve into the full capabilities of ResponsiveTasks by looking at how the can be created and consumed.

**Remember:** When creating a ResponsiveTask, te parameter count ***must*** be known and ***obeyed*** when raising that task.

This is the *default* ResponsiveTask declaration:
``` C#
public IResponsiveTask BasicRespTask { get; set; } = new ResponsiveTask(1);
```
It has ***one*** parameter. So when raised:
``` C#
public async Task RaiseBasicRespTask(object someParam)
{
   await BasicRespTask.AwaitAllTasksUsingDefaults(new[] { someParam });
}
```
The parameters to the task were never declared, so they were autmatically cerated using integers. On consumption, one ***must*** refer to the partameters exactly like this:
``` C#
public async Task HandleBasicRespTask(object[] paramDict)
{
   //  You don't know or care what type the variable is.
   var retrievedParam = paramDict[0];
}
```
For better type safety, use this approach:
``` C#
public async Task HandleBasicRespTask(object[] paramDict)
{
   // If the paremeter is not an object, this raises an error, 
   //    though the error handling defaults to
   //    You will not receive a parameter unless the type matches.
   var retrievedParam = paramDict.GetTypeSafeValue<object>(0);
}
```
Another option is to declare the task with *named* params:
``` C#
public IResponsiveTask NamedRespTask { get; set; } = new ResponsiveTask("param1", "param2");
```
This doesn't anything much about the broadcast:
``` C#
public async Task RaiseBasicRespTask(int someParam1, string someParam2)
{
   // With params of varying types, the compiler will ask they be boxed as an object array:
   await BasicRespTask.AwaitAllTasksUsingDefaults(new object[] { someParam1, someParam2 });
}
```
But on cnsumption, you ***must*** request the params by name or you wil receive an error and no params in return:
``` C#
public async Task HandleNamedRespTask(object[] paramDict)
{
   // Either:
   var retrievedParam1 = paramDict["param1"];
   var retrievedParam2 = paramDict["param2"];
   // Or:
   var retrievedParam1 = paramDict.GetTypeSafeValue<int>("param1");
   var retrievedParam2 = paramDict.GetTypeSafeValue<string>("param2");
}
```
### Raising ResponsiveTasks for Different Effects
*{ To be written }*

### How XAML Affects Xamarin and Responsive Tasks
*{ To be written }*

### The Cancellation Dilemma
*{ To be written }*


