# Fixing TPL with Responsive Tasks

See the [Responsive Tasks Demo](https://github.com/marcusts/Com.MarcusTS.ResponsiveTasksDemo/blob/main/README.md) for a complete example of how to use this library along with a discussion of all of the issues addressed.  That project includes unit tests as well.

## Quick Start 





## Responsive Tasks Features


#### On Creating Tasks
* Can assign any parameter count; on firing the task, the provided parameters must match that count or an error will result.
* Can run the tasks in parallel or consecutively *(default)*
* Can respond to errors as the tasks run or not *(default)*
* Can set the error level *(debug output vs. modal dialog, etc.)*
* Can provide a custom error handler

 #### On Handling Tasks
 * Can get the parameters with type safety *(recommended)*
 * Can get parameters directly through array referencing *(unsafe)*
 * Upon request to handle a hosted task, if that subscription already exists, it is ignored - NO duplicate subscriptions as with events!
 * Very well-behaved storage model; subscribed tasks do not mis-behave like subscribed events do on disposal of the listening class.
