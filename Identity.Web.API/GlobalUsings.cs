// global using 指令

global using System.ComponentModel.DataAnnotations;
global using System.Reflection;
global using CommonsInitializer;
global using Identity.Domain.Dto.Request;
global using Identity.Domain.IRepository;
global using Identity.Domain.IService;
global using Identity.Domain.Options;
global using Identity.Domain.Entities.UserAggregate;
global using Microsoft.EntityFrameworkCore;
global using Notcomd.Evenbus;
global using Notcomd.Evenbus.Extension;
global using Notcomd.NotEmail.Core;
global using Notcomd.Token.JWT.Security;
global using NotMediator;
global using Scalar.AspNetCore;
global using CacheMemory.Extensions;
global using DomainInfrastructure;
global using Identity.Infrastructure;
global using Identity.Infrastructure.Services;
global using Identity.Web.API.APIs;
global using Microsoft.Extensions.Http.Resilience;
global using NotBlog.ServiceDefaults;
global using Notcomd.NotEmail.Extensions;
global using Polly;
global using RabbitMQ.Client;