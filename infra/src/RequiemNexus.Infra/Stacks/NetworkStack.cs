using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace RequiemNexus.Infra.Stacks;

public class NetworkStack : Stack
{
    public IVpc Vpc { get; }

    public NetworkStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        // 1. Create the VPC for the environment
        // Explicitly set maxAZs to 2 for high availability without excessive cost
        Vpc = new Vpc(this, "RequiemNexusVpc", new VpcProps
        {
            MaxAzs = 2,
            NatGateways = 1, // Enable 1 NAT Gateway for private subnet internet access (image pulls, etc.)
            SubnetConfiguration = new[]
            {
                new SubnetConfiguration
                {
                    Name = "Public",
                    SubnetType = SubnetType.PUBLIC,
                    CidrMask = 24
                },
                new SubnetConfiguration
                {
                    Name = "Private",
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS, // Fargate tasks — routed through NAT Gateway
                    CidrMask = 24
                },
                new SubnetConfiguration
                {
                    Name = "Isolated",
                    SubnetType = SubnetType.PRIVATE_ISOLATED, // RDS and ElastiCache — no internet access
                    CidrMask = 28
                }
            }
        });
    }
}
