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
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    ///     Provides a factory for a class that Abstracts Http client requests.
    ///     NOTE: This interface is intended for internal use.  It will be marked internal once a problem with mocking is resolved.
    /// </summary>
    // NEIN: This should be internal, only public now because of a moq problem
    public interface IHttpClientAbstractionFactory
    {
        /// <summary>
        ///     Creates a new HttpClientAbstraction class.
        /// </summary>
        /// <param name="cert">
        ///     The X509 cert to use.
        /// </param>
        /// <returns>
        ///     A new instance of the HttpClientAbstraction.
        /// </returns>
        IHttpClientAbstraction Create(X509Certificate2 cert);

        /// <summary>
        ///     Creates a new HttpClientAbstraction class.
        /// </summary>
        /// <returns>
        ///     A new instance of the HttpClientAbstraction.
        /// </returns>
        IHttpClientAbstraction Create();
    }
}
