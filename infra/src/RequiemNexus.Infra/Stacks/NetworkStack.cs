using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace RequiemNexus.Infra.Stacks;

public class NetworkStack : Stack
{
    public IVpc Vpc { get; }

    public NetworkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
        // 1. Create the VPC for the environment
        // Explicitly set maxAZs to 2 for high availability without excessive cost
        Vpc = new Vpc(this, "RequiemNexusVpc", new VpcProps
        {
            MaxAzs = 2,
            NatGateways = 0, // Set to 1 later if private subnets need internet access.
            SubnetConfiguration = new[]
            {
                new SubnetConfiguration
                {
                    Name = "Public",
                    SubnetType = SubnetType.PUBLIC
                },
                new SubnetConfiguration
                {
                    Name = "Isolated",
                    SubnetType = SubnetType.PRIVATE_ISOLATED
                }
            }
        });
    }
}
