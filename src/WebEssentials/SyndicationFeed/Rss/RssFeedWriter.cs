﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.SyndicationFeed.Rss
{
    public class RssFeedWriter(XmlWriter writer, IEnumerable<ISyndicationAttribute>? attributes, ISyndicationFeedFormatter? formatter)
        : XmlFeedWriter(writer, formatter ?? new RssFormatter(attributes, writer.Settings))
    {
        private readonly XmlWriter _writer = writer;
        private bool _feedStarted;
        private readonly IEnumerable<ISyndicationAttribute>? _attributes = attributes;

        public RssFeedWriter(XmlWriter writer, IEnumerable<ISyndicationAttribute>? attributes = null)
            : this(writer, attributes, null)
        {
        }

        public virtual Task WriteTitle(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return WriteValue(RssElementNames.Title, value);
        }

        public virtual Task WriteDescription(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return WriteValue(RssElementNames.Description, value);
        }

        public virtual Task WriteLanguage(CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(culture);
            return WriteValue(RssElementNames.Language, culture.Name);
        }

        public virtual Task WriteCopyright(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return WriteValue(RssElementNames.Copyright, value);
        }

        public virtual Task WritePubDate(DateTimeOffset dt)
        {
            if (dt == default)
                throw new ArgumentException(null, nameof(dt));

            return WriteValue(RssElementNames.PubDate, dt);
        }

        public virtual Task WriteLastBuildDate(DateTimeOffset dt)
        {
            if (dt == default)
                throw new ArgumentException(null, nameof(dt));

            return WriteValue(RssElementNames.LastBuildDate, dt);
        }

        public virtual Task WriteGenerator(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return WriteValue(RssElementNames.Generator, value);
        }

        public virtual Task WriteDocs() => WriteValue(RssElementNames.Docs, RssConstants.SpecificationLink);
        public virtual Task WriteCloud(Uri uri, string registerProcedure, string protocol)
        {
            ArgumentNullException.ThrowIfNull(uri);
            ArgumentNullException.ThrowIfNullOrEmpty(registerProcedure);

            if (!uri.IsAbsoluteUri)
                throw new ArgumentException("Absolute uri required");


            var cloud = new SyndicationContent(RssElementNames.Cloud);
            cloud.AddAttribute(new SyndicationAttribute("domain", uri.GetComponents(UriComponents.Host, UriFormat.UriEscaped)));
            cloud.AddAttribute(new SyndicationAttribute("port", uri.GetComponents(UriComponents.StrongPort, UriFormat.UriEscaped)));
            cloud.AddAttribute(new SyndicationAttribute("path", uri.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped)));
            cloud.AddAttribute(new SyndicationAttribute("registerProcedure", registerProcedure));
            cloud.AddAttribute(new SyndicationAttribute("protocol", protocol ?? "xml-rpc"));
            return Write(cloud);
        }

        public virtual Task WriteTimeToLive(TimeSpan ttl)
        {
            if (ttl == default)
                throw new ArgumentException(null, nameof(ttl));

            return WriteValue(RssElementNames.TimeToLive, (long)Math.Max(1, Math.Ceiling(ttl.TotalMinutes)));
        }

        public virtual Task WriteSkipHours(IEnumerable<byte> hours)
        {
            ArgumentNullException.ThrowIfNull(hours);
            var skipHours = new SyndicationContent(RssElementNames.SkipHours);
            foreach (var h in hours)
            {
                if (h < 0 || h > 23)
                    throw new ArgumentOutOfRangeException(nameof(hours), "Hour value must be between 0 and 23");

                skipHours.AddField(new SyndicationContent("hour", Formatter.FormatValue(h)));
            }
            return Write(skipHours);
        }

        public virtual Task WriteSkipDays(IEnumerable<DayOfWeek> days)
        {
            ArgumentNullException.ThrowIfNull(days);
            var skipDays = new SyndicationContent(RssElementNames.SkipDays);
            foreach (var d in days)
            {
                skipDays.AddField(new SyndicationContent("day", Formatter.FormatValue(d)));
            }
            return Write(skipDays);
        }

        public override Task WriteRaw(string content)
        {
            if (!_feedStarted)
            {
                StartFeed();
            }

            return XmlUtils.WriteRawAsync(_writer, content);
        }

        private void StartFeed()
        {
            // Write <rss version="2.0">
            _writer.WriteStartElement(RssElementNames.Rss);

            // Write attributes if exist
            if (_attributes != null)
            {
                foreach (var a in _attributes)
                {
                    _writer.WriteSyndicationAttribute(a);
                }
            }

            _writer.WriteAttributeString(RssElementNames.Version, RssConstants.Version);
            _writer.WriteStartElement(RssElementNames.Channel);
            _feedStarted = true;
        }
    }
}