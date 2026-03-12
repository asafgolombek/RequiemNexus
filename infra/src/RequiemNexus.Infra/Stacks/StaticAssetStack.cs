using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Constructs;

namespace RequiemNexus.Infra.Stacks;

public class StaticAssetStack : Stack
{
    public Bucket StaticBucket { get; }
    public Distribution CdnDistribution { get; }

    public StaticAssetStack(Construct scope, string id, StackProps props) : base(scope, id, props)
    {
        StaticBucket = new Bucket(this, "RequiemNexusStaticBucket", new BucketProps
        {
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true
        });

        // OAI is automatically created by CDK for S3Origin
        CdnDistribution = new Distribution(this, "RequiemNexusCdn", new DistributionProps
        {
            DefaultBehavior = new BehaviorOptions
            {
                Origin = new S3Origin(StaticBucket),
                ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                CachePolicy = CachePolicy.CACHING_OPTIMIZED
            },
            DefaultRootObject = "index.html"
        });
    }
}
