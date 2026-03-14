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

    /// <summary>
    /// Pre-built container image URI (e.g. from ECR). When set, CDK skips the local Docker build.
    /// Omit for local development — CDK will build from the Dockerfile via FromAsset.
    /// </summary>
    public string? ImageUri { get; init; }
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
                // Use a pre-built ECR image when available (CI path — avoids rebuilding during cdk deploy).
                // Fall back to FromAsset for local development where no pre-built image exists.
                Image = string.IsNullOrEmpty(props.ImageUri)
                    ? ContainerImage.FromAsset("..", new AssetImageProps
                    {
                        File = "src/RequiemNexus.Web/Dockerfile",
                        Exclude = new[] { "**/bin", "**/obj", "**/node_modules", ".git", "infra/cdk.out" }
                    })
                    : ContainerImage.FromRegistry(props.ImageUri),
                ContainerPort = 8080,
                // Plain (non-sensitive) configuration — embedded in CloudFormation as-is.
                Environment = new Dictionary<string, string>
                {
                    { "ASPNETCORE_ENVIRONMENT", "Production" },
                    { "ConnectionStrings__DefaultConnection", $"Host={props.PostgresDatabase.DbInstanceEndpointAddress};Database=requiemnexus;Username=postgres;SSL Mode=Require;" },
                    { "Redis__Configuration", $"{props.RedisCluster.AttrPrimaryEndPointAddress}:{props.RedisCluster.AttrPrimaryEndPointPort}" }
                },

                // Secrets — ECS fetches these from Secrets Manager at container startup.
                // The value is injected as an environment variable and never stored in the CloudFormation template.
                // The CDK automatically grants the task execution role the required secretsmanager:GetSecretValue permission.
                Secrets = new Dictionary<string, Secret>
                {
                    { "DB__Password", Secret.FromSecretsManager(props.PostgresDatabase.Secret!, "password") }
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

        // Circuit breaker: if new tasks fail to start, ECS stops retrying and rolls back immediately
        // instead of looping for up to 3 hours (CloudFormation's default stabilization timeout).
        // Uses the L1 escape hatch because ApplicationLoadBalancedFargateService does not expose
        // DeploymentCircuitBreaker directly in its props.
        if (FargateService.Service.Node.DefaultChild is CfnService cfnService)
        {
            cfnService.DeploymentConfiguration = new CfnService.DeploymentConfigurationProperty
            {
                DeploymentCircuitBreaker = new CfnService.DeploymentCircuitBreakerProperty
                {
                    Enable = true,
                    Rollback = true
                }
            };
        }

        // Faster health checks: 2 passes × 10s = 20s to declare healthy vs the ALB default of 5 × 30s = 150s.
        // UnhealthyThresholdCount stays at 2 (default) — fail fast if the container is broken.
        FargateService.TargetGroup.ConfigureHealthCheck(new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
        {
            Path = "/health",
            Interval = Duration.Seconds(10),
            Timeout = Duration.Seconds(5),
            HealthyHttpCodes = "200",
            HealthyThresholdCount = 2
        });

        // Export the Load Balancer URL for use in smoke tests and documentation.
        _ = new CfnOutput(this, "LoadBalancerUrl", new CfnOutputProps
        {
            Value = $"http://{FargateService.LoadBalancer.LoadBalancerDnsName}",
            Description = "The URL of the Fargate application Load Balancer",
            ExportName = "RequiemNexus-LoadBalancerUrl"
        });
    }
}
