# Turtle

## What does it do?
Turtle saves you from writing your logic to retry an action when it fails. It allows you to fine tune the behavior.

## Why Turtle?
A turtle might be slow, but it is determined to get where it wants to be!

## Examples
```C#
var result = Retry.This(() =>
{
    Console.WriteLine("Hello");
    Throws();
})
.MaximumNumberOfTries(5)
.Run();
```
This will repeat the Action passed in This() every 100ms for a maximum of 5 tries as long as the action throws an exception.
`result` contains the CompletionState. The CompletionState is an enum with the following values: Failed, Aborted, Success.

```C#
Retry.This(() =>
{
    Console.WriteLine("Hello");
    return false;
})
.MaximumNumberOfTries(5)
.Run();
```
This will repeat the Func<bool> pass in This() every 100ms for a maximum of tries as long as the Func return false.


```C#
Retry.This(() => Console.WriteLine("Hello"),
        () => true)
    .Run();
This uses a predicate to determine if the try was successful.


```C#
Retry.This(() =>
{
    Console.WriteLine("Hello");
    return false;
})
.Using(new LimitedExponentialBackoffRetryStrategy
{
    BaseDelay = TimeSpan.FromMilliseconds(50),
    MaxDelay = TimeSpan.FromSeconds(5)
})
.MaximumNumberOfTries(5)
.Run();
```
This example uses a limited exponential backoff strategy. The time wait between tries increases exponentially.

Different RetryStrategies can be easily created.


```C#  
var tokenSource = new CancellationTokenSource();
var t = Retry.This(() =>
{
    Console.WriteLine("async");
    Throws();
}).RunAsync(tokenSource.Token);
```
It works with Tasks as well.


## Control behavior based on the exception
It is possible to define how Turtle.Retry will behave in case of an exception. You can do this by passing an
implementation of IExceptionBehavior like this:
```C#
Retry.This(() => action)
.OnException(new MyExceptionBehavior()
.Run();
```
This interface is fairly simple. Just implement the OnException method,
and return the appropriate AfterExceptionBehavior. AfterExceptionBehavior is an enum with the following values:
Retry, Rethrow, Abort. Retry will result in another try if MaximumNumberOfTries has not been reached yet,
Abort will stop the Retry process and return CompletionState.Aborted. Retrow instructs Turtle.Retry to rethrow
the exception and therefore stop the Retry process.