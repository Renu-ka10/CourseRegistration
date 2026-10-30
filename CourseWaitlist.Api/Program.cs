using CourseWaitlist.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<ICourseRegistrationService, CourseRegistrationService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Course Waitlist API V1");
        c.RoutePrefix = string.Empty; // Loads Swagger at root http://localhost:5000/
    });
}

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }