using Amazon.CDK;

namespace CourseManagement.AppHost.Stacks;

public class CourseStackProps : StackProps
{
    public string BucketName { get; set; } = string.Empty;
    
    public string TopicName { get; set; } = string.Empty;
}