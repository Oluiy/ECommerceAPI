using EcommerceAPI.Cache;
using ECommerceAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EcommerceAPI.Services;
using Scalar.AspNetCore;


namespace EcommerceAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var config = builder.Configuration;
            var keyBytes = Encoding.UTF8.GetBytes(config["Jwt:Key"]);
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
            builder.Services.AddScoped<ProductCache>();
            
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // This will use the property names as defined in the C# model
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddAutoMapper(typeof(Program).Assembly);
            //Configure the automapper
            
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false; // set true in production with HTTPS
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = config["Jwt:Issuer"],

                        ValidateAudience = true,
                        ValidAudience = config["Jwt:Audience"],

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });
            // builder.Services.AddOpenApi();
            // builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            // Configure EF Core with SQL Server
            builder.Services.AddDbContext<ECommerceDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddSingleton<TokenService>();
            
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
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