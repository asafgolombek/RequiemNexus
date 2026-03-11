using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace RequiemNexus.Infra.Stacks;

public class ComputeStackProps : StackProps
{
    public required IVpc Vpc { get; init; }
}

public class ComputeStack : Stack
{
    public ComputeStack(Construct scope, string id, ComputeStackProps props) : base(scope, id, props)
    {
        // TODO: Provision ECS Cluster
        // TODO: Provision ECS Fargate Service
        // TODO: Provision ALB (Application Load Balancer)
    }
}
