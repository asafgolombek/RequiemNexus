using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.ElastiCache;
using Amazon.CDK.AWS.RDS;
using Constructs;

namespace RequiemNexus.Infra.Stacks;

public class ComputeStackProps : StackProps
{
    public required IVpc Vpc { get; init; }
    public required DatabaseInstance PostgresDatabase { get; init; }
    public required ISecurityGroup DbSecurityGroup { get; init; }
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
                    File = "src/RequiemNexus.Web/Dockerfile",
                    Exclude = new[] { "**/bin", "**/obj", "**/node_modules", ".git", "infra/cdk.out" }
                }),
                ContainerPort = 8080,
                Environment = new Dictionary<string, string>
                {
                    { "ASPNETCORE_ENVIRONMENT", "Production" },
                    { "ConnectionStrings__DefaultConnection", $"Host={props.PostgresDatabase.DbInstanceEndpointAddress};Database=requiemnexus;Username=postgres;Password={props.PostgresDatabase.Secret!.SecretValueFromJson("password")};SSL Mode=Require;" },
                    { "Redis__Configuration", $"{props.RedisCluster.AttrPrimaryEndPointAddress}:{props.RedisCluster.AttrPrimaryEndPointPort}" }
                }
            },
            PublicLoadBalancer = true,
            AssignPublicIp = false // Running in private subnets with NAT Gateway
        });

        // Allow Fargate service to connect to RDS and Redis.
        // CfnSecurityGroupIngress is used intentionally here: calling AllowDefaultPortFrom or AddIngressRule
        // on security groups owned by DataStack would cause CDK to export the Fargate SG ID into DataStack,
        // creating a circular cross-stack dependency (DataStack ↔ ComputeStack).
        // By declaring CfnSecurityGroupIngress resources inside ComputeStack, the ingress rules reference
        // DataStack's SG IDs (ComputeStack → DataStack, already valid) without DataStack ever referencing
        // ComputeStack, breaking the cycle.
        string fargateSecurityGroupId = FargateService.Service.Connections.SecurityGroups[0].SecurityGroupId;

        _ = new Amazon.CDK.AWS.EC2.CfnSecurityGroupIngress(this, "DbIngressFromFargate", new Amazon.CDK.AWS.EC2.CfnSecurityGroupIngressProps
        {
            GroupId = props.DbSecurityGroup.SecurityGroupId,
            IpProtocol = "tcp",
            FromPort = 5432,
            ToPort = 5432,
            SourceSecurityGroupId = fargateSecurityGroupId,
            Description = "Allow PostgreSQL access from Fargate"
        });

        _ = new Amazon.CDK.AWS.EC2.CfnSecurityGroupIngress(this, "RedisIngressFromFargate", new Amazon.CDK.AWS.EC2.CfnSecurityGroupIngressProps
        {
            GroupId = props.RedisSecurityGroup.SecurityGroupId,
            IpProtocol = "tcp",
            FromPort = 6379,
            ToPort = 6379,
            SourceSecurityGroupId = fargateSecurityGroupId,
            Description = "Allow Redis access from Fargate"
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
