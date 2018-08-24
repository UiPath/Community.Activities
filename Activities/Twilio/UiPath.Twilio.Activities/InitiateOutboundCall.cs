using System;
using System.Activities;
using System.ComponentModel;

namespace Twilio.Workflow.Activities
{
    public class InitiateOutboundCall : CodeActivity
    {
        [Description("Your Twilio Account SID.  The SID can be obtained from your Twilio dashboard: https://www.twilio.com/user/account")]
        public InArgument<string> AccountSid { get; set; }

        [Description("Your Twilio Authorization Token.  The token can be obtained from your Twilio dashboard: https://www.twilio.com/user/account")]
        public InArgument<string> AuthToken { get; set; }

        [Description("The phone number to send this message to")]
        public InArgument<string> To { get; set; }

        [Description("The phone number to send this message from")]
        public InArgument<string> From { get; set; }

        [Description("The URL Twilio should call when the call is answered")]
        public InArgument<string> CallbackUri { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var client = new TwilioRestClient(this.AccountSid.Get(context), this.AuthToken.Get(context));
            var result = client.InitiateOutboundCall(this.From.Get(context), this.To.Get(context), this.CallbackUri.Get(context));

            if (result.RestException != null)
            {
                throw new Exception(String.Format("Outbound call failed: {0}", result.RestException.Message));
            }
        }
    }
}