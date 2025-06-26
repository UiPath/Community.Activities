using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using System.ComponentModel;
using System.Activities.Statements;

using System.Threading.Tasks;
using System.Windows;
//using System.Activities.Presentation.Model;

using UiPathTeam.XLExcel;


namespace UiPathTeam.XLExcel.Activities
{

    //[Designer(typeof(XLExcelApplicationScopeDesigner))]
    public class XLExcelApplicationScope : NativeActivity
    {
        [Browsable(false)]
        public ActivityAction<XLExcelContextInfo> Body { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> FilePath { get; set; }


       


        public XLExcelApplicationScope()
        {

           Body = new ActivityAction<XLExcelContextInfo>
            {

                Argument = new DelegateInArgument<XLExcelContextInfo>(XLExcelContextInfo.XLExcelContextInfoTag),
                Handler = new Sequence { DisplayName = "Do" }
            };
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            //activity delegates are automatically cached in the base.CacheMetadata
            base.CacheMetadata(metadata);
        }


        protected override void Execute(NativeActivityContext context)
        {
            string filePath = FilePath.Get(context);
           

            XLExcelContextInfo customExcelCtx = new XLExcelContextInfo()
            {
                Path = filePath,
               
            };

            if (Body != null)
            {
               
                context.ScheduleAction<XLExcelContextInfo>(Body, customExcelCtx, OnCompleted, OnFaulted);
            }
        }
        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            //TODO
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            //TODO
        }

       
    }

}
