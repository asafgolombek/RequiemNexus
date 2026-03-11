using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Constructs;

namespace RequiemNexus.Infra.Stacks;

public class IdentityStack : Stack
{
    public Role GitHubActionsRole { get; }

    public IdentityStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        // 1. Create OIDC Provider for GitHub Actions
        var provider = new OpenIdConnectProvider(this, "GitHubOidcProvider", new OpenIdConnectProviderProps
        {
            Url = "https://token.actions.githubusercontent.com",
            ClientIds = ["sts.amazonaws.com"],
            // Note: Thumbprint is automatically handled by CDK for known providers like GitHub
        });

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

        // 3. Grant permissions to the role
        // For now, granting AdministratorAccess for CDK deployments. 
        // In a production environment, this should be scoped down to the minimum required permissions.
        GitHubActionsRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AdministratorAccess"));

        // Output the Role ARN
        new CfnOutput(this, "GitHubActionsRoleArn", new CfnOutputProps
        {
            Value = GitHubActionsRole.RoleArn,
            ExportName = "GitHubActionsRoleArn"
        });
    }
}
