using System;

namespace OpenNETCF.Web.UI
{
    public static class HtmlFormExtensions
    {
        public static HtmlTextWriter Form(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Form);
        }

        public static HtmlTextWriter Form(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Form, attributes);
        }

        public static HtmlTextWriter Fieldset(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Fieldset, attributes);
        }

        public static HtmlTextWriter Fieldset(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Fieldset);
        }

        public static HtmlTextWriter Legend(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Legend, attributes);
        }

        public static HtmlTextWriter Legend(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Legend);
        }

        public static HtmlTextWriter Select(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Select);
        }

        public static HtmlTextWriter Select(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Select, attributes);
        }

        public static HtmlTextWriter Option(this HtmlTextWriter writer)
        {
            return writer.Tag(HtmlTextWriterTag.Option);
        }

        public static HtmlTextWriter Option(this HtmlTextWriter writer, string name, string value, string text)
        {
            return writer
                .Tag(HtmlTextWriterTag.Option, t => t
                    [HtmlTextWriterAttribute.Value, value]
                    [HtmlTextWriterAttribute.Name, name])
                .Text(text);
        }

        public static HtmlTextWriter Option(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Option, attributes);
        }

        public static HtmlTextWriter Button(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Button, attributes);
        }

        public static HtmlTextWriter SubmitButton(this HtmlTextWriter writer, string name, string value)
        {
            return writer
                        .Tag(HtmlTextWriterTag.Input, t => t
                            [HtmlTextWriterAttribute.Type, "submit"]
                            [HtmlTextWriterAttribute.Name, name]
                            [HtmlTextWriterAttribute.Value, value])
                        .EndTag(); // input
        }

        public static HtmlTextWriter SubmitButton(this HtmlTextWriter writer, string id, string name, string value)
        {
            return writer
                        .Tag(HtmlTextWriterTag.Input, t => t
                            [HtmlTextWriterAttribute.Type, "submit"]
                            [HtmlTextWriterAttribute.Name, name]
                            [HtmlTextWriterAttribute.Id, id]
                            [HtmlTextWriterAttribute.Value, value])
                        .EndTag(); // input
        }

        public static HtmlTextWriter Input(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer.Tag(HtmlTextWriterTag.Input, attributes);
        }

        public static HtmlTextWriter Label(this HtmlTextWriter writer, Func<HtmlAttributeManager, HtmlAttributeManager> attributes)
        {
            return writer
                .Tag(HtmlTextWriterTag.Label, attributes);
        }

        public static HtmlTextWriter Label(this HtmlTextWriter writer)
        {
            return writer
                .Tag(HtmlTextWriterTag.Label);
        }

        public static HtmlTextWriter TextArea(this HtmlTextWriter writer, string name, string id, int rows, int columns)
        {
            return writer.Tag(HtmlTextWriterTag.Textarea, t => t
                [HtmlTextWriterAttribute.Name, name]
                [HtmlTextWriterAttribute.Id, id]
                [HtmlTextWriterAttribute.Rows, rows.ToString()]
                [HtmlTextWriterAttribute.Cols, columns.ToString()]);
        }
        public static HtmlTextWriter TextArea(this HtmlTextWriter writer, string id, int rows, int columns)
        {
            return writer.Tag(HtmlTextWriterTag.Textarea, t => t
                [HtmlTextWriterAttribute.Id, id]
                [HtmlTextWriterAttribute.Rows, rows.ToString()]
                [HtmlTextWriterAttribute.Cols, columns.ToString()]);
        }
    }
}
