using EcommerceAPI.Cache;
using ECommerceAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            //         public static class StringExtensions
            //         {
            //             public static string ToTitleCase(this string input)
            //             {
            //                 return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input);
            //             }
            //         }
            //
            // // Usage:
            //         string name = "hello world";
            //         Console.WriteLine(name.ToTitleCase()); // Output: Hello World

            // builder.Services.AddControllers(options =>
            // {
            //     // Remove JSON formatter
            //     options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonOutputFormatter>();
            // }).AddXmlSerializerFormatters(); //Adding XML Formatter

            // Add services to the container.
            //To register Dependency(In using DI<Dependency Injection>)
            // builder.Services.AddScoped<IPaymentService, PaymentService>()
            // RequestDelegate
            
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<OrderCache>();
            builder.Services.AddScoped<CustomerCache>();
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // This will use the property names as defined in the C# model
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddAutoMapper(typeof(Program).Assembly);
            //Configure the automapper
            
            
            // builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            // Configure EF Core with SQL Server
            builder.Services.AddDbContext<ECommerceDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            // app.UseRouting();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}