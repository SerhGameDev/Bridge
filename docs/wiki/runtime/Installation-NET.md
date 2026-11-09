<!-- Bridge managed wiki source -->
# Установка в .NET

Для готовых примеров нужен .NET8 SDK; сам Runtime — netstandard2.1/C#9 и зависит только от Core0.2. MCP — отдельный net8 пакет и для первого примера не требуется.

Из корня checkout Bridge выполните:

```powershell
dotnet run --project samples/Bridge.Runtime.Console.Sample -c Release
dotnet new console -o work/first-runtime
dotnet add work/first-runtime reference src/Bridge.Runtime/Bridge.Runtime.csproj
```

Скопируйте весь код со страницы [первая команда](First-Command.md) в `work/first-runtime/Program.cs`, затем `dotnet run --project work/first-runtime`. Runtime автоматически подтянет Core ProjectReference. Ожидается sum=5. Не нужна сеть/listener, Unity, SDK MCP или JSON-файл.

## Локальные NuGet packages

Bridge.Runtime0.1 и Core0.2 пока **не опубликованы** в NuGet.org. Локальная проверка сама создаёт пакеты, отдельный consumer и изолированный NuGet cache:

```powershell
./tools/Verify-RuntimePackages.ps1
```

Результат — RUNTIME_NUGET_CONSUMER_PASS, nupkgs в `work/packages`, готовый `work/runtime-package-consumer` с Program.cs/NuGet.Config. Он использует **PackageReference**, не ProjectReference. Local feed mapping удерживает Bridge.* в локальном источнике; внешние зависимости NuGet восстанавливаются обычным способом. Для своего приложения перенесите его NuGet.Config и укажите абсолютный путь к вашему feed; добавьте PackageReference Bridge.Runtime0.1.0. MCP добавляйте только если нужен transport.

Нельзя устанавливать нулевой пакет из публичного feed и считать это опубликованным Bridge. После будущей разрешённой публикации обычный dotnet add package станет отдельным маршрутом. [Следующий шаг](First-Command.md).
