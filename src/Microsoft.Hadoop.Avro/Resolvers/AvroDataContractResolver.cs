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
namespace Microsoft.Hadoop.Avro
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    /// <summary>
    /// Allows using standard <see cref="T:System.Runtime.Serialization.DataContractAttribute"/> and 
    /// <see cref="T:System.Runtime.Serialization.DataMemberAttribute"/> attributes for defining what types/properties/fields
    /// should be serialized.
    /// </summary>
    public class AvroDataContractResolver : AvroContractResolver
    {
        private readonly bool allowNullable;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvroDataContractResolver"/> class.
        /// </summary>
        public AvroDataContractResolver() : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvroDataContractResolver"/> class.
        /// </summary>
        /// <param name="allowNullable">If set to <c>true</c>, null values are allowed.</param>
        public AvroDataContractResolver(bool allowNullable)
        {
            this.allowNullable = allowNullable;
        }

        /// <summary>
        /// Gets the known types out of an abstract type or interface that could be present in the tree of
        /// objects serialized with this contract resolver.
        /// </summary>
        /// <param name="type">The abstract type.</param>
        /// <returns>
        /// An enumerable of known types.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The type argument is null.</exception>
        public override IEnumerable<Type> GetKnownTypes(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            Contract.EndContractBlock();

            return new HashSet<Type>(type.GetAllKnownTypes());
        }

        /// <summary>
        /// Gets the serialization information about the type.
        /// This information is used for creation of the corresponding schema node.
        /// </summary>
        /// <param name="type">The type to resolve.</param>
        /// <returns>
        /// Serialization information about the type.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The type argument is null.</exception>
        public override TypeSerializationInfo ResolveType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            Contract.EndContractBlock();

            if (type.IsUnsupported())
            {
                throw new SerializationException(
                    string.Format(CultureInfo.InvariantCulture, "Type '{0}' is not supported by the resolver.", type));
            }

            bool canContainNull = this.allowNullable && type.CanContainNull();

            if (type.IsInterface ||
                type.IsNativelySupported() ||
                (type.IsEnum && !type.GetCustomAttributes(false).OfType<DataContractAttribute>().Any()))
            {
                return new TypeSerializationInfo
                {
                    Name = StripAvroNonCompatibleCharacters(type.Name),
                    Namespace = StripAvroNonCompatibleCharacters(type.Namespace),
                    Nullable = canContainNull
                };
            }

            type = Nullable.GetUnderlyingType(type) ?? type;

            var attributes = type.GetCustomAttributes(false);
            var dataContract = attributes.OfType<DataContractAttribute>().SingleOrDefault();
            if (dataContract == null)
            {
                throw new SerializationException(
                    string.Format(CultureInfo.InvariantCulture, "Type '{0}' is not supported by the resolver.", type));
            }

            var name = StripAvroNonCompatibleCharacters(dataContract.Name ?? type.Name);
            var nspace = StripAvroNonCompatibleCharacters(dataContract.Namespace ?? type.Namespace);
            return new TypeSerializationInfo
            {
                Name = name,
                Namespace = nspace,
                Nullable = canContainNull
            };
        }

        /// <summary>
        /// Gets the serialization information about the type members.
        /// This information is used for creation of the corresponding schema nodes.
        /// </summary>
        /// <param name="type">The type, members of which should be serialized.</param>
        /// <returns>
        /// Serialization information about the fields/properties.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The type argument is null.</exception>
        public override MemberSerializationInfo[] ResolveMembers(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            Contract.EndContractBlock();

            if (type.IsKeyValuePair())
            {
                var keyValueProperties = type.GetAllProperties();
                return keyValueProperties
                    .Select(p => new MemberSerializationInfo
                                 {
                                     Name = p.Name,
                                     MemberInfo = p,
                                     Nullable = false
                                 })
                    .ToArray();
            }

            var fields = type.GetAllFields();
            var properties = type.GetAllProperties();

            var members = fields
                .Concat<MemberInfo>(properties)
                .Select(m => new
                {
                    Member = m,
                    Attribute = m.GetCustomAttributes(false).OfType<DataMemberAttribute>().SingleOrDefault(),
                    Nullable = m.GetCustomAttributes(false).OfType<NullableSchemaAttribute>().Any()
                });

            return members.Where(m => m.Attribute != null)
                          .Select(m => new MemberSerializationInfo
                          {
                              Name = m.Attribute.Name ?? m.Member.Name,
                              MemberInfo = m.Member,
                              Nullable = m.Nullable
                          })
                          .ToArray();
        }
    }
}