# Grpc Rich Error Model Prototype

This is a protoype NuGet package providing client and server side support for the
[gRPC rich error model](https://grpc.io/docs/guides/error/#richer-error-model).

It had dependencies NuGet packages on:
* `Google.Api.CommonProtos` - to provide the proto implementations used by the rich error model
* `Grpc.Core.Api` - for API classes such as `RpcExcetion`

The client side borrows from ideas used in [googleapi/gax-dotnet](https://github.com/googleapis/gax-dotnet), specifically [RpcExceptionExtensions.cs](https://github.com/googleapis/gax-dotnet/blob/main/Google.Api.Gax.Grpc/RpcExceptionExtensions.cs)

The server side uses extension methods to facilitate using C#'s Object and Collection initializer syntax. The avoids the needs to a *builder* API to be developed.

## Server Side

The server returns an error by throwing an `RpcException` that contains metadata with key `"grpc-status-details-bin"` and value that is a serialized `Google.Rpc.Status`.

The `Google.Rpc.Status` extension method `ToRpcException` creates the appropriate `RpcException` from the status.

Example:
```C#
throw new Google.Rpc.Status
{
    Code = (int)StatusCode.NotFound,
    Message = "Simple error message",
    Details =
    {
        new ErrorInfo
        {
            Domain = "Rich Error Model Demo",
            Reason = "Simple error requested in the demo"
        },
        new RequestInfo
        {
            RequestId = "EchoRequest",
            ServingData = "Param: " + request.Action.ToString()
        },
        }
}.ToRpcException();
```

## Client Side

There is an extension method to retrieve a `Google.Rpc.Status` from the metadata in an `RpcException`.  There are also extension methods to retieve the details from the `Google.Rpc.Status`.

If the client knows what details to expect for a specific error code then it can use the extension methods the explicitly extract the know type from the status details. 

Example:
```C#
void PrintError(RpcException ex)
{
    // Get the status from the RpcException
    Google.Rpc.Status? rpcStatus = ex.GetRpcStatus(); // Extension method

    if (rpcStatus != null)
    {
        Console.WriteLine($"Google.Rpc Status: Code: {rpcStatus.Code}, Message: {rpcStatus.Message}");

        // Try and get the ErrorInfo from the details
        ErrorInfo? errorInfo = rpcStatus.GetErrorInfo(); // Extension method
        if (errorInfo != null)
        {
            Console.WriteLine($"\tErrorInfo: Reason: {errorInfo.Reason}, Domain: {errorInfo.Domain}");
            foreach (var md in errorInfo.Metadata)
            {
                Console.WriteLine($"\tKey: {md.Key}, Value: {md.Value}");
            }
        }
        // etc ...
    }
}
```

Alternatively the client can walk the list of details contained in the status, processing
each object:

Example:
```C#
void PrintStatusDetails(RpcException ex)
{
    // Get the status from the RpcException
    Google.Rpc.Status? rpcStatus = ex.GetRpcStatus(); // Extension method

    if (rpcStatus != null)
    {
        // Decode each "Any" item from the details in turn
        foreach (Any any in rpcStatus.Details)
        {
            var typeUrl = any.TypeUrl;
            switch (typeUrl)
            {
                case TypeUrls.ErrorInfoTypeUrl:
                    var errorInfo = any.SafeUnpack<ErrorInfo>(); // Extension method
                    if (errorInfo != null)
                    {
                        Console.WriteLine($"ErrorInfo: Reason: {errorInfo.Reason}, Domain: {errorInfo.Domain}");
                        foreach (var md in errorInfo.Metadata)
                        {
                            Console.WriteLine($"\tKey: {md.Key}, Value: {md.Value}");
                        }
                    }
                    break;
                case TypeUrls.BadRequestTypeUrl:
                    // etc ..
                    break;

                // Other cases handled here ...
            }
        }
    }

```

## See also
* [Richer error model](https://grpc.io/docs/guides/error/#richer-error-model)
* [Google.Api.CommonProtos](https://cloud.google.com/dotnet/docs/reference/Google.Api.CommonProtos/latest/Google.Api)
