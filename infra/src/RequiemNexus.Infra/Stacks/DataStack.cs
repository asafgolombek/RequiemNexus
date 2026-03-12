using System.Linq;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ElastiCache;
using Amazon.CDK.AWS.RDS;
using Constructs;

namespace RequiemNexus.Infra.Stacks;

public class DataStackProps : StackProps
{
    public required IVpc Vpc { get; init; }
}

public class DataStack : Stack
{
    public IDatabaseInstance PostgresDatabase { get; }
    public CfnReplicationGroup RedisCluster { get; }
    public ISecurityGroup DbSecurityGroup { get; }
    public ISecurityGroup RedisSecurityGroup { get; }

    public DataStack(Construct scope, string id, DataStackProps props) : base(scope, id, props)
    {
        DbSecurityGroup = new SecurityGroup(this, "DbSecurityGroup", new SecurityGroupProps
        {
            Vpc = props.Vpc,
            Description = "Allow access to PostgreSQL",
            AllowAllOutbound = true
        });

        PostgresDatabase = new DatabaseInstance(this, "PostgresRDS", new DatabaseInstanceProps
        {
            Engine = DatabaseInstanceEngine.Postgres(new PostgresInstanceEngineProps { Version = PostgresEngineVersion.VER_16 }),
            InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(InstanceClass.BURSTABLE4_GRAVITON, InstanceSize.MICRO),
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED },
            SecurityGroups = new[] { DbSecurityGroup },
            DatabaseName = "requiemnexus",
            MultiAz = false,
            AllocatedStorage = 20,
            StorageType = StorageType.GP3,
            BackupRetention = Duration.Days(1), // 24-hr SLA
            RemovalPolicy = RemovalPolicy.SNAPSHOT,
            DeleteAutomatedBackups = false
        });

        RedisSecurityGroup = new SecurityGroup(this, "RedisSecurityGroup", new SecurityGroupProps
        {
            Vpc = props.Vpc,
            Description = "Allow access to Redis",
            AllowAllOutbound = true
        });

        var redisSubnetGroup = new CfnSubnetGroup(this, "RedisSubnetGroup", new CfnSubnetGroupProps
        {
            Description = "Subnet group for Redis",
            SubnetIds = props.Vpc.IsolatedSubnets.Select(s => s.SubnetId).ToArray()
        });

        RedisCluster = new CfnReplicationGroup(this, "RedisCluster", new CfnReplicationGroupProps
        {
            ReplicationGroupDescription = "Redis cluster for Requiem Nexus",
            Engine = "redis",
            CacheNodeType = "cache.t4g.micro",
            NumCacheClusters = 2,
            AutomaticFailoverEnabled = true,
            MultiAzEnabled = true,
            CacheSubnetGroupName = redisSubnetGroup.Ref,
            SecurityGroupIds = new[] { RedisSecurityGroup.SecurityGroupId }
        });
    }
}
