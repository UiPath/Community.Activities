using System.Activities;

namespace UiPath.Examples.Activities
{
    public class Mail : CodeActivity<string>
    {
        public string ConnectionId { get; set; }

        public string Connector { get; set; }

        /*
         * The returned value will be used to set the value of the Result argument
         */
        protected override string Execute(CodeActivityContext context)
        {
            return string.Empty;
        }
    }
}
