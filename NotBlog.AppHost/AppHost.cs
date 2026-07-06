using Projects;

var builder = DistributedApplication.CreateBuilder(args);

#if DEBUG

var identity = builder
    .AddConnectionString("IdentityPostgres");
var notfile = builder
    .AddConnectionString("NotFilePostgres");
var message = builder
    .AddConnectionString("MessagePostgres");
var video = builder
    .AddConnectionString("VideoPostgres");
var mark = builder
    .AddConnectionString("MarkDownPostgres");
var redis = builder
    .AddConnectionString("Redis");

// var rabbitmq = builder
//     .AddConnectionString("RabbitMQ");
# else
var postgres = builder.AddPostgres("PostgresSQL")
    .WithDataVolume();
var post = postgres.AddDatabase("MyData");
var redis = builder.AddRedis("Redis");

builder.AddProject<Projects.FileDev_Web_API>("filedev-web-api")
    .WithReference(post) ;

builder.AddProject<Projects.Identity_Web_API>("identity-web-api")
    .WithReference(post)
    .WithReference(redis);

builder.AddProject<Projects.Markdown_Web_API>("markdown-web-api")
    .WithReference(post)
    .WithReference(redis);

builder.AddProject<Projects.Message_Web_API>("message-web-api")
    .WithReference(post)
    .WithReference(redis);

builder.AddProject<Projects.Video_Web_API>("video-web-api")
    .WithReference(post)
    .WithReference(redis);
#endif
builder.AddProject<FileDev_Web_API>("filedev-web-api")
    .WithReference(notfile);


builder.AddProject<NotBlog_Yarp>("notblog-yarp-gateway")
    .WithReference(notfile);

builder.AddProject<Identity_Web_API>("identity-web-api")
    .WithReference(identity)
    .WithReference(redis);

builder.AddProject<Markdown_Web_API>("markdown-web-api")
    .WithReference(mark)
    .WithReference(redis);

builder.AddProject<Message_Web_API>("message-web-api")
    .WithReference(message)
    .WithReference(redis);

builder.AddProject<Video_Web_API>("video-web-api")
    .WithReference(video)
    .WithReference(redis);


builder.Build().Run();