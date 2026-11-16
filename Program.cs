using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ContextDb>(
    option => option.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")),
    ServiceLifetime.Scoped
);
builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            "AllowUnity",
            policy =>
            {
                policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod();
            }
        );
    });
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowUnity");
app.UseAuthorization();
app.MapControllers();
app.Run();
