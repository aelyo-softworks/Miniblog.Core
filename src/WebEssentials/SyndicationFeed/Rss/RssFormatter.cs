// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;

namespace Microsoft.SyndicationFeed.Rss
{
    public class RssFormatter : ISyndicationFeedFormatter
    {
        private readonly XmlWriter _writer;
        private readonly StringBuilder _buffer;

        public RssFormatter()
            : this(null, null)
        {
        }

        public RssFormatter(IEnumerable<ISyndicationAttribute>? knownAttributes, XmlWriterSettings? settings)
        {
            _buffer = new StringBuilder();
            _writer = XmlUtils.CreateXmlWriter(settings?.Clone() ?? new XmlWriterSettings(), knownAttributes!, _buffer);
        }

        public bool UseCDATA { get; set; }

        public string Format(ISyndicationContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            try
            {
                WriteSyndicationContent(content);

                _writer.Flush();

                return _buffer.ToString();
            }
            finally
            {
                _buffer.Clear();
            }
        }

        public string Format(ISyndicationCategory category) => Format(CreateContent(category));

        public string Format(ISyndicationImage image) => Format(CreateContent(image));

        public string Format(ISyndicationPerson person) => Format(CreateContent(person));

        public string Format(ISyndicationItem item) => Format(CreateContent(item));

        public string Format(ISyndicationLink link) => Format(CreateContent(link));

        [return: NotNullIfNotNull(nameof(value))]
        public virtual string? FormatValue<T>(T? value)
        {
            if (value == null)
                return null;

            var type = typeof(T);
            if (type == typeof(DateTimeOffset))
                return DateTimeUtils.ToRfc1123String((DateTimeOffset)(object)value);

            if (type == typeof(DateTime))
                return DateTimeUtils.ToRfc1123String(new DateTimeOffset((DateTime)(object)value));

            return value.ToString() ?? string.Empty;
        }

        public virtual ISyndicationContent CreateContent(ISyndicationLink link)
        {
            ArgumentNullException.ThrowIfNull(link);
            ArgumentNullException.ThrowIfNull(link.Uri);

            return link.RelationshipType switch
            {
                RssElementNames.Enclosure => CreateEnclosureContent(link),
                RssElementNames.Comments => CreateCommentsContent(link),
                RssElementNames.Source => CreateSourceContent(link),
                _ => CreateLinkContent(link),
            };
        }

        public virtual ISyndicationContent CreateContent(ISyndicationCategory category)
        {
            ArgumentNullException.ThrowIfNull(category);
            ArgumentNullException.ThrowIfNull(category.Name);

            var content = new SyndicationContent(RssElementNames.Category, category.Name);
            if (category.Scheme != null)
            {
                content.AddAttribute(new SyndicationAttribute(RssConstants.Domain, category.Scheme));
            }

            return content;
        }

        public virtual ISyndicationContent CreateContent(ISyndicationPerson person)
        {
            ArgumentNullException.ThrowIfNull(person);
            ArgumentNullException.ThrowIfNull(person.Email);

            // Real name recommended with RSS e-mail addresses
            // Ex: <author>email@address.com (John Doe)</author>
            var value = string.IsNullOrEmpty(person.Name) ? person.Email : $"{person.Email} ({person.Name})";

            return new SyndicationContent(person.RelationshipType ?? RssElementNames.Author, value);
        }

        public virtual ISyndicationContent CreateContent(ISyndicationImage image)
        {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(image.Title);
            ArgumentNullException.ThrowIfNull(image.Link);
            ArgumentNullException.ThrowIfNull(image.Url);

            var content = new SyndicationContent(RssElementNames.Image);

            // Write required contents of image
            content.AddField(new SyndicationContent(RssElementNames.Url, FormatValue(image.Url)));
            content.AddField(new SyndicationContent(RssElementNames.Title, image.Title));
            content.AddField(CreateContent(image.Link));


            // Write optional elements
            if (!string.IsNullOrEmpty(image.Description))
            {
                content.AddField(new SyndicationContent(RssElementNames.Description, image.Description));
            }

            return content;
        }

