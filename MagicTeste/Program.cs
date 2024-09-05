using Microsoft.EntityFrameworkCore;
using System.Net;
using MagicTeste.Context;



var builder = WebApplication.CreateBuilder(args);
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;


builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DatabaseConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
