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
using Grpc.Core;
using Google.Rpc;
using Grpc.RichErrorModel;
using REM.Example;

using Google.Protobuf.WellKnownTypes;

namespace REMDemoClient
{
    class ClientProgram
    {
        public static void Main(string[] args)
        {
            Channel channel = new Channel("127.0.0.1:30051", ChannelCredentials.Insecure);

            var client = new RichErrorModelDemo.RichErrorModelDemoClient(channel);

            try
            {
                Console.WriteLine("*** First request");
                var reply = client.EchoRequest(
                    new DemoRequest { Message = "first message", Action = REM.Example.Action.Echo });
                Console.WriteLine("Response: " + reply.EchoedMessage);
            }
            catch (RpcException ex)
            {
                PrintStatusDetails(ex);
            }

            try
            {
                Console.WriteLine();
                Console.WriteLine("*** Second request");
                var reply = client.EchoRequest(
                    new DemoRequest { Message = "second message", Action = REM.Example.Action.SimpleError });

                // should not get here as an exception should have been thrown
                Console.WriteLine("Response: " + reply.EchoedMessage);
            }
            catch (RpcException ex)
            {
                PrintSimpleError(ex);
            }

            try
            {
                Console.WriteLine();
                Console.WriteLine("*** Third request");
                var reply = client.EchoRequest(
                    new DemoRequest { Message = "third message", Action = REM.Example.Action.ComplexError });

                // should not get here as an exception should have been thrown
                Console.WriteLine("Response: " + reply.EchoedMessage);
            }
            catch (RpcException ex)
            {
                PrintStatusDetails(ex);
            }

            channel.ShutdownAsync().Wait();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Example of extracting rich error information when it is known
        /// in advanced what is expected.
        /// 
        /// In this example we are just expecting ErrorInfo and RequestInfo
        /// so can ask for those explicitly.
        /// </summary>
        /// <param name="ex"></param>
        private static void PrintSimpleError(RpcException ex)
        {
            Grpc.Core.Status status = ex.Status;

            Console.WriteLine("Exception returned from the call.");
            Console.WriteLine($"RpcException: Status: {status.StatusCode} : {status.Detail}");

            Google.Rpc.Status? rpcStatus = ex.GetRpcStatus();

            if (rpcStatus == null)
            {
                Console.WriteLine("No Rich Error information found.");
            }
            else
            {
                Console.WriteLine("Rich Error information found.");
                Console.WriteLine($"Google.Rpc Status: Code: {rpcStatus.Code}, Message: {rpcStatus.Message}");

                ErrorInfo? errorInfo = rpcStatus.GetErrorInfo();
                if (errorInfo != null)
                {
                    Console.WriteLine($"\tErrorInfo: Reason: {errorInfo.Reason}, Domain: {errorInfo.Domain}");
                    foreach (var md in errorInfo.Metadata)
                    {
                        Console.WriteLine($"\tKey: {md.Key}, Value: {md.Value}");
                    }
                }

                RequestInfo? requestInfo = rpcStatus.GetRequestInfo();
                if (requestInfo != null)
                {
                    Console.WriteLine($"RequestInfo: RequestId: {requestInfo.RequestId}," +
                        $" ServingData: {requestInfo.ServingData}");
                }
            }
        }

        /// <summary>
        /// An example of processing all the list of details in the rich error status
        /// without having the know in advanced what is being sent.
        /// </summary>
        /// <param name="ex"></param>
        private static void PrintStatusDetails(RpcException ex)
        {
            Grpc.Core.Status status = ex.Status;

            Console.WriteLine("Exception returned from the call.");
            Console.WriteLine($"RpcException: Status: {status.StatusCode} : {status.Detail}");

            Google.Rpc.Status? rpcStatus = ex.GetRpcStatus();

            if (rpcStatus == null)
            {
                Console.WriteLine("No Rich Error information found.");
            }
            else
            {
                // Decode each "Any" item in turn
                foreach (Any any in rpcStatus.Details)
                {
                    var typeUrl = any.TypeUrl;
                    switch(typeUrl)
                    {
                        case TypeUrls.ErrorInfoTypeUrl:
                            var errorInfo = any.SafeUnpack<ErrorInfo>();
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
                            var badRequest = any.SafeUnpack<BadRequest>();
                            if (badRequest != null)
                            {
                                Console.WriteLine("BadRequest:");
                                foreach (BadRequest.Types.FieldViolation fv in badRequest.FieldViolations)
                                {
                                    Console.WriteLine($"\tField: {fv.Field}, Description: {fv.Description}");
                                }
                            }
                            break;
                        case TypeUrls.RetryInfoTypeUrl:
                            var retryInfo = any.SafeUnpack<RetryInfo>();
                            if (retryInfo != null)
                            {
                                Console.WriteLine($"RetryInfo: RetryDelay: {retryInfo.RetryDelay}");
                            }
                            break;
                        case TypeUrls.DebugInfoTypeUrl:
                            var debugInfo = any.SafeUnpack<DebugInfo>();
                            if (debugInfo != null)
                            {
                                Console.WriteLine($"DebugInfo: Detail: {debugInfo.Detail}");
                                foreach (string stackEntry in debugInfo.StackEntries)
                                {
                                    Console.WriteLine($"\tStackEntry: {stackEntry}");
                                }
                            }
                            break;
                        case TypeUrls.QuotaFailureTypeUrl:
                            var quotaFailure = any.SafeUnpack<QuotaFailure>();
                            if (quotaFailure != null)
                            {
                                Console.WriteLine("QuotaFailure:");
                                foreach (QuotaFailure.Types.Violation v in quotaFailure.Violations)
                                {
                                    Console.WriteLine($"\tDescription: {v.Description}, Subject: {v.Subject}");
                                }
                            }
                            break;
                        case TypeUrls.PreconditionFailureTypeUrl:
                            var preconditionFailure = any.SafeUnpack<PreconditionFailure>();
                            if (preconditionFailure != null)
                            {
                                Console.WriteLine("PreconditionFailure:");
                                foreach (PreconditionFailure.Types.Violation v in preconditionFailure.Violations)
                                {
                                    Console.WriteLine($"\tDescription: {v.Description}, Subject: {v.Subject}, Type: {v.Type}");
                                }
                            }
                            break;
                        case TypeUrls.RequestInfoTypeUrl:
                            var requestInfo = any.SafeUnpack<RequestInfo>();
                            if (requestInfo != null)
                            {
                                Console.WriteLine("RequestInfo");
                                Console.WriteLine($"\tRequestId: {requestInfo.RequestId}, ServingData: {requestInfo.ServingData}");
                            }
                            break;
                        case TypeUrls.ResourceInfoTypeUrl:
                            var resourceInfo = any.SafeUnpack<ResourceInfo>();
                            if (resourceInfo != null)
                            {
                                Console.WriteLine("ResourceInfo");
                                Console.WriteLine($"\tResourceName: {resourceInfo.ResourceName}," +
                                    $" ResourceType: {resourceInfo.ResourceType}," +
                                    $" Owner: {resourceInfo.Owner}");
                            }
                            break;
                        case TypeUrls.HelpTypeUrl:
                            var help = any.SafeUnpack<Help>();
                            if (help != null)
                            {
                                Console.WriteLine("Help:");
                                foreach (Help.Types.Link link in help.Links)
                                {
                                    Console.WriteLine($"\tLink: {link.Description} : {link.Url}");
                                }
                            }
                            break;
                        case TypeUrls.LocalizedMessageTypeUrl:
                            var localizedMessage = any.SafeUnpack<LocalizedMessage>();
                            if (localizedMessage != null)
                            {
                                Console.WriteLine($"LocalizedMessage:");
                                Console.WriteLine($"\tLocale: {localizedMessage.Locale}, Message: {localizedMessage.Message}");
                            }
                            break;
                        default:
                            Console.WriteLine($"Unknown type URL: {typeUrl}");
                            break;
                    }
                }
            }
        }
    }
}
