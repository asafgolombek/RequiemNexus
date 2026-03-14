using Amazon.CDK;
using RequiemNexus.Infra.Stacks;

namespace RequiemNexus.Infra;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var app = new App();

        var envName = app.Node.TryGetContext("env") as string ?? "dev";

        var identityStack = new IdentityStack(app, $"RequiemNexus-Identity-{envName}", new StackProps
        {
            Description = $"Requiem Nexus Identity Stack ({envName}) (OIDC, IAM Roles)",
            Env = new Amazon.CDK.Environment { Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") ?? "us-east-1" }
        });

        var networkConfig = new NetworkStack(app, "RequiemNexus-Network-Stack", new StackProps
        {
            Description = "Requiem Nexus Network Stack (VPC, Subnets)",
            // Environment can be set later if needed
            // Env = new Amazon.CDK.Environment { Account = "...", Region = "..." }
        });

        var dataConfig = new DataStack(app, "RequiemNexus-Data-Stack", new DataStackProps
        {
            Description = "Requiem Nexus Data Stack (RDS, ElastiCache)",
            Vpc = networkConfig.Vpc,
            IsProductionGrade = envName == "production"
        });

        // imageUri is injected by CI via: cdk deploy --context imageUri=<ecr-uri>
        // When absent (local dev), ComputeStack falls back to ContainerImage.FromAsset.
        string? imageUri = app.Node.TryGetContext("imageUri") as string;

        var computeConfig = new ComputeStack(app, "RequiemNexus-Compute-Stack", new ComputeStackProps
        {
            Description = "Requiem Nexus Compute Stack (ECS, ALB)",
            Vpc = networkConfig.Vpc,
            PostgresDatabase = dataConfig.PostgresDatabase,
            DbSecurityGroup = dataConfig.DbSecurityGroup,
            RedisCluster = dataConfig.RedisCluster,
            RedisSecurityGroup = dataConfig.RedisSecurityGroup,
            ImageUri = imageUri
        });

        var staticAssetConfig = new StaticAssetStack(app, $"RequiemNexus-Static-{envName}", new StackProps
        {
            Description = $"Requiem Nexus Static Asset Stack ({envName}) (S3, CloudFront)",
            Env = new Amazon.CDK.Environment { Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") ?? "us-east-1" }
        });

        var billingStack = new BillingStack(app, $"RequiemNexus-Billing-{envName}", new StackProps
        {
            Description = $"Requiem Nexus Billing Stack ({envName})",
            Env = new Amazon.CDK.Environment { Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") ?? "us-east-1" }
        });

        app.Synth();
    }
}
