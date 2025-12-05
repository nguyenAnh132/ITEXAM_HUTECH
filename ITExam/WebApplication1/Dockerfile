# ==============================
# 1️⃣ Build stage
# ==============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["WebApplication1/ITExam.csproj", "WebApplication1/"]
RUN dotnet restore "WebApplication1/ITExam.csproj"
COPY . .
WORKDIR "/src/WebApplication1"
RUN dotnet publish "ITExam.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore

# ==============================
# 2️⃣ Runtime stage
# ==============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Tối ưu môi trường
ENV ASPNETCORE_URLS=http://+:5000
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false

EXPOSE 5000
COPY --from=build /app/publish .

# Tạo user không đặc quyền
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "ITExam.dll"]
