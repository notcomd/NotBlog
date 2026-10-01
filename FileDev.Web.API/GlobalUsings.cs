global using System.Reflection;

global using CacheMemory.Extensions;

global using Commons.Web;

global using FileDev.Domain.Dto.Request;
global using FileDev.Domain.Entities;
global using FileDev.Domain.Enum;
global using FileDev.Domain.Exception;
global using FileDev.Domain.IServices;
global using FileDev.Domain.Options;
global using FileDev.Infrastructure.EntityFramework;
global using FileDev.Infrastructure.Idempotent;
global using FileDev.Infrastructure.Service;
global using FileDev.Web.API.APIs;
global using FileDev.Web.API.Application.Commands;
global using FileDev.Web.API.Application.Queries;
global using FileDev.Web.API.DependencyInjection;
global using FileDev.Web.API.Helpers;
global using FileDev.Web.API.Middleware;

global using Microsoft.AspNetCore.Http.Features;
global using Microsoft.Extensions.Options;

global using NotBlog.ServiceDefaults;

global using Notcomd.EventBus.Extension;

global using NotMediator.Abstractions;
global using NotMediator.Mediator;

global using Scalar.AspNetCore;
global using Commons.Extensions;
global using FileDev.Web.API.ActionFilter.Behaviors;
global using FileDev.Web.API.Background;
global using Notcomd.Token.JWT.Extensions;
global using Microsoft.EntityFrameworkCore;
global using OpenTelemetry.Resources;
global using FileDev.Web.API.Resources;
global using RabbitMQ.Client;
global using FileDev.Web.API.Dto.Response;
global using FileDev.Web.API.Dto.Request;