# Usamos a imagem do SDK do .NET 9.0 para compilar o código
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /App

# Copia os arquivos e restaura as dependências
COPY . ./
RUN dotnet restore

# Compila o projeto em modo Release
RUN dotnet publish -c Release -o out

# Cria a imagem final usando apenas o necessário para rodar
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /App
COPY --from=build-env /App/out .

# Expõe a porta que o Render vai usar
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Comando para iniciar a API
ENTRYPOINT ["dotnet", "PrototipoBackend.dll"]
