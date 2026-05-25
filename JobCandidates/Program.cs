using JobCandidates;
using JobCandidates.Repository;
using JobCandidates.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

namespace JobCandidates
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var jwtSection = builder.Configuration.GetSection("Jwt");
            var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
            var jwtIssuer = jwtSection["Issuer"] ?? "JobCandidatesApi";
            var jwtAudience = jwtSection["Audience"] ?? "JobCandidatesApiClient";

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            const string CorsPolicyName = "AllowAll";

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName, policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "JobCandidates API",
                    Version = "v1"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: Bearer {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.IncludeErrorDetails = true;
                    options.MapInboundClaims = false;
                    options.SaveToken = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.FromMinutes(2),

                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

                        NameClaimType = "email",
                        RoleClaimType = "role"
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            Console.WriteLine("======================================");
                            Console.WriteLine("[JWT] OnMessageReceived");
                            Console.WriteLine($"[JWT] Authorization Header: {context.Request.Headers["Authorization"]}");
                            Console.WriteLine("======================================");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            Console.WriteLine("======================================");
                            Console.WriteLine("[JWT] Token validated successfully.");
                            Console.WriteLine("[JWT] Claims:");
                            foreach (var claim in context.Principal?.Claims ?? Enumerable.Empty<System.Security.Claims.Claim>())
                            {
                                Console.WriteLine($"[JWT CLAIM] {claim.Type} = {claim.Value}");
                            }
                            Console.WriteLine("======================================");
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine("======================================");
                            Console.WriteLine("[JWT] Authentication FAILED");
                            Console.WriteLine($"[JWT] Exception Type: {context.Exception.GetType().FullName}");
                            Console.WriteLine($"[JWT] Message: {context.Exception.Message}");
                            Console.WriteLine($"[JWT] Full Exception: {context.Exception}");
                            Console.WriteLine("======================================");
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            Console.WriteLine("======================================");
                            Console.WriteLine("[JWT] Challenge triggered");
                            Console.WriteLine($"[JWT] Error: {context.Error}");
                            Console.WriteLine($"[JWT] Error Description: {context.ErrorDescription}");
                            Console.WriteLine("======================================");
                            return Task.CompletedTask;
                        },
                        OnForbidden = context =>
                        {
                            Console.WriteLine("======================================");
                            Console.WriteLine("[JWT] Forbidden triggered");
                            Console.WriteLine("======================================");
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            builder.Services.AddScoped<ICandidateRepository, CandidateRepository>();
            builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
            builder.Services.AddScoped<IInterviewRepository, InterviewRepository>();
            builder.Services.AddScoped<IRankingService, RankingService>();
            builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
            builder.Services.AddScoped<IJobRepository, JobRepository>();
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();

            builder.Services.AddAutoMapper(typeof(Program));

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseCors(CorsPolicyName);

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}