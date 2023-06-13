// Copyright 2023 gRPC authors.
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

using System;
using System.Threading.Tasks;
using Google.Rpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.RichErrorModel;
using REM.Example;

namespace REMDemoServer
{
    class RichErrorModelDemoImpl : RichErrorModelDemo.RichErrorModelDemoBase
    {
        // Server side handler of the EchoRequest RPC
        public override Task<DemoResponse> EchoRequest(DemoRequest request, ServerCallContext context)
        {
            switch (request.Action)
            {
                case REM.Example.Action.SimpleError:
                    throw CreateSimpleError(request);
                case REM.Example.Action.ComplexError:
                    throw CreateComplexError(request);
                default:
                    return Task.FromResult(new DemoResponse { EchoedMessage = "ECHO " + request.Message });
            }
        }

        private static RpcException CreateSimpleError(DemoRequest request)
        {
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

        private static RpcException CreateComplexError(DemoRequest request)
        {
            ErrorInfo errorInfo = new ErrorInfo()
            {
                Domain = "Rich Error Model Demo",
                Reason = "Complex error requested in the demo",
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

    class ServerProgram
    {
        const int Port = 30051;

        public static void Main(string[] args)
        {
            Server server = new Server
            {
                Services = { RichErrorModelDemo.BindService(new RichErrorModelDemoImpl()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("RichErrorModelDemo server listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
