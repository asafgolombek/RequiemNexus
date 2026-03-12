using Amazon.CDK;
using Amazon.CDK.AWS.Budgets;
using Constructs;

namespace RequiemNexus.Infra.Stacks;

public class BillingStack : Stack
{
    public BillingStack(Construct scope, string id, StackProps props) : base(scope, id, props)
    {
        var budget = new CfnBudget(this, "MonthlyBudget", new CfnBudgetProps
        {
            Budget = new CfnBudget.BudgetDataProperty
            {
                BudgetLimit = new CfnBudget.SpendProperty
                {
                    Amount = 20, // $20/month threshold
                    Unit = "USD"
                },
                BudgetName = "RequiemNexus-Monthly-Budget",
                BudgetType = "COST",
                TimeUnit = "MONTHLY"
            },
            NotificationsWithSubscribers = new[]
            {
                new CfnBudget.NotificationWithSubscribersProperty
                {
                    Notification = new CfnBudget.NotificationProperty
                    {
                        ComparisonOperator = "GREATER_THAN",
                        NotificationType = "ACTUAL",
                        Threshold = 80, // Send alert at 80%
                        ThresholdType = "PERCENTAGE"
                    },
                    Subscribers = new[]
                    {
                        new CfnBudget.SubscriberProperty
                        {
                            Address = "admin@example.com", // Placeholder to be replaced via Secrets or Config
                            SubscriptionType = "EMAIL"
                        }
                    }
                }
            }
        });
    }
}
