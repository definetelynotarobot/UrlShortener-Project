using Microsoft.EntityFrameworkCore;
using System.Security.Policy;
using UrlShortener.Models;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlite(connStr));
//builder.Services.AddDbContext<ApiDbContext>(optionsAction:options => options.UseSqlite(connStr));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/shorturl", async (UrlDto url, ApiDbContext db, HttpContext ctx) =>
{
    //validate the url
    if (!Uri.TryCreate(url.Url, UriKind.Absolute, out var inputUrl))
    return Results.BadRequest("Invalid Url");

    //creating a short version of the url
    var random = new Random();
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890@aztxymn";
    var randomStr = new string(Enumerable.Repeat(chars, 6).Select(x => x[random.Next(x.Length)]).ToArray());

    //Map the short url with the long one
    var sUrl = new UrlManagement()
    {
        Url = url.Url,
        ShortUrl = randomStr,
    };

    //save it to the db
    db.Urls.Add(sUrl);
    db.SaveChangesAsync();

    //construct url
    var result = $"{ctx.Request.Scheme}://{ctx.Request.Host}/{sUrl.ShortUrl}";

    return Results.Ok(new UrlShortDto()
    {

        Url = result
    });
});


//short url to original one
app.MapFallback(async (ApiDbContext db, HttpContext ctx) =>
{
    var path = ctx.Request.Path.ToUriComponent().Trim('/');
    var UrlMatch = await db.Urls.FirstOrDefaultAsync(x => 
    x.ShortUrl.Trim() == path.Trim());
    if(UrlMatch == null)
    
        return Results.BadRequest("error: Invalid short Url");
    
    return Results.Redirect(UrlMatch.Url);
});

app.Run();

//to retrieve stuff out of db
class ApiDbContext : DbContext
{
    //to table "Urls"
    public virtual DbSet<UrlManagement> Urls { get; set; }

    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
    { 

    }

}


