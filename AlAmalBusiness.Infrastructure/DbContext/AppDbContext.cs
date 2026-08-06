using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AlAmalBusiness.DbContext.Infrastructure;

public class AppDbContext:IdentityDbContext<User>
{
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
}