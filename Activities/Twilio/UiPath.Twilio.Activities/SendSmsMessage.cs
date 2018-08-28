using System;
using System.Activities;
using System.ComponentModel;

namespace Twilio.Workflow.Activities
{
    public class SendSmsMessage : CodeActivity
    {
        [Description("Your Twilio Account SID.  The SID can be obtained from your Twilio dashboard: https://www.twilio.com/user/account")]
        public InArgument<string> AccountSid { get; set; }

        [Description("Your Twilio Authorization Token.  The token can be obtained from your Twilio dashboard: https://www.twilio.com/user/account")]
        public InArgument<string> AuthToken { get; set; }

        [Description("The phone number to send this message to")]
        public InArgument<string> To { get; set; }

        [Description("The phone number to send this message from")]
        public InArgument<string> From { get; set; }

        [Description("The message to send")]
        public InArgument<string> Message { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var client = new TwilioRestClient(this.AccountSid.Get(context), this.AuthToken.Get(context));
            var result = client.SendSmsMessage(this.From.Get(context), this.To.Get(context), this.Message.Get(context));

            if (result.RestException != null)
            {
                throw new Exception(String.Format("Message send failed: {0}", result.RestException.Message));
            }
        }
    }
}