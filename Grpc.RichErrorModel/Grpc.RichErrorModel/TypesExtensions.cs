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
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace Grpc.RichErrorModel
{
    /// <summary>
    /// Extensions for protocol buffers types
    /// </summary>
    public static class TypesExtensions
    {

        /// <summary>
        /// Adds a protocol buffers message to a <see cref="RepeatedField{T}"/> list.
        /// This allows the collection initialization syntax to be used on a RepeatedField&lt;Any&gt;
        /// <para>In the example below Details is a RepeatedField&lt;Any&gt;
        /// </para>
        /// <code>
        /// var status = new Google.Rpc.Status {
        ///   Code = (int) StatusCode.NotFound,
        ///   Message = "error message",
        ///   Details = {
        ///     new ErrorInfo { /* ... */ },
        ///     new RequestInfo { /* ... */ }
        ///   }
        /// }
        /// </code>
        /// </summary>
        /// <param name="list">List to add to</param>
        /// <param name="anyMsg">Protocol buffers message to add to the list. Must not be null.</param>
        public static void Add(this RepeatedField<Any> list, IMessage anyMsg)
        {
            list.Add(Any.Pack(anyMsg));
        }

        /// <summary>
        /// Unpack the given "Any" type, or return null if it is malformed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="any"></param>
        /// <returns>The unpacked type or null if not found or malformed</returns>
        public static T? SafeUnpack<T>(this Any any) where T : class, IMessage<T>, new()
        {
            try
            {
                return any.Unpack<T>();
            }
            catch
            {
                // If the message is malformed, just report there's no information.
                return null;
            }
        }
     }
}
