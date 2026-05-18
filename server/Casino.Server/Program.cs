using Casino.Server.Games.Blackjack;
using Casino.Server.Hubs;
using Casino.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddSingleton<IPlayerStore, PlayerStore>();
builder.Services.AddSingleton<ITableStore, TableStore>();
builder.Services.AddSingleton<IBlackjackTableStore, BlackjackTableStore>();
builder.Services.AddSingleton<BlackjackEngine>();
builder.Services.AddSingleton<BlackjackTimerService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("client", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("client");

app.MapControllers();
app.MapHub<LobbyHub>("/hubs/lobby");
app.MapHub<BlackjackHub>("/hubs/blackjack");
app.MapHub<PokerHub>("/hubs/poker");
app.MapHub<RouletteHub>("/hubs/roulette");

app.Run();
