using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.Web.UI
{
    public static class HtmlTableExtensions
    {
        public static HtmlTextWriter Table(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Table);
        }

        public static HtmlTextWriter Table(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Table, attributes);
        }

        /// <summary>
        /// Renders a caption start tag.
        /// </summary>
        /// <param name="writer">The writer to render to.</param>
        /// <returns>The writer.</returns>
        public static HtmlTextWriter Caption(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Caption);
        }

        public static HtmlTextWriter Caption(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Caption, attributes);
        }
        /// <summary>
        /// Renders a complete caption element (start and end tag) with the specified text
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static HtmlTextWriter Caption(this HtmlTextWriter writer, string text)
        {
            return Caption(writer)
                .Text(text)
                .EndTag();
        }

        public static HtmlTextWriter Caption(this HtmlTextWriter writer, string text, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return Caption(writer, attributes)
                .Text(text)
                .EndTag();
        }

        public static HtmlTextWriter Colgroup(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Colgroup);
        }

        public static HtmlTextWriter Colgroup(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Colgroup, attributes);
        }

        public static HtmlTextWriter Col(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Col);
        }

        public static HtmlTextWriter Col(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Col, attributes);
        }

        public static HtmlTextWriter Thead(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Thead);
        }

        public static HtmlTextWriter Thead(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Thead, attributes);
        }

        public static HtmlTextWriter Tbody(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Tbody);
        }

        public static HtmlTextWriter Tbody(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Tbody, attributes);
        }

        public static HtmlTextWriter Tfoot(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Tfoot);
        }

        public static HtmlTextWriter Tfoot(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Tfoot, attributes);
        }

        public static HtmlTextWriter Tr(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Tr);
        }

        public static HtmlTextWriter Tr(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Tr, attributes);
        }

        /// <summary>
        /// Renders a th start tag.
        /// </summary>
        /// <param name="writer">The writer to render to.</param>
        /// <returns>The writer.</returns>
        public static HtmlTextWriter Th(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Th);
        }

        public static HtmlTextWriter Th(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Th, attributes);
        }

        /// <summary>
        /// Renders a complete th element (start and end tag) with the specified text
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static HtmlTextWriter Th(this HtmlTextWriter writer, string text)
        {
            return Th(writer)
                .Text(text)
                .EndTag();
        }

        public static HtmlTextWriter Th(this HtmlTextWriter writer, string text, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return Th(writer, attributes)
                .Text(text)
                .EndTag();
        }

        /// <summary>
        /// Renders a td start tag.
        /// </summary>
        /// <param name="writer">The writer to render to.</param>
        /// <returns>The writer.</returns>
        public static HtmlTextWriter Td(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Td);
        }

        public static HtmlTextWriter Td(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Td, attributes);
        }

        /// <summary>
        /// Renders a complete TD element (start and end tag) with the specified text
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static HtmlTextWriter Td(this HtmlTextWriter writer, string text)
        {
            return Td(writer)
                .Text(text)
                .EndTag();
        }

        public static HtmlTextWriter Td(this HtmlTextWriter writer, string text, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return Td(writer, attributes)
                .Text(text)
                .EndTag();
        }
    }
}
