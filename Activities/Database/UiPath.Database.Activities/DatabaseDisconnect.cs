using System;
using System.Activities;
using System.ComponentModel;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities
{
    public class DatabaseDisconnect : AsyncCodeActivity
    {
        [LocalizedCategory(nameof(Resources.Connection))]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.DatabaseConnectionDisplayName))]
        public InArgument<DatabaseConnection> DatabaseConnection { get; set; }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var dbConnection = DatabaseConnection.Get(context);
            // create the action for doing the actual work
            Action action = () => dbConnection.Dispose();
            context.UserState = action;

            return action.BeginInvoke(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            Action action = (Action)context.UserState;
            action.EndInvoke(result);
        }
    }
}
