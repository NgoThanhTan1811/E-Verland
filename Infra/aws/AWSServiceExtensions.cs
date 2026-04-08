using Amazon.S3;
using Amazon.SQS;
using Amazon.SimpleNotificationService;
using Amazon.EventBridge;
using Amazon.CloudWatch;
using Infra.AWS.S3;
using Infra.AWS.SQS;
using Infra.AWS.SNS;
using Infra.AWS.EventBridge;
using Infra.AWS.CloudWatch;
using Infra.AWS.OpenSearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infra.AWS;

/// <summary>
/// Extension methods for registering AWS services
/// </summary>
public static class AWSServiceExtensions
{
    public static IServiceCollection AddAWSInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));
        services.Configure<SQSOptions>(configuration.GetSection(SQSOptions.SectionName));
        services.Configure<SNSOptions>(configuration.GetSection(SNSOptions.SectionName));
        services.Configure<EventBridgeOptions>(configuration.GetSection(EventBridgeOptions.SectionName));
        services.Configure<CloudWatchOptions>(configuration.GetSection(CloudWatchOptions.SectionName));
        services.Configure<OpenSearchOptions>(configuration.GetSection(OpenSearchOptions.SectionName));

        // Register AWS SDK clients
        services.AddAWSService<IAmazonS3>();
        services.AddAWSService<IAmazonSQS>();
        services.AddAWSService<IAmazonSimpleNotificationService>();
        services.AddAWSService<IAmazonEventBridge>();
        services.AddAWSService<IAmazonCloudWatch>();

        // Register custom services
        services.AddSingleton<IS3StorageService, S3StorageService>();
        services.AddSingleton<ISQSService, SQSService>();
        services.AddSingleton<ISNSService, SNSService>();
        services.AddSingleton<IEventBridgeService, EventBridgeService>();
        services.AddSingleton<ICloudWatchService, CloudWatchService>();
        services.AddSingleton<IOpenSearchService, OpenSearchService>();

        return services;
    }
}
