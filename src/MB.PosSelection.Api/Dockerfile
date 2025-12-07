FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# KRİTİK DEĞİŞİKLİK: Tüm COPY yollarına 'src/' eklendi.
# Çünkü build context ana dizin, dosyalar ise src altında.

COPY ["src/MB.PosSelection.Api/MB.PosSelection.Api.csproj", "src/MB.PosSelection.Api/"]
COPY ["src/MB.PosSelection.Application/MB.PosSelection.Application.csproj", "src/MB.PosSelection.Application/"]
COPY ["src/MB.PosSelection.Domain/MB.PosSelection.Domain.csproj", "src/MB.PosSelection.Domain/"]
COPY ["src/MB.PosSelection.Infrastructure/MB.PosSelection.Infrastructure.csproj", "src/MB.PosSelection.Infrastructure/"]

# Restore işlemi için csproj'un yeni yerini gösteriyoruz
RUN dotnet restore "src/MB.PosSelection.Api/MB.PosSelection.Api.csproj"

# Kalan tüm dosyaları kopyala
COPY . .

# Build işlemini yeni path üzerinden yap
WORKDIR "/src/src/MB.PosSelection.Api"
RUN dotnet build "MB.PosSelection.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MB.PosSelection.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
USER app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MB.PosSelection.Api.dll"]