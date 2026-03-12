using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Constructs;

namespace RequiemNexus.Infra.Stacks;

public class ComputeStackProps : StackProps
{
    public required IVpc Vpc { get; init; }
}

public class ComputeStack : Stack
{
    public ApplicationLoadBalancedFargateService FargateService { get; }

    public ComputeStack(Construct scope, string id, ComputeStackProps props) : base(scope, id, props)
    {
        var cluster = new Cluster(this, "RequiemNexusCluster", new ClusterProps
        {
            Vpc = props.Vpc,
            ClusterName = "RequiemNexusCluster"
        });

        FargateService = new ApplicationLoadBalancedFargateService(this, "RequiemNexusFargateService", new ApplicationLoadBalancedFargateServiceProps
        {
            Cluster = cluster,
            MemoryLimitMiB = 1024,
            Cpu = 512,
            TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
            {
                Image = ContainerImage.FromRegistry("mcr.microsoft.com/dotnet/aspnet:10.0"),
                ContainerPort = 8080,
                Environment = new Dictionary<string, string>
                {
                    { "ASPNETCORE_ENVIRONMENT", "Production" }
                }
            },
            PublicLoadBalancer = true
        });

        FargateService.TargetGroup.ConfigureHealthCheck(new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
        {
            Path = "/health",
            Interval = Duration.Seconds(30),
            Timeout = Duration.Seconds(5),
            HealthyHttpCodes = "200"
        });
    }
}
