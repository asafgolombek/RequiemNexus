using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Constructs;

namespace RequiemNexus.Infra.Stacks;

public class IdentityStack : Stack
{
    public Role GitHubActionsRole { get; }

    public IdentityStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        // 1. Handle OIDC Provider for GitHub Actions
        // Using FromOpenIdConnectProviderArn to avoid "EntityAlreadyExists" if it was created manually or by another stack.
        var providerArn = $"arn:aws:iam::216938126042:oidc-provider/token.actions.githubusercontent.com";
        var provider = OpenIdConnectProvider.FromOpenIdConnectProviderArn(this, "GitHubOidcProvider", providerArn);

        // 2. Define the IAM Role for GitHub Actions
        GitHubActionsRole = new Role(this, "GitHubActionsRole", new RoleProps
        {
            AssumedBy = new OpenIdConnectPrincipal(provider, new System.Collections.Generic.Dictionary<string, object>
            {
                { "StringLike", new System.Collections.Generic.Dictionary<string, string> { { "token.actions.githubusercontent.com:sub", "repo:asafgolombek/RequiemNexus:*" } } },
                { "StringEquals", new System.Collections.Generic.Dictionary<string, string> { { "token.actions.githubusercontent.com:aud", "sts.amazonaws.com" } } }
            }),
            RoleName = "GitHubActionsServiceRole",
            Description = "Role assumed by GitHub Actions via OIDC"
        });

        // 3. Grant permissions to the role using an Inline Policy (Long-term fix for 5-version limit)
        GitHubActionsRole.AttachInlinePolicy(new Policy(this, "GitHubActionsManagementPolicy", new PolicyProps
        {
            Statements = [
                new PolicyStatement(new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions = ["*"],
                    Resources = ["*"]
                })
            ]
        }));

        // Output the Role ARN
        new CfnOutput(this, "GitHubActionsRoleArn", new CfnOutputProps
        {
            Value = GitHubActionsRole.RoleArn,
            ExportName = "GitHubActionsRoleArn"
        });
    }
}
