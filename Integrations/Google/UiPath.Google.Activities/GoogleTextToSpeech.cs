using Google.Cloud.TextToSpeech.V1;
using System;
using System.Activities;
using System.ComponentModel;

namespace UiPath.Google.Activities
{
    public enum GenderType
    {
        Male,
        Female
    };

    public class GoogleTextToSpeech : CodeActivity
    {
        [Category("Input")]
        [Description("Text to be transformed into speech")]
        [RequiredArgument]
        public InArgument<string> Text { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> LanguageCode { get; set; }

        [Category("Input")]
        public GenderType Gender { get; set; }

        [Category("Input")]
        [Description("The service account json file generated from Google Cloud Platform")]
        [RequiredArgument]
        public InArgument<string> ServiceAccountFile { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var text = Text.Get(context);
            var languageCode = LanguageCode.Get(context);
            var serviceAcc = ServiceAccountFile.Get(context);
            SsmlVoiceGender gender = (SsmlVoiceGender)Enum.Parse(typeof(SsmlVoiceGender), Gender.ToString());

            Recognize.TextToSpeech(text, languageCode, gender, serviceAcc);
        }
    }
}
