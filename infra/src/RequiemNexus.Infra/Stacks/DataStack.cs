using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Constructs;

namespace RequiemNexus.Infra.Stacks;

public class DataStackProps : StackProps
{
    public required IVpc Vpc { get; init; }
}

public class DataStack : Stack
{
    public DataStack(Construct scope, string id, DataStackProps props) : base(scope, id, props)
    {
        // TODO: Provision PostgreSQL RDS 
        // TODO: Provision ElastiCache (Redis)
    }
}
