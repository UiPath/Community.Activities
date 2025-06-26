using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Activities;
using System.ComponentModel;
using System.Activities.Statements;
using System.Activities.Validation;


namespace UiPathTeam.XLExcel.Activities
{
    public abstract class ExcelSheetExtensionCodeActivity : CodeActivity
    {

        public ExcelSheetExtensionCodeActivity()
        {
            base.Constraints.Add(CheckParent());
         
        }

        public string Name { get; set; }

        static Constraint CheckParent()
        {
            DelegateInArgument<ExcelSheetExtensionCodeActivity> element = new DelegateInArgument<ExcelSheetExtensionCodeActivity>();
            DelegateInArgument<ValidationContext> context = new DelegateInArgument<ValidationContext>();
            Variable<bool> result = new Variable<bool>();
            DelegateInArgument<Activity> parent = new DelegateInArgument<Activity>();

            return new Constraint<ExcelSheetExtensionCodeActivity>
            {
                Body = new ActivityAction<ExcelSheetExtensionCodeActivity, ValidationContext>
                {
                    Argument1 = element,
                    Argument2 = context,
                    Handler = new Sequence
                    {
                        Variables =
                    {
                        result
                    },
                        Activities =
                    {
                        new ForEach<Activity>
                        {
                            Values = new GetParentChain
                            {
                                ValidationContext = context
                            },
                            Body = new ActivityAction<Activity>
                            {
                                Argument = parent,
                                Handler = new If()
                                {
                                    Condition = new InArgument<bool>((env) => object.Equals(parent.Get(env).GetType(),typeof(XLExcelApplicationScope))),
                                    Then = new Assign<bool>
                                    {
                                        Value = true,
                                        To = result
                                    }
                                }
                            }
                        },
                        new AssertValidation
                        {
                            Assertion = new InArgument<bool>(result),
                            Message = new InArgument<string> ("This activity has to be inside a XL Excel Application Scope activity"),
                        }
                    }
                    }
                }
            };
        }
    }
}
