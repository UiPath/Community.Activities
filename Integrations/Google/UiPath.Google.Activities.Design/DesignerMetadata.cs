using System.Activities.Presentation.Metadata;
using System.ComponentModel;

namespace UiPath.Google.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            AttributeTableBuilder builder = new AttributeTableBuilder();
            builder.AddCustomAttributes(typeof(GoogleSpeechToText), new CategoryAttribute("Integrations.Google.Speech"));
            builder.AddCustomAttributes(typeof(GoogleTextToSpeech), new CategoryAttribute("Integrations.Google.Speech"));

            builder.AddCustomAttributes(typeof(GoogleSpeechToText), new DisplayNameAttribute("Speech to text"));
            builder.AddCustomAttributes(typeof(GoogleTextToSpeech), new DisplayNameAttribute("Text to speech"));
            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
