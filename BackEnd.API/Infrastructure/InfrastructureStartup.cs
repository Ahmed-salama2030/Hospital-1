using System;
using AutoMapper;
using Core.Interfaces;
 using Infrastracture.Repository;
using Infrastructure.Data;
using Infrastructure.Helper;
 using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class InfrastructureStartup
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, Action<DbContextOptionsBuilder> options)
        {
            //Register DbContext
            services.AddDbContext<ApplicationDbContext>(options);

            // Register AutoMapper 
            services.AddAutoMapper(typeof(AutoMapperProfiles) /* You can add more Assembly profiles*/);
            services.AddTransient<TrailData>();
            
             services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
            return services;
        }
    }
}