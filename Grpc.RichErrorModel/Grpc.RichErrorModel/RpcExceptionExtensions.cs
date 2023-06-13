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

using Google.Protobuf;
using Grpc.Core;
using System.Linq;
using Grpc.Core.Utils;

namespace Grpc.RichErrorModel
{
    /// <summary>
    /// Based on https://github.com/googleapis/gax-dotnet/blob/main/Google.Api.Gax.Grpc/RpcExceptionExtensions.cs
    /// </summary>
    public static class RpcExceptionExtensions
    {
        // Name used in the metadata from the Google.Rpc.Status details
        internal const string StatusDetailsTrailerName = "grpc-status-details-bin";

        /// <summary>
        /// Retrieves the <see cref="Google.Rpc.Status"/> message containing extended error information
        /// from the trailers in an <see cref="RpcException"/>, if present.
        /// </summary>
        /// <param name="ex">The RPC exception to retrieve details from. Must not be null.</param>
        /// <returns>The <see cref="Google.Rpc.Status"/> message specified in the exception, or null
        /// if there is no such information.</returns>
        public static Google.Rpc.Status? GetRpcStatus(this RpcException ex)
        {
            return DecodeTrailer(GetTrailer(ex, StatusDetailsTrailerName), Google.Rpc.Status.Parser);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static Metadata.Entry GetTrailer(RpcException ex, string key)
        {
            GrpcPreconditions.CheckNotNull(ex, nameof(ex));
            return ex.Trailers.FirstOrDefault(t => t.Key == key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entry"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        private static T? DecodeTrailer<T>(Metadata.Entry entry, MessageParser<T> parser) where T : class, IMessage<T>
        {
            if (entry is null)
            {
                return null;
            }
            try
            {
                return parser.ParseFrom(entry.ValueBytes);
            }
            catch
            {
                // If the message is malformed, just report there's no information.
                return null;
            }
        }
    }
}
