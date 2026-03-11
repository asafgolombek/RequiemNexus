using Amazon.CDK;
using RequiemNexus.Infra.Stacks;

namespace RequiemNexus.Infra;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var app = new App();

        var identityStack = new IdentityStack(app, "RequiemNexus-Identity-Stack", new StackProps
        {
            Description = "Requiem Nexus Identity Stack (OIDC, IAM Roles)"
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
            Vpc = networkConfig.Vpc
        });

        var computeConfig = new ComputeStack(app, "RequiemNexus-Compute-Stack", new ComputeStackProps
        {
            Description = "Requiem Nexus Compute Stack (ECS, ALB)",
            Vpc = networkConfig.Vpc
        });

        app.Synth();
    }
}
