# Turtle

## What does it do?
Turtle saves you from writing your logic to retry an action when it fails. It allows you to fine tune the behavior.

## Why Turtle?
A turtle might be slow, but it is determined to get where it wants to be!

## Examples
```C#
Retry.This(() =>
{
    Console.WriteLine("Hello");
    Throws();
})
.MaximumNumberOfTries(5)
.Run();
```
This will repeat the Action passed in This() every 100ms for a maximum of 5 tries as long as the action throws an exception.


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
