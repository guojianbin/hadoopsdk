﻿// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not
// use this file except in compliance with the License.  You may obtain a copy
// of the License at http://www.apache.org/licenses/LICENSE-2.0
// 
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.
// 
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

namespace Microsoft.WindowsAzure.Management.Framework.WebRequest
{
    using System;
    using System.Net;
    using System.Net.Http.Headers;

    /// <summary>
    ///     Abstracts Http client responses.
    ///     NOTE: This interface is intended for internal use.  It will be marked internal once a problem with mocking is resolved.
    /// </summary>
    // NEIN: This should be internal, only public now because of a moq problem
    public interface IHttpResponseMessageAbstraction 
    {
        /// <summary>
        ///     Gets the status code for the response.
        /// </summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Gets the Response Headers for the response.
        /// </summary>
        IHttpResponseHeadersAbstraction Headers { get; }

        /// <summary>
        ///     Gets the content returned by the response.
        /// </summary>
        string Content { get; }
    }
}
