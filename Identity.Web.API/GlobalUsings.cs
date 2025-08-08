// global using 指令

global using System.ComponentModel.DataAnnotations;
global using System.Reflection;

global using CommonsInitializer;

global using DomainCommonst;

global using EmailSendServer;

global using Identity.Domain.AggregatesModel.RoleAggregate;
global using Identity.Domain.AggregatesModel.UserAggregate;
global using Identity.Domain.Entities;
global using Identity.Domain.Events;
global using Identity.Domain.INotDateTime;
global using Identity.Domain.IRepository;
global using Identity.Infrastructure.NotMemoryCache;
global using Identity.Infrastructure.RequestManager;
global using Identity.Web.API.ActionFilter;

global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Controllers;
global using Microsoft.AspNetCore.Mvc.Filters;
global using Microsoft.EntityFrameworkCore;

global using Notcomd.Evenbus;
global using Notcomd.Token.JWT;

global using NotMediator;

global using Scalar.AspNetCore;