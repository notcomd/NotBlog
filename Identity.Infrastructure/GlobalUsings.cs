global using System.Diagnostics;

global using EmailSendServer;

global using Identity.Domain.AggregatesModel.Author2Aggregate;
global using Identity.Domain.AggregatesModel.ClientAggregate;
global using Identity.Domain.AggregatesModel.RoleAggregate;
global using Identity.Domain.AggregatesModel.UserAggregate;
global using Identity.Domain.Entities;
global using Identity.Domain.Events;
global using Identity.Domain.INotDateTime;
global using Identity.Domain.IRepository;
global using Identity.Domain.Option;
global using Identity.Domain.SeedWork;
global using Identity.Infrastructure.Configuration;
global using Identity.Infrastructure.EntityFramework;
global using Identity.Infrastructure.Repository;

global using MailKit.Security;

global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.EntityFrameworkCore.Storage;
global using Microsoft.Extensions.Caching.Distributed;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using MimeKit;

global using Notcomd.Token.JWT;

global using NotMediator;