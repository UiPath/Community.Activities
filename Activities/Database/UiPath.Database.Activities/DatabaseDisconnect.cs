using System;
using System.Activities;
using System.ComponentModel;
using System.Data.Common;
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
            try
            {
                var dbConnection = DatabaseConnection.Get(context);
                // create the action for doing the actual work
                Action action = () => dbConnection.Dispose();
                context.UserState = action;

                return action.BeginInvoke(callback, state);
            }
            catch (DbException ex)
            {
                throw new Exception("[Database driver error]: " + ex.Message + " " + ex?.InnerException?.Message, ex);
            }
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            try
            {
                Action action = (Action)context.UserState;
                action.EndInvoke(result);
            }
            catch (DbException ex)
            {
                throw new Exception("[Database driver error]: " + ex.Message + " " + ex?.InnerException?.Message, ex);
            }
        }
    }
}
