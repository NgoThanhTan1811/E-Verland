using Modules.User;

var builder = WebApplication.CreateBuilder(args);





builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddTransient<ApiExceptionMiddleware>();




builder.Services.AddUserModule(builder.Configuration);


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ApiExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();