        public virtual ISyndicationContent CreateContent(ISyndicationItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentException.ThrowIfNullOrEmpty(item.Title);
            ArgumentException.ThrowIfNullOrEmpty(item.Description);

            // Spec requires to have at least one title or description

            // Write <item> tag
            var content = new SyndicationContent(RssElementNames.Item);

            // Title
            if (!string.IsNullOrEmpty(item.Title))
            {
                content.AddField(new SyndicationContent(RssElementNames.Title, item.Title));
            }

            // Links
            ISyndicationLink? guidLink = null;
            if (item.Links != null)
            {
                foreach (var link in item.Links)
                {
                    if (link.RelationshipType == RssElementNames.Guid)
                    {
                        guidLink = link;
                    }

                    content.AddField(CreateContent(link));
                }
            }

            // Description
            if (!string.IsNullOrEmpty(item.Description))
            {
                content.AddField(new SyndicationContent(RssElementNames.Description, item.Description));
            }

            // Authors (persons)
            if (item.Contributors != null)
            {
                foreach (var person in item.Contributors)
                {
                    content.AddField(CreateContent(person));
                }
            }

            // Cathegory
            if (item.Categories != null)
            {
                foreach (var category in item.Categories)
                {
                    content.AddField(CreateContent(category));
                }
            }

            // Guid (id)
            if (guidLink == null && !string.IsNullOrEmpty(item.Id))
            {
                var guid = new SyndicationContent(RssElementNames.Guid, item.Id);
                guid.AddAttribute(new SyndicationAttribute(RssConstants.IsPermaLink, "false"));
                content.AddField(guid);
            }

            // PubDate
            if (item.Published != DateTimeOffset.MinValue)
            {
                content.AddField(new SyndicationContent(RssElementNames.PubDate, FormatValue(item.Published)));
            }

            return content;
        }


        private SyndicationContent CreateEnclosureContent(ISyndicationLink link)
        {
            var content = new SyndicationContent(RssElementNames.Enclosure);

            // Url
            content.AddAttribute(new SyndicationAttribute(RssElementNames.Url, FormatValue(link.Uri)));

            // Length
            if (link.Length == 0)
                throw new ArgumentException("Enclosure requires length attribute");

            content.AddAttribute(new SyndicationAttribute(RssConstants.Length, FormatValue(link.Length)));

            // MediaType
            ArgumentException.ThrowIfNullOrEmpty(link.MediaType);

            content.AddAttribute(new SyndicationAttribute(RssConstants.Type, link.MediaType));
            return content;
        }

        private SyndicationContent CreateLinkContent(ISyndicationLink link)
        {
            SyndicationContent content;

            if (string.IsNullOrEmpty(link.RelationshipType) || link.RelationshipType == RssLinkTypes.Alternate)
            {
                // Regular <link>
                content = new SyndicationContent(RssElementNames.Link);
            }
            else
            {
                // Custom
                content = new SyndicationContent(link.RelationshipType);
            }

            // title 
            if (!string.IsNullOrEmpty(link.Title))
            {
                content.Value = link.Title;
            }

            // url
            var url = FormatValue(link.Uri);
            if (content.Value != null)
            {
                content.AddAttribute(new SyndicationAttribute(RssElementNames.Url, url));
            }
            else
            {
                content.Value = url;
            }

            // Type
            if (!string.IsNullOrEmpty(link.MediaType))
            {
                content.AddAttribute(new SyndicationAttribute(RssConstants.Type, link.MediaType));
            }

            // Lenght
            if (link.Length != 0)
            {
                content.AddAttribute(new SyndicationAttribute(RssConstants.Length, FormatValue(link.Length)));
            }

            return content;
        }

        private SyndicationContent CreateCommentsContent(ISyndicationLink link)
        {
            ArgumentException.ThrowIfNullOrEmpty(link.RelationshipType);
            return new SyndicationContent(link.RelationshipType) { Value = FormatValue(link.Uri) };
        }

        private SyndicationContent CreateSourceContent(ISyndicationLink link)
        {
            ArgumentException.ThrowIfNullOrEmpty(link.RelationshipType);
            var content = new SyndicationContent(link.RelationshipType);

            // Url
            var url = FormatValue(link.Uri);
            if (link.Title != url)
            {
                content.AddAttribute(new SyndicationAttribute(RssElementNames.Url, url));
            }

            // Title
            if (!string.IsNullOrEmpty(link.Title))
            {
                content.Value = link.Title;
            }

            return content;
        }

        private void WriteSyndicationContent(ISyndicationContent content)
        {
            // Write Start
            _writer.WriteStartSyndicationContent(content, null);

            // Write attributes
            if (content.Attributes != null)
            {
                foreach (var a in content.Attributes)
                {
                    _writer.WriteSyndicationAttribute(a);
                }
            }

            // Write value
            if (content.Value != null)
            {
                _writer.WriteString(content.Value, UseCDATA);
            }
            // Write Fields
            else
            {
                if (content.Fields != null)
                {
                    foreach (var field in content.Fields)
                    {
                        WriteSyndicationContent(field);
                    }
                }
            }

            //
            // Write End
            _writer.WriteEndElement();
        }
    }
}