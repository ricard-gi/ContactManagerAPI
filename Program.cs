using Microsoft.EntityFrameworkCore;
using contacts.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Identity;



public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configura CORS per permetre qualsevol origen, capçalera i mètode
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });


        // Configura la connexió a la base de dades
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Configura l'autorització amb la política "AdminOnly"
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireClaim("IsAdmin", "True"));
        });

        builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();


        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
            };
        });

        builder.Services.AddControllers();

        var app = builder.Build();

        // Reinicialitza la base de dades i afegeix usuaris i contactes de prova en desenvolupament
        if (app.Environment.IsDevelopment())
        {
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

                // Reinicialitza la base de dades i aplica les migracions
                db.Database.EnsureDeleted();
                db.Database.Migrate();

                // Usuari administrador de prova
                if (!db.Users.Any(u => u.Username == "admin"))
                {
                    var adminUser = new User
                    {
                        Username = "admin",
                        IsAdmin = true,
                    };
                    adminUser.Password = passwordHasher.HashPassword(adminUser, "1234");
                    db.Users.Add(adminUser);
                }

                // Usuari regular de prova
                if (!db.Users.Any(u => u.Username == "user1"))
                {
                    var user1 = new User
                    {
                        Username = "user1",
                        IsAdmin = false,
                    };
                    user1.Password = passwordHasher.HashPassword(user1, "1234");
                    db.Users.Add(user1);
                    db.SaveChanges(); // Guardem per assegurar-nos que l'usuari té un ID abans de crear contactes

                    // Afegeix contactes de prova per a `user1`
                    var contacte1 = new Contact
                    {
                        Name = "contacte1",
                        PhoneNumber = "123456789",
                        UserId = user1.Id, // Assigna l'ID de `user1`
                        PhotoFileName = ""
                    };
                    var contacte2 = new Contact
                    {
                        Name = "contacte2",
                        PhoneNumber = "987654321",
                        UserId = user1.Id, // Assigna l'ID de `user1`
                        PhotoFileName = ""

                    };

                    db.Contacts.AddRange(contacte1, contacte2);
                }

                db.SaveChanges(); // Guarda tots els canvis a la base de dades
            }
        }



        app.UseCors("AllowAllOrigins");

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), "uploads")),
            RequestPath = "/uploads"
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
