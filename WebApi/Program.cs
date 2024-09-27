using Microsoft.AspNetCore.Identity;
using WebApi.Identity;
using WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IDatabase, Database>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<IUserStore<WebApi.Identity.IdentityUser>, UserStore>();
builder.Services.AddSingleton<IRoleStore<WebApi.Identity.IdentityRole>, RoleStore>();
builder.Services.AddIdentity<WebApi.Identity.IdentityUser, WebApi.Identity.IdentityRole>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
