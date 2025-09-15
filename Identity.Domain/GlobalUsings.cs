// global using 指令

global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Security.Claims;
global using System.Text;
global using System.Threading.Tasks;
global using System.Xml.Serialization;

global using DomainCommonst;

global using Identity.Domain.AggregatesModel.Author2Aggregate;
global using Identity.Domain.AggregatesModel.RoleAggregate;
global using Identity.Domain.AggregatesModel.UserAggregate;
global using Identity.Domain.DomainEvents;
global using Identity.Domain.Entities;
global using Identity.Domain.IRepository;

global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Logging;

global using Notcomd.Token.JWT;

global using NotMediator;