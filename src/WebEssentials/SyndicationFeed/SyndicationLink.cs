﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.SyndicationFeed
{
    public sealed class SyndicationLink(Uri url, string? relationshipType = null) : ISyndicationLink
    {
        public Uri Uri { get; } = url ?? throw new ArgumentNullException(nameof(url));
        public string? Title { get; set; }
        public string? MediaType { get; set; }
        public string? RelationshipType { get; } = relationshipType;
        public long Length { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
    }
}