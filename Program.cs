using PrototipoBackend.Config;
using PrototipoBackend.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Supabase (Vindo da pasta Config)
builder.Services.AddSupabaseConfig();

// Configuração de CORS
builder.Services.AddCors(options => 
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod())
);

var app = builder.Build();
app.UseCors();

// Registrando as Rotas (Vindo da pasta Endpoints)
app.MapAuthEndpoints();
app.MapCandidatoEndpoints();

app.Run();