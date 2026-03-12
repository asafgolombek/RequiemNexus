using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.ElastiCache;
using Constructs;

namespace RequiemNexus.Infra.Stacks;

public class ComputeStackProps : StackProps
{
    public required IVpc Vpc { get; init; }
    public required IDatabaseInstance PostgresDatabase { get; init; }
    public required CfnReplicationGroup RedisCluster { get; init; }
    public required ISecurityGroup RedisSecurityGroup { get; init; }
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
                Image = ContainerImage.FromAsset("..", new AssetImageProps
                {
                    File = "src/RequiemNexus.Web/Dockerfile"
                }),
                ContainerPort = 8080,
                Environment = new Dictionary<string, string>
                {
                    { "ASPNETCORE_ENVIRONMENT", "Production" },
                    { "ConnectionStrings__DefaultConnection", $"Host={props.PostgresDatabase.DbInstanceEndpointAddress};Database=requiemnexus;Username=postgres;Password=password123" }, // Placeholder till Secret integration is fully verified
                    { "Redis__Configuration", $"{props.RedisCluster.AttrPrimaryEndPointAddress}:{props.RedisCluster.AttrPrimaryEndPointPort}" }
                }
            },
            PublicLoadBalancer = true,
            AssignPublicIp = false // Running in private subnets with NAT Gateway
        });

        // Allow Fargate service to connect to RDS
        props.PostgresDatabase.Connections.AllowDefaultPortFrom(FargateService.Service);

        // Allow Fargate service to connect to Redis
        props.RedisSecurityGroup.AddIngressRule(FargateService.Service.Connections.SecurityGroups[0], Port.Tcp(6379), "Allow Redis access from Fargate");

        FargateService.TargetGroup.ConfigureHealthCheck(new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
        {
            Path = "/health",
            Interval = Duration.Seconds(30),
            Timeout = Duration.Seconds(5),
            HealthyHttpCodes = "200"
        });
    }
}
