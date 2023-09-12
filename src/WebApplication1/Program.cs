using WebApplication1;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.ConfigureLaters();


//builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IStartupFilter, SetupLaters>();
builder.Services.AddTransient<IStartupFilter, SetupLaters2>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//app.UseLaters();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/dave3", context => context.Response.WriteAsync("Hello, world!"));

app.Run();