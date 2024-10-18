using System;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Validation;

namespace UiPath.Shared.Activities
{
    public static class ActivityConstraints
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static Constraint HasParentType<TActivity, TParent>(string validationMessage) where TActivity : Activity where TParent : Activity
        {
            return HasParent<TActivity>(p => p as TParent != null, validationMessage);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static Constraint HasParent<TActivity>(Func<Activity, bool> condition, string validationMessage)
            where TActivity : Activity
        {
            var element = new DelegateInArgument<TActivity>();
            DelegateInArgument<ValidationContext> context = new DelegateInArgument<ValidationContext>();
            Variable<bool> result = new Variable<bool>();
            DelegateInArgument<Activity> parent = new DelegateInArgument<Activity>();

            return new Constraint<TActivity>
            {
                Body = new ActivityAction<TActivity, ValidationContext>()
                {
                    Argument1 = element,
                    Argument2 = context,
                    Handler = new Sequence()
                    {
                        Variables =
                        {
                            result
                        },
                        Activities =
                        {
                            new  ForEach<Activity>()
                            {
                                Values = new GetParentChain
                                {
                                    ValidationContext = context
                                },
                                Body = new ActivityAction<Activity>()
                                {
                                    Argument = parent,
                                    Handler = new If()
                                    {
                                        Condition = new InArgument<bool>(ctx => condition(parent.Get(ctx))),
                                        Then = new Assign<bool>
                                        {
                                            Value = true,
                                            To = result
                                        }
                                    }
                                }
                            },
                            new AssertValidation()
                            {
                                Assertion = new InArgument<bool>(result),
                                Message = new InArgument<string> (validationMessage),
                            }
                        }
                    }
                }
            };
        }
    }
}