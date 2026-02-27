using PrototipoBackend.Config;
using PrototipoBackend.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Supabase (Vindo da pasta Config)
builder.Services.AddSupabaseConfig();

// Procure a parte do builder.Services.AddCors e deixe assim:
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.WithOrigins("https://prot-tipo2-0.vercel.app") // Use o link que a Vercel te deu
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors();

// Registrando as Rotas (Vindo da pasta Endpoints)
app.MapAuthEndpoints();
app.MapCandidatoEndpoints();

app.Run();