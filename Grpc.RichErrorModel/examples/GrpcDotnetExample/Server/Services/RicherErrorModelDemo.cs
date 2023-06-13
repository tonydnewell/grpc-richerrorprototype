#region Copyright notice and license

// Copyright 2023 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using Grpc.Core;
using Grpc.RichErrorModel;
using Google.Rpc;
using Google.Protobuf.WellKnownTypes;
using REM.Example;

// The RichErrorModelDemo service is to demonstate how a service can return
// errors using the Rich Error Model - see https://grpc.io/docs/guides/error/#richer-error-model
//
// This service allows a client to ask the server to generate an error. The DemoRequest request
// parameter specifies an action: echo (no error), simple error, or complex error.

namespace REMDotnetServer.Services
{
    public class RichErrorModelDemoImpl : RichErrorModelDemo.RichErrorModelDemoBase
    {
        // Server side handler of the EchoRequest RPC
        public override Task<DemoResponse> EchoRequest(DemoRequest request, ServerCallContext context)
        {
            switch (request.Action)
            {
                case REM.Example.Action.SimpleError:
                    // create and throw a simple error
                    throw CreateSimpleError(request);
                case REM.Example.Action.ComplexError:
                    // create and throw a more complex error
                    throw CreateComplexError(request);
                default:
                    // echo the request (no error)
                    return Task.FromResult(new DemoResponse { EchoedMessage = "ECHO " + request.Message });
            }
        }

        /// <summary>
        /// Create an <see cref="RpcException"/> that contains a <see cref="Google.Rpc.Status"/>
        /// with simple error details using the Rich Error Model.
        /// 
        /// This creates the Status with just two "Details" - ErrorInfo and RequestInfo.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Created RpcExeption</returns>
        private static RpcException CreateSimpleError(DemoRequest request)
        {
            // This demonstates how to populate a Google.Rpc.Status for the
            // rich error model using C#'s object initializer syntax.
            //
            // This has been made possible by the extension methods provided
            // in Grpc.RichErrorModel.
            return new Google.Rpc.Status
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
        }

        /// <summary>
        /// Create an <see cref="RpcException"/> that contains a <see cref="Google.Rpc.Status"/>
        /// with more complex error details using the Rich Error Model.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private static RpcException CreateComplexError(DemoRequest request)
        {
            // This demonstates how to populate a Google.Rpc.Status for the
            // rich error model using C#'s object initializer syntax, building
            // from objects already created.
            //
            // This has been made possible by the extension methods provided
            // in Grpc.RichErrorModel.
            ErrorInfo errorInfo = new ErrorInfo()
            {
                Domain = "Rich Error Model Demo",
                Reason = "Full error requested in the demo",
                Metadata =
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                }
            };
            RetryInfo retryInfo = new RetryInfo();
            retryInfo.RetryDelay = Duration.FromTimeSpan(new TimeSpan(0, 0, 5));
            QuotaFailure quotaFailure = new QuotaFailure()
            {
                Violations =
                {
                    new QuotaFailure.Types.Violation()
                    {
                        Description =  "Too much disk space used",
                        Subject = "Disk23"
                    }
                }
            };

            LocalizedMessage localizedMessage = new LocalizedMessage()
            {
                Locale = "en-GB",
                Message = "Example localised error message"
            };

            Google.Rpc.Status status = new Google.Rpc.Status
            {
                Code = (int)StatusCode.ResourceExhausted,
                Message = "Demo error - resource exhausted",
                Details =
                {
                    errorInfo,
                    retryInfo,
                    quotaFailure,                        
                    localizedMessage
                }
            };

            return status.ToRpcException();
        }

    }
}