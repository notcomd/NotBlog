using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NotMediator;

namespace Message.Infrastructure.EntityFramework;

public class MessageDbContext(DbContextOptions<MessageDbContext> options,INotMediator notMediator) : DbContext(options)
{
    
}