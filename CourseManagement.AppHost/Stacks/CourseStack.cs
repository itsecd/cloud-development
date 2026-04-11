using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SNS;
using Constructs;
using CdkTags = Amazon.CDK.Tags;

namespace CourseManagement.AppHost.Stacks;

public class CourseStack : Stack
{
    public ITopic CourseTopic { get; }
    public IBucket ContractsBucket { get; }

    public CourseStack(Construct scope, string id, IStackProps? props = null)
        : base(scope, id, props)
    {
        // SNS топик для контрактов
        CourseTopic = new Topic(this, "CourseContractsTopic", new TopicProps
        {
            TopicName = "course-contracts-topic",
            DisplayName = "Course Contracts Topic",
            Fifo = false
        });

        // Теги топика
        CdkTags.Of(CourseTopic).Add("Environment", "Development");
        CdkTags.Of(CourseTopic).Add("Project", "CourseManagement");

        // S3 бакет для хранения контрактов
        ContractsBucket = new Bucket(this, "CourseContractsBucket", new BucketProps
        {
            BucketName = "course-contracts-bucket",
            Versioned = false,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true,
            PublicReadAccess = false,
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL
        });

        // Теги бакета
        CdkTags.Of(ContractsBucket).Add("Environment", "Development");
        CdkTags.Of(ContractsBucket).Add("Project", "CourseManagement");

        // Outputs
        _ = new CfnOutput(this, "TopicArn", new CfnOutputProps
        {
            Value = CourseTopic.TopicArn,
            Description = "ARN of the SNS topic",
            ExportName = "course-topic-arn"
        });

        _ = new CfnOutput(this, "TopicName", new CfnOutputProps
        {
            Value = CourseTopic.TopicName,
            Description = "Name of the SNS topic",
            ExportName = "course-topic-name"
        });

        _ = new CfnOutput(this, "BucketName", new CfnOutputProps
        {
            Value = ContractsBucket.BucketName,
            Description = "Name of the S3 bucket",
            ExportName = "course-bucket-name"
        });
    }
}