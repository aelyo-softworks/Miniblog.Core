// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.SyndicationFeed
{
    public interface ISyndicationFeedReader
    {
        SyndicationElementType ElementType { get; }

        string? ElementName { get; }

        Task<bool> Read();
        Task Skip();
        Task<ISyndicationItem> ReadItem();
        Task<ISyndicationLink> ReadLink();
        Task<ISyndicationPerson> ReadPerson();
        Task<ISyndicationImage> ReadImage();
        Task<ISyndicationContent> ReadContent();
        Task<ISyndicationCategory> ReadCategory();
        Task<T> ReadValue<T>();
        Task<string> ReadElementAsString();
    }
}
